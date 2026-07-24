# 01 · Multi-Tenancy y Connection Schema — Nexo (MVP)

> **Documento:** `design/01-multi-tenancy-connection.md` · **Estado:** Borrador v0.1 · **Actualizado:** 2026-07-11
> **Roles:** Software Architect · Tech Lead
> **Relacionados:** [00-tech-baseline.md](./00-tech-baseline.md) · [02-event-model.md](./02-event-model.md) · [03-data-schema.md](./03-data-schema.md) · [07-security.md](./07-security.md) · [08-observability-ops.md](./08-observability-ops.md) · [../specs/specs/multi-tenancy.md](../specs/specs/multi-tenancy.md) · [../specs/specs/control-plane.md](../specs/specs/control-plane.md) · [../specs/specs/master-data.md](../specs/specs/master-data.md)

## Resumen ejecutivo

Este documento traduce a **diseño técnico concreto** el requisito no negociable de Nexo: **una base de datos
dedicada por tenant** ([`multi-tenancy.md`](../specs/specs/multi-tenancy.md)). La decisión de infraestructura del
baseline ([`00-tech-baseline.md`](./00-tech-baseline.md), ADR-T3) es materializar cada tenant como **un proyecto
Neon** (Postgres serverless sobre AWS) independiente, y una **DB Global (Control Plane)** como proyecto Neon aparte
que aloja el **Tenant Connection Registry**.

El corazón del diseño es el **connection schema**: el modelo de datos del Registry, el formato de la cadena de
conexión de Neon (endpoint `-pooler` con PgBouncer vs. endpoint directo) y la regla de oro de que el Registry guarda
**solo la referencia** al secreto (un ARN de AWS Secrets Manager), **nunca** la credencial en claro. Sobre esa base
se definen: el **provisioning** de un tenant como **saga** de 7 pasos idempotente y con compensación —cuyo paso de
**seed** ahora carga la **master data semilla** que permite operar **sin ERP** desde el día uno (§2.5)—; la **resolución
de tenant por request** (host/subdominio o claim `tenant_id` del JWT → Registry con caché → `DbContext`), y su
propagación por MediatR, EF Core, MassTransit y los logs; las **migraciones across-tenants** por cohortes con feature
flags y objetivo zero-downtime; el **backup/DR y offboarding** apoyados en PITR y branching de Neon con borrado
verificable; y el **aislamiento** en datos, red (PrivateLink), storage (S3 por prefijo) y secretos.

El alcance es **DISEÑO**: se incluyen DDL Postgres, diagramas Mermaid, tablas e **fragmentos ilustrativos** de
C#/interfaces. No es la implementación de la app. Los puntos abiertos están en **§8 · Decisiones pendientes**.

---

## 0. Contexto y decisiones fijas heredadas del baseline

| Tema | Decisión fija (baseline) | Referencia |
|---|---|---|
| Aislamiento de datos | **Un proyecto Neon por tenant**; DB Global = proyecto Neon aparte | ADR-T3 |
| ORM / driver | **EF Core + Npgsql**; migraciones EF versionadas | ADR-T3 |
| Secretos | **AWS Secrets Manager**; el Registry guarda **solo referencias (ARN)** | §7 baseline |
| Mensajería | **MSK/Kafka** detrás de **MassTransit**; clave de partición = `tenant_id` | ADR-T4 |
| Identidad | **Duende IdentityServer**; JWT con claim `tenant_id` | ADR-T6 |
| Observabilidad | **OpenTelemetry**; correlación por `tenant_id` | ADR-T9 |
| Contexto de tenant | `ITenantContext` **scoped** atraviesa todo el pipeline | §5 baseline |

Todo lo que sigue **debe respetar** estas decisiones. Este documento no las reabre; las convierte en esquema y código
ilustrativo. El código productivo vive en `Nexo.BuildingBlocks.MultiTenancy` y `Nexo.Tenancy` (ver estructura del
monorepo en [`00-tech-baseline.md`](./00-tech-baseline.md) §2).

---

## 1. Connection schema — Tenant Connection Registry

El **Tenant Connection Registry** es la tabla-núcleo del Control Plane: mapea `tenant_id` → **coordenadas de conexión**
(proyecto/branch/endpoints/DB/rol) + **referencia al secreto** (ARN). Es la única fuente de verdad de "dónde y cómo me
conecto a la DB de un tenant". Vive en la **DB Global**, nunca en la DB de un tenant.

### 1.1 Principio: el Registry guarda coordenadas, el secreto guarda la credencial

```mermaid
flowchart LR
    REG["Tenant Connection Registry<br/>(DB Global / Control Plane)"]
    SM["AWS Secrets Manager<br/>1 secreto por tenant"]
    NEON[("Proyecto Neon del tenant<br/>(compute + storage aislados)")]

    REG -->|"secret_arn (referencia, NO credencial)"| SM
    SM -->|"password / connection URI (bajo demanda, en memoria)"| APP["Servicio por-tenant"]
    REG -->|"host_pooled / host_direct / database / role (NO sensible)"| APP
    APP -->|"SQL (TLS / PrivateLink)"| NEON
```

- **En el Registry** viven datos **no sensibles**: `neon_project_id`, `region`, `branch`, `database`, `role`,
  `endpoint_pooled`, `endpoint_direct`, `schema_version`, `status`, timestamps.
- **En Secrets Manager** vive lo **sensible**: la **contraseña** y/o la **cadena de conexión completa** (URIs pooled y
  direct). El Registry solo apunta con `secret_arn`.
- **Nunca** se persiste el password en el Registry, ni se escribe en logs, ni viaja en el JWT (ver
  [`../specs/specs/multi-tenancy.md`](../specs/specs/multi-tenancy.md) §5.2 y [`07-security.md`](./07-security.md)).
- El servicio compone la conexión en memoria: **coordenadas del Registry (cache) + password del secreto (cache corto
  con TTL)**. Así una operación normal necesita 0 lecturas de Secrets Manager (cache hit) y el Registry no expone
  credenciales aunque se filtre una consulta a la DB Global.

### 1.2 DDL — esquema `control_plane`

```sql
-- ============================================================
-- DB GLOBAL (Control Plane) · esquema de conexión de tenants
-- Proyecto Neon aparte, NO contiene dato operativo de clientes.
-- ============================================================
CREATE SCHEMA IF NOT EXISTS control_plane;

-- Estado del REGISTRO DE CONEXIÓN (no confundir con el ciclo de vida
-- comercial del tenant, que vive en control_plane.tenant).
CREATE TYPE control_plane.tenant_connection_status AS ENUM (
    'provisioning',   -- saga de alta en curso; aún no operable
    'active',         -- conexión utilizable por los servicios
    'migrating',      -- rollout de migración en curso sobre esta DB
    'suspended',      -- acceso operativo cortado (impago/incidente); DB preservada
    'offboarding',    -- export + borrado verificable en curso
    'deleted',        -- proyecto Neon eliminado (baja definitiva); fila conservada como tombstone
    'failed'          -- provisioning/rollout falló; requiere intervención
);

-- Nivel de aislamiento de red hacia Neon.
CREATE TYPE control_plane.neon_connectivity AS ENUM (
    'public_tls',     -- TLS público con IP allowlist (dev/staging)
    'privatelink'     -- AWS PrivateLink (prod)
);

-- ------------------------------------------------------------
-- Tenant (metadato comercial) — resumido; detalle en 03-data-schema.md
-- ------------------------------------------------------------
CREATE TABLE control_plane.tenant (
    tenant_id       uuid        PRIMARY KEY DEFAULT gen_random_uuid(),
    slug            citext      NOT NULL UNIQUE,          -- subdominio: <slug>.nexo.app
    legal_name      text        NOT NULL,
    lifecycle_state text        NOT NULL DEFAULT 'provisioning', -- ver control-plane.md §8
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now()
);

-- ------------------------------------------------------------
-- TENANT CONNECTION REGISTRY (tabla-núcleo de este documento)
-- ------------------------------------------------------------
CREATE TABLE control_plane.tenant_connection (
    tenant_id        uuid        PRIMARY KEY
                                 REFERENCES control_plane.tenant(tenant_id) ON DELETE RESTRICT,

    -- Coordenadas Neon (no sensibles)
    neon_org_id      text        NOT NULL,                       -- organización Neon (sharding/cuota, ver DT-04)
    neon_project_id  text        NOT NULL UNIQUE,                -- 1 proyecto Neon por tenant
    region           text        NOT NULL,                       -- p.ej. 'aws-us-east-2'
    branch           text        NOT NULL DEFAULT 'main',        -- branch productiva del tenant
    database         text        NOT NULL DEFAULT 'nexo',        -- DB operativa del tenant
    role             text        NOT NULL DEFAULT 'nexo_app',    -- rol de aplicación (mínimo privilegio)

    -- Endpoints (host, no sensible). El password NUNCA vive acá.
    endpoint_pooled  text        NOT NULL,   -- ep-...-pooler.<region>.aws.neon.tech  (PgBouncer)
    endpoint_direct  text        NOT NULL,   -- ep-...........<region>.aws.neon.tech  (directo, DDL/migraciones)
    port             int         NOT NULL DEFAULT 5432,

    -- REFERENCIA al secreto (ARN). Solo referencia; credencial en Secrets Manager.
    secret_arn       text        NOT NULL,   -- arn:aws:secretsmanager:...:secret:nexo/tenant/<id>/db-*
    secret_version   text        NULL,       -- VersionId fijado tras rotación (opcional)

    -- Conectividad / aislamiento de red
    connectivity     control_plane.neon_connectivity NOT NULL DEFAULT 'public_tls',
    vpce_id          text        NULL,        -- VPC Endpoint (PrivateLink) cuando connectivity='privatelink'
    ip_allowlist     inet[]      NULL,        -- allowlist cuando connectivity='public_tls'

    -- Storage por tenant (aislamiento S3, ver §6)
    s3_bucket        text        NOT NULL,    -- bucket compartido o dedicado
    s3_prefix        text        NOT NULL,    -- prefijo exclusivo del tenant: tenants/<id>/
    kms_key_arn      text        NULL,        -- CMK por tenant (opcional, feature enterprise)

    -- Versionado de esquema y estado del registro
    schema_version   text        NOT NULL DEFAULT '0',  -- última migración EF aplicada (mirror observable)
    target_version   text        NULL,                  -- versión objetivo durante un rollout
    cohort           text        NULL,                  -- cohorte de rollout (piloto/early/general/...)
    status           control_plane.tenant_connection_status NOT NULL DEFAULT 'provisioning',

    -- Auditoría / concurrencia
    provisioned_at   timestamptz NULL,
    activated_at     timestamptz NULL,
    last_migrated_at timestamptz NULL,
    deleted_at       timestamptz NULL,
    created_at       timestamptz NOT NULL DEFAULT now(),
    updated_at       timestamptz NOT NULL DEFAULT now(),
    row_version      xid8        NOT NULL DEFAULT pg_current_xact_id()  -- optimistic concurrency (EF: xmin/rowversion)
);

-- Índices de operación / observabilidad
CREATE INDEX ix_tenant_connection_status  ON control_plane.tenant_connection (status);
CREATE INDEX ix_tenant_connection_cohort  ON control_plane.tenant_connection (cohort, schema_version);
CREATE INDEX ix_tenant_connection_region  ON control_plane.tenant_connection (region);
CREATE UNIQUE INDEX ux_tenant_connection_secret ON control_plane.tenant_connection (secret_arn);

COMMENT ON TABLE  control_plane.tenant_connection IS
  'Tenant Connection Registry: coordenadas de conexión por tenant. Guarda SOLO referencias de secreto (secret_arn), nunca credenciales.';
COMMENT ON COLUMN control_plane.tenant_connection.secret_arn IS
  'ARN del secreto en AWS Secrets Manager con la cadena de conexión/credencial. NUNCA se guarda el password en claro.';
```

> **Nota `citext` / `gen_random_uuid`:** requieren las extensiones `citext` y `pgcrypto`, disponibles en Neon. Se
> habilitan en la migración inicial de la DB Global.

### 1.3 Formato de la cadena de conexión de Neon (pooled vs. directo)

Neon expone **dos hosts** por endpoint de cómputo. El sufijo `-pooler` enruta a través de **PgBouncer** (pooling del
lado servidor, modo *transaction*); el host sin sufijo es la **conexión directa**.

```text
# POOLED (runtime de la app: consultas transaccionales cortas, alta concurrencia)
postgresql://nexo_app:<PASSWORD>@ep-cool-darkness-123456-pooler.aws-us-east-2.aws.neon.tech/nexo?sslmode=require

# DIRECTO (migraciones/DDL, operaciones administrativas, sesiones largas, LISTEN/NOTIFY)
postgresql://nexo_app:<PASSWORD>@ep-cool-darkness-123456.aws-us-east-2.aws.neon.tech/nexo?sslmode=require
```

| Aspecto | Endpoint **pooled** (`-pooler`) | Endpoint **directo** |
|---|---|---|
| Uso en Nexo | **Runtime** de servicios por-tenant (99% de las queries) | **Migraciones EF**, seed, DDL, jobs admin, backup lógico |
| Pooling | **PgBouncer** server-side (miles de conexiones lógicas → pocas físicas) | Sin PgBouncer; 1 conexión = 1 backend |
| Encaja con | Scale-to-zero + muchos tenants ociosos + picos de concurrencia | Operaciones que necesitan sesión estable / features de sesión |
| Prepared statements | Compatibles con el pooler de Neon; en Npgsql se limita el auto-prepare (ver abajo) | Sin restricciones |

**Mapeo a Npgsql (ilustrativo).** El runtime usa el host pooled; las migraciones usan el directo:

```csharp
// Cadena reconstruida en memoria a partir de (Registry [cache] + password [secreto, cache TTL])
var runtime = new NpgsqlConnectionStringBuilder
{
    Host = reg.EndpointPooled,          // ...-pooler.<region>.aws.neon.tech
    Port = reg.Port,                    // 5432
    Database = reg.Database,            // "nexo"
    Username = reg.Role,                // "nexo_app"
    Password = secret.Password,         // desde Secrets Manager (NUNCA del Registry)
    SslMode = SslMode.VerifyFull,       // TLS obligatorio (o Require + PrivateLink)
    Pooling = true,                     // pool del lado cliente (Npgsql) además de PgBouncer
    MaxPoolSize = 20,                   // acotado: multiplicado por N servicios/pods
    MaxAutoPrepare = 0,                 // compat. con PgBouncer transaction mode
    Timeout = 15, CommandTimeout = 30
};

// Migraciones/DDL → SIEMPRE endpoint directo
var migrations = new NpgsqlConnectionStringBuilder(runtime.ToString())
{
    Host = reg.EndpointDirect,          // sin sufijo -pooler
    MaxPoolSize = 2                     // sesión estable, poco paralelismo
};
```

### 1.4 Estructura del secreto en Secrets Manager

Un secreto **por tenant**. El Registry solo guarda su `secret_arn`. Convención de nombre:
`nexo/tenant/<tenant_id>/db`. Contenido (JSON) — **el único lugar con la credencial**:

```jsonc
{
  "engine": "postgres",
  "host_pooled":  "ep-cool-darkness-123456-pooler.aws-us-east-2.aws.neon.tech",
  "host_direct":  "ep-cool-darkness-123456.aws-us-east-2.aws.neon.tech",
  "port": 5432,
  "database": "nexo",
  "username": "nexo_app",
  "password": "••••••••••••••••",          // secreto real; rota periódicamente
  "sslmode": "verify-full",
  "connection_uri_pooled": "postgresql://nexo_app:••••@ep-...-pooler.../nexo?sslmode=verify-full",
  "connection_uri_direct": "postgresql://nexo_app:••••@ep-...../nexo?sslmode=verify-full"
}
```

- **Rotación:** rotación programada (Lambda de rotación) cambia el password del rol Neon y actualiza el secreto; el
  resolver invalida su cache al detectar nueva `VersionId`. Detalle de política en [`07-security.md`](./07-security.md).
- **Aislamiento IAM:** cada servicio recibe una policy que permite `secretsmanager:GetSecretValue` **solo** sobre el
  patrón de secretos que necesita (ver §6.4).
- **Además del secreto de DB**, existe el **secreto de la API Key de Neon** (`nexo/platform/neon-api-key`), usado solo
  por `Nexo.Tenancy` durante el provisioning/offboarding.

---

## 2. Provisioning de tenant — saga de 7 pasos en Neon

El alta de tenant (los 7 pasos funcionales de [`multi-tenancy.md`](../specs/specs/multi-tenancy.md) §6) se implementa
como una **saga de orquestación** en `Nexo.Tenancy`, coordinada con **MassTransit** sobre MSK. Cada paso es
**idempotente** y tiene su **compensación**. El disparador es un rol global (Super Admin / Implementador) desde el
Control Plane ([`control-plane.md`](../specs/specs/control-plane.md) §3).

### 2.1 Pasos técnicos (mapeo a Neon/AWS)

| # | Paso funcional | Acción técnica | Compensación |
|---|---|---|---|
| 1 | Registrar empresa | `INSERT tenant` + `INSERT tenant_connection(status='provisioning')`; crea `provisioning_saga` (correlation_id) | Marcar `failed`; no borra tombstone |
| 2 | Crear DB del tenant | **Neon API** `POST /projects` (org, region) → `project_id`, endpoints, branch `main` | **Neon API** `DELETE /projects/{id}` |
| 3a | Crear database/role | Neon API `POST .../databases` + `POST .../roles` (rol `nexo_app` mínimo privilegio) | Cubierto por delete del proyecto |
| 3b | Guardar secreto + registrar | `PutSecretValue` (URI pooled/direct + password) → `secret_arn`; `UPDATE tenant_connection` con endpoints/ARN | `DeleteSecret`; limpiar coordenadas |
| 4 | Migraciones EF | Job de migración contra **endpoint directo**; setea `schema_version` | Idempotente; borrado del proyecto revierte |
| 5 | Seed | Seed **idempotente** de la **master data semilla**: unidades de medida estándar, roles base, motivos (reason codes), turnos base **y los catálogos propios inicializados y listos para operar sin ERP** (ver §2.5) | Idempotente; no requiere compensación |
| 6 | Usuario admin (Identity) | Llamada a **Duende/`Nexo.Identity`**: crea usuario admin del tenant + invitación | Deshabilitar/eliminar usuario admin |
| 7 | Activar | `status='active'`, `lifecycle_state='active'`, `activated_at`; publicar `TenantProvisioned`; notificar bienvenida | — |

> Los pasos 3a/3b y el orden "crear proyecto → guardar secreto → registrar" refinan el paso 6 funcional (registrar en
> el Registry) y el paso 2 funcional (crear DB) del spec. El resultado es el mismo contrato de 7 pasos.

### 2.2 Diagrama de secuencia

```mermaid
sequenceDiagram
    autonumber
    actor Admin as Super Admin / Implementador
    participant CP as Control Plane (Admin & Licensing)
    participant TP as Nexo.Tenancy (Saga)
    participant NEON as Neon API
    participant SM as AWS Secrets Manager
    participant REG as Tenant Connection Registry
    participant MIG as Migration/Seed Job
    participant IDP as Nexo.Identity (Duende)
    participant BUS as MSK / MassTransit

    Admin->>CP: Alta de empresa (datos comerciales + plan)
    CP->>TP: StartProvisioning(tenant_id, region, plan)
    TP->>REG: (1) INSERT tenant + tenant_connection(status=provisioning)
    TP->>NEON: (2) POST /projects (org, region)  [idempotency-key=tenant_id]
    NEON-->>TP: project_id, endpoints(pooled/direct), branch=main
    TP->>NEON: (3a) POST /databases + /roles (nexo_app)
    NEON-->>TP: database, role, password (one-time)
    TP->>SM: (3b) PutSecretValue(uri_pooled, uri_direct, password) -> secret_arn
    TP->>REG: (3b) UPDATE endpoints + secret_arn + s3_prefix
    TP->>MIG: (4) Aplicar migraciones EF (endpoint DIRECTO)
    MIG-->>TP: schema_version = N
    TP->>MIG: (5) Seed idempotente de MASTER DATA SEMILLA
    Note over MIG: Unidades estándar + roles base + motivos + turnos<br/>+ catálogos propios inicializados (productos, insumos,<br/>procesos, personas, centros de costo) en modo standalone
    TP->>IDP: (6) Crear usuario admin del tenant + invitación
    IDP-->>TP: admin_user_id
    TP->>REG: (7) status=active, schema_version=N, activated_at
    TP->>CP: lifecycle_state=active
    TP->>BUS: publish TenantProvisioned(tenant_id)
    BUS-->>CP: Notifications -> bienvenida al admin
    Note over TP,BUS: Cualquier fallo -> compensación en orden inverso (§2.4)
```

### 2.3 Estado de la saga (persistencia)

```sql
CREATE TYPE control_plane.provisioning_step AS ENUM (
    'registered','project_created','db_role_created','secret_stored',
    'migrated','seeded','admin_created','activated','compensating','compensated','failed');

CREATE TABLE control_plane.tenant_provisioning_saga (
    correlation_id  uuid PRIMARY KEY,              -- = idempotency key de la saga
    tenant_id       uuid NOT NULL REFERENCES control_plane.tenant(tenant_id),
    current_step    control_plane.provisioning_step NOT NULL DEFAULT 'registered',
    neon_project_id text NULL,                     -- se conoce tras el paso 2
    secret_arn      text NULL,                     -- se conoce tras el paso 3b
    admin_user_id   uuid NULL,                     -- se conoce tras el paso 6
    attempt         int  NOT NULL DEFAULT 0,
    last_error      text NULL,
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now()
);
```

Bosquejo de la máquina de estados (MassTransit `MassTransitStateMachine`, **ilustrativo**):

```csharp
public sealed class TenantProvisioningStateMachine : MassTransitStateMachine<ProvisioningState>
{
    public State ProjectCreated { get; } = default!;
    public State SecretStored  { get; } = default!;
    public State Migrated      { get; } = default!;
    public State AdminCreated  { get; } = default!;

    public TenantProvisioningStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Initially(
            When(ProvisioningRequested)
                .Then(ctx => ctx.Saga.TenantId = ctx.Message.TenantId)
                .SendAsync(/* CreateNeonProject */)   // idempotency-key = correlation_id
                .TransitionTo(ProjectCreated));

        During(ProjectCreated,
            When(NeonProjectCreated)
                .Then(ctx => ctx.Saga.NeonProjectId = ctx.Message.ProjectId)
                .SendAsync(/* StoreSecret + RegisterConnection */)
                .TransitionTo(SecretStored),
            When(NeonProjectFailed).ThenAsync(CompensateAsync).TransitionTo(Failed));

        // ... Migrated -> AdminCreated -> Activate (publica TenantProvisioned)
        // Cada 'When(...Failed)' encadena la compensación en orden inverso.
    }
}
```

### 2.4 Idempotencia, fallos y compensación

- **Idempotency key = `tenant_id`/`correlation_id`.** Se propaga a Neon API (header `Idempotency-Key`) y a los comandos
  de la saga; reintentar un paso ya aplicado no crea recursos duplicados.
- **Reintentos:** política Polly (retry exponencial + jitter) para errores transitorios de Neon API/AWS; el paso solo
  avanza cuando confirma. `attempt` y `last_error` quedan en `tenant_provisioning_saga` para observabilidad.
- **Detección de "ya hecho":**
  - Neon: buscar proyecto por **tag `tenant_id`** antes de crear (evita huérfanos si se perdió la respuesta).
  - Secreto: `DescribeSecret` por nombre convención antes de `CreateSecret`.
  - Registry: upsert por PK `tenant_id`.
  - Migraciones/seed: idempotentes por diseño (EF `__EFMigrationsHistory` + seed *merge*).
- **Compensación (orden inverso)** ante fallo no recuperable: eliminar usuario admin → borrar/schedule-delete del
  secreto → borrar proyecto Neon (`DELETE /projects/{id}`) → marcar `status='failed'` (la fila se conserva como
  tombstone para diagnóstico). El tenant queda en `Fallido` (ciclo de vida de
  [`control-plane.md`](../specs/specs/control-plane.md) §8), habilitando **reintento** o descarte.
- **Timeouts / poison:** si la saga excede un SLA, se agenda un `ProvisioningTimeout` que dispara compensación y alerta
  a Observability. Los mensajes irrecuperables van a **error queue** (MassTransit) para intervención manual.

### 2.5 Paso 5 en detalle — seed de master data semilla

Con el **ERP opcional** y la **master data propia** de la plataforma
([`master-data.md`](../specs/specs/master-data.md)), el seed deja de ser "cuatro catálogos de configuración" y pasa a
ser **la condición para que el tenant pueda operar el día uno en modo standalone**. Un tenant recién provisionado tiene
que poder cargar su planta, modelar un proceso y capturar producción **sin ningún conector activo**.

| Bloque del seed | Qué se crea | Por qué es imprescindible |
|---|---|---|
| **Unidades de medida** | Juego estándar por magnitud (SI + conteo + tiempo) con unidad base y factores de conversión | Todo lo demás las referencia; sin unidades, ningún número de la plataforma es interpretable ([`master-data.md`](../specs/specs/master-data.md) §2.4) |
| **Roles base** | Los roles canónicos del tenant (Administrador, Supervisor, Producción, Calidad, Mantenimiento, Integraciones, Gerencia, Operario) con sus scopes | El admin creado en el paso 6 necesita un rol existente; sin roles no hay delegación posible ([`07-security.md`](./07-security.md) §4) |
| **Motivos (reason codes)** | Catálogo mínimo de paradas, scrap y defectos | Downtime/Scrap/Quality exigen catálogo; los códigos libres no se admiten |
| **Turnos y calendario** | Turno único 24 h por defecto, editable | Sin ventana planificada no hay tiempo muerto medible |
| **Catálogos propios inicializados** | Productos/ítems, insumos, procesos, personas, centros de costo y clientes: **vacíos pero operables** — tabla creada, gobierno declarado (`fuente de verdad = Nexo`), plantilla CSV disponible y ABM habilitado | Es la diferencia entre "el catálogo no existe" y "el catálogo está vacío": el segundo se puede llenar el mismo día, el primero requiere una migración |
| **Modo de operación del tenant** | `operation_mode = standalone` y **gobierno por catálogo** con fuente de verdad = Nexo para todos | El modo es **por entidad**, no por tenant ([`master-data.md`](../specs/specs/master-data.md) §3.2); conectar un ERP después solo cambia esas declaraciones, no destruye el dato |

**Reglas del seed:**

- **Idempotente por clave natural** (código del registro), no por *insert* ciego: reejecutar el paso 5 tras un
  reintento de la saga **actualiza**, no duplica. Igual criterio que el importador CSV
  ([`master-data.md`](../specs/specs/master-data.md) §6.1).
- **Semilla ≠ dato del cliente.** Los registros semilla se marcan como tales para poder distinguirlos en soporte y
  para no confundir "catálogo vacío" con "catálogo sin cargar". El tenant puede archivarlos, nunca se le imponen.
- **Orden de dependencias obligatorio:** unidades → jerarquía física → roles/personas → motivos/turnos → resto de
  catálogos. Sembrar productos antes que unidades falla de entrada.
- **Sin compensación:** si el seed falla, la compensación efectiva es el borrado del proyecto Neon (paso 2). No existe
  un "des-seed" parcial.
- **El seed no crea jerarquía física ni procesos de negocio.** Crea el marco; la planta, las líneas, los activos y los
  procesos los carga el implantador o el cliente (asistente de implantación de
  [`master-data.md`](../specs/specs/master-data.md) §6.3).

> **Consecuencia operativa:** el criterio de aceptación del alta de tenant deja de ser "la DB responde" y pasa a ser
> **"un usuario puede entrar y declarar producción sin ERP"**. Ese es el nuevo *definition of done* del paso 7.

---

## 3. Resolución de tenant por request

Cada request a un servicio por-tenant **resuelve el tenant antes de tocar dato alguno**. La resolución produce un
`ITenantContext` **scoped** que atraviesa MediatR, EF, mensajería y logs.

### 3.1 Flujo

```mermaid
flowchart LR
    REQ["Request<br/>host: empresa.nexo.app<br/>Authorization: JWT(tenant_id)"] --> MW["TenantResolutionMiddleware"]
    MW -->|"1. host -> slug"| MAP["slug -> tenant_id (cache)"]
    MW -->|"2. claim tenant_id (JWT)"| CHK{"host == claim ?"}
    CHK -- no --> R403["403 tenant_mismatch"]
    CHK -- sí --> LK["3. lookup Registry (cache L1/L2)"]
    LK --> TC["ITenantContext (scoped)<br/>tenant_id, endpoints, secret_arn, schema_version"]
    TC --> MED["MediatR pipeline"]
    MED --> EF["TenantDbContext (endpoint pooled)"]
    EF --> DB[("Proyecto Neon del tenant")]
    TC --> LOG["Logs/Traces enriquecidos (tenant_id)"]
    TC --> MSG["Publicaciones MassTransit (header tenant_id)"]
```

Pasos:
1. **Host/subdominio** → `slug` → `tenant_id` (cache). Fuente inicial.
2. **Claim `tenant_id` del JWT** (Duende) → **fuente autoritativa** dentro de la sesión. Se **valida coherencia**
   host == claim; discrepancia = `403 tenant_mismatch` (ver [`07-security.md`](./07-security.md)).
3. **Lookup en el Registry** por `tenant_id`, servido desde caché (§3.4). Devuelve endpoints + `secret_arn` +
   `schema_version` + `status`. Si `status ∈ {suspended, provisioning, offboarding}` → se rechaza con el código
   correspondiente.
4. Se materializa `ITenantContext` **scoped** y se inyecta en el pipeline.

### 3.2 `ITenantContext` y contratos (ilustrativo)

```csharp
public interface ITenantContext
{
    Guid   TenantId       { get; }
    string Slug           { get; }
    string SchemaVersion  { get; }
    bool   IsResolved     { get; }
}

// Coordenadas de conexión (proyección del Registry, SIN password)
public sealed record TenantConnectionInfo(
    Guid   TenantId,
    string EndpointPooled,
    string EndpointDirect,
    string Database,
    string Role,
    int    Port,
    string SecretArn,
    string SchemaVersion,
    TenantConnectionStatus Status);

// Resuelve coordenadas (cache) y credencial (Secrets Manager, cache TTL)
public interface ITenantConnectionResolver
{
    Task<TenantConnectionInfo> GetAsync(Guid tenantId, CancellationToken ct);
    Task<string> BuildRuntimeConnectionStringAsync(Guid tenantId, CancellationToken ct);   // pooled
    Task<string> BuildMigrationConnectionStringAsync(Guid tenantId, CancellationToken ct); // directo
}
```

Middleware (borde del servicio, **antes** de MediatR):

```csharp
public sealed class TenantResolutionMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext ctx, ITenantResolver resolver, ITenantContextSetter setter)
    {
        var host  = ctx.Request.Host.Host;                       // empresa.nexo.app
        var claim = ctx.User.FindFirst("tenant_id")?.Value;      // JWT (Duende)
        var tenant = await resolver.ResolveAsync(host, claim);   // valida host==claim
        if (tenant is null) { ctx.Response.StatusCode = 403; return; }

        setter.Set(tenant);                                      // ITenantContext scoped
        using (LogContext.PushProperty("tenant_id", tenant.TenantId))  // Serilog -> OTel
            await next(ctx);
    }
}
```

### 3.3 Factory de `DbContext` por tenant (EF Core)

El `TenantDbContext` recibe su cadena de conexión **en runtime** desde el resolver; nunca se configura estáticamente.

```csharp
public interface ITenantDbContextFactory<TContext> where TContext : DbContext
{
    Task<TContext> CreateAsync(CancellationToken ct);   // usa endpoint POOLED
}

public sealed class TenantDbContextFactory<TContext>(
    ITenantContext tenant,
    ITenantConnectionResolver resolver) : ITenantDbContextFactory<TContext>
    where TContext : DbContext
{
    public async Task<TContext> CreateAsync(CancellationToken ct)
    {
        var cs = await resolver.BuildRuntimeConnectionStringAsync(tenant.TenantId, ct);
        var options = new DbContextOptionsBuilder<TContext>()
            .UseNpgsql(cs, o => o.MigrationsHistoryTable("__EFMigrationsHistory")
                                 .EnableRetryOnFailure())
            .Options;
        return (TContext)Activator.CreateInstance(typeof(TContext), options)!;
    }
}
```

Registro DI (por-request): `ITenantContext` **scoped**; el factory abre la conexión al proyecto Neon del tenant
resuelto. Si el tenant no está resuelto, el factory **lanza** (fail-closed): ningún handler puede tocar datos sin
tenant.

### 3.4 Caché del Registry

| Capa | Qué cachea | TTL / invalidación |
|---|---|---|
| **L1 (in-memory, por pod)** | `TenantConnectionInfo` (coordenadas, sin password) | TTL corto (p.ej. 60 s) + invalidación por evento |
| **L2 (distribuida, Redis opcional)** | Igual, compartida entre pods | TTL medio; invalidación por evento |
| **Secreto (Secrets Manager)** | password / URI | Cache con TTL (p.ej. 5 min); invalida al cambiar `VersionId` |

Invalidación por evento: `Nexo.Tenancy` publica `TenantConnectionChanged` / `TenantSuspended` / `TenantMigrated` en
MSK; los servicios lo consumen y **purgan** su entrada L1/L2. Así una suspensión o un cambio de endpoint se propaga en
segundos sin esperar el TTL.

### 3.5 Propagación del contexto (MediatR · EF · mensajería · logs)

- **MediatR:** un `TenantScopeBehavior` valida que `ITenantContext.IsResolved` y expone `tenant_id` a los handlers;
  un `LoggingBehavior` enriquece cada comando/query con `tenant_id` + `correlation_id`.
- **EF Core:** el `TenantDbContextFactory` liga la conexión al tenant resuelto; un `SaveChanges` interceptor puede
  sellar `tenant_id` en columnas de auditoría y en la **Outbox** (ver [`02-event-model.md`](./02-event-model.md)).
- **Mensajería (MassTransit/MSK):** filtros de **publish/send** estampan el header `tenant_id` desde `ITenantContext`;
  filtros de **consume** reconstruyen `ITenantContext` desde el header **antes** de ejecutar el consumer (idéntico a
  un request). La **clave de partición Kafka = `tenant_id`** (baseline §4.1).

```csharp
// Publish: estampa tenant_id como header saliente
public class TenantPublishFilter<T>(ITenantContext t) : IFilter<PublishContext<T>> where T : class
{
    public Task Send(PublishContext<T> ctx, IPipe<PublishContext<T>> next)
    { ctx.Headers.Set("tenant_id", t.TenantId.ToString()); return next.Send(ctx); }
    public void Probe(ProbeContext c) { }
}

// Consume: reconstruye ITenantContext desde el header
public class TenantConsumeFilter<T>(ITenantContextSetter s, ITenantResolver r) : IFilter<ConsumeContext<T>> where T : class
{
    public async Task Send(ConsumeContext<T> ctx, IPipe<ConsumeContext<T>> next)
    {
        var id = Guid.Parse(ctx.Headers.Get<string>("tenant_id")!);
        s.Set(await r.ResolveByIdAsync(id));
        await next.Send(ctx);
    }
    public void Probe(ProbeContext c) { }
}
```

- **Logs/Trazas (OpenTelemetry):** `tenant_id` va como **atributo/baggage** en toda traza y como propiedad estructurada
  en todo log (Serilog → OTel). Correlación transversal por tenant (baseline §7; [`08-observability-ops.md`](./08-observability-ops.md)).

---

## 4. Migraciones across-tenants

Con N proyectos Neon (uno por tenant), una migración no es un `dotnet ef database update`: es un **rollout gobernado por
cohortes con feature flags**, con estado por tenant y objetivo **zero-downtime** (decisión ya cerrada,
[`multi-tenancy.md`](../specs/specs/multi-tenancy.md) §8.1 y [`control-plane.md`](../specs/specs/control-plane.md) §7).

### 4.1 Modelo de versionado

- **EF Core migrations versionadas** en el assembly del servicio; cada DB de tenant tiene su `__EFMigrationsHistory`
  (fuente de verdad *física* de qué se aplicó).
- El Registry **espeja** el estado en `schema_version` / `target_version` / `cohort` para **observabilidad y planeo**
  del rollout (quién está atrasado, quién falló, quién al día) sin abrir cada DB.
- Se aplican contra el **endpoint directo** (DDL). El runtime sigue por el pooled.

### 4.2 Tracking por tenant

```sql
CREATE TYPE control_plane.migration_state AS ENUM ('pending','running','succeeded','failed','rolled_back');

CREATE TABLE control_plane.tenant_migration_status (
    tenant_id      uuid NOT NULL REFERENCES control_plane.tenant(tenant_id),
    target_version text NOT NULL,                 -- migración objetivo del rollout
    from_version   text NULL,
    cohort         text NOT NULL,                 -- piloto / early / general / total
    state          control_plane.migration_state NOT NULL DEFAULT 'pending',
    branch_backup  text NULL,                     -- branch Neon creada como snapshot pre-migración
    started_at     timestamptz NULL,
    finished_at    timestamptz NULL,
    error          text NULL,
    PRIMARY KEY (tenant_id, target_version)
);
CREATE INDEX ix_tmig_cohort_state ON control_plane.tenant_migration_status (cohort, state);
```

### 4.3 Job de rollout por cohortes

```mermaid
flowchart LR
    REL["Release N (imagen + migraciones EF)"] --> FF{"Feature flag<br/>rollout habilitado?"}
    FF -- sí --> C0["Cohorte 0: interno/piloto"]
    C0 -->|"éxito + gate"| C1["Cohorte 1: early adopters"]
    C1 -->|"éxito + gate"| C2["Cohorte 2: general"]
    C2 -->|"éxito + gate"| CT["Todos los tenants"]
    C0 -. incidente .-> RB["Rollback (§4.5)"]
    C1 -. incidente .-> RB
    C2 -. incidente .-> RB
```

- **Orquestación:** un **Kubernetes Job** (o comando de `Nexo.Tenancy`) itera los tenants de la cohorte objetivo. Por
  cada tenant: (1) crea un **branch Neon de respaldo** desde `main` (snapshot instantáneo, §4.5); (2) `state='running'`;
  (3) aplica migraciones EF contra el endpoint directo; (4) `state='succeeded'`, actualiza `schema_version` en el
  Registry; (5) emite `TenantMigrated` (invalida caché §3.4).
- **Feature flags** (Administration & Licensing) **desacoplan schema de comportamiento**: la migración puede estar
  aplicada pero el código nuevo permanece inactivo hasta encender el flag de la cohorte — habilita activar por lotes y
  cortar sin redeploy.
- **Gates entre cohortes:** métricas de salud/errores por cohorte en Observability deben estar verdes antes de avanzar.
- **Concurrencia acotada:** N migraciones en paralelo con límite (evita saturar Neon API / cuotas).

### 4.4 Zero-downtime: expand → migrate → contract

Migraciones **backward-compatible** en dos (o tres) fases, para que código viejo y nuevo convivan durante el rollout:

1. **Expand:** cambios **aditivos** (nueva columna nullable, nueva tabla, nuevo índice `CONCURRENTLY`). No rompen el
   código en producción.
2. **Migrate/backfill + activar:** desplegar código que **lee/escribe** lo nuevo detrás del **feature flag**; backfill
   idempotente de datos en background.
3. **Contract:** una vez toda la cohorte migró y el flag está estable, migración posterior que **elimina** lo viejo.

Esto evita bloqueos: se prohíben en una sola fase los cambios destructivos o los `ALTER` con lock largo sobre tablas
calientes (usar `CREATE INDEX CONCURRENTLY`, columnas nullable + default en fase separada, etc.).

### 4.5 Rollback

| Mecanismo | Cuándo | Cómo |
|---|---|---|
| **Forward-fix** (preferido) | Bug detectado con datos ya escritos | Nueva migración correctiva; nunca perder datos |
| **Migración `Down`** | Cambio reversible sin pérdida | `ef migrations` inverso por tenant afectado |
| **Branch Neon de respaldo** | Migración corrupta/irreversible en un tenant | Restaurar desde `branch_backup` (snapshot pre-migración, instantáneo) y repuntar el endpoint |
| **Feature flag off** | El schema está OK pero el comportamiento nuevo falla | Apagar el flag de la cohorte (sin tocar DB, inmediato) |

El **branch de respaldo por tenant** (creado en §4.3) da rollback casi instantáneo y aislado: revertir un tenant **no
afecta** a los demás (blast radius = 1). El estado `rolled_back` queda en `tenant_migration_status` para auditoría.

---

## 5. Backup/DR y offboarding por tenant

El modelo proyecto-por-tenant hace que backup, restore y borrado sean **operaciones por cliente**, con blast radius = 1.
Se apoyan en dos capacidades nativas de Neon: **PITR** (point-in-time recovery vía history retention) y **branching**
(copy-on-write instantáneo).

### 5.1 Backup / PITR / branching

| Capacidad | Uso en Nexo | Nota |
|---|---|---|
| **History retention (PITR)** | Restaurar la DB de un tenant a cualquier instante dentro de la ventana | Ventana por **plan/licencia** (RPO/RTO por plan, ver [`control-plane.md`](../specs/specs/control-plane.md) §5) |
| **Branching** | Clonar el estado a un instante como **branch** sin copiar TB; inspección/restore/preview | Instantáneo, aislado; también base de dev/staging (baseline §8) |
| **Export lógico** | Snapshot portable (offboarding, archivado, salida de la plataforma) | `pg_dump` contra endpoint directo → S3 del tenant |

Flujo de **restore a un punto en el tiempo** (sin afectar a otros tenants):

```mermaid
sequenceDiagram
    autonumber
    participant OP as Soporte / Job DR
    participant NEON as Neon API
    participant REG as Registry
    OP->>NEON: Crear branch desde main @ timestamp T (PITR)
    NEON-->>OP: branch "restore-T" (copy-on-write, instantáneo)
    OP->>NEON: Validar datos en el branch (read-only)
    alt Restauración total
        OP->>NEON: Promover branch / reset de main a "restore-T"
        OP->>REG: Actualizar endpoints/branch si cambió
    else Recuperación parcial
        OP->>OP: Extraer subset del branch y aplicar a main
    end
```

- **RPO/RTO por tenant**: derivados del plan; observables desde el Control Plane (última fecha de backup por tenant).
- **DR regional**: como cada tenant es un proyecto Neon con `region` propia, la estrategia de recuperación regional se
  decide por tenant sin acoplar a los demás (residencia de datos, §6 y decisiones pendientes).

### 5.2 Offboarding y borrado verificable

Al llegar a **baja definitiva** (fin de retención, [`control-plane.md`](../specs/specs/control-plane.md) §8), la baja
es un proceso auditado con **export total + borrado verificable + certificado**:

```mermaid
flowchart LR
    A["Baja definitiva (fin retención)"] --> B["status=offboarding"]
    B --> C["Export total: pg_dump -> S3 del tenant (cifrado)"]
    C --> D["Entregar/archivar export (según contrato)"]
    D --> E["DELETE proyecto Neon (Neon API)"]
    E --> F["ScheduleDeleteSecret (Secrets Manager)"]
    F --> G["Borrar prefijo S3 del tenant (+ CMK si dedicada)"]
    G --> H["status=deleted (tombstone) + deleted_at"]
    H --> I["Emitir Certificado de Borrado (hash, recursos, operador, timestamp)"]
    I --> J["Auditoría global + notificación"]
```

- **Export total** antes de destruir: `pg_dump` (endpoint directo) → objeto cifrado en el prefijo S3 del tenant; se
  entrega o archiva según contrato.
- **Borrado efectivo:** `DELETE /projects/{id}` (destruye compute + storage del tenant), `ScheduleDeleteSecret` del
  secreto de DB, borrado del prefijo S3 (y de la CMK dedicada si aplica).
- **Certificado de borrado:** documento firmado con `tenant_id`, lista de recursos destruidos (project_id, secret_arn,
  s3_prefix), operador, timestamp y **hash** del export; registrado en la **auditoría global** y notificado. La fila del
  Registry se conserva como **tombstone** (`status='deleted'`) para trazabilidad, sin ninguna coordenada utilizable.
- El **borrado verificable** cumple la promesa de aislamiento y de residencia/cumplimiento (ver
  [`07-security.md`](./07-security.md)).

---

## 6. Aislamiento (datos · red · storage · secretos)

Las cuatro dimensiones de aislamiento del spec ([`multi-tenancy.md`](../specs/specs/multi-tenancy.md) §1) se
materializan así:

### 6.1 Datos — proyecto Neon por tenant

- **Un proyecto Neon = compute + storage aislados** por tenant. Ninguna query cruza el límite del tenant porque **no hay
  otra DB de tenant** en el mismo proyecto. El aislamiento es **físico**, no un `WHERE tenant_id`.
- Rol de aplicación (`nexo_app`) de **mínimo privilegio** por tenant; sin acceso cruzado posible (credencial distinta,
  proyecto distinto).
- El **scale-to-zero** de Neon hace económico tener miles de proyectos ociosos (baseline §3, DT-04 abierto sobre cuotas).

### 6.2 Red — PrivateLink

| Entorno | Conectividad a Neon | Control |
|---|---|---|
| dev / staging | **TLS público** + **IP allowlist** (`ip_allowlist` en el Registry) | `connectivity='public_tls'` |
| **prod** | **AWS PrivateLink** (VPC Endpoint → Neon) | `connectivity='privatelink'`, `vpce_id` en el Registry |

- En prod el tráfico SQL **no sale a internet**: viaja por PrivateLink dentro de AWS (baseline §3). El `vpce_id` queda
  registrado por tenant/organización Neon.
- `SslMode=VerifyFull` (o `Require` sobre PrivateLink) en todos los entornos.

### 6.3 Storage — S3 por prefijo/tenant

- Cada tenant tiene **prefijo exclusivo** `tenants/<tenant_id>/` (columnas `s3_bucket` + `s3_prefix` del Registry).
- **IAM condition** sobre el prefijo: los servicios solo pueden `s3:GetObject`/`PutObject` bajo el prefijo del tenant
  **resuelto**; una policy con `Condition StringLike s3:prefix = tenants/<id>/*` impide cruce.
- **Cifrado**: SSE-KMS; CMK por tenant opcional (`kms_key_arn`, feature enterprise) para residencia/segregación fuerte.
- Detalle de política en [`07-security.md`](./07-security.md); evidencias/adjuntos por tenant en
  [`../specs/specs/multi-tenancy.md`](../specs/specs/multi-tenancy.md) §7.

### 6.4 Secretos — un secreto por tenant + IAM scoping

- **Un secreto por tenant** (`nexo/tenant/<id>/db`) con `secret_arn` referenciado en el Registry (nunca la credencial).
- **IAM least-privilege:** cada servicio recibe permiso `GetSecretValue` **solo** sobre el patrón que necesita; el
  provisioning/offboarding (secreto de la Neon API Key) queda restringido a `Nexo.Tenancy`.
- **Rotación** programada + ante incidente; invalidación de caché por `VersionId` (§3.4). El password **jamás** aparece
  en Registry, JWT ni logs.

```mermaid
flowchart TB
    subgraph T1["Tenant A"]
      PA[("Proyecto Neon A")]
      SA["Secreto A (ARN_A)"]
      S3A["s3://.../tenants/A/"]
    end
    subgraph T2["Tenant B"]
      PB[("Proyecto Neon B")]
      SB["Secreto B (ARN_B)"]
      S3B["s3://.../tenants/B/"]
    end
    REG["Tenant Connection Registry (DB Global)"] --> SA
    REG --> SB
    SVC["Servicio por-tenant (ctx = A)"] -->|"PrivateLink + TLS"| PA
    SVC -.->|"IAM: solo ARN_A"| SA
    SVC -.->|"IAM: solo prefijo A"| S3A
    SVC -. "sin acceso" .-x PB
```

---

## 7. Trazabilidad con el spec (checklist de cobertura)

| Requisito funcional | Dónde se diseña acá |
|---|---|
| DB-per-tenant, aislamiento total | §1, §6 |
| Control Plane = única base compartida, solo metadatos | §1.2 (esquema `control_plane`) |
| Tenant Connection Registry con **solo referencia** al secreto | §1.1, §1.2, §1.4 |
| Resolución por host + claim `tenant_id`, coherencia | §3.1, §3.2 |
| Alta en 7 pasos | §2 (saga) |
| Master data semilla en el alta: tenant operable **sin ERP** desde el día uno | §2.1 (paso 5), §2.5 |
| Migraciones versionadas/idempotentes por cohortes + feature flags + zero-downtime + estado observable | §4 |
| Backup/restore por tenant, PITR, recuperación granular | §5.1 |
| Baja lógica/definitiva, borrado seguro | §5.2 |
| Aislamiento datos/storage/cómputo/credenciales | §6 |

---

## 8. Decisiones pendientes

| # | Pregunta | Contexto | Default provisional |
|---|---|---|---|
| DP-01 | **Organización Neon a escala** (proyectos por org, cuotas) | Miles de proyectos-por-tenant; hereda **DT-04** del baseline | Confirmar plan enterprise Neon; sharding por `neon_org_id` (ya modelado en el Registry) |
| DP-02 | **Pooler + Npgsql**: ¿prepared statements habilitados en el pooler de Neon o `MaxAutoPrepare=0` fijo? | Rendimiento vs. compatibilidad PgBouncer | MVP: `MaxAutoPrepare=0` (seguro); medir y habilitar si Neon pooler lo soporta establemente |
| DP-03 | **Caché L2 (Redis) del Registry**: ¿se incluye en MVP o basta L1 + eventos? | Cientos/miles de pods resolviendo tenants | MVP: L1 in-memory + invalidación por evento; L2 si la carga lo exige |
| DP-04 | **PrivateLink**: ¿un VPC Endpoint por organización Neon o por proyecto? | Aislamiento de red vs. costo/gestión | Confirmar con Neon; default: por organización, `vpce_id` por tenant en el Registry |
| DP-05 | **RPO/RTO por plan** y ventana de PITR por licencia | Backup por plan (spec pregunta abierta) | Definir en Administration & Licensing; reflejar ventana de retención Neon por tenant |
| DP-06 | **Residencia de datos**: regiones ofrecidas en MVP/V1 | `region` por tenant ya soportada | A definir comercialmente; el diseño ya lo permite sin cambios de lógica |
| DP-07 | **Break-glass de Soporte** a la DB de un tenant | Acceso temporal auditado | Coordinar con [`07-security.md`](./07-security.md) y spec [`control-plane.md`](../specs/specs/control-plane.md) |
| DP-08 | **Densidad**: ¿algún día varios tenants por proyecto Neon para tenants muy chicos? | Costo por tenant pequeño | MVP: 1 proyecto = 1 tenant (aislamiento máximo). Reevaluar solo si el costo lo exige |

> Al resolverse, estas decisiones se promueven a ADR en [`00-tech-baseline.md`](./00-tech-baseline.md) o al
> [tablero de decisiones](../specs/open-questions-board.md) si son de negocio.
