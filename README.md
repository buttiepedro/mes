# Nexo

Plataforma industrial SaaS que actúa como **capa única de captura de datos entre la planta y el ERP**
(agnóstica de ERP; primer ERP soportado: Odoo). Multi-tenant con **base de datos por tenant**,
Cloud Native sobre AWS, microservicios en .NET.

Este repositorio contiene, por ahora, la **documentación** (funcional y técnica) y el **scaffolding
inicial** del backend.

## Estructura del repositorio

| Carpeta | Qué es |
|---|---|
| [`specs/`](./specs/README.md) | Documentación **funcional** (producto, dominios, UX, roadmap) |
| [`design/`](./design/README.md) | **Diseño técnico** del MVP (baseline, multi-tenancy, eventos, esquema, contratos, edge, Odoo, seguridad, ops) |
| `src/` | Código .NET (monorepo): `BuildingBlocks/`, `ControlPlane/`, `Services/`, `Gateways/` |
| `tests/` | Pruebas |
| `deploy/` | Infra local y despliegue |
| `docker-compose.yml` | Entorno local de pruebas |

> **Decisiones vivas:** [`specs/open-questions-board.md`](./specs/open-questions-board.md) ·
> **Stack y ADRs:** [`design/00-tech-baseline.md`](./design/00-tech-baseline.md)

## Estado del scaffolding

Se scaffoldeó una **rebanada vertical del caso estrella (Producción)** end-to-end, más los paquetes
compartidos (`BuildingBlocks`) y la infra local. El resto de los servicios (Devices, Quality, Scrap,
Downtime, Traceability, Connectors, Dashboards, Notifications, Tenancy, Identity, Gateway) siguen el
**mismo patrón** de `Nexo.Production` y se irán agregando.

> ⚠️ El scaffolding se generó sin compilar localmente (esta máquina no tiene .NET SDK ni Docker).
> Al primer `dotnet build` pueden requerirse ajustes menores de versiones de paquetes; reportalos y se corrigen.

## Requisitos

- **.NET SDK 8.0+**
- **Docker** + Docker Compose

## Puesta en marcha local

```bash
# 1) Infra local (Postgres, Redpanda/Kafka, MinIO, Jaeger)
cp .env.example .env
docker compose up -d

# 2) Compilar la solución
dotnet build nexo.sln

# 3) Migraciones de la DB del tenant demo (Producción)
#    (requiere dotnet-ef:  dotnet tool install --global dotnet-ef)
cd src/Services/Nexo.Production/Nexo.Production.Infrastructure
dotnet ef migrations add InitialCreate -s ../Nexo.Production.Api
dotnet ef database update -s ../Nexo.Production.Api
cd -

# 4) Correr la API de Producción
dotnet run --project src/Services/Nexo.Production/Nexo.Production.Api
# Swagger:  http://localhost:<puerto>/swagger

# --- Alternativa: todo en contenedores ---
docker compose --profile app up -d --build
```

**Consolas locales:** Redpanda `http://localhost:8080` · MinIO `http://localhost:9001` · Jaeger `http://localhost:16686`

## Convenciones de código

- .NET 8, **Clean Architecture + CQRS (MediatR)** por servicio, **Central Package Management**
  ([`Directory.Packages.props`](./Directory.Packages.props)).
- Multi-tenant: `ITenantContext` + resolución de conexión por tenant (local: por configuración;
  prod: Tenant Connection Registry sobre Neon — ver [`design/01`](./design/01-multi-tenancy-connection.md)).
- Eventos: envelope canónico + catálogo único ([`design/02`](./design/02-event-model.md)); constantes en
  `Nexo.BuildingBlocks.Messaging.EventTypes`.
