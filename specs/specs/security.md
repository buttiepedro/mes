# Seguridad y aislamiento

> **Documento:** `specs/specs/security.md` · **Estado:** Borrador v0.1 · **Actualizado:** 2026-07-11
> **Roles:** Product Manager · Software Architect · UX Designer
> **Relacionados:** [users-permissions.md](./users-permissions.md) · [multi-tenancy.md](./multi-tenancy.md) · [devices.md](./devices.md) · [traceability.md](./traceability.md) · [control-plane.md](./control-plane.md) · [integrations.md](./integrations.md) · [architecture.md](./architecture.md) · [glossary.md](./glossary.md)

## Resumen ejecutivo

La seguridad de **Nexo** se construye sobre un **principio fundamental: el aislamiento total entre empresas cliente**. Como la plataforma adopta **base de datos por tenant (DB-per-tenant)** de forma no negociable (ver [multi-tenancy.md](./multi-tenancy.md)), el aislamiento no es una capa añadida sino una propiedad de la topología: una empresa nunca puede acceder a datos, storage, secretos ni red de otra. Este documento define cómo se protege ese aislamiento y cómo se asegura toda la plataforma —desde el edge industrial hasta el Control Plane del proveedor— en un contexto de datos de producción sensibles y exigencias de trazabilidad.

El documento cubre siete frentes: (1) **autenticación y autorización** (tokens con claim de tenant, SSO, MFA); (2) **aislamiento entre tenants** en datos, storage, secretos y red; (3) **cifrado** en tránsito y en reposo, y **gestión de secretos** y credenciales de conexión; (4) **seguridad del edge y dispositivos** (aprovisionamiento, certificados, firmware/OTA); (5) **auditoría y trazabilidad** de acciones; (6) **cumplimiento** (protección de datos, retención, requisitos GDPR-like e industriales); y (7) un **modelo de amenazas resumido** en formato amenaza/mitigación.

La seguridad es transversal y se apoya en varios microservicios canónicos: **Identity & Access** (AuthN/AuthZ), **Audit** (auditoría por tenant y global), **Devices** (seguridad del edge), **Observability** y **Tenant Provisioning** (gestión de secretos/registro de conexión). El detalle de roles y matriz de permisos vive en [users-permissions.md](./users-permissions.md); la trazabilidad del dato de negocio, en [traceability.md](./traceability.md); y la frontera de datos entre tenant y proveedor, en [multi-tenancy.md](./multi-tenancy.md) y [control-plane.md](./control-plane.md).

---

## 1. Principio fundamental: aislamiento total

El aislamiento total es la piedra angular de la seguridad de Nexo y se materializa en cuatro planos, todos obligatorios (alineado con [multi-tenancy.md](./multi-tenancy.md)):

| Plano | Garantía | Cómo se logra |
|---|---|---|
| **Datos** | Ninguna consulta cruza el límite del tenant. | DB-per-tenant; conexión resuelta por tenant antes de tocar dato. |
| **Storage** | Las evidencias/archivos de un tenant no son accesibles por otro. | Bucket/prefijo por tenant, con credenciales/scopes propios. |
| **Secretos** | Las credenciales de un tenant no sirven ni se ven desde otro. | Secretos separados por tenant en gestor central; nunca en tokens ni logs. |
| **Red / cómputo** | El tráfico y el procesamiento de un tenant no interfieren con otro. | Segmentación de red, contexto de tenant explícito, colas/particiones por tenant. |

> Defensa en profundidad: el aislamiento físico (DB separada) **más** los controles de aplicación (claim de tenant, RBAC/ABAC) hacen que un único fallo no exponga datos de otro cliente.

---

## 2. Autenticación y autorización (AuthN / AuthZ)

Gestionadas por **Identity & Access** (servicio compartido; ver [architecture.md](./architecture.md)). La matriz de permisos operativa está en [users-permissions.md](./users-permissions.md).

### 2.1 Autenticación (AuthN)

- **Tokens con claim de tenant:** toda sesión autenticada emite un token que incluye el claim `tenant_id`. Ese claim es la base de la resolución de tenant (ver [multi-tenancy.md](./multi-tenancy.md), sección 5) y se valida contra el host/subdominio.
- **SSO:** soporte de inicio de sesión único (identidad corporativa del cliente) como capacidad, típicamente feature enterprise gestionada por licencia (ver [control-plane.md](./control-plane.md)).
- **MFA:** autenticación multifactor, **obligatoria** para roles sensibles —usuarios globales del Control Plane (Super Administrador, Soporte) y administradores de tenant— y ofrecible al resto.
- **Sesiones:** expiración, renovación y revocación controladas; posibilidad de cierre de sesión forzado ante incidentes.

### 2.2 Autorización (AuthZ)

- **Modelo RBAC** con alcance por planta/línea (scoping) y extensiones **ABAC** donde aplique, según el brief y [users-permissions.md](./users-permissions.md).
- **Separación de planos:** los **roles globales** (Super Administrador, Soporte, Implementador, Partner) operan solo en el Control Plane; los **roles operativos** (Operario, Supervisor, Calidad, Producción, Mantenimiento, Gerencia, Administrador del tenant, Integraciones) operan dentro de su tenant.
- **Mínimo privilegio:** cada rol recibe solo los permisos que necesita; las acciones sensibles requieren MFA y quedan auditadas.
- **Validación de coherencia:** cada request valida que el usuario, su claim de tenant y el recurso pertenezcan al mismo tenant antes de autorizar.

---

## 3. Aislamiento entre tenants (detalle)

- **Datos:** cada empresa tiene su DB; no hay tabla compartida con dato operativo. La resolución de tenant precede a cualquier acceso (ver [multi-tenancy.md](./multi-tenancy.md)).
- **Storage:** archivos, fotos y evidencias (Files / Media) se guardan en bucket/prefijo exclusivo del tenant, con acceso mediante URLs/credenciales de alcance limitado y temporal.
- **Secretos:** las cadenas de conexión, claves de storage y credenciales de integración de cada tenant se guardan por separado y se resuelven solo bajo el contexto correcto (ver sección 4).
- **Red / cómputo:** segmentación de red entre planos (edge ↔ nube, servicios internos, Control Plane) y procesamiento con contexto de tenant explícito; las colas/particiones del broker se segmentan por tenant para evitar fugas y "noisy neighbor".
- **Control Plane:** concentra metadatos del proveedor y **nunca** dato operativo del cliente (ver [control-plane.md](./control-plane.md)); su acceso está más restringido que el de cualquier plano de tenant.

---

## 4. Cifrado y gestión de secretos

### 4.1 Cifrado

| Ámbito | Requisito |
|---|---|
| **En tránsito** | Todo el tráfico cifrado (cliente↔nube, edge↔nube outbound, servicio↔servicio, hacia ERPs). Sin canales en claro. |
| **En reposo** | Cifrado de las DB de tenant, del Control Plane, del object storage y de los backups. |
| **Datos sensibles** | Protección reforzada de credenciales, secretos y datos personales; nunca en texto plano en logs. |
| **Claves** | Gestión del ciclo de vida de claves (rotación, revocación) mediante un gestor central. |

### 4.2 Gestión de secretos y credenciales de conexión

- Las **credenciales de conexión de cada tenant** se almacenan en un **gestor de secretos** central y se referencian desde el **Tenant Connection Registry** (Control Plane) — nunca se persisten en el token, ni en el código, ni en los logs (ver [multi-tenancy.md](./multi-tenancy.md), sección 5, y [control-plane.md](./control-plane.md)).
- Los secretos se resuelven **bajo demanda** y solo en el contexto de tenant correcto; su uso queda trazado.
- **Rotación** periódica y ante incidentes; revocación inmediata si un secreto se compromete.
- Las credenciales de **integración con ERPs** (Odoo y otros) se gestionan por tenant con el mismo estándar (ver [integrations.md](./integrations.md)).

---

## 5. Seguridad del edge y de los dispositivos

El mundo físico de la planta (PLCs, dataloggers, gateways, sensores) es un frente crítico. El **Agente Edge / Gateway** vive on-premise y se conecta **outbound** hacia la nube, con **store-and-forward** ante cortes (ver [architecture.md](./architecture.md) y [devices.md](./devices.md)).

| Aspecto | Medida de seguridad |
|---|---|
| **Aprovisionamiento** | Alta segura de cada dispositivo/gateway con identidad única, asociada a un tenant y a su planta/línea. |
| **Certificados / identidad** | Cada dispositivo/gateway se autentica con credenciales/certificados propios; comunicación mutuamente autenticada y cifrada. |
| **Conexión outbound** | El edge inicia la conexión hacia la nube (no se exponen puertos entrantes en planta); reduce superficie de ataque. |
| **Firmware / OTA** | Actualizaciones firmadas y verificadas; control de versión de firmware; rollback ante fallo (ver [devices.md](./devices.md)). |
| **Store-and-forward** | Buffer local cifrado ante cortes; reenvío con deduplicación (dedup_key del Evento canónico) sin pérdida ni duplicado. |
| **Rotación / revocación** | Posibilidad de rotar credenciales y revocar un dispositivo comprometido sin afectar al resto del tenant. |
| **Aislamiento por tenant** | Un dispositivo pertenece a un único tenant; sus datos se enrutan solo a la DB de ese tenant. |

---

## 6. Auditoría y trazabilidad de acciones

- **Auditoría por tenant:** el servicio **Audit** registra las acciones y cambios relevantes dentro de cada tenant (quién hizo qué, cuándo y sobre qué recurso), en la DB del tenant.
- **Auditoría global:** las acciones del proveedor sobre los tenants (alta, baja, suspensión, cambios de licencia, accesos de Soporte/break-glass) se registran en la **auditoría global** del Control Plane (ver [control-plane.md](./control-plane.md)).
- **Inmutabilidad:** los registros de auditoría y el historial de eventos son **inmutables** una vez escritos; esto se alinea con el Event Store y la genealogía de [traceability.md](./traceability.md).
- **Break-glass auditado:** cualquier acceso excepcional de Soporte a un tenant queda registrado, acotado en el tiempo y sujeto a revisión.
- **Correlación:** los logs centralizados (Observability) permiten correlacionar por tenant y por evento sin mezclar datos entre clientes (ver [architecture.md](./architecture.md)).

---

## 7. Cumplimiento (compliance)

| Área | Requisito |
|---|---|
| **Protección de datos** | Tratamiento de datos personales conforme a marcos GDPR-like (y normativa local aplicable); minimización y propósito. |
| **Retención** | Políticas de retención por tenant/plan; borrado seguro al finalizar la retención (baja definitiva, ver [control-plane.md](./control-plane.md)). |
| **Residencia de datos** | Posibilidad de alojar la DB de un tenant en una región específica (habilitado por DB-per-tenant; ver [multi-tenancy.md](./multi-tenancy.md)). |
| **Derechos del titular** | Soporte a solicitudes de acceso/rectificación/eliminación dentro del alcance del tenant. |
| **Requisitos industriales** | Trazabilidad de lote/serie, integridad e inmutabilidad del historial para auditorías de calidad/regulatorias (ver [traceability.md](./traceability.md)). |
| **Segregación de funciones** | Separación entre roles globales del proveedor y roles operativos del tenant; mínimo privilegio. |

---

## 8. Modelo de amenazas resumido

Amenazas principales y sus mitigaciones. No es exhaustivo; se refinará con un ejercicio formal de threat modeling.

| # | Amenaza | Vector | Mitigación en Nexo |
|---|---|---|---|
| T1 | **Fuga de datos entre tenants** | Bug de filtro, escalada de privilegios | DB-per-tenant (aislamiento físico) + validación de claim de tenant + RBAC/ABAC. Ver [multi-tenancy.md](./multi-tenancy.md). |
| T2 | **Robo de credenciales de conexión** | Secreto expuesto en token/log/código | Gestor de secretos central; secretos por tenant, nunca en token/logs; rotación y revocación. |
| T3 | **Acceso no autorizado (cuentas)** | Credenciales robadas, phishing | MFA obligatorio para roles sensibles; SSO; expiración/revocación de sesión; mínimo privilegio. |
| T4 | **Abuso del Control Plane** | Cuenta global comprometida | MFA, mínimo privilegio, break-glass auditado, separación de planos, auditoría global. Ver [control-plane.md](./control-plane.md). |
| T5 | **Dispositivo edge comprometido** | Firmware alterado, credencial de device robada | Aprovisionamiento con identidad única, certificados, firmware firmado/OTA verificada, revocación por device. Ver [devices.md](./devices.md). |
| T6 | **Interceptación de datos** | Tráfico en claro (MITM) | Cifrado en tránsito extremo a extremo; conexiones mutuamente autenticadas. |
| T7 | **Manipulación del historial** | Alteración de eventos/auditoría | Historial de eventos y auditoría **inmutables**; dedup_key. Ver [traceability.md](./traceability.md). |
| T8 | **Denegación de servicio / noisy neighbor** | Picos de eventos, tenant abusivo | Backpressure, colas/particiones por tenant, autoscaling, límites de licencia (quotas). Ver [scalability.md](./scalability.md). |
| T9 | **Inyección vía integraciones/ERP** | Datos maliciosos desde sistemas externos | Anti-Corruption Layer (ACL), validación/normalización, credenciales de integración por tenant. Ver [integrations.md](./integrations.md). |
| T10 | **Pérdida de datos** | Fallo de infraestructura, error humano | Backup/restore por tenant, recuperación granular, cifrado de backups. Ver [multi-tenancy.md](./multi-tenancy.md). |
| T11 | **Repudio de acciones** | Usuario niega haber actuado | Auditoría por tenant y global con atribución de identidad; trazabilidad. |
| T12 | **Exposición por storage mal configurado** | Bucket/URL accesible indebidamente | Storage segmentado por tenant, credenciales de alcance limitado y URLs temporales. |

---

## 9. Relación con otros documentos

- **[users-permissions.md](./users-permissions.md):** roles, RBAC/ABAC, scoping por planta/línea, matriz de permisos.
- **[multi-tenancy.md](./multi-tenancy.md):** DB-per-tenant, resolución de tenant, aislamiento, backup/restore, residencia.
- **[devices.md](./devices.md):** dispositivos, aprovisionamiento, firmware/OTA, salud del edge.
- **[traceability.md](./traceability.md):** historial inmutable, genealogía de lote/serie, integridad del dato.
- **[control-plane.md](./control-plane.md):** usuarios globales, break-glass, auditoría global, ciclo de vida y retención.
- **[integrations.md](./integrations.md):** ACL, credenciales de integración con ERPs.
- **[architecture.md](./architecture.md):** Identity & Access, Audit, Observability, API Gateway, edge outbound.

---

## Preguntas abiertas

1. **Estándares de cumplimiento objetivo:** ¿a qué certificaciones/marcos apunta Nexo (por ejemplo, SOC 2, ISO 27001, GDPR, normativa local es-AR/industrial) y en qué fase del roadmap?
2. **Break-glass de Soporte:** ¿qué flujo de aprobación, límite temporal y revisión gobierna el acceso excepcional a la DB de un tenant? (coordinar con [control-plane.md](./control-plane.md)).
3. **MFA y SSO:** ¿MFA obligatorio solo para roles sensibles o para todos? ¿Qué proveedores de SSO/identidad se soportan y bajo qué plan?
4. **Gestión de claves:** ¿claves gestionadas por la plataforma o posibilidad de "bring your own key" (BYOK) para clientes enterprise?
5. **Retención y borrado:** ¿qué períodos de retención por plan y qué garantías de borrado seguro (crypto-shredding) se ofrecen?
6. **Seguridad del edge:** ¿qué mecanismo concreto de identidad de dispositivo (certificados) y qué política de rotación/OTA se define para el MVP vs. fases posteriores?
7. **Respuesta a incidentes:** ¿cuál es el plan de respuesta y notificación ante brechas (plazos, comunicación al tenant, obligaciones regulatorias)?
8. **Pentesting y auditorías externas:** ¿con qué frecuencia se realizarán pruebas de penetración y auditorías de seguridad independientes?
