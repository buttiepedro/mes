# 006 · M1 — Relay outbox → Kafka

> **Estado:** ✅ Completado y verificado · **Fecha:** 2026-07-26
> **Implementa:** [mvp-execution-roadmap.md](../mvp-execution-roadmap.md) · Milestone **M1**
> **Salda:** el pendiente transversal #1 (**"Relay del outbox → Kafka"**), abierto desde [001](./001-scaffold-inicial.md)

## Qué es M1

Los eventos de dominio ya se persistían como filas de outbox (`{schema}.outbox_messages`) en la misma transacción que el agregado, pero **nadie las publicaba**: los productores de MassTransit estaban cableados pero sin drenador. M1 agrega el **relay** que lee el outbox y publica a Kafka. Es la pieza que hace que el sistema esté **vivo** (event-driven de verdad).

## Qué se construyó

**Nuevo building block `Nexo.BuildingBlocks.Outbox`** (para no contaminar la capa Application con Kafka/EF):

| Archivo | Contenido |
|---|---|
| `IOutboxPublisher` | Puerto: publica un `OutboxMessage` al bus |
| `KafkaOutboxPublisher` | Confluent.Kafka `IProducer<string,string>`: topic = `{Type}.v1`, key = `TenantId` (preserva orden por tenant), value = `Payload` (JSON), + headers `nexo-event-{type,id}` / `nexo-tenant-id` / `nexo-occurred-on`. `Acks=All`, `EnableIdempotence=true` |
| `OutboxRelayHostedService<TContext>` | `BackgroundService` genérico: cada 2 s abre un scope, lee hasta 100 filas con `ProcessedOn == null` (orden por `OccurredOn`), publica cada una, marca `ProcessedOn`, `SaveChanges`. **At-least-once**: si falla el publish, deja la fila pendiente con `Error` para reintentar |
| `AddOutboxRelay<TContext>()` | Registra el publisher (singleton) + el hosted service |

**Registrado en los 4 servicios** (`AddOutboxRelay<XDbContext>()` en cada `Program.cs`): Execution, MasterData, WorkModel, Production. Paquetes: `Confluent.Kafka` 2.5.2 + `Microsoft.Extensions.Hosting.Abstractions` 8.0.1 (Central Package Management).

## Decisiones de implementación

| # | Decisión | Motivo |
|---|---|---|
| 1 | **Building block dedicado**, no dentro de `Messaging` | Evita arrastrar EF Core + Confluent.Kafka a la capa Application, que referencia Messaging |
| 2 | **Confluent.Kafka directo**, no los productores tipados de MassTransit | El relay es genérico sobre `Type` (string) + `Payload` (JSON); no debe conocer cada tipo de evento CLR |
| 3 | **Topic = `{Type}.v1`** | Misma convención que los productores de MassTransit ya cableados; consistencia |
| 4 | **Key = TenantId** | Preserva el orden de los eventos de un tenant dentro de la partición |
| 5 | **Genérico sobre el DbContext** (`OutboxRelayHostedService<TContext>`) | Un solo relay sirve a los 4 servicios sin duplicar código; cada uno lo instancia con su DbContext |
| 6 | **At-least-once** (marca `ProcessedOn` tras publish OK) | Simple y correcto; el `dedup_key`/`EventId` downstream absorbe posibles reentregas |

## Multi-tenancy (limitación conocida en local)

El `BackgroundService` es singleton; abre un scope por tick y resuelve el `DbContext`. Sin tenant en el contexto ambiental (no hay request), el factory **cae a la connection `*Default`** = DB del tenant demo. El relay productivo deberá **iterar los tenants** del Connection Registry y fijar el tenant por scope — **TODO** documentado en el código y en [01-multi-tenancy-connection.md](../01-multi-tenancy-connection.md).

## Verificación ejecutada

| Comprobación | Resultado |
|---|---|
| `dotnet build nexo.sln` (28 proyectos) | ✅ **0 errores** (solo warning NU1902 OTel) |
| 4 APIs levantadas con el relay | ✅ `/health/ready` → 200 |
| `POST /v1/uoms` (crea UoM → escribe outbox) | ✅ **201** `{id: 019f9f62-...}` |
| Fila de outbox tras ~2 s | ✅ `nexo.masterdata.record_upserted` → **ProcessedOn seteado**, `Error` NULL |
| Topic en Kafka | ✅ `nexo.masterdata.record_upserted.v1` **auto-creado** (1 partición) |
| Mensaje en el topic | ✅ key = GUID del tenant demo; value = JSON del evento; headers `nexo-event-type/id`, `nexo-tenant-id`, `nexo-occurred-on` |

> Verificado con MasterData; como el relay es **genérico**, los otros 3 servicios (Execution/WorkModel/Production) publican por el mismo camino con solo cambiar el DbContext registrado.

## Pendientes que deja

| Pendiente | Prioridad | Detalle |
|---|---|---|
| **Relay multi-tenant real** (iterar Connection Registry) | Media | Hoy usa el fallback `*Default` (tenant demo) en el scope de background |
| **Reintentos con backoff / dead-letter** | Baja | Hoy reintenta cada tick sin límite; falta política de fallo persistente |
| **Consumidor de estos eventos (Capa 4)** | Alta | Es el próximo milestone **M2**: proyección de progreso por ejecución |
