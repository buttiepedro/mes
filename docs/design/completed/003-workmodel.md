# 003 · Servicio `Nexo.WorkModel` (Capa 2 · modelo de trabajo)

> **Estado:** ✅ Completado y verificado · **Fecha:** 2026-07-24
> **Implementa:** [03-data-schema.md](../03-data-schema.md) §2.6 (`work`) · [04-service-contracts.md](../04-service-contracts.md) §2.6
> **Decisiones de producto que materializa:** **MOD-18** (DAG completo) · perfil `repetitive | project` (PRD-16)

## Qué es la Capa 2

Es **la plantilla del trabajo**: un `Process` versionado e **inmutable una vez publicado**, con sus `Task`s
en un **grafo dirigido acíclico** (precedencias con tipo y lag), sus insumos y su rol responsable. No conoce
ejecuciones ni cantidades reales. El atributo `profile` (`repetitive | project`) es lo único que distingue
"hacer ventanas" de "hacer una obra": el modelo es el mismo.

## Qué se construyó

| Proyecto | Contenido |
|---|---|
| **Domain** | `Process` (con `ProcessProfile`), `ProcessVersion` (Draft/Published/Suspended/Archived) como raíz del grafo, `WorkTask`, `TaskDependency` (tipo FS/SS/FF + lag), `TaskInput`, `TaskGraph`. Detección de ciclos y validación del grafo en el dominio. Eventos de dominio de publicación y suspensión |
| **Application** | Comandos: crear proceso, crear versión borrador, agregar/quitar tarea, `SetGraph`, `ValidateVersion`, publicar, suspender. Queries: listar procesos, versión publicada con grafo completo, tareas de una versión. Puerto `IWorkModelDbContext` (EF-free); integration events |
| **Infrastructure** | `WorkModelDbContext` (schema `work`); las lecturas de versión traen el **grafo completo** (`Include(Tasks).ThenInclude(Inputs).Include(Dependencies)`); outbox transaccional en `work.outbox_messages` |
| **Api** | Minimal API `/v1/processes/…` con 9 rutas; scopes `nexo.workmodel.read \| write \| publish` (publish separado por SoD) |
| **Tests** | 10 tests de dominio, foco en el DAG |

**Modelo creado** (`work`): `processes`, `process_versions`, `tasks`, `task_dependencies`, `task_inputs`, `outbox_messages`.

## Nota de proceso: el agente se cayó a mitad

El primer intento de scaffold **se colgó** (stall del stream a los 600 s) tras completar Domain y Application.
Se **rescató** lo escrito (que compilaba con 0 errores) y un segundo agente, acotado, completó Infrastructure,
Api y Tests imitando `Nexo.MasterData`. Lección: los scaffolds grandes conviene partirlos por capa desde el inicio.

## Decisiones de implementación

| # | Decisión | Motivo |
|---|---|---|
| 1 | **Detección de ciclos en el dominio** (no en la base) | El diseño propone un *constraint trigger* recursivo en Postgres. Se implementó el rechazo de ciclos, aristas triviales y duplicadas en el agregado (`Result` de error de dominio); el trigger de base queda como refuerzo futuro documentado |
| 2 | **Outbox en `work.outbox_messages`** | Misma convención "outbox por servicio" establecida en [002](./002-masterdata.md). Sin colisión con los demás |
| 3 | **Refs a `master.items` / `config.*` sin FK** | `TaskInput.ItemId` y `responsible_role_id` son referencias lógicas (uuid) sin foreign key: no se acoplan migraciones entre bounded contexts |
| 4 | **Una sola versión publicada por proceso** vía índice único parcial | `ux_process_versions_published ... WHERE state='published' AND deleted_at IS NULL`. Verificado en la base |
| 5 | **Sin costo** | Sin `standard_cost` ni tarifas (MOD-17) |

## Verificación ejecutada

| Comprobación | Resultado |
|---|---|
| `dotnet build nexo.sln` (21 proyectos) | ✅ **0 errores** |
| `dotnet test nexo.sln` | ✅ **49/49** (10 WorkModel + 10 Production + 29 MasterData) |
| `dotnet ef migrations add WorkModelInitial` + update | ✅ Aplicada |
| Tablas creadas | ✅ `work.{processes, process_versions, tasks, task_dependencies, task_inputs, outbox_messages}` |
| Índice DAG vs. diseño | ✅ `ux_process_versions_published` único parcial confirmado en la base |
| API arriba | ✅ `/health/ready` → **200 Healthy** |
| Contrato expuesto | ✅ 9 rutas `/v1/processes/…` |
| Seguridad | ✅ `GET /v1/processes` sin token → **401** |

## Deuda técnica saldada en este mismo trabajo

- ⭐ **`Nexo.Production` alineado a la convención "outbox por servicio".** Se cambió su configuración a
  `production.outbox_messages` y se aplicó la migración `OutboxToProductionSchema`. **`platform` quedó vacío**
  (0 tablas) — verificado. Cierra el desvío #1 de [002](./002-masterdata.md).
- **`03-data-schema.md` §2.17 corregido**: documenta ahora la convención "outbox por servicio" y por qué
  (surgió al implementar, con el error `42P07` como evidencia).

## Pendientes que deja abiertos

| Pendiente | Prioridad | Detalle |
|---|---|---|
| **Registrar validadores en `Nexo.Production`** | Media | Sigue con la omisión de [002](./002-masterdata.md); WorkModel y MasterData ya lo tienen |
| **Trigger de anti-ciclos en base** como refuerzo | Baja | La validación de dominio ya cubre el caso; el trigger sería defensa en profundidad |
| **Relay del outbox → Kafka** | Alta | Sigue pendiente para todos los servicios |
