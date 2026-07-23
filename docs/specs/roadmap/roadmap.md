# Nexo — Roadmap por fases

> **Documento:** `specs/roadmap/roadmap.md` · **Estado:** Borrador v0.1 · **Actualizado:** 2026-07-13
> **Roles:** Product Manager · Software Architect · UX Designer
> **Relacionados:** [vision.md](./vision.md) · [milestones.md](./milestones.md) · [backlog.md](./backlog.md) · [idea.md](../idea.md) · [product.md](../specs/product.md) · [layered-architecture.md](../specs/layered-architecture.md) · [master-data.md](../specs/master-data.md) · [architecture.md](../specs/architecture.md) · [modules.md](../specs/modules.md) · [future-features.md](../specs/future-features.md)

## Resumen ejecutivo

Este documento traduce la [visión](./vision.md) en un **plan de fases** ejecutable: **MVP → V1 → V2 → Enterprise**. Para cada fase define objetivos, funcionalidades, prioridad **MoSCoW**, dependencias, una **tabla de riesgos y mitigaciones** y **criterios de salida** medibles que habilitan el pasaje a la siguiente. Es el puente entre la estrategia (qué queremos ser) y la ejecución (qué construimos y en qué orden), y la fuente de la que se derivan los [hitos](./milestones.md) y el [backlog](./backlog.md).

La secuencia respeta la **escalera evolutiva de peldaños** de la visión: el MVP entrega el peldaño de **captura y ejecución** (eliminar la carga manual, ver el progreso real, tablero en tiempo real, multi-tenant DB-per-tenant); V1 y V2 construyen el **MES ligero** (reglas, notificaciones, trazabilidad, reportes, más protocolos, Marketplace, multi-ERP, distribución geográfica); y Enterprise incorpora la **inteligencia industrial** (IA/visión, mantenimiento predictivo, simulación sobre el gemelo digital, SLAs enterprise). Cada fase reutiliza y capitaliza la anterior; el **Evento canónico** capturado desde el MVP es el activo que habilita todo lo demás.

> **🔺 Impacto de fase del cambio de encuadre (2026-07-13) — modelo por capas + ERP opcional.** La adopción del **modelo de 4 capas** (ver [layered-architecture.md](../specs/layered-architecture.md) y [vision.md](./vision.md) §1.4) mueve dos cosas en este roadmap, y **ninguna es cosmética**:
>
> 1. **El MVP suma alcance: Master Data propia mínima.** Como el sistema debe funcionar **sin ERP** (modo *standalone*), el MVP necesita sus propios catálogos —productos/ítems, insumos, unidades de medida, procesos, personas/roles— con ABM e importación CSV. Es el **costo oculto más grande** del cambio y **agranda la fase MVP**; el mínimo exacto se cierra en **MOD-17** del [tablero](../open-questions-board.md).
> 2. **El MVP pierde un bloqueante: la integración ERP pasa a ser opcional.** El conector Odoo deja de ser `Must` y de condicionar los criterios de salida: baja a `Should` y se valida **solo en tenants en modo conectado**. Un piloto sin ERP es un piloto válido (reencuadre de **INT-01**).
>
> Además, las cuatro capas están **presentes desde el MVP en versión mínima** (gemelo digital, procesos/tareas, ejecuciones, motor de eventos): lo que evoluciona por fase es su **profundidad**, no su existencia. Las métricas derivadas de la Capa 4 (**progreso, cuellos de botella, tiempos muertos**) entran ya en el MVP.

Las fases y su contenido son **canónicos** (brief §11): no se agregan ni se mueven capacidades entre fases sin actualizar este documento y la visión. Las fechas relativas del diagrama son orientativas de secuencia y dependencia, no compromisos contractuales; los compromisos verificables viven como criterios de aceptación en [milestones.md](./milestones.md).

---

## 1. Vista general de las fases

| Fase | Tema | Peldaño (visión) | Profundidad de las 4 capas | Resultado de negocio |
|---|---|---|---|---|
| **MVP** | Capturar, ejecutar y probar el valor | Captura y ejecución | Las 4 capas en versión mínima + master data propia | La planta deja de cargar datos a mano y ve su progreso real; opera **sin ERP** (Odoo opcional) |
| **V1** | Automatizar y trazar | MES ligero | Capa 2 (perfil proyecto) y Capa 4 (reglas, trazabilidad, reportes) | El dato dispara acciones (reglas/notificaciones), se traza y se reporta |
| **V2** | Ecosistema y multi-ERP | MES ligero | Conector lateral: multi-ERP + Marketplace | Marketplace, varios ERPs, despliegues progresivos, DBs distribuidas |
| **Enterprise** | Inteligencia industrial | Inteligencia | Capa 1 (simulación) y Capa 4 (predicción/visión) | IA/visión, predicción, simulación sobre el gemelo digital, SLAs y multi-región |

### 1.1 Cronograma orientativo (Gantt)

> Las duraciones son relativas y sirven para comunicar **secuencia y solapamiento**, no fechas comprometidas. El día cero es el inicio de construcción del MVP.

```mermaid
gantt
    title Roadmap Nexo — secuencia orientativa de fases
    dateFormat  YYYY-MM-DD
    axisFormat  %b %Y

    section MVP · Captura y ejecución
    Fundaciones (multi-tenant, Control Plane mínimo, Identity)   :mvp1, 2026-08-01, 90d
    Master data propia mínima + Gemelo digital (jerarquía y activos) :mvp1b, after mvp1, 45d
    Procesos/Tareas/Insumos + Ejecuciones (perfil repetitivo)    :mvp1c, after mvp1b, 60d
    Ingesta datalogger/CSV/Excel + Evento canónico               :mvp2, after mvp1, 75d
    Formularios de captura en tablet + módulos de dominio (Prod/Scrap/QC/Downtime) :mvp3, after mvp1, 90d
    Motor de eventos base (progreso, tiempos muertos) + Tablero tiempo real :mvp4, after mvp2, 60d
    Conector Odoo (opcional, modo conectado)                     :mvp4b, after mvp4, 30d
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
    MVP["MVP · Captura y ejecución<br/>4 capas mínimas · Master data propia · DB-per-tenant<br/>Odoo OPCIONAL"]
    V1["V1 · MES ligero<br/>Reglas · Notif · Trazabilidad · Reportes"]
    V2["V2 · Ecosistema<br/>Marketplace · Multi-ERP · Feature flags"]
    ENT["Enterprise · IA<br/>Visión · Predictivo · Gemelo digital"]
    MVP --> V1 --> V2 --> ENT
    MVP -. Evento canónico alimenta .-> ENT
    V1 -. Reglas habilitan alertas de IA .-> ENT
    V2 -. Feature flags habilitan rollout de IA .-> ENT
```

---

## 2. Fase MVP — Captura, ejecución y prueba de valor

**Tema:** eliminar la carga manual, **mostrar el progreso real del trabajo** y demostrar valor con el mínimo alcance viable — **sin depender de un ERP**.

### 2.1 Objetivos

- Modelar un **gemelo digital mínimo** de la planta (Empresa → Planta → Sector → Línea → Centro de trabajo/Máquina) con **cada señal ligada a su activo**.
- Definir **Procesos, Tareas e Insumos** (perfil repetitivo) y **ejecutarlos** como Ejecuciones con avance, consumo real y evidencia.
- Poseer una **Master Data propia mínima** (ítems, insumos, unidades de medida, procesos, personas/roles) que permita operar en **modo standalone**, sin ERP.
- Derivar en la Capa 4 las primeras **métricas de verdad**: **progreso, tiempos muertos y cuellos de botella**, además del OEE base.
- Registrar **Producción, Scrap, Controles de Calidad, Paradas y Eventos de máquina** normalizados al **Evento canónico** (fecha, origen, valor, evidencia).
- Capturar desde **datalogger vía carga de archivo/CSV/Excel** y **carga manual**, normalizando al Evento canónico (el modelo de Devices/ingesta contempla los protocolos industriales desde el día uno, pero se activan en V1).
- Permitir la carga manual desde tablets mediante **formularios de captura** con UX de operario (offline-first). *(Formulario de captura = el operario ingresa datos; tablero = solo visualiza KPIs.)*
- Demostrar el **caso estrella del MVP** (perfil repetitivo): **producción manual → tablero en tiempo real**, y **→ Odoo** cuando el tenant está en modo conectado.
- Ofrecer un **tablero en tiempo real** con KPIs base (OEE y sus factores, scrap rate) **y progreso de las ejecuciones**.
- **Integrar con Odoo** vía conector desacoplado con ACL — **opcional, no bloqueante** (reencuadre de INT-01, 2026-07-13).
- Operar **multi-tenant con base de datos por tenant** y un **Control Plane mínimo** (alta de tenant en 7 pasos, licencias básicas).
- Probar objetivamente la **reducción de carga manual** con clientes de referencia.

### 2.2 Funcionalidades y prioridad (MoSCoW)

| Funcionalidad | Módulo (BC) | MoSCoW |
|---|---|---|
| Alta de tenant end-to-end (7 pasos, DB-per-tenant) | Tenant Provisioning | **Must** |
| AuthN/AuthZ con claim de tenant en el token | Identity & Access | **Must** |
| Registro de conexión de tenant (Connection Registry) | Tenant Provisioning / Control Plane | **Must** |
| Planes/licencias básicas y límites | Administration & Licensing | **Must** |
| **Master data propia mínima** (ítems, insumos, unidades de medida, personas/roles) con ABM + importación CSV | Master Data | **Must** |
| **Modo de operación del tenant** (*standalone* / *conectado*) y su efecto sobre qué catálogos son editables | Master Data / Connectors | **Must** |
| **Gemelo digital mínimo**: jerarquía Empresa→Planta→Sector→Línea→Activo y **binding señal↔activo** | Digital Twin | **Must** |
| Estado en vivo del activo y navegación del gemelo en la UI | Digital Twin | **Should** |
| **Definición de Procesos** (perfil repetitivo) con **Tareas** e **Insumos**, tiempos estándar y rol responsable | Work Model | **Must** |
| Versionado de Proceso y ejecución atada a la versión con la que arrancó | Work Model | **Should** |
| **Ejecución (Run)** de perfil **Lote**: tareas instanciadas, asignación, estados, consumo real y cierre | Execution | **Must** |
| Ejecución de perfil **Proyecto** (hitos, cronograma, ruta crítica) | Execution | **Won't** (V1) |
| **Motor de eventos**: contrato de evento (fecha/origen/valor/evidencia) + atribución a activo/tarea/ejecución | Event Engine | **Must** |
| **Métricas derivadas base**: **progreso** ponderado, **tiempos muertos** y **cuellos de botella** | Event Engine | **Must** |
| **Evidencia** adjunta al evento/tarea (foto, archivo, lectura), requerida por tarea de forma configurable | Event Engine / Files / Media | **Should** |
| Ingesta de datalogger vía carga de archivo/CSV/Excel + carga manual | Ingestion / Edge Gateway | **Must** |
| Normalización al Evento canónico + `dedup_key` (idempotencia) | Ingestion / Edge Gateway | **Must** |
| Store-and-forward / offline-first ante cortes de conectividad (manual y datalogger) | Ingestion / Edge Gateway | **Must** |
| Alta y salud básica de dispositivos y señales/tags | Devices | **Must** |
| Registro de producción (orden/máquina/turno) | Production | **Must** |
| Registro de scrap (motivo + cantidad + costo) | Scrap | **Must** |
| Inspección de calidad con checklist/variables | Quality | **Must** |
| Registro de paradas con motivo (Reason Code) | Downtime | **Must** |
| **Formularios de captura** en tablet (UX operario) para los 5 registros + avance de tarea | Production/Scrap/Quality/Downtime | **Must** |
| **Tablero** en tiempo real (CQRS/read models) con OEE, scrap rate y **progreso de ejecuciones** | Dashboards / Analytics | **Must** |
| Conector Odoo (órdenes/productos/cantidades) con ACL — **opcional: el MVP funciona sin ERP** | Connectors / Integrations | **Should** |
| Job de sincronización con reintentos básicos (solo modo conectado) | Connectors / Integrations | **Should** |
| Historial de eventos inmutable (base de trazabilidad) | Traceability / Event Store | **Should** |
| Auditoría de acciones básicas | Audit | **Should** |
| Adjuntar foto/evidencia a un registro | Files / Media | **Could** |
| Estado de tenants/servicios en Control Plane (salud mínima) | Observability | **Could** |
| Notificación de bienvenida al alta de tenant | Notifications | **Could** |
| Captura automática por protocolos industriales (S7/OPC UA/Modbus/MQTT), motor de reglas, multi-ERP, marketplace, IA, simulación sobre el gemelo | (varios) | **Won't** (fase posterior) |

### 2.3 Dependencias

- **Internas:** el multi-tenancy DB-per-tenant y el Control Plane mínimo son prerrequisito de todo lo demás; Identity & Access habilita el resto de servicios. **La Master Data y el gemelo digital (Capa 1) son ahora prerrequisito de Procesos (Capa 2), que a su vez lo es de Ejecución (Capa 3)**; el Evento canónico (Ingestion) alimenta el Motor de eventos (Capa 4), que es prerrequisito del tablero. El Conector Odoo depende del Evento canónico, pero **ninguna otra pieza depende de él**.
- **Externas:** acceso de red desde el edge del cliente hacia la nube (outbound); datalogger / archivos CSV/Excel del piloto (los PLC/protocolos industriales entran en V1); **catálogos del cliente** (ítems, insumos, unidades) para cargar la master data inicial. La disponibilidad de un **entorno Odoo** deja de ser dependencia bloqueante: solo aplica a pilotos en modo conectado.
- **De arquitectura:** broker de mensajería (event-driven), object storage por tenant, gestión de secretos para cadenas de conexión. Ver [architecture.md](../specs/architecture.md).

### 2.4 Riesgos y mitigaciones

| Riesgo | Impacto | Prob. | Mitigación |
|---|---|---|---|
| Complejidad del alta automatizada de DB-per-tenant (7 pasos) retrasa todo | Alto | Media | Automatizar y probar el flujo como primer hito; idempotencia y rollback por paso; ver [milestones.md](./milestones.md) |
| Conectividad intermitente del edge causa pérdida de datos | Alto | Alta | Store-and-forward + `dedup_key` como Must; pruebas de corte de red desde el diseño |
| Formatos heterogéneos de datalogger/CSV/Excel dificultan el parseo | Medio | Media | Acotar el MVP a datalogger + CSV/Excel con plantillas; validar con archivos reales del piloto; los protocolos industriales (S7/OPC UA/Modbus/MQTT) se acotan en V1 |
| Mapeo Odoo (objetos/direccionalidad) mal definido | Medio | Alta | Cerrar alcance de sincronización con el cliente piloto; ACL aísla el core; **el conector es `Should`: si se atrasa, el MVP igual sale en modo standalone** |
| **La master data propia agranda el MVP** (ABM, importación, validaciones, permisos) y retrasa la fase | Alto | **Alta** | Acotar al mínimo duro (ítems, insumos, UoM, procesos, personas/roles) según **MOD-17**; seed idempotente + importación CSV antes que UI rica; declarar el sobrecosto en la planificación, no absorberlo en silencio |
| **Conciliación al conectar un ERP después** (duplicados, referencias rotas de procesos/ejecuciones vivas) | Alto | Media | Referencia externa por entidad desde el día uno; conciliación asistida con confirmación humana; ver **INT-07** |
| Modelo de tareas insuficiente (solo lineal) obliga a migrar procesos y ejecuciones vivas | Alto | Media | **DAG en el modelo de datos desde el MVP** aunque la UI sea lineal; ver **MOD-18** |
| Evidencia obligatoria mal calibrada frena la línea (o queda decorativa) | Medio | Media | Requisito configurable por tarea con override justificado y auditado; ver **MOD-19** |
| UX de operario insuficiente → los operarios no cargan | Alto | Media | Diseño con operarios reales, pruebas en planta, mínimos toques; ver [ui-ux.md](../specs/ui-ux.md) |
| Fuga de datos entre tenants (aislamiento) | Crítico | Baja | DB-per-tenant no negociable; resolución de tenant por claim/Registry; auditoría; ver [multi-tenancy.md](../specs/multi-tenancy.md) |
| Sobre-alcance (scope creep) hacia features de V1 | Medio | Alta | MoSCoW estricto; "Won't" explícito; disciplina de fase |

### 2.5 Criterios de salida (Exit Criteria)

- [ ] **Alta de tenant end-to-end** ejecuta los 7 pasos y deja la empresa en estado "activo" de forma automatizada y repetible.
- [ ] **Un tenant opera de punta a punta en modo *standalone*, sin ERP:** carga su master data mínima, define un Proceso con Tareas e Insumos, lanza una Ejecución y ve su progreso en el tablero. **Este es el criterio que reemplaza a "sin ERP no hay MVP".**
- [ ] **Producción manual → tablero en tiempo real (caso estrella):** un registro de producción cargado a mano en un formulario de captura se ve en el tablero en tiempo real, atribuido a su activo y a su tarea.
- [ ] **Sincronización con Odoo (solo modo conectado, no bloqueante):** en un tenant con ERP, el mismo registro se sincroniza con Odoo vía el conector. Si no hay tenant piloto con ERP, este criterio se valida en entorno de prueba y **no detiene el cierre de fase**.
- [ ] **Primer dato de datalogger/CSV:** un evento capturado desde un datalogger (carga de archivo/CSV/Excel) se ve en el tablero en tiempo real (y se refleja en Odoo si el tenant está conectado).
- [ ] Los **cinco registros** (producción, scrap, calidad, paradas, eventos) se capturan por **formulario de captura en tablet** y por **datalogger/CSV** (la captura automática por protocolos industriales se valida en V1).
- [ ] **Cada señal está ligada a un activo** del gemelo digital: no existen datos sin dueño físico en el tenant piloto.
- [ ] El **tablero** muestra OEE (con sus tres factores) y scrap rate calculados con las fórmulas canónicas, en tiempo real, **más el progreso de las ejecuciones activas y sus tiempos muertos**.
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
- Abrir el **perfil Proyecto** de la Capa 3 (entregable único, hitos, cronograma, ruta crítica, % de avance) sobre el mismo modelo de Proceso/Tarea/Insumo.
- Completar la **Master Data propia** (clientes/pedidos, centros de costo) y la **conciliación asistida** al conectar un ERP a un tenant que venía en modo *standalone*.

### 3.2 Funcionalidades y prioridad (MoSCoW)

| Funcionalidad | Módulo (BC) | MoSCoW |
|---|---|---|
| Motor de reglas trigger-condición-acción en tiempo real | Rules Engine | **Must** |
| Notificaciones multicanal + plantillas + escalado | Notifications | **Must** |
| Agente Edge/Gateway + adapters Siemens S7, OPC UA y Modbus (captura automática) | Ingestion / Edge Gateway | **Must** |
| Modo híbrido real (manual + automático por planta) | Ingestion / Devices | **Must** |
| Adapter MQTT completo | Ingestion / Edge Gateway | **Should** |
| **Perfil Proyecto** en Ejecución (hitos, cronograma, ruta crítica, % de avance, desvío) | Execution / Work Model | **Must** |
| Editor visual del **grafo de tareas (DAG)** con precedencias y paralelismo | Work Model | **Should** |
| **Master data completa** (clientes/pedidos, centros de costo) y **conciliación asistida** standalone → conectado | Master Data / Connectors | **Must** |
| Métricas derivadas avanzadas de la Capa 4 (productividad por recurso, costo real vs. estimado) | Event Engine / Dashboards | **Should** |
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
- [ ] Un **Proyecto** (entregable único con hitos y fecha objetivo) se planifica y ejecuta con el **mismo modelo** de Proceso/Tarea/Insumo que un lote, con sus KPIs propios (% de avance, desvío de cronograma) y **sin aplicarle OEE**.
- [ ] Un tenant que operaba en **modo standalone conecta un ERP** y la **conciliación asistida** enlaza sus catálogos sin duplicar ítems ni romper procesos/ejecuciones vivas.
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
| **Time-to-value** | Alta 7 pasos + master data mínima + carga manual | Reglas listas | Marketplace autoservicio | Onboarding enterprise |
| **Empaquetado / pricing** | Base por planta (standalone completo; ERP como add-on) | + Precio por dispositivo (protocolos) | Feature flags por peldaño/plan | Add-ons IA / por consumo |
| **Autonomía (ERP opcional)** | Master data propia mínima + modo standalone | Master data completa + conciliación al conectar | Multi-ERP y fuente de verdad por entidad | Integración con MES/SCADA existentes |
| **Modelo de trabajo (Capas 2-3)** | Procesos/Tareas/Insumos + Ejecución de perfil repetitivo | + Perfil Proyecto y DAG visual | Procesos reutilizables entre plantas | Procesos sugeridos/optimizados por IA |

---

## 7. Enlaces de trazabilidad

- Cada fase se descompone en **hitos con criterios de aceptación medibles** en [milestones.md](./milestones.md).
- El trabajo concreto (épicas y user stories con tag de fase) vive en [backlog.md](./backlog.md).
- El **modelo de 4 capas** que estructura todas las fases está en [layered-architecture.md](../specs/layered-architecture.md) y se desarrolla en [digital-twin.md](../specs/digital-twin.md), [work-model.md](../specs/work-model.md), [execution.md](../specs/execution.md), [event-engine.md](../specs/event-engine.md) y [master-data.md](../specs/master-data.md).
- El detalle por módulo y su mapeo a fases está en [modules.md](../specs/modules.md) y los documentos de dominio ([production.md](../specs/production.md), [quality.md](../specs/quality.md), [scrap.md](../specs/scrap.md), [downtime.md](../specs/downtime.md), [traceability.md](../specs/traceability.md), [integrations.md](../specs/integrations.md), [rules-engine.md](../specs/rules-engine.md), [dashboards.md](../specs/dashboards.md), [notifications.md](../specs/notifications.md), [devices.md](../specs/devices.md)).
- Las capacidades marcadas **Won't** en cada fase se documentan como visión futura en [future-features.md](../specs/future-features.md).

---

## Preguntas abiertas

1. **Fechas reales por fase.** El Gantt es orientativo; falta convertir la secuencia en un calendario con capacidad de equipo real y compromisos de cliente.
2. ♻️ **Resuelto (2026-07-11), reencuadrado (2026-07-13):** el conector Odoo del MVP hace *pull* de MO/Producto/UoM/Motivos y *push* de producción real (avance/cierre de MO) y scrap (agregado por cierre de corrida); calidad opcional. **Ese alcance sigue vigente cuando hay ERP, pero la integración pasa a ser opcional y baja a `Should`** (INT-01 marcada "a revisar") — ver [tablero de decisiones](../open-questions-board.md).
3. **Corte MVP/V1 para trazabilidad.** ¿La captura de lote/serie se inicia ya en el MVP (aunque la genealogía completa sea V1) para evitar backfills costosos?
4. **Orden interno de V2.** ¿Multi-ERP antes que Marketplace, o Marketplace primero para habilitar conectores de terceros que aceleren el multi-ERP?
5. **Criterio de entrada a Enterprise.** ¿Qué masa de datos/clientes se requiere para que la IA sea viable y no una promesa? Debe definirse un umbral objetivo.
6. ✅ **Resuelto (2026-07-11):** cada capa se monetiza como **suscripción base por planta + precio por dispositivo conectado**, con módulos empaquetados por capa vía feature flags (Captura base → MES ligero V1 → IA Enterprise) y add-ons por consumo — ver [tablero de decisiones](../open-questions-board.md).
7. **Gestión de "Won't" que se vuelven urgentes.** ¿Qué proceso reevalúa una capacidad diferida si un cliente estratégico la exige antes de tiempo, sin romper la disciplina de fases?
8. **Deuda técnica entre fases.** ¿Cómo se reserva capacidad para hardening/refactor entre fases para no comprometer la escala diseñada?
9. **Cuánto se corre la fecha del MVP por la master data propia.** El alcance nuevo (catálogos, ABM, importación) no es gratis: ¿se absorbe extendiendo el MVP, se recorta otra capacidad, o se libera un MVP standalone acotado y se completa en V1? Depende de **MOD-17**.
10. **Perfil del piloto (repetitivo o proyecto).** Si el piloto termina siendo de perfil **proyecto**, hay que adelantar parte de V1 al MVP (hitos, cronograma). ¿Se acepta ese intercambio o se exige un piloto repetitivo? Ver **PRD-16**.
11. **Criterio de salida del conector ERP.** Con el conector en `Should`, ¿el cierre del MVP exige igualmente una demo Odoo en entorno de prueba, o basta con el modo standalone en producción?
