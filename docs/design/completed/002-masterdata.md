# 002 · Servicio `Nexo.MasterData` (catálogos mínimos del MVP)

> **Estado:** ✅ Completado y verificado · **Fecha:** 2026-07-13
> **Implementa:** [03-data-schema.md](../03-data-schema.md) §2.5 · [04-service-contracts.md](../04-service-contracts.md) §2.5
> **Decisión de producto que materializa:** **MOD-17** — master data mínima **sin costo**

## Por qué es el primer servicio del modelo por capas

Es la base de la promesa **"el ERP es opcional"**. Sin catálogos propios, el modo *standalone* no existe: no se
puede dar de alta un ítem, ni asignar una persona a una tarea, ni identificar al cliente de un proyecto.
Todo lo demás (Capa 2 Procesos, Capa 3 Ejecución) depende de que esto exista primero.

## Qué se construyó

| Proyecto | Contenido |
|---|---|
| **Domain** | Agregados `Uom`, `Item`, `Person`, `Customer` sobre una base común `MasterRecord` (código, governance, status, `external_ref`, auditoría, soft-delete, `Archive()`). Enums `MasterGovernance` (Local/Mirror/Linked/Divergent), `MasterStatus`, `ItemRole`, `TrackingMode`, `UomMagnitude`. Eventos de dominio de alta/actualización y archivado |
| **Application** | 6 comandos (`CreateUom`, `CreateItem`, `UpdateItem`, `ArchiveItem`, `CreatePerson`, `CreateCustomer`) con validadores FluentValidation; 8 queries (listado con filtros + get-by-id de los 4 catálogos); puerto `IMasterDataDbContext`; integration events mapeados a `EventTypes.MasterData_*` |
| **Infrastructure** | `MasterDataDbContext` (schema `master`), 4 configuraciones EF, outbox transaccional en `SaveChanges`, design-time factory, DI |
| **Api** | Minimal API `/v1` con 9 rutas, scopes `nexo.masterdata.read\|write\|admin`, JWT, Swagger, health checks, Dockerfile |
| **Tests** | 29 tests de dominio |

**Modelo de datos creado** (`master`): `uom`, `items`, `people`, `customers`, `outbox_messages`.

## Decisiones de implementación

| # | Decisión | Motivo |
|---|---|---|
| 1 | **`master.uom` se crea NUEVA**, no se reubica | El diseño indica `alter table config.uom set schema master`, pero **el schema `config` no existe** en la base real: la migración `InitialCreate` solo creó `production.*` y `platform.outbox_messages`. Tampoco se creó la vista de compatibilidad `config.uom` |
| 2 | **Sin FK hacia `config.*`** | `Person.DefaultRoleId`, `SiteId`, `LineId`, `UserId` e `Item.DefaultProcessId` quedan como `uuid` nullable **sin foreign key**, porque esas tablas todavía no existen. Única FK real: `fk_items_uom` → `master.uom` |
| 3 | ⭐ **Outbox por servicio, en el schema del servicio** (`master.outbox_messages`) | **Problema real encontrado al aplicar la migración.** Los servicios de un tenant comparten una única base física; con un `platform.outbox_messages` compartido, el primer servicio que migra crea la tabla y **todos los demás fallan** (Postgres `42P07: relation already exists`). El outbox pertenece a la **frontera transaccional del servicio**, así que cada uno es dueño del suyo: sin ambigüedad de propiedad ni dependencia de orden entre migraciones. Cada relay drena su propia tabla |
| 4 | **Valores de wire en inglés** (`product\|input`, `none\|batch\|serial`, `mass\|length\|…`) | El contrato REST los escribe en castellano pero el DDL —autoritativo para almacenamiento— los define en inglés. Se unificó en inglés para que la API y la columna hablen el mismo idioma |
| 5 | **Columnas `snake_case` explícitas** en todas las configuraciones EF | Necesario para que los filtros de los índices únicos parciales (`deleted_at IS NULL`) sean válidos. `Nexo.Production` había quedado con PascalCase |
| 6 | **Filtro por rol vía SQL** (`= ANY(roles)`) | `Roles` usa value converter a `text[]` y LINQ no lo traduce a *array containment*. Se verificó que el índice **GIN** se aplica |
| 7 | **`Item` único para producto e insumo** | Son **roles**, no catálogos separados. Separarlos rompería la genealogía multinivel de trazabilidad (el producto de una ejecución es insumo de la siguiente) |

## Verificación ejecutada

| Comprobación | Resultado |
|---|---|
| `dotnet build nexo.sln` (16 proyectos) | ✅ **0 errores** |
| `dotnet test nexo.sln` | ✅ **39/39** (10 Production + **29 MasterData**) |
| `dotnet ef migrations add` + `database update` | ✅ Migración `MasterDataInitial` aplicada |
| Tablas creadas | ✅ `master.uom`, `master.items`, `master.people`, `master.customers`, `master.outbox_messages` |
| Índices contra el diseño | ✅ `ix_items_roles` **GIN** sobre `roles`; `ux_items_code` y `ux_items_external_ref` **únicos parciales** con `deleted_at IS NULL` |
| API arriba | ✅ `/health/ready` → **200 Healthy** |
| Contrato expuesto | ✅ Swagger publica **9 rutas** `/v1/{uoms,items,people,customers}` + `:archive` |
| Seguridad | ✅ `GET /v1/items` sin token → **401** |

## Desvíos respecto del diseño

1. **Outbox en `master`, no en `platform`** (decisión 3). ⚠️ Esto deja una **inconsistencia con `Nexo.Production`**, que sigue usando `platform.outbox_messages`. Hay que alinearlo.
2. **`master.uom` nueva** y sin vista `config.uom` (decisión 1). Cuando exista el schema `config`, habrá que revisar si `uom` se unifica o convive.
3. **Sin reporte de impacto en `:archive`**: el contrato pide devolver `{eventos, ejecuciones, procesos}` afectados, pero eso requiere Traceability, Execution y WorkModel, que aún no existen. El endpoint archiva y devuelve el ítem.
4. **Extras mínimos sobre el contrato**: se agregaron `PUT /v1/items/{itemId}` y `GET` por id de los 4 catálogos, por coherencia con los comandos ya pedidos.
5. Se agregó `AddValidatorsFromAssemblyContaining` en `Program.cs`: sin eso el `ValidationBehavior` recibe la lista vacía y **los validadores no corren**.

## Pendientes que deja abiertos

| Pendiente | Prioridad | Detalle |
|---|---|---|
| **Alinear el outbox de `Nexo.Production`** a `production.outbox_messages` | Alta | Hoy quedan dos convenciones conviviendo. Requiere una migración de Production |
| **Actualizar `03-data-schema.md`** con la convención "outbox por servicio" | Alta | El diseño todavía dice `platform`; la realidad ya es otra |
| **Registrar validadores en `Nexo.Production`** | Media | Tiene la misma omisión que se corrigió acá: sus validadores no se ejecutan |
| **Importador CSV** (`/import-templates`, `/imports:csv`) | Media | Estaba fuera del alcance de este scaffold; es parte del MVP según MOD-17 |
| **Endpoints de governance y conciliación** | Baja | Modo conectado; no bloquea el MVP standalone |
| **Relay del outbox → Kafka** | Alta | Sigue pendiente para todos los servicios (heredado de [001](./001-scaffold-inicial.md)) |
