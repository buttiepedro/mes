# 007 · M2 — Capa 4 mínima: motor de eventos (progreso por ejecución)

> **Estado:** ✅ Completado y verificado · **Fecha:** 2026-07-26
> **Implementa:** [mvp-execution-roadmap.md](../mvp-execution-roadmap.md) · Milestone **M2**
> **Avanza:** el gap grande del MVP **"Capa 4 · Motor de eventos"** (versión mínima)

## Qué es M2

La **Capa 4**: un motor que **observa el stream de eventos y deriva métricas de verdad**. M2 entrega su versión mínima: un servicio que consume `nexo.execution.*` / `nexo.task.*` desde Kafka y mantiene un **read model de progreso por ejecución**, calculado sobre **hechos** (task-runs resueltas / total congelado en la corrida), nunca estimado. Es el "¿cómo venimos?" y el paso previo al tablero (M4).

## Qué se construyó

**Nuevo servicio `Nexo.EventEngine.Api`** (Capa 4, un solo proyecto minimal API):

| Archivo | Contenido |
|---|---|
| `ExecutionProgressProjection` | Read model **en memoria** (`ConcurrentDictionary` + lock por ejecución). `Apply(type, json)` interpreta cada evento; `Get(id)`/`All()` exponen el DTO. Progreso = completadas / total × 100 |
| `ExecutionEventsConsumer` | `BackgroundService` con consumer Confluent.Kafka, suscripción **por regex** `^nexo\.(execution\|task)\..*`, `AutoOffsetReset=Earliest` + `EnableAutoCommit=false` → **reconstruye la proyección desde el log** en cada arranque. `TopicMetadataRefreshIntervalMs=5000` para descubrir topics nuevos rápido |
| `ProgressEndpoints` | `GET /v1/executions/progress` (listado) y `GET /v1/executions/{id}/progress` (uno; 404 si no existe), con `RequireAuthorization()` |
| `Program.cs` + appsettings | Observability + Web + dev-auth (M0) + Swagger + health; puerto **5084** |

**Registrado en `scripts/run-local.ps1`** como 5ª API (puerto 5084; el loop de migraciones lo saltea porque no tiene EF).

## Lógica de proyección

| Evento | Efecto en el read model |
|---|---|
| `execution.created` | Inicializa: `code`, `flavor`, `totalTasks = taskRunCount`, status `created` |
| `execution.started` | status `started` |
| `execution.closed` / `cancelled` | status `closed` / `cancelled` |
| `task.completed` / `task.skipped` | agrega el `taskRunId` al set de **resueltas** (dedup) — ambos cuentan como hechas |
| `task.started` | agrega al set de iniciadas |

**Tolerante a desorden entre topics:** un evento de tarea de una ejecución aún desconocida crea un placeholder; el total se completa cuando llega `execution.created`. Los eventos de un mismo tenant llegan ordenados (el relay usa key = TenantId), pero entre topics distintos no hay orden garantizado — de ahí la tolerancia.

## Decisiones de implementación

| # | Decisión | Motivo |
|---|---|---|
| 1 | **Read model en memoria**, reconstruido desde el log en cada arranque | M2 mínimo; persistirlo en una tabla de read model se difiere (M4/M5). El replay desde `Earliest` lo hace determinista |
| 2 | **Consume JSON crudo**, sin referenciar los contratos CLR de Execution | El motor lee solo los campos que necesita (`executionId`, `taskRunId`, `taskRunCount`, `type`); queda desacoplado de Execution |
| 3 | **`task.skipped` cuenta como resuelta** | Una tarea saltada no se ejecutará; para el progreso está "cerrada" |
| 4 | **Progreso por conteo** (resueltas/total) | Mínimo verificable; la ponderación por `progressWeight` y los tiempos muertos/cuellos de botella quedan para después |

## Verificación ejecutada

Se publicaron eventos a los **mismos topics y con el mismo shape JSON** que el relay ya alimenta en vivo (probado en [006](./006-m1-outbox-relay.md)):

| Comprobación | Resultado |
|---|---|
| `dotnet build` (29 proyectos, incl. EventEngine) | ✅ **0 errores** |
| 5 APIs arriba (Prod/MasterData/WorkModel/Execution/**EventEngine 5084**) | ✅ `/health/ready` → 200 |
| Proyección inicial | ✅ `[]` |
| `execution.created` (taskRunCount=3) + `execution.started` + 2× `task.completed` | ✅ `GET /{id}/progress` → status **started**, total 3, completadas 2, **progressPct 66.7** |
| 3ª `task.completed` + `execution.closed` | ✅ status **closed**, completadas 3, **progressPct 100** |
| `GET /v1/executions/progress` (listado) | ✅ devuelve la ejecución proyectada |

> Los eventos de verificación son sintéticos (vía `rpk produce`), pero idénticos en topic y forma a los que emite el relay real; M1 ya probó que un evento real de dominio llega a esos topics. La cadena escritura → outbox → Kafka → proyección queda cubierta entre M1 y M2.

## Pendientes que deja

| Pendiente | Prioridad | Detalle |
|---|---|---|
| **Flujo real end-to-end** (crear proceso→ejecución→avanzar tareas por API y ver el progreso) | Alta | Falta orquestar el flujo completo; hoy la Capa 3 y la Capa 4 se verificaron por separado |
| **Persistir el read model** (tabla + offsets committeados) | Media | Hoy es en memoria, se reconstruye desde el log en cada arranque |
| **Métricas Capa 4 ricas**: progreso ponderado, tiempos muertos, cuellos de botella, desvío de cronograma | Media | M2 hace solo progreso por conteo |
| **Tablero (M4)** que consuma este read model | Alta | Próximo salto de valor visible |
