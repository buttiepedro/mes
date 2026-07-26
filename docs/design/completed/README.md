# Registro de trabajo completado

> **Documento:** `docs/design/completed/README.md` · **Actualizado:** 2026-07-26 · **Plan de lo que falta:** [roadmap de ejecución](../mvp-execution-roadmap.md)

Bitácora de lo que **efectivamente se construyó y se verificó**, en orden cronológico. Complementa a
[`docs/design/`](../README.md) (que describe *qué se va a construir*) respondiendo *qué se construyó, cómo se
verificó y qué decisiones se tomaron al bajar el diseño a código*.

## Para qué sirve

- **Trazabilidad diseño → código:** cada registro enlaza el documento de diseño que implementa.
- **Decisiones de implementación:** el diseño nunca cubre todo. Las decisiones que se toman al codear se
  registran acá, no se pierden en un commit.
- **Evidencia de verificación:** cada registro dice **cómo se comprobó** que funciona (build, tests, migración
  aplicada, endpoint respondiendo). Sin evidencia, el trabajo no se considera completado.
- **Desvíos respecto del diseño:** cuando la implementación se aparta del documento, queda escrito **por qué**.

## Convenciones

- Un archivo por unidad de trabajo: `NNN-<slug>.md` (numeración correlativa, estable).
- Estado de cada registro: **✅ Completado y verificado** · **🟡 Parcial** · **⛔ Bloqueado**.
- Todo registro incluye: qué se construyó · decisiones de implementación · **verificación ejecutada** ·
  desvíos respecto del diseño · qué queda pendiente.
- La verificación se reporta **con el resultado real**. Si algo no se pudo probar, se dice explícitamente.

## Índice

| # | Trabajo | Estado | Fecha | Diseño que implementa |
|---|---|---|---|---|
| [001](./001-scaffold-inicial.md) | Scaffold del monorepo + slice de Producción + infra local | ✅ Completado y verificado | 2026-07-13 | [00](../00-tech-baseline.md) · [03](../03-data-schema.md) · [04](../04-service-contracts.md) |
| [002](./002-masterdata.md) | Servicio `Nexo.MasterData` (catálogos mínimos, MOD-17) | ✅ Completado y verificado | 2026-07-13 | [03](../03-data-schema.md) §2.5 · [04](../04-service-contracts.md) §2.5 |
| [003](./003-workmodel.md) | Servicio `Nexo.WorkModel` (Capa 2 · Procesos + DAG, MOD-18) | ✅ Completado y verificado | 2026-07-24 | [03](../03-data-schema.md) §2.6 · [04](../04-service-contracts.md) §2.6 |
| [004](./004-execution.md) | Servicio `Nexo.Execution` (Capa 3 · Lote y Proyecto, PRD-16) | ✅ Completado y verificado | 2026-07-25 | [03](../03-data-schema.md) §2.7-2.8 · [04](../04-service-contracts.md) §2.7 |
| [005](./005-m0-dev-auth.md) | **M0** · Modo dev sin auth (bypass en Development) | ✅ Completado y verificado | 2026-07-26 | [mvp-execution-roadmap.md](../mvp-execution-roadmap.md) M0 |
| [006](./006-m1-outbox-relay.md) | **M1** · Relay outbox → Kafka (`Nexo.BuildingBlocks.Outbox`) | ✅ Completado y verificado | 2026-07-26 | [mvp-execution-roadmap.md](../mvp-execution-roadmap.md) M1 |
| [007](./007-m2-event-engine.md) | **M2** · Capa 4 mínima: motor de eventos (`Nexo.EventEngine`, progreso por ejecución) | ✅ Completado y verificado | 2026-07-26 | [mvp-execution-roadmap.md](../mvp-execution-roadmap.md) M2 |
| [008](./008-m4-dashboard.md) | **M4** · Tablero en vivo (progreso de ejecuciones, `http://localhost:5084/`) | ✅ Completado y verificado | 2026-07-26 | [mvp-execution-roadmap.md](../mvp-execution-roadmap.md) M4 |
| [009](./009-m3-real-flow.md) | **M3** · Flujo real end-to-end (captura por API → tablero) + fix estado monotónico | ✅ Completado y verificado | 2026-07-26 | [mvp-execution-roadmap.md](../mvp-execution-roadmap.md) M3 |
| [010](./010-m14-console.md) | **M14 (slice)** · Consola web (master data + lanzar corrida + avanzar tareas) + CORS dev | 🟡 Slice usable y verificado | 2026-07-26 | [mvp-execution-roadmap.md](../mvp-execution-roadmap.md) M14 |

## Estado general del código

| Servicio | Capa | Estado |
|---|---|---|
| `BuildingBlocks` (Domain, Application, MultiTenancy, Messaging, Observability, Web) | transversal | ✅ Scaffold verificado |
| `Nexo.Production` | 3 (perfil repetitivo) | ✅ Scaffold verificado · outbox alineado ([003](./003-workmodel.md)) · modelo previo al pivot |
| `Nexo.MasterData` | master data | ✅ Implementado y verificado ([002](./002-masterdata.md)) |
| `Nexo.WorkModel` | 2 | ✅ Implementado y verificado ([003](./003-workmodel.md)) |
| `Nexo.Execution` | 3 | ✅ Implementado y verificado ([004](./004-execution.md)) |
| `Nexo.EventEngine` | 4 | ✅ Mínimo implementado y verificado ([007](./007-m2-event-engine.md)) · progreso por ejecución en memoria |
| **Plataforma local** (dev-auth [005](./005-m0-dev-auth.md) · relay outbox→Kafka [006](./006-m1-outbox-relay.md)) | transversal | ✅ Verificado |
| `Nexo.Tenancy` · `Nexo.Identity` · `Nexo.Ingestion` · resto | varias | ⬜ Pendiente |

> **Núcleo del modelo por capas (2→3→4) en código.** Con MasterData + WorkModel + Execution, la cadena
> Proceso → Ejecución (lote y proyecto) está implementada y verificada; con el **relay outbox→Kafka** ([006](./006-m1-outbox-relay.md))
> y el **motor de eventos mínimo (Capa 4)** ([007](./007-m2-event-engine.md)) los eventos ya fluyen y se
> proyecta **progreso por ejecución**. Lo que falta para un flujo **vivo end-to-end por API**: la **integración
> gRPC** WorkModel→Execution, la **ingesta/captura manual** (M3) y el **tablero** (M4). Ver el
> [roadmap de ejecución](../mvp-execution-roadmap.md).

## Pendientes transversales acumulados

Los que **cruzan varios servicios** y conviene no perder de vista:

| Pendiente | Prioridad | Origen | Estado |
|---|---|---|---|
| ~~**Relay del outbox → Kafka**: los eventos se persisten pero nadie los publica~~ | Alta | [001](./001-scaffold-inicial.md) | ✅ Saldado en [006](./006-m1-outbox-relay.md) (M1) |
| ~~Alinear el outbox de `Nexo.Production`~~ + actualizar `03-data-schema.md` | Alta | [002](./002-masterdata.md) | ✅ Saldado en [003](./003-workmodel.md) |
| **Registrar validadores en `Nexo.Production`** (los suyos no se ejecutan) | Media | [002](./002-masterdata.md) | Abierto |
| **Sin servicio de Identity**: no se puede ejercitar escritura autenticada end-to-end | Media | [001](./001-scaffold-inicial.md) | 🟡 Mitigado en local por [005](./005-m0-dev-auth.md) (bypass dev); Identity real = M5 |
| **Vulnerabilidad NU1902** en OpenTelemetry OTLP 1.9.0 | Media | [001](./001-scaffold-inicial.md) | Abierto |
