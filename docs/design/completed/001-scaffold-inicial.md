# 001 · Scaffold del monorepo + slice de Producción + infra local

> **Estado:** ✅ Completado y verificado · **Fecha:** 2026-07-13
> **Implementa:** [00-tech-baseline.md](../00-tech-baseline.md) · [03-data-schema.md](../03-data-schema.md) · [04-service-contracts.md](../04-service-contracts.md)

## Qué se construyó

Primera bajada de diseño a código: el esqueleto del monorepo .NET, una **rebanada vertical completa**
del caso estrella (Producción) y el entorno local de pruebas.

| Bloque | Contenido |
|---|---|
| **Configuración del repo** | `nexo.sln`, `Directory.Build.props`, `Directory.Packages.props` (Central Package Management), `global.json`, `.editorconfig`, `.gitignore` |
| **BuildingBlocks** (6 proyectos) | `Domain` (Entity, AggregateRoot, ValueObject, Result/Error, UuidV7), `Application` (CQRS con MediatR, behaviors de validación y logging), `MultiTenancy` (`ITenantContext`, resolución de conexión por tenant), `Messaging` (IntegrationEvent, OutboxMessage, `EventTypes` canónicos), `Observability` (Serilog + OpenTelemetry), `Web` (middleware de tenant, ProblemDetails RFC 7807) |
| **Nexo.Production** (4 proyectos) | `Domain` (WorkOrder, ProductionRun, ProductionRecord, Quantity), `Application` (RegisterProduction, CloseRun, queries), `Infrastructure` (EF Core/Npgsql, outbox transaccional en `SaveChanges`), `Api` (Minimal API `/v1`, JWT, Swagger, health) |
| **Tests** | `Nexo.Production.Tests` — xUnit sobre el dominio |
| **Infra local** | `docker-compose.yml`: Postgres, Redpanda (Kafka + schema registry), MinIO (S3), Jaeger (OTLP) |

## Decisiones de implementación

| # | Decisión | Motivo |
|---|---|---|
| 1 | **`IProductionDbContext` es estilo repositorio** (sin `DbSet<T>` expuestos) | El contrato de paquetes deja a `Application` sin dependencia de EF Core; el puerto queda limpio y los `DbSet` viven solo en `Infrastructure` |
| 2 | **Outbox escrito transaccionalmente en `SaveChanges`** | Los eventos de dominio se serializan a `platform.outbox_messages` en la misma transacción que el cambio de estado. El **relay** hacia Kafka queda como `TODO` explícito en `Program.cs` |
| 3 | **Resolver de conexión por configuración** (`ConfigurationTenantConnectionResolver`) | Permite correr local sin Control Plane. El resolver contra el Tenant Connection Registry sobre Neon queda documentado como pendiente en [01](../01-multi-tenancy-connection.md) |
| 4 | **Postgres local en el puerto 5433**, no 5432 | La máquina de desarrollo tiene un **PostgreSQL 17 nativo** ocupando `0.0.0.0:5432`; Docker solo podía bindear IPv6 y `localhost:5432` resolvía a la base equivocada (error `28P01`). No se tocó el Postgres del sistema |
| 5 | **SDK .NET 8 fijado con `rollForward: latestFeature`** | La máquina tenía .NET 10; el código apunta a `net8.0` (ADR-T1). Se instaló el SDK 8 y se fijó `global.json` para que compile y ejecute contra net8 de forma predecible |

## Verificación ejecutada

| Comprobación | Resultado |
|---|---|
| `dotnet build nexo.sln` | ✅ **0 errores** (4 warnings `NU1902`, ver pendientes) |
| `dotnet test` (dominio de Producción) | ✅ **10/10 pasan** |
| `docker compose up -d` | ✅ Postgres y Redpanda `healthy`; MinIO y Jaeger arriba |
| `dotnet ef database update` | ✅ Migración `InitialCreate` aplicada |
| Esquema resultante en `nexo_tenant_demo` | ✅ `production.work_orders`, `production.production_runs`, `production.production_records`, `platform.outbox_messages` |
| API arriba | ✅ `/health/live` y `/health/ready` → **200 Healthy** (conecta a Postgres **y** a Kafka) |
| Contrato expuesto | ✅ Swagger publica los 3 endpoints de `/v1/production` |
| Seguridad | ✅ `POST` sin token → **401** |

## Desvíos respecto del diseño

1. **Un error de compilación real corregido**: `WriteAsJsonAsync` no tiene sobrecarga `(valor, contentType)`; se
   usa la que lleva `options` + `contentType`.
2. **Puerto de Postgres 5433** en lugar del 5432 del diseño (ver decisión 4). Afecta `docker-compose.yml`,
   `appsettings*.json` y el design-time factory de EF.
3. **Kafka en `9092`**: el `appsettings.Development.json` generado apuntaba a `19092`, que no corresponde
   al puerto expuesto por Redpanda.

## Pendientes que deja abiertos

| Pendiente | Detalle |
|---|---|
| **Relay del outbox → Kafka** | El evento se persiste en `platform.outbox_messages` pero **nadie lo publica todavía**. Es el `TODO` de `Program.cs` |
| **Vulnerabilidad NU1902** | `OpenTelemetry.Exporter.OpenTelemetryProtocol` 1.9.0 tiene una vulnerabilidad moderada conocida; conviene bumpear |
| **Sin Identity** | Los `POST` requieren JWT y **no hay servicio Duende levantado**, así que no se pudo ejercitar la escritura end-to-end |
| **Modelo previo al pivot** | `Nexo.Production` implementa el modelo **anterior** al modelo por capas. La convivencia está diseñada en 4 fases (M0–M3) en [03](../03-data-schema.md) §2.9.1, y **la fase aditiva no invalida esta migración** |
