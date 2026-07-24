# 00 · Baseline Técnico — Nexo (MVP)

> **Documento:** `design/00-tech-baseline.md` · **Estado:** Borrador v0.2 · **Actualizado:** 2026-07-13
> **Roles:** Software Architect · Tech Lead
> **Relacionados:** [README](./README.md) · [01-multi-tenancy-connection.md](./01-multi-tenancy-connection.md) · [02-event-model.md](./02-event-model.md) · [06-odoo-connector.md](./06-odoo-connector.md) · [../specs/specs/architecture.md](../specs/specs/architecture.md) · [../specs/specs/layered-architecture.md](../specs/specs/layered-architecture.md) · [../specs/specs/master-data.md](../specs/specs/master-data.md) · [../specs/specs/integrations.md](../specs/specs/integrations.md)

## Resumen ejecutivo

Este documento es el **ancla técnica** de Nexo. Fija el stack, las convenciones de código, la topología en
AWS, los patrones de comunicación y los aspectos transversales (identidad, secretos, observabilidad, CI/CD)
que **todos los demás documentos de diseño deben respetar**. Traduce las decisiones funcionales de
[`specs/`](../specs/README.md) a un diseño técnico concreto para el **MVP**, sin romper el camino hacia la
escala Enterprise (miles de tenants, millones de eventos/día).

Las decisiones aquí registradas están cerradas (ver §9, ADRs). Los puntos aún abiertos están en §10.

> **Reencuadre del 2026-07-13 — modelo de 4 capas y ERP opcional.** El diseño técnico queda alineado al
> **modelo conceptual de 4 capas** (física/gemelo digital · modelo de trabajo/procesos · ejecución · motor de
> eventos) definido en [`layered-architecture.md`](../specs/specs/layered-architecture.md). Las capas son una
> **vista conceptual del dominio**: **no** son capas de despliegue, **no** son módulos de código y **no**
> reemplazan los *bounded contexts* — el mapeo capa ↔ servicio está en **§2.2** (ADR-T10). El **ERP pasa a ser
> opcional**: la plataforma opera en **modo standalone** con **master data propia**
> ([`master-data.md`](../specs/specs/master-data.md)), lo que incorpora el servicio `Nexo.MasterData` y
> **reencuadra INT-01** (ADR-T11, ver [06-odoo-connector.md](./06-odoo-connector.md)). El MVP soporta los **dos
> perfiles** de trabajo —lote y proyecto— sobre el mismo modelo de Ejecución (ADR-T12) y **DAG completo** de
> precedencias en el modelo de trabajo (ADR-T13). **Ninguna decisión técnica estructural previa se revierte**:
> DB-per-tenant, Control Plane, backbone de eventos, CQRS y edge-first siguen igual.

---

## 1. Stack y decisiones fundamentales

| Dimensión | Decisión | ADR |
|---|---|---|
| Lenguaje/runtime | **.NET 8 (C#)**, LTS | ADR-T1 |
| Estilo de servicio | Microservicios, **Clean Architecture** por servicio | ADR-T2 |
| Base de datos | **PostgreSQL en Neon** (serverless), **un proyecto Neon por tenant** | ADR-T3 |
| Persistencia | **EF Core + Npgsql**; migraciones EF versionadas | ADR-T3 |
| Mensajería async | **Amazon MSK (Kafka Serverless)** detrás de **MassTransit** | ADR-T4 |
| APIs | **REST/OpenAPI** en el borde · **gRPC** interno sync · **eventos** async | ADR-T5 |
| Identidad | **Duende IdentityServer** (OIDC/OAuth2, federación por tenant, MFA) | ADR-T6 |
| Nube | **AWS** (EKS, MSK, S3, Secrets Manager, ECR) | ADR-T7 |
| Repositorio | **Monorepo** (una solución .NET + paquetes compartidos) | ADR-T8 |
| Observabilidad | **OpenTelemetry** (trazas/métricas/logs), correlación por `tenant_id` | ADR-T9 |
| Modelo conceptual de dominio | **4 capas** como **vista conceptual**; los servicios siguen siendo los *bounded contexts* | ADR-T10 |
| Dependencia del ERP | **Opcional**: modo *standalone* con **master data propia** (`Nexo.MasterData`) | ADR-T11 |
| Perfiles de trabajo en el MVP | **Ambos**: lote (repetitivo) y proyecto, sobre el mismo agregado `Run` | ADR-T12 |
| Precedencias de tareas | **DAG completo** desde el MVP (no secuencia lineal) | ADR-T13 |

---

## 2. Estructura del monorepo (.NET)

Un único repositorio con una solución .NET, servicios independientes desplegables y paquetes compartidos
(`BuildingBlocks`). Cada servicio es su propio *bounded context* y su propio contenedor.

```text
/nexo
├── nexo.sln
├── src/
│   ├── BuildingBlocks/                 # Paquetes compartidos (NuGet internos)
│   │   ├── Nexo.BuildingBlocks.Domain          # Entity, AggregateRoot, ValueObject, DomainEvent, Result
│   │   ├── Nexo.BuildingBlocks.Application      # CQRS (MediatR), behaviors, validación
│   │   ├── Nexo.BuildingBlocks.Messaging        # Abstracción de eventos (MassTransit), Outbox
│   │   ├── Nexo.BuildingBlocks.MultiTenancy     # Resolución de tenant, ITenantContext, connection factory
│   │   ├── Nexo.BuildingBlocks.Observability    # OpenTelemetry, logging, correlación
│   │   └── Nexo.BuildingBlocks.Web              # Middlewares, manejo de errores, auth handlers
│   ├── ControlPlane/                   # Servicios del plano global (DB global)
│   │   ├── Nexo.Tenancy                         # Provisioning, Tenant Connection Registry
│   │   ├── Nexo.Admin                           # Licencias, feature flags, facturación
│   │   └── Nexo.Identity                        # Host de Duende IdentityServer
│   ├── Services/                       # Servicios por-tenant (DB del tenant)
│   │   ├── Nexo.MasterData            # Catálogos propios: UoM, ítems (producto/insumo), personas,
│   │   │                              #   centros de costo, clientes/pedidos, jerarquía física + importador CSV
│   │   ├── Nexo.Ingestion
│   │   ├── Nexo.Devices
│   │   ├── Nexo.WorkModel             # Capa 2: Proceso/Tarea/Insumo versionados, DAG de precedencias
│   │   ├── Nexo.Execution             # Capa 3: Ejecución (Run) — perfiles lote y proyecto
│   │   ├── Nexo.Production            # Perfil repetitivo: órdenes, corridas, OEE/takt
│   │   ├── Nexo.Quality
│   │   ├── Nexo.Scrap
│   │   ├── Nexo.Downtime
│   │   ├── Nexo.Traceability
│   │   ├── Nexo.Connectors            # Conector Odoo (ACL) — OPCIONAL: ningún flujo depende de él
│   │   ├── Nexo.Dashboards            # Read models / CQRS de lectura
│   │   └── Nexo.Notifications
│   └── Gateways/
│       └── Nexo.ApiGateway            # BFF / reverse proxy (YARP) para el frontend
├── edge/                              # Agente Edge (.NET, se empaqueta aparte) — ver 05
├── deploy/                            # Helm charts / manifests EKS, IaC (Terraform)
└── tests/                             # Unit + integration (Testcontainers) + contract tests
```

### 2.1 Estructura interna de cada servicio (Clean Architecture)

```text
Nexo.Production/
├── Api/              # ASP.NET Core: endpoints REST (OpenAPI), gRPC, consumers de eventos, DI
├── Application/      # Casos de uso (MediatR handlers), comandos/queries, validadores, puertos
├── Domain/           # Agregados, entidades, value objects, eventos de dominio, invariantes
└── Infrastructure/   # EF Core (DbContext del tenant), repos, publicación de eventos, gRPC clients
```

- **CQRS con MediatR**: comandos y queries separados; *pipeline behaviors* para validación (FluentValidation), logging, transacciones y `tenant scope`.
- **Dominio puro**: sin dependencias de infraestructura; emite **eventos de dominio**; los invariantes viven en los agregados.
- **Mapeo**: Mapster (DTO ↔ dominio). **Resiliencia**: Polly (reintentos, circuit breaker) en clientes salientes.
- **Validación de entrada**: FluentValidation en la capa Application.

### 2.2 Mapeo capas ↔ servicios / bounded contexts

Las **4 capas** de [`layered-architecture.md`](../specs/specs/layered-architecture.md) son una **vista
conceptual del dominio**, no una descomposición física (**ADR-T10**). Traducción operativa para este repositorio:

> **Regla dura:** no existe —ni va a existir— un proyecto `Nexo.Capa1`, un namespace `Layer3` ni una carpeta
> por capa. La unidad de despliegue sigue siendo el **bounded context**. Una capa puede estar servida por
> **varios** servicios y un servicio puede participar de **varias** capas.

| Capa conceptual | Servicios .NET que la materializan | Rol técnico | Datos |
|---|---|---|---|
| **1 · Física — Gemelo digital** | `Nexo.MasterData` (jerarquía Empresa→Planta→Sector→Línea→Activo) · `Nexo.Devices` · `Nexo.Ingestion` (borde de captura) · S3/Files | Declarar qué existe, atar señal ↔ activo (*binding* obligatorio) y capturar | DB del tenant · tablas particionadas por tiempo (DT-01) · S3 |
| **2 · Modelo de trabajo** | `Nexo.WorkModel` (Proceso/Tarea/Insumo, versionado, **DAG**) · `Nexo.MasterData` (ítems, UoM, personas) · `Nexo.Quality` (criterios) · `Nexo.Identity` (roles) | Plantilla versionada y reutilizable; **nunca** guarda estado de ejecución | DB del tenant |
| **3 · Ejecución** | `Nexo.Execution` (agregado `Run`: lote \| proyecto) · `Nexo.Production` (perfil repetitivo) · `Nexo.Scrap` · `Nexo.Downtime` · `Nexo.Quality` | Instanciar una versión de Proceso, asignar activos/personas, registrar avance y consumo | DB del tenant + Outbox |
| **4 · Motor de eventos** | `Nexo.Ingestion` (normalización al evento canónico) · `Nexo.Traceability` (event store) · `Nexo.Dashboards` (read models/CQRS) · `Nexo.Notifications` | Observar (solo lectura) y derivar métricas: progreso, cuellos de botella, tiempos muertos, costo | Event store append-only · read models |
| **Lateral · ERP (OPCIONAL)** | `Nexo.Connectors` (ACL, adapter Odoo) | Sincronización bidireccional de contexto de negocio; **habilitable, no estructural** | Config + `connector_xref` en DB del tenant · catálogo en Control Plane |
| **Transversales** | `Nexo.Tenancy` · `Nexo.Admin` · `Nexo.Identity` · `Nexo.ApiGateway` · `BuildingBlocks` | Plano global y borde; atraviesan todas las capas | DB Global (Control Plane) |

```mermaid
flowchart TB
    subgraph L1["Capa 1 · Física (gemelo digital)"]
        MD["Nexo.MasterData"]
        DEV["Nexo.Devices"]
        ING["Nexo.Ingestion (captura)"]
    end
    subgraph L2["Capa 2 · Modelo de trabajo"]
        WM["Nexo.WorkModel (DAG)"]
    end
    subgraph L3["Capa 3 · Ejecución"]
        EXE["Nexo.Execution (Run: lote o proyecto)"]
        PRD["Nexo.Production · Scrap · Downtime · Quality"]
    end
    subgraph L4["Capa 4 · Motor de eventos"]
        TRC["Nexo.Traceability (event store)"]
        DSH["Nexo.Dashboards (read models)"]
    end

    BUS["Backbone de eventos (MSK / MassTransit)"]
    CONN["Nexo.Connectors — ERP OPCIONAL"]

    L1 --> L2 --> L3
    L1 --> BUS
    L3 --> BUS
    BUS --> L4
    L4 -.->|"métricas derivadas (dato, no estado)"| L3
    CONN <-.->|"sincronización opcional"| MD
    CONN <-.->|"sincronización opcional"| L3
```

**Reglas técnicas que se derivan del principio de dependencia:**

| Regla | Traducción a este repositorio |
|---|---|
| **Dependencia descendente** | `Nexo.WorkModel` puede referenciar tipos de activo/UoM de `Nexo.MasterData`; `Nexo.MasterData` **no** referencia procesos ni ejecuciones |
| **Sin dependencia ascendente** | Ningún servicio de Capa 1 consume contratos gRPC de Capa 2/3/4; solo **publica** eventos |
| **Plantilla ≠ instancia** | `Nexo.Execution` **congela** la versión del Proceso (`process_version_id`) al arrancar el `Run`; `Nexo.WorkModel` nunca guarda tiempos reales |
| **Observación de solo lectura** | Los servicios de Capa 4 consumen del backbone y proyectan read models; **no** escriben en las DB de las otras capas |
| **Dato con dueño físico** | Todo evento lleva referencia a Activo (`asset_id`) resuelta en ingesta; sin binding, cuarentena (ver [02-event-model.md](./02-event-model.md)) |
| **ERP fuera del camino crítico** | Ningún servicio del Core referencia `Nexo.Connectors`. La comunicación es **solo por eventos**; si el conector no existe, nada se degrada |

---

## 3. Topología en AWS

```mermaid
flowchart TB
    subgraph Edge["Planta (on-premise)"]
        AG["Agente Edge (.NET)\ndatalogger/CSV (MVP)"]
    end

    subgraph AWS["AWS (región primaria)"]
        subgraph Net["VPC"]
            ALB["ALB Ingress + WAF"]
            subgraph EKS["Amazon EKS"]
                GW["Nexo.ApiGateway (YARP/BFF)"]
                IDP["Nexo.Identity (Duende)"]
                CP["Control Plane\n(Tenancy · Admin)"]
                SVC["Servicios por-tenant\n(MasterData, WorkModel, Execution,\nIngestion, Production, …)"]
                CONN["Nexo.Connectors\n(ACL — OPCIONAL)"]
            end
            MSK["Amazon MSK\n(Kafka Serverless)"]
        end
        SM["AWS Secrets Manager"]
        S3["S3 (Files/Media)"]
        ECR["ECR (imágenes)"]
        OBS["Observabilidad\n(OTel Collector → CloudWatch/Grafana)"]
    end

    subgraph NeonCloud["Neon (Serverless Postgres, en AWS)"]
        GDB[("DB Global\n(Control Plane)")]
        TDB[("1 proyecto Neon\npor tenant")]
    end

    ERP["ERP del cliente\n(Odoo / SAP / Dynamics)\nLATERAL y OPCIONAL"]

    AG -->|"HTTPS outbound + mTLS"| ALB
    ALB --> GW --> SVC
    ALB --> IDP
    GW --> CP
    SVC <-->|"eventos"| MSK
    CP <-->|"eventos"| MSK
    CONN <-->|"eventos"| MSK
    SVC -->|"SQL (TLS/PrivateLink)"| TDB
    CONN -->|"SQL"| TDB
    CP -->|"SQL"| GDB
    SVC -.->|"referencias de secreto"| SM
    CONN -.->|"referencias de secreto"| SM
    SVC --> S3
    EKS --> OBS
    CONN <-.->|"sincronización opcional (ACL)\nsi el tenant lo habilita"| ERP
```

> **El ERP queda fuera del camino crítico.** `Nexo.Connectors` se despliega y se habilita **por tenant**; en
> **modo standalone** simplemente no hay instancia activa y la topología funciona completa (los catálogos los
> sirve `Nexo.MasterData`). Ninguna flecha del diagrama que sostiene captura, ejecución o tableros pasa por el
> ERP (**ADR-T11**; detalle en [06-odoo-connector.md](./06-odoo-connector.md)).

**Mapeo de servicios gestionados:**

| Necesidad | AWS / Servicio | Nota |
|---|---|---|
| Orquestación | **Amazon EKS** (Kubernetes) | Diseño portable; Helm por servicio |
| Mensajería | **Amazon MSK Serverless** (Kafka) | Auth IAM, dentro de la VPC |
| Base de datos | **Neon** (Postgres serverless, corre en AWS) | Conexión vía TLS público o **PrivateLink** en prod; proyecto por tenant |
| Secretos | **AWS Secrets Manager** | Cadenas de conexión Neon y credenciales ERP/canales (solo referencias en el Registry) |
| Archivos/evidencias | **Amazon S3** | Prefijo/bucket por tenant (aislamiento) |
| Imágenes | **Amazon ECR** | Build en CI |
| CDN/borde web | **CloudFront** | Frontend estático + caché |
| Observabilidad | **OTel Collector** → CloudWatch/Grafana | Trazas/métricas/logs correlacionados |

> **Nota Neon + AWS:** Neon es Postgres serverless que corre sobre AWS; se integra en la misma región y admite
> **AWS PrivateLink** para tráfico privado. El *scale-to-zero* por proyecto hace económico tener **un proyecto por
> tenant** aunque la mayoría esté ociosa.

---

## 4. Comunicación entre servicios

| Tipo | Cuándo | Tecnología |
|---|---|---|
| **REST / OpenAPI** | Borde: frontend, integraciones externas, webhooks | ASP.NET Core Minimal APIs + Swashbuckle (OpenAPI) |
| **gRPC** | Llamadas internas **sync** de baja latencia (p. ej. Ingestion→Devices para resolver contexto) | gRPC + contratos `.proto` versionados |
| **Eventos (async)** | Columna vertebral: propagación de hechos entre dominios (CQRS, integraciones) | **MassTransit** sobre **MSK/Kafka** |

- **Borde unificado** por `Nexo.ApiGateway` (YARP): enrutamiento, agregación BFF para dashboards, *rate limiting*, terminación de auth.
- **Versionado**: REST por URL (`/v1`), gRPC por paquete `.proto`, eventos por versión en el envelope (ver [02-event-model.md](./02-event-model.md)).
- **Contrato de evento canónico**: envelope común (`event_id`, `tenant_id`, `type`, `occurred_at`, `payload`, `dedup_key`, `origin_metadata`) — detalle en [02](./02-event-model.md).

### 4.1 Fiabilidad de mensajería
- **Transactional Outbox** (tabla `outbox` en la DB del tenant) para publicar eventos de forma atómica con el cambio de estado.
- **Idempotencia** en consumidores vía `dedup_key`/`event_id` (tabla `inbox`/`processed_events`).
- **Orden** por clave de partición = `tenant_id` (+ `aggregate_id` cuando importa el orden intra-agregado).
- **Reproceso**: retención en MSK + posibilidad de re-consumir desde offset (útil para reconstruir read models).

---

## 5. Multi-tenancy (resumen técnico)

- **Aislamiento por proyecto Neon por tenant** (cómputo + datos aislados). La **DB Global (Control Plane)** es un proyecto Neon aparte.
- **Resolución de tenant** por request: host/subdominio o claim `tenant_id` del JWT → **Tenant Connection Registry** (en Control Plane) → **cadena de conexión** (referencia en Secrets Manager) → `DbContext` del tenant.
- `ITenantContext` (scoped) transporta `tenant_id` por todo el pipeline (MediatR, EF, mensajería, logs).
- **Migraciones** versionadas (EF Core), aplicadas **por cohortes con feature flags**, objetivo zero-downtime, estado por tenant.
- Diseño detallado y el **connection schema** en [01-multi-tenancy-connection.md](./01-multi-tenancy-connection.md).

---

## 6. Identidad y acceso (Duende IdentityServer)

- **`Nexo.Identity`** hostea **Duende IdentityServer** (OIDC/OAuth2). Emite **JWT** con claims `sub`, `tenant_id`, `roles`, `scopes`.
- **Multi-tenant**: federación por tenant (OIDC/SAML externo) + cuentas locales (pymes); resolución de tenant por dominio/realm lógico.
- **MFA obligatoria** para roles con escritura sensible/administración y todos los roles globales; operario en kiosco con **PIN/badge/NFC** + dispositivo confiable; step-up para acciones críticas.
- **AuthZ**: RBAC con *scoping* por planta/línea (claims + políticas ASP.NET Core); validación de `scopes` por endpoint. Detalle en [07-security.md](./07-security.md).

---

## 7. Aspectos transversales

| Aspecto | Decisión |
|---|---|
| **Config** | `appsettings` + variables de entorno + Secrets Manager (nada de secretos en repo) |
| **Secretos** | AWS Secrets Manager; el Connection Registry guarda **solo referencias**; rotación programada |
| **Logging** | Serilog estructurado → OTel; **siempre** con `tenant_id` y `correlation_id` |
| **Trazas/métricas** | OpenTelemetry (auto-instrumentación ASP.NET/EF/Kafka) → Collector |
| **Health** | `/health/live` y `/health/ready` por servicio; sonda de conectividad a Neon/MSK |
| **Resiliencia** | Polly (retry, timeout, circuit breaker) en clientes salientes y consumers |
| **Errores** | `ProblemDetails` (RFC 7807) en REST; códigos de error de dominio estables |
| **Tests** | xUnit + FluentAssertions; integración con **Testcontainers** (Postgres/Kafka); contract tests de eventos |
| **CI/CD** | GitHub Actions → build/test → imagen a **ECR** → deploy a **EKS** (Helm); migraciones como job por cohorte |

---

## 8. Entornos

| Entorno | Cómputo | Datos |
|---|---|---|
| **dev** | EKS dev / local (Docker Compose) | Neon **branch** efímero por feature (branching de Neon) |
| **staging** | EKS staging | Proyecto(s) Neon de staging con tenants de prueba |
| **prod** | EKS prod (multi-AZ) | Proyecto Neon por tenant + DB Global; PrivateLink |

> El **branching de Neon** permite clonar datos de forma instantánea para entornos de prueba/preview sin copiar TB.

---

## 9. Registro de decisiones (ADRs)

| ADR | Decisión | Motivo | Consecuencia |
|---|---|---|---|
| ADR-T1 | .NET 8 (C#) | Stack elegido; LTS, rendimiento, ecosistema | Equipo .NET; drivers industriales (S7/OPC UA) a evaluar en V1 |
| ADR-T2 | Clean Architecture + CQRS (MediatR) | Aísla dominio, testeable, escala en equipo | Más *boilerplate* inicial |
| ADR-T3 | Postgres/Neon, proyecto por tenant, EF Core | Aislamiento fuerte + scale-to-zero + provisioning por API | Muchos proyectos Neon a gestionar; sin TimescaleDB (ver §10) |
| ADR-T4 | MSK/Kafka Serverless tras MassTransit | Objetivo de escala (orden/retención/reproceso), abstracción para no acoplar | Costo/ops de MSK; alternativa AWS-native descartada por reproceso |
| ADR-T5 | REST/OpenAPI + gRPC interno + eventos | Contratos claros por caso; async como columna vertebral | Tres estilos a gobernar |
| ADR-T6 | Duende IdentityServer | Nativo .NET, control total, federación por tenant y MFA | Licencia comercial al crecer (Community gratis al inicio) |
| ADR-T7 | AWS (EKS, MSK, S3, Secrets Manager) | Nube elegida; managed maduro | Acoplamiento gestionado tras abstracciones portables |
| ADR-T8 | Monorepo (.NET) | Refactors atómicos, tooling único, equipo chico | Requiere disciplina de límites entre servicios |
| ADR-T9 | OpenTelemetry | Estándar, portable, correlación por tenant | Tuning de muestreo a escala |
| **ADR-T10** | **Las 4 capas son una vista conceptual, no una descomposición de servicios** (2026-07-13) | Ordena el razonamiento de dominio y la documentación sin tocar la arquitectura: la unidad de despliegue y de propiedad del dato sigue siendo el *bounded context* | Prohibido crear proyectos/namespaces "Capa N"; el mapeo capa ↔ servicio se documenta en §2.2 y se mantiene al agregar servicios. Un servicio puede participar de varias capas |
| **ADR-T11** | **ERP opcional + master data propia** (`Nexo.MasterData`) (2026-07-13) | El sistema debe operar *standalone*: si no hay ERP, la plataforma tiene que poseer sus catálogos (UoM, ítems, insumos, procesos, personas, centros de costo, clientes/pedidos mínimos) | **Agranda el alcance del MVP** (costo explícito): ABM + importador CSV con validación/simulación, estados de gobierno por catálogo, conciliación al conectar un ERP y bandeja de conflictos. `Nexo.Connectors` deja de ser estructural y pasa a habilitable ⇒ **reencuadre de INT-01**. **Costo → V1** (tarifas/valorización), fuera del mínimo del MVP |
| **ADR-T12** | **El MVP soporta ambos perfiles** (lote/repetitivo y proyecto) (2026-07-13) | Un solo modelo de trabajo cubre producción repetitiva y obra/fabricación a medida; duplicar dominio después sería más caro que soportarlo desde el inicio | `Nexo.Execution` expone **un agregado `Run`** con discriminador de perfil (esqueleto común: estado, tareas instanciadas, consumo, avance, evidencia). KPIs divergentes: OEE/takt solo en repetitivo; % de avance, desvío y ruta crítica en proyecto ⇒ **dos familias de read models** en `Nexo.Dashboards` |
| **ADR-T13** | **DAG completo de precedencias desde el MVP** (2026-07-13) | La secuencia lineal no modela trabajo real (tareas paralelas, convergencias) y hace incalculables cuello de botella y ruta crítica | `Nexo.WorkModel` persiste precedencias como grafo dirigido con **validación de aciclicidad** en el agregado; `Nexo.Execution` resuelve tareas habilitadas por cierre de predecesoras. Mayor costo de UI (editor de grafo) y de cálculo de avance ponderado |

---

## 10. Decisiones técnicas pendientes (a resolver a medida que avanzamos)

| # | Pregunta | Contexto | Default provisional |
|---|---|---|---|
| DT-01 | **Store de series de tiempo** para lecturas de alto volumen (V1) | Neon **no** soporta TimescaleDB | MVP: tablas Postgres **particionadas por tiempo** en la DB del tenant. V1: evaluar Timestream/ClickHouse/Influx u offload a S3+Athena |
| DT-02 | **Serialización de eventos** y schema registry | Kafka/MSK | JSON + **JSON Schema** en un registry; evaluar Avro/Protobuf si el volumen lo exige |
| DT-03 | **API Gateway**: YARP en EKS vs. AWS API Gateway | Borde REST | YARP/BFF en EKS (control y BFF de dashboards); reevaluar para exposición pública |
| DT-04 | **Límites de proyectos Neon** por cuenta/organización a miles de tenants | Escala del modelo proyecto-por-tenant | Confirmar cuotas/plan enterprise Neon; estrategia de *sharding* por organización Neon |
| DT-05 | **Frontend / app de tablet** (offline-first) | .NET elegido en backend | A definir al diseñar la UI (Blazor vs. React PWA vs. .NET MAUI) — ver cuando lleguemos a la capa de presentación |
| DT-06 | **Runtime del Agente Edge** | Debe correr en PC industrial/Raspberry | .NET (compartir lenguaje); confirmar footprint en hardware objetivo — ver [05-edge-agent.md](./05-edge-agent.md) |
| DT-07 | **Frontera `Nexo.MasterData` ↔ `Nexo.Production` / `Nexo.Devices`** para la jerarquía física (Planta/Sector/Línea/Activo) | ADR-T11 crea el servicio de catálogos; hoy la jerarquía está repartida (MOD-19 del [tablero](../specs/open-questions-board.md)) | `Nexo.MasterData` es el **dueño canónico** de la jerarquía y la publica por eventos; Devices posee hardware/señales y Production consume referencias |
| DT-08 | **¿`Nexo.Production` se absorbe en `Nexo.Execution`?** | Con ADR-T12 el `Run` es el agregado raíz y Production queda como **perfil repetitivo** | Mantener servicios separados en el MVP (Production = KPIs y órdenes del perfil repetitivo); reevaluar la fusión si el solapamiento de agregados crece |
| DT-09 | **Mínimo viable de `Nexo.MasterData` en el MVP** y alcance del importador CSV | Es el costo de alcance de ADR-T11 ([master-data.md §7.3](../specs/specs/master-data.md)) | UoM, ítems (producto/insumo), personas y jerarquía física; clientes/pedidos solo si el piloto es perfil proyecto; **costo/tarifas → V1** |

> Estas preguntas se responden **a medida que el diseño las necesita**. Al resolverse, se promueven a ADR.
