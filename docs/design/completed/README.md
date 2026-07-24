# Registro de trabajo completado

> **Documento:** `docs/design/completed/README.md` · **Actualizado:** 2026-07-13

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

## Estado general del código

| Servicio | Capa | Estado |
|---|---|---|
| `BuildingBlocks` (Domain, Application, MultiTenancy, Messaging, Observability, Web) | transversal | ✅ Scaffold verificado |
| `Nexo.Production` | 3 (perfil repetitivo) | ✅ Scaffold verificado · modelo previo al pivot por capas |
| `Nexo.MasterData` | master data | ✅ Implementado y verificado ([002](./002-masterdata.md)) |
| `Nexo.WorkModel` | 2 | 🔜 Siguiente |
| `Nexo.Execution` | 3 | ⬜ Pendiente |
| `Nexo.Tenancy` · `Nexo.Identity` · `Nexo.Ingestion` · resto | varias | ⬜ Pendiente |

## Pendientes transversales acumulados

Los que **cruzan varios servicios** y conviene no perder de vista:

| Pendiente | Prioridad | Origen |
|---|---|---|
| **Relay del outbox → Kafka**: los eventos se persisten pero nadie los publica | **Alta** | [001](./001-scaffold-inicial.md) |
| **Alinear el outbox de `Nexo.Production`** a su propio schema (hoy usa `platform`) y actualizar `03-data-schema.md` con la convención "outbox por servicio" | **Alta** | [002](./002-masterdata.md) |
| **Registrar validadores en `Nexo.Production`** (los suyos no se ejecutan) | Media | [002](./002-masterdata.md) |
| **Sin servicio de Identity**: no se puede ejercitar escritura autenticada end-to-end | Media | [001](./001-scaffold-inicial.md) |
| **Vulnerabilidad NU1902** en OpenTelemetry OTLP 1.9.0 | Media | [001](./001-scaffold-inicial.md) |
