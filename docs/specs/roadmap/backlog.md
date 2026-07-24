# Nexo — Backlog inicial (épicas y user stories)

> **Documento:** `specs/roadmap/backlog.md` · **Estado:** Borrador v0.1 · **Actualizado:** 2026-07-13
> **Roles:** Product Manager · Software Architect · UX Designer
> **Relacionados:** [roadmap.md](./roadmap.md) · [milestones.md](./milestones.md) · [vision.md](./vision.md) · [idea.md](../idea.md) · [layered-architecture.md](../specs/layered-architecture.md) · [master-data.md](../specs/master-data.md) · [modules.md](../specs/modules.md) · [users-permissions.md](../specs/users-permissions.md) · [product.md](../specs/product.md)

## Resumen ejecutivo

Este documento contiene el **backlog inicial de producto** de Nexo, organizado por **épicas** (una por microservicio / bounded context canónico, brief §5.1) y desglosado en **user stories** con el formato *"Como &lt;rol&gt; quiero &lt;objetivo&gt; para &lt;beneficio&gt;"*. Cada historia lleva su **prioridad MoSCoW** y su **tag de fase** (MVP / V1 / V2 / Enterprise), de modo que el backlog sea directamente derivable del [roadmap](./roadmap.md) y verificable contra los [hitos](./milestones.md).

Las historias del **MVP** están marcadas de forma explícita (columna **MVP** con el ícono ✅) para separar sin ambigüedad el alcance mínimo viable —captura de Producción/Scrap/Calidad/Paradas/Eventos, **formularios de captura en tablet + datalogger vía carga de archivo/CSV/Excel**, **ejecución en ambos perfiles (lote y proyecto) con DAG completo de tareas**, tablero en tiempo real y multi-tenant DB-per-tenant (la **captura automática por protocolos industriales** —S7/OPC UA/Modbus/MQTT— y **toda la dimensión de costo** pasan a V1)— del resto de la evolución. El backlog cubre **todos los módulos** del brief, incluidos los servicios compartidos del Control Plane y los de fase futura (IA/visión), para dar una vista completa del producto aunque muchas historias sean posteriores al MVP.

> **🔺 Actualización 2026-07-13 — modelo por capas + ERP opcional.** La adopción del **modelo de 4 capas** (ver [layered-architecture.md](../specs/layered-architecture.md)) agrega **cinco épicas nuevas** —**E21 Digital Twin**, **E22 Work Model (Procesos)**, **E23 Execution**, **E24 Event Engine** y **E25 Master Data**— con historias en el MVP. Y cambia dos cosas de alcance:
> - El MVP suma **master data propia mínima** (E25): sin ella el sistema no puede operar sin ERP. Es el mayor sobrecosto del cambio.
> - Las historias del **conector Odoo (E13)** pasan a ser **opcionales y no bloqueantes**: aplican solo a tenants en **modo conectado** (reencuadre de INT-01 en el [tablero](../open-questions-board.md)).

> **🔺 Actualización 2026-07-13 — PRD-16, MOD-18 y MOD-17.** Tres decisiones cerradas reordenan las historias entre MVP y V1:
> - **PRD-16 — ambos perfiles en el MVP:** las historias de **perfil proyecto** de **E23** pasan a **MVP** (US-EXE-08), y se suman las del **compromiso del proyecto** —entregable, fecha objetivo y cliente como **atributos de la Ejecución**, no como catálogo de pedidos— y las de hitos y desvío (US-EXE-12/13). En **E16**, los **KPIs por perfil** (US-DSH-08) también pasan a MVP.
> - **MOD-18 — DAG completo en el MVP:** **E22** suma **tipos de precedencia** y **validación de ciclos** (US-WM-04 ampliada, US-WM-11). El **editor visual** del DAG (US-WM-08) se queda en V1.
> - **MOD-17 — master data mínima sin costo:** **todas las historias de costo pasan a V1** —centros de costo (US-MD-08), **tarifas con vigencia** (US-MD-11), **costo de insumos** (US-MD-12), **costo real vs. estimado** (US-EVT-10) y **costo del scrap** (US-SCR-03)—. El **importador CSV se acota** a unidades, productos, insumos y personas (US-MD-04) y entra **clientes (mínimo)** al MVP (US-MD-07). **El MVP mide tiempo y avance, no costo.**

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
| **E21** | [**Digital Twin (Capa 1)**](#e21--digital-twin-capa-1) | Por tenant | MVP |
| **E22** | [**Work Model / Procesos (Capa 2)**](#e22--work-model--procesos-capa-2) | Por tenant | MVP |
| **E23** | [**Execution / Ejecución (Capa 3)**](#e23--execution--ejecución-capa-3) | Por tenant | MVP |
| **E24** | [**Event Engine / Motor de eventos (Capa 4)**](#e24--event-engine--motor-de-eventos-capa-4) | Por tenant | MVP |
| **E25** | [**Master Data**](#e25--master-data) | Por tenant | MVP |

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
| US-SCR-03 | Como **Gerencia** quiero asociar un costo al scrap para cuantificar la pérdida económica. *(Movida a V1 por **MOD-17**: el MVP registra cantidad y motivo, sin valorizar.)* | Gerencia | Should | V1 | |
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

**Objetivo de la épica:** sincronización con ERPs vía Conectores + ACL, mapeos, jobs de sincronización y reintentos; evolución a multi-ERP. **Desde 2026-07-13 el ERP es un conector OPCIONAL:** estas historias aplican solo a tenants en **modo conectado** y **no bloquean** el cierre del MVP.

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-INT-01 | Como **Integraciones** quiero conectar Nexo con Odoo vía un conector desacoplado (ACL) para sincronizar sin acoplar el core. *(Opcional: solo modo conectado.)* | Integraciones | Should | MVP | ✅ |
| US-INT-02 | Como **Integraciones** quiero mapear el *pull* de MO/Producto/UoM/Motivos y el *push* de producción real y scrap entre Nexo y Odoo para alinear los modelos. *(Opcional: solo modo conectado.)* | Integraciones | Should | MVP | ✅ |
| US-INT-03 | Como **plataforma** quiero ejecutar jobs de sincronización con reintentos para tolerar fallos transitorios del ERP. | Integraciones | Should | MVP | ✅ |
| US-INT-04 | Como **Integraciones** quiero ver el estado de cada job de sincronización para diagnosticar problemas. | Integraciones | Should | MVP | ✅ |
| US-INT-07 | Como **Administrador del tenant** quiero **operar sin ningún ERP conectado**, con mi master data propia, para usar Nexo como sistema autónomo de ejecución. | Administrador | Must | MVP | ✅ |
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
| US-DSH-07 | Como **Supervisor** quiero visualizar en el tablero el **progreso, los tiempos muertos y el cuello de botella** de mis ejecuciones activas para actuar dentro del turno. | Supervisor | Must | MVP | ✅ |
| US-DSH-08 | Como **Producción** quiero que el tablero muestre los **KPIs correctos según el perfil** (OEE/scrap para repetitivo; % de avance, desvío e hitos para proyecto) para no comparar peras con manzanas. *(Movida a MVP por **PRD-16**; **sin indicadores de costo**, que llegan en V1.)* | Producción | Must | MVP | ✅ |
| US-DSH-09 | Como **Responsable de proyecto** quiero un tablero de mi **proyecto** con avance, hitos, desvío contra la fecha objetivo y cuello de botella para conducir el trabajo con hechos. | Producción | Should | MVP | ✅ |
| US-DSH-10 | Como **Gerencia** quiero ver en el tablero el **costo real vs. estimado** por ejecución para gestionar el margen. *(Diferida a V1 por **MOD-17**.)* | Gerencia | Should | V1 | |
| US-DSH-05 | Como **Gerencia** quiero comparativas y tendencias (cohortes, históricos) para análisis avanzado. | Gerencia | Must | V2 | |
| US-DSH-06 | Como **Gerencia** quiero **simular escenarios sobre el gemelo digital** de la línea (que ya refleja su estado real desde el MVP) para optimizar antes de decidir. | Gerencia | Should | Enterprise | |

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

## E21 · Digital Twin (Capa 1)

**Objetivo de la épica:** representación viva y consultable de la planta —Empresa → Planta → Sector → Línea → Centro de trabajo/Máquina— con **cada sensor/señal ligado a un activo**, su estado en vivo y sus formularios de captura. Es la capa base: ningún dato "flota". Ver [digital-twin.md](../specs/digital-twin.md).

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-TWIN-01 | Como **Administrador del tenant** quiero modelar la jerarquía física de mi empresa (planta, sector, línea, centro de trabajo/máquina) para representar mi planta en el sistema. | Administrador | Must | MVP | ✅ |
| US-TWIN-02 | Como **Integraciones** quiero **ligar cada señal/sensor a un activo** para que ningún dato quede sin dueño físico y pueda atribuirse a tareas y métricas. | Integraciones | Must | MVP | ✅ |
| US-TWIN-03 | Como **Administrador del tenant** quiero definir atributos y capacidades de cada activo (capacidad, unidad, tiempo de ciclo ideal) para contextualizar sus KPIs. | Administrador | Must | MVP | ✅ |
| US-TWIN-04 | Como **Supervisor** quiero navegar el gemelo digital y ver el **estado en vivo** de cada activo para saber qué está pasando sin recorrer la planta. | Supervisor | Should | MVP | ✅ |
| US-TWIN-05 | Como **Operario** quiero un **formulario de captura** asociado al activo donde trabajo para ingresar datos sin buscar dónde cargarlos. | Operario | Must | MVP | ✅ |
| US-TWIN-06 | Como **Mantenimiento** quiero registrar la calibración y la ubicación de un sensor para confiar en sus lecturas. | Mantenimiento | Should | V1 | |
| US-TWIN-07 | Como **Calidad** quiero asociar una cámara a un activo y capturar snapshots como evidencia para documentar lo que pasó. | Calidad | Could | V1 | |
| US-TWIN-08 | Como **Producción** quiero simular escenarios sobre el gemelo digital (qué pasa si cambio la asignación) para optimizar antes de decidir. | Producción | Should | Enterprise | |

---

## E22 · Work Model / Procesos (Capa 2)

**Objetivo de la épica:** modelar **cómo se hace el trabajo** con plantillas versionadas: Procesos, Tareas (**DAG completo desde el MVP**, MOD-18: ramas paralelas, tipos de precedencia y validación de ciclos), Insumos, roles responsables y tiempos estándar. Una producción repetitiva y un proyecto único **se modelan igual**: cambia el **perfil**, no el modelo — y **ambos perfiles entran en el MVP** (PRD-16). Ver [work-model.md](../specs/work-model.md).

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-WM-01 | Como **Producción** quiero definir un **Proceso** (plantilla de trabajo) con su nombre, perfil y versión para estandarizar cómo se hace el trabajo. | Producción | Must | MVP | ✅ |
| US-WM-02 | Como **Producción** quiero descomponer un Proceso en **Tareas** con duración estimada/estándar y rol responsable para saber quién hace qué y en cuánto tiempo. | Producción | Must | MVP | ✅ |
| US-WM-03 | Como **Producción** quiero declarar los **Insumos** que consume cada tarea (cantidad y unidad) para conocer el consumo esperado. | Producción | Must | MVP | ✅ |
| US-WM-04 | Como **Producción** quiero definir **precedencias entre tareas** con **ramas paralelas** y **tipos de precedencia** (fin-inicio, inicio-inicio, fin-fin) para expresar el orden real del trabajo. *(DAG completo, MOD-18.)* | Producción | Must | MVP | ✅ |
| US-WM-05 | Como **Calidad** quiero marcar **evidencia requerida** y un punto de control de calidad en una tarea para que no se cierre sin la prueba correspondiente. | Calidad | Should | MVP | ✅ |
| US-WM-06 | Como **Producción** quiero **versionar** un Proceso y publicar versiones para mejorar el método sin alterar el historial. | Producción | Should | MVP | ✅ |
| US-WM-07 | Como **Producción** quiero marcar el **perfil** del Proceso (repetitivo o proyecto) para que el sistema aplique el disparador y los KPIs correctos. | Producción | Must | MVP | ✅ |
| US-WM-11 | Como **Producción** quiero que el sistema **valide que el grafo de tareas no tenga ciclos** (y me indique dónde está el ciclo) para no publicar un proceso imposible de ejecutar. *(MOD-18.)* | Producción | Must | MVP | ✅ |
| US-WM-12 | Como **Producción** quiero que el **progreso y la ruta crítica se calculen sobre el DAG** (respetando ramas paralelas y convergencias) para que el avance refleje el trabajo real y no un conteo lineal. | Producción | Must | MVP | ✅ |
| US-WM-08 | Como **Producción** quiero editar el **grafo de tareas (DAG) en forma visual** para modelar procesos complejos con convergencias y paralelismo. *(El modelo de DAG completo ya está en el MVP; acá entra la UI.)* | Producción | Should | V1 | |
| US-WM-09 | Como **Producción** quiero definir el **criterio de terminación** de cada tarea para que "hecho" signifique lo mismo para todos. | Producción | Should | V1 | |
| US-WM-10 | Como **Administrador del tenant** quiero reutilizar y clonar Procesos entre plantas para no rehacer el modelado en cada sitio. | Administrador | Could | V2 | |

---

## E23 · Execution / Ejecución (Capa 3)

**Objetivo de la épica:** instanciar un Proceso y **ejecutarlo**: Ejecución (Run) en su sabor **Lote** o **Proyecto**, con tareas instanciadas, asignación, estados, consumo real, avance y evidencia. **Desde PRD-16 (2026-07-13) los dos sabores entran en el MVP**; el **compromiso del proyecto** (entregable, fecha objetivo, cliente) es **atributo de la Ejecución**, no un catálogo. El **consumo real se registra en cantidades, sin valorización** — el costo es V1 (MOD-17). Ver [execution.md](../specs/execution.md).

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-EXE-01 | Como **Supervisor** quiero lanzar una **Ejecución** a partir de un Proceso (disparada por una orden, un plan o el stock) para poner el trabajo en marcha. | Supervisor | Must | MVP | ✅ |
| US-EXE-02 | Como **plataforma** quiero **instanciar las tareas** del Proceso al crear la Ejecución para que cada una tenga estado, responsable y tiempos propios. | Supervisor | Must | MVP | ✅ |
| US-EXE-03 | Como **Supervisor** quiero **asignar responsables** a las tareas de una Ejecución para que cada operario sepa qué le toca. | Supervisor | Must | MVP | ✅ |
| US-EXE-04 | Como **Operario** quiero **iniciar y terminar una tarea** desde la tablet, adjuntando la evidencia requerida, para reportar mi avance en el momento. | Operario | Must | MVP | ✅ |
| US-EXE-05 | Como **Operario** quiero registrar el **consumo real de insumos** de una tarea para reflejar lo que realmente se usó. | Operario | Should | MVP | ✅ |
| US-EXE-06 | Como **Supervisor** quiero ver el **ciclo de vida y el estado** de una Ejecución (planificada, en curso, pausada, cerrada) para gobernar el trabajo en piso. | Supervisor | Must | MVP | ✅ |
| US-EXE-07 | Como **Supervisor** quiero **cerrar una Ejecución** (total o parcialmente) dejando registro del motivo para reflejar lo que efectivamente se completó. | Supervisor | Must | MVP | ✅ |
| US-EXE-08 | Como **Producción** quiero lanzar una Ejecución de perfil **Proyecto** con entregable único, fecha objetivo e **hitos** para gestionar trabajo a medida. *(Movida a MVP por **PRD-16**.)* | Producción | Must | MVP | ✅ |
| US-EXE-12 | Como **Responsable de proyecto** quiero registrar el **compromiso del proyecto** —entregable, **fecha objetivo** y **cliente**— como **atributos de la Ejecución** (sin depender de un catálogo de pedidos ni de un ERP) para saber a qué me comprometí y con quién. *(MOD-17: el pedido no es catálogo.)* | Producción | Must | MVP | ✅ |
| US-EXE-13 | Como **Responsable de proyecto** quiero ver el **desvío contra la fecha objetivo** (avance real vs. plan) de mi proyecto para reaccionar antes de incumplir el compromiso. | Producción | Should | MVP | ✅ |
| US-EXE-14 | Como **Responsable de proyecto** quiero marcar **hitos** sobre tareas del DAG y ver su cumplimiento para comunicar el estado al cliente con hechos. | Producción | Should | MVP | ✅ |
| US-EXE-09 | Como **Supervisor** quiero **reprogramar** tareas y ejecuciones (mover fechas, reasignar) para responder a los imprevistos del turno. | Supervisor | Should | V1 | |
| US-EXE-15 | Como **Responsable de proyecto** quiero un **cronograma editable** y **ruta crítica avanzada** sobre el proyecto para replanificar cuando cambian las condiciones. | Producción | Should | V1 | |
| US-EXE-10 | Como **plataforma** quiero que cada Ejecución quede **atada a la versión del Proceso** con la que arrancó para preservar la coherencia histórica. | Producción | Should | MVP | ✅ |
| US-EXE-11 | Como **Producción** quiero que el sistema **sugiera la reprogramación** ante un desvío detectado para reaccionar antes de perder la fecha. | Producción | Could | Enterprise | |

---

## E24 · Event Engine / Motor de eventos (Capa 4)

**Objetivo de la épica:** observar las tres capas inferiores y **derivar el dato de verdad**. Define el contrato del evento (**fecha, origen, valor, evidencia** + atribución) y calcula **progreso, cuellos de botella y tiempos muertos**. No duplica ingesta ([data-ingestion.md](../specs/data-ingestion.md)), almacenamiento ([traceability.md](../specs/traceability.md)), automatizaciones ([rules-engine.md](../specs/rules-engine.md)) ni visualización ([dashboards.md](../specs/dashboards.md)). Ver [event-engine.md](../specs/event-engine.md).

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-EVT-01 | Como **plataforma** quiero que **todo** genere eventos (sensor, cámara, operario, sistema) con fecha, origen, valor y evidencia para tener una única unidad de verdad. | Producción | Must | MVP | ✅ |
| US-EVT-02 | Como **plataforma** quiero **atribuir cada evento** a su activo, tarea y ejecución para poder derivar métricas por recurso y por trabajo. | Producción | Must | MVP | ✅ |
| US-EVT-03 | Como **Supervisor** quiero ver el **progreso** de una Ejecución calculado como tareas completadas **ponderadas** para saber el avance real y no un conteo engañoso. | Supervisor | Must | MVP | ✅ |
| US-EVT-04 | Como **Producción** quiero identificar el **cuello de botella** (recurso o tarea con mayor cola/espera acumulada) para saber dónde intervenir. | Producción | Must | MVP | ✅ |
| US-EVT-05 | Como **Producción** quiero detectar **tiempos muertos** (intervalos sin eventos productivos dentro de la ventana planificada) para recuperar capacidad perdida. | Producción | Must | MVP | ✅ |
| US-EVT-06 | Como **Calidad** quiero adjuntar **evidencia** (foto, archivo, lectura, firma) a un evento y consultarla después para sostener la trazabilidad. | Calidad | Should | MVP | ✅ |
| US-EVT-07 | Como **Gerencia** quiero medir la **productividad por recurso** de una ejecución para gestionar por datos. | Gerencia | Should | V1 | |
| US-EVT-10 | Como **Gerencia** quiero medir el **costo real vs. estimado** de una ejecución y de cada tarea (aplicando tarifas con vigencia y costo de insumos) para saber si el trabajo dejó margen. *(Diferida a V1 por **MOD-17**: el MVP mide tiempo y avance, no costo.)* | Gerencia | Must | V1 | |
| US-EVT-08 | Como **Producción** quiero que las métricas derivadas se recalculen ante eventos tardíos (store-and-forward) sin romper la coherencia histórica. | Producción | Should | V1 | |
| US-EVT-09 | Como **Gerencia** quiero que el motor **anticipe** desvíos de progreso y cuellos de botella a partir del histórico para actuar antes de que ocurran. | Gerencia | Could | Enterprise | |

---

## E25 · Master Data

**Objetivo de la épica:** catálogos propios que permiten operar **sin ERP** (modo *standalone*) y que se **sincronizan** cuando hay ERP (modo *conectado*). Es la consecuencia obligatoria del ERP opcional. **Acotada por MOD-17 (2026-07-13) a un mínimo SIN COSTO**: entran unidades, productos/ítems, procesos (con DAG, ver E22), personas y roles, **insumos sin costo** y **clientes (mínimo)**; el **importador CSV cubre solo unidades, productos, insumos y personas**. **Centros de costo, tarifas con vigencia y costo de insumos se difieren a V1.** Ver [master-data.md](../specs/master-data.md).

| ID | User story | Rol | MoSCoW | Fase | MVP |
|---|---|---|---|---|---|
| US-MD-01 | Como **Administrador del tenant** quiero administrar mi catálogo de **productos/ítems** dentro de Nexo para operar sin depender de un ERP. | Administrador | Must | MVP | ✅ |
| US-MD-02 | Como **Administrador del tenant** quiero administrar **insumos (sin costo)** y **unidades de medida** (con factores de conversión) para modelar consumos en cantidades. *(El costo del insumo es V1, MOD-17.)* | Administrador | Must | MVP | ✅ |
| US-MD-03 | Como **Administrador del tenant** quiero administrar **personas y roles** de planta para asignar responsables a tareas sin sincronizar con nada externo. | Administrador | Must | MVP | ✅ |
| US-MD-04 | Como **Administrador del tenant** quiero **importar por CSV** los catálogos de **unidades, productos, insumos y personas** —con validación previa y reporte de errores por fila— para cargar mis datos maestros el primer día sin tipear todo. *(Alcance acotado por **MOD-17**: el resto se carga por ABM.)* | Administrador | Must | MVP | ✅ |
| US-MD-11 | Como **Administrador del tenant** quiero **descargar una plantilla CSV por catálogo** (unidades, productos, insumos, personas) para preparar mis datos en el formato correcto y no fallar en la importación. | Administrador | Should | MVP | ✅ |
| US-MD-12 | Como **Administrador del tenant** quiero que la importación CSV sea **reintentable e idempotente** (que reimportar el mismo archivo no duplique registros) para corregir y volver a cargar sin ensuciar el catálogo. | Administrador | Should | MVP | ✅ |
| US-MD-05 | Como **Administrador del tenant** quiero declarar el **modo de operación** de mi empresa (*standalone* o *conectado*) para saber qué catálogos edito acá y cuáles llegan del ERP. | Administrador | Must | MVP | ✅ |
| US-MD-06 | Como **plataforma** quiero cargar un **seed idempotente y versionado** de catálogos por defecto (motivos, roles, unidades, turnos) para que el tenant opere desde el alta. | Administrador | Must | MVP | ✅ |
| US-MD-07 | Como **Administrador del tenant** quiero administrar un catálogo **mínimo de clientes** (nombre e identificación) para atribuir a un cliente el compromiso de una Ejecución de perfil proyecto sin depender de un ERP. *(Movida a MVP por **MOD-17**; el **pedido no es catálogo**: es atributo de la Ejecución, ver US-EXE-12.)* | Administrador | Must | MVP | ✅ |
| US-MD-13 | Como **Administrador del tenant** quiero enriquecer el catálogo de **clientes** (contactos, condiciones) y administrar **pedidos** cuando mi negocio los requiera, para gestionar la demanda dentro de Nexo. | Administrador | Should | V1 | |
| US-MD-08 | Como **Gerencia** quiero administrar **centros de costo** para imputar el costo real de las ejecuciones. *(Diferida a V1 por **MOD-17**.)* | Gerencia | Must | V1 | |
| US-MD-14 | Como **Gerencia** quiero administrar **tarifas de personas y recursos con vigencia por fecha** para valorizar el tiempo trabajado con el precio correcto de cada período. *(Diferida a V1 por **MOD-17**.)* | Gerencia | Must | V1 | |
| US-MD-15 | Como **Gerencia** quiero cargar el **costo de los insumos** (con vigencia) para valorizar el consumo real de las ejecuciones. *(Diferida a V1 por **MOD-17**.)* | Gerencia | Must | V1 | |
| US-MD-09 | Como **Integraciones** quiero que, al conectar un ERP, una **conciliación asistida** enlace mis catálogos locales con los del ERP (sin duplicar ni borrar) para migrar de standalone a conectado sin perder datos. | Integraciones | Must | V1 | |
| US-MD-10 | Como **Integraciones** quiero configurar la **fuente de verdad por entidad** (Nexo o ERP) para resolver quién manda sobre cada catálogo. | Integraciones | Should | V1 | |

---

## 2. Resumen del alcance MVP (historias marcadas ✅)

El MVP queda cubierto por las historias ✅ de las épicas **E1 Identity & Access, E2 Tenant Provisioning, E3 Administration & Licensing, E5 Observability (mínima), E6 Ingestion/Edge Gateway, E7 Devices, E8 Production, E9 Quality, E10 Scrap, E11 Downtime, E12 Traceability (base), E13 Connectors (Odoo, **opcional**), E16 Dashboards, E18 Files/Media (básico), E19 Audit (básico)** y las **cinco épicas del modelo por capas: E21 Digital Twin, E22 Work Model, E23 Execution, E24 Event Engine y E25 Master Data**. En conjunto realizan el alcance canónico del MVP (brief §4 + modelo por capas del 2026-07-13 + **PRD-16, MOD-18 y MOD-17**) y los criterios de salida de la fase MVP del [roadmap](./roadmap.md) §2.5, probados por los [hitos](./milestones.md) M-MVP-01 a M-MVP-16 (que requieren revisión para incorporar las capas nuevas, **el perfil proyecto y el DAG completo**).

| Fase | Épicas con historias | Foco |
|---|---|---|
| **MVP** ✅ | E1, E2, E3, E5, E6, E7, E8, E9, E10, E11, E12, E13*, E16, E18, E19, **E21, E22, E23, E24, E25** | 4 capas mínimas + **ambos perfiles (lote y proyecto)** + **DAG completo** + master data mínima **sin costo** + CSV acotado + captura + tiempo real + multi-tenant (*Odoo opcional). **Mide tiempo y avance, no costo.** |
| **V1** | E5, E6, E7, E9, E10, E12, E14, E15, E16, E17, E18, E19, **E21, E22, E23, E24, E25** | Reglas, notificaciones, protocolos, trazabilidad, reportes, RBAC, **toda la capa de costo (centros de costo, tarifas con vigencia, costo de insumos, costo real vs. estimado, costo del scrap)**, **DAG visual**, cronograma/reprogramación del proyecto y conciliación con ERP |
| **V2** | E3, E4, E6, E13, E16, E19, **E22** | Marketplace, multi-ERP, analytics, feature flags, DBs distribuidas, reutilización de procesos entre plantas |
| **Enterprise** | E3, E4, E7, E9, E11, E13, E16, E17, E18, E20, **E21, E23, E24** | IA/visión, predictivo, simulación sobre el gemelo digital, energía, SLAs, multi-región |

**Prioridad relativa dentro del MVP (orden de dependencia de capas):** **E25 Master Data** y **E21 Digital Twin** habilitan **E22 Work Model** (con el **DAG completo**), que habilita **E23 Execution** (en **ambos perfiles**), que alimenta **E24 Event Engine**, que alimenta **E16 Dashboards**. Ninguna de ellas depende de **E13 Connectors**.

> **Nota de alcance (2026-07-13).** El MVP **creció** con **PRD-16** (perfil proyecto y su compromiso: US-EXE-08/12/13/14, US-MD-07, US-DSH-08/09) y **MOD-18** (DAG completo: US-WM-04 ampliada, US-WM-11/12), y **se recortó** con **MOD-17** (salen a V1: US-MD-08/14/15, US-EVT-10, US-SCR-03, US-DSH-10; y el importador CSV se acota a cuatro catálogos). **El recorte de costo es la compensación del crecimiento, no un ahorro adicional.** Consecuencia práctica: ninguna historia del MVP produce un número en dinero.

---

## Preguntas abiertas

1. **Estimación y capacidad.** Las historias aún no están estimadas ni asignadas a sprints; falta pasarlas por refinamiento con el equipo para dimensionar el MVP realista.
2. **Granularidad de "plataforma" como rol.** Varias historias tienen a "plataforma" como sujeto (comportamiento del sistema); ¿se modelan como historias técnicas/enablers o se reescriben desde un rol humano responsable?
3. **Historias de UX de operario.** ¿Cuánto detalle de la experiencia de tablet (offline, mínimos toques, guantes) se desglosa en historias propias vs. criterios de aceptación? Coordinar con [ui-ux.md](../specs/ui-ux.md).
4. **Captura de lote/serie en MVP (US-TRC-04).** Marcada ✅ como Should: ¿entra realmente al MVP para habilitar la genealogía de V1 sin backfill, o se pospone?
5. ♻️ **Resuelto (2026-07-11), reencuadrado (2026-07-13):** el conector Odoo del MVP (US-INT-01/02) hace *pull* de MO/Producto/UoM/Motivos y *push* de producción real (avance/cierre de MO) y scrap (agregado por cierre de corrida); calidad opcional. **Esas historias bajan a `Should` y aplican solo al modo conectado** — ver [tablero de decisiones](../open-questions-board.md).
6. **Roles globales en historias.** ¿Falta detallar historias del Implementador (onboarding de clientes) más allá del alta técnica de tenant?
7. **Criterios de aceptación por historia.** Este backlog fija prioridad y fase; los criterios de aceptación detallados por historia se elaborarán junto con [milestones.md](./milestones.md) y los documentos de dominio.
8. **Definición de "Done".** ¿Qué exige la definición de terminado transversal (observabilidad, aislamiento, pruebas) para considerar una historia cerrada en cada fase?
9. ✅ **Resuelto (2026-07-13) — MOD-17:** el MVP de E25 queda en **US-MD-01 a US-MD-07 + US-MD-11/12** (mínimo **sin costo** + CSV acotado a unidades/productos/insumos/personas + clientes mínimo); **US-MD-08, US-MD-13, US-MD-14 y US-MD-15 pasan a V1**, junto con US-EVT-10, US-SCR-03 y US-DSH-10 — ver el [tablero](../open-questions-board.md).
10. ✅ **Resuelto (2026-07-13) — PRD-16 y MOD-18:** **US-EXE-08 pasa al MVP** con el compromiso del proyecto (US-EXE-12), desvío (US-EXE-13) e hitos (US-EXE-14); el **DAG completo** entra con US-WM-04 ampliada y US-WM-11/12. El **editor visual del DAG (US-WM-08)** y el **cronograma editable (US-EXE-15)** siguen en V1. El piloto ya puede ser de cualquier perfil.
11. **Solapamiento E8 Production ↔ E22/E23.** La Orden de producción pasa a ser un **disparador** de una Ejecución: ¿algunas historias de E8 (US-PROD-01/03/06) se reescriben como historias de E22/E23 o conviven como la vista de dominio del perfil repetitivo?
12. **Solapamiento E24 Event Engine ↔ E12/E16.** El motor de eventos define el contrato y las métricas; el Event Store persiste y Dashboards visualiza. ¿La frontera queda clara en las historias o hay que fusionar alguna?
13. **Solapamiento E21 Digital Twin ↔ E7 Devices.** El hardware se modela en E7 y el gemelo (jerarquía + binding señal↔activo) en E21: ¿US-DEV-03 se retira por quedar cubierta por US-TWIN-02?
14. **Estimación del intercambio de alcance.** El crecimiento del MVP (ambos perfiles + DAG completo) se compensa con el recorte de costo, pero **falta cuantificarlo en refinamiento**: si no cierra, la palanca a reabrir es **PRD-16**, no MOD-18.
15. **Nivel de detalle del cliente en el MVP (US-MD-07).** ¿Alcanza con nombre e identificación, o un piloto de perfil proyecto va a exigir contactos y condiciones ya en la primera entrega?
16. **Consumo de insumos sin costo (US-EXE-05).** El MVP registra cantidades; ¿se guarda algo más (proveedor, lote del insumo) para que la valorización de V1 no exija recarga?
