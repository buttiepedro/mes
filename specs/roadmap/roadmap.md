# Nexo — Roadmap por fases

> **Documento:** `specs/roadmap/roadmap.md` · **Estado:** Borrador v0.1 · **Actualizado:** 2026-07-11
> **Roles:** Product Manager · Software Architect · UX Designer
> **Relacionados:** [vision.md](./vision.md) · [milestones.md](./milestones.md) · [backlog.md](./backlog.md) · [idea.md](../idea.md) · [product.md](../specs/product.md) · [architecture.md](../specs/architecture.md) · [modules.md](../specs/modules.md) · [future-features.md](../specs/future-features.md)

## Resumen ejecutivo

Este documento traduce la [visión](./vision.md) en un **plan de fases** ejecutable: **MVP → V1 → V2 → Enterprise**. Para cada fase define objetivos, funcionalidades, prioridad **MoSCoW**, dependencias, una **tabla de riesgos y mitigaciones** y **criterios de salida** medibles que habilitan el pasaje a la siguiente. Es el puente entre la estrategia (qué queremos ser) y la ejecución (qué construimos y en qué orden), y la fuente de la que se derivan los [hitos](./milestones.md) y el [backlog](./backlog.md).

La secuencia respeta la **escalera de capas** de la visión: el MVP entrega la **capa de captura** (eliminar la carga manual, dashboard en tiempo real, integración Odoo, multi-tenant DB-per-tenant); V1 y V2 construyen el **MES ligero** (reglas, notificaciones, trazabilidad, reportes, más protocolos, Marketplace, multi-ERP, distribución geográfica); y Enterprise incorpora la **inteligencia industrial** (IA/visión, mantenimiento predictivo, gemelo digital, SLAs enterprise). Cada fase reutiliza y capitaliza la anterior; el **Evento canónico** capturado desde el MVP es el activo que habilita todo lo demás.

Las fases y su contenido son **canónicos** (brief §11): no se agregan ni se mueven capacidades entre fases sin actualizar este documento y la visión. Las fechas relativas del diagrama son orientativas de secuencia y dependencia, no compromisos contractuales; los compromisos verificables viven como criterios de aceptación en [milestones.md](./milestones.md).

---

## 1. Vista general de las fases

| Fase | Tema | Capa (visión) | Resultado de negocio |
|---|---|---|---|
| **MVP** | Capturar y probar el valor | Captura | La planta deja de cargar datos a mano; dato en tiempo real y en Odoo |
| **V1** | Automatizar y trazar | MES ligero | El dato dispara acciones (reglas/notificaciones), se traza y se reporta |
| **V2** | Ecosistema y multi-ERP | MES ligero | Marketplace, varios ERPs, despliegues progresivos, DBs distribuidas |
| **Enterprise** | Inteligencia industrial | Inteligencia | IA/visión, predicción, gemelo digital, SLAs y multi-región |

### 1.1 Cronograma orientativo (Gantt)

> Las duraciones son relativas y sirven para comunicar **secuencia y solapamiento**, no fechas comprometidas. El día cero es el inicio de construcción del MVP.

```mermaid
gantt
    title Roadmap Nexo — secuencia orientativa de fases
    dateFormat  YYYY-MM-DD
    axisFormat  %b %Y

    section MVP · Captura
    Fundaciones (multi-tenant, Control Plane mínimo, Identity)   :mvp1, 2026-08-01, 90d
    Ingesta datalogger/CSV/Excel + Evento canónico               :mvp2, after mvp1, 75d
    Carga manual tablet + módulos de dominio (Prod/Scrap/QC/Downtime) :mvp3, after mvp1, 90d
    Dashboard tiempo real + Conector Odoo                        :mvp4, after mvp2, 60d
    Hardening, piloto y clientes de referencia                   :mvp5, after mvp4, 45d

    section V1 · MES ligero
    Motor de reglas + Notificaciones multicanal                  :v1a, after mvp5, 90d
    Protocolos industriales (S7/OPC UA/Modbus/MQTT) + híbrido real :v1b, after mvp5, 90d
    Trazabilidad lote/serie + Reportes                           :v1c, after v1a, 75d
    RBAC avanzado + Observabilidad                               :v1d, after mvp5, 90d

    section V2 · Ecosistema
    Marketplace de conectores                                    :v2a, after v1c, 90d
    Multi-ERP (SAP/Dynamics/Oracle)                              :v2b, after v1c, 120d
    Analytics avanzado + Feature flags / despliegues progresivos :v2c, after v1d, 90d
    Distribución geográfica de DBs                               :v2d, after v2b, 90d

    section Enterprise · IA
    IA de calidad / visión artificial                            :ent1, after v2c, 120d
    Mantenimiento predictivo                                     :ent2, after v2c, 120d
    Gemelo digital + Energía/sustentabilidad                     :ent3, after ent1, 120d
    SLAs enterprise + alta disponibilidad multi-región           :ent4, after v2d, 120d
```

### 1.2 Dependencias entre fases (mapa)

```mermaid
flowchart LR
    MVP["MVP · Captura<br/>Evento canónico · DB-per-tenant · Odoo"]
    V1["V1 · MES ligero<br/>Reglas · Notif · Trazabilidad · Reportes"]
    V2["V2 · Ecosistema<br/>Marketplace · Multi-ERP · Feature flags"]
    ENT["Enterprise · IA<br/>Visión · Predictivo · Gemelo digital"]
    MVP --> V1 --> V2 --> ENT
    MVP -. Evento canónico alimenta .-> ENT
    V1 -. Reglas habilitan alertas de IA .-> ENT
    V2 -. Feature flags habilitan rollout de IA .-> ENT
```

---

## 2. Fase MVP — Captura y prueba de valor

**Tema:** eliminar la carga manual y demostrar valor con el mínimo alcance viable.

### 2.1 Objetivos

- Registrar **Producción, Scrap, Controles de Calidad, Paradas y Eventos de máquina** normalizados al **Evento canónico**.
- Capturar desde **datalogger vía carga de archivo/CSV/Excel** y **carga manual**, normalizando al Evento canónico (el modelo de Devices/ingesta contempla los protocolos industriales desde el día uno, pero se activan en V1).
- Permitir **carga manual desde tablets** con UX de operario (offline-first).
- Demostrar el **caso estrella del MVP**: **producción manual → dashboard → Odoo** end-to-end.
- Ofrecer un **dashboard en tiempo real** con KPIs base (OEE y sus factores, scrap rate).
- **Integrar con Odoo** vía conector desacoplado con ACL.
- Operar **multi-tenant con base de datos por tenant** y un **Control Plane mínimo** (alta de tenant en 7 pasos, licencias básicas).
- Probar objetivamente la **reducción de carga manual** con clientes de referencia.

### 2.2 Funcionalidades y prioridad (MoSCoW)

| Funcionalidad | Módulo (BC) | MoSCoW |
|---|---|---|
| Alta de tenant end-to-end (7 pasos, DB-per-tenant) | Tenant Provisioning | **Must** |
| AuthN/AuthZ con claim de tenant en el token | Identity & Access | **Must** |
| Registro de conexión de tenant (Connection Registry) | Tenant Provisioning / Control Plane | **Must** |
| Planes/licencias básicas y límites | Administration & Licensing | **Must** |
| Ingesta de datalogger vía carga de archivo/CSV/Excel + carga manual | Ingestion / Edge Gateway | **Must** |
| Normalización al Evento canónico + `dedup_key` (idempotencia) | Ingestion / Edge Gateway | **Must** |
| Store-and-forward / offline-first ante cortes de conectividad (manual y datalogger) | Ingestion / Edge Gateway | **Must** |
| Alta y salud básica de dispositivos y señales/tags | Devices | **Must** |
| Registro de producción (orden/máquina/turno) | Production | **Must** |
| Registro de scrap (motivo + cantidad + costo) | Scrap | **Must** |
| Inspección de calidad con checklist/variables | Quality | **Must** |
| Registro de paradas con motivo (Reason Code) | Downtime | **Must** |
| Carga manual en tablet (UX operario) para los 5 registros | Production/Scrap/Quality/Downtime | **Must** |
| Dashboard en tiempo real (CQRS/read models) con OEE, scrap rate | Dashboards / Analytics | **Must** |
| Conector Odoo (órdenes/productos/cantidades) con ACL | Connectors / Integrations | **Must** |
| Job de sincronización con reintentos básicos | Connectors / Integrations | **Should** |
| Historial de eventos inmutable (base de trazabilidad) | Traceability / Event Store | **Should** |
| Auditoría de acciones básicas | Audit | **Should** |
| Adjuntar foto/evidencia a un registro | Files / Media | **Could** |
| Estado de tenants/servicios en Control Plane (salud mínima) | Observability | **Could** |
| Notificación de bienvenida al alta de tenant | Notifications | **Could** |
| Captura automática por protocolos industriales (S7/OPC UA/Modbus/MQTT), motor de reglas, multi-ERP, marketplace, IA | (varios) | **Won't** (fase posterior) |

### 2.3 Dependencias

- **Internas:** el multi-tenancy DB-per-tenant y el Control Plane mínimo son prerrequisito de todo lo demás; Identity & Access habilita el resto de servicios; el Evento canónico (Ingestion) es prerrequisito de Dashboards y del Conector Odoo.
- **Externas:** disponibilidad de un entorno Odoo de destino; acceso de red desde el edge del cliente hacia la nube (outbound); datalogger / archivos CSV/Excel del piloto (los PLC/protocolos industriales entran en V1).
- **De arquitectura:** broker de mensajería (event-driven), object storage por tenant, gestión de secretos para cadenas de conexión. Ver [architecture.md](../specs/architecture.md).

### 2.4 Riesgos y mitigaciones

| Riesgo | Impacto | Prob. | Mitigación |
|---|---|---|---|
| Complejidad del alta automatizada de DB-per-tenant (7 pasos) retrasa todo | Alto | Media | Automatizar y probar el flujo como primer hito; idempotencia y rollback por paso; ver [milestones.md](./milestones.md) |
| Conectividad intermitente del edge causa pérdida de datos | Alto | Alta | Store-and-forward + `dedup_key` como Must; pruebas de corte de red desde el diseño |
| Formatos heterogéneos de datalogger/CSV/Excel dificultan el parseo | Medio | Media | Acotar el MVP a datalogger + CSV/Excel con plantillas; validar con archivos reales del piloto; los protocolos industriales (S7/OPC UA/Modbus/MQTT) se acotan en V1 |
| Mapeo Odoo (objetos/direccionalidad) mal definido | Medio | Alta | Cerrar alcance de sincronización con el cliente piloto; ACL aísla el core; ver preguntas abiertas de [idea.md](../idea.md) |
| UX de operario insuficiente → los operarios no cargan | Alto | Media | Diseño con operarios reales, pruebas en planta, mínimos toques; ver [ui-ux.md](../specs/ui-ux.md) |
| Fuga de datos entre tenants (aislamiento) | Crítico | Baja | DB-per-tenant no negociable; resolución de tenant por claim/Registry; auditoría; ver [multi-tenancy.md](../specs/multi-tenancy.md) |
| Sobre-alcance (scope creep) hacia features de V1 | Medio | Alta | MoSCoW estricto; "Won't" explícito; disciplina de fase |

### 2.5 Criterios de salida (Exit Criteria)

- [ ] **Alta de tenant end-to-end** ejecuta los 7 pasos y deja la empresa en estado "activo" de forma automatizada y repetible.
- [ ] **Producción manual → dashboard → Odoo end-to-end (caso estrella):** un registro de producción cargado a mano en tablet se ve en el dashboard en tiempo real y se sincroniza con Odoo.
- [ ] **Primer dato de datalogger/CSV a Odoo:** un evento capturado desde un datalogger (carga de archivo/CSV/Excel) se ve en el dashboard en tiempo real y se refleja en Odoo vía el conector.
- [ ] Los **cinco registros** (producción, scrap, calidad, paradas, eventos) se capturan por **carga manual en tablet** y por **datalogger/CSV** (la captura automática por protocolos industriales se valida en V1).
- [ ] El **dashboard** muestra OEE (con sus tres factores) y scrap rate calculados con las fórmulas canónicas, en tiempo real.
- [ ] **Store-and-forward** demostrado: tras un corte de red simulado, ningún evento se pierde ni se duplica.
- [ ] **Aislamiento** verificado: un tenant no puede acceder a datos de otro (prueba de penetración básica).
- [ ] Al menos **un cliente de referencia** en producción con evidencia objetiva de reducción de carga manual (NSM en movimiento, ver [vision.md](./vision.md) §2).

---

## 3. Fase V1 — MES ligero: automatizar y trazar

**Tema:** el dato deja de solo mostrarse y empieza a **disparar acciones**, a **trazarse** y a **reportarse**.

### 3.1 Objetivos

- Habilitar un **motor de reglas** (trigger-condición-acción) en tiempo real.
- Enviar **notificaciones multicanal** con plantillas y escalado.
- Incorporar la **captura automática por protocolos industriales** (**Siemens S7, OPC UA, Modbus, MQTT**) y habilitar el **modo híbrido real** (manual + automático por planta).
- Entregar **reportes** on-demand y programados, exportables.
- Implementar **trazabilidad de lote/serie** (genealogía) sobre el Event Store inmutable.
- Elevar el control de acceso a **RBAC avanzado** con scoping por planta/línea (y ABAC donde aplique).
- Consolidar la **observabilidad** transversal (logs/métricas/trazas en Control Plane).

### 3.2 Funcionalidades y prioridad (MoSCoW)

| Funcionalidad | Módulo (BC) | MoSCoW |
|---|---|---|
| Motor de reglas trigger-condición-acción en tiempo real | Rules Engine | **Must** |
| Notificaciones multicanal + plantillas + escalado | Notifications | **Must** |
| Agente Edge/Gateway + adapters Siemens S7, OPC UA y Modbus (captura automática) | Ingestion / Edge Gateway | **Must** |
| Modo híbrido real (manual + automático por planta) | Ingestion / Devices | **Must** |
| Adapter MQTT completo | Ingestion / Edge Gateway | **Should** |
| Trazabilidad y genealogía de lote/serie | Traceability / Event Store | **Must** |
| Reportes on-demand y programados, exportables | Reports | **Must** |
| RBAC avanzado con scoping por planta/línea | Identity & Access | **Must** |
| Extensiones ABAC donde aplique | Identity & Access | **Should** |
| Observabilidad transversal (logs/métricas/trazas) | Observability | **Must** |
| Alertas/alarmas por umbral disparadas por reglas | Rules Engine / Notifications | **Should** |
| Salud avanzada de dispositivos y firmware/OTA | Devices | **Could** |
| Gestión de evidencias/archivos enriquecida | Files / Media | **Could** |
| Marketplace, multi-ERP, IA | (varios) | **Won't** (fase posterior) |

### 3.3 Dependencias

- **Del MVP:** el Evento canónico, el Event Store y el edge deben estar sólidos; las reglas y la trazabilidad se apoyan en ellos.
- **Internas:** el motor de reglas es prerrequisito de las alertas/alarmas; RBAC avanzado condiciona el scoping de reportes y dashboards; la observabilidad requiere instrumentación en todos los servicios.
- **Externas:** entornos de prueba con OPC UA/Modbus/MQTT reales; catálogos de motivos (Reason Codes) del cliente para trazabilidad y reportes.

### 3.4 Riesgos y mitigaciones

| Riesgo | Impacto | Prob. | Mitigación |
|---|---|---|---|
| Motor de reglas mal acotado deriva en complejidad inmanejable | Alto | Media | Modelo trigger-condición-acción simple y evaluable; límites por tenant; iterar casos reales |
| "Tormenta de notificaciones" molesta y se ignora | Medio | Alta | Escalado, agrupación, umbrales y silenciamiento; plantillas por rol/persona |
| Heterogeneidad de OPC UA/Modbus entre fabricantes | Alto | Media | Certificar por dispositivo; suite de pruebas de interoperabilidad; priorizar los más comunes |
| Trazabilidad exige datos que el MVP no capturó (lote/serie) | Alto | Media | Definir captura de lote/serie desde el diseño; migración/backfill controlada |
| Costo de almacenamiento del Event Store inmutable crece | Medio | Media | Almacenamiento time-series, políticas de retención por plan/licencia |
| RBAC/ABAC complejo genera errores de permisos | Alto | Media | Matriz de permisos canónica en [users-permissions.md](../specs/users-permissions.md); pruebas de scoping |

### 3.5 Criterios de salida

- [ ] Una **regla** definida por el cliente dispara una **acción/notificación** en tiempo real ante una condición de planta.
- [ ] **Siemens S7, OPC UA y Modbus** capturan datos de al menos un dispositivo real cada uno, normalizados al Evento canónico.
- [ ] El **modo híbrido** combina, en una misma planta, captura manual y automática por protocolo sobre el mismo Evento canónico.
- [ ] La **genealogía de un lote/serie** se reconstruye de punta a punta desde el Event Store.
- [ ] Un **reporte programado** se genera y exporta automáticamente con datos consistentes con el dashboard.
- [ ] El **RBAC avanzado** restringe correctamente el acceso por planta/línea según la matriz de permisos.
- [ ] La **observabilidad** permite diagnosticar un incidente de un tenant desde el Control Plane (traza extremo a extremo).

---

## 4. Fase V2 — Ecosistema y multi-ERP

**Tema:** abrir la plataforma al **ecosistema** y romper la dependencia de un único ERP y una única ubicación de datos.

### 4.1 Objetivos

- Lanzar el **Marketplace de conectores** (oficiales y de terceros).
- Soportar **multi-ERP**: SAP, Microsoft Dynamics, Oracle, además de Odoo.
- Entregar **analytics avanzado** sobre los read models.
- Habilitar **feature flags** y **despliegues progresivos** (canary/blue-green).
- Permitir la **distribución geográfica de las DBs por tenant**.

### 4.2 Funcionalidades y prioridad (MoSCoW)

| Funcionalidad | Módulo (BC) | MoSCoW |
|---|---|---|
| Marketplace de conectores oficiales | Marketplace | **Must** |
| Conectores multi-ERP (SAP / Dynamics / Oracle) vía ACL | Connectors / Integrations | **Must** |
| Analytics avanzado (tendencias, comparativas, cohortes) | Dashboards / Analytics | **Must** |
| Feature flags por tenant/plan | Administration & Licensing | **Must** |
| Despliegues progresivos (canary / blue-green) | (plataforma) / Observability | **Should** |
| Distribución geográfica de DBs por tenant | Tenant Provisioning / Control Plane | **Must** |
| Catálogo de conectores de terceros + certificación | Marketplace | **Should** |
| Facturación por uso/plan avanzada | Administration & Licensing | **Should** |
| SDK/portal para partners | Marketplace | **Could** |
| IA/visión, mantenimiento predictivo, gemelo digital | AI / Computer Vision | **Won't** (fase posterior) |

### 4.3 Dependencias

- **Del MVP/V1:** el patrón Conectores + ACL (probado con Odoo) es la base del multi-ERP; los read models de V1 sostienen el analytics avanzado; la observabilidad habilita los despliegues progresivos.
- **Internas:** el Marketplace depende del catálogo y de Administration & Licensing (planes/feature flags); la distribución geográfica depende del Connection Registry y de la resolución de tenant.
- **Externas:** entornos y credenciales de SAP/Dynamics/Oracle; requisitos de residencia de datos por región de cada cliente.

### 4.4 Riesgos y mitigaciones

| Riesgo | Impacto | Prob. | Mitigación |
|---|---|---|---|
| Cada ERP nuevo multiplica el esfuerzo de integración | Alto | Alta | ACL estricto + mapeos declarativos reutilizables; certificación por conector |
| Marketplace de terceros introduce conectores de baja calidad | Alto | Media | Proceso de certificación, sandbox, revisión y revocación; gobernanza del catálogo |
| Distribución geográfica rompe supuestos de latencia/consistencia | Alto | Media | DB-per-tenant ya particiona; probar migración individual sin cambio de lógica |
| Feature flags mal gestionados generan estados inconsistentes | Medio | Media | Flags por tenant/plan versionados; despliegues progresivos con rollback |
| Complejidad operativa y costo de multi-región | Alto | Media | Autoscaling por servicio, políticas de costo por plan; ver [scalability.md](../specs/scalability.md) |
| Residencia de datos y cumplimiento por país | Alto | Media | Elección de región por tenant; auditoría; alinear con [security.md](../specs/security.md) |

### 4.5 Criterios de salida

- [ ] Un cliente **instala un conector desde el Marketplace** y queda operativo sin intervención manual del proveedor.
- [ ] Un tenant **sincroniza con un ERP distinto de Odoo** (SAP, Dynamics u Oracle) reutilizando el patrón ACL.
- [ ] El **analytics avanzado** entrega comparativas/tendencias que el dashboard base no ofrecía.
- [ ] Un **feature flag** habilita/inhabilita una capacidad por tenant sin re-despliegue.
- [ ] La **DB de un tenant se migra a otra región** sin cambios en la lógica de negocio y sin downtime perceptible.

---

## 5. Fase Enterprise — Inteligencia industrial

**Tema:** construir **inteligencia** sobre el activo de datos y operar con exigencias enterprise.

### 5.1 Objetivos

- Incorporar **IA de calidad y visión artificial** (inspección, OCR, clasificación).
- Habilitar **mantenimiento predictivo** sobre señales y eventos históricos.
- Ofrecer un **gemelo digital** de la planta/línea.
- Añadir **energía y sustentabilidad** (consumo, huella).
- Integrar con **MES/SCADA existentes**.
- Cumplir **SLAs enterprise** y **alta disponibilidad multi-región**.

### 5.2 Funcionalidades y prioridad (MoSCoW)

| Funcionalidad | Módulo (BC) | MoSCoW |
|---|---|---|
| IA de calidad y visión artificial (inspección/OCR/ML) | AI / Computer Vision | **Must** |
| Mantenimiento predictivo (modelos sobre señales/eventos) | AI / Computer Vision + Devices | **Must** |
| Gemelo digital de planta/línea | (plataforma) / Dashboards | **Should** |
| Energía y sustentabilidad (consumo, huella) | Devices / Dashboards | **Should** |
| Integración con MES/SCADA existentes | Connectors / Integrations | **Should** |
| SLAs enterprise (soporte, disponibilidad, respuesta) | Administration & Licensing / Observability | **Must** |
| Alta disponibilidad multi-región | (plataforma) / Tenant Provisioning | **Must** |
| Marketplace de modelos/algoritmos de IA | Marketplace / AI | **Could** |

### 5.3 Dependencias

- **De fases previas:** el **Evento canónico** y el **Event Store** acumulados desde el MVP son el conjunto de datos para IA/predictivo; V2 (feature flags, multi-región, observabilidad) sostiene el rollout controlado y los SLAs.
- **Internas:** la IA de calidad depende de Files/Media (imágenes) y Quality; el mantenimiento predictivo depende de Devices y Downtime (MTBF/MTTR).
- **Externas:** capacidad de cómputo para modelos (GPU); cámaras IP/USB en planta; integraciones con MES/SCADA de terceros.

### 5.4 Riesgos y mitigaciones

| Riesgo | Impacto | Prob. | Mitigación |
|---|---|---|---|
| Datos insuficientes/sesgados para entrenar modelos | Alto | Media | Capitalizar Evento canónico desde el MVP; validar calidad del dato (`origin_metadata`) |
| Expectativas irreales sobre la IA (sobrepromesa) | Alto | Alta | Casos acotados y medibles; IA como asistencia, no reemplazo; pilotos controlados |
| Aislamiento de modelos/datos por tenant en IA compartida | Crítico | Media | Modelos y storage por tenant; IA compartida trata dato de forma segmentada (brief §6) |
| Costo de cómputo (GPU/visión) erosiona márgenes | Alto | Alta | Pricing por uso; procesamiento en edge cuando aplique; políticas por plan |
| Multi-región y SLAs elevan complejidad operativa | Alto | Media | HA probada en V2; runbooks; observabilidad y automatización de failover |
| Integración con MES/SCADA legados heterogéneos | Medio | Alta | ACL y conectores certificados; alcance por caso; no comandar máquinas (Nexo no es SCADA) |

### 5.5 Criterios de salida

- [ ] Un **modelo de visión/IA** clasifica o inspecciona un caso real de calidad con precisión aceptada por el cliente.
- [ ] El **mantenimiento predictivo** anticipa al menos una condición de falla con antelación útil, sobre datos históricos reales.
- [ ] Los **SLAs enterprise** se cumplen y se reportan (disponibilidad, tiempos de respuesta).
- [ ] La plataforma opera en **al menos dos regiones** con failover probado.
- [ ] La IA respeta el **aislamiento por tenant** (modelos y datos no se filtran entre clientes).

---

## 6. Prioridades transversales (todas las fases)

Estas capacidades no pertenecen a una sola fase; se refuerzan en cada una:

| Eje transversal | MVP | V1 | V2 | Enterprise |
|---|---|---|---|---|
| **Aislamiento multi-tenant** | Base no negociable | Scoping RBAC | Multi-región | Aislamiento de modelos IA |
| **Escalabilidad** | Diseño para escala | Time-series/read models | Autoscaling/distribución | HA multi-región |
| **Observabilidad** | Salud mínima | Transversal completa | Despliegues progresivos | SLAs y failover |
| **Seguridad** | Aislamiento + auditoría | RBAC/ABAC | Residencia de datos | Cumplimiento enterprise |
| **Time-to-value** | Alta 7 pasos + carga manual | Reglas listas | Marketplace autoservicio | Onboarding enterprise |
| **Empaquetado / pricing** | Base por planta (manual, Odoo, dashboard) | + Precio por dispositivo (protocolos) | Feature flags por capa/plan | Add-ons IA / por consumo |

---

## 7. Enlaces de trazabilidad

- Cada fase se descompone en **hitos con criterios de aceptación medibles** en [milestones.md](./milestones.md).
- El trabajo concreto (épicas y user stories con tag de fase) vive en [backlog.md](./backlog.md).
- El detalle por módulo y su mapeo a fases está en [modules.md](../specs/modules.md) y los documentos de dominio ([production.md](../specs/production.md), [quality.md](../specs/quality.md), [scrap.md](../specs/scrap.md), [downtime.md](../specs/downtime.md), [traceability.md](../specs/traceability.md), [integrations.md](../specs/integrations.md), [rules-engine.md](../specs/rules-engine.md), [dashboards.md](../specs/dashboards.md), [notifications.md](../specs/notifications.md), [devices.md](../specs/devices.md)).
- Las capacidades marcadas **Won't** en cada fase se documentan como visión futura en [future-features.md](../specs/future-features.md).

---

## Preguntas abiertas

1. **Fechas reales por fase.** El Gantt es orientativo; falta convertir la secuencia en un calendario con capacidad de equipo real y compromisos de cliente.
2. ✅ **Resuelto (2026-07-11):** el conector Odoo del MVP hace *pull* de MO/Producto/UoM/Motivos y *push* de producción real (avance/cierre de MO) y scrap (agregado por cierre de corrida); calidad opcional — ver [tablero de decisiones](../open-questions-board.md).
3. **Corte MVP/V1 para trazabilidad.** ¿La captura de lote/serie se inicia ya en el MVP (aunque la genealogía completa sea V1) para evitar backfills costosos?
4. **Orden interno de V2.** ¿Multi-ERP antes que Marketplace, o Marketplace primero para habilitar conectores de terceros que aceleren el multi-ERP?
5. **Criterio de entrada a Enterprise.** ¿Qué masa de datos/clientes se requiere para que la IA sea viable y no una promesa? Debe definirse un umbral objetivo.
6. ✅ **Resuelto (2026-07-11):** cada capa se monetiza como **suscripción base por planta + precio por dispositivo conectado**, con módulos empaquetados por capa vía feature flags (Captura base → MES ligero V1 → IA Enterprise) y add-ons por consumo — ver [tablero de decisiones](../open-questions-board.md).
7. **Gestión de "Won't" que se vuelven urgentes.** ¿Qué proceso reevalúa una capacidad diferida si un cliente estratégico la exige antes de tiempo, sin romper la disciplina de fases?
8. **Deuda técnica entre fases.** ¿Cómo se reserva capacidad para hardening/refactor entre fases para no comprometer la escala diseñada?
