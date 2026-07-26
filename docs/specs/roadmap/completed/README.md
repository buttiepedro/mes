# Roadmap — ítems completamente implementados

> **Documento:** `specs/roadmap/completed/README.md` · **Actualizado:** 2026-07-25
> **Relacionados:** [roadmap.md](../roadmap.md) · [milestones.md](../milestones.md) · [design/completed/](../../design/completed/README.md)

Este subfolder registra **qué ítems del roadmap ya están completamente implementados y verificados en código**. Es la vista "roadmap" de la bitácora técnica que vive en [`docs/design/completed/`](../../design/completed/README.md): allí está el *cómo se construyó y se verificó*; acá está el *qué punto del plan quedó cerrado*.

## Por qué no se movieron los documentos de planificación

Los cuatro documentos de la carpeta padre —[`roadmap.md`](../roadmap.md), [`milestones.md`](../milestones.md), [`backlog.md`](../backlog.md) y [`vision.md`](../vision.md)— **siguen siendo documentos vivos** y **no** se archivan acá. Cada uno cubre todo el arco de fases (MVP → V1 → V2 → Enterprise); mientras el MVP no esté cerrado, **ninguno describe, como documento entero, trabajo "completamente implementado"**. Archivar cualquiera de ellos sería engañoso y además rompería la red de enlaces relativos entre las specs. Por eso lo que se registra acá son **ítems**, no archivos de plan.

## Criterio de entrada

Un ítem entra en esta lista **solo** cuando está **implementado y verificado** (compila + tests en verde + corre localmente), con su **evidencia técnica** en un registro de [`design/completed/`](../../design/completed/README.md). Sin evidencia, no entra.

## Ítems de la fase MVP completamente implementados

| Ítem del roadmap (fase MVP) | Decisión(es) que materializa | Estado | Evidencia |
|---|---|---|---|
| Scaffold del monorepo (.NET 8, Clean Arch + CQRS, multi-tenant DB-per-tenant, outbox por servicio) + infraestructura local (Postgres, Redpanda/Kafka, MinIO, Jaeger) | — | ✅ Completado y verificado | [001](../../design/completed/001-scaffold-inicial.md) |
| **Master data mínima SIN COSTO** (unidades, productos/ítems, personas, clientes) con ABM | **MOD-17** | ✅ Completado y verificado | [002](../../design/completed/002-masterdata.md) |
| **Capa 2 · Procesos + DAG completo** (Proceso/Versión inmutable, tareas, precedencias FS/SS/FF+lag, validación de ciclos, versionado) | **MOD-18**, **MOD-20** | ✅ Completado y verificado | [003](../../design/completed/003-workmodel.md) |
| **Capa 3 · Ejecución en ambos perfiles** (sabores Lote y Proyecto, DAG congelado por ejecución, evidencia obligatoria configurable) | **PRD-16**, **MOD-19** | ✅ Completado y verificado | [004](../../design/completed/004-execution.md) |

> Con estos cuatro ítems, la cadena **Proceso → Ejecución (lote y proyecto)** del MVP está construida y verificada, y quedaron validadas en código las decisiones **PRD-16 / MOD-17 / MOD-18 / MOD-19 / MOD-20**.

## Ítems de la fase MVP que NO están completos todavía

No entran acá hasta cumplir el criterio de entrada:

- **Capa 4 · Motor de eventos** (contrato de evento canónico + métricas derivadas: progreso, tiempos muertos, cuellos de botella).
- **Relay del outbox → Kafka** (los eventos se persisten pero nadie los publica).
- **Integración gRPC WorkModel → Execution** (`GetPublishedVersion`; hoy el snapshot se pasa como input).
- **Servicio de Identity** (Duende); hoy todos los endpoints responden 401 sin token.
- **Ingesta de datalogger / CSV / Excel** + carga manual desde tablet (formularios de captura).
- **Tablero en tiempo real** con KPIs por perfil.
- **Conector Odoo** (opcional, modo conectado).
- `Nexo.Production`: registrar validadores (los suyos no se ejecutan) — modelo previo al pivot.

Cuando cualquiera de estos se implemente y verifique, se agrega una fila a la tabla de arriba con su registro de `design/completed/`.
