# Nexo

Plataforma industrial SaaS que actúa como **capa única de captura de datos entre la planta y el ERP**
(agnóstica de ERP; primer ERP soportado: Odoo). Multi-tenant con **base de datos por tenant**,
Cloud Native sobre AWS, microservicios en .NET.

Este repositorio contiene, por ahora, la **documentación** (funcional y técnica) y el **scaffolding
inicial** del backend.

## Estructura del repositorio

| Carpeta | Qué es |
|---|---|
| [`docs/specs/`](./docs/specs/README.md) | Documentación **funcional** (producto, dominios, UX, roadmap) |
| [`docs/design/`](./docs/design/README.md) | **Diseño técnico** del MVP (baseline, multi-tenancy, eventos, esquema, contratos, edge, Odoo, seguridad, ops) |
| `src/` | Código .NET (monorepo): `BuildingBlocks/`, `ControlPlane/`, `Services/`, `Gateways/` |
| `tests/` | Pruebas |
| `deploy/` | Infra local y despliegue |
| `docker-compose.yml` | Entorno local de pruebas |

> **Decisiones vivas:** [`docs/specs/open-questions-board.md`](./docs/specs/open-questions-board.md) ·
> **Stack y ADRs:** [`docs/design/00-tech-baseline.md`](./docs/design/00-tech-baseline.md)

## Estado del scaffolding

Se scaffoldeó una **rebanada vertical del caso estrella (Producción)** end-to-end, más los paquetes
compartidos (`BuildingBlocks`) y la infra local. El resto de los servicios (Devices, Quality, Scrap,
Downtime, Traceability, Connectors, Dashboards, Notifications, Tenancy, Identity, Gateway) siguen el
**mismo patrón** de `Nexo.Production` y se irán agregando.

Verificado localmente: `dotnet build` ✅ · tests 10/10 ✅ · migración EF aplicada ✅ · API arriba
(`/health/live` y `/health/ready` → 200) contra Postgres + Kafka ✅.

## Requisitos

- **.NET SDK 8** (el código apunta a `net8.0`; `global.json` fija el SDK 8.x).
  Si tenés .NET 10 instalado, igual necesitás el 8: `winget install Microsoft.DotNet.SDK.8`.
- **Docker** + Docker Compose.
- **dotnet-ef**: `dotnet tool install --global dotnet-ef --version 8.0.8`

## Puesta en marcha local

```bash
# 1) Infra local (Postgres, Redpanda/Kafka, MinIO, Jaeger)
cp .env.example .env
docker compose up -d

# 2) Compilar y testear
dotnet build nexo.sln
dotnet test tests/Nexo.Production.Tests/Nexo.Production.Tests.csproj

# 3) Aplicar migraciones a la DB del tenant demo
dotnet ef database update \
  -p src/Services/Nexo.Production/Nexo.Production.Infrastructure \
  -s src/Services/Nexo.Production/Nexo.Production.Api

# 4) Correr la API de Producción
ASPNETCORE_URLS=http://localhost:5080 \
  dotnet run --project src/Services/Nexo.Production/Nexo.Production.Api
# Swagger: http://localhost:5080/swagger   ·   Health: /health/live · /health/ready

# --- Alternativa: todo en contenedores ---
docker compose --profile app up -d --build
```

### Puertos locales

| Servicio | Host | Nota |
|---|---|---|
| PostgreSQL | **5433** | ⚠️ Se usa 5433 (no 5432) a propósito: si hay un **PostgreSQL nativo** instalado, ocupa el 5432 y `localhost:5432` iría a la base equivocada. |
| Redpanda (Kafka) | 9092 | Schema Registry en 8081 |
| Consola Redpanda | 8080 | |
| MinIO (S3) | 9000 / 9001 | Consola en 9001 |
| Jaeger | 16686 | OTLP en 4317/4318 |
| Production.API | 5080 | Swagger en `/swagger` |

> Los endpoints de escritura requieren **JWT** (`[Authorize]` con scopes). Sin el servicio de Identity
> (Duende) levantado, un POST responde **401** — es el comportamiento esperado.

## Convenciones de código

- .NET 8, **Clean Architecture + CQRS (MediatR)** por servicio, **Central Package Management**
  ([`Directory.Packages.props`](./Directory.Packages.props)).
- Multi-tenant: `ITenantContext` + resolución de conexión por tenant (local: por configuración;
  prod: Tenant Connection Registry sobre Neon — ver [`docs/design/01`](./docs/design/01-multi-tenancy-connection.md)).
- Eventos: envelope canónico + catálogo único ([`docs/design/02`](./docs/design/02-event-model.md)); constantes en
  `Nexo.BuildingBlocks.Messaging.EventTypes`.
