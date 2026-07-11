# Glosario — Plataforma "Nexo"

> **Documento:** `specs/specs/glossary.md` · **Estado:** Borrador v0.1 · **Actualizado:** 2026-07-11
> **Roles:** Product Manager · Software Architect · UX Designer
> **Relacionados:** [architecture.md](./architecture.md) · [data-model.md](./data-model.md) · [data-ingestion.md](./data-ingestion.md) · [devices.md](./devices.md) · [multi-tenancy.md](./multi-tenancy.md) · [dashboards.md](./dashboards.md) · [open-questions.md](./open-questions.md) · [future-features.md](./future-features.md) · [roadmap](../roadmap/roadmap.md) · [idea](../idea.md)

## Resumen ejecutivo

Este documento es el **diccionario único de términos** de la plataforma "Nexo": una capa de captura de datos industriales / MES ligero que se ubica entre el mundo físico de la planta (operarios, máquinas, dispositivos, sensores) y los sistemas de gestión (ERP). Su objetivo es que todos los roles del proyecto —negocio, arquitectura, UX, integraciones y operación— usen **exactamente el mismo vocabulario**, tanto para conceptos industriales de dominio (OEE, SPC, MTBF, takt time) como para conceptos de la plataforma (Tenant, Control Plane, DB-per-tenant, CQRS, ACL).

El glosario está ordenado **alfabéticamente** y se presenta en una tabla de tres columnas: **Término**, **Definición** y **Notas / relación** (con enlaces a los documentos donde el término se desarrolla en profundidad). Sirve como puente entre la jerga de planta y la jerga de software, reduce ambigüedades en las conversaciones con clientes industriales y evita divergencias de naming entre microservicios y documentos.

Se incluye además una sección dedicada a las **fórmulas de KPI canónicas** (OEE y sus factores, Scrap Rate, FPY, MTBF, MTTR). Estas fórmulas son la **fuente de verdad de cálculo** y deben usarse idénticas en [dashboards.md](./dashboards.md), [production.md](./production.md), [downtime.md](./downtime.md) y [quality.md](./quality.md). Cualquier término nuevo que aparezca en otros documentos debe darse de alta primero aquí.

> **Convención:** cuando un término tiene sinónimo industrial y nombre de entidad canónica, se muestran juntos (p. ej. "Centro de trabajo / Máquina (Work Center / Asset)"). Los nombres de microservicios y entidades siguen la lista canónica del brief de fundamentos.

---

## 1. Cómo leer este glosario

- **Término:** entrada principal (con sinónimos y sigla en inglés entre paréntesis cuando aplica).
- **Definición:** explicación funcional en el contexto de "Nexo", no una definición genérica de enciclopedia.
- **Notas / relación:** vínculos con otros términos, con entidades canónicas, con microservicios y con los documentos donde el concepto se especifica.
- Los términos marcados como **(fase futura)** pertenecen a la etapa Enterprise del [roadmap](../roadmap/roadmap.md) y se detallan en [future-features.md](./future-features.md).

---

## 2. Glosario alfabético

| Término | Definición | Notas / relación |
|---------|------------|------------------|
| **ABAC (Attribute-Based Access Control)** | Modelo de autorización basado en atributos (usuario, recurso, contexto, planta, línea) que complementa a RBAC para reglas finas. | Extiende **RBAC** con alcance por planta/línea. Ver [users-permissions.md](./users-permissions.md). |
| **ACL (Anti-Corruption Layer)** | Capa de traducción que aísla el modelo interno de "Nexo" del modelo de sistemas externos (ERP), evitando que conceptos ajenos "contaminen" el dominio. | Patrón central de **Conectores**. El core nunca depende de un ERP. Ver [integrations.md](./integrations.md), [architecture.md](./architecture.md). |
| **Alerta / Alarma (Alert)** | Condición notificable disparada por una regla o por el cruce de un umbral (p. ej. temperatura fuera de rango, parada prolongada). | Producida por el **Rules Engine**; entregada como **Notificación**. Ver [rules-engine.md](./rules-engine.md), [notifications.md](./notifications.md). |
| **Andon** | Sistema visual/sonoro de aviso en planta que señala anomalías o solicitudes de asistencia en tiempo real (torres de luz, tableros). | Caso de uso de **Notifications** + tableros; ampliación (fase futura). Ver [notifications.md](./notifications.md), [future-features.md](./future-features.md). |
| **API Gateway** | Punto de entrada único que enruta, autentica y limita el tráfico hacia los microservicios. | Principio de arquitectura; trabaja con **Identity & Access**. Ver [architecture.md](./architecture.md). |
| **Archivo / Media (File)** | Evidencia asociada a un evento o inspección: foto, adjunto, documento. | Entidad canónica; storage aislado por tenant (servicio **Files / Media**). Ver [quality.md](./quality.md), [multi-tenancy.md](./multi-tenancy.md). |
| **Backpressure** | Mecanismo que regula el ritmo de ingesta cuando un consumidor no puede seguir el ritmo del productor, evitando saturación. | Estrategia de escalabilidad junto con **Store-and-forward** y broker. Ver [scalability.md](./scalability.md), [data-ingestion.md](./data-ingestion.md). |
| **Bounded Context** | Frontera lógica de un dominio dentro de DDD; cada microservicio de "Nexo" es un bounded context con su lenguaje ubicuo. | Base de la lista canónica de microservicios. Ver [architecture.md](./architecture.md), [modules.md](./modules.md). |
| **Broker de mensajería** | Middleware asíncrono (tipo Kafka/RabbitMQ, tech-agnóstico) que transporta eventos entre servicios; columna vertebral event-driven. | Sostiene **Event-driven**, **CQRS** y picos de eventos. Ver [architecture.md](./architecture.md), [scalability.md](./scalability.md). |
| **Calidad (factor de OEE)** | Proporción de piezas buenas sobre el total producido; uno de los tres factores del **OEE**. | Fórmula en §3. No confundir con el módulo **Quality**. Ver [dashboards.md](./dashboards.md), [quality.md](./quality.md). |
| **Centro de trabajo / Máquina (Work Center / Asset)** | Recurso productivo donde se ejecutan operaciones; puede tener dispositivos y sensores asociados. | Entidad canónica; jerarquía Planta→Sector→Línea→Máquina. Ver [data-model.md](./data-model.md), [production.md](./production.md). |
| **Cloud Native** | Enfoque de diseño para la nube: microservicios, contenedores, escalado horizontal, resiliencia y despliegue independiente. | Principio de arquitectura #1. Ver [architecture.md](./architecture.md). |
| **Conector (Connector)** | Integración con un sistema externo/ERP que sincroniza datos a través de la **ACL**. | Entidad canónica; servicio **Connectors / Integrations**; catálogo en **Marketplace**. Ver [integrations.md](./integrations.md). |
| **Control Plane** | Plano de control del proveedor: base global y servicios que gestionan tenants, planes, licencias, feature flags, marketplace, observabilidad y métricas. **Nunca** almacena datos operativos de clientes. | Contraparte del plano de datos (DB por tenant). Ver [control-plane.md](./control-plane.md), [multi-tenancy.md](./multi-tenancy.md). |
| **CQRS (Command Query Responsibility Segregation)** | Patrón que separa el modelo de escritura (comandos) del de lectura (**Read model**), habilitando dashboards y reportes en tiempo real. | Principio #6; alimenta **Dashboards** y **Reports**. Ver [architecture.md](./architecture.md), [dashboards.md](./dashboards.md). |
| **Cycle time (Tiempo de ciclo)** | Tiempo real que tarda producirse una pieza/unidad en una operación. El **tiempo de ciclo ideal** es el óptimo teórico usado en el factor **Rendimiento**. | Insumo de OEE (Rendimiento). Comparar con **Takt time**. Ver [production.md](./production.md). |
| **Datalogger** | Dispositivo que registra lecturas de sensores/señales en el tiempo, con o sin conexión permanente. | Fuente de datos canónica del MVP; entidad **Dispositivo**. Ver [devices.md](./devices.md), [data-ingestion.md](./data-ingestion.md). |
| **DB-per-tenant (Base de datos por tenant)** | Modelo de multi-tenancy elegido: cada empresa/tenant tiene su **propia base de datos** aislada. Requisito **NO negociable**. | Inspirado en el proyecto Hexa; aísla datos, habilita distribución geográfica. Ver [multi-tenancy.md](./multi-tenancy.md). |
| **DDD (Domain-Driven Design)** | Metodología de diseño orientada al dominio: lenguaje ubicuo, bounded contexts, agregados. | Guía la partición en microservicios. Ver [architecture.md](./architecture.md). |
| **Dedup key (Clave de deduplicación)** | Identificador que permite descartar eventos/lecturas duplicados producidos por reintentos o store-and-forward. | Campo del **Evento canónico**. Ver [data-ingestion.md](./data-ingestion.md), [data-model.md](./data-model.md). |
| **Defecto (Defect)** | No conformidad detectada durante una inspección o en proceso. | Entidad canónica; asociado a **Motivo (Reason Code)**. Ver [quality.md](./quality.md). |
| **Disponibilidad (Availability)** | Factor de OEE: proporción del tiempo productivo planificado en que la máquina estuvo efectivamente operando. | Fórmula en §3; depende de **Paradas**. Ver [downtime.md](./downtime.md), [dashboards.md](./dashboards.md). |
| **Dispositivo (Device)** | Hardware de captura de datos: PLC, ESP32, Arduino, Raspberry Pi, datalogger, gateway, cámara. | Entidad canónica; gestionado por el servicio **Devices** (salud, firmware/**OTA**). Ver [devices.md](./devices.md). |
| **Edge / Agente Edge** | Ver **Gateway / Edge**. | — |
| **ERP (Enterprise Resource Planning)** | Sistema de gestión empresarial (compras, ventas, inventario, finanzas, MRP). "Nexo" lo complementa, no lo reemplaza; primer ERP soportado: **Odoo**. | Integrado vía **Conectores + ACL**; agnóstico de ERP. Ver [integrations.md](./integrations.md), [product.md](./product.md). |
| **ESP32 / Arduino / Raspberry Pi** | Microcontroladores y microcomputadores usados como dispositivos de captura de bajo costo en planta. | Fuentes de datos canónicas; entidad **Dispositivo**. Ver [devices.md](./devices.md). |
| **Evento (Event)** | **Unidad normalizada canónica** del sistema. Todo dato de planta se convierte en un evento inmutable con esquema común. | Corazón del modelo; producido por **Ingestion**, persistido por **Traceability / Event Store**. Esquema en §4 de [data-model.md](./data-model.md). Ver [data-ingestion.md](./data-ingestion.md). |
| **Event-driven (Orientado a eventos)** | Estilo de arquitectura donde los servicios se comunican mediante eventos asíncronos a través de un broker. | Principio #2; habilita desacople y escalado. Ver [architecture.md](./architecture.md). |
| **Feature Flag** | Interruptor de configuración que activa/desactiva funcionalidades por tenant, plan o despliegue progresivo, sin re-desplegar. | Gestionado por **Administration & Licensing** en Control Plane. Ver [control-plane.md](./control-plane.md). |
| **FPY (First Pass Yield / Rendimiento a la primera)** | Proporción de piezas conformes obtenidas **a la primera** sin retrabajo, sobre el total ingresado. | KPI de calidad; fórmula en §3. Ver [quality.md](./quality.md). |
| **Gateway / Edge (Agente Edge)** | Componente on-premise que conecta PLC/OPC UA/Modbus con la nube mediante conexión **outbound**, con **store-and-forward** ante cortes. | Enfoque edge-first (principio #4); servicio **Ingestion / Edge Gateway**. Ver [data-ingestion.md](./data-ingestion.md), [devices.md](./devices.md). |
| **Gemelo digital (Digital Twin)** | Réplica virtual de una línea/máquina alimentada con datos en tiempo real para simulación y análisis. **(fase futura)** | Fuera del MVP; fase Enterprise. Ver [future-features.md](./future-features.md), [roadmap](../roadmap/roadmap.md). |
| **Genealogía / Trazabilidad (Traceability)** | Capacidad de reconstruir la historia completa de un lote/serie: insumos, procesos, controles, eventos y responsables (aguas arriba y aguas abajo). | Servicio **Traceability / Event Store**; historial inmutable. Ver [traceability.md](./traceability.md). |
| **gRPC / REST** | Protocolos de comunicación **síncrona** interna entre microservicios (complementan la comunicación asíncrona por eventos). | Principio #7. Ver [architecture.md](./architecture.md). |
| **Historian (Historiador)** | Base de datos especializada en series temporales de proceso. "Nexo" **no es** un historiador puro, aunque usa almacenamiento time-series para lecturas. | Contraste de categoría; ver **Lectura**, **Time-series**. Ver [product.md](./product.md). |
| **Identity & Access** | Microservicio compartido de autenticación/autorización, usuarios, SSO y tokens con **claim de tenant**. | Emite el **JWT** con `tenant_id`. Ver [users-permissions.md](./users-permissions.md), [security.md](./security.md). |
| **Ingestion / Edge Gateway** | Microservicio que recibe datos, aplica adapters de protocolo y **normaliza** todo a **Evento canónico**. | Procesa por tenant; punto de entrada del dato. Ver [data-ingestion.md](./data-ingestion.md). |
| **JWT (JSON Web Token)** | Token firmado que transporta la identidad y el **claim `tenant_id`** para resolver el tenant y autorizar. | Base de la **Resolución de tenant**. Ver [security.md](./security.md), [multi-tenancy.md](./multi-tenancy.md). |
| **Lectura (Reading)** | Muestra puntual del valor de una **Señal/Tag** en un instante (temperatura, contador, estado). | Entidad canónica; se almacena en time-series y puede generar **Eventos**. Ver [data-model.md](./data-model.md), [devices.md](./devices.md). |
| **Línea (Line)** | Línea de producción dentro de un sector de una planta. | Entidad canónica; nivel de la jerarquía y de **scoping** de permisos. Ver [data-model.md](./data-model.md). |
| **Lote / Batch (Lot)** | Conjunto de unidades producidas juntas bajo condiciones homogéneas; unidad de trazabilidad. | Entidad canónica; base de **Genealogía**. Ver [traceability.md](./traceability.md). |
| **Marketplace** | Catálogo de conectores oficiales y de terceros, gestionado en Control Plane. | Servicio global; habilita ecosistema (fase V2). Ver [integrations.md](./integrations.md), [roadmap](../roadmap/roadmap.md). |
| **MES (Manufacturing Execution System)** | Sistema de ejecución de manufactura que gestiona y registra la actividad de planta en tiempo real, entre el ERP y el piso. "Nexo" se posiciona como **MES ligero** orientado a captura e integración planta↔ERP. | Categoría del producto. Ver [product.md](./product.md), [architecture.md](./architecture.md). |
| **Modbus** | Protocolo industrial serie/TCP muy difundido para leer/escribir registros de dispositivos. | Fuente de datos canónica; adapter en **Ingestion**. Ver [data-ingestion.md](./data-ingestion.md), [devices.md](./devices.md). |
| **Motivo (Reason Code)** | Código catalogado que clasifica una parada, un scrap o un defecto (p. ej. "falta de material", "cambio de formato"). | Entidad canónica; catálogo semilla al alta de tenant. Ver [downtime.md](./downtime.md), [scrap.md](./scrap.md). |
| **MQTT** | Protocolo de mensajería ligero publish/subscribe, habitual en IoT y dispositivos de campo. | Fuente de datos canónica; adapter en **Ingestion**. Ver [data-ingestion.md](./data-ingestion.md). |
| **MTBF (Mean Time Between Failures)** | Tiempo medio entre fallas: indicador de fiabilidad de un activo. | Fórmula en §3; calculado por **Downtime**. Ver [downtime.md](./downtime.md). |
| **MTTR (Mean Time To Repair)** | Tiempo medio de reparación: indicador de mantenibilidad de un activo. | Fórmula en §3; calculado por **Downtime**. Ver [downtime.md](./downtime.md). |
| **Multi-tenant / Multi-tenancy** | Capacidad de servir a múltiples empresas (tenants) desde la misma plataforma con **aislamiento total** de datos. En "Nexo" se implementa como **DB-per-tenant**. | Requisito NO negociable. Ver [multi-tenancy.md](./multi-tenancy.md). |
| **Odoo** | Primer ERP soportado por "Nexo". El core no depende de él; se integra vía conector desacoplado. | Alcance del MVP. Ver [integrations.md](./integrations.md). |
| **OEE (Overall Equipment Effectiveness)** | Indicador maestro de eficiencia global del equipo: producto de **Disponibilidad × Rendimiento × Calidad**. | KPI central; fórmula en §3. Ver [dashboards.md](./dashboards.md), [production.md](./production.md). |
| **OPC UA (OPC Unified Architecture)** | Estándar de interoperabilidad industrial orientado a servicios para intercambio seguro de datos entre dispositivos y software. | Fuente de datos canónica; adapter en **Ingestion**; vive on-premise (edge). Ver [data-ingestion.md](./data-ingestion.md), [devices.md](./devices.md). |
| **Operación / Ruta (Routing)** | Secuencia de pasos/operaciones que sigue un producto en su proceso. | Entidad canónica; contexto de registros de producción. Ver [production.md](./production.md). |
| **Operario (Operator)** | Usuario que opera en planta; subtipo de Usuario con rol operativo. | Persona/rol canónico; captura manual desde tablet. Ver [users-permissions.md](./users-permissions.md). |
| **Orden de producción (Work Order / MO — Manufacturing Order)** | Orden a ejecutar en planta; se sincroniza con el ERP y contextualiza los registros. | Entidad canónica; ver también **Work Order/MO**. Ver [production.md](./production.md), [integrations.md](./integrations.md). |
| **OTA (Over-The-Air)** | Actualización remota del firmware/configuración de dispositivos edge sin intervención física. | Gestionado por **Devices**. Ver [devices.md](./devices.md). |
| **Parada (Downtime Event)** | Detención de una máquina, programada o no programada, con su **Motivo**. | Entidad canónica; base de **Disponibilidad**, MTBF/MTTR. Ver [downtime.md](./downtime.md). |
| **PLC (Programmable Logic Controller)** | Controlador lógico programable que gobierna máquinas y procesos en planta; principal fuente de señales industriales. | Fuente canónica (incluye Siemens **S7** y otros); vive on-premise. Ver [devices.md](./devices.md), [data-ingestion.md](./data-ingestion.md). |
| **Planta / Site** | Instalación física de una empresa. | Entidad canónica; raíz de la jerarquía y del scoping. Ver [data-model.md](./data-model.md). |
| **Poka-yoke** | Diseño "a prueba de errores" que impide o detecta equivocaciones del operario en el proceso (validaciones, bloqueos, guías). | Principio de calidad aplicable a formularios y **Rules Engine**. Ver [quality.md](./quality.md), [ui-ux.md](./ui-ux.md). |
| **Producto / SKU (Stock Keeping Unit)** | Ítem fabricado identificado unívocamente. | Entidad canónica; se sincroniza con ERP. Ver [data-model.md](./data-model.md), [integrations.md](./integrations.md). |
| **RBAC (Role-Based Access Control)** | Control de acceso basado en roles, con alcance por planta/línea (scoping). | Modelo base de permisos, extendido con **ABAC**. Ver [users-permissions.md](./users-permissions.md). |
| **Read model (Modelo de lectura)** | Proyección optimizada para consulta, materializada a partir de eventos, que alimenta dashboards y reportes. | Consecuencia de **CQRS**. Ver [architecture.md](./architecture.md), [dashboards.md](./dashboards.md). |
| **Registro de producción (Production Record)** | Cantidad producida en un contexto (orden/máquina/turno). | Entidad canónica; insumo de OEE. Ver [production.md](./production.md). |
| **Registro de scrap (Scrap Record)** | Cantidad descartada + motivo + costo asociado. | Entidad canónica; base de **Scrap Rate**. Ver [scrap.md](./scrap.md). |
| **Rendimiento (Performance)** | Factor de OEE: relación entre la producción real y la teóricamente posible según el tiempo de ciclo ideal. | Fórmula en §3; usa **Cycle time** ideal. Ver [dashboards.md](./dashboards.md), [production.md](./production.md). |
| **Regla (Rule)** | Automatización del tipo **trigger–condición–acción** evaluada en tiempo real. | Entidad canónica; servicio **Rules Engine**; produce **Alertas**. Ver [rules-engine.md](./rules-engine.md). |
| **Resolución de tenant** | Proceso de determinar a qué tenant pertenece una petición (por subdominio/host o claim `tenant_id`) y obtener su cadena de conexión desde el **Tenant Connection Registry**. | Núcleo del aislamiento. Ver [multi-tenancy.md](./multi-tenancy.md), [control-plane.md](./control-plane.md). |
| **S7** | Familia de protocolos/PLC de **Siemens** (SIMATIC S7); fuente industrial prioritaria. | Caso concreto de **PLC**; adapter dedicado en **Ingestion**. Ver [devices.md](./devices.md), [data-ingestion.md](./data-ingestion.md). |
| **SCADA (Supervisory Control And Data Acquisition)** | Sistema de supervisión y adquisición de datos de procesos industriales. "Nexo" **no es** un SCADA; puede integrarse con SCADA existentes (fase futura). | Contraste de categoría e integración. Ver [product.md](./product.md), [future-features.md](./future-features.md). |
| **Scrap** | Material o producto descartado por no conformidad; también el módulo que lo registra y clasifica. | Alcance del MVP; ver **Scrap Rate** en §3. Ver [scrap.md](./scrap.md). |
| **Sector / Área** | Subdivisión de una planta que agrupa líneas/máquinas. | Entidad canónica; nivel de jerarquía. Ver [data-model.md](./data-model.md). |
| **Sensor** | Punto de medición físico asociado a un dispositivo o máquina. | Entidad canónica; genera **Señales/Tags** y **Lecturas**. Ver [devices.md](./devices.md). |
| **Señal / Tag** | Variable leída de un dispositivo (temperatura, contador, estado, presión). | Entidad canónica; se muestrea como **Lectura**. Ver [devices.md](./devices.md), [data-model.md](./data-model.md). |
| **Serie / Serial (Serial number)** | Identificador único de una unidad individual; unidad fina de trazabilidad. | Entidad canónica; complementa al **Lote**. Ver [traceability.md](./traceability.md). |
| **Sharding** | Partición horizontal de datos entre múltiples nodos. En "Nexo", el propio **DB-per-tenant** ya particiona por cliente. | Estrategia de escalabilidad. Ver [scalability.md](./scalability.md). |
| **SPC (Statistical Process Control / Control Estadístico de Procesos)** | Uso de estadística (cartas de control, límites) para monitorear la estabilidad y capacidad de un proceso. | Herramienta de **Quality**; base de análisis de variabilidad. Ver [quality.md](./quality.md). |
| **SSO (Single Sign-On)** | Inicio de sesión único que permite acceder con una sola credencial corporativa. | Gestionado por **Identity & Access**. Ver [users-permissions.md](./users-permissions.md), [security.md](./security.md). |
| **Store-and-forward** | Técnica del edge: almacenar localmente los datos ante un corte de conexión y reenviarlos cuando se restablece, sin pérdida. | Clave de resiliencia edge (principio #4); usa **dedup key**. Ver [data-ingestion.md](./data-ingestion.md), [devices.md](./devices.md). |
| **Takt time** | Ritmo de producción requerido para satisfacer la demanda (tiempo disponible / demanda). Marca el "pulso" objetivo. | Comparar con **Cycle time**. Ver [production.md](./production.md). |
| **Tenant / Empresa** | Cliente de la plataforma; unidad de aislamiento. En "Nexo" **1 tenant = 1 base de datos**. | Entidad canónica raíz; ver **DB-per-tenant**. Ver [multi-tenancy.md](./multi-tenancy.md). |
| **Tenant Connection Registry** | Registro (en Control Plane) que mapea cada tenant con la cadena de conexión de su base de datos (secreto gestionado). | Habilita la **Resolución de tenant**. Ver [control-plane.md](./control-plane.md), [multi-tenancy.md](./multi-tenancy.md). |
| **Time-series (Serie temporal)** | Datos indexados por tiempo; almacenamiento optimizado para **Lecturas** de alta frecuencia. | Estrategia de escalabilidad. Ver [scalability.md](./scalability.md), [data-ingestion.md](./data-ingestion.md). |
| **Turno (Shift)** | Franja horaria de trabajo que contextualiza registros y KPIs (mañana/tarde/noche). | Entidad canónica; dimensión de análisis en dashboards. Ver [production.md](./production.md). |
| **Usuario / Rol / Permiso** | Sujeto de acceso y su autorización (rol + permisos con alcance). | Entidades canónicas; modelo **RBAC/ABAC**. Ver [users-permissions.md](./users-permissions.md). |
| **Work Center / Asset** | Ver **Centro de trabajo / Máquina**. | — |
| **Work Order / MO** | Ver **Orden de producción**. | Sincronizada con ERP. Ver [production.md](./production.md), [integrations.md](./integrations.md). |

---

## 3. Fórmulas de KPI (canónicas)

> Estas fórmulas son la **fuente única de verdad de cálculo**. Deben usarse **idénticas** en [dashboards.md](./dashboards.md), [production.md](./production.md), [downtime.md](./downtime.md) y [quality.md](./quality.md). Cualquier variación (por industria o por cliente) debe documentarse como excepción en [open-questions.md](./open-questions.md), no cambiarse aquí sin acuerdo.

| KPI | Fórmula | Componentes | Documento de referencia |
|-----|---------|-------------|-------------------------|
| **OEE** | `OEE = Disponibilidad × Rendimiento × Calidad` | Producto de los tres factores (valor 0–1 o %) | [dashboards.md](./dashboards.md), [production.md](./production.md) |
| **Disponibilidad** | `Disponibilidad = Tiempo operativo / Tiempo productivo planificado` | `Tiempo operativo = Tiempo planificado − Paradas` | [downtime.md](./downtime.md) |
| **Rendimiento** | `Rendimiento = (Tiempo de ciclo ideal × Total de piezas producidas) / Tiempo operativo` | Usa el **Cycle time** ideal | [production.md](./production.md) |
| **Calidad** | `Calidad = Piezas buenas / Total de piezas producidas` | Factor de OEE (no es el módulo Quality) | [quality.md](./quality.md) |
| **Scrap Rate** | `Scrap Rate = Piezas descartadas / Total producidas` (o por costo) | Puede expresarse por unidades o por valor | [scrap.md](./scrap.md) |
| **FPY (First Pass Yield)** | `FPY = Piezas buenas a la primera / Total ingresadas` | Sin considerar retrabajo | [quality.md](./quality.md) |
| **MTBF** | `MTBF = Tiempo operativo total / N.º de fallas` | Fiabilidad del activo | [downtime.md](./downtime.md) |
| **MTTR** | `MTTR = Tiempo total de reparación / N.º de reparaciones` | Mantenibilidad del activo | [downtime.md](./downtime.md) |

**Notas de cálculo (para evitar ambigüedades entre módulos):**

- Los factores de OEE se expresan como fracción (0–1) internamente y se muestran como porcentaje en UI; la multiplicación se hace sobre las fracciones.
- **Tiempo productivo planificado** excluye paradas planificadas de calendario (p. ej. sin turno asignado); las **Paradas** que restan al tiempo operativo son las que ocurren dentro del tiempo planificado. La política exacta de qué paradas cuentan se define en [downtime.md](./downtime.md).
- "Piezas buenas" = Total producidas − Scrap − retrabajo no conforme, según la definición unificada de [quality.md](./quality.md) y [scrap.md](./scrap.md).
- Todo KPI se calcula sobre un **contexto** (planta/línea/máquina, turno, orden, rango temporal) provisto por los **read models** del servicio **Dashboards / Analytics**.

---

## 4. Términos de proceso y roadmap (referencia rápida)

| Término | Definición breve | Relación |
|---------|------------------|----------|
| **MVP** | Primera versión con captura Producción/Scrap/Calidad/Paradas/Eventos, **carga manual (tablet) + datalogger vía CSV/archivo**, dashboard en tiempo real e integración Odoo. La **captura automática por protocolos industriales (S7/OPC UA/Modbus/MQTT) llega en V1**. | Ver [roadmap](../roadmap/roadmap.md), [tablero](../open-questions-board.md). |
| **V1 / V2 / Enterprise** | Fases sucesivas del producto (reglas y reportes → marketplace y multi-ERP → IA/visión y gemelo digital). | Ver [roadmap](../roadmap/roadmap.md), [future-features.md](./future-features.md). |
| **MoSCoW** | Método de priorización (Must/Should/Could/Won't) usado en cada fase. | Ver [roadmap](../roadmap/roadmap.md). |
| **SLA (Service Level Agreement)** | Acuerdo de nivel de servicio (disponibilidad, soporte) ofrecido a clientes enterprise. | Ver [open-questions.md](./open-questions.md), [security.md](./security.md). |
| **Observability (Observabilidad)** | Logs, métricas y trazas centralizadas para conocer el estado de tenants, servicios y conectores. | Servicio global; ver [architecture.md](./architecture.md), [control-plane.md](./control-plane.md). |

---

## Preguntas abiertas

Estas dudas se consolidan en [open-questions.md](./open-questions.md):

1. **Naming del producto:** el nombre **"Nexo" es provisional (working name)**; falta verificación de marca/dominio y decisión definitiva antes de exponerlo a clientes.
2. **Expresión de KPIs:** ¿los KPIs se muestran siempre en % o se permite configurar unidades/objetivos por tenant e industria (targets de OEE distintos por sector)?
3. **Definición de "pieza buena":** ¿se estandariza globalmente o cada tenant puede parametrizar qué cuenta como buena/retrabajo/scrap?
4. **Idioma del glosario en producto:** ¿se mantiene bilingüe (término industrial en inglés + es-AR) en la UI, o se localiza completamente por tenant?
5. **Alta de términos:** ¿qué proceso de gobierno se usa para incorporar términos nuevos y evitar divergencias entre equipos/documentos?
6. **Takt vs. Cycle time:** ¿se calcula y muestra takt time en el MVP o queda para V1 (requiere demanda/planificación del ERP)?
