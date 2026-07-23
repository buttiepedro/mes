# 07 · Seguridad — AuthN/Z, Secretos, Edge y Aislamiento — Nexo (MVP)

> **Documento:** `design/07-security.md` · **Estado:** Borrador v0.1 · **Actualizado:** 2026-07-11
> **Roles:** Software Architect · Tech Lead
> **Relacionados:** [00-tech-baseline.md](./00-tech-baseline.md) · [01-multi-tenancy-connection.md](./01-multi-tenancy-connection.md) · [02-event-model.md](./02-event-model.md) · [05-edge-agent.md](./05-edge-agent.md) · [08-observability-ops.md](./08-observability-ops.md) · [../specs/specs/security.md](../specs/specs/security.md) · [../specs/specs/users-permissions.md](../specs/specs/users-permissions.md) · [../specs/specs/multi-tenancy.md](../specs/specs/multi-tenancy.md) · [../specs/specs/control-plane.md](../specs/specs/control-plane.md) · [../specs/specs/devices.md](../specs/specs/devices.md)

## Resumen ejecutivo

Este documento traduce a **diseño técnico** los requisitos funcionales de [`security.md`](../specs/specs/security.md) y
[`users-permissions.md`](../specs/specs/users-permissions.md), respetando el [baseline técnico](./00-tech-baseline.md).
Define **cómo se autentican y autorizan** los usuarios humanos, los operarios en kiosco, las cuentas de servicio y los
agentes edge; **cómo se gestionan secretos y cifrado**; **cómo se asegura el edge** con mTLS y tokens rotables; y **cómo
se hace cumplir técnicamente el aislamiento entre tenants**, con un modelo de amenazas y auditoría.

Los pilares del diseño son:

1. **AuthN con Duende IdentityServer** (`Nexo.Identity`): OIDC/OAuth2 con federación por tenant (OIDC/SAML), cuentas
   locales para pymes, **MFA obligatoria** para roles sensibles y globales, autenticación de piso (PIN/badge/NFC + device
   trust) para el operario en kiosco, y **step-up** para acciones críticas.
2. **AuthZ como cadena de tres filtros** — `tenant` → `RBAC` → `scoping (planta/línea)` con extensión `ABAC` — implementada
   con **políticas y `AuthorizationHandler` de ASP.NET Core**, alimentada por los claims del JWT.
3. **Secretos en AWS Secrets Manager**: el [Tenant Connection Registry](./01-multi-tenancy-connection.md) guarda **solo
   referencias** (ARNs), nunca valores; rotación programada y bajo demanda; resolución en el contexto de tenant correcto.
4. **Cifrado extremo a extremo**: TLS 1.2+ en tránsito, **mTLS** en el borde y en tráfico servicio↔servicio sensible,
   cifrado en reposo (Neon, S3, backups) con KMS.
5. **Edge zero-trust**: identidad por dispositivo, mTLS + JWT de agente rotable, provisioning (zero-touch opcional),
   revocación por dispositivo sin afectar al resto del tenant.
6. **Aislamiento por diseño**: DB-per-tenant (proyecto Neon por tenant) + validación de `tenant_id` en cada capa
   (gateway, pipeline MediatR, EF Core, MSK, S3), defensa en profundidad.

El alcance es **diseño**: diagramas, tablas de políticas/scopes y **ejemplos ilustrativos** de claims y políticas
ASP.NET Core. La implementación completa vive en el código de `Nexo.Identity` y `Nexo.BuildingBlocks.Web`.

---

## 1. Modelo de identidad y topología de AuthN

### 1.1 Planos de identidad

Coherente con [users-permissions.md §2](../specs/specs/users-permissions.md) y [§4](../specs/specs/control-plane.md), hay
**dos planos de identidad que nunca se mezclan**:

| Plano | Realm en Duende | Emisor (`iss`) | Token | Directorio |
|---|---|---|---|---|
| **Tenant** | `tenant/{slug}` (realm lógico) | `https://id.nexo.io` con claim `tenant_id` | JWT con `tenant_id` | IdP federado del cliente **o** cuentas locales de Nexo |
| **Control Plane** | `global` | `https://id.nexo.io` sin `tenant_id` (o `tenant_id` destino solo en break-glass) | JWT global | Directorio del proveedor (SSO propio) |

> Duende IdentityServer es **single-issuer, multi-tenant lógico**: un único host `Nexo.Identity` sirve todos los realms;
> la separación es por **cliente OIDC**, **esquema de identidad** y **claim `tenant_id`**, no por instancia. Ver §1.4.

### 1.2 Topología de componentes

```mermaid
flowchart TB
    subgraph Client["Clientes"]
        WEB["Frontend web / BFF"]
        TAB["Tablet kiosco (operario)"]
        EDGE["Agente Edge (planta)"]
        SVC["Cuentas de servicio\n(conectores/automatización)"]
    end

    subgraph AWS["AWS · EKS"]
        ALB["ALB + WAF\n(TLS termination)"]
        GW["Nexo.ApiGateway (YARP/BFF)\nvalida JWT + tenant"]
        IDP["Nexo.Identity\n(Duende IdentityServer)"]
        API["Servicios por-tenant\n(RBAC + scoping)"]
    end

    subgraph Ext["IdP externos por tenant"]
        OIDC["OIDC (Azure AD / Okta…)"]
        SAML["SAML 2.0 (ADFS…)"]
    end

    SM["AWS Secrets Manager\n(signing keys, client secrets)"]
    GDB[("Control Plane DB\nusuarios locales, MFA, device trust")]

    WEB -->|"OIDC Auth Code + PKCE"| IDP
    TAB -->|"PIN/badge/NFC + device cert"| IDP
    SVC -->|"OAuth2 Client Credentials"| IDP
    EDGE -->|"mTLS + Client Credentials"| IDP
    IDP -.->|"federación"| OIDC
    IDP -.->|"federación"| SAML
    IDP --> GDB
    IDP -.->|"signing key ref"| SM
    WEB & TAB & SVC & EDGE -->|"HTTPS + Bearer JWT"| ALB --> GW --> API
    GW -.->|"JWKS (validar firma)"| IDP
```

### 1.3 Flujos OIDC/OAuth2 por tipo de cliente

| Cliente | Flujo OAuth2/OIDC | Motivo | Tokens |
|---|---|---|---|
| **Frontend web / BFF** | **Authorization Code + PKCE** (client público/confidencial vía BFF) | Estándar seguro para SPA/BFF; sin secreto en el browser | `id_token` + `access_token` (JWT) + `refresh_token` (rotativo, en BFF) |
| **Tablet en kiosco** | Authorization Code + PKCE con **factor de piso** (PIN/badge/NFC) sobre **device trust** | Sesión larga de dispositivo enrolado; login rápido de operario | `access_token` de vida corta + `refresh_token` ligado al device |
| **Cuenta de servicio (conector)** | **Client Credentials** | Identidad no humana, sin usuario interactivo | `access_token` (JWT) con `scopes` acotados; sin `refresh_token` |
| **Agente Edge** | **Client Credentials sobre mTLS** (client cert-bound token, RFC 8705) | Identidad por dispositivo, certificado mutuo | `access_token` bound al cert (`cnf.x5t#S256`) |
| **Integración externa (webhook entrante)** | Client Credentials o API key gestionada | M2M de terceros | `access_token` con `scope` mínimo |

> **No usamos** *Resource Owner Password Credentials* (deprecado en OAuth 2.1). El login local de pymes se hace vía la UI
> de login de Duende (Authorization Code), nunca enviando contraseña directo a un endpoint de token.

**Ejemplo de flujo Authorization Code + PKCE (web):**

```mermaid
sequenceDiagram
    autonumber
    participant U as Usuario
    participant B as BFF (ApiGateway)
    participant I as Nexo.Identity (Duende)
    participant IdP as IdP del tenant (opcional)
    participant A as Servicio por-tenant

    U->>B: GET /login?tenant=acme
    B->>I: /authorize (code_challenge, tenant hint)
    I->>IdP: (si federado) redirect OIDC/SAML
    IdP-->>I: assertion / id_token
    I->>I: resolver tenant_id, roles, scopes, plant scope
    I->>I: ¿MFA requerida? -> desafío / step-up
    I-->>B: authorization_code
    B->>I: /token (code + code_verifier + client secret)
    I-->>B: id_token + access_token (JWT) + refresh_token
    B->>A: request + Bearer access_token
    A->>A: validar firma (JWKS), tenant_id, roles, scopes
    A-->>B: 200 (o 403 si scoping/ABAC falla)
```

### 1.4 Federación SSO por tenant (OIDC / SAML)

Cada tenant enterprise puede federar su IdP corporativo. La configuración vive en el **Control Plane** (por tenant) y
Duende la carga dinámicamente:

- **Resolución de tenant en login:** por **subdominio/host** (`acme.nexo.io`) o selección explícita → fija el realm y el
  `tenant_id` **antes** de cualquier decisión (P2 de [users-permissions.md](../specs/specs/users-permissions.md)).
- **OIDC externo:** Duende actúa como *relying party* del IdP del cliente (Azure AD, Okta, Google Workspace…).
- **SAML 2.0:** vía plugin SAML de Duende / componente `Rsk.Saml`; el cliente es el IdP, Nexo el SP.
- **Just-in-Time provisioning:** al primer login federado se crea el usuario con **rol mínimo y sin alcance**; el
  Administrador del tenant lo eleva (mínimo privilegio, P1).
- **Mapeo de claims externos → claims Nexo:** un perfil por tenant traduce grupos/roles del IdP a **roles Nexo** y
  **scopes de planta/línea** (ver Decisión pendiente DS-06 sobre mapeo de grupos).
- **Offboarding:** la baja en el directorio corporativo corta el acceso (sin sesión renovable); refuerza [ciclo de vida
  §8.3](../specs/specs/users-permissions.md).

```mermaid
flowchart LR
    A["acme.nexo.io/login"] --> B{Tenant resuelto\npor host}
    B --> C{¿IdP federado\nconfigurado?}
    C -- "OIDC" --> D["Redirect a IdP OIDC del tenant"]
    C -- "SAML" --> E["Redirect a IdP SAML del tenant"]
    C -- "No (pyme)" --> F["Login local Duende\n(password + MFA)"]
    D & E --> G["Mapear claims externos\n→ roles + scopes Nexo"]
    F --> G
    G --> H["Emitir JWT con tenant_id,\nroles, scopes, plant_scope"]
```

### 1.5 Cuentas locales (pymes)

Para tenants sin IdP, Duende gestiona cuentas locales con:

- **Política de contraseñas robusta** (longitud mínima, verificación contra listas de comprometidas, hashing con
  **ASP.NET Identity / PBKDF2** o Argon2 vía extensión).
- **MFA obligatoria** para roles sensibles (§2).
- **Bloqueo por intentos** y recuperación con códigos de un solo uso.
- Gobierno por el **Administrador del tenant** (alta/baja, reseteo de MFA), todo auditado.

---

## 2. MFA, kiosco y step-up

### 2.1 Matriz de obligatoriedad de MFA

Deriva de [users-permissions.md §6.2](../specs/specs/users-permissions.md) y [security.md §2.1](../specs/specs/security.md):

| Rol | Plano | MFA | Segundo factor típico |
|---|---|---|---|
| Super Administrador, Soporte, Implementador, Partner | Global | **Obligatoria** | TOTP / WebAuthn (FIDO2) |
| Administrador (tenant), Integraciones | Tenant | **Obligatoria** | TOTP / WebAuthn |
| Producción, Calidad, Mantenimiento, Supervisor | Tenant | **Obligatoria** | TOTP / WebAuthn / push |
| **Operario (kiosco)** | Tenant | **Factor de piso** | PIN/badge/NFC + **device trust** (no teléfono) |
| Gerencia (solo lectura) | Tenant | **Obligatoria** (recomendada) | TOTP / WebAuthn |
| Cuentas de servicio / Edge | Tenant | **No interactiva** | Credencial/clave rotable + mTLS (edge) |

### 2.2 Operario en kiosco (PIN / badge / NFC + device trust)

El operario opera una **tablet compartida en modo kiosco** a pie de línea. Exigir TOTP en teléfono personal rompería el
flujo (guantes, ritmo de producción). El diseño:

1. **Enrolamiento del dispositivo (device trust):** la tablet se enrola como *trusted device* → recibe un **certificado
   de dispositivo** (mTLS) y un `device_id`. El enrolamiento lo hace el Administrador/Supervisor y queda auditado.
2. **Sesión de dispositivo larga + sesión de operario corta:** el dispositivo mantiene una sesión confiable; el operario
   se autentica con **PIN corto**, **badge** o **NFC** para "asumir" una sesión de usuario efímera sobre ese device.
3. **El factor de piso vale solo sobre device confiable:** un PIN/badge sin device enrolado **no** autentica (el device
   es el segundo factor de facto).
4. **Revocación remota:** una tablet perdida se revoca (certificado + device trust) sin cambiar credenciales del turno.
5. **Alcance mínimo:** el operario tiene el scope más chico (P1), lo que acota el riesgo residual.

```mermaid
sequenceDiagram
    autonumber
    participant T as Tablet (device enrolado)
    participant O as Operario
    participant I as Nexo.Identity (Duende)
    T->>I: mTLS handshake (device cert) → sesión de dispositivo
    O->>T: PIN / badge / NFC
    T->>I: /connect/token (device-bound) + factor de piso
    I->>I: validar device trust + factor + tenant + línea
    I-->>T: access_token corto (operario, scope de línea)
    Note over T,I: Refresh ligado al device; revocable por device_id
```

### 2.3 Step-up authentication

Acciones críticas exigen **reautenticación/segundo factor en el momento**, aunque la sesión esté activa
([users-permissions.md §6.2](../specs/specs/users-permissions.md)):

- Ejecutar **OTA/firmware en activo crítico** (ver [devices.md](../specs/specs/devices.md)).
- **Cambio de mapeo ERP** en producción.
- **Break-glass** de Soporte/Super Admin.
- **Cambios masivos de rol/alcance**.

**Diseño técnico:** el `access_token` porta el claim **`acr`** (Authentication Context Class Reference) y **`amr`**
(métodos usados) y un **`auth_time`**. Los endpoints críticos exigen `acr` de nivel elevado y `auth_time` reciente; si no
se cumple, responden **`403` con `WWW-Authenticate: insufficient_user_authentication`** y el cliente dispara un **step-up**
(nuevo `/authorize` con `acr_values=mfa` y `max_age=0`).

```csharp
// Ilustrativo — política de step-up para acciones críticas
options.AddPolicy("StepUpMfa", policy =>
    policy.RequireAssertion(ctx =>
    {
        var acr = ctx.User.FindFirst("acr")?.Value ?? "";
        var authTime = ctx.User.FindFirst("auth_time")?.Value;
        var fresh = long.TryParse(authTime, out var t)
                    && DateTimeOffset.FromUnixTimeSeconds(t) > DateTimeOffset.UtcNow.AddMinutes(-5);
        return acr.Contains("mfa") && fresh; // MFA + reautenticación reciente
    }));
```

---

## 3. Estructura de claims del JWT

El `access_token` emitido por Duende es un **JWT firmado (RS256/ES256)** cuya firma se valida contra el **JWKS** de
`Nexo.Identity`. Estructura ilustrativa para un usuario de tenant:

```json
{
  "iss": "https://id.nexo.io",
  "aud": ["nexo.api", "nexo.production", "nexo.devices"],
  "sub": "u_9f2c...",
  "tenant_id": "acme",
  "roles": ["Supervisor", "Calidad"],
  "scopes": ["production:write", "quality:disposition", "devices:read"],
  "plant_scope": [
    { "site": "plant-norte", "lines": ["L1", "L2"] },
    { "site": "plant-sur",   "lines": ["L3"] }
  ],
  "acr": "mfa",
  "amr": ["pwd", "otp"],
  "auth_time": 1752230400,
  "device_id": null,
  "sid": "sess_7a1b...",
  "exp": 1752234000,
  "iat": 1752230400,
  "jti": "tok_5e8d..."
}
```

**Claims de servicio / edge** (Client Credentials):

```json
{
  "iss": "https://id.nexo.io",
  "aud": ["nexo.ingestion"],
  "sub": "svc_edge_plant-norte-gw01",
  "tenant_id": "acme",
  "client_type": "edge_agent",
  "device_id": "dev_gw01",
  "scopes": ["ingestion:write"],
  "cnf": { "x5t#S256": "b64u-cert-thumbprint" },
  "plant_scope": [{ "site": "plant-norte", "lines": ["L1"] }],
  "exp": 1752232200
}
```

| Claim | Uso técnico | Notas |
|---|---|---|
| `tenant_id` | Resolución de tenant + validación contra host/subdominio | **Nunca** ausente en tokens de tenant; base de todo el aislamiento |
| `roles` | Entrada de RBAC (policies `RequireRole`/handlers) | Roles canónicos de [users-permissions.md §3](../specs/specs/users-permissions.md) |
| `scopes` | Autorización fina por endpoint (verbo/módulo) | Ver matriz §5 |
| `plant_scope` | **Scoping** planta/línea/máquina (claim custom compacto) | Se compara contra el recurso; ver §4.3 |
| `acr` / `amr` / `auth_time` | Step-up y política de MFA | §2.3 |
| `device_id` / `cnf` | Kiosco y edge (token cert-bound, RFC 8705) | Revocación por dispositivo |
| `sid` / `jti` | Revocación de sesión / token; correlación en auditoría | `jti` en logs de auditoría |

> **`plant_scope` como claim vs. lookup:** para tenants con jerarquías grandes, un `plant_scope` extenso infla el token.
> Diseño: si el número de asignaciones supera un umbral, el token porta una **referencia** (`scope_ref`) y el servicio
> resuelve el árbol de alcance desde la DB del tenant (cacheado). Ver Decisión pendiente **DS-02**.

**Ciclo de vida de tokens y revocación:**

- `access_token`: **vida corta** (5–15 min). Se valida solo por firma + claims (stateless) en el camino caliente.
- `refresh_token`: **rotativo** (one-time use), en el BFF (web) o ligado al device (kiosco); reuso detectado ⇒ revoca la
  familia.
- **Revocación:** lista de `sid`/`jti` revocados publicada por `Nexo.Identity` (endpoint + evento MSK
  `identity.session.revoked`); los servicios consultan una **cache de revocación** para acciones sensibles. El cierre de
  sesión forzado ante incidente revoca por `sub`, `sid` o `device_id`.

---

## 4. Autorización (AuthZ): RBAC + scoping + ABAC

### 4.1 Cadena de decisión (tres filtros)

Implementa la cadena de [users-permissions.md §2.2](../specs/specs/users-permissions.md) con el pipeline de ASP.NET Core:

```mermaid
flowchart LR
    A["Request + JWT"] --> M0["Middleware: validar firma + aud + exp"]
    M0 --> M1{"Tenant resuelto y activo?\n(tenant_id == host)"}
    M1 -- No --> X["403 / 401"]
    M1 -- Sí --> M2{"RBAC: rol concede\nacción sobre módulo?\n(scope en JWT)"}
    M2 -- No --> X
    M2 -- Sí --> M3{"Scoping: plant_scope cubre\nsite/line/machine del recurso?"}
    M3 -- No --> X
    M3 -- Sí --> M4{"ABAC: turno/propiedad/\nventana/estado/criticidad?"}
    M4 -- No --> X
    M4 -- Sí --> Y["Permitir + auditar"]
```

**Por qué en este orden:** primero lo barato e infalible (tenant), luego lo estructural (rol/scope/alcance), y por último
lo contextual y costoso (ABAC que puede requerir leer estado del recurso). Protege rendimiento a escala.

### 4.2 Requisitos y handlers de ASP.NET Core (ilustrativos)

**Validación del bearer y tenant** (en `Nexo.BuildingBlocks.Web`):

```csharp
// Program.cs — validación de JWT emitido por Duende
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.Authority = "https://id.nexo.io";      // descubre JWKS
        o.MapInboundClaims = false;               // conservar nombres de claim originales
        o.TokenValidationParameters = new()
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidAudiences = new[] { "nexo.api", "nexo.production", "nexo.devices" },
            ValidateLifetime = true,
            RoleClaimType = "roles",
            NameClaimType = "sub"
        };
    });

// Middleware: coherencia tenant_id (claim) == tenant del host/subdominio
app.UseMiddleware<TenantConsistencyMiddleware>();  // 403 si no coinciden (P2)
```

**Requisito de scope + scoping planta/línea:**

```csharp
// Requisito compuesto
public sealed record ScopedRequirement(string RequiredScope) : IAuthorizationRequirement;

public sealed class ScopedHandler : AuthorizationHandler<ScopedRequirement, IScopedResource>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext ctx, ScopedRequirement req, IScopedResource resource)
    {
        // 1) RBAC/scope: el token debe portar el scope requerido
        var scopes = ctx.User.FindAll("scopes").Select(c => c.Value);
        if (!scopes.Contains(req.RequiredScope)) return Task.CompletedTask; // deny-by-default (P7)

        // 2) Scoping: plant_scope del token cubre el site/line del recurso
        var plantScope = PlantScope.Parse(ctx.User.FindAll("plant_scope"));
        if (plantScope.Covers(resource.Site, resource.Line, resource.Machine))
            ctx.Succeed(req);

        return Task.CompletedTask; // si no cubre → permanece denegado
    }
}
```

**Registro de políticas por scope:**

```csharp
builder.Services.AddAuthorization(o =>
{
    o.AddPolicy("Production.Write", p => p.AddRequirements(new ScopedRequirement("production:write")));
    o.AddPolicy("Quality.Disposition", p => p.AddRequirements(new ScopedRequirement("quality:disposition")));
    o.AddPolicy("Devices.Ota", p => p.Requirements.Add(new ScopedRequirement("devices:ota")));
    // step-up encadenado para acciones críticas
    o.AddPolicy("Devices.Ota.Critical", p =>
    {
        p.AddRequirements(new ScopedRequirement("devices:ota"));
        p.RequireAssertion(StepUp.FreshMfa); // §2.3
    });
});
```

**Uso en endpoint (resource-based authorization para el scoping):**

```csharp
app.MapPost("/v1/production/records", async (
        CreateProductionRecord cmd, IAuthorizationService authz, ClaimsPrincipal user, IMediator mediator) =>
{
    var resource = new ScopedResource(cmd.Site, cmd.Line, cmd.Machine);
    var result = await authz.AuthorizeAsync(user, resource, "Production.Write");
    if (!result.Succeeded) return Results.Forbid();          // 403 + auditar
    return Results.Ok(await mediator.Send(cmd));             // ABAC se evalúa en el handler MediatR
})
.RequireAuthorization();
```

**ABAC en el pipeline MediatR (behavior):** las condiciones que requieren leer estado del recurso (ventana de edición,
propiedad, turno activo, estado sincronizado, criticidad) se evalúan en un `IPipelineBehavior` de
`Nexo.BuildingBlocks.Application`, ya con el `DbContext` del tenant resuelto:

```csharp
public sealed class AbacBehavior<TReq, TRes> : IPipelineBehavior<TReq, TRes>
    where TReq : IAbacGuarded
{
    public async Task<TRes> Handle(TReq req, RequestHandlerDelegate<TRes> next, CancellationToken ct)
    {
        foreach (var rule in req.AbacRules)                 // p.ej. EditWindow, Ownership, ShiftOpen, NotSynced
            if (!await rule.IsSatisfiedAsync(_tenantCtx, _db, ct))
                throw new AbacDeniedException(rule.Reason);  // → 403 + auditoría con razón
        return await next();
    }
}
```

### 4.3 Modelo del `plant_scope` y jerarquía

```mermaid
flowchart TD
    T["Tenant (acme)"] --> S1["Site: plant-norte"]
    T --> S2["Site: plant-sur"]
    S1 --> A1["Sector: envasado"]
    A1 --> L1["Line: L1"] --> M1["Machine: M-101"]
    A1 --> L2["Line: L2"]
    S2 --> L3["Line: L3"]
```

- El `plant_scope` es un **conjunto de subárboles**: `{ site, lines[] , machines[]? }`. `Covers()` implementa cobertura
  jerárquica (un scope de `site` cubre sus líneas; un scope de `tenant` — p.ej. Gerencia/Administrador — cubre todo).
- **Gerencia/Administrador:** `plant_scope: [{ "site": "*" }]` (tenant completo, lectura o CRUD según rol).
- **Un usuario, varias asignaciones:** el token concatena todos los subárboles de los role bindings del usuario.

### 4.4 Matriz scope ↔ endpoint (extracto)

Traducción de la [Matriz de permisos §5.2](../specs/specs/users-permissions.md) a **scopes OAuth** y endpoints REST del
[contrato de servicios](./04-service-contracts.md). `★` = sujeto a scoping planta/línea; `†` = ABAC.

| Módulo | Endpoint (ej.) | Scope requerido | Roles que lo portan | Notas |
|---|---|---|---|---|
| Producción | `POST /v1/production/records` | `production:write` ★† | Operario†, Supervisor, Producción | Operario: estado borrador + propiedad + turno (ABAC) |
| Producción | `POST /v1/production/records/{id}:confirm` | `production:confirm` ★ | Supervisor, Producción | SoD: quien confirma ≠ quien capturó |
| Calidad | `POST /v1/quality/dispositions` | `quality:disposition` ★ | **Solo Calidad** | Acción exclusiva (P4) |
| Scrap | `POST /v1/scrap` | `scrap:write` ★† | Operario†, Supervisor, Calidad(clasif.) | — |
| Paradas | `POST /v1/downtime/{id}:close` | `downtime:close` ★ | Mantenimiento, Supervisor | — |
| Dispositivos | `POST /v1/devices/{id}:ota` | `devices:ota` ★† | Mantenimiento | **Step-up** si activo crítico (†) |
| Integraciones | `POST /v1/connectors/{id}:retry-sync` | `connectors:sync` | Integraciones, Administrador | — |
| Trazabilidad | `GET /v1/traceability/**` | `traceability:read` ★ | Todos (según alcance) | Inmutable, solo lectura (P6) |
| Auditoría | `GET /v1/audit/**` | `audit:read` ★ | Admin, Supervisor★, … | Nadie `U/D` (P6) |
| Usuarios (tenant) | `POST /v1/users` | `identity:admin` | **Solo Administrador** | — |
| Config (sites/líneas) | `PUT /v1/config/lines/{id}` | `config:write` ★ | Administrador (+delegación) | — |

**Control Plane (roles globales):** scopes con prefijo `cp:` (`cp:tenants:admin`, `cp:licensing`, `cp:observability`,
`cp:break-glass`). Emitidos en tokens **sin `tenant_id`**; el acceso a dato operativo requiere **break-glass** (§7).

### 4.5 Cuentas de servicio

- **Identidad no humana** (Client Credentials): conectores ERP y automatizaciones autentican con `client_id`/secreto
  rotable; **sin MFA interactiva**. Rol **Integraciones** acotado, `scopes` mínimos.
- El **secreto de cliente** vive en Secrets Manager (§5); rotación programada. La cuenta se **audita igual que un humano**
  (`sub = svc_...`).
- **Nunca** reutilizan credenciales de un administrador humano (P1). El token porta `tenant_id` y `scopes` reducidos.
- **Edge:** ver §6 (mTLS + token cert-bound por dispositivo).

---

## 5. Secretos y cifrado

### 5.1 Gestión de secretos (AWS Secrets Manager)

Coherente con [00 §7](./00-tech-baseline.md), [01](./01-multi-tenancy-connection.md) y
[security.md §4.2](../specs/specs/security.md): **el Connection Registry y toda configuración guardan solo referencias
(ARNs); los valores viven en Secrets Manager.**

```mermaid
flowchart LR
    subgraph CP["Control Plane"]
        REG[("Tenant Connection Registry\nsolo ARNs / referencias")]
    end
    subgraph SM["AWS Secrets Manager"]
        S1["nexo/tenant/acme/neon-conn"]
        S2["nexo/tenant/acme/s3-creds"]
        S3["nexo/tenant/acme/odoo-creds"]
        S4["nexo/identity/signing-key"]
        S5["nexo/tenant/acme/edge/gw01-token"]
    end
    SVC["Servicio por-tenant\n(IRSA / rol IAM)"]
    SVC -->|"1. resolver tenant"| REG
    REG -->|"2. ARN"| SVC
    SVC -->|"3. GetSecretValue (IAM scoped)"| S1
    SM -.->|"rotación Lambda"| S1 & S3 & S5
```

| Secreto | Ruta (convención) | Rotación | Consumidor |
|---|---|---|---|
| Cadena de conexión Neon (tenant) | `nexo/tenant/{id}/neon-conn` | Programada + on-demand | Servicios por-tenant (EF Core) |
| Credenciales S3 (prefijo tenant) | `nexo/tenant/{id}/s3-creds` | Programada | Files/Media |
| Credenciales ERP (Odoo) | `nexo/tenant/{id}/odoo-creds` | Programada + incidente | `Nexo.Connectors` |
| Canales de notificación | `nexo/tenant/{id}/notify/*` | Programada | `Nexo.Notifications` |
| Signing key de Duende | `nexo/identity/signing-key` | Rotación con solape (JWKS) | `Nexo.Identity` |
| Client secrets (svc/edge) | `nexo/tenant/{id}/edge/{dev}-token` | Rotación por dispositivo | Edge / conectores |

**Reglas de diseño:**

- **Acceso IAM por servicio con IRSA** (IAM Roles for Service Accounts en EKS): cada pod asume un rol con política
  `secretsmanager:GetSecretValue` acotada por **ruta/tag de tenant**; un servicio no puede leer secretos de otro tenant.
- **Resolución bajo demanda y cache corta** (TTL) en memoria; **nunca** se persiste el valor ni se loguea (redacción en
  Serilog). El **uso queda trazado** (auditoría + CloudTrail).
- **Rotación:** Lambda de rotación de Secrets Manager; para Neon, coordina con la [rotación de credenciales del proyecto
  Neon](./01-multi-tenancy-connection.md). El JWKS de Duende publica **dos claves** durante el solape (rollover sin corte).
- **Revocación inmediata** ante compromiso: nueva versión del secreto + invalidación de cache (evento
  `identity.secret.rotated`).

### 5.2 Cifrado en tránsito

| Tramo | Protocolo | Notas |
|---|---|---|
| Cliente ↔ ALB | **TLS 1.2+** (cert ACM) | WAF + terminación TLS en ALB |
| ALB ↔ servicios (in-mesh) | TLS / mTLS | mTLS servicio↔servicio sensible (Identity, Tenancy) — ver DS-03 (mesh) |
| **Edge ↔ nube** | **mTLS** (outbound) | Cert por dispositivo; §6 |
| Servicio ↔ Neon | TLS (público) / **PrivateLink** en prod | `sslmode=require`/`verify-full` |
| Servicio ↔ MSK | TLS + **IAM auth** | Dentro de la VPC |
| Servicio ↔ S3 | TLS | Endpoints VPC (gateway) |
| Servicio ↔ ERP (Odoo) | TLS | Credenciales por tenant |

> **Sin canales en claro** en ningún tramo ([security.md §4.1](../specs/specs/security.md)).

### 5.3 Cifrado en reposo y gestión de claves

| Dato | Cifrado | Clave |
|---|---|---|
| DB de tenant (Neon) | Cifrado en reposo del proyecto Neon | Gestionado (Neon/AWS) |
| Control Plane DB | Cifrado en reposo | KMS |
| S3 (evidencias/media) | SSE-KMS, **por prefijo/bucket de tenant** | **CMK por tenant** (aislamiento; habilita crypto-shredding) |
| Backups (Neon/S3) | Cifrados | KMS |
| Secrets Manager | Cifrado con KMS | CMK dedicada |
| Buffer store-and-forward (edge) | Cifrado local | Clave del dispositivo |

- **KMS** gestiona el ciclo de vida (rotación anual de CMK, políticas de clave por servicio/tenant).
- **CMK por tenant** en S3 habilita **crypto-shredding**: destruir la clave inutiliza los datos (borrado seguro al fin de
  la retención; ver [control-plane.md](../specs/specs/control-plane.md) y Preguntas abiertas de la spec).
- **BYOK** para enterprise queda como **DS-05** (Decisión pendiente).

---

## 6. Seguridad del edge

Deriva de [security.md §5](../specs/specs/security.md) y [devices.md](../specs/specs/devices.md); el diseño del agente vive
en [05-edge-agent.md](./05-edge-agent.md). Principio: **el edge inicia siempre la conexión (outbound), nunca expone puertos
entrantes en planta**, y cada dispositivo es una identidad propia.

### 6.1 Identidad por dispositivo (mTLS + token rotable)

- Cada gateway/dispositivo tiene un **certificado X.509 propio** (identidad) emitido por una **CA privada de Nexo**
  (AWS Private CA), asociado a `tenant_id` + `site` + `device_id`.
- El edge autentica con **mTLS** y obtiene un **`access_token` cert-bound** (RFC 8705, claim `cnf.x5t#S256`): el token solo
  vale presentado sobre la misma conexión mTLS ⇒ un token robado sin el cert es inútil.
- **Token de agente rotable** de vida corta; el certificado es el ancla de confianza de larga vida (rotación periódica).

### 6.2 Provisioning (zero-touch opcional)

```mermaid
sequenceDiagram
    autonumber
    participant Adm as Admin/Implementador
    participant CP as Control Plane (Devices/Tenancy)
    participant Dev as Dispositivo/Gateway
    participant CA as AWS Private CA
    participant I as Nexo.Identity

    Adm->>CP: registrar device (tenant, site, line)
    CP-->>Adm: bootstrap token (un solo uso, corto)
    Note over Dev: instalación en planta
    Dev->>CP: presenta bootstrap token (o attestation zero-touch)
    CP->>CA: CSR firmado → cert de dispositivo
    CA-->>Dev: certificado X.509 (identidad)
    Dev->>I: mTLS + Client Credentials → access_token cert-bound
    I-->>Dev: token (scope ingestion:write, tenant_id, device_id)
```

- **Bootstrap token de un solo uso** (o **attestation** para zero-touch: TPM/secure element del hardware industrial).
- El alta asocia el dispositivo a **un único tenant** y su planta/línea; sus datos se enrutan **solo** a la DB de ese
  tenant (particiones MSK por `tenant_id`).

### 6.3 Rotación y revocación

- **Rotación** de certificados/tokens programada; el token de agente rota sin intervención (refresh sobre mTLS).
- **Revocación por dispositivo:** revocar el certificado (CRL/OCSP de la Private CA) + invalidar tokens por `device_id`
  ⇒ el dispositivo comprometido queda fuera **sin afectar al resto del tenant** ni cambiar credenciales del turno.
- **Store-and-forward seguro:** buffer local **cifrado** ante cortes; reenvío con **deduplicación** por `dedup_key` del
  [Evento canónico](./02-event-model.md) (sin pérdida ni duplicado). Backlog observado en [08](./08-observability-ops.md).
- **Firmware/OTA firmado y verificado** con rollback: detalle en [devices.md](../specs/specs/devices.md); la ejecución
  requiere `devices:ota` + **step-up** si el activo es crítico (§2.3, §4.4).

---

## 7. Aislamiento entre tenants (enforcement técnico)

El aislamiento es una **propiedad de la topología** (DB-per-tenant / proyecto Neon por tenant) **más** controles de
aplicación en cada capa: defensa en profundidad, un solo fallo no expone otro tenant.

```mermaid
flowchart TB
    R["Request + JWT(tenant_id=acme)"] --> G["Gateway/Middleware\ntenant_id == host? (P2)"]
    G --> P["Pipeline MediatR\nITenantContext(scoped)=acme"]
    P --> DB["EF Core: DbContext resuelto\n→ proyecto Neon de acme"]
    P --> BUS["MSK: key=tenant_id\npartición/topic por tenant"]
    P --> ST["S3: prefijo/bucket de acme\ncreds scoped + CMK acme"]
    P --> SEC["Secrets: IAM scoped por tag tenant"]
    style DB fill:#0b7285,color:#fff
    style ST fill:#0b7285,color:#fff
```

| Capa | Enforcement técnico | Falla contenida |
|---|---|---|
| **Borde** | `TenantConsistencyMiddleware`: `tenant_id` (claim) debe igualar host/subdominio → 403 | Token de un tenant usado en otro host |
| **Aplicación** | `ITenantContext` (scoped) propaga `tenant_id`; behaviors bloquean queries sin tenant | Bug de filtro / falta de `WHERE tenant` (no aplica: DB separada) |
| **Datos** | **Proyecto Neon por tenant**: la cadena de conexión resuelta apunta a **otra base física** | Consulta jamás cruza al dato de otro tenant |
| **Mensajería** | Key de partición = `tenant_id`; consumers validan `tenant_id` del envelope | Fuga entre streams / noisy neighbor |
| **Storage** | Prefijo/bucket por tenant + credenciales de alcance limitado + **CMK por tenant** | Bucket cruzado |
| **Secretos** | IAM (IRSA) acotado por ruta/tag de tenant | Lectura de secreto ajeno |
| **Control Plane** | Sin `tenant_id`; nunca dato operativo; break-glass auditado | Cuenta global no ve dato del cliente por defecto |

**Break-glass (acceso de emergencia):** Soporte/Super Admin acceden a dato de un tenant solo con **justificación escrita,
consentimiento/registro del tenant, duración limitada, caducidad automática y notificación** al Administrador
([users-permissions.md §9](../specs/specs/users-permissions.md)). Técnicamente: token global con `tenant_id` **destino
explícito** + scope `cp:break-glass` + **step-up**; toda la sesión se audita en la **auditoría global** y la del tenant.

---

## 8. Auditoría

Coherente con [security.md §6](../specs/specs/security.md) y [users-permissions.md §9](../specs/specs/users-permissions.md):

| Ámbito | Dónde | Qué registra | Inmutabilidad |
|---|---|---|---|
| **Auditoría por tenant** | Servicio **Audit**, DB del tenant | quién/qué/cuándo/sobre qué; login, cambios de rol, edición fuera de ventana, disposiciones, OTA | **Inmutable** (P6): solo append; correcciones por evento compensatorio |
| **Auditoría global** | Control Plane | alta/baja/suspensión de tenants, licencias, accesos de Soporte/break-glass | Inmutable |

- **Atribución de identidad** (contra repudio, T11): cada registro lleva `sub`, `tenant_id`, `roles`, `scope`, `jti`,
  `correlation_id`, resultado y **razón** (en denegaciones ABAC).
- **Correlación** con [08-observability-ops.md](./08-observability-ops.md): los logs de auditoría comparten
  `tenant_id`/`correlation_id` pero **no** se mezclan entre clientes.
- **Inmutabilidad técnica:** tabla append-only + (opcional) sellado por hash encadenado; alineado con el Event Store de
  [traceability.md](../specs/specs/traceability.md).

---

## 9. Modelo de amenazas (amenaza / mitigación)

Extiende el modelo de [security.md §8](../specs/specs/security.md) con el **enforcement técnico** de este diseño.
Referencia STRIDE entre paréntesis.

| # | Amenaza (STRIDE) | Vector | Mitigación técnica en el diseño |
|---|---|---|---|
| T1 | Fuga entre tenants (Information Disclosure) | Bug de filtro, escalada | **DB-per-tenant (Neon)** + `TenantConsistencyMiddleware` + `ITenantContext` + IAM/S3/MSK por tenant (§7) |
| T2 | Robo de credenciales de conexión (I.D.) | Secreto en token/log/código | **Secrets Manager** (solo ARNs en Registry), IRSA scoped, redacción en logs, rotación (§5.1) |
| T3 | Acceso no autorizado a cuentas (Spoofing) | Phishing, credenciales robadas | **MFA** obligatoria + SSO federado + `access_token` corto + `refresh` rotativo con detección de reuso (§2, §3) |
| T4 | Abuso del Control Plane (Elevation of Privilege) | Cuenta global comprometida | Separación de planos, **break-glass** con step-up y auditoría, MFA WebAuthn, mínimo privilegio (§7) |
| T5 | Dispositivo edge comprometido (Spoofing/Tampering) | Firmware alterado, cert robado | **mTLS + token cert-bound**, provisioning con identidad única, **revocación por device**, OTA firmado (§6) |
| T6 | Interceptación / MITM (I.D.) | Tráfico en claro | **TLS 1.2+ extremo a extremo + mTLS** en edge y servicio↔servicio sensible (§5.2) |
| T7 | Manipulación del historial (Tampering/Repudiation) | Alterar eventos/auditoría | Auditoría/Event Store **inmutables (append-only)** + `dedup_key`; corrección por compensación (§8, P6) |
| T8 | DoS / noisy neighbor (Denial of Service) | Picos de eventos, tenant abusivo | Particiones MSK por tenant + backpressure + autoscaling + **proyecto Neon aislado** + quotas de licencia (§7, [08](./08-observability-ops.md)) |
| T9 | Inyección vía integraciones/ERP (Tampering) | Datos maliciosos externos | **ACL** en `Nexo.Connectors` + validación/normalización + credenciales por tenant + cuenta de servicio acotada (§4.5) |
| T10 | Pérdida de datos (Denial of Service) | Fallo infra / error humano | Backup/restore por tenant, cifrado de backups, recuperación granular ([08](./08-observability-ops.md)) |
| T11 | Repudio de acciones (Repudiation) | Usuario niega actuar | Auditoría con **atribución** (`sub`,`jti`,`acr`) por tenant y global (§8) |
| T12 | Storage mal configurado (I.D.) | Bucket/URL accesible | Prefijo/bucket por tenant + **CMK por tenant** + URLs prefirmadas temporales (§5.3) |
| T13 | Token robado / replay (Spoofing) | Bearer interceptado o filtrado | Vida corta, **cert-bound** (edge/kiosco), `aud` estricta, revocación por `sid`/`jti`/`device_id` (§3) |
| T14 | Confusión de tenant (Elevation of Privilege) | Token de tenant A hacia recurso de tenant B | Coherencia `tenant_id`==host + validación de coherencia usuario/recurso/tenant antes de autorizar (§4, §7) |
| T15 | Escalada de privilegios ABAC (E.o.P.) | Editar fuera de ventana/propiedad | **AbacBehavior** en MediatR con reglas de ventana/propiedad/turno/estado/criticidad (§4.2) |

---

## 10. Decisiones pendientes

| # | Pregunta | Contexto | Default provisional |
|---|---|---|---|
| DS-01 | **Estándares de cumplimiento objetivo** (SOC 2, ISO 27001, GDPR-like, es-AR/industrial) y fase del roadmap | Pregunta abierta 1 de [security.md](../specs/specs/security.md) | MVP: buenas prácticas + auditoría inmutable; certificación en V1 |
| DS-02 | **`plant_scope` en token vs. lookup por referencia** para jerarquías grandes | Tamaño del JWT a escala | Claim compacto; `scope_ref` + cache si supera umbral (§3) |
| DS-03 | **Service mesh / mTLS interno** (Istio/Linkerd vs. mTLS puntual) | mTLS servicio↔servicio (§5.2) | mTLS puntual en Identity/Tenancy; evaluar mesh en V1 |
| DS-04 | **WebAuthn/FIDO2 vs. TOTP** como segundo factor por defecto | UX vs. resistencia a phishing | Soportar ambos; recomendar WebAuthn para roles globales |
| DS-05 | **BYOK / crypto-shredding** para enterprise | Pregunta abierta 4 de [security.md](../specs/specs/security.md) | Plataforma gestiona KMS; CMK por tenant en S3; BYOK en V1 |
| DS-06 | **Mapeo de grupos IdP → roles/scopes Nexo** | Federación (§1.4), pregunta abierta 6 de [users-permissions.md](../specs/specs/users-permissions.md) | Perfil de mapeo por tenant; JIT con rol mínimo |
| DS-07 | **Break-glass y residencia de datos** (Soporte global vs. regionalizado) | Pregunta abierta 7 de [users-permissions.md](../specs/specs/users-permissions.md) | Break-glass global auditado; regionalizar según residencia en V1 |
| DS-08 | **Roles personalizados por tenant** (composición de scopes) | Pregunta abierta 1 de [users-permissions.md](../specs/specs/users-permissions.md) | Catálogo cerrado de 8 roles en MVP; custom en fase avanzada |
| DS-09 | **Plan de respuesta a incidentes / notificación de brechas** | Pregunta abierta 7 de [security.md](../specs/specs/security.md) | Runbook base en [08](./08-observability-ops.md); formalizar en V1 |

---

## 11. Relación con otros documentos

- **[00-tech-baseline.md](./00-tech-baseline.md):** stack, Duende, Secrets Manager, OTel, EKS, MSK, Neon.
- **[01-multi-tenancy-connection.md](./01-multi-tenancy-connection.md):** Connection Registry (referencias), resolución de
  tenant, provisioning Neon.
- **[02-event-model.md](./02-event-model.md):** envelope, `dedup_key`, particiones por `tenant_id`.
- **[05-edge-agent.md](./05-edge-agent.md):** agente edge, store-and-forward, OTA.
- **[08-observability-ops.md](./08-observability-ops.md):** auditoría/observabilidad correlacionadas, health del edge, DR.
- **[../specs/specs/security.md](../specs/specs/security.md)** y **[../specs/specs/users-permissions.md](../specs/specs/users-permissions.md):** requisitos funcionales que este diseño implementa.
- **[../specs/specs/multi-tenancy.md](../specs/specs/multi-tenancy.md)** · **[../specs/specs/control-plane.md](../specs/specs/control-plane.md)** · **[../specs/specs/devices.md](../specs/specs/devices.md).**
