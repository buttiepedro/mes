# 02 · Modelo de Eventos — Nexo (MVP)

> **Documento:** `design/02-event-model.md` · **Estado:** Borrador v0.2 · **Actualizado:** 2026-07-23
> **Roles:** Software Architect · Tech Lead
> **Relacionados:** [00-tech-baseline.md](./00-tech-baseline.md) · [01-multi-tenancy-connection.md](./01-multi-tenancy-connection.md) · [03-data-schema.md](./03-data-schema.md) · [04-service-contracts.md](./04-service-contracts.md) · [05-edge-agent.md](./05-edge-agent.md) · [06-odoo-connector.md](./06-odoo-connector.md) · [../specs/specs/data-ingestion.md](../specs/specs/data-ingestion.md) · [../specs/specs/traceability.md](../specs/specs/traceability.md) · [../specs/specs/architecture.md](../specs/specs/architecture.md) · [../specs/specs/integrations.md](../specs/specs/integrations.md)
> **Modelo por capas (base funcional):** [../specs/specs/layered-architecture.md](../specs/specs/layered-architecture.md) · [../specs/specs/work-model.md](../specs/specs/work-model.md) · [../specs/specs/execution.md](../specs/specs/execution.md) · [../specs/specs/event-engine.md](../specs/specs/event-engine.md) · [../specs/specs/master-data.md](../specs/specs/master-data.md)

## Resumen ejecutivo

Este documento define el **contrato de eventos** de Nexo: la unidad normalizada que atraviesa toda la plataforma
desde la captura en planta hasta la sincronización con el ERP. Es la materialización técnica del **Evento canónico**
descrito en [`data-ingestion.md`](../specs/specs/data-ingestion.md) §3 y [`architecture.md`](../specs/specs/architecture.md) §4.4,
y el sustrato del **Event Store inmutable** de [`traceability.md`](../specs/specs/traceability.md).

Respeta el [baseline técnico](./00-tech-baseline.md) sin excepciones:

- **Backbone:** Amazon **MSK (Kafka Serverless)** detrás de **MassTransit** (ADR-T4), auth **IAM/SASL** dentro de la VPC.
- **Serialización:** **JSON + JSON Schema** en un **schema registry** (DT-02); Avro/Protobuf se reevalúan si el volumen lo exige.
- **Particionamiento:** clave por **`tenant_id`** (+ `aggregate_id` cuando el orden intra-agregado importa).
- **Fiabilidad:** **Transactional Outbox** e **Inbox/idempotencia** (`processed_events`), garantías **at-least-once**.
- **Runtime:** **.NET 8**, un `DbContext` por tenant, correlación por `tenant_id` y `correlation_id` (ADR-T9).

El alcance de este documento es **diseño**: fija el envelope, su JSON Schema, la taxonomía de topics, los patrones de
fiabilidad (con DDL ilustrativo), el catálogo de eventos del MVP y el enfoque de configuración de MassTransit sobre Kafka.
**No** implementa la aplicación. Las decisiones abiertas se registran en §8 y se promueven a ADR de [00](./00-tech-baseline.md)
al cerrarse.

**Alineación con el modelo por capas (2026-07-13).** El envelope se amplía para sostener lo que declara el
[Motor de Eventos](../specs/specs/event-engine.md): los cuatro atributos irrenunciables del hecho —**fecha, origen, valor y
evidencia**— y la **imputación** a **activo** (Capa 1), **tarea ejecutada** y **ejecución** (Capa 3). La **evidencia** entra
como ciudadano de primera clase (referencia inmutable a Files/Media, nunca el binario), y el catálogo suma los eventos de las
capas nuevas —Proceso, Ejecución, Tarea instanciada y Master Data— sin renombrar **ninguno** de los existentes:
`nexo.production.registered` y compañía **siguen tal cual**, ahora imputables a una ejecución. El MVP soporta **ambos
perfiles** (Lote y Proyecto) y **DAG completo**, y **ningún evento del MVP depende del conector ERP**, que es opcional
([master-data.md](../specs/specs/master-data.md), [integrations.md](../specs/specs/integrations.md)).

---

## 1. Envelope del Evento canónico

Todo evento de Nexo —sin importar su origen (`device` / `vision` / `manual` / `system` / `api` / `file`) ni su dominio— viaja
dentro de un **envelope común**. El envelope separa **metadatos de transporte y trazabilidad** (estables, conocidos por toda la
plataforma) del **`payload`** (específico del `type`, gobernado por su propio JSON Schema). Esta separación permite que
Ingestion, el broker, Traceability y los conectores operen sobre el envelope sin conocer el detalle de cada dominio.

**Los cuatro atributos del hecho, mapeados al envelope** ([event-engine.md](../specs/specs/event-engine.md) §4.1):

| Atributo funcional | Dónde vive en el envelope |
|---|---|
| **Fecha** (terna: ocurrencia · captura · ingesta) | `occurred_at` · `origin_metadata.captured_at` · `ingested_at` |
| **Origen** (naturaleza · identidad de la fuente · cadena de custodia) | `source` · `device_id` / `operator_id` / `origin_metadata.component` · `origin_metadata` |
| **Valor** (tipado, con unidad y confianza) | `payload` (gobernado por el JSON Schema del `type`) |
| **Evidencia** (referencia, nunca el binario) | `evidence[]` |
| *Imputación* (activo · tarea ejecutada · ejecución) | `context.asset` · `task_instance_id` · `execution_id` (+ `attribution`) |

### 1.1 Campos del envelope

| Campo | Tipo | Req. | Descripción | Notas de diseño |
|---|---|:--:|---|---|
| `event_id` | `string` (UUID) | ✔ | Identidad única e inmutable del evento. | **UUIDv7** (ordenable en el tiempo); base de idempotencia junto con `dedup_key`. |
| `tenant_id` | `string` (UUID) | ✔ | Empresa dueña del evento. | Resuelto en la admisión (host/subdominio o claim JWT); **nunca** lo define el payload ([data-ingestion.md](../specs/specs/data-ingestion.md) §3). Clave de partición por defecto. |
| `type` | `string` | ✔ | Tipo canónico del evento: `nexo.<domain>.<event>`. | p. ej. `nexo.production.registered`. El segmento `<domain>` es la **categoría** que las specs llaman `type` (production/scrap/quality/downtime/reading/machine_event/custom) + dominios de plataforma (tenant/device/integration). Ver §1.4. |
| `occurred_at` | `string` (date-time) | ✔ | **Tiempo de origen**: cuándo ocurrió el hecho en planta. | RFC 3339 / ISO-8601 UTC. Preferente para negocio; puede venir del PLC/OPC UA o sellarse en el agente ([data-ingestion.md](../specs/specs/data-ingestion.md) §8). |
| `ingested_at` | `string` (date-time) | ✔ | **Tiempo de ingesta**: cuándo la nube admitió y persistió el evento. | Diagnóstico de latencia (`ingested_at − occurred_at`) y detección de tardíos. |
| `source` | `enum` | ✔ | Procedencia: `device` \| `vision` \| `manual` \| `system` \| `api` \| `file`. | Los **cuatro generadores** de [event-engine.md](../specs/specs/event-engine.md) §3.1 (sensor, visión, persona, **sistema**) + `api`/`file` por continuidad. Determina qué metadatos de origen acompañan ([traceability.md](../specs/specs/traceability.md) §3.1). Ampliación **aditiva** del enum (§3.3, DT-EV-13). |
| `device_id` | `string` | — | Dispositivo emisor (si aplica). | Del mapeo de tagging ([devices.md](../specs/specs/devices.md)); requerido cuando `source=device` o `source=vision` (la cámara **es** un dispositivo). |
| `context` | `object` | — | Contexto físico: `site` / `line` / `asset`. | Planta → línea → **activo**. Se completa por el contexto del dispositivo/operario. Regla de Capa 1: **ningún dato flota**; toda señal tiene activo dueño ([digital-twin.md](../specs/specs/digital-twin.md)). |
| `operator_id` | `string` | — | Operario asociado. | Requerido cuando `source=manual`. Base de productividad por persona y de no repudio. |
| `execution_id` | `string` | — | **Ejecución** (sabor Lote o Proyecto) a la que se imputa el hecho — Capa 3. | Contexto de agregación de casi todas las métricas ([execution.md](../specs/specs/execution.md) §2). Vacío en eventos de plataforma y en hechos aún **pendientes de imputación**. |
| `task_instance_id` | `string` | — | **Tarea ejecutada** (tarea instanciada) a la que se imputa el hecho — Capa 3. | Unidad de imputación de tiempo, consumo, evidencia y calidad; es lo que hace medibles **progreso** y **espera** ([event-engine.md](../specs/specs/event-engine.md) §4.3). |
| `attribution` | `object` | — | Cómo se resolvió la imputación: `method`, `confidence`, `pending`, `resolved_by`. | `method ∈ {explicit, active_execution, time_window, unassigned}`. Un evento **no imputado no se descarta**: viaja con `pending=true`, alimenta métricas de activo y va a la bandeja de imputación (E24 de [execution.md](../specs/specs/execution.md)). La reimputación es un evento de corrección, nunca una edición. |
| `evidence` | `array` | — | **Referencias** a los artefactos probatorios del hecho. | El evento **no contiene** la evidencia: la **referencia**. El binario vive en Files/Media aislado por tenant ([event-engine.md](../specs/specs/event-engine.md) §5). Offline-first: se admite con `status=pending` y se materializa al sincronizar, **sin duplicar el evento**. |
| `productive` | `boolean` | — | Si el hecho cuenta como **actividad productiva**. | Resuelto en la admisión según la configuración **versionada** por tenant/señal ([event-engine.md](../specs/specs/event-engine.md) §6). Base del cálculo de tiempos muertos; la versión de la política queda en `origin_metadata.productive_policy_version` para poder reproyectar. |
| `shift` | `string` | — | Turno resuelto por contexto temporal/planta. | Crítico para KPIs por turno ([production.md](../specs/specs/production.md) §7.3). **Fijado en el evento**: un cambio posterior de calendario no reescribe la historia. |
| `payload` | `object` | ✔ | Contenido normalizado del hecho (**el `valor`**), según `type`. | Gobernado por el JSON Schema específico del `type`+`schema_version` (§3). Unidades/escalas/códigos **ya convertidos**; toda magnitud lleva unidad del catálogo ([master-data.md](../specs/specs/master-data.md) §2.4) y los valores inferidos llevan `confidence`. |
| `dedup_key` | `string` | ✔ | Clave determinística de deduplicación/idempotencia. | Derivada de atributos invariantes del hecho, **no** del momento de envío ([data-ingestion.md](../specs/specs/data-ingestion.md) §5.2). |
| `origin_metadata` | `object` | — (recom.) | Linaje técnico: protocolo, firmware, calidad del dato, agente, offset de reloj, ref. al crudo, **componente y versión de lógica** (origen `system`), versión de la política de "productivo". | Sustento de la cadena de custodia y del diagnóstico ([traceability.md](../specs/specs/traceability.md) §4.2). El origen `system` **no es menos trazable**: declara qué componente y qué versión de lógica generó el hecho, para poder reproducirlo. |
| `schema_version` | `integer` | ✔ | Versión **mayor** del contrato del `payload`/envelope. | Igual al `vN` del topic. Cambios aditivos no la incrementan; cambios rompientes sí (§3). |
| `correlation_id` | `string` (UUID) | ✔ | Hilo que une eventos de una misma operación/flujo. | Se propaga por toda la cadena (Gateway→Ingestion→broker→dominios→ERP) y a logs/trazas OTel. |
| `causation_id` | `string` (UUID) | — | `event_id` del evento que causó éste. | Habilita reconstruir árboles de causalidad; opcional pero recomendado en reacciones. |
| `sequence` | `integer` | — | Posición monótona por partición asignada por el Event Store. | Orden lógico de ingesta, complementario a los timestamps ([traceability.md](../specs/specs/traceability.md) §4.1). |

> **Tres tiempos.** `occurred_at` (origen) e `ingested_at` (ingesta) van en el envelope; el **tiempo de captura del
> agente** se conserva en `origin_metadata.captured_at`. Los tres, juntos, permiten diagnosticar latencia, ordenar
> por tiempo de origen y tratar *late arrivals* ([data-ingestion.md](../specs/specs/data-ingestion.md) §8).

> **Aislamiento por tenant.** El envelope siempre lleva `tenant_id`, pero el aislamiento **físico** lo garantiza la
> DB-per-tenant y el particionamiento del topic ([01-multi-tenancy-connection.md](./01-multi-tenancy-connection.md)).
> El `tenant_id` del envelope es el mecanismo de **enrutamiento y correlación**, no el control de acceso.

> **Imputación, no invención.** `execution_id`/`task_instance_id` son **opcionales por diseño**: un contador de máquina puede
> reportar piezas sin ejecución activa. En ese caso el evento se admite con `attribution.method="unassigned"` y
> `pending=true`, alimenta las métricas de **activo** (utilización, silencio) y espera imputación diferida del supervisor.
> **Nunca** se descarta ni se fuerza a una ejecución arbitraria: *es preferible un hecho sin dueño y visible que un hecho
> asignado mal y silencioso* ([execution.md](../specs/specs/execution.md) §13.3).

### 1.2 JSON Schema del envelope (Draft 2020-12)

```json
{
  "$schema": "https://json-schema.org/draft/2020-12/schema",
  "$id": "https://schemas.nexo.io/events/envelope/v1.json",
  "title": "NexoCanonicalEventEnvelope",
  "type": "object",
  "additionalProperties": false,
  "required": [
    "event_id", "tenant_id", "type", "occurred_at", "ingested_at",
    "source", "payload", "dedup_key", "schema_version", "correlation_id"
  ],
  "properties": {
    "event_id":       { "type": "string", "format": "uuid",
                        "description": "UUIDv7 único e inmutable del evento" },
    "tenant_id":      { "type": "string", "format": "uuid" },
    "type":           { "type": "string",
                        "pattern": "^nexo\\.[a-z0-9_]+\\.[a-z0-9_]+$",
                        "description": "nexo.<domain>.<event>" },
    "occurred_at":    { "type": "string", "format": "date-time" },
    "ingested_at":    { "type": "string", "format": "date-time" },
    "source":         { "type": "string",
                        "enum": ["device", "vision", "manual", "system", "api", "file"],
                        "description": "Naturaleza del generador (event-engine.md §3.1)" },
    "device_id":      { "type": "string" },
    "context": {
      "type": "object",
      "additionalProperties": false,
      "properties": {
        "site":  { "type": "string" },
        "line":  { "type": "string" },
        "asset": { "type": "string", "description": "Activo dueño del hecho (Capa 1)" }
      }
    },
    "operator_id":    { "type": "string" },
    "execution_id":     { "type": "string",
                        "description": "Ejecución (Lote|Proyecto) a la que se imputa el hecho — Capa 3" },
    "task_instance_id": { "type": "string",
                        "description": "Tarea ejecutada (tarea instanciada) a la que se imputa el hecho — Capa 3" },
    "attribution": {
      "type": "object",
      "additionalProperties": false,
      "description": "Cómo se resolvió la imputación (event-engine.md §4.3)",
      "properties": {
        "method":      { "type": "string",
                         "enum": ["explicit", "active_execution", "time_window", "unassigned"] },
        "confidence":  { "type": "number", "minimum": 0, "maximum": 1 },
        "pending":     { "type": "boolean", "default": false,
                         "description": "true = a la bandeja de pendientes de imputación; no contamina KPIs de ejecución" },
        "resolved_by": { "type": "string",
                         "description": "Componente o usuario que imputó / reimputó" }
      }
    },
    "evidence": {
      "type": "array",
      "description": "Referencias a los artefactos probatorios. El binario vive en Files/Media, aislado por tenant.",
      "items": {
        "type": "object",
        "additionalProperties": false,
        "required": ["evidence_id", "kind", "status"],
        "properties": {
          "evidence_id":     { "type": "string" },
          "kind":            { "type": "string",
                               "enum": ["photo", "file", "sensor_reading", "signature",
                                        "video_frame", "structured_note"] },
          "media_ref":       { "type": "string",
                               "description": "Puntero inmutable en Files/Media (ausente mientras status=pending)" },
          "media_type":      { "type": "string", "description": "MIME" },
          "content_hash":    { "type": "string",
                               "description": "Huella de integridad, p. ej. 'sha256:...'" },
          "size_bytes":      { "type": "integer", "minimum": 0 },
          "captured_at":     { "type": "string", "format": "date-time" },
          "status":          { "type": "string", "enum": ["pending", "materialized", "verified"] },
          "requirement_ref": { "type": "string",
                               "description": "Requisito de evidencia de la Tarea (Capa 2) que satisface" }
        }
      }
    },
    "productive":     { "type": "boolean",
                        "description": "Marca de actividad productiva resuelta en la admisión (event-engine.md §6)" },
    "shift":          { "type": "string" },
    "payload":        { "type": "object",
                        "description": "Contenido según type; validado por el schema específico" },
    "dedup_key":      { "type": "string", "minLength": 1, "maxLength": 512 },
    "origin_metadata": {
      "type": "object",
      "additionalProperties": true,
      "properties": {
        "protocol":     { "type": "string",
                          "enum": ["manual", "file", "http", "s7", "opcua", "modbus", "mqtt"] },
        "firmware":     { "type": "string" },
        "agent_id":     { "type": "string" },
        "captured_at":  { "type": "string", "format": "date-time",
                          "description": "Tiempo de captura del agente (3er reloj)" },
        "clock_offset_ms": { "type": "integer",
                          "description": "Offset de reloj del edge respecto de la nube" },
        "data_quality": { "type": "string",
                          "enum": ["good", "suspect", "substituted", "interpolated", "out_of_range"] },
        "raw_ref":      { "type": "string",
                          "description": "Referencia a la evidencia cruda (Files/Media o Event Store)" },
        "component":    { "type": "string",
                          "description": "Componente emisor cuando source=system (p. ej. 'execution.dag_scheduler')" },
        "logic_version":{ "type": "string",
                          "description": "Versión de la lógica que derivó el hecho (reproducibilidad del origen system)" },
        "productive_policy_version": { "type": "string",
                          "description": "Versión de la configuración 'productivo' aplicada al resolver el flag" },
        "model_version":{ "type": "string",
                          "description": "Modelo/versión del detector cuando source=vision" }
      }
    },
    "schema_version": { "type": "integer", "minimum": 1 },
    "correlation_id": { "type": "string", "format": "uuid" },
    "causation_id":   { "type": "string", "format": "uuid" },
    "sequence":       { "type": "integer", "minimum": 0 }
  },
  "allOf": [
    {
      "if":   { "properties": { "source": { "const": "device" } } },
      "then": { "required": ["device_id"] }
    },
    {
      "if":   { "properties": { "source": { "const": "vision" } } },
      "then": { "required": ["device_id"] }
    },
    {
      "if":   { "properties": { "source": { "const": "manual" } } },
      "then": { "required": ["operator_id"] }
    },
    {
      "if":   { "properties": { "source": { "const": "system" } } },
      "then": { "required": ["origin_metadata"] }
    },
    {
      "if":   { "properties": { "task_instance_id": { "type": "string" } },
                "required":   ["task_instance_id"] },
      "then": { "required": ["execution_id"] }
    }
  ]
}
```

> **Invariante de imputación.** Una tarea instanciada **siempre** pertenece a una ejecución: si viaja `task_instance_id`,
> `execution_id` es obligatorio. Lo inverso no aplica (hay hechos de ejecución sin tarea: creación, cierre, hito).

> El `payload` se valida en **dos pasos**: (1) contra este envelope (`payload` es `object`), y (2) contra el JSON Schema
> específico de `type`+`schema_version` resuelto en el registry (§3). Se evita un único mega-schema para no acoplar el
> envelope a cada dominio.

### 1.3 Ejemplo — `nexo.production.registered` (v1)

```json
{
  "event_id": "018f7c2a-9b3e-7a41-8c1d-2f5e6a7b8c90",
  "tenant_id": "9c3b1e77-2d4a-4b8f-9e1a-6f0c2d3b4a55",
  "type": "nexo.production.registered",
  "occurred_at": "2026-07-11T14:03:12.480Z",
  "ingested_at": "2026-07-11T14:03:18.902Z",
  "source": "device",
  "device_id": "plc-l3-contador-01",
  "context": { "site": "PLANTA-CBA", "line": "L3", "asset": "EXTRUSORA-07" },
  "shift": "TARDE",
  "payload": {
    "order_id": "OP-2026-000482",
    "run_id": "RUN-77af12",
    "product_sku": "PT-500-XL",
    "uom": "uds",
    "good_qty": 37,
    "nonconforming_qty": 1,
    "counter_start": 1450,
    "counter_end": 1488,
    "counter_rollover": false
  },
  "dedup_key": "plc-l3-contador-01|counter|1450-1488|2026-07-11T14:03:12Z",
  "origin_metadata": {
    "protocol": "file",
    "agent_id": "edge-cba-01",
    "captured_at": "2026-07-11T14:03:12.500Z",
    "clock_offset_ms": 20,
    "data_quality": "good"
  },
  "schema_version": 1,
  "correlation_id": "018f7c2a-9b3e-7a41-8c1d-2f5e6a7b8c00"
}
```

### 1.4 Nomenclatura de `type` (reconciliación con las specs)

Las specs usan la forma corta `type=production | scrap | quality | downtime | reading | machine_event | custom`
para el **enrutamiento primario**. En el diseño, ese valor es el **segmento `<domain>`** de un identificador
más específico, para poder distinguir eventos dentro del mismo dominio (p. ej. `nexo.downtime.started` vs `nexo.downtime.ended`).

| Concepto | Forma | Ejemplo |
|---|---|---|
| **Categoría** (specs `type=`) | `<domain>` | `production` |
| **Tipo canónico** (envelope `type`) | `nexo.<domain>.<event>` | `nexo.production.registered` |
| **Topic** (broker) | `nexo.<domain>.<event>.v<major>` | `nexo.production.registered.v1` |
| **Contrato C#** (MassTransit) | `PascalCase` | `ProductionRegistered` |

Los dominios del modelo por capas siguen **exactamente** la misma convención y se suman como `<domain>` nuevos:
`process` (Capa 2), `execution` y `task` (Capa 3) y `masterdata` (catálogos). **Ningún `type` existente se renombra.**

### 1.5 Ejemplo — `nexo.task.completed` (v1), perfil **Proyecto**, con evidencia e imputación

Cierre de la tarea instanciada `P10 · Ensayo de estanqueidad` (que además es **hito**) de la ejecución de sabor Proyecto
`PRY-2026-012`. Muestra los cuatro atributos del hecho y la imputación completa activo → tarea ejecutada → ejecución.

```json
{
  "event_id": "019046f1-2c7b-7d12-a3e4-91b0c7d5e2a3",
  "tenant_id": "9c3b1e77-2d4a-4b8f-9e1a-6f0c2d3b4a55",
  "type": "nexo.task.completed",
  "occurred_at": "2026-07-21T11:42:07.120Z",
  "ingested_at": "2026-07-21T12:05:44.310Z",
  "source": "manual",
  "operator_id": "OP-1042",
  "context": { "site": "OBRA-TORRE-CALLAO", "asset": "FRENTE-VIDRIADO-N3" },
  "execution_id": "PRY-2026-012",
  "task_instance_id": "PRY-2026-012/P10#1",
  "attribution": { "method": "explicit", "pending": false, "resolved_by": "OP-1042" },
  "productive": true,
  "payload": {
    "process_ref": { "process_id": "PRC-OBRA-FV", "version": "1.0" },
    "task_ref": "P10",
    "outcome": "completed",
    "is_milestone": true,
    "completion_criterion": "quality_gate",
    "worked_time_s": 25200,
    "progress_pct": 100,
    "quality_gate": { "inspection_id": "INS-2026-0914", "result": "pass" }
  },
  "evidence": [
    {
      "evidence_id": "EV-9f21a",
      "kind": "file",
      "media_ref": "files://9c3b1e77/2026/07/EV-9f21a.pdf",
      "media_type": "application/pdf",
      "content_hash": "sha256:3b1f...c7a2",
      "size_bytes": 481233,
      "captured_at": "2026-07-21T11:40:55Z",
      "status": "materialized",
      "requirement_ref": "PRC-OBRA-FV@1.0/P10/EV-protocolo-ensayo"
    },
    {
      "evidence_id": "EV-9f21b",
      "kind": "photo",
      "captured_at": "2026-07-21T11:41:30Z",
      "status": "pending",
      "requirement_ref": "PRC-OBRA-FV@1.0/P10/EV-foto-junta"
    }
  ],
  "dedup_key": "PRY-2026-012|P10#1|completed|2026-07-21T11:42:07Z",
  "origin_metadata": {
    "protocol": "manual",
    "agent_id": "tablet-obra-07",
    "captured_at": "2026-07-21T11:42:07.200Z",
    "clock_offset_ms": -140,
    "data_quality": "good",
    "productive_policy_version": "2026.07-r1"
  },
  "schema_version": 1,
  "correlation_id": "019046f1-2c7b-7d12-a3e4-91b0c7d5e200"
}
```

**Lectura del ejemplo:**

- La foto viaja `status: "pending"`: la tablet estaba **sin red** al capturarla. El artefacto se sube después y se completa
  con `nexo.task.evidence_attached` (`causation_id` = este `event_id`), **sin duplicar** el cierre —misma `dedup_key`—.
  Mientras tanto la tarea queda con **deuda de evidencia**, que es una métrica en sí misma
  ([event-engine.md](../specs/specs/event-engine.md) §5.4). Si la política de la tarea fuera *obligatoria bloqueante*, el
  cierre se rechaza en `Nexo.Execution` (422) y este evento **no existe**.
- `occurred_at` ≠ `ingested_at` por 23 minutos (store-and-forward). El hecho se computa **cuando ocurrió**, y las métricas
  de la ventana afectada se recalculan ([event-engine.md](../specs/specs/event-engine.md) §8).
- El mismo envelope, con `execution_id` de sabor Lote y sin `is_milestone`, sirve al perfil repetitivo: **un solo contrato
  para los dos perfiles**.

---

## 2. Domain events vs Integration events

Nexo distingue **eventos de dominio** (internos a un *bounded context*) de **eventos de integración** (contratos públicos
que cruzan el backbone). La confusión entre ambos es una fuente clásica de acoplamiento; el baseline (Clean Architecture,
ADR-T2) exige separarlos.

| Aspecto | **Domain event** | **Integration event** |
|---|---|---|
| Propósito | Expresar un hecho dentro del agregado; disparar invariantes/side-effects locales | Notificar a otros contextos un hecho relevante y estable |
| Alcance | Dentro del servicio (proceso) | Entre servicios (backbone Kafka/MSK) |
| Transporte | **MediatR** (in-process), sin salir del `DbContext` | **MassTransit → Kafka**, vía **Outbox** |
| Acoplamiento | Al modelo de dominio (puede cambiar seguido) | Contrato versionado y gobernado (cambia con cuidado) |
| Forma | Objeto de dominio rico (VOs, entidades) | **DTO plano** serializable (JSON), envelope canónico |
| Estabilidad | Interna; refactorizable libremente | Pública; sujeta a compatibilidad (§3) |
| Ejemplo | `RunClosedDomainEvent` (recalcula acumulados) | `nexo.production.run_closed.v1` (dispara push a Odoo) |

**Flujo típico** (Clean Architecture + Outbox):

```mermaid
flowchart LR
    subgraph Svc["Servicio (bounded context)"]
        AGG["Agregado<br/>(Domain)"]
        DE["Domain event<br/>(MediatR, in-process)"]
        H["Handler / Application<br/>(traduce a Integration event)"]
        OB[("Outbox<br/>(misma tx que el estado)")]
    end
    AGG -->|"emite"| DE --> H
    H -->|"map DTO + envelope"| OB
    OB -->|"relay tras commit"| K["Kafka / MSK"]
    K --> OTHER["Otros contextos<br/>(consumers)"]
```

### 2.1 Convención de nombres

- **Domain events:** PascalCase con sufijo `DomainEvent` y **verbo en pasado**: `ProductionRegisteredDomainEvent`,
  `DowntimeStartedDomainEvent`. Viven en `Nexo.<Servicio>.Domain`.
- **Integration events (contrato C#):** PascalCase, **verbo en pasado**, sin sufijo: `ProductionRegistered`,
  `DowntimeStarted`, `OdooSyncCompleted`. Viven en un paquete de contratos compartido (`Nexo.Contracts.<Domain>`),
  para que productor y consumidores compartan el tipo.
- **Tipo canónico (wire):** `nexo.<domain>.<event>` en `snake_case`: `nexo.production.registered`.
- **Comandos** (no eventos): imperativo presente: `PushProductionToOdoo`, `ProvisionTenant`. Se envían punto-a-punto, no se
  publican como hechos.

> **Regla:** un evento de integración es un **hecho ya ocurrido** (inmutable), nunca una orden. Si el emisor "quiere que
> alguien haga algo" es un **comando**, no un evento. `OdooSyncRequested` es la excepción aparente: se modela como
> **evento** porque representa el hecho "se necesita sincronizar" que el orquestador del conector consume de forma
> desacoplada (coreografía), no como RPC directo.

---

## 3. Serialización y compatibilidad

### 3.1 Formato y registry

- **Formato:** **JSON UTF-8** (DT-02). Legible, tooling universal, bajo costo de gobierno en el MVP. Se paga overhead de
  tamaño/CPU frente a binarios; aceptable para el volumen del MVP y mitigable con compresión del topic (`zstd`).
- **Schema registry:** cada `type`+`vN` tiene un **JSON Schema** publicado y versionado. Default: **AWS Glue Schema
  Registry** (nativo AWS/MSK, soporta JSON Schema y validación en el cliente MassTransit); alternativa compatible:
  registry estilo Confluent. La resolución es `type` → schema; el `schema_version` del envelope selecciona la versión.
- **Ubicación de los schemas:** versionados en el monorepo (`/contracts/events/**.json`) y publicados al registry en CI;
  el envelope referencia por `type`+`schema_version`, no por URL embebida (evita acoplar el evento al hosting del schema).

```mermaid
flowchart LR
    PROD["Productor<br/>(MassTransit)"] -->|"1 · valida payload vs schema local"| SR["Schema Registry<br/>(Glue / JSON Schema)"]
    PROD -->|"2 · publica envelope JSON"| K["Kafka / MSK"]
    K --> CONS["Consumidor"]
    CONS -->|"3 · resuelve schema por type+version"| SR
    CONS -->|"4 · valida y deserializa"| APP["Handler"]
```

### 3.2 Estrategia de versionado

Dos ejes, deliberadamente simples para el MVP:

1. **`schema_version` (entero = versión mayor)**: nombra el topic (`.vN`). Solo se incrementa ante un **cambio rompiente**.
2. **Evolución aditiva dentro de una versión**: agregar campos **opcionales** con default no incrementa `schema_version`
   ni crea topic nuevo. Los consumidores viejos los ignoran; los nuevos los aprovechan.

### 3.3 Compatibilidad

Se adopta compatibilidad **BACKWARD_TRANSITIVE** como política por defecto del registry: un consumidor nuevo puede leer
**todo** el histórico del topic. Es la política correcta para un sistema con **reproceso/replay** y **Event Store de
retención larga** ([traceability.md](../specs/specs/traceability.md)): reconstruir un read model relee eventos antiguos.

| Cambio | ¿Compatible? | Acción |
|---|---|---|
| Agregar campo **opcional** (con default) | ✅ Sí | Aditivo; misma `vN`; sin migración |
| Ampliar un `enum` (nuevo valor) | ⚠️ Depende | Aditivo si los consumidores toleran valores desconocidos; documentar |
| Renombrar/eliminar campo | ❌ No | Rompiente; nuevo topic `v(N+1)` + período de doble publicación |
| Cambiar tipo/semántica de un campo | ❌ No | Rompiente; nuevo topic `v(N+1)` |
| Volver **requerido** un campo antes opcional | ❌ No | Rompiente; nuevo topic `v(N+1)` |

> **La ampliación del envelope del modelo por capas es aditiva.** `execution_id`, `task_instance_id`, `attribution`,
> `evidence` y `productive` son **opcionales**: los consumidores viejos los ignoran y **no se incrementa `schema_version`**
> ni se crean topics nuevos para los eventos ya existentes. El único punto sensible es la **ampliación del enum `source`**
> (`system`, `vision`): se rige por la fila "Ampliar un `enum`" y exige que los consumidores toleren valores desconocidos
> (DT-EV-13).

### 3.4 Migración de una versión rompiente (`vN` → `v(N+1)`)

1. Publicar el schema `v(N+1)` en el registry.
2. **Doble publicación**: los productores emiten en `nexo.<domain>.<event>.vN` **y** `.v(N+1)` durante una ventana.
3. Migrar consumidores a `.v(N+1)` de forma independiente (despliegue por servicio).
4. Cuando todos consumen `.v(N+1)` y la retención de `.vN` venció, **retirar** `.vN`.
5. Para reproceso histórico se conserva el schema `vN` en el registry aunque el topic se retire.

> **Inmutabilidad.** Un evento ya ingerido **no** se re-serializa a una versión nueva. La migración es de **contrato de
> productor**, no de los eventos históricos, que permanecen en su `vN` original ([data-ingestion.md](../specs/specs/data-ingestion.md) §3).

---

## 4. Taxonomía de topics en Kafka/MSK

### 4.1 Nomenclatura

```
nexo.<domain>.<event>.v<major>
└┬─┘ └──┬──┘ └──┬──┘ └──┬──┘
 │      │       │       └─ versión mayor del contrato (= schema_version)
 │      │       └───────── evento en snake_case (pasado): registered, started, ended...
 │      └───────────────── bounded context / categoría: production, scrap, quality,
 │                          downtime, reading, machine_event, device, tenant, integration, custom
 └──────────────────────── raíz del namespace de la plataforma
```

Ejemplos: `nexo.production.registered.v1`, `nexo.downtime.started.v1`, `nexo.reading.ingested.v1`,
`nexo.integration.odoo_sync_completed.v1`.

**Topic por tipo de evento** (fine-grained), no un topic gigante por dominio. Ventajas: retención y particiones
por evento (readings ≠ órdenes), ACLs finas, consumidores que se suscriben solo a lo que necesitan, y evolución de
esquema aislada. El costo (más topics) es asumible en MSK Serverless.

### 4.2 Particionamiento

- **Clave por defecto:** `tenant_id`. Garantiza (a) aislamiento de orden y throughput por tenant y (b) que un tenant
  ruidoso no descoordine el orden de otro. Alineado con [00](./00-tech-baseline.md) §4.1.
- **Clave compuesta `tenant_id|aggregate_id`** cuando **importa el orden intra-agregado**: contador por dispositivo,
  ciclo de una orden, secuencia de una parada por máquina. Kafka preserva orden **dentro de la partición**; la clave
  define qué eventos quedan co-ordenados.

| Evento | Clave de partición | Por qué |
|---|---|---|
| `nexo.reading.ingested.v1` | `tenant_id \| device_id` | El delta de contador depende del orden por dispositivo |
| `nexo.production.registered.v1` | `tenant_id \| order_id` | Acumulados por orden en secuencia |
| `nexo.production.run_closed.v1` | `tenant_id \| order_id` | Cierre después de todos los registros de la corrida |
| `nexo.downtime.started.v1` / `ended.v1` | `tenant_id \| asset_id` | Inicio/fin ordenados por máquina |
| `nexo.device.status_changed.v1` | `tenant_id \| device_id` | Transiciones de estado ordenadas |
| `nexo.execution.*.v1` | `tenant_id \| execution_id` | Ciclo de vida, consumo y avance ordenados por ejecución |
| `nexo.task.*.v1` | `tenant_id \| execution_id` | **La tarea se co-ordena con su ejecución**, no consigo misma: habilitada→iniciada→terminada y el recálculo de progreso dependen de ese orden común |
| `nexo.process.version_published.v1` | `tenant_id \| process_id` | Versiones de un Proceso en secuencia (una sola vigente) |
| `nexo.masterdata.*.v1` | `tenant_id \| catalog` | *Upserts* del mismo catálogo en secuencia (idempotencia por código) |
| `nexo.tenant.provisioned.v1` | `tenant_id` | Evento de plataforma (topic global) |

> **Orden global.** Kafka **no** garantiza orden entre particiones. Los dominios que necesiten línea de tiempo global
> reordenan por `occurred_at` dentro de ventanas de tolerancia ([data-ingestion.md](../specs/specs/data-ingestion.md) §8).

```mermaid
flowchart TB
    subgraph T["Topic nexo.production.registered.v1"]
        P0["Partición 0"]
        P1["Partición 1"]
        P2["Partición 2"]
    end
    E1["tenant=A order=OP-1"] -->|"hash(A|OP-1)"| P0
    E2["tenant=A order=OP-1"] -->|"misma clave → misma partición (orden)"| P0
    E3["tenant=A order=OP-2"] -->|"hash(A|OP-2)"| P2
    E4["tenant=B order=OP-9"] -->|"hash(B|OP-9)"| P1
    P0 --> C0["Consumer inst. 0"]
    P1 --> C1["Consumer inst. 1"]
    P2 --> C2["Consumer inst. 2"]
```

### 4.3 Consumer groups

- **Un consumer group por servicio consumidor**: `nexo.<servicio>` (p. ej. `nexo.traceability`, `nexo.dashboards`,
  `nexo.connectors`). Cada servicio recibe **su propia copia** del stream (pub/sub) y avanza offsets de forma
  independiente.
- El **paralelismo** de un servicio se logra con **más instancias** en el mismo group (hasta N = nº de particiones).
- **Rebalanceo** gestionado por MassTransit/consumer Kafka; los handlers deben ser **idempotentes** (§5) porque un
  rebalanceo puede re-entregar el último batch no confirmado.

### 4.4 Retención y reproceso

| Clase de topic | Retención en Kafka (MVP) | Fuente de verdad de largo plazo |
|---|---|---|
| Eventos de dominio (`production`, `scrap`, `quality`, `downtime`) | 30 días | **Event Store** por tenant (inmutable, retención larga) — [traceability.md](../specs/specs/traceability.md) |
| Eventos del modelo por capas (`process`, `execution`, `task`, `masterdata`) | 30 días | **Event Store** por tenant. Una ejecución de sabor **Proyecto** puede durar meses: el estado **nunca** se reconstruye desde Kafka, siempre desde el Event Store |
| `reading.ingested` (alta frecuencia) | 7 días | **Time-series** del tenant (append-only, downsampling) — [scalability.md](../specs/specs/scalability.md) §5 |
| Eventos de plataforma (`tenant`, `device`, `integration`) | 30 días | Control Plane DB / Audit |

- **Reproceso / replay:** re-consumir desde un offset o timestamp reconstruye read models (CQRS) sin violar
  inmutabilidad ([data-ingestion.md](../specs/specs/data-ingestion.md) §9). Para reprocesos que exceden la retención de
  Kafka, la fuente es el **Event Store** (relee y re-publica a un topic de reproceso dedicado).
- **Compactación:** los topics de eventos de negocio son *append/log* (no compactados) para preservar el historial. Solo
  se consideraría compactación en topics de **estado/snapshot** (fuera del MVP).
- La retención concreta por plan comercial es una **decisión abierta** (ver [scalability.md](../specs/specs/scalability.md) PA-3 y §8).

### 4.5 Dead-Letter Queue (DLQ)

- **DLQ por topic**: `nexo.<domain>.<event>.v<major>.dlq`. A ella van los mensajes que agotan reintentos o son
  no-procesables (envelope inválido, schema desconocido, error permanente de negocio).
- El mensaje en DLQ conserva el **envelope original** + headers de diagnóstico (`x-exception`, `x-original-topic`,
  `x-attempts`, `x-consumer`). MassTransit adjunta metadatos de fallo automáticamente.
- **Cuarentena de ingesta** (eventos inválidos/duplicados/no contextualizados de [data-ingestion.md](../specs/specs/data-ingestion.md) §4)
  se modela como DLQ del pipeline de admisión, con reinyección tras corrección.
- **Reproceso de DLQ:** operación auditada (quién, qué rango, con qué versión de lógica) — [data-ingestion.md](../specs/specs/data-ingestion.md) §9.

```mermaid
flowchart LR
    IN["Topic nexo.X.v1"] --> C["Consumer<br/>(MassTransit)"]
    C -->|"ok"| DONE["Handler + Inbox"]
    C -->|"error transitorio"| R["Retry backoff<br/>(n intentos)"]
    R --> C
    C -->|"agota retries / error permanente"| DLQ[("nexo.X.v1.dlq")]
    DLQ -->|"corrección + reinyección auditada"| IN
```

---

## 5. Fiabilidad

Garantía extremo a extremo: **at-least-once + idempotencia**. Nunca se pierde un evento (outbox), y los duplicados por
reintentos/reentregas no cambian el estado (inbox). Alineado con [data-ingestion.md](../specs/specs/data-ingestion.md) §5
y [architecture.md](../specs/specs/architecture.md) §4.3.

### 5.1 Transactional Outbox

**Problema:** publicar al broker y persistir el cambio de estado son dos sistemas distintos; si uno falla queda
inconsistencia (evento sin estado, o estado sin evento). **Solución:** escribir el evento en una tabla `outbox` **en la
misma transacción** que el cambio de estado; un *relay* lo publica luego y marca `published_at`.

```mermaid
sequenceDiagram
    autonumber
    participant App as Handler (Application)
    participant DB as DB del tenant (EF Core)
    participant Rel as Outbox Relay
    participant K as Kafka / MSK
    App->>DB: BEGIN TX
    App->>DB: UPDATE estado de dominio
    App->>DB: INSERT INTO outbox (envelope)
    App->>DB: COMMIT  (atómico)
    Rel->>DB: SELECT pendientes (published_at IS NULL)
    Rel->>K: Publicar envelope (con clave de partición)
    K-->>Rel: Ack
    Rel->>DB: UPDATE outbox SET published_at = now()
```

**DDL — tabla `outbox`** (en la DB del tenant):

```sql
CREATE TABLE outbox (
    id              BIGINT      GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    event_id        UUID        NOT NULL UNIQUE,          -- = envelope.event_id (idempotencia de publicación)
    tenant_id       UUID        NOT NULL,
    aggregate_type  TEXT        NOT NULL,                 -- p. ej. 'ProductionOrder'
    aggregate_id    TEXT        NOT NULL,                 -- p. ej. 'OP-2026-000482'
    type            TEXT        NOT NULL,                 -- 'nexo.production.registered'
    schema_version  INT         NOT NULL DEFAULT 1,
    topic           TEXT        NOT NULL,                 -- 'nexo.production.registered.v1'
    partition_key   TEXT        NOT NULL,                 -- 'tenant|order_id'
    envelope        JSONB       NOT NULL,                 -- envelope canónico completo
    headers         JSONB       NOT NULL DEFAULT '{}'::jsonb,
    correlation_id  UUID,
    occurred_at     TIMESTAMPTZ NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT now(),
    published_at    TIMESTAMPTZ,                          -- NULL = pendiente
    attempts        INT         NOT NULL DEFAULT 0,
    next_attempt_at TIMESTAMPTZ NOT NULL DEFAULT now(),
    last_error      TEXT
);

-- Índice parcial: el relay solo barre lo no publicado y vencido
CREATE INDEX ix_outbox_pending
    ON outbox (next_attempt_at)
    WHERE published_at IS NULL;

-- Purga de publicados (housekeeping por antigüedad; el historial vive en el Event Store)
CREATE INDEX ix_outbox_published_at ON outbox (published_at)
    WHERE published_at IS NOT NULL;
```

> **MassTransit built-in.** MassTransit ofrece un **EF Core Transactional Outbox** (`AddEntityFrameworkOutbox`) con tablas
> `OutboxMessage` / `OutboxState` / `InboxState`. El default del MVP es **usar esa implementación** (menos código a
> mantener); la DDL de arriba documenta el **modelo conceptual** y sirve si se necesita un outbox propio (p. ej. para
> incluir `partition_key`/`topic` explícitos). Decisión en §8 (DT-EV-02).

### 5.2 Inbox / Idempotencia (`processed_events`)

Cada consumidor registra qué eventos ya aplicó. Antes de procesar, consulta; si el evento ya está, lo **descarta
silenciosamente** (idempotencia). El registro y el cambio de estado del handler ocurren en la **misma transacción**.

**DDL — tabla `processed_events`** (en la DB del tenant, por servicio consumidor):

```sql
CREATE TABLE processed_events (
    consumer       TEXT        NOT NULL,           -- 'nexo.traceability' (group/servicio lógico)
    event_id       UUID        NOT NULL,           -- = envelope.event_id
    dedup_key      TEXT        NOT NULL,           -- = envelope.dedup_key
    tenant_id      UUID        NOT NULL,
    type           TEXT        NOT NULL,
    processed_at   TIMESTAMPTZ NOT NULL DEFAULT now(),
    result         TEXT        NOT NULL DEFAULT 'ok',   -- 'ok' | 'skipped' | 'compensated'
    PRIMARY KEY (consumer, event_id)
);

-- Idempotencia también por dedup_key (cubre reingestas con distinto event_id pero mismo hecho)
CREATE INDEX ix_processed_dedup ON processed_events (consumer, tenant_id, dedup_key);
```

> **`event_id` vs `dedup_key`.** `event_id` es la identidad **técnica** (una publicación). `dedup_key` es la identidad
> **del hecho** (determinística, estable entre reintentos edge→nube, [data-ingestion.md](../specs/specs/data-ingestion.md) §5.2).
> El inbox chequea **ambos**: `event_id` neutraliza reentregas del broker; `dedup_key` neutraliza reingestas del mismo
> hecho con distinto `event_id` (p. ej. un lote reenviado por el agente tras un corte).

### 5.3 Garantías, orden, reintentos y backoff

| Aspecto | Diseño |
|---|---|
| **Entrega** | At-least-once (outbox garantiza publicación; el broker puede re-entregar) |
| **Idempotencia** | Inbox `processed_events` por consumidor; handlers idempotentes por diseño |
| **Orden** | Por partición (clave `tenant_id[\|aggregate_id]`); reordenamiento por `occurred_at` en ventanas donde haga falta |
| **Reintentos** | **Backoff exponencial + jitter** en el consumidor: p. ej. 5 intentos, base 1s, tope 5 min (`MessageRetry`) |
| **Backoff de publicación** | El relay reintenta con `next_attempt_at` creciente; `attempts`/`last_error` para diagnóstico |
| **Errores permanentes** | A **DLQ** tras agotar reintentos; alerta a Observability |
| **Envenenamiento** | Un mensaje "poison" no bloquea la partición: se envía a DLQ y se avanza el offset |
| **Exactly-once (aparente)** | No se busca EOS de Kafka; se logra el **efecto** exactly-once vía at-least-once + idempotencia |

---

## 6. Catálogo canónico de eventos (MVP)

Las dos tablas de esta sección (**§6** eventos de plataforma y de dominio ya vigentes; **§6.1** eventos del modelo por capas)
son, en conjunto, la **única fuente de verdad** de los nombres de evento del MVP: todo otro documento (en especial
[04-service-contracts.md](./04-service-contracts.md)) **referencia** estos mismos nombres, no los redefine. **`type (wire)`**
= valor del envelope `type` (`nexo.<domain>.<event>`, minúscula, `snake_case`, verbo en pasado); **`topic`** = el topic
Kafka/MSK (`type` + `.v<major>`); **Evento (contrato C#)** = clase de integración PascalCase mapeada 1:1 al `type`. Todos
viajan en el envelope de §1.

| Evento (contrato C#) | type (wire) | topic | Productor | Consumidores | Clave de partición | Payload resumido |
|---|---|---|---|---|---|---|
| **TenantProvisioned** | `nexo.tenant.provisioned` | `nexo.tenant.provisioned.v1` | Tenant Provisioning (CP) | Observability, Admin, (bootstrap de servicios del tenant) | `tenant_id` | `tenant_id`, `plan`, `db_ref`, `region`, `provisioned_at`, `admin_user_ref` |
| **ReadingIngested** | `nexo.reading.ingested` | `nexo.reading.ingested.v1` | Ingestion | Devices, Rules Engine, Dashboards (agg), Traceability (muestreado) | `tenant_id\|device_id` | `device_id`, `tag`, `value`, `uom`, `quality`, `occurred_at` |
| **ProductionRegistered** | `nexo.production.registered` | `nexo.production.registered.v1` | Production | Traceability, Dashboards, Rules Engine, Connectors (agrega) | `tenant_id\|order_id` | `order_id`, `run_id`, `product_sku`, `good_qty`, `nonconforming_qty`, `uom` |
| **RunClosed** | `nexo.production.run_closed` | `nexo.production.run_closed.v1` | Production | **Connectors (dispara push Odoo)**, Dashboards, Traceability | `tenant_id\|order_id` | `run_id`, `order_id`, `totals{good,nonconforming,scrap}`, `operative_time`, `closed_by` |
| **ScrapRegistered** | `nexo.scrap.registered` | `nexo.scrap.registered.v1` | Scrap | Production (ajusta total), Traceability, Dashboards, Connectors | `tenant_id\|order_id` | `order_id`, `product_sku`, `qty`, `reason_code`, `cost?`, `lot?` |
| **QualityInspected** | `nexo.quality.inspection_completed` | `nexo.quality.inspection_completed.v1` | Quality | Traceability, Dashboards, Production (reclasifica) | `tenant_id\|order_id` | `inspection_id`, `order_id`, `result{pass\|fail}`, `measurements[]`, `defects[]` |
| **QualityDispositionSet** | `nexo.quality.disposition_set` | `nexo.quality.disposition_set.v1` | Quality | Production (buenas/no conformes), Scrap (si rechazo) | `tenant_id\|order_id` | `subject{lot\|serial}`, `disposition{rework\|use_as_is\|scrap}`, `by` |
| **DowntimeStarted** | `nexo.downtime.started` | `nexo.downtime.started.v1` | Downtime | Rules Engine, Notifications, Dashboards, Production (pausa) | `tenant_id\|asset_id` | `asset_id`, `started_at`, `reason_code?`, `planned` |
| **DowntimeEnded** | `nexo.downtime.ended` | `nexo.downtime.ended.v1` | Downtime | Dashboards, Traceability, Reports | `tenant_id\|asset_id` | `asset_id`, `ended_at`, `duration_s`, `reason_code` |
| **DeviceStatusChanged** | `nexo.device.status_changed` | `nexo.device.status_changed.v1` | Devices (desde presencia del edge) | Observability, Dashboards, Rules Engine, Notifications | `tenant_id\|device_id` | `device_id`, `status{online\|offline\|degraded}`, `last_seen`, `agent_id` |
| **OdooSyncRequested** | `nexo.integration.odoo_sync_requested` | `nexo.integration.odoo_sync_requested.v1` | Connectors (orquestador) | Connectors · Adapter Odoo | `tenant_id\|job_id` | `job_id`, `entity{mo\|scrap\|quality}`, `nexo_ref`, `dedup_key` |
| **OdooSyncCompleted** | `nexo.integration.odoo_sync_completed` | `nexo.integration.odoo_sync_completed.v1` | Connectors · Adapter Odoo | Production/Scrap/Quality (estado sync), Traceability, Observability | `tenant_id\|job_id` | `job_id`, `result{ok\|failed}`, `external_ref?`, `error?` |

**Notas de catálogo:**

- **`RunClosed` → push a Odoo agregado (INT-01).** El reporte de producción a Odoo se agrega **por cierre de corrida**,
  no por cada `ProductionRegistered`; por eso `RunClosed` es el disparador de `OdooSyncRequested`
  ([integrations.md](../specs/specs/integrations.md) §4, [production.md](../specs/specs/production.md) §3).
- **`ReadingIngested`** de alta frecuencia se persiste en **time-series** y solo genera evento de dominio según la config
  de la señal ([data-ingestion.md](../specs/specs/data-ingestion.md) §6); Traceability consume una **muestra/resumen**,
  no el 100% (decisión abierta [traceability.md](../specs/specs/traceability.md) PA-1).
- **Traceability** consume **prácticamente todos** los eventos para el historial inmutable y la genealogía
  ([traceability.md](../specs/specs/traceability.md) §5–6).
- **`machine_event`** (arranque/paro de máquina) existe como categoría; en el MVP se materializa vía `DowntimeStarted/Ended`
  y `DeviceStatusChanged` según semántica ([downtime.md](../specs/specs/downtime.md), [devices.md](../specs/specs/devices.md)).
- **Correcciones** (nunca edición destructiva): eventos compensatorios/de anexo con `causation_id` al `event_id` original,
  p. ej. `nexo.production.adjustment_recorded.v1` ([traceability.md](../specs/specs/traceability.md) §4.1, [production.md](../specs/specs/production.md) CB11).
- **Estos nombres NO cambian con el modelo por capas.** `nexo.production.registered` y el resto de la tabla se mantienen tal
  cual; lo único que ganan es **contexto**: cuando el hecho ocurre dentro de una ejecución, viajan con `execution_id` y
  `task_instance_id` en el envelope (§1.1). El reencuadre es de **posicionamiento conceptual, no una demolición**
  ([work-model.md](../specs/specs/work-model.md) §10.5).

### 6.1 Catálogo del modelo por capas — Proceso · Ejecución · Tarea · Master Data

Mismo envelope, misma convención `nexo.<domain>.<event>`, mismo formato de tabla. Cubre las capas incorporadas el
2026-07-13: **Capa 2** ([work-model.md](../specs/specs/work-model.md)), **Capa 3** ([execution.md](../specs/specs/execution.md))
y los catálogos propios ([master-data.md](../specs/specs/master-data.md)). Aplica a **ambos perfiles** (Lote y Proyecto).

| Evento (contrato C#) | type (wire) | topic | Productor | Consumidores | Clave de partición | Payload resumido |
|---|---|---|---|---|---|---|
| **ProcessVersionPublished** | `nexo.process.version_published` | `nexo.process.version_published.v1` | WorkModel | Execution (habilita instanciar), Dashboards, Traceability, Audit | `tenant_id\|process_id` | `process_id`, `version`, `profile{repetitivo\|proyecto}`, `task_count`, `critical_path_s`, `published_by`, `diff_summary` |
| **ProcessVersionSuspended** | `nexo.process.version_suspended` | `nexo.process.version_suspended.v1` | WorkModel | Execution (bloquea nuevas instanciaciones; las en curso siguen), Notifications, Audit | `tenant_id\|process_id` | `process_id`, `version`, `reason`, `suspended_by` |
| **ExecutionCreated** | `nexo.execution.created` | `nexo.execution.created.v1` | Execution | Dashboards, Traceability, Notifications, Connectors *(si está activo)* | `tenant_id\|execution_id` | `execution_id`, `code`, `flavor{lote\|proyecto}`, `process_ref{process_id,version}`, `trigger{type,ref}`, `target{product_sku,qty,uom}?`, `deliverable?`, `customer_ref?`, `due_at?` |
| **ExecutionScheduled** | `nexo.execution.scheduled` | `nexo.execution.scheduled.v1` | Execution | Dashboards (baseline y ruta crítica), Traceability | `tenant_id\|execution_id` | `execution_id`, `baseline_id`, `planned_start`, `planned_end`, `frozen_version`, `task_instances`, `critical_path[]` |
| **ExecutionReleased** | `nexo.execution.released` | `nexo.execution.released.v1` | Execution | Dashboards, Notifications (planta habilitada) | `tenant_id\|execution_id` | `execution_id`, `released_by`, `shortages[]` |
| **ExecutionStarted** | `nexo.execution.started` | `nexo.execution.started.v1` | Execution | Dashboards, Traceability, Downtime (abre ventana), Connectors *(opcional)* | `tenant_id\|execution_id` | `execution_id`, `started_at`, `first_task_ref`, `assets[]` |
| **ExecutionPaused** | `nexo.execution.paused` | `nexo.execution.paused.v1` | Execution | Downtime (parada/tiempo no productivo), Dashboards, Rules | `tenant_id\|execution_id` | `execution_id`, `paused_at`, `cause{parada\|fin_turno\|bloqueo\|decisión}`, `reason_code?`, `by` |
| **ExecutionResumed** | `nexo.execution.resumed` | `nexo.execution.resumed.v1` | Execution | Downtime (cierra el intervalo), Dashboards | `tenant_id\|execution_id` | `execution_id`, `resumed_at`, `paused_duration_s`, `by` |
| **ExecutionRescheduled** | `nexo.execution.rescheduled` | `nexo.execution.rescheduled.v1` | Execution | Dashboards (desvío de cronograma), Traceability, Audit | `tenant_id\|execution_id` | `execution_id`, `new_baseline_id`, `previous_baseline_id`, `kind{fechas\|alcance\|recursos\|split\|migración}`, `reason`, `by` |
| **ExecutionInputConsumed** | `nexo.execution.input_consumed` | `nexo.execution.input_consumed.v1` | Execution | Traceability (genealogía), Dashboards, Connectors *(opcional)* | `tenant_id\|execution_id` | `execution_id`, `task_instance_id`, `item_code`, `qty`, `uom`, `lot?`, `method{declarado\|backflush\|báscula\|escaneo}`, `deviation_pct?` |
| **ExecutionMilestoneReached** | `nexo.execution.milestone_reached` | `nexo.execution.milestone_reached.v1` | Execution | Dashboards (hitos), Notifications, Traceability | `tenant_id\|execution_id` | `execution_id`, `task_instance_id`, `milestone_ref`, `committed_at`, `reached_at`, `delay_s` |
| **ExecutionImputationPending** | `nexo.execution.imputation_pending` | `nexo.execution.imputation_pending.v1` | Execution | Dashboards (bandeja), Notifications, Traceability | `tenant_id\|asset_id` | `source_event_id`, `asset_id`, `occurred_at`, `candidates[]`, `reason{sin_ejecución_activa\|ambiguo}` |
| **ExecutionClosed** | `nexo.execution.closed` | `nexo.execution.closed.v1` | Execution | Dashboards, Traceability, **Connectors *(si está activo)***, Notifications | `tenant_id\|execution_id` | `execution_id`, `flavor`, `mode{normal\|parcial\|forzado\|vencimiento}`, `totals{good,nonconforming,scrap}?`, `progress_pct`, `worked_time_s`, `closed_by`, `reason?` |
| **ExecutionCancelled** | `nexo.execution.cancelled` | `nexo.execution.cancelled.v1` | Execution | Dashboards, Traceability, Connectors *(opcional)* | `tenant_id\|execution_id` | `execution_id`, `reason`, `incurred{worked_time_s,inputs[]}`, `cancelled_by` |
| **TaskEnabled** | `nexo.task.enabled` | `nexo.task.enabled.v1` | Execution (origen `system`) | **Dashboards (cola y espera)**, Notifications, Traceability | `tenant_id\|execution_id` | `execution_id`, `task_instance_id`, `task_ref`, `enabled_at`, `predecessors[]`, `lag_expired_at?`, `required_role`, `resource_ref?` |
| **TaskAssigned** | `nexo.task.assigned` | `nexo.task.assigned.v1` | Execution | Dashboards (productividad), Notifications | `tenant_id\|execution_id` | `execution_id`, `task_instance_id`, `mode{individual\|equipo\|rol\|automático\|externo}`, `assignees[]`, `role`, `by` |
| **TaskStarted** | `nexo.task.started` | `nexo.task.started.v1` | Execution | Dashboards (reloj, espera = start − enabled), Traceability, Downtime | `tenant_id\|execution_id` | `execution_id`, `task_instance_id`, `task_ref`, `started_at`, `operator_id`, `asset_id?`, `wait_s` |
| **TaskPaused** | `nexo.task.paused` | `nexo.task.paused.v1` | Execution | Dashboards, Downtime | `tenant_id\|execution_id` | `execution_id`, `task_instance_id`, `paused_at`, `cause` |
| **TaskResumed** | `nexo.task.resumed` | `nexo.task.resumed.v1` | Execution | Dashboards, Downtime | `tenant_id\|execution_id` | `execution_id`, `task_instance_id`, `resumed_at`, `paused_duration_s` |
| **TaskProgressReported** | `nexo.task.progress_reported` | `nexo.task.progress_reported.v1` | Execution | Dashboards (progreso), Traceability | `tenant_id\|execution_id` | `execution_id`, `task_instance_id`, `progress_pct`, `method{declarado\|cantidad\|checklist\|tiempo\|señal}`, `qty?`, `uom?` |
| **TaskBlocked** | `nexo.task.blocked` | `nexo.task.blocked.v1` | Execution | **Rules, Notifications**, Dashboards (cuellos de botella), Downtime | `tenant_id\|execution_id` | `execution_id`, `task_instance_id`, `blocked_at`, `cause{insumo\|recurso\|aprobación\|calidad}`, `reason_code`, `by` |
| **TaskUnblocked** | `nexo.task.unblocked` | `nexo.task.unblocked.v1` | Execution | Dashboards (duración del bloqueo), Notifications | `tenant_id\|execution_id` | `execution_id`, `task_instance_id`, `unblocked_at`, `blocked_duration_s`, `resolution` |
| **TaskCompleted** | `nexo.task.completed` | `nexo.task.completed.v1` | Execution | **Dashboards (progreso ponderado)**, Traceability, Quality, Connectors *(opcional)* | `tenant_id\|execution_id` | `execution_id`, `task_instance_id`, `task_ref`, `outcome{completed\|forced}`, `is_milestone`, `completion_criterion`, `worked_time_s`, `quality_gate?`, `evidence_debt?` |
| **TaskSkipped** | `nexo.task.skipped` | `nexo.task.skipped.v1` | Execution | Dashboards (recalcula denominador), Audit, Notifications | `tenant_id\|execution_id` | `execution_id`, `task_instance_id`, `kind{opcional\|condicional\|obligatoria}`, `reason`, `authorized_by` |
| **TaskEvidenceAttached** | `nexo.task.evidence_attached` | `nexo.task.evidence_attached.v1` | Execution | **Traceability**, Files/Media, Dashboards (deuda de evidencia) | `tenant_id\|execution_id` | `execution_id`, `task_instance_id`, `evidence[]` (ver §1.1), `satisfies_requirement`, `late` |
| **MasterDataRecordUpserted** | `nexo.masterdata.record_upserted` | `nexo.masterdata.record_upserted.v1` | MasterData | WorkModel, Execution, Ingestion (caché de catálogos), Dashboards, Traceability | `tenant_id\|catalog` | `catalog{uom\|item\|person\|role\|customer}`, `record_id`, `code`, `change{created\|updated}`, `governance{nexo\|erp}`, `external_ref?` |
| **MasterDataRecordArchived** | `nexo.masterdata.record_archived` | `nexo.masterdata.record_archived.v1` | MasterData | WorkModel (advertencia al publicar), Execution, Dashboards, Audit | `tenant_id\|catalog` | `catalog`, `record_id`, `code`, `reason`, `impact{events,executions}` |
| **MasterDataImportCompleted** | `nexo.masterdata.import_completed` | `nexo.masterdata.import_completed.v1` | MasterData | Notifications, Audit, Observability | `tenant_id\|import_job_id` | `import_job_id`, `catalog`, `file_ref`, `created`, `updated`, `rejected`, `by` |

**Notas del catálogo por capas:**

- **`nexo.task.enabled` lo emite el sistema** (`source=system`, con `origin_metadata.component` y `logic_version`). Es el
  evento que fija el instante *"el trabajo estaba listo para empezar"*: **sin él la espera no es medible** y el KPI de
  **cuello de botella** queda degradado ([event-engine.md](../specs/specs/event-engine.md) §7.3.1). Se emite igual en ambos
  perfiles y con **DAG completo** (precedencias Fin→Inicio + demora, decisión 2026-07-13).
- **La cantidad producida del perfil Lote NO se duplica.** Sigue viajando en `nexo.production.registered` (§6), ahora con
  `execution_id`/`task_instance_id`. No existe `nexo.execution.quantity_registered`: eso sería renombrar un contrato vigente.
- **Perfil Proyecto:** `milestone_reached` y `rescheduled` (con baseline) son sus eventos distintivos. **No se calcula OEE**
  para ejecuciones de sabor Proyecto (E23 de [execution.md](../specs/specs/execution.md)); los consumidores deben **ocultar**
  la métrica, no publicarla en cero.
- **ERP opcional (INT-01 a revisar).** Connectors aparece como consumidor **entre paréntesis** en toda la tabla: si el
  conector no está configurado, **ningún flujo se degrada**. Ninguna capa publica hacia el ERP de forma obligatoria.
- **Costo → V1.** `input_consumed` viaja **sin valorizar** (cantidad + unidad + lote). La master data del MVP no incluye
  tarifas ni centros de costo, así que la métrica de costo real se muestra **no disponible con motivo explícito**, nunca a
  medias ([master-data.md](../specs/specs/master-data.md) §7.3, [event-engine.md](../specs/specs/event-engine.md) §7.6.2).
- **Correcciones y reimputación:** nunca edición destructiva. Una reimputación (el supervisor mueve un hecho a otra tarea) se
  publica como evento de corrección con `causation_id` al original y `attribution.resolved_by`, y dispara **recálculo de
  ambas** ventanas ([event-engine.md](../specs/specs/event-engine.md) §8).
- **Volumen:** una ejecución de Proyecto genera decenas de eventos en meses; una de Lote, miles en horas. La clave
  `tenant_id|execution_id` reparte ambos casos sin puntos calientes por dominio.

### 6.2 Contratos .NET ilustrativos

```csharp
// Nexo.Contracts — envelope compartido (metadatos de transporte/trazabilidad)
public sealed record EventEnvelope<TPayload>(
    Guid            EventId,        // UUIDv7
    Guid            TenantId,
    string          Type,           // "nexo.production.registered"
    DateTimeOffset  OccurredAt,
    DateTimeOffset  IngestedAt,
    string          Source,         // device | manual | api | file
    TPayload        Payload,
    string          DedupKey,
    int             SchemaVersion,
    Guid            CorrelationId,
    string?         DeviceId       = null,
    EventContext?   Context        = null,   // site / line / asset
    string?         OperatorId     = null,
    string?         ExecutionId    = null,   // Capa 3 — ejecución (lote | proyecto)
    string?         TaskInstanceId = null,   // Capa 3 — tarea ejecutada
    Attribution?    Attribution    = null,   // cómo se resolvió la imputación
    IReadOnlyList<EvidenceRef>? Evidence = null,
    bool?           Productive     = null,
    string?         Shift          = null,
    OriginMetadata? OriginMetadata = null,
    Guid?           CausationId    = null);

public sealed record EventContext(string? Site, string? Line, string? Asset);

// Imputación: explicit | active_execution | time_window | unassigned
public sealed record Attribution(string Method, bool Pending = false,
                                 double? Confidence = null, string? ResolvedBy = null);

// La evidencia NO viaja en el evento: viaja su referencia inmutable + su huella.
public sealed record EvidenceRef(
    string          EvidenceId,
    string          Kind,            // photo | file | sensor_reading | signature | video_frame | structured_note
    string          Status,          // pending | materialized | verified
    string?         MediaRef        = null,
    string?         MediaType       = null,
    string?         ContentHash     = null,
    long?           SizeBytes       = null,
    DateTimeOffset? CapturedAt      = null,
    string?         RequirementRef  = null);

// Nexo.Contracts.Production — payload del evento de integración (SIN CAMBIOS de nombre)
public sealed record ProductionRegistered(
    string OrderId,
    string RunId,
    string ProductSku,
    string Uom,
    int    GoodQty,
    int    NonconformingQty,
    long?  CounterStart = null,
    long?  CounterEnd   = null,
    bool   CounterRollover = false);

// Nexo.Contracts.Execution — Capa 3, sirve a los DOS perfiles (lote | proyecto)
public sealed record ExecutionStarted(
    string          ExecutionId,
    string          Code,
    string          Flavor,          // lote | proyecto
    ProcessRef      ProcessRef,      // versión CONGELADA al programar
    DateTimeOffset  StartedAt,
    IReadOnlyList<string> Assets);

public sealed record ProcessRef(string ProcessId, string Version);

// Emitido por el SISTEMA al cumplirse las precedencias del DAG: sin él, la espera no se mide.
public sealed record TaskEnabled(
    string          ExecutionId,
    string          TaskInstanceId,
    string          TaskRef,
    DateTimeOffset  EnabledAt,
    IReadOnlyList<string> Predecessors,
    string          RequiredRole,
    string?         ResourceRef    = null,
    DateTimeOffset? LagExpiredAt   = null);

public sealed record TaskCompleted(
    string          ExecutionId,
    string          TaskInstanceId,
    string          TaskRef,
    string          Outcome,         // completed | forced
    bool            IsMilestone,
    string          CompletionCriterion,
    long            WorkedTimeSeconds,
    bool            EvidenceDebt = false);
```

> **gRPC/`.proto`:** los eventos usan **JSON** (no Protobuf). El `.proto` se reserva para el **sync interno**
> (Ingestion→Devices para resolver contexto, ADR-T5). No se define aquí porque no es parte del contrato de eventos.

---

## 7. MassTransit sobre Kafka (enfoque de configuración)

Configuración **ilustrativa** (no implementación) del *Kafka Rider* de MassTransit sobre MSK. Muestra: outbox EF Core,
producers, topic endpoints, clave de partición desde `tenant_id`, reintentos con backoff y auth IAM.

```csharp
services.AddMassTransit(x =>
{
    x.SetKebabCaseEndpointNameFormatter();

    // (1) Transactional Outbox sobre EF Core, en la DB del tenant
    x.AddEntityFrameworkOutbox<TenantDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();                       // publica al bus tras el commit de la tx
        o.QueryDelay = TimeSpan.FromSeconds(1); // barrido del relay
    });

    // (2) Consumidores (idempotentes vía inbox)
    x.AddConsumer<ProductionRegisteredConsumer>(c =>
        c.UseMessageRetry(r => r.Exponential(
            retryLimit:   5,
            minInterval:  TimeSpan.FromSeconds(1),
            maxInterval:  TimeSpan.FromMinutes(5),
            intervalDelta:TimeSpan.FromSeconds(2))));

    // Bus de control en memoria (el transporte de eventos es el Rider de Kafka)
    x.UsingInMemory((ctx, cfg) => cfg.ConfigureEndpoints(ctx));

    // (3) Kafka Rider (MSK)
    x.AddRider(rider =>
    {
        // Productores: contrato .NET -> topic versionado
        rider.AddProducer<ProductionRegistered>("nexo.production.registered.v1");
        rider.AddProducer<RunClosed>("nexo.production.run_closed.v1");

        rider.AddConsumer<ProductionRegisteredConsumer>();

        rider.UsingKafka((context, k) =>
        {
            // Auth IAM/SASL contra MSK, dentro de la VPC (00-tech-baseline §3)
            k.Host("b-1.msk.<cluster>.kafka.<region>.amazonaws.com:9098", h =>
            {
                h.UseSasl(s =>
                {
                    s.Mechanism = SaslMechanism.OAuthBearer;  // aws-msk-iam-sasl-signer
                    s.SecurityProtocol = SecurityProtocol.SaslSsl;
                });
            });

            // Endpoint de consumo: topic + consumer group por servicio
            k.TopicEndpoint<ProductionRegistered>(
                topic: "nexo.production.registered.v1",
                groupId: "nexo.production",
                e =>
                {
                    e.AutoOffsetReset      = AutoOffsetReset.Earliest;
                    e.CreateIfMissing(t => { t.NumPartitions = 12; t.ReplicationFactor = 3; });
                    e.ConcurrentMessageLimit = 8;
                    e.ConfigureConsumer<ProductionRegisteredConsumer>(context);
                });
        });
    });
});
```

**Clave de partición (`tenant_id[|aggregate_id]`)** al producir:

```csharp
// El productor fija la Kafka key = clave de partición del §4.2
await _producer.Produce(
    key: $"{tenantId}|{orderId}",           // co-ordena por orden dentro del tenant
    message: new ProductionRegistered(/* ... */),
    pipe: Pipe.Execute<KafkaSendContext>(ctx =>
    {
        ctx.Headers.Set("nexo-correlation-id", correlationId.ToString());
        ctx.Headers.Set("nexo-schema-version", "1");
    }),
    cancellationToken);
```

**Consumidor idempotente** (usa el inbox de §5.2):

```csharp
public sealed class ProductionRegisteredConsumer(
    IProcessedEventStore inbox,           // processed_events
    IProductionProjector projector)
    : IConsumer<ProductionRegistered>
{
    public async Task Consume(ConsumeContext<ProductionRegistered> ctx)
    {
        var eventId = ctx.MessageId ?? throw new InvalidOperationException("MessageId requerido");

        // Idempotencia: si ya se procesó, se descarta silenciosamente
        if (await inbox.AlreadyProcessed("nexo.production", eventId, ctx.CancellationToken))
            return;

        // Mismo scope transaccional: proyección + marca en inbox
        await projector.Apply(ctx.Message, ctx.CancellationToken);
        await inbox.MarkProcessed("nexo.production", eventId, ctx.CancellationToken);
    }
}
```

**Puntos de gobierno:**

- **Un `AddRider`/host por cluster MSK**; los servicios comparten convención de topics y groups.
- **`tenant_id` scoped**: el `ITenantContext` ([00](./00-tech-baseline.md) §5) se resuelve al consumir (del envelope) y
  fija el `DbContext` correcto antes de proyectar.
- **Validación de schema** como *filter* del pipeline (producer y consumer) contra el registry (§3).
- **OTel**: MassTransit propaga `correlation_id`/trace context a las trazas Kafka (ADR-T9).
- **`CreateIfMissing`** solo en dev/staging; en prod los topics se crean por **IaC** (Terraform) con particiones/retención
  gobernadas ([08-observability-ops.md](./08-observability-ops.md)).

---

## 8. Decisiones pendientes

| # | Pregunta | Contexto | Default provisional |
|---|---|---|---|
| **DT-EV-01** | Formato de serialización a escala | JSON encarece tamaño/CPU a millones de eventos/día (DT-02) | **JSON + JSON Schema** en MVP; reevaluar **Avro/Protobuf** en topics de `reading` de alto volumen en V1 |
| **DT-EV-02** | Outbox: MassTransit built-in vs. tabla propia | La DDL de §5.1 vs. `AddEntityFrameworkOutbox` | **MassTransit built-in** en MVP; outbox propio solo si se necesita `partition_key`/`topic` explícitos o multi-topic por evento |
| **DT-EV-03** | Schema registry concreto | AWS Glue vs. Confluent-compatible vs. propio | **AWS Glue Schema Registry** (nativo MSK/JSON Schema); confirmar límites y tooling .NET |
| **DT-EV-04** | Retención por clase de topic y por plan | Kafka (hot) vs. Event Store/time-series (largo plazo) | 30 d dominio / 7 d readings en MVP; retención comercial por plan a definir con [scalability.md](../specs/specs/scalability.md) PA-3 |
| **DT-EV-05** | Ventana y almacén de deduplicación | Balance memoria vs. cobertura de reenvíos tardíos | Inbox `processed_events` con purga por ventana; casos críticos contra Event Store ([data-ingestion.md](../specs/specs/data-ingestion.md) PA-4) |
| **DT-EV-06** | Nivel de encadenamiento criptográfico del evento | `sequence`/hash-chain por partición vs. sellado externo (RFC-3161) | Hash-chain por partición en Event Store; detalle en [07-security.md](./07-security.md) ([traceability.md](../specs/specs/traceability.md) PA-3) |
| **DT-EV-07** | Granularidad de `machine_event` en el envelope | ¿Categoría propia o subsumida en downtime/device? | Subsumida en `downtime.*` / `device.status_changed` en MVP; promover a categoría propia si V1 lo exige |
| **DT-EV-08** | Muestreo de `reading` hacia Traceability | 100% del crudo vs. muestra/resumen (costo vs. trazabilidad) | Muestra/resumen configurable por tag ([traceability.md](../specs/specs/traceability.md) PA-1) |
| **DT-EV-09** | ¿Cuándo se emite `nexo.task.enabled`? | Instanciación completa del DAG al programar vs. perezosa ([execution.md](../specs/specs/execution.md) PA-1). Afecta volumen de eventos y la medición de espera | **Instanciación completa al programar** + emisión de `enabled` cuando cada tarea cumple precedencias y vence el lag; revisar si un proyecto de ~200 tareas lo justifica |
| **DT-EV-10** | Evidencia offline: referencia `pending` vs. evento de anexo | El artefacto puede subir horas después (store-and-forward del edge) | Admitir el hecho con `evidence[].status=pending` y completarlo con `nexo.task.evidence_attached` (`causation_id` al original, **misma `dedup_key` del hecho**); nunca duplicar el evento de cierre |
| **DT-EV-11** | ¿`productive` viaja resuelto en el envelope o se deriva en la Capa 4? | La marca es configurable por tenant/señal y su cambio **altera retroactivamente** los tiempos muertos ([event-engine.md](../specs/specs/event-engine.md) PA-3) | Viaja **resuelto en la admisión** + `origin_metadata.productive_policy_version`, para poder reproyectar la serie histórica de forma determinista |
| **DT-EV-12** | Granularidad de los eventos de master data | Uno por catálogo (`item_upserted`, `uom_upserted`…) vs. genérico con discriminador | **Genérico** `nexo.masterdata.record_upserted` con `catalog` en el payload; abrir por catálogo solo si los consumidores divergen (MOD-17) |
| **DT-EV-13** | Ampliación del enum `source` con `system` y `vision` | Es un cambio **aditivo de enum** (§3.3, "⚠️ depende") sobre un contrato ya publicado | Se amplía dentro de **`v1`**: los consumidores deben tolerar valores desconocidos (regla de tolerancia documentada). No se abre `v2` |
| **DT-EV-14** | Imputación por inferencia en las métricas | ¿Se admiten métricas con eventos imputados por ventana temporal, o requieren confirmación humana? ([event-engine.md](../specs/specs/event-engine.md) PA-8) | El envelope **siempre** declara `attribution.method`; el filtrado por método es decisión del consumidor (Dashboards), no del contrato |

> Estas preguntas se resuelven **a medida que el diseño las necesita**. Al cerrarse, se promueven a **ADR** en
> [00-tech-baseline.md](./00-tech-baseline.md) §9.

> **Ya no son preguntas (decisiones del 2026-07-13).** El MVP soporta **ambos perfiles** (Lote y Proyecto) y **DAG completo**
> —por eso `nexo.task.enabled` y `nexo.execution.milestone_reached` entran al MVP y no a V1—; la **master data mínima es sin
> costo** —por eso `input_consumed` no lleva valorización—; y el **ERP es opcional** —por eso ningún evento del catálogo tiene
> a Connectors como consumidor obligatorio—.

---

## 9. Referencias cruzadas

- Stack, comunicación y fiabilidad de mensajería (base): [00-tech-baseline.md](./00-tech-baseline.md) §4
- Connection schema y resolución de tenant: [01-multi-tenancy-connection.md](./01-multi-tenancy-connection.md)
- Esquema lógico por tenant (incluye `outbox`/`processed_events`): [03-data-schema.md](./03-data-schema.md)
- Contratos por servicio (REST/gRPC/eventos): [04-service-contracts.md](./04-service-contracts.md)
- Agente Edge (dedup_key, store-and-forward): [05-edge-agent.md](./05-edge-agent.md)
- Conector Odoo (sync jobs, ACL, push por cierre de corrida): [06-odoo-connector.md](./06-odoo-connector.md)
- Pipeline de ingesta, validación, orden y reproceso: [../specs/specs/data-ingestion.md](../specs/specs/data-ingestion.md)
- Event Store inmutable, genealogía y cadena de trazabilidad: [../specs/specs/traceability.md](../specs/specs/traceability.md)
- Arquitectura, evento canónico y backbone: [../specs/specs/architecture.md](../specs/specs/architecture.md)
- Modelo de 4 capas y ERP como conector lateral opcional: [../specs/specs/layered-architecture.md](../specs/specs/layered-architecture.md)
- Contrato funcional del hecho (fecha/origen/valor/evidencia), imputación y métricas derivadas: [../specs/specs/event-engine.md](../specs/specs/event-engine.md)
- Proceso, Tarea, DAG y versionado (Capa 2): [../specs/specs/work-model.md](../specs/specs/work-model.md)
- Ejecución (Lote/Proyecto), tareas instanciadas y ciclo de vida (Capa 3): [../specs/specs/execution.md](../specs/specs/execution.md)
- Catálogos propios, modos standalone/conectado e importador CSV: [../specs/specs/master-data.md](../specs/specs/master-data.md)
- Integraciones y patrones de sync/idempotencia: [../specs/specs/integrations.md](../specs/specs/integrations.md)
- Escala, particionamiento, time-series y retención: [../specs/specs/scalability.md](../specs/specs/scalability.md)
