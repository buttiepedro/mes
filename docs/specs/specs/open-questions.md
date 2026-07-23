# Preguntas abiertas — Plataforma "Nexo"

> **Documento:** `specs/specs/open-questions.md` · **Estado:** Borrador v0.1 · **Actualizado:** 2026-07-11
> **Roles:** Product Manager · Software Architect · UX Designer
> **Relacionados:** [product.md](./product.md) · [architecture.md](./architecture.md) · [multi-tenancy.md](./multi-tenancy.md) · [integrations.md](./integrations.md) · [devices.md](./devices.md) · [security.md](./security.md) · [glossary.md](./glossary.md) · [future-features.md](./future-features.md) · [roadmap](../roadmap/roadmap.md) · [idea](../idea.md)

> [!IMPORTANT]
> **El tablero maestro, priorizado y respondible, vive en [../open-questions-board.md](../open-questions-board.md).**
> Ahí están consolidadas y deduplicadas las **105 preguntas** de todos los documentos, con ID estable,
> recomendación por defecto, prioridad (P0/P1/P2), documentos afectados y columna de **Respuesta**.
> Este documento queda como **vista temática** de referencia; para responder y hacer seguimiento, usá el tablero.
>
> **✅ Las 13 P0 se resolvieron el 2026-07-11** (respuestas completas en el [tablero](../open-questions-board.md)):
> **PR-01** → Producción + dashboard · **PR-02** → híbrido configurable (MVP: manual + datalogger/CSV) · **AR-01** → broker tipo Kafka (abstraído) · **MT-01** → migraciones por cohortes con feature flags · **MT-02** → secretos en Vault/KMS (Registry solo referencias) · **IN-01** → Odoo *pull* MO/Producto/UoM/Motivos + *push* producción/scrap por cierre de corrida · **ED-01** → Agente Edge contenedor/software + appliance opcional · **ED-02** → protocolos industriales (S7/OPC UA/Modbus/MQTT) a V1; MVP con datalogger/CSV · **ED-05** → mTLS + tokens rotables · **SE-02** → SSO OIDC/SAML + MFA obligatoria (roles sensibles/globales) · **UX-01** → captura offline-first · **CO-01** → pricing base por planta + por dispositivo · **OP-02** → observabilidad agregada + salud por tenant/edge.

## Resumen ejecutivo

Este documento consolida las **decisiones pendientes** que deben resolverse (o al menos acotarse con una hipótesis explícita) **antes de comprometer desarrollo**. Reúne las dudas transversales que emergen de todos los módulos —producto, arquitectura, multi-tenancy, integraciones, edge/dispositivos, seguridad, UX, comercial y operación— para que ninguna quede solo enterrada en un documento de dominio. Cada módulo aporta 3–8 preguntas en su propia sección "Preguntas abiertas"; aquí se agregan, se les asigna impacto, opciones, owner sugerido y prioridad, y se les da seguimiento.

La primera y más visible: **el nombre "Nexo" es provisional (working name)**. Se usa en toda la documentación por consistencia, pero **no está confirmado** y debe validarse (marca, dominio, redes, conflicto de mercado) antes de cualquier exposición pública o comercial.

El propósito es doble: (1) evitar que supuestos implícitos se conviertan en deuda de diseño costosa (especialmente en el modelo **DB-per-tenant**, el edge y las integraciones ERP, que son difíciles de revertir); y (2) dar a cada decisión un **owner** y una **prioridad** para que el roadmap ([roadmap](../roadmap/roadmap.md)) avance sin bloqueos ocultos. Las decisiones ya cerradas en el brief de fundamentos (multi-tenancy DB-per-tenant, event-driven, core agnóstico de ERP, edge-first) **no** se reabren aquí; sólo se documentan las aristas que aún requieren definición.

> **Cómo usar este documento**
> - **Prioridad:** `P0` (bloquea el MVP) · `P1` (necesaria para V1) · `P2` (V2/Enterprise o mejora).
> - **Impacto:** consecuencia si se decide mal o tarde.
> - **Owner sugerido:** rol responsable de proponer/decidir (no necesariamente quien ejecuta).
> - Cada fila enlaza al documento donde la decisión se materializa.

---

## 0. Decisión destacada — Nombre del producto

| # | Pregunta | Impacto | Opciones | Owner sugerido | Prioridad |
|---|----------|---------|----------|----------------|-----------|
| N-01 | **El nombre "Nexo" es PROVISIONAL (working name). ¿Se confirma "Nexo" o se elige otro nombre definitivo?** | Alto en marca, dominio, marketing, contratos y UI. Cambiarlo tarde implica retrabajo de assets, dominios y material comercial. | (a) Confirmar "Nexo" tras verificación legal/marca/dominio; (b) shortlist de alternativas + estudio de marca; (c) posponer y usar "Nexo" solo interno hasta go-to-market | Product Manager + Legal/Marca | **P0** |
| N-02 | ¿Disponibilidad de dominio, marca registrada y handles de redes para "Nexo"? ¿Conflicto con productos existentes homónimos? | Riesgo legal/comercial; posible rebranding forzado. | (a) Búsqueda de antecedentes marcarios; (b) registrar defensivamente; (c) nombre + sufijo diferenciador | Legal + Marketing | P0 |

---

## 1. Producto / Alcance

| # | Pregunta | Impacto | Opciones | Owner sugerido | Prioridad |
|---|----------|---------|----------|----------------|-----------|
| PR-01 | ¿Cuál es el **caso de uso "estrella"** del MVP para validar con el primer cliente (¿registro de producción?, ¿paradas?, ¿scrap?)? | Enfoca el esfuerzo y la demo; evita dispersión. | (a) Producción + dashboard; (b) Paradas + OEE; (c) Scrap + costos | Product Manager | **P0** |
| PR-02 | ¿El MVP incluye **carga 100% manual** como camino válido, o exige al menos una fuente automática (PLC/datalogger)? | Define si se puede vender sin hardware en planta. | (a) Manual-first; (b) requiere 1 fuente automática; (c) híbrido configurable | Product Manager | P0 |
| PR-03 | ¿Qué **industria piloto** se prioriza (metalúrgica, alimenticia, plásticos…)? Cada una tiene matices de calidad/trazabilidad. | Sesga catálogos semilla, KPIs y validaciones. | (a) 1 vertical foco; (b) 2 verticales; (c) genérico configurable | Product Manager | P1 |
| PR-04 | ¿Se definen **objetivos/targets de KPI configurables** por tenant (metas de OEE, scrap máximo) o valores globales? | Afecta dashboards, alertas y contratos de valor. | (a) Global; (b) por tenant; (c) por línea/máquina | Product Manager | P1 |
| PR-05 | ¿La plataforma ofrecerá **planificación/scheduling** de producción o solo captura/ejecución contra órdenes del ERP? | Delimita frontera con el ERP; evita convertirse en APS. | (a) Solo ejecución; (b) scheduling básico; (c) integración con APS externo | Product Manager + Architect | P2 |
| PR-06 | ¿Alcance de **mantenimiento** en el producto (correctivo/preventivo) más allá de MTBF/MTTR? | Puede solaparse con CMMS; define límites. | (a) Solo indicadores; (b) órdenes de trabajo de mantenimiento; (c) integración con CMMS | Product Manager | P2 |

---

## 2. Arquitectura

| # | Pregunta | Impacto | Opciones | Owner sugerido | Prioridad |
|---|----------|---------|----------|----------------|-----------|
| AR-01 | ¿Qué **broker de mensajería** concreto se adopta (Kafka, RabbitMQ, NATS, cloud-managed)? El brief lo deja tech-agnóstico. | Columna vertebral event-driven; difícil de cambiar luego. | (a) Kafka (throughput/orden); (b) RabbitMQ (simplicidad); (c) managed cloud | Software Architect | **P0** |
| AR-02 | ¿Se usa **event sourcing** puro en Traceability/Event Store o event store con proyecciones + estado? | Impacta complejidad, auditoría y coste operativo. | (a) Event sourcing completo; (b) log de eventos inmutable + estado; (c) híbrido por dominio | Software Architect | P1 |
| AR-03 | ¿Comunicación interna síncrona por **gRPC, REST o ambas**? ¿Dónde se permite sync vs. async? | Latencia, contratos y tooling. | (a) gRPC interno + REST externo; (b) REST todo; (c) mixto por caso | Software Architect | P1 |
| AR-04 | ¿Cómo se **versionan los eventos canónicos** y sus contratos (schema registry, compatibilidad)? | Evita romper consumidores al evolucionar. | (a) Schema registry + compat; (b) versión en payload; (c) contract testing | Software Architect | P1 |
| AR-05 | ¿Orquestación de contenedores y despliegue: **Kubernetes** gestionado, serverless o mixto? | Coste, portabilidad y complejidad SRE. | (a) K8s gestionado; (b) serverless donde encaje; (c) PaaS | Software Architect + SRE | P1 |
| AR-06 | ¿Estrategia de **sagas/consistencia eventual** entre servicios (producción↔trazabilidad↔integraciones)? | Correctitud de flujos distribuidos. | (a) Sagas coreografiadas; (b) orquestadas; (c) outbox + idempotencia | Software Architect | P1 |

---

## 3. Multi-Tenancy / Datos

> El modelo **DB-per-tenant** es una decisión cerrada (ver [multi-tenancy.md](./multi-tenancy.md)); estas preguntas afinan su implementación y operación.

| # | Pregunta | Impacto | Opciones | Owner sugerido | Prioridad |
|---|----------|---------|----------|----------------|-----------|
| MT-01 | ¿Cómo se ejecutan y versionan las **migraciones de esquema across miles de tenants** (rolling, por lotes, ventanas)? | Riesgo operativo mayor con DB-per-tenant. | (a) Migración por lotes con feature flag; (b) blue/green por tenant; (c) online + backfill | Software Architect + SRE | **P0** |
| MT-02 | ¿Dónde y cómo se **almacenan/rotan los secretos** de conexión del Tenant Connection Registry? | Seguridad crítica; compromiso = fuga de datos. | (a) Vault/secret manager; (b) KMS + cifrado; (c) managed identities | Security + Architect | **P0** |
| MT-03 | ¿Política de **almacenamiento time-series** de lecturas: misma DB del tenant o store dedicado por tenant? | Coste y rendimiento a millones de eventos/día. | (a) TSDB por tenant; (b) TSDB compartido segmentado; (c) híbrido caliente/frío | Software Architect | P1 |
| MT-04 | ¿**Retención y archivado** de eventos/lecturas por tenant (cuánto tiempo, tiering, borrado)? | Coste de almacenamiento y cumplimiento. | (a) Retención configurable por plan; (b) tiering caliente/frío; (c) export + purga | Product + SRE | P1 |
| MT-05 | ¿Modelo de **backups y disaster recovery por tenant** (RPO/RTO), y restore selectivo de un solo tenant? | Continuidad y confianza enterprise. | (a) Backup por DB + PITR; (b) snapshots; (c) DR multi-región (Enterprise) | SRE | P1 |
| MT-06 | ¿Se permite **residencia de datos por región/país** (una DB de tenant en otra geografía)? | Cumplimiento (soberanía de datos) y latencia. | (a) Región por tenant; (b) global con opción regional; (c) solo Enterprise | Architect + Legal | P2 |
| MT-07 | ¿Cómo se hace **onboarding/offboarding** completo de un tenant (export total, borrado verificable)? | Requisito legal (portabilidad, derecho al olvido). | (a) Export estándar + certificado de borrado; (b) manual asistido | Product + Security | P1 |

---

## 4. Integraciones / ERP

| # | Pregunta | Impacto | Opciones | Owner sugerido | Prioridad |
|---|----------|---------|----------|----------------|-----------|
| IN-01 | ¿Qué **entidades de Odoo** se sincronizan en el MVP (órdenes de producción, productos, lotes, movimientos) y en qué **dirección**? | Define el valor real de la integración inicial. | (a) Órdenes + productos (lectura); (b) + reporte de producción (escritura); (c) bidireccional completo | Integraciones + Product | **P0** |
| IN-02 | ¿La sincronización es **push, pull o webhooks**? ¿Frecuencia y tolerancia a desfases? | Latencia, carga sobre el ERP y consistencia. | (a) Pull programado; (b) webhooks; (c) híbrido | Integraciones + Architect | P1 |
| IN-03 | ¿Cómo se resuelven **conflictos de datos** (mismo campo cambiado en Nexo y en el ERP)? | Integridad; evita sobrescrituras erróneas. | (a) ERP como fuente de verdad; (b) last-write-wins; (c) reglas por campo | Integraciones | P1 |
| IN-04 | ¿Qué versiones de **Odoo** se soportan (SaaS vs. on-premise, Community vs. Enterprise)? | Alcance del conector y esfuerzo de mantenimiento. | (a) 1 versión LTS; (b) rango de versiones; (c) solo Odoo.sh | Integraciones | P1 |
| IN-05 | ¿Roadmap de **multi-ERP** (SAP, Dynamics, Oracle): contrato de conector común desde ya o después? | Diseñar la ACL genérica temprano ahorra retrabajo (V2). | (a) Contrato genérico desde MVP; (b) refactor en V2 | Architect + Product | P2 |
| IN-06 | ¿Gobernanza del **Marketplace de conectores** (certificación de terceros, seguridad, revenue share)? | Modelo de ecosistema y riesgo de seguridad. | (a) Solo oficiales al inicio; (b) partners certificados; (c) abierto con revisión | Product + Security | P2 |

---

## 5. Dispositivos / Edge

| # | Pregunta | Impacto | Opciones | Owner sugerido | Prioridad |
|---|----------|---------|----------|----------------|-----------|
| ED-01 | ¿El **Agente Edge** se distribuye como appliance, contenedor, o software sobre hardware del cliente? | Instalación, soporte y compatibilidad en planta. | (a) Contenedor/soft en PC industrial; (b) appliance provisto; (c) ambos | Architect + Product | **P0** |
| ED-02 | ¿Qué **protocolos** entran realmente en el MVP vs. V1 (S7 y datalogger sí; OPC UA/Modbus/MQTT ¿cuándo)? | Alinea expectativas comerciales con capacidad real. | (a) MVP: S7 + datalogger; (b) + Modbus; (c) todos en V1 | Product + Devices | **P0** |
| ED-03 | ¿Cómo se gestiona **OTA de firmware/config** y rollback ante fallo en dispositivos remotos? | Riesgo de dejar dispositivos inoperables (bricking). | (a) OTA con canary + rollback; (b) manual asistido; (c) sin OTA en MVP | Devices + SRE | P1 |
| ED-04 | ¿Límites de **buffer de store-and-forward** (tamaño, tiempo, política de descarte) ante cortes largos? | Pérdida de datos vs. saturación del edge. | (a) Buffer por tamaño+TTL; (b) persistente en disco; (c) configurable por tenant | Devices + Architect | P1 |
| ED-05 | ¿**Seguridad del edge**: cómo se autentica el gateway contra la nube y cómo se aprovisiona (zero-touch)? | Superficie de ataque en planta. | (a) mTLS + tokens rotables; (b) certificados por dispositivo; (c) provisioning zero-touch | Security + Devices | **P0** |
| ED-06 | ¿Estrategia de **reloj/timestamping** (hora del dispositivo vs. servidor) y manejo de desfasajes horarios? | Correctitud temporal de eventos y KPIs. | (a) Sello en edge + corrección; (b) NTP obligatorio; (c) doble timestamp | Devices + Architect | P1 |

---

## 6. Seguridad / Cumplimiento

| # | Pregunta | Impacto | Opciones | Owner sugerido | Prioridad |
|---|----------|---------|----------|----------------|-----------|
| SE-01 | ¿Qué **certificaciones/compliance** se apuntan (ISO 27001, SOC 2, y sectoriales como GxP/FSMA en alimentos/farma)? | Habilita ventas enterprise/reguladas; condiciona diseño. | (a) SOC 2 primero; (b) ISO 27001; (c) sectorial según vertical | Security + Product | P1 |
| SE-02 | ¿Modelo de **autenticación** (IdP propio, SSO SAML/OIDC, social) y MFA obligatorio? | Seguridad de acceso y adopción enterprise. | (a) OIDC + SSO empresarial; (b) IdP propio + MFA; (c) ambos | Security | **P0** |
| SE-03 | ¿Requisitos de **auditoría inmutable** (quién hizo qué) y su retención por tenant y global? | Cumplimiento y trazabilidad de acciones. | (a) Audit log append-only por tenant; (b) + global CP; (c) firmado/WORM | Security + Architect | P1 |
| SE-04 | ¿Cifrado **en tránsito y en reposo** por defecto, y gestión de claves por tenant (BYOK)? | Confianza enterprise y cumplimiento. | (a) TLS + cifrado at-rest; (b) + BYOK Enterprise; (c) HSM | Security | P1 |
| SE-05 | ¿Política de **gestión de vulnerabilidades y pentesting** (frecuencia, responsable)? | Reduce riesgo operativo y de reputación. | (a) SAST/DAST en CI; (b) pentest anual; (c) bug bounty | Security + SRE | P2 |
| SE-06 | ¿Cómo se maneja **PII de operarios** (fotos, nombres) frente a normativas de privacidad locales? | Cumplimiento laboral y de datos personales. | (a) Minimización + consentimiento; (b) anonimización; (c) configurable | Legal + Security | P1 |

---

## 7. UX

| # | Pregunta | Impacto | Opciones | Owner sugerido | Prioridad |
|---|----------|---------|----------|----------------|-----------|
| UX-01 | ¿La **captura en tablet** funciona **offline** (planta con conectividad intermitente)? | Usabilidad real en piso de planta. | (a) Offline-first con sync; (b) online-only; (c) modo degradado | UX + Architect | **P0** |
| UX-02 | ¿Diseño para **guantes/entornos hostiles** (botones grandes, alto contraste, poco texto)? | Adopción por operarios; errores de captura. | (a) UI "planta" dedicada; (b) responsive estándar; (c) kiosco | UX Designer | P1 |
| UX-03 | ¿**Idiomas** de la interfaz (es-AR base) y necesidad de multilenguaje por tenant/planta? | Alcance internacional y esfuerzo de i18n. | (a) es-AR + en; (b) i18n completo por tenant; (c) es solo | Product + UX | P1 |
| UX-04 | ¿Qué **dashboards estándar** vienen listos vs. configurables por el usuario? | Time-to-value vs. flexibilidad. | (a) Plantillas fijas; (b) builder de tableros; (c) mixto | Product + UX | P1 |
| UX-05 | ¿Soporte de **pantallas grandes/andon** en planta (modo TV, auto-refresh, sin login)? | Visibilidad en piso; caso de uso frecuente. | (a) Modo andon dedicado; (b) dashboard fullscreen; (c) fase futura | UX + Product | P2 |
| UX-06 | ¿Nivel de **personalización de marca por tenant** (logo, colores) en la UI? | Percepción enterprise y white-label. | (a) Logo+color; (b) white-label completo; (c) sin branding | Product | P2 |

---

## 8. Comercial / Licenciamiento

| # | Pregunta | Impacto | Opciones | Owner sugerido | Prioridad |
|---|----------|---------|----------|----------------|-----------|
| CO-01 | ¿**Modelo de pricing** (por planta, por dispositivo, por usuario, por eventos, por módulos)? | Núcleo del negocio; condiciona feature flags y métricas. | (a) Por planta+módulos; (b) por dispositivos; (c) por consumo de eventos | Product + Comercial | **P0** |
| CO-02 | ¿Qué **planes/tiers** y qué límites (dispositivos, usuarios, retención, conectores) por plan? | Empaquetado y upsell; gestionado por Administration & Licensing. | (a) 3 tiers; (b) modular a la carta; (c) enterprise custom | Product | P1 |
| CO-03 | ¿Cómo se **miden y facturan** el consumo (eventos, almacenamiento, integraciones)? | Requiere medición fiable y transparente. | (a) Metering por eventos; (b) flat por plan; (c) híbrido | Product + SRE | P1 |
| CO-04 | ¿Existe **free trial / freemium / PoC** y con qué límites y duración? | Adquisición y fricción de entrada. | (a) Trial 30 días; (b) PoC guiada; (c) freemium acotado | Comercial + Product | P1 |
| CO-05 | ¿Modelo de **partners/implementadores** y su remuneración (revenue share, referidos)? | Escala go-to-market vía canal. | (a) Programa de partners; (b) directo; (c) mixto | Comercial | P2 |
| CO-06 | ¿Qué **SLAs** se ofrecen por tier (uptime, soporte, tiempos de respuesta)? | Compromiso contractual y costo operativo. | (a) SLA por tier; (b) best-effort MVP; (c) Enterprise dedicado | Product + SRE | P2 |

---

## 9. Operación / SRE

| # | Pregunta | Impacto | Opciones | Owner sugerido | Prioridad |
|---|----------|---------|----------|----------------|-----------|
| OP-01 | ¿Qué **objetivos de disponibilidad (SLO)** de la plataforma y por servicio, y su presupuesto de error? | Guía inversión en resiliencia y on-call. | (a) 99.9% MVP; (b) 99.95% V1; (c) multi-región Enterprise | SRE | P1 |
| OP-02 | ¿Cómo se **monitorea la salud de miles de tenants y edges** de forma escalable (Observability)? | Detección proactiva; soporte eficiente. | (a) Métricas agregadas + alertas; (b) health por tenant/edge; (c) AIOps (futuro) | SRE | **P0** |
| OP-03 | ¿Estrategia de **despliegue** (CI/CD por servicio, canary, feature flags, rollback)? | Velocidad y seguridad de cambios. | (a) Canary + flags; (b) blue/green; (c) rolling | SRE + Architect | P1 |
| OP-04 | ¿Cómo se maneja el **soporte multi-tenant** (acceso de soporte a datos de un tenant con auditoría y consentimiento)? | Privacidad vs. capacidad de resolver incidentes. | (a) Impersonación auditada + consentimiento; (b) solo lectura; (c) break-glass | SRE + Security | P1 |
| OP-05 | ¿Manejo de **"ruidosos" (noisy neighbors)** y aislamiento de recursos entre tenants? | Rendimiento y equidad. | (a) Límites por tenant; (b) tiers de recursos; (c) DBs dedicadas Enterprise | SRE | P1 |
| OP-06 | ¿**Runbooks y on-call** desde el MVP o se difieren? ¿Quién opera el edge en planta del cliente? | Continuidad y responsabilidad de incidentes. | (a) Runbooks mínimos + on-call; (b) soporte horario; (c) partner local | SRE + Comercial | P2 |

---

## 10. Seguimiento y gobierno de decisiones

- Cada pregunta debería migrar a un **ADR (Architecture Decision Record)** o a una decisión de producto cuando se resuelva, dejando aquí el enlace y la fecha.
- Revisión recomendada: al cierre de cada fase del [roadmap](../roadmap/roadmap.md) (MVP → V1 → V2 → Enterprise) se recorre esta lista y se cierran/re-priorizan ítems.
- Las **P0** deben quedar decididas (o con hipótesis firmada) **antes** de arrancar el desarrollo del MVP.
- Toda nueva pregunta abierta que aparezca en un documento de dominio se **consolida aquí** con su identificador de sección.

---

## Preguntas abiertas (meta)

1. ¿Qué herramienta/registro se usa para el **seguimiento formal** de estas decisiones (ADRs en repo, backlog en [backlog](../roadmap/backlog.md), issue tracker)?
2. ¿Con qué **cadencia** y con qué **comité** (PM + Architect + Security + Comercial) se revisan y cierran las preguntas?
3. ¿Qué criterio define que una pregunta pasa de "abierta" a "decidida con hipótesis" vs. "decidida y validada con cliente"?
4. ¿Se necesita una **matriz RACI** por área para clarificar owners definitivos más allá del "owner sugerido"?
