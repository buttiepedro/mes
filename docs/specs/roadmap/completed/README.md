# Roadmap — ítems completamente implementados

> **Documento:** `specs/roadmap/completed/README.md` · **Actualizado:** 2026-07-26
> **Relacionados:** [roadmap.md](../roadmap.md) · [milestones.md](../milestones.md) · [design/completed/](../../../design/completed/README.md)

Este subfolder registra **qué ítems del roadmap ya están completamente implementados y verificados en código**. Es la vista "roadmap" de la bitácora técnica que vive en [`docs/design/completed/`](../../../design/completed/README.md): allí está el *cómo se construyó y se verificó*; acá está el *qué punto del plan quedó cerrado*.

## Por qué no se movieron los documentos de planificación

Los cuatro documentos de la carpeta padre —[`roadmap.md`](../roadmap.md), [`milestones.md`](../milestones.md), [`backlog.md`](../backlog.md) y [`vision.md`](../vision.md)— **siguen siendo documentos vivos** y **no** se archivan acá. Cada uno cubre todo el arco de fases (MVP → V1 → V2 → Enterprise); mientras el MVP no esté cerrado, **ninguno describe, como documento entero, trabajo "completamente implementado"**. Archivar cualquiera de ellos sería engañoso y además rompería la red de enlaces relativos entre las specs. Por eso lo que se registra acá son **ítems**, no archivos de plan.

## Criterio de entrada

Un ítem entra en esta lista **solo** cuando está **implementado y verificado** (compila + tests en verde + corre localmente), con su **evidencia técnica** en un registro de [`design/completed/`](../../../design/completed/README.md). Sin evidencia, no entra.

## Ítems de la fase MVP completamente implementados

| Ítem del roadmap (fase MVP) | Decisión(es) que materializa | Estado | Evidencia |
|---|---|---|---|
| Scaffold del monorepo (.NET 8, Clean Arch + CQRS, multi-tenant DB-per-tenant, outbox por servicio) + infraestructura local (Postgres, Redpanda/Kafka, MinIO, Jaeger) | — | ✅ Completado y verificado | [001](../../../design/completed/001-scaffold-inicial.md) |
| **Master data mínima SIN COSTO** (unidades, productos/ítems, personas, clientes) con ABM | **MOD-17** | ✅ Completado y verificado | [002](../../../design/completed/002-masterdata.md) |
| **Capa 2 · Procesos + DAG completo** (Proceso/Versión inmutable, tareas, precedencias FS/SS/FF+lag, validación de ciclos, versionado) | **MOD-18**, **MOD-20** | ✅ Completado y verificado | [003](../../../design/completed/003-workmodel.md) |
| **Capa 3 · Ejecución en ambos perfiles** (sabores Lote y Proyecto, DAG congelado por ejecución, evidencia obligatoria configurable) | **PRD-16**, **MOD-19** | ✅ Completado y verificado | [004](../../../design/completed/004-execution.md) |
| **Relay outbox → Kafka** (los eventos persistidos se publican al bus; `Nexo.BuildingBlocks.Outbox`) | — (M1) | ✅ Completado y verificado | [006](../../../design/completed/006-m1-outbox-relay.md) |
| **Capa 4 · Motor de eventos mínimo** (`Nexo.EventEngine`: progreso por ejecución derivado de hechos) | — (M2) | ✅ Completado y verificado | [007](../../../design/completed/007-m2-event-engine.md) |
| **Tablero en vivo** (progreso de ejecuciones en tiempo real, `http://localhost:5084/`) | — (M4) | ✅ Completado y verificado | [008](../../../design/completed/008-m4-dashboard.md) |
| **Flujo real end-to-end** (crear ejecución + avanzar tareas por API → progreso real en el tablero) | — (M3) | ✅ Completado y verificado | [009](../../../design/completed/009-m3-real-flow.md) |

> Con estos ítems, la **cadena completa Proceso → Ejecución → evento → progreso → tablero** está construida y verificada **con datos reales**, y quedaron validadas en código las decisiones **PRD-16 / MOD-17 / MOD-18 / MOD-19 / MOD-20** y el flujo event-driven. Es la "tajada vertical" (fase A del [roadmap de ejecución](../../../design/mvp-execution-roadmap.md)).

## Ítems de la fase MVP que NO están completos todavía

No entran acá hasta cumplir el criterio de entrada. Su desglose y orden viven en el **[roadmap de ejecución](../../../design/mvp-execution-roadmap.md)** (fases B-E):

- **Gemelo digital (Capa 1)**: jerarquía de activos + binding señal↔activo (M5).
- **Dominios de captura**: Scrap, Calidad, Paradas (hoy solo Producción es scaffold) + validadores de `Nexo.Production` (M6).
- **Ingesta de datalogger / CSV / Excel** + store-and-forward (M7).
- **Identity real** (Duende) — reemplaza el dev-bypass de desarrollo (M8).
- **Control Plane** (alta de tenant en 7 pasos, licencias) + **multi-tenancy productivo** (Connection Registry, relay multi-tenant) (M9-M10).
- **Capa 4 rica**: persistir el read model, tiempos muertos, cuellos de botella, **KPIs por perfil** (M11-M12).
- **gRPC WorkModel → Execution** (`GetPublishedVersion`; hoy el snapshot se pasa como input) (M13).
- **Frontend**: ABM master data, editor de procesos, formularios de captura en tablet (M14).
- **Conector Odoo** (opcional, modo conectado) (M15).

Cuando cualquiera se implemente y verifique, se agrega una fila a la tabla de arriba con su registro de `design/completed/`.
