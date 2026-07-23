# EF Core migrations — Nexo.Production.Infrastructure

Migration files are **not** committed by the scaffold (no .NET SDK on the generating machine).
Generate the initial migration once the SDK is available.

## Prerequisites

- .NET 8 SDK
- `dotnet-ef` tool: `dotnet tool install --global dotnet-ef` (or `dotnet tool restore`)
- The `Microsoft.EntityFrameworkCore.Design` package is referenced by the startup project
  (`Nexo.Production.Api`), which is required for the tooling.

## Create the initial migration

Run from this directory (`src/Services/Nexo.Production/Nexo.Production.Infrastructure`):

```bash
dotnet ef migrations add InitialCreate -s ../Nexo.Production.Api
```

- `-s ../Nexo.Production.Api` sets the **startup project** (it carries `EntityFrameworkCore.Design`
  and the runtime configuration). The DbContext itself lives in this Infrastructure project.
- At design time the context is built by `ProductionDbContextDesignTimeFactory`, which reads the
  connection string from the `PRODUCTION_DB_CONNECTION` environment variable (falling back to a
  local Postgres default). No tenant resolution happens at design time.

## Apply the migration

```bash
dotnet ef database update -s ../Nexo.Production.Api
```

## Notes

- The domain tables (`work_orders`, `production_runs`, `production_records`) are created in the
  **`production`** schema; the transactional outbox (`outbox_messages`) is created in the shared
  **`platform`** schema. Both schemas must exist in the tenant database (the local docker-compose
  init SQL seeds `nexo_tenant_demo`).
- In production, migrations are applied per tenant during provisioning (see
  `docs/design/01-multi-tenancy-connection.md`), not at API startup.
