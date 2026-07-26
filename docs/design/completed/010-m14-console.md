# 010 · M14 (slice) — Consola web del operador/planificador

> **Estado:** 🟡 Parcial (slice usable) y verificado · **Fecha:** 2026-07-26
> **Implementa:** [mvp-execution-roadmap.md](../mvp-execution-roadmap.md) · Milestone **M14** (subconjunto)
> **Cierra el lazo humano:** ahora una persona maneja todo lo construido **sin curl/Swagger**

## Qué es este slice

M14 completo es el frontend real (formularios de captura en tablet, editor visual de procesos, KPIs por perfil). Este registro entrega un **slice usable**: una **consola web** que deja a un humano hacer el flujo completo que antes solo se hacía por API — **alta de master data → definir y lanzar una corrida → avanzar tareas → ver el progreso en vivo**.

## Qué se construyó

| Archivo | Contenido |
|---|---|
| `Nexo.EventEngine/.../wwwroot/console.html` | Consola self-contained (HTML+CSS+JS, sin dependencias). 3 secciones: **(1) Master data** (crear unidad con base/factor + ítem con unidad base por select; listar), **(2) Definir y lanzar** (código + constructor de tareas en secuencia FS + objetivo ítem/unidad/cantidad → `POST /v1/executions` con snapshot inline), **(3) Corrida activa** (botones Iniciar/Completar por tarea con gating DAG, cerrar, barra de progreso en vivo desde el motor) |
| `index.html` | Link **Consola →** desde el tablero |
| `BuildingBlocks.Web/DevCors.cs` | `AddNexoDevCors()`: política CORS permisiva **solo en Development** |
| `MasterData` y `Execution` `Program.cs` | `AddNexoDevCors()` + `UseCors(...)` en Development, para que la consola (servida en :5084) llame a :5081 y :5083 |

**URL:** `http://localhost:5084/console.html` (link desde el tablero en `/`).

## Decisiones de implementación

| # | Decisión | Motivo |
|---|---|---|
| 1 | **Consola servida por el EventEngine** (junto al tablero) | Cero infra nueva; el tablero y la consola conviven en :5084 |
| 2 | **CORS permisivo solo en Development** (building block compartido) | La consola llama a otras APIs por puerto distinto; en prod el front va same-origin/gateway |
| 3 | **Snapshot inline** al crear la ejecución (no vía WorkModel) | El puente gRPC WorkModel→Execution es **M13** (pendiente); mientras tanto la consola arma el DAG y lo congela en la corrida |
| 4 | **Unidad base del ítem por select** de unidades existentes | Evita el error "unidad base no encontrada" al tipear |

## Verificación ejecutada

| Comprobación | Resultado |
|---|---|
| `dotnet build nexo.sln` (con CORS) | ✅ **0 errores** |
| Consola servida | ✅ `GET /console.html` → **200** |
| CORS | ✅ `Access-Control-Allow-Origin: *` en MasterData (:5081) y Execution (:5083) con `Origin: :5084` |
| Flujo completo por el camino de la consola | ✅ crear ítem (unidad base existente) → **lanzar** `LOTE-CONSOLA` (batch, 2 tareas) → **iniciar/completar** T1 y T2 (gating DAG) → **cerrar** → progreso **100% closed** en el motor y en el tablero |

## Pendientes / desvíos

| Pendiente | Prioridad | Detalle |
|---|---|---|
| **M14 completo**: formularios de captura en tablet (UX operario, offline-first), editor visual de procesos, KPIs por perfil, ABM completo | Media | Este slice es una consola de escritorio para dev/demo, no la UX de operario final |
| **Backend: `POST /v1/uoms` devuelve 500 en vez de 409 ante base duplicada** (`ux_uom_base_per_magnitude`) | Media | La violación de constraint única se filtra como error 500; debería mapearse a un `Result.Failure`/409 en el handler (aplica a otros upserts) |
| **Integración WorkModel→Execution por gRPC** (M13) | Alta | La consola usa snapshot inline; falta lanzar desde una versión publicada real de WorkModel |
| **Auth real** (M8) | Media | La consola opera con el dev-bypass (M0) |
