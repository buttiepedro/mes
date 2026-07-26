# 009 · M3 — Flujo real end-to-end (captura por API → tablero)

> **Estado:** ✅ Completado y verificado · **Fecha:** 2026-07-26
> **Implementa:** [mvp-execution-roadmap.md](../mvp-execution-roadmap.md) · Milestone **M3**
> **Cierra la tajada vertical:** Capa 3 (Execution) → outbox → relay ([006](./006-m1-outbox-relay.md)) → Kafka → motor ([007](./007-m2-event-engine.md)) → tablero ([008](./008-m4-dashboard.md)), **con datos reales**

## Qué es M3

Hasta acá el tablero se había probado con eventos **sintéticos**. M3 demuestra el **flujo real de captura**: un operario (vía la API de Execution) crea una corrida y **reporta hechos** (inicia y completa tareas), y esos hechos —sin ningún evento inyectado a mano— fluyen por toda la cadena hasta verse en el tablero. Es la prueba de que las Capas 3 y 4 funcionan **juntas**.

No hubo código de dominio nuevo: los endpoints del ciclo de vida de tarea (`:start`, `:complete`, `:close`) ya existían ([004](./004-execution.md)). M3 es la **integración verificada** end-to-end + un fix de robustez en la proyección.

## El flujo ejecutado (todo por API, sin tocar Kafka)

`LOTE-REAL-1` — lote de 2 tareas con DAG **T1 → T2 (FS)**:

| Paso | Llamada | Resultado |
|---|---|---|
| Crear corrida | `POST /v1/executions` (snapshot 2 tareas + 1 precedencia + target) | **201**, flavor batch, status released, taskRunCount 2 |
| — | el motor la proyecta sola | total 2, **0%**, status created |
| Iniciar T1 | `POST /v1/tasks/{T1}:start` | 200 |
| Completar T1 | `POST /v1/tasks/{T1}:complete` | 200 → **T2 pasa a `ready`** (gating FS del DAG) |
| — | el motor | 1/2, **50%**, status started |
| Iniciar+completar T2 | `POST /v1/tasks/{T2}:start` + `:complete` | 200 |
| Cerrar corrida | `POST /v1/executions/{id}:close` `{"mode":"normal"}` | 200 |
| — | el motor | 2/2, **100%**, status **closed** |

**Cada paso emitió su evento de dominio** (`nexo.task.started/completed`, `nexo.execution.started/closed`), verificado en `execution.outbox_messages` (todos `processed`) y consumido por el EventEngine, que actualizó el progreso en vivo.

## Fix incluido: estado monotónico en la proyección

Al reproducir el log, se detectó que el estado de una ejecución podía **retroceder**: como cada tipo de evento va a un topic distinto y **no hay orden garantizado entre topics**, un `execution.started` leído tarde pisaba un `execution.closed` ya aplicado (una ejecución cerrada aparecía "en curso").

**Corrección:** el estado ahora **solo avanza** (`created < started < closed = cancelled`), vía un `StatusRank` en `ExecutionProgressProjection`. La proyección quedó **independiente del orden de replay**. Verificado: tras el fix, tanto `OBRA-DEMO` como `LOTE-REAL-1` quedan `closed 100%`.

## Verificación ejecutada

| Comprobación | Resultado |
|---|---|
| `dotnet build` EventEngine (con el guard) | ✅ **0 errores** |
| Crear ejecución real por API | ✅ 201 · `LOTE-REAL-1` batch, 2 tareas |
| DAG gating real (T2 no arranca hasta cerrar T1) | ✅ T2 `pending` → `ready` recién tras completar T1 |
| Progreso proyectado en vivo | ✅ 0% → **50%** → **100% closed**, todo desde eventos reales |
| Estado monotónico (no regresa) | ✅ `OBRA-DEMO` y `LOTE-REAL-1` quedan `closed` tras replay |
| Tablero (`http://localhost:5084/`) | ✅ muestra `LOTE-REAL-1` cerrada al 100% junto al resto |

## Pendientes que deja

| Pendiente | Prioridad | Detalle |
|---|---|---|
| **UI de captura** (formularios de operario en tablet) | Media | Hoy la captura se hace por API/Swagger; falta la UX de operario (M5 / dominio) |
| **Persistir la proyección** (tabla + offsets) | Media | Sigue en memoria, reconstruida desde el log |
| **Métricas ricas de Capa 4** (tiempos muertos, cuellos de botella, KPIs por perfil) | Media | Hoy solo progreso por conteo |
| **Identity real** para reemplazar el dev-bypass | Media | M5 |
