# Nexo — Diseño Técnico (MVP)

> **Estado:** En progreso · **Actualizado:** 2026-07-11
> **Roles:** Software Architect · Tech Lead
> **Base funcional:** [../specs/README.md](../specs/README.md) · **Decisiones:** [../specs/open-questions-board.md](../specs/open-questions-board.md)

Esta carpeta contiene el **diseño técnico** de Nexo, derivado de la documentación funcional de [`specs/`](../specs/README.md)
y de las decisiones ya cerradas. El foco de esta etapa es el **MVP**; el diseño de V1+ (protocolos industriales,
motor de reglas completo, multi-ERP, IA) se aborda después.

**Alcance:** artefactos de diseño (contratos, esquemas, specs, diagramas). **Todavía no es la implementación de la app.**

---

## Stack confirmado (2026-07-11)

| Capa | Decisión | Notas |
|---|---|---|
| **Backend** | **.NET (C#)** — microservicios | ASP.NET Core; convenciones en [00-tech-baseline.md](./00-tech-baseline.md) |
| **Base de datos** | **PostgreSQL en Neon** (serverless) | DB-per-tenant; **connection schema** en [01-multi-tenancy-connection.md](./01-multi-tenancy-connection.md) |
| **APIs / comunicación** | **REST/OpenAPI** (borde) + **eventos async** (broker) + **gRPC** (interno) | Envelope de evento en [02-event-model.md](./02-event-model.md) |
| **Nube** | **AWS** | Mapeo de servicios gestionados en [00-tech-baseline.md](./00-tech-baseline.md) |
| **Multi-tenancy** | DB-per-tenant (Neon) + Control Plane global | Requisito no negociable — ver [../specs/specs/multi-tenancy.md](../specs/specs/multi-tenancy.md) |

Por qué Neon encaja con DB-per-tenant: **scale-to-zero** por tenant ocioso (economía a miles de tenants),
**branching** (entornos/efímeros y clonado para pruebas), **autoscaling** y **API de gestión** para automatizar
el provisioning (crear base/rol/branch por tenant en el flujo de alta de 7 pasos).

---

## Plan de documentos de diseño

| # | Documento | Contenido |
|---|---|---|
| 00 | [00-tech-baseline.md](./00-tech-baseline.md) | Stack, convenciones .NET, topología en AWS, comunicación, cross-cutting (auth, secretos, observabilidad, CI/CD) |
| 01 | [01-multi-tenancy-connection.md](./01-multi-tenancy-connection.md) | **Connection schema** por tenant, Tenant Connection Registry, provisioning en Neon, migraciones por cohortes |
| 02 | [02-event-model.md](./02-event-model.md) | Envelope del Evento canónico, serialización, schema registry, topics/particiones, idempotencia |
| 03 | [03-data-schema.md](./03-data-schema.md) | Esquema lógico por tenant + esquema del Control Plane (global) |
| 04 | [04-service-contracts.md](./04-service-contracts.md) | Contratos REST/OpenAPI + gRPC + eventos por servicio del MVP |
| 05 | [05-edge-agent.md](./05-edge-agent.md) | Spec del Agente Edge (MVP: datalogger/CSV; protocolos industriales en V1) |
| 06 | [06-odoo-connector.md](./06-odoo-connector.md) | Conector Odoo: ACL, sync jobs, mapeo, reintentos |
| 07 | [07-security.md](./07-security.md) | AuthN/Z técnico, secretos (AWS SM), mTLS del edge |
| 08 | [08-observability-ops.md](./08-observability-ops.md) | OpenTelemetry, health por tenant/edge, despliegue EKS, CI/CD |

> El plan puede crecer a medida que el diseño lo requiera. Las decisiones técnicas que surjan se registran
> como ADRs dentro de [00-tech-baseline.md](./00-tech-baseline.md) y, si son de negocio, en el [tablero](../specs/open-questions-board.md).

---

## Decisiones fundamentales cerradas (2026-07-11)

Las cuatro bifurcaciones que abría el stack quedaron resueltas y registradas como ADRs en [00-tech-baseline.md](./00-tech-baseline.md) §9:

1. **Neon — organización de tenants:** **proyecto por tenant** (aislamiento + scale-to-zero). → ADR-T3
2. **Mensajería asíncrona en AWS:** **Amazon MSK (Kafka Serverless)** tras MassTransit. → ADR-T4
3. **Estrategia de repositorios:** **monorepo** (.NET). → ADR-T8
4. **Identity Provider:** **Duende IdentityServer**. → ADR-T6

Las decisiones técnicas que aún quedan abiertas (store de series de tiempo, serialización de eventos, API Gateway, cuotas de Neon a escala, frontend/tablet, runtime del edge) están en [00-tech-baseline.md](./00-tech-baseline.md) §10 y se resuelven a medida que el diseño las necesita.
