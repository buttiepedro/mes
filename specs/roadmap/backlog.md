# Nexo — Backlog inicial (épicas y user stories)

> **Documento:** `specs/roadmap/backlog.md` · **Estado:** Borrador v0.1 · **Actualizado:** 2026-07-11
> **Roles:** Product Manager · Software Architect · UX Designer
> **Relacionados:** [roadmap.md](./roadmap.md) · [milestones.md](./milestones.md) · [vision.md](./vision.md) · [idea.md](../idea.md) · [modules.md](../specs/modules.md) · [users-permissions.md](../specs/users-permissions.md) · [product.md](../specs/product.md)

## Resumen ejecutivo

Este documento contiene el **backlog inicial de producto** de Nexo, organizado por **épicas** (una por microservicio / bounded context canónico, brief §5.1) y desglosado en **user stories** con el formato *"Como &lt;rol&gt; quiero &lt;objetivo&gt; para &lt;beneficio&gt;"*. Cada historia lleva su **prioridad MoSCoW** y su **tag de fase** (MVP / V1 / V2 / Enterprise), de modo que el backlog sea directamente derivable del [roadmap](./roadmap.md) y verificable contra los [hitos](./milestones.md).

Las historias del **MVP** están marcadas de forma explícita (columna **MVP** con el ícono ✅) para separar sin ambigüedad el alcance mínimo viable —captura de Producción/Scrap/Calidad/Paradas/Eventos, **carga manual en tablet + datalogger vía carga de archivo/CSV/Excel**, dashboard en tiempo real, integración Odoo y multi-tenant DB-per-tenant (la **captura automática por protocolos industriales** —S7/OPC UA/Modbus/MQTT— pasa a V1)— del resto de la evolución. El backlog cubre **todos los módulos** del brief, incluidos los servicios compartidos del Control Plane y los de fase futura (IA/visión), para dar una vista completa del producto aunque muchas historias sean posteriores al MVP.

Los **roles** usados en las historias son los canónicos (brief §9): personas del tenant (Operario, Supervisor, Calidad, Producción, Mantenimiento, Gerencia, Administrador del tenant, Integraciones) y roles globales del Control Plane (Super Administrador, Soporte, Implementador, Partner). Este backlog es un punto de partida vivo: se refina y se estima con el equipo, y se sincroniza con [milestones.md](./milestones.md) a medida que avanza cada fase.

---

## 1. Convenciones

- **Formato de historia:** *Como &lt;rol&gt; quiero &lt;objetivo&gt; para &lt;beneficio&gt;.*
- **Roles (brief §9):** Operario, Supervisor, Calidad, Producción, Mantenimiento, Gerencia, Administrador (del tenant), Integraciones · globales: Super Administrador, Soporte, Implementador, Partner.
- **MoSCoW:** Must / Should / Could / Won't (por fase).
- **Fase (tag):** MVP · V1 · V2 · Enterprise (canónico, brief §11).
- **MVP:** ✅ marca historias dentro del alcance del MVP.
- **Identificadores:** `US-<MOD>-<n>` donde `<MOD>` abrevia el módulo (p. ej. `US-PROV-01`).

### 1.1 Índice de épicas (módulos / bounded contexts)

| # | Épica (módulo) | Ámbito | Fase de entrada |
|---|---|---|---|
| E1 | [Identity & Access](#e1--identity--access) | Compartido/CP | MVP |
| E2 | [Tenant Provisioning](#e2--tenant-provisioning) | Global/CP | MVP |
| E3 | [Administration & Licensing](#e3--administration--licensing) | Global/CP | MVP |
| E4 | [Marketplace](#e4--marketplace) | Global/CP | V2 |
| E5 | [Observability](#e5--observability) | Global/CP | MVP→V1 |
| E6 | [Ingestion / Edge Gateway](#e6--ingestion--edge-gateway) | Compartido | MVP |
| E7 | [Devices](#e7--devices) | Por tenant | MVP |
| E8 | [Production](#e8--production) | Por tenant | MVP |
| E9 | [Quality](#e9--quality) | Por tenant | MVP |
| E10 | [Scrap](#e10--scrap) | Por tenant | MVP |
| E11 | [Downtime (Paradas)](#e11--downtime-paradas) | Por tenant | MVP |
| E12 | [Traceability / Event Store](#e12--traceability--event-store) | Por tenant | MVP→V1 |
| E13 | [Connectors / Integrations](#e13--connectors--integrations) | Compartido | MVP |
| E14 | [Rules Engine](#e14--rules-engine) | Por tenant | V1 |
| E15 | [Notifications](#e15--notifications) | Compartido | V1 |
| E16 | [Dashboards / Analytics](#e16--dashboards--analytics) | Por tenant | MVP |
| E17 | [Reports](#e17--reports) | Por tenant | V1 |
| E18 | [Files / Media](#e18--files--media) | Compartido | MVP→V1 |
| E19 | [Audit](#e19--audit) | Por tenant/CP | MVP |
| E20 | [AI / Computer Vision](#e20--ai--computer-vision) | Compartido | Enterprise |

---

## E1 · Identity & Access

**Objetivo de la épica:** autenticación y autorización centralizadas con claim de tenant, evolucionando a RBAC avanzado con scoping por planta/línea y ABAC donde aplique.

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-IAM-01 | Como **Administrador del tenant** quiero autenticarme y recibir un token con claim de mi tenant para acceder solo a los datos de mi empresa. | Administrador | Must | MVP | ✅ |
| US-IAM-02 | Como **Operario** quiero iniciar sesión rápido en la tablet (PIN/credencial simple) para empezar a cargar sin fricción. | Operario | Must | MVP | ✅ |
| US-IAM-03 | Como **Administrador del tenant** quiero crear usuarios y asignarles roles para controlar quién hace qué. | Administrador | Must | MVP | ✅ |
| US-IAM-04 | Como **plataforma** quiero denegar el acceso a recursos de otro tenant para garantizar el aislamiento. | Administrador | Must | MVP | ✅ |
| US-IAM-05 | Como **Supervisor** quiero que mi acceso esté acotado a mi planta/línea para ver solo lo que me corresponde. | Supervisor | Must | V1 | |
| US-IAM-06 | Como **Administrador del tenant** quiero definir permisos por rol según una matriz para aplicar RBAC avanzado. | Administrador | Must | V1 | |
| US-IAM-07 | Como **Administrador del tenant** quiero reglas ABAC (por atributo, p. ej. turno) para casos que RBAC no cubre. | Administrador | Should | V1 | |
| US-IAM-08 | Como **Administrador del tenant** quiero SSO corporativo para integrar Nexo con mi gestión de identidades. | Administrador | Could | V2 | |

---

## E2 · Tenant Provisioning

**Objetivo de la épica:** alta automatizada de tenants bajo el modelo DB-per-tenant (7 pasos canónicos) y, más adelante, distribución geográfica de las DBs.

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-PROV-01 | Como **Super Administrador** quiero registrar una empresa en la base global (datos comerciales, plan, estado) para iniciar su alta. | Super Administrador | Must | MVP | ✅ |
| US-PROV-02 | Como **plataforma** quiero crear automáticamente la DB dedicada del tenant para garantizar el aislamiento de datos. | Super Administrador | Must | MVP | ✅ |
| US-PROV-03 | Como **plataforma** quiero ejecutar las migraciones iniciales del esquema operativo para dejar la DB lista. | Super Administrador | Must | MVP | ✅ |
| US-PROV-04 | Como **plataforma** quiero cargar datos base (seed: motivos de scrap/parada, roles, unidades) para que el tenant opere con catálogos por defecto. | Super Administrador | Must | MVP | ✅ |
| US-PROV-05 | Como **plataforma** quiero crear el usuario administrador inicial del tenant para que la empresa pueda gestionarse. | Super Administrador | Must | MVP | ✅ |
| US-PROV-06 | Como **plataforma** quiero registrar la conexión del tenant en el Connection Registry (secreto/credenciales) para resolver su DB en cada request. | Super Administrador | Must | MVP | ✅ |
| US-PROV-07 | Como **plataforma** quiero dejar la empresa en estado "activo" con notificación de bienvenida para completar el alta. | Super Administrador | Must | MVP | ✅ |
| US-PROV-08 | Como **Super Administrador** quiero que el alta sea idempotente y con rollback por paso para tolerar fallos sin dejar estados a medias. | Super Administrador | Should | MVP | ✅ |
| US-PROV-09 | Como **Super Administrador** quiero suspender o dar de baja un tenant para gestionar su ciclo de vida. | Super Administrador | Should | V1 | |
| US-PROV-10 | Como **Super Administrador** quiero migrar la DB de un tenant a otra región sin cambiar la lógica para habilitar distribución geográfica. | Super Administrador | Must | V2 | |

---

## E3 · Administration & Licensing

**Objetivo de la épica:** suscripción base por planta, precio por dispositivo conectado, capas por feature flags, límites y facturación en el Control Plane.

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-LIC-01 | Como **Super Administrador** quiero asignar a cada tenant una suscripción base por planta con sus límites para controlar su alcance y facturación. | Super Administrador | Must | MVP | ✅ |
| US-LIC-02 | Como **plataforma** quiero aplicar la base por planta y los límites (usuarios, dispositivos, eventos) para respetar la licencia. | Super Administrador | Must | MVP | ✅ |
| US-LIC-03 | Como **Gerencia** quiero ver el consumo de mi plan para anticipar necesidades de upgrade. | Gerencia | Should | V1 | |
| US-LIC-04 | Como **Super Administrador** quiero habilitar/inhabilitar capacidades por feature flag y por tenant para desplegar gradualmente. | Super Administrador | Must | V2 | |
| US-LIC-05 | Como **Super Administrador** quiero facturar por uso/plan para monetizar de forma flexible. | Super Administrador | Should | V2 | |
| US-LIC-06 | Como **Gerencia** quiero contratar un SLA enterprise para asegurar disponibilidad y soporte. | Gerencia | Must | Enterprise | |
| US-LIC-07 | Como **Super Administrador** quiero cobrar por **dispositivo conectado** y empaquetar módulos por **capa** vía feature flags (Captura base → MES ligero → IA) para escalar el pricing con la captura automática. | Super Administrador | Should | V1 | |

---

## E4 · Marketplace

**Objetivo de la épica:** catálogo de conectores oficiales y de terceros, con gobernanza y certificación.

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-MKT-01 | Como **Integraciones** quiero explorar un catálogo de conectores para encontrar el que necesito. | Integraciones | Must | V2 | |
| US-MKT-02 | Como **Integraciones** quiero instalar un conector desde el Marketplace y dejarlo operativo sin intervención del proveedor. | Integraciones | Must | V2 | |
| US-MKT-03 | Como **Partner** quiero publicar un conector de terceros para ofrecerlo a los clientes de Nexo. | Partner | Should | V2 | |
| US-MKT-04 | Como **Super Administrador** quiero certificar y poder revocar conectores de terceros para proteger la calidad del catálogo. | Super Administrador | Should | V2 | |
| US-MKT-05 | Como **Partner** quiero un portal/SDK para desarrollar y probar conectores para acelerar mi integración. | Partner | Could | V2 | |
| US-MKT-06 | Como **Partner** quiero publicar modelos/algoritmos de IA en el Marketplace para extender las capacidades inteligentes. | Partner | Could | Enterprise | |

---

## E5 · Observability

**Objetivo de la épica:** estado de tenants, servicios y conectores; métricas, logs y trazas centralizadas en el Control Plane.

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-OBS-01 | Como **Soporte** quiero ver el estado de salud de los tenants y servicios para detectar problemas temprano. | Soporte | Could | MVP | ✅ |
| US-OBS-02 | Como **Soporte** quiero ver el estado de los conectores y del edge de cada tenant para diagnosticar integraciones. | Soporte | Must | V1 | |
| US-OBS-03 | Como **Soporte** quiero una traza extremo a extremo de un evento para diagnosticar incidentes de un tenant. | Soporte | Must | V1 | |
| US-OBS-04 | Como **Soporte** quiero métricas y alertas de plataforma para operar proactivamente. | Soporte | Must | V1 | |
| US-OBS-05 | Como **plataforma** quiero soportar despliegues progresivos (canary/blue-green) con reversión automática ante degradación para reducir riesgo de release. | Soporte | Should | V2 | |
| US-OBS-06 | Como **Gerencia** quiero reportes de cumplimiento de SLA para verificar el servicio contratado. | Gerencia | Must | Enterprise | |

---

## E6 · Ingestion / Edge Gateway

**Objetivo de la épica:** recepción y normalización al Evento canónico desde múltiples protocolos, edge-first con store-and-forward.

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-ING-01 | Como **Integraciones** quiero instalar un Agente Edge/Gateway en planta que conecte outbound a la nube para capturar sin abrir puertos de entrada. | Integraciones | Must | MVP | ✅ |
| US-ING-02 | Como **Integraciones** quiero capturar datos de un PLC Siemens S7 para automatizar el registro de máquina. | Integraciones | Must | V1 | |
| US-ING-03 | Como **Integraciones** quiero capturar datos de un datalogger para incorporar equipos de adquisición. | Integraciones | Must | MVP | ✅ |
| US-ING-04 | Como **plataforma** quiero normalizar toda fuente al Evento canónico (con `origin_metadata` y `dedup_key`) para unificar el dato. | Integraciones | Must | MVP | ✅ |
| US-ING-05 | Como **plataforma** quiero descartar eventos duplicados por `dedup_key` para garantizar idempotencia. | Integraciones | Must | MVP | ✅ |
| US-ING-06 | Como **Integraciones** quiero store-and-forward en el edge para no perder datos ante cortes de conectividad. | Integraciones | Must | MVP | ✅ |
| US-ING-07 | Como **Integraciones** quiero capturar vía OPC UA para integrar sistemas industriales estándar. | Integraciones | Must | V1 | |
| US-ING-08 | Como **Integraciones** quiero capturar vía Modbus para integrar dispositivos que usan ese protocolo. | Integraciones | Must | V1 | |
| US-ING-09 | Como **Integraciones** quiero capturar vía MQTT para incorporar dispositivos IoT que publican por ese canal. | Integraciones | Should | V1 | |
| US-ING-10 | Como **Integraciones** quiero ingerir archivos de datalogger (carga de archivo/CSV/Excel) para capturar sin protocolo industrial en el MVP. | Integraciones | Must | MVP | ✅ |
| US-ING-11 | Como **plataforma** quiero aplicar backpressure ante picos de eventos para sostener la ingesta a escala. | Integraciones | Should | V2 | |
| US-ING-12 | Como **Integraciones** quiero ingerir datos de APIs/sistemas externos para cubrir fuentes no industriales. | Integraciones | Should | V1 | |

---

## E7 · Devices

**Objetivo de la épica:** gestión de dispositivos, sensores y señales/tags, su salud y su firmware/OTA.

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-DEV-01 | Como **Integraciones** quiero dar de alta dispositivos (PLC, datalogger, gateway) para modelar mi hardware de captura. | Integraciones | Must | MVP | ✅ |
| US-DEV-02 | Como **Integraciones** quiero definir sensores y señales/tags de cada dispositivo para saber qué se lee. | Integraciones | Must | MVP | ✅ |
| US-DEV-03 | Como **Integraciones** quiero asociar dispositivos a planta/sector/línea/máquina para contextualizar sus lecturas. | Integraciones | Must | MVP | ✅ |
| US-DEV-04 | Como **Mantenimiento** quiero ver la salud básica de los dispositivos (en línea/fuera de línea) para detectar caídas. | Mantenimiento | Should | MVP | ✅ |
| US-DEV-05 | Como **Mantenimiento** quiero salud avanzada y diagnóstico de dispositivos para anticipar problemas de captura. | Mantenimiento | Could | V1 | |
| US-DEV-06 | Como **Integraciones** quiero gestionar firmware y actualizaciones OTA de dispositivos para mantenerlos al día de forma remota. | Integraciones | Could | V1 | |
| US-DEV-07 | Como **Mantenimiento** quiero medir el consumo energético por dispositivo/línea para gestionar sustentabilidad. | Mantenimiento | Should | Enterprise | |

---

## E8 · Production

**Objetivo de la épica:** órdenes, registros de producción, turnos y productividad.

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-PROD-01 | Como **Operario** quiero registrar la cantidad producida en una orden/máquina/turno para reflejar mi producción real. | Operario | Must | MVP | ✅ |
| US-PROD-02 | Como **plataforma** quiero generar registros de producción automáticamente desde el contador de un PLC para eliminar la carga manual. | Operario | Must | V1 | |
| US-PROD-03 | Como **Producción** quiero asociar la producción a una orden (Work Order/MO) sincronizada con el ERP para conectar planta y gestión. | Producción | Must | MVP | ✅ |
| US-PROD-04 | Como **Supervisor** quiero registrar/gestionar turnos para contextualizar la producción por franja horaria. | Supervisor | Must | MVP | ✅ |
| US-PROD-05 | Como **Producción** quiero ver la productividad por línea/turno para evaluar el desempeño. | Producción | Should | MVP | ✅ |
| US-PROD-06 | Como **Producción** quiero definir operaciones/rutas del proceso para modelar los pasos de fabricación. | Producción | Should | V1 | |
| US-PROD-07 | Como **Producción** quiero comparar producción real vs. planificada para detectar desvíos. | Producción | Could | V1 | |

---

## E9 · Quality

**Objetivo de la épica:** inspecciones, checklists, defectos, tolerancias y disposición.

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-QC-01 | Como **Calidad** quiero registrar una inspección con checklist/variables para dejar constancia del control. | Calidad | Must | MVP | ✅ |
| US-QC-02 | Como **Operario** quiero completar un control de calidad simple en la tablet para reportar en el momento. | Operario | Must | MVP | ✅ |
| US-QC-03 | Como **Calidad** quiero registrar defectos (no conformidades) con su motivo (Reason Code) para clasificarlos. | Calidad | Must | MVP | ✅ |
| US-QC-04 | Como **Calidad** quiero definir tolerancias/límites por variable para evaluar automáticamente conformidad. | Calidad | Should | V1 | |
| US-QC-05 | Como **Calidad** quiero registrar la disposición de un lote no conforme (aceptar/rechazar/retrabajar) para cerrar el circuito. | Calidad | Should | V1 | |
| US-QC-06 | Como **Calidad** quiero calcular FPY (First Pass Yield) con la fórmula canónica para medir calidad a la primera. | Calidad | Should | V1 | |
| US-QC-07 | Como **Calidad** quiero adjuntar una foto de evidencia al defecto para documentar la no conformidad. | Calidad | Could | MVP | ✅ |
| US-QC-08 | Como **Calidad** quiero que un modelo de visión inspeccione automáticamente para detectar defectos sin intervención. | Calidad | Must | Enterprise | |

---

## E10 · Scrap

**Objetivo de la épica:** registros de scrap, motivos, costos y clasificación.

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-SCR-01 | Como **Operario** quiero registrar scrap (cantidad + motivo) para reflejar el descarte real. | Operario | Must | MVP | ✅ |
| US-SCR-02 | Como **Producción** quiero elegir el motivo (Reason Code) de scrap de un catálogo para estandarizar la clasificación. | Producción | Must | MVP | ✅ |
| US-SCR-03 | Como **Gerencia** quiero asociar un costo al scrap para cuantificar la pérdida económica. | Gerencia | Should | MVP | ✅ |
| US-SCR-04 | Como **plataforma** quiero calcular el scrap rate (piezas descartadas / total producidas) con la fórmula canónica para medir de forma consistente. | Producción | Must | MVP | ✅ |
| US-SCR-05 | Como **Producción** quiero clasificar el scrap por tipo/categoría para analizar causas. | Producción | Should | V1 | |
| US-SCR-06 | Como **Gerencia** quiero analizar el scrap por costo y tendencia para priorizar mejoras. | Gerencia | Could | V2 | |

---

## E11 · Downtime (Paradas)

**Objetivo de la épica:** eventos de parada, motivos, MTBF/MTTR.

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-DWN-01 | Como **Operario** quiero registrar una parada de máquina con su motivo para explicar la detención. | Operario | Must | MVP | ✅ |
| US-DWN-02 | Como **plataforma** quiero detectar automáticamente paradas desde el estado de un PLC para no depender del registro manual. | Operario | Must | V1 | |
| US-DWN-03 | Como **Producción** quiero distinguir paradas programadas y no programadas para analizar disponibilidad. | Producción | Must | MVP | ✅ |
| US-DWN-04 | Como **Producción** quiero elegir el motivo (Reason Code) de parada de un catálogo para estandarizar. | Producción | Must | MVP | ✅ |
| US-DWN-05 | Como **Mantenimiento** quiero calcular MTBF y MTTR con las fórmulas canónicas para medir la confiabilidad. | Mantenimiento | Should | V1 | |
| US-DWN-06 | Como **Mantenimiento** quiero anticipar paradas por falla con mantenimiento predictivo para reducir tiempos muertos. | Mantenimiento | Must | Enterprise | |

---

## E12 · Traceability / Event Store

**Objetivo de la épica:** trazabilidad, genealogía lote/serie e historial inmutable.

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-TRC-01 | Como **plataforma** quiero almacenar cada Evento canónico de forma inmutable (append-only) para tener un historial confiable. | Calidad | Should | MVP | ✅ |
| US-TRC-02 | Como **Calidad** quiero consultar el historial de eventos por contexto (planta/línea/máquina) para investigar incidencias. | Calidad | Should | MVP | ✅ |
| US-TRC-03 | Como **Calidad** quiero reconstruir la genealogía de un lote/serie de punta a punta para responder auditorías y recalls. | Calidad | Must | V1 | |
| US-TRC-04 | Como **Producción** quiero capturar el lote/serie asociado a cada registro de producción para habilitar la trazabilidad. | Producción | Should | MVP | ✅ |
| US-TRC-05 | Como **Gerencia** quiero un reporte de trazabilidad exportable para presentar ante auditores/clientes. | Gerencia | Should | V1 | |

---

## E13 · Connectors / Integrations

**Objetivo de la épica:** sincronización con ERPs vía Conectores + ACL, mapeos, jobs de sincronización y reintentos; evolución a multi-ERP.

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-INT-01 | Como **Integraciones** quiero conectar Nexo con Odoo vía un conector desacoplado (ACL) para sincronizar sin acoplar el core. | Integraciones | Must | MVP | ✅ |
| US-INT-02 | Como **Integraciones** quiero mapear el *pull* de MO/Producto/UoM/Motivos y el *push* de producción real y scrap entre Nexo y Odoo para alinear los modelos. | Integraciones | Must | MVP | ✅ |
| US-INT-03 | Como **plataforma** quiero ejecutar jobs de sincronización con reintentos para tolerar fallos transitorios del ERP. | Integraciones | Should | MVP | ✅ |
| US-INT-04 | Como **Integraciones** quiero ver el estado de cada job de sincronización para diagnosticar problemas. | Integraciones | Should | MVP | ✅ |
| US-INT-05 | Como **Integraciones** quiero conectar con SAP/Dynamics/Oracle reutilizando el patrón ACL para soportar multi-ERP. | Integraciones | Must | V2 | |
| US-INT-06 | Como **Integraciones** quiero integrar datos de un MES/SCADA existente (sin comandar máquinas) para unificar el dato de planta. | Integraciones | Should | Enterprise | |

---

## E14 · Rules Engine

**Objetivo de la épica:** reglas trigger-condición-acción en tiempo real.

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-RUL-01 | Como **Supervisor** quiero definir una regla trigger-condición-acción para automatizar respuestas ante eventos. | Supervisor | Must | V1 | |
| US-RUL-02 | Como **plataforma** quiero evaluar reglas sobre eventos en tiempo real para reaccionar sin demora. | Supervisor | Must | V1 | |
| US-RUL-03 | Como **Supervisor** quiero disparar alertas/alarmas por umbral para actuar ante desvíos. | Supervisor | Should | V1 | |
| US-RUL-04 | Como **Administrador del tenant** quiero límites por tenant en el motor de reglas para evitar sobrecarga. | Administrador | Should | V1 | |
| US-RUL-05 | Como **Mantenimiento** quiero reglas basadas en predicciones de IA para actuar antes de la falla. | Mantenimiento | Could | Enterprise | |

---

## E15 · Notifications

**Objetivo de la épica:** envío multicanal, plantillas y escalado.

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-NOT-01 | Como **plataforma** quiero enviar una notificación de bienvenida al alta de tenant para cerrar el onboarding. | Administrador | Could | MVP | ✅ |
| US-NOT-02 | Como **Supervisor** quiero recibir notificaciones multicanal (p. ej. email + app) ante eventos críticos para enterarme a tiempo. | Supervisor | Must | V1 | |
| US-NOT-03 | Como **Administrador del tenant** quiero definir plantillas por rol/persona para adaptar el mensaje al destinatario. | Administrador | Must | V1 | |
| US-NOT-04 | Como **Supervisor** quiero escalado ante notificaciones no atendidas para asegurar la respuesta. | Supervisor | Should | V1 | |
| US-NOT-05 | Como **Supervisor** quiero agrupar/silenciar notificaciones para evitar la "tormenta" de avisos. | Supervisor | Should | V1 | |

---

## E16 · Dashboards / Analytics

**Objetivo de la épica:** KPIs y tableros en tiempo real (CQRS/read models) y analítica avanzada.

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-DSH-01 | Como **Gerencia** quiero un dashboard en tiempo real del estado de la planta para decidir con datos actuales. | Gerencia | Must | MVP | ✅ |
| US-DSH-02 | Como **Producción** quiero ver el OEE (Disponibilidad × Rendimiento × Calidad) con la fórmula canónica para medir la eficiencia real. | Producción | Must | MVP | ✅ |
| US-DSH-03 | Como **Producción** quiero ver el scrap rate en tiempo real para reaccionar en el turno. | Producción | Must | MVP | ✅ |
| US-DSH-04 | Como **Supervisor** quiero un tablero por línea/turno para monitorear mi área. | Supervisor | Should | MVP | ✅ |
| US-DSH-05 | Como **Gerencia** quiero comparativas y tendencias (cohortes, históricos) para análisis avanzado. | Gerencia | Must | V2 | |
| US-DSH-06 | Como **Gerencia** quiero un gemelo digital de la línea que refleje su estado real para simular y optimizar. | Gerencia | Should | Enterprise | |

---

## E17 · Reports

**Objetivo de la épica:** reportes on-demand y programados, exportables.

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-REP-01 | Como **Gerencia** quiero generar un reporte on-demand (producción, scrap, calidad, paradas) para análisis puntuales. | Gerencia | Must | V1 | |
| US-REP-02 | Como **Gerencia** quiero programar reportes periódicos para recibirlos automáticamente. | Gerencia | Must | V1 | |
| US-REP-03 | Como **Gerencia** quiero exportar reportes (p. ej. a planilla/PDF) para compartirlos fuera de Nexo. | Gerencia | Should | V1 | |
| US-REP-04 | Como **Producción** quiero que las cifras del reporte coincidan con el dashboard para tener una única verdad. | Producción | Must | V1 | |
| US-REP-05 | Como **Gerencia** quiero un reporte de cumplimiento de SLA para verificar el servicio enterprise. | Gerencia | Should | Enterprise | |

---

## E18 · Files / Media

**Objetivo de la épica:** fotos, adjuntos y evidencias, con storage aislado por tenant.

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-FIL-01 | Como **Operario** quiero adjuntar una foto a un registro (scrap/defecto) para documentar la evidencia. | Operario | Could | MVP | ✅ |
| US-FIL-02 | Como **plataforma** quiero almacenar los archivos en storage aislado por tenant (bucket/prefijo) para respetar el aislamiento. | Calidad | Must | MVP | ✅ |
| US-FIL-03 | Como **Calidad** quiero gestionar y consultar las evidencias asociadas a inspecciones/defectos para auditar la calidad. | Calidad | Should | V1 | |
| US-FIL-04 | Como **Calidad** quiero que las imágenes alimenten los modelos de visión para habilitar la IA de calidad. | Calidad | Should | Enterprise | |

---

## E19 · Audit

**Objetivo de la épica:** auditoría de acciones y cambios, por tenant y global (Control Plane).

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-AUD-01 | Como **Administrador del tenant** quiero auditar acciones sensibles (alta de usuario, cambios de config) para tener trazabilidad de quién hizo qué. | Administrador | Should | MVP | ✅ |
| US-AUD-02 | Como **Super Administrador** quiero una auditoría global en el Control Plane (altas de tenant, cambios de plan) para gobernar la plataforma. | Super Administrador | Should | MVP | ✅ |
| US-AUD-03 | Como **Administrador del tenant** quiero consultar y filtrar el registro de auditoría para investigar incidentes. | Administrador | Should | V1 | |
| US-AUD-04 | Como **Gerencia** quiero exportar la auditoría para cumplimiento regulatorio para responder ante auditores. | Gerencia | Could | V2 | |

---

## E20 · AI / Computer Vision

**Objetivo de la épica:** visión artificial, OCR y ML (fase futura), con modelos y storage por tenant.

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-AI-01 | Como **Calidad** quiero que un modelo de visión inspeccione piezas para detectar defectos automáticamente. | Calidad | Must | Enterprise | |
| US-AI-02 | Como **Producción** quiero OCR sobre etiquetas/documentos para capturar datos sin tipeo. | Producción | Should | Enterprise | |
| US-AI-03 | Como **Mantenimiento** quiero modelos predictivos sobre señales/eventos históricos para anticipar fallas. | Mantenimiento | Must | Enterprise | |
| US-AI-04 | Como **plataforma** quiero que los modelos y datos de IA estén aislados por tenant para no filtrar información entre clientes. | Administrador | Must | Enterprise | |
| US-AI-05 | Como **Gerencia** quiero indicadores de energía y sustentabilidad derivados de IA para gestionar consumo y huella. | Gerencia | Should | Enterprise | |

---

## 2. Resumen del alcance MVP (historias marcadas ✅)

El MVP queda cubierto por las historias ✅ de las épicas **E1 Identity & Access, E2 Tenant Provisioning, E3 Administration & Licensing, E5 Observability (mínima), E6 Ingestion/Edge Gateway, E7 Devices, E8 Production, E9 Quality, E10 Scrap, E11 Downtime, E12 Traceability (base), E13 Connectors (Odoo), E16 Dashboards, E18 Files/Media (básico) y E19 Audit (básico)**. En conjunto realizan el alcance canónico del MVP (brief §4) y los criterios de salida de la fase MVP del [roadmap](./roadmap.md) §2.5, probados por los [hitos](./milestones.md) M-MVP-01 a M-MVP-16.

| Fase | Épicas con historias | Foco |
|---|---|---|
| **MVP** ✅ | E1, E2, E3, E5, E6, E7, E8, E9, E10, E11, E12, E13, E16, E18, E19 | Captura + tiempo real + Odoo + multi-tenant |
| **V1** | E5, E6, E7, E9, E10, E12, E14, E15, E16, E17, E18, E19 | Reglas, notificaciones, protocolos, trazabilidad, reportes, RBAC |
| **V2** | E3, E4, E13, E16, E19, E6 | Marketplace, multi-ERP, analytics, feature flags, DBs distribuidas |
| **Enterprise** | E3, E4, E7, E9, E11, E13, E16, E17, E18, E20 | IA/visión, predictivo, gemelo digital, energía, SLAs, multi-región |

---

## Preguntas abiertas

1. **Estimación y capacidad.** Las historias aún no están estimadas ni asignadas a sprints; falta pasarlas por refinamiento con el equipo para dimensionar el MVP realista.
2. **Granularidad de "plataforma" como rol.** Varias historias tienen a "plataforma" como sujeto (comportamiento del sistema); ¿se modelan como historias técnicas/enablers o se reescriben desde un rol humano responsable?
3. **Historias de UX de operario.** ¿Cuánto detalle de la experiencia de tablet (offline, mínimos toques, guantes) se desglosa en historias propias vs. criterios de aceptación? Coordinar con [ui-ux.md](../specs/ui-ux.md).
4. **Captura de lote/serie en MVP (US-TRC-04).** Marcada ✅ como Should: ¿entra realmente al MVP para habilitar la genealogía de V1 sin backfill, o se pospone?
5. ✅ **Resuelto (2026-07-11):** el conector Odoo del MVP (US-INT-01/02) hace *pull* de MO/Producto/UoM/Motivos y *push* de producción real (avance/cierre de MO) y scrap (agregado por cierre de corrida); calidad opcional — ver [tablero de decisiones](../open-questions-board.md).
6. **Roles globales en historias.** ¿Falta detallar historias del Implementador (onboarding de clientes) más allá del alta técnica de tenant?
7. **Criterios de aceptación por historia.** Este backlog fija prioridad y fase; los criterios de aceptación detallados por historia se elaborarán junto con [milestones.md](./milestones.md) y los documentos de dominio.
8. **Definición de "Done".** ¿Qué exige la definición de terminado transversal (observabilidad, aislamiento, pruebas) para considerar una historia cerrada en cada fase?
