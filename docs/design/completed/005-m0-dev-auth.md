# 005 · M0 — Modo dev sin auth (bypass de autenticación en Development)

> **Estado:** ✅ Completado y verificado · **Fecha:** 2026-07-26
> **Implementa:** [mvp-execution-roadmap.md](../mvp-execution-roadmap.md) · Milestone **M0**
> **Desbloquea:** ejercitar las 4 APIs end-to-end sin Identity (Duende), que aún no existe

## Qué es M0

Hasta ahora **todos los endpoints devolvían 401**: validan un JWT contra un `Authority:Issuer` (Duende IdentityServer) que **no está construido ni corriendo**. Sin destrabar esto no se puede ejercitar ni demostrar nada. M0 agrega un **bypass de autenticación que solo se registra en `Development`**.

## Qué se construyó

| Archivo | Contenido |
|---|---|
| `src/BuildingBlocks/Nexo.BuildingBlocks.Web/DevAuthentication.cs` | `DevAuthenticationHandler` (esquema `DevBypass`) que autentica **cada** request como un usuario dev con **todos los scopes `nexo.*`** y el **tenant demo** (claim `tenant_id`); + extensión `AddNexoDevAuth()` |
| Los 4 `Program.cs` (Execution, MasterData, WorkModel, Production) | La registración de auth se ramifica: `if (env.IsDevelopment()) AddNexoDevAuth(); else { AddJwtBearer(...) }`. En cualquier entorno que no sea Development, el flujo JWT/Duende real queda **intacto** |

**Cómo funciona sin fricción de tenant:** el handler emite el claim `tenant_id` = GUID del tenant demo; el `TenantResolutionMiddleware` lo lee y, aunque la key no matchee en el resolver de config, el `ConfigurationBasedDbContextFactory` cae a la connection string `*Default` (que apunta al mismo `nexo_tenant_demo`). Resultado: las lecturas/escrituras van al DB demo sin pasar headers.

## Decisiones de implementación

| # | Decisión | Motivo |
|---|---|---|
| 1 | **Bypass en un building block compartido**, no copiado en cada servicio | Un único punto; los 4 `Program.cs` solo ramifican una línea |
| 2 | **Solo default-scheme en Development**; el `else` conserva el JWT real | El andamiaje no contamina staging/prod; el día que exista Duende, no hay que revertir nada |
| 3 | **Scopes y tenant configurables** por `DevAuth:Scopes` / `DevAuth:TenantId` | Permite simular otro tenant o scopes acotados sin recompilar |
| 4 | El bypass **no reemplaza Identity** | La solución real (Duende + JWT + scopes por rol) es **M5** del roadmap de ejecución |

## Verificación ejecutada

| Comprobación | Resultado |
|---|---|
| `dotnet build nexo.sln` (27 proyectos) | ✅ **0 errores** (solo warning NU1902 OTel conocido) |
| 4 APIs levantadas con el build nuevo | ✅ `/health/ready` → 200 en 5080/5081/5082/5083 |
| `GET /v1/executions` (antes 401) | ✅ **200** → `[]` |
| `GET /v1/uoms`, `GET /v1/items` (MasterData) | ✅ **200** |
| `GET /v1/processes` (WorkModel) | ✅ **200** |
| `GET /v1/production/runs/{guid}` (Production) | ✅ **404** (sin dato) — **no 401**: auth pasó |

> **Regla que cambió:** el criterio "sin token → 401" (documentado en [004](./004-execution.md)) sigue valiendo **fuera de Development**; en Development ahora se responde autenticado como usuario dev.

## Pendientes que deja

| Pendiente | Prioridad | Detalle |
|---|---|---|
| **Identity real (Duende)** | Media | M0 es andamiaje; la seguridad de verdad (JWT, scopes por rol, MFA) es **M5** |
| **CORS para el tablero** | Baja | Cuando exista el tablero web (M4) habrá que habilitar CORS en Development |
