# HEXA ⇄ MES — Brief de integración del módulo "Producción (eventos + visión por computadora)"

> **Para:** equipo/agente que trabaja el repo **HEXA** (`buttiepedro/hexa`).
> **De:** repo **MES** (motor de eventos + visión por computadora), stack .NET/Kafka, repo separado.
> **Fecha:** 2026-08-11 · **Estado:** propuesta para acordar contratos antes de codear.
> Este documento es **autocontenido**: describe qué tiene que construir HEXA para integrar el MES como un módulo nativo. El plan completo del lado MES vive en el otro repo (`docs/design/hexa-integration/README.md`).

---

## 1. Qué es el MES y por qué es un repo aparte

El **MES** es el subsistema de **generación de eventos + visión por computadora** de HEXA: captura lo que pasa en la planta (operarios, PLC/OPC-UA/Modbus/MQTT, **cámaras**), lo normaliza a un **evento canónico**, corre **inferencia de visión** (conteo, defectos, presencia, OCR) y deriva **métricas en vivo** (progreso, OEE, scrap, tiempos muertos, cuellos de botella).

Es un repo aparte porque su perfil de carga —tiempo real, alta frecuencia, edge, GPU— **no encaja** en el backend Python CRUD de HEXA. Pero se integra para que, **para el usuario final, sea un módulo más de HEXA** (menú, tablero embebido, agente IA que lo consulta).

**La buena noticia:** HEXA y el MES ya comparten lo caro — **DB-per-tenant en Neon por `slug`** y **multitenancy por empresa**— así que la integración se apoya en lo que HEXA ya tiene.

---

## 2. Principio de frontera (quién es dueño de qué)

> **Regla de oro:** *"¿qué debería pasar?"* → **HEXA**. *"¿qué está pasando / qué ven las cámaras?"* → **MES**.

| Dominio | Dueño |
|---|---|
| Empresas, usuarios, roles, auth, tenant DB | **HEXA** (el MES los consume) |
| Artículos, clientes, depósitos, stock, unidades de medida | **HEXA** (el MES los referencia por id) |
| **Órdenes de producción** (qué producir, cuánto, cuándo) | **HEXA** |
| **Rutas / procesos / DAG** (cómo se hace) — *autoría* | **HEXA** |
| **Planes de calidad** (qué inspeccionar) — *definición y registro formal* | **HEXA** |
| Gemelo digital: líneas, estaciones, **cámaras**, sensores | **MES** |
| Captura/ingesta (operario, PLC, archivos) + **visión por computadora** | **MES** |
| Runtime de corrida (estados de tarea, gating del DAG, atribución evento→orden/tarea) | **MES** |
| Motor de eventos + métricas en vivo (progreso, OEE, scrap…) | **MES** |

**Implicancia clave para HEXA:** el MES **no** tiene master data propia. HEXA es la fuente de verdad de artículos/clientes/depósitos/unidades; el MES los referencia por `id`.

---

## 3. Lo que hay que construir en HEXA

Todo sigue el patrón canónico de HEXA (`BaseModule` + `AppSpec` en el registry + `service/router/tool/schemas` por app + agente por módulo). Los módulos `produccion` y `calidad` hoy son stubs vacíos → greenfield.

### 3.1 Módulo `produccion`

**Apps (cada una con `service.py` / `router.py` / `tool.py` / `schemas.py` + `AppSpec`):**

| App | Qué hace |
|---|---|
| **Órdenes de producción** | CRUD de órdenes (nacen de una venta/plan). Estado (`abierta/en_curso/cerrada`) se **sincroniza desde el MES** vía webhooks. Al lanzarse, empuja el contexto al MES (§4.2). |
| **Rutas / Procesos** | Autoría del proceso: tareas + precedencias (DAG: FS/SS/FF + lag). Se envía al MES como parte del contexto de corrida. |
| **Tablero en vivo** | Página que **embebe** el tablero del MES (iframe con el JWT del usuario). |

**`module.py` → `on_activate(company_id, tenant_db_url)`:**
1. Crear tablas tenant: `orden_produccion`, `ruta`, `ruta_tarea`, `ruta_precedencia`.
2. Llamar al **provisioning del MES** (§4.1) para crear/vincular el tenant del MES.
3. Guardar el handle/secreto del MES en `module_subscriptions.config` + un `integration_config` (URL base del MES + secreto de servicio).

**Agente del módulo (`tool.py`):** tools que llaman al **API de lectura del MES** (§4.4):
- `estado_orden(orden_id)` → progreso, estado, tareas.
- `oee_turno(fecha, linea?)` → OEE y factores.
- `cuellos_de_botella(orden_id?)` → tareas/estaciones que traban.
- `defectos_recientes(fecha?)` → detecciones de visión.

Así el `CentralAgent` responde "¿cómo viene la orden 123?" o "¿cuántos defectos hoy?" sin lógica nueva: delega al agente de `produccion`, que llama al MES.

### 3.2 Módulo `calidad`

| App | Qué hace |
|---|---|
| **Planes de calidad** | Qué inspeccionar por artículo/ruta (checklist, variables). |
| **No-conformidades** | Registro formal, **alimentado por las detecciones de visión del MES** (webhook `quality.defect_detected`) + carga manual. |
| **Tablero de calidad** | FPY, Pareto de defectos (embebe/consume del MES). |

### 3.3 Núcleo / plataforma HEXA

- **Ingesta de eventos del MES (webhook + Celery):** un endpoint que recibe los eventos del MES (§4.3), valida la firma/secreto, y encola un **task de Celery** que actualiza órdenes/calidad/KPIs. Reusar el patrón de `integration_sync_log`.
- **Emisión de contexto:** endpoints para que el MES **lea** órdenes/rutas/artículos/estaciones (§4.2), **o** un push al MES al lanzar la orden (recomendado: push).
- **Identidad para el MES (§4.5):** exponer el secreto para que el MES valide los JWT de usuario de HEXA + emitir una credencial de servicio para los callbacks.
- **Unidades de medida:** agregar catálogo de UoM si no existe (el MES lo necesita para referenciar cantidades).
- **Estaciones/líneas:** guardar un **id/nombre liviano** de estación en HEXA (para asociar una orden a una línea); el detalle del gemelo digital vive en el MES.

### 3.4 Frontend HEXA

- Páginas `/modules/produccion/{ordenes,rutas,tablero}` y `/modules/calidad/{...}` en `frontend/src/modules/`.
- El **tablero/andon** se embebe con un `<iframe src="{MES_URL}/embed/tablero?...">` pasando el JWT del usuario (el MES lo valida). Estilo Tailwind alineado para que se vea nativo.

---

## 4. Contratos del *seam* (a acordar)

> URLs de ejemplo; los nombres finales se cierran entre ambos equipos. `{MES}` = base URL del MES por empresa; `{HEXA}` = base URL de HEXA.

### 4.1 Provisioning (al activar el módulo)
```
POST {MES}/v1/tenants
Authorization: Bearer <service-token-de-HEXA>
{ "companyId": "<uuid>", "slug": "acme" }
→ 201 { "tenantId": "...", "status": "ready" }   // el MES crea/vincula hexa_acme_mes
```

### 4.2 Contexto HEXA → MES (lanzar una corrida desde una orden)
```
POST {MES}/v1/runs
Authorization: Bearer <service-token-de-HEXA>
{
  "orderId": "<uuid>", "orderCode": "OP-1024",
  "target": { "itemId": "<uuid articulo HEXA>", "quantity": 100, "uomId": "<uuid>" },
  "stationId": "<uuid o null>",
  "route": {
    "tasks": [
      { "taskId": "<uuid>", "code": "T1", "name": "Corte", "obligation": "mandatory",
        "requiredEvidenceKind": null, "responsibleRoleId": "<uuid o null>" }
    ],
    "precedences": [
      { "predecessorTaskId": "<uuid>", "successorTaskId": "<uuid>", "type": "FS", "lagSeconds": 0 }
    ]
  }
}
→ 201 { "runId": "<uuid>" }
```
> Alternativa (pull): el MES llama `GET {HEXA}/api/v1/modules/produccion/orders/{id}` para traerse el contexto. **Recomendado el push** (HEXA controla cuándo arranca la corrida).

### 4.3 Realidad MES → HEXA (webhooks de eventos de negocio)
```
POST {HEXA}/api/v1/modules/produccion/events
X-MES-Signature: hmac-sha256(<company-secret>, body)
{
  "type": "run.progressed" | "task.completed" | "run.closed"
        | "quality.defect_detected" | "metric.oee",
  "companyId": "<uuid>", "orderId": "<uuid>", "runId": "<uuid>",
  "occurredAt": "2026-08-11T12:00:00Z",
  "payload": { ... }   // p.ej. { "progressPct": 60, "completedTasks": 3, "totalTasks": 5 }
                       // defecto: { "defectType": "rayon", "imageRef": "...", "stationId": "..." }
}
→ 200 (HEXA encola un Celery task que actualiza la orden / no-conformidad / KPI)
```

### 4.4 API de lectura del MES (para embeber y para los tools IA)
```
GET  {MES}/v1/runs/{runId}/progress        → { status, progressPct, completedTasks, totalTasks, ... }
GET  {MES}/v1/metrics/oee?date=&line=      → { oee, availability, performance, quality }
GET  {MES}/v1/quality/defects?date=        → [ { defectType, station, at, imageRef }, ... ]
GET  {MES}/embed/tablero?token=<jwt-user>  → HTML embebible (tablero en vivo)
```

### 4.5 Identidad
- **Usuario (UI embebida + lectura):** las requests al MES llevan el **JWT de usuario de HEXA** (HS256). El MES lo valida con el **mismo `JWT_SECRET`** (compartir como secreto de plataforma) o con un **par de claves dedicado** que HEXA emita para el MES. Extrae `company_id` → resuelve su tenant (`hexa_{slug}_mes`).
- **Servicio (context push HEXA→MES y provisioning):** HEXA usa un **service token** que el MES valida.
- **Servicio (webhooks MES→HEXA):** el MES firma cada webhook con un **secreto por empresa** (HMAC); HEXA lo verifica. El secreto se acuerda en el provisioning (§4.1).

---

## 5. Decisiones que HEXA necesita confirmar

1. **Identidad del MES:** ¿compartir el `JWT_SECRET` de plataforma para que el MES valide, o emitir un par de claves dedicado? (recomendado: par dedicado para no exponer el secreto de HEXA).
2. **Contexto push vs pull:** ¿HEXA hace push al MES al lanzar la orden (recomendado), o el MES pull-ea de HEXA?
3. **UoM y estaciones:** ¿HEXA agrega catálogo de unidades + un modelo liviano de estación/línea? (el MES lo necesita para referenciar cantidades y asociar corridas a líneas).
4. **Provisioning del tenant del MES:** ¿el MES crea su propia DB companion en Neon (`hexa_{slug}_mes`), o HEXA le pasa una connection string? (recomendado: el MES gestiona su propio store).
5. **Embed de UI:** iframe (rápido, recomendado para empezar) vs micro-frontend.

---

## 6. Checklist de implementación sugerida (lado HEXA)

- [ ] **P0** Módulo `produccion` con app **Órdenes** (CRUD) + `AppSpec` en el registry + navegación.
- [ ] **P0** `on_activate` que crea tablas tenant y llama al provisioning del MES; `integration_config` con URL/secreto.
- [ ] **P0** Endpoint webhook `/modules/produccion/events` + verificación de firma + Celery task que actualiza la orden.
- [ ] **P0** Identidad para el MES (secreto/claves) + service token para el push.
- [ ] **P1** App **Rutas/Procesos** (autoría del DAG) + push del contexto al MES al lanzar la orden.
- [ ] **P1** Página **Tablero** que embebe el tablero del MES (iframe con JWT).
- [ ] **P1** Catálogo UoM + modelo liviano de estación.
- [ ] **P2** Módulo `calidad` consumiendo `quality.defect_detected`.
- [ ] **P2** Tools del agente de `produccion` que llaman al API de lectura del MES.

---

## 7. Qué NO tiene que hacer HEXA

- ❌ **No** modelar captura de sensores/cámaras ni inferencia de visión — es del MES.
- ❌ **No** calcular OEE/progreso en tiempo real en Python — lo hace el MES y lo empuja.
- ❌ **No** duplicar artículos/clientes en el MES — el MES referencia los de HEXA por id.
- ❌ **No** agregar Kafka — el contrato con el MES es **HTTP (webhooks + API)**; Kafka queda interno al MES.
