# 008 · M4 — Tablero en vivo (progreso de ejecuciones)

> **Estado:** ✅ Completado y verificado · **Fecha:** 2026-07-26
> **Implementa:** [mvp-execution-roadmap.md](../mvp-execution-roadmap.md) · Milestone **M4**
> **Depende de:** [007](./007-m2-event-engine.md) (Capa 4 · read model de progreso)

## Qué es M4

El **payoff visible** de la tajada vertical: una página web que muestra el **progreso de las ejecuciones en tiempo real**, alimentada por el read model de la Capa 4 (M2). Convierte el JSON del motor en algo que se *ve* y se actualiza solo.

## Qué se construyó

| Archivo | Contenido |
|---|---|
| `src/Services/Nexo.EventEngine/Nexo.EventEngine.Api/wwwroot/index.html` | Tablero **self-contained** (HTML + CSS + JS inline, sin dependencias externas). Una tarjeta por ejecución: código, badge de perfil (batch/project), estado (creada/en curso/cerrada/cancelada con color), **barra de progreso** y `X / Y tareas`. Hace `fetch('/v1/executions/progress')` cada **2 s** y muestra "actualizado hace Xs" / "sin conexión" |
| `Program.cs` | `app.UseDefaultFiles()` + `app.UseStaticFiles()` **antes** de la autenticación |

**URL:** `http://localhost:5084/` (servido por el propio EventEngine).

## Decisiones de implementación

| # | Decisión | Motivo |
|---|---|---|
| 1 | **Servido por el propio EventEngine** (mismo origen que la API) | Evita CORS: el `fetch` a `/v1/executions/progress` es same-origin |
| 2 | **Estáticos antes de `UseAuthentication`** | La página es pública; su `fetch` sí pasa por auth (dev-bypass M0 lo autentica solo, sin token) |
| 3 | **Vanilla JS, sin framework ni CDN** | Cero dependencias; carga instantánea; fácil de mantener |
| 4 | **Polling cada 2 s** | Suficiente para el MVP; el push por WebSocket/SSE (UX-05, objetivo ≤5 s) queda para después |

## Verificación ejecutada

| Comprobación | Resultado |
|---|---|
| `dotnet build` EventEngine (con wwwroot) | ✅ **0 errores** |
| `GET http://localhost:5084/` | ✅ **200**, sirve el tablero (`<title>Nexo · Tablero en vivo</title>`) |
| Dataset de demo sembrado (eventos a Kafka) | ✅ 3 ejecuciones proyectadas |
| `GET /v1/executions/progress` que consume el tablero | ✅ **LOTE-101** batch 3/5 = **60%** (en curso) · **OBRA-202** project 1/4 = **25%** (en curso) · **OBRA-DEMO** project 3/3 = **100%** (cerrada) |

> El tablero se ve mejor en el navegador (`http://localhost:5084/`): tres tarjetas con barras a 60/25/100 %, que se actualizan solas a medida que llegan eventos.

## Nota sobre los datos

Las ejecuciones del tablero provienen de eventos **sintéticos** publicados a los topics (mismo shape que emite el relay real, [006](./006-m1-outbox-relay.md)). El **flujo real por API** —crear una ejecución en `Nexo.Execution` y avanzar sus tareas para que el tablero muestre datos reales— es el siguiente paso (parte de M3 / M5). El tablero ya está listo para mostrarlo sin cambios.

## Pendientes que deja

| Pendiente | Prioridad | Detalle |
|---|---|---|
| **Datos reales por API** (flujo Execution → eventos → tablero) | Alta | Hoy el tablero se probó con eventos sintéticos |
| **Push en vez de polling** (WebSocket/SSE, UX-05) | Baja | Hoy hace polling cada 2 s |
| **KPIs por perfil en el tablero** (OEE/scrap para lote; desvío/hitos para proyecto) | Media | Hoy muestra progreso y estado; falta el detalle por perfil |
| **Autenticación real** para el tablero | Media | Hoy usa el dev-bypass (M0); Identity real es M5 |
