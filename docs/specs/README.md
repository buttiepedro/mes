# Nexo — Documentación de Producto y Arquitectura

> **Estado:** Borrador v0.1 · **Actualizado:** 2026-07-13
> **Roles que redactaron esta documentación:** Product Manager · Software Architect · UX Designer
> **Nombre del producto:** **Nexo** *(provisional — ver [open-questions.md](./specs/open-questions.md))*

Esta carpeta contiene la **documentación funcional y de arquitectura** de **Nexo**, una plataforma
industrial SaaS que funciona como **sistema autónomo de ejecución y trazabilidad del trabajo en planta**
—para producción repetitiva **y** trabajo por proyecto—, con **integración a ERP opcional**
(agnóstica de ERP; primer ERP soportado: **Odoo**). El objetivo de esta etapa es **definir el producto**:
no contiene código, componentes, APIs, base de datos ni migraciones.

Toda la documentación asume dos requisitos de arquitectura **no negociables**:

- **Multi-tenant con base de datos por tenant** (una base independiente por empresa, estilo *Hexa*),
  con una **base Global (Control Plane)** exclusiva del proveedor.
- **Cloud Native, microservicios y DDD**, diseñado para escalar a miles de empresas, cientos de miles
  de dispositivos y millones de eventos diarios — sin depender del ERP.

El producto se ordena sobre un **modelo de 4 capas** (gemelo digital → modelo de trabajo → ejecución →
motor de eventos); el ERP **no es una capa**, es un conector opcional. Ver
[layered-architecture.md](./specs/layered-architecture.md).

> [!IMPORTANT]
> **Decisiones pendientes → [open-questions-board.md](./open-questions-board.md).**
> Tablero maestro con las **105 preguntas abiertas** consolidadas de todos los documentos, priorizadas
> (P0/P1/P2), con recomendación por defecto y columna de **Respuesta**. Es el punto de entrada para
> tomar decisiones y saber qué documentos reescribir después de cada una.

---

## Cómo leer esta documentación

**Recorrido sugerido (de negocio a técnico):**

1. [idea.md](./idea.md) → [specs/product.md](./specs/product.md) → [specs/modules.md](./specs/modules.md)
2. **Modelo por capas:** [specs/layered-architecture.md](./specs/layered-architecture.md) → [digital-twin](./specs/digital-twin.md) → [work-model](./specs/work-model.md) → [execution](./specs/execution.md) → [event-engine](./specs/event-engine.md) → [master-data](./specs/master-data.md)
3. [specs/architecture.md](./specs/architecture.md) → [specs/multi-tenancy.md](./specs/multi-tenancy.md) → [specs/data-ingestion.md](./specs/data-ingestion.md)
4. Módulos de dominio: producción, calidad, scrap, paradas, trazabilidad
5. [specs/data-model.md](./specs/data-model.md) → [specs/ui-ux.md](./specs/ui-ux.md) → [specs/mockups.md](./specs/mockups.md)
6. [roadmap/](./roadmap/roadmap.md) y [specs/open-questions.md](./specs/open-questions.md)

**Glosario transversal:** [specs/glossary.md](./specs/glossary.md) (términos industriales y fórmulas de KPI canónicas).

---

## Estructura

```text
specs/
├── README.md                  ← este índice
├── idea.md                    ← la idea y el problema
│
├── roadmap/
│   ├── vision.md
│   ├── roadmap.md
│   ├── milestones.md
│   └── backlog.md
│
└── specs/
    ├── product.md             ├── rules-engine.md
    ├── architecture.md        ├── users-permissions.md
    ├── modules.md             ├── notifications.md
    ├── production.md          ├── data-model.md
    ├── quality.md             ├── ui-ux.md
    ├── scrap.md               ├── mockups.md
    ├── downtime.md            ├── glossary.md
    ├── traceability.md        ├── open-questions.md
    ├── devices.md             ├── future-features.md
    ├── integrations.md        ├── multi-tenancy.md   (agregado)
    ├── dashboards.md          ├── control-plane.md   (agregado)
    ├── scalability.md (agr.)  ├── security.md        (agregado)
    ├── reports.md   (agr.)    ├── data-ingestion.md  (agregado)
    │
    │   ── Modelo por capas ──
    ├── layered-architecture.md   ← documento ancla de las 4 capas
    ├── digital-twin.md    (Capa 1)   ├── execution.md     (Capa 3)
    ├── work-model.md      (Capa 2)   ├── event-engine.md  (Capa 4)
    └── master-data.md     (catálogos propios · ERP opcional)
```

---

## Índice de documentos

### Punto de partida

| Documento | Qué contiene |
|---|---|
| [idea.md](./idea.md) | El problema (el trabajo en planta no está medido), visión, modelo de 4 capas, propuesta de valor, qué **es** y qué **no es**, ERP opcional, fuentes de datos e industrias objetivo (repetitivo y proyecto). |

### Modelo por capas

Los cinco documentos que definen el **encuadre canónico del producto**: cuatro capas —cada una depende
solo de la de abajo— más la master data que las alimenta. El **ERP no es una capa**: es un conector
opcional conectado lateralmente.

| Documento | Qué contiene |
|---|---|
| [layered-architecture.md](./specs/layered-architecture.md) | **Documento ancla:** el modelo de 4 capas completo, cómo se relacionan, el principio de dependencia hacia abajo y por qué el ERP queda fuera del modelo como conector opcional. |
| [digital-twin.md](./specs/digital-twin.md) | **Capa 1 · Física:** gemelo digital de la planta (Empresa→Planta→Sector→Línea→Activo), binding sensor↔activo, estado en vivo y distinción canónica entre *formulario de captura* y *tablero de KPI*. |
| [work-model.md](./specs/work-model.md) | **Capa 2 · Modelo de trabajo:** Procesos versionados, Tareas con precedencias (DAG), Insumos, responsables y tiempos; perfiles **repetitivo** y **proyecto** sobre un mismo modelo. |
| [execution.md](./specs/execution.md) | **Capa 3 · Ejecución:** la instancia viva del Proceso (Run) en sus dos sabores, **Lote** y **Proyecto**: ciclo de vida, tareas instanciadas, consumo real, avance, evidencia y cierre. |
| [event-engine.md](./specs/event-engine.md) | **Capa 4 · Motor de eventos:** contrato del Evento canónico (fecha, origen, valor, evidencia) y métricas derivadas —progreso, cuellos de botella, tiempos muertos, productividad y costo real. |
| [master-data.md](./specs/master-data.md) | Catálogos propios del tenant (ítems, insumos, UoM, procesos, personas/roles, clientes, centros de costo) y los dos modos de operación: **standalone** (sin ERP) y **conectado**. |

### Visión y Roadmap (`roadmap/`)

| Documento | Qué contiene |
|---|---|
| [vision.md](./roadmap/vision.md) | Misión/visión, North Star, panorama a 3 años, pilares estratégicos y principios de producto. |
| [roadmap.md](./roadmap/roadmap.md) | Fases **MVP / V1 / V2 / Enterprise**: funcionalidades, prioridad (MoSCoW), dependencias y riesgos. |
| [milestones.md](./roadmap/milestones.md) | Hitos concretos con criterios de aceptación medibles por fase. |
| [backlog.md](./roadmap/backlog.md) | Backlog inicial: épicas y user stories por módulo, con tag de fase y marca de MVP. |

### Producto y Arquitectura

| Documento | Qué contiene |
|---|---|
| [product.md](./specs/product.md) | Visión de producto, posicionamiento, modelo de 4 capas a alto nivel, personas, propuesta de valor, alcance MVP e **impacto de alcance del pivot**, licenciamiento y métricas de éxito. |
| [architecture.md](./specs/architecture.md) | Arquitectura Cloud Native, DDD y event-driven; bounded contexts/microservicios justificados; vistas C4; ADRs. |
| [modules.md](./specs/modules.md) | Catálogo de módulos con su microservicio, fase, dependencias y persona principal. |
| [multi-tenancy.md](./specs/multi-tenancy.md) | *(agregado)* Estrategia **DB-por-tenant**, Control Plane, resolución de tenant, flujo de alta (7 pasos) y aislamiento. |
| [control-plane.md](./specs/control-plane.md) | *(agregado)* Plataforma global del proveedor: empresas, licencias, usuarios globales, observabilidad, versiones, marketplace, facturación. |
| [scalability.md](./specs/scalability.md) | *(agregado)* Metas de escala, sharding por tenant, distribución geográfica, time-series, capacity planning. |
| [security.md](./specs/security.md) | *(agregado)* Aislamiento total, AuthN/AuthZ, cifrado, seguridad del edge, auditoría, cumplimiento y modelo de amenazas. |
| [data-ingestion.md](./specs/data-ingestion.md) | *(agregado)* Pipeline de ingesta y **normalización a evento canónico**: edge, adapters de protocolo, validación, dedup, enrutamiento. |

### Módulos de dominio (planta)

| Documento | Qué contiene |
|---|---|
| [production.md](./specs/production.md) | **Perfil repetitivo** del modelo de trabajo: órdenes (como disparador), cantidades, tiempos, turnos, máquinas, estados y productividad. |
| [quality.md](./specs/quality.md) | Inspecciones, variables, checklists, defectos, tolerancias, disposición y FPY. |
| [scrap.md](./specs/scrap.md) | Motivos, costos, responsables, clasificación, estadísticas y Scrap Rate. |
| [downtime.md](./specs/downtime.md) | Paradas programadas/no programadas, MTBF, MTTR, causas y aporte a la Disponibilidad. |
| [traceability.md](./specs/traceability.md) | Event store inmutable, genealogía lote/serie, historial, auditoría y origen del dato. |

### Conectividad e integraciones

| Documento | Qué contiene |
|---|---|
| [devices.md](./specs/devices.md) | Modelado de PLCs, sensores, gateways, ESP32, cámaras, dataloggers; protocolos, salud y firmware/OTA. |
| [integrations.md](./specs/integrations.md) | **Conector opcional:** arquitectura de **conectores + ACL** que desacopla el Core del ERP; Odoo detallado, SAP/Dynamics/Oracle y webhooks. El sistema funciona sin este módulo. |

### Inteligencia y engagement

| Documento | Qué contiene |
|---|---|
| [dashboards.md](./specs/dashboards.md) | KPIs (OEE, disponibilidad, rendimiento, calidad, scrap…), tiempo real vs histórico, CQRS y andon. |
| [rules-engine.md](./specs/rules-engine.md) | Motor de reglas trigger-condición-acción en tiempo real, alertas y workflows. |
| [notifications.md](./specs/notifications.md) | Notificaciones multicanal, plantillas, preferencias y escalado. |
| [reports.md](./specs/reports.md) | Reportes on-demand/programados, exportables y constructor de reportes. |

### Accesos y experiencia de usuario

| Documento | Qué contiene |
|---|---|
| [users-permissions.md](./specs/users-permissions.md) | Roles, RBAC con scoping por planta/línea, matriz de permisos, SSO/MFA. |
| [ui-ux.md](./specs/ui-ux.md) | Navegación, pantallas y experiencias tablet/desktop/mobile, con la justificación de cada decisión. |
| [mockups.md](./specs/mockups.md) | Wireframes descriptivos (listos para Figma) de las 11 pantallas principales. |

### Modelo y referencia

| Documento | Qué contiene |
|---|---|
| [data-model.md](./specs/data-model.md) | Modelo **conceptual** de negocio (sin tablas SQL): entidades canónicas y sus relaciones. |
| [glossary.md](./specs/glossary.md) | Glosario de términos industriales y del producto + fórmulas de KPI canónicas. |
| [open-questions.md](./specs/open-questions.md) | Preguntas a resolver antes de desarrollar, categorizadas y priorizadas. |
| [future-features.md](./specs/future-features.md) | Funcionalidades futuras: IA, visión artificial, gemelo digital **de simulación** (el gemelo operativo de Capa 1 es parte del producto), predictivo, energía, MES/SCADA. |

---

## Documentos agregados por iniciativa (y por qué)

El prompt original definía 25 archivos. Se agregaron **6 documentos** porque el propio pedido los
requería con peso de módulo propio y mezclarlos habría diluido su detalle:

| Agregado | Justificación |
|---|---|
| [multi-tenancy.md](./specs/multi-tenancy.md) | El requisito **DB-por-tenant** es central y transversal; merece un documento dedicado, no un apartado. |
| [control-plane.md](./specs/control-plane.md) | El prompt pide explícitamente un **módulo específico** para la plataforma de administración global. |
| [scalability.md](./specs/scalability.md) | Las metas de escala condicionan cada decisión y ameritan su propio análisis. |
| [security.md](./specs/security.md) | El **aislamiento entre tenants** se define como principio fundamental → documento propio. |
| [reports.md](./specs/reports.md) | *Reportes* aparece como microservicio y como feature del Control Plane. |
| [data-ingestion.md](./specs/data-ingestion.md) | La **normalización de eventos** es el núcleo de valor del producto (planta → evento canónico). |

Más tarde, el **cambio de posicionamiento** (sistema autónomo, ERP opcional, unificación de producción
repetitiva y trabajo por proyecto) incorporó **6 documentos adicionales** que forman la sección
**Modelo por capas**:

| Agregado | Justificación |
|---|---|
| [layered-architecture.md](./specs/layered-architecture.md) | El modelo de 4 capas es el encuadre canónico del producto; necesita un documento ancla que explique el conjunto. |
| [digital-twin.md](./specs/digital-twin.md) | El gemelo de la planta y el **binding sensor↔activo** son la base de toda atribución de eventos y métricas. |
| [work-model.md](./specs/work-model.md) | Unificar proyecto y producción repetitiva en un mismo modelo de Proceso/Tarea/Insumo amplía el mercado y exige su propia especificación. |
| [execution.md](./specs/execution.md) | La instancia viva del trabajo (Lote o Proyecto) generaliza la corrida de producción y tiene ciclo de vida propio. |
| [event-engine.md](./specs/event-engine.md) | El contrato del evento y las **métricas derivadas** (progreso, cuellos de botella, tiempos muertos) son el corazón del valor. |
| [master-data.md](./specs/master-data.md) | Consecuencia obligatoria de que el ERP sea opcional: sin ERP, la plataforma debe poseer sus propios catálogos. |

---

## Convenciones

- **Idioma:** español (es-AR). **Sin código**: solo documentación, tablas y diagramas Mermaid.
- **Nombres canónicos** de microservicios, entidades y fórmulas de KPI son consistentes entre todos
  los documentos (ver [architecture.md](./specs/architecture.md), [data-model.md](./specs/data-model.md) y [glossary.md](./specs/glossary.md)).
- Cada documento incluye encabezado con estado/fecha, resumen ejecutivo, referencias cruzadas y una
  sección final **"Preguntas abiertas"** que se consolida en [open-questions.md](./specs/open-questions.md).

---

## Próximo paso

Esta documentación deja la base para pasar a la **etapa de diseño técnico** (contratos de servicios,
esquemas por tenant, especificación del Agente Edge y del conector Odoo) y, recién después, al desarrollo.
Antes de eso, conviene resolver las [preguntas abiertas](./specs/open-questions.md), empezando por
confirmar el nombre definitivo del producto.
