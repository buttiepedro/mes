# Nexo — Diseño Técnico (MVP)

> **Estado:** En progreso — **alineado al modelo de 4 capas (2026-07-13)** · **Actualizado:** 2026-07-13
> **Roles:** Software Architect · Tech Lead
> **Base funcional:** [../specs/README.md](../specs/README.md) · **Modelo canónico:** [../specs/specs/layered-architecture.md](../specs/specs/layered-architecture.md) · **Decisiones:** [../specs/open-questions-board.md](../specs/open-questions-board.md)

Esta carpeta contiene el **diseño técnico** de Nexo, derivado de la documentación funcional de [`specs/`](../specs/README.md)
y de las decisiones ya cerradas. El foco de esta etapa es el **MVP**; el diseño de V1+ (protocolos industriales,
motor de reglas completo, multi-ERP, IA) se aborda después.

**Alcance:** artefactos de diseño (contratos, esquemas, specs, diagramas). **Todavía no es la implementación de la app.**

---

## Alineación con el modelo de 4 capas (2026-07-13)

> **El diseño técnico ya está alineado al modelo nuevo.** El documento ancla funcional es
> [**`layered-architecture.md`**](../specs/specs/layered-architecture.md): **Capa 1 Física/Gemelo digital →
> Capa 2 Modelo de trabajo (Procesos) → Capa 3 Ejecución (Lote o Proyecto) → Capa 4 Motor de eventos**, con el
> **ERP fuera del stack, como conector lateral OPCIONAL**.

| Qué cambió en el diseño | Dónde quedó registrado |
|---|---|
| Las capas son **vista conceptual**: no hay proyectos ni namespaces "Capa N"; la unidad de despliegue sigue siendo el *bounded context* | **ADR-T10** + mapeo capa ↔ servicio en [00-tech-baseline.md](./00-tech-baseline.md) **§2.2** |
| **ERP opcional** ⇒ **master data propia** (`Nexo.MasterData`) y **reencuadre de INT-01**; ningún flujo del MVP depende del conector | **ADR-T11** · [06-odoo-connector.md](./06-odoo-connector.md) · [../specs/specs/master-data.md](../specs/specs/master-data.md) |
| **Ambos perfiles** (lote y proyecto) en el MVP sobre un único agregado `Run` | **ADR-T12** · nuevo servicio `Nexo.Execution` |
| **DAG completo** de precedencias en el modelo de trabajo | **ADR-T13** · nuevo servicio `Nexo.WorkModel` |
| Topología: el ERP queda **lateral y punteado**; `Nexo.Connectors` se habilita por tenant | [00-tech-baseline.md](./00-tech-baseline.md) §3 |

**Lo que NO cambió:** DB-per-tenant en Neon, Control Plane global, backbone de eventos (MSK/MassTransit),
CQRS/read models, captura edge-first *outbound-only* y evento canónico inmutable. Ningún ADR previo (T1–T9)
se revierte.

---

## Stack confirmado (2026-07-11 · ampliado el 2026-07-13)

> **Nota de terminología:** en esta tabla "dimensión" es un eje del stack. Las **capas** (1 a 4) son siempre las
> del [modelo conceptual](../specs/specs/layered-architecture.md), nunca capas de despliegue.

| Dimensión | Decisión | Notas |
|---|---|---|
| **Backend** | **.NET (C#)** — microservicios | ASP.NET Core; convenciones en [00-tech-baseline.md](./00-tech-baseline.md) |
| **Base de datos** | **PostgreSQL en Neon** (serverless) | DB-per-tenant; **connection schema** en [01-multi-tenancy-connection.md](./01-multi-tenancy-connection.md) |
| **APIs / comunicación** | **REST/OpenAPI** (borde) + **eventos async** (broker) + **gRPC** (interno) | Envelope de evento en [02-event-model.md](./02-event-model.md) |
| **Nube** | **AWS** | Mapeo de servicios gestionados en [00-tech-baseline.md](./00-tech-baseline.md) |
| **Multi-tenancy** | DB-per-tenant (Neon) + Control Plane global | Requisito no negociable — ver [../specs/specs/multi-tenancy.md](../specs/specs/multi-tenancy.md) |
| **Modelo de dominio** | **4 capas como vista conceptual** + **ERP opcional** (2026-07-13) | Mapeo capa ↔ servicio en [00-tech-baseline.md](./00-tech-baseline.md) §2.2 — ver [../specs/specs/layered-architecture.md](../specs/specs/layered-architecture.md) |

Por qué Neon encaja con DB-per-tenant: **scale-to-zero** por tenant ocioso (economía a miles de tenants),
**branching** (entornos/efímeros y clonado para pruebas), **autoscaling** y **API de gestión** para automatizar
el provisioning (crear base/rol/branch por tenant en el flujo de alta de 7 pasos).

---

## Plan de documentos de diseño

| # | Documento | Capa(s) del modelo | Contenido | Estado |
|---|---|---|---|---|
| 00 | [00-tech-baseline.md](./00-tech-baseline.md) | Transversal (+ **mapeo capa ↔ servicio**, §2.2) | Stack, convenciones .NET, topología en AWS, comunicación, cross-cutting (auth, secretos, observabilidad, CI/CD), ADRs | ✅ Alineado (v0.2) |
| 01 | [01-multi-tenancy-connection.md](./01-multi-tenancy-connection.md) | Transversal | **Connection schema** por tenant, Tenant Connection Registry, provisioning en Neon, migraciones por cohortes | ✅ Sin impacto |
| 02 | [02-event-model.md](./02-event-model.md) | **Capa 4** | Envelope del Evento canónico, serialización, schema registry, topics/particiones, idempotencia | ⏳ A revisar: evidencia/valor/origen de primera clase y eventos de las capas 2 y 3 |
| 03 | [03-data-schema.md](./03-data-schema.md) | Todas | Esquema lógico por tenant + esquema del Control Plane (global) | ⏳ A ampliar: catálogos propios, Proceso/Tarea (DAG) y `Run` |
| 04 | [04-service-contracts.md](./04-service-contracts.md) | Todas | Contratos REST/OpenAPI + gRPC + eventos por servicio del MVP | ⏳ A ampliar: `Nexo.MasterData`, `Nexo.WorkModel`, `Nexo.Execution` |
| 05 | [05-edge-agent.md](./05-edge-agent.md) | **Capa 1** | Spec del Agente Edge (MVP: datalogger/CSV; protocolos industriales en V1) | ✅ Sin impacto |
| 06 | [06-odoo-connector.md](./06-odoo-connector.md) | **Lateral · OPCIONAL** | Conector Odoo: ACL, sync jobs, mapeo, reintentos + **modo standalone** y **conciliación al conectar un ERP más tarde** | ✅ Reencuadrado (v0.2) |
| 07 | [07-security.md](./07-security.md) | Transversal | AuthN/Z técnico, secretos (AWS SM), mTLS del edge | ✅ Sin impacto |
| 08 | [08-observability-ops.md](./08-observability-ops.md) | Transversal | OpenTelemetry, health por tenant/edge, despliegue EKS, CI/CD | ✅ Sin impacto |
| 09 | `09-master-data.md` *(planificado)* | **Capa 1–2** | Diseño de `Nexo.MasterData`: catálogos propios, gobierno por entidad, estados del registro, importador CSV con simulación | 🔲 Pendiente (ADR-T11) |
| 10 | `10-work-model-execution.md` *(planificado)* | **Capas 2–3** | Diseño de `Nexo.WorkModel` y `Nexo.Execution`: Proceso versionado, **DAG**, agregado `Run` con **ambos perfiles** | 🔲 Pendiente (ADR-T12/T13) |

> El plan puede crecer a medida que el diseño lo requiera. Las decisiones técnicas que surjan se registran
> como ADRs dentro de [00-tech-baseline.md](./00-tech-baseline.md) y, si son de negocio, en el [tablero](../specs/open-questions-board.md).

---

## Decisiones fundamentales cerradas (2026-07-11)

Las cuatro bifurcaciones que abría el stack quedaron resueltas y registradas como ADRs en [00-tech-baseline.md](./00-tech-baseline.md) §9:

1. **Neon — organización de tenants:** **proyecto por tenant** (aislamiento + scale-to-zero). → ADR-T3
2. **Mensajería asíncrona en AWS:** **Amazon MSK (Kafka Serverless)** tras MassTransit. → ADR-T4
3. **Estrategia de repositorios:** **monorepo** (.NET). → ADR-T8
4. **Identity Provider:** **Duende IdentityServer**. → ADR-T6

### Reencuadre del 2026-07-13 (modelo por capas)

Cuatro ADRs nuevos, también en [00-tech-baseline.md](./00-tech-baseline.md) §9:

5. **Capas = vista conceptual**, no descomposición de servicios ni de despliegue. → **ADR-T10**
6. **ERP opcional + master data propia** (`Nexo.MasterData`); **INT-01 reencuadrada** y costo de alcance explícito. → **ADR-T11**
7. **Ambos perfiles en el MVP** (lote y proyecto) sobre un único agregado `Run` (`Nexo.Execution`). → **ADR-T12**
8. **DAG completo** de precedencias desde el MVP (`Nexo.WorkModel`). → **ADR-T13**

Las decisiones técnicas que aún quedan abiertas (store de series de tiempo, serialización de eventos, API Gateway, cuotas de Neon a escala, frontend/tablet, runtime del edge y, desde el reencuadre, la frontera de `Nexo.MasterData`, la relación `Production`↔`Execution` y el mínimo viable de catálogos) están en [00-tech-baseline.md](./00-tech-baseline.md) §10 y se resuelven a medida que el diseño las necesita.
