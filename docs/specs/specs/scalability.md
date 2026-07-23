# Escalabilidad y Capacity Planning

> **Documento:** `specs/specs/scalability.md` · **Estado:** Borrador v0.1 · **Actualizado:** 2026-07-11
> **Roles:** Product Manager · Software Architect · UX Designer
> **Relacionados:** [architecture.md](./architecture.md) · [multi-tenancy.md](./multi-tenancy.md) · [data-ingestion.md](./data-ingestion.md) · [control-plane.md](./control-plane.md) · [glossary.md](./glossary.md)

## Resumen ejecutivo

Nexo se diseña para operar a **escala industrial masiva**: miles de empresas, miles de plantas, decenas de miles de usuarios, cientos de miles de dispositivos y **millones de eventos diarios**, con integraciones simultáneas contra múltiples ERPs. Toda decisión de arquitectura debe justificarse contra estas metas canónicas (sección 7 del brief). Este documento traduce esas metas en una **estrategia de escalado concreta**: cómo crece cada parte del sistema, dónde están los cuellos de botella y cómo se mitigan.

La estrategia se apoya en dos palancas estructurales. La primera es el **escalado horizontal por servicio**: al ser microservicios desacoplados por el backbone de eventos (ver [architecture.md](./architecture.md)), cada dominio escala de forma independiente según su propio perfil de carga —la ingesta absorbe picos de captura, los reportes escalan en ráfagas de cómputo, los dashboards escalan con la concurrencia de lectura—. La segunda es el **particionamiento natural por tenant**: el modelo **base de datos por tenant** (ver [multi-tenancy.md](./multi-tenancy.md)) es, en sí mismo, una estrategia de *sharding* que reparte la carga de datos entre bases independientes y elimina el cuello de botella de una base compartida monolítica.

Sobre esas palancas se construyen los mecanismos habilituantes: **colas/broker** para absorber picos y aplicar backpressure (backbone **tipo Kafka detrás de una abstracción**, agnóstico de nube y con opción de *managed* equivalente sin acoplarse a primitivas propietarias — decisión ARQ-01, ver [architecture.md](./architecture.md)), **almacenamiento time-series** con retención y *downsampling* para las lecturas de alta frecuencia, **read models materializados (CQRS)** para servir dashboards y reportes sin castigar la escritura, **caching** en capas, **balanceo de carga** y **autoscaling** por métricas de carga real, y la capacidad de **distribuir geográficamente** las bases de tenant y **migrarlas de forma transparente** entre servidores/clústeres.

Este documento presenta las metas, la estrategia por dimensión, un ejercicio de *capacity planning* con supuestos de carga explícitos, y un registro de cuellos de botella con sus mitigaciones. Las cifras son **supuestos de diseño** para dimensionar, no compromisos contractuales; se refinan con datos reales de operación.

---

## 1. Metas de escala (canónicas)

Del brief (sección 7), las metas de diseño que gobiernan este documento:

| Dimensión | Meta de diseño |
|---|---|
| Empresas (tenants) | **Miles** |
| Plantas (sites) | **Miles** |
| Usuarios | **Decenas de miles** |
| Dispositivos | **Cientos de miles** |
| Eventos | **Millones por día** |
| Integraciones ERP | **Múltiples y simultáneas** |

Estrategias base declaradas: *sharding* por tenant (DB-per-tenant ya particiona), colas/broker para picos, time-series para lecturas, read models materializados, caching, autoscaling por servicio, y backpressure + store-and-forward en el edge (ver [data-ingestion.md](./data-ingestion.md)).

---

## 2. Escalado horizontal por servicio

Cada microservicio escala **independientemente** según su perfil de carga, porque el desacople por eventos elimina dependencias de despliegue conjunto (ver [architecture.md](./architecture.md)). Servicios *stateless* en Kubernetes escalan sumando réplicas; el estado vive en almacenes gestionados que escalan con sus propias estrategias.

| Servicio | Driver de carga dominante | Dimensión de escalado | Señal de autoscaling |
|---|---|---|---|
| **Ingestion / Edge Gateway** | Eventos/seg entrantes, picos | Réplicas de admisión + particiones de broker | Lag de consumidor, profundidad de cola, CPU |
| **API Gateway** | Peticiones/seg de clientes | Réplicas + balanceo | RPS, latencia p95, CPU |
| **Production / Quality / Scrap / Downtime** | Eventos de dominio/seg por tenant | Réplicas de consumidor por tópico particionado | Lag por partición, CPU |
| **Traceability / Event Store** | Volumen append-only | Réplicas de consumo + almacenamiento | Lag, tasa de escritura |
| **Dashboards / Analytics** | Concurrencia de lectura, tiempo real | Réplicas de lectura + read models + caché | RPS de consulta, latencia, hit ratio de caché |
| **Reports** | Ráfagas de generación/exportación | Workers escalables (colas de trabajo) | Profundidad de cola de trabajos |
| **Rules Engine** | Eventos evaluados/seg | Réplicas de evaluación | Lag, latencia de evaluación |
| **Connectors / Integrations** | Jobs de sync por ERP/tenant | Workers por conector | Cola de sync, reintentos pendientes |
| **Notifications** | Mensajes/seg | Workers por canal | Cola de envío |
| **Files / Media** | Ancho de banda de binarios | Escala de object storage + réplicas de servicio | Throughput, tamaño de cola de upload |

- **Aislamiento de rendimiento entre servicios:** un pico en Reports no consume la capacidad de Ingestion. Los límites de servicio son también límites de contención de carga.
- **Aislamiento por tenant:** *rate limiting*/*throttling* por tenant en el borde evita que un tenant ruidoso degrade a otros (*noisy neighbor*).

---

## 3. Particionamiento por tenant (DB-per-tenant como sharding)

El modelo **base de datos por tenant** es la estrategia de particionamiento primaria y la principal ventaja de escala de la plataforma.

- **Sharding natural:** cada tenant ya es una partición física de datos. No existe una tabla global gigante que se convierta en cuello de botella; la carga de datos de un tenant no compite con la de otro a nivel de base.
- **Crecimiento lineal por incorporación:** sumar tenants = sumar bases independientes. El sistema crece **horizontalmente en el eje de clientes** sin re-particionar datos existentes.
- **Reparto entre servidores/clústeres:** las DBs de tenant se distribuyen entre múltiples servidores/clústeres de base de datos. Los tenants grandes pueden aislarse en infraestructura dedicada; los pequeños se agrupan de forma densa para eficiencia de costos.
- **Blast radius acotado:** un problema de rendimiento o un incidente en la DB de un tenant no afecta a los demás. Mantenimiento, migraciones y *tuning* se aplican por tenant.
- **Migraciones de esquema a escala (decisión TEN-01):** el crecimiento del esquema sobre miles de bases de tenant se gestiona con **migraciones versionadas, idempotentes y desplegadas por cohortes con *feature flags***, con **zero-downtime como objetivo**: cada cohorte de tenants se migra y valida de forma incremental, y la funcionalidad nueva se activa por flag una vez que el esquema está listo, evitando ventanas de indisponibilidad global.
- **Sin lógica de negocio dependiente de la ubicación:** la resolución de tenant vía **Tenant Connection Registry** (ver [multi-tenancy.md](./multi-tenancy.md) y [control-plane.md](./control-plane.md)) abstrae **dónde** vive cada DB; la lógica de dominio no cambia.

> Comparación de estrategias multi-tenant (a título ilustrativo; la recomendación y el diseño asumen DB-per-tenant):

| Estrategia | Aislamiento | Escala de datos | Complejidad operativa | Encaje con Nexo |
|---|---|---|---|---|
| Shared DB (discriminador) | Bajo | Cuello de botella en tablas comunes | Baja | No (viola aislamiento no negociable) |
| Schema-per-tenant | Medio | Límite por instancia | Media | Parcial; no da distribución/migración por tenant |
| **DB-per-tenant** | **Alto** | **Sharding natural, distribuible** | **Media-alta (mitigada por automatización)** | **Elegida** |

---

## 4. Distribución geográfica y migración transparente

- **Distribución geográfica:** como cada DB de tenant es autónoma, puede alojarse en la **región más cercana a la planta** del cliente, reduciendo latencia y habilitando requisitos de **residencia de datos** (datos que no salen de un país/región).
- **Migración transparente:** una empresa puede **moverse** de un servidor/clúster/región a otro sin cambios en la lógica de negocio. El procedimiento funcional: aprovisionar destino → replicar/copiar datos → sincronizar el delta → **actualizar la cadena de conexión en el Registry** → conmutar → verificar → liberar origen. La aplicación sigue resolviendo el tenant por el Registry, ajeno a la mudanza.
- **Rebalanceo de densidad:** los tenants se redistribuyen entre clústeres para equilibrar carga y costo (p. ej. mover un tenant que creció mucho a infraestructura dedicada).
- **Servicios stateless facilitan multi-región:** los microservicios de cómputo, al no guardar estado, se replican por región; el estado sigue al tenant vía su DB. Alta disponibilidad multi-región es capacidad de fase Enterprise (ver [roadmap](../roadmap/roadmap.md)).

---

## 5. Almacenamiento time-series y retención

Las **lecturas/señales** (`type=reading`) de alta frecuencia son la fuente de mayor volumen. Se tratan con un almacén **time-series** especializado, separado del estado transaccional de dominio.

- **Perfil de escritura:** append-only, alta cardinalidad (cientos de miles de dispositivos × múltiples tags), alta frecuencia. El motor time-series está optimizado para esto.
- **Downsampling / rollups:** se conservan agregaciones (por minuto/hora/día) que sirven la mayoría de las consultas de dashboards; el dato crudo de alta resolución se retiene por menos tiempo.
- **Retención escalonada (tiering):** política por antigüedad —crudo reciente en almacenamiento rápido, histórico agregado en almacenamiento barato, y purga/archivo según retención del tenant/plan—.
- **Separación de responsabilidades:** los **eventos de dominio** (producción, scrap, calidad, paradas) van a la DB relacional del tenant y al Event Store (retención larga, inmutable, ver [traceability.md](./traceability.md)); las **lecturas** van a time-series. Esto evita inflar la base relacional con volumen de sensores.

| Capa de dato | Almacén | Retención típica (a definir por plan) | Uso |
|---|---|---|---|
| Lecturas crudas alta frecuencia | Time-series (hot) | Corta | Diagnóstico fino, zoom temporal |
| Lecturas agregadas (rollups) | Time-series (warm) | Media-larga | Dashboards, tendencias |
| Eventos de dominio | Relacional por tenant + Event Store | Larga / inmutable | KPIs, trazabilidad, auditoría |
| Read models | Store de lectura (CQRS) | Reconstruible | Dashboards/Reports en vivo |

---

## 6. Read models materializados (CQRS)

- **Separación lectura/escritura:** el lado de escritura (dominios) publica eventos; el lado de lectura materializa **read models** optimizados para consulta (ver [architecture.md](./architecture.md)). Dashboards y Reports **no consultan las tablas transaccionales**.
- **Escala independiente de la lectura:** los read models escalan con la concurrencia de usuarios sin afectar la captura ni la escritura de dominio.
- **Precomputación de KPIs:** OEE, Disponibilidad, Rendimiento, Calidad, Scrap Rate, FPY, MTBF/MTTR (fórmulas canónicas del brief §10.1) se **precalculan/materializan** para responder en tiempo real, en vez de recomputar sobre datos crudos en cada consulta.
- **Reconstruibles:** ante cambios de vista o incidentes, se **reproyectan** desde el log de eventos/Event Store (ver [data-ingestion.md](./data-ingestion.md)), sin tocar la fuente de verdad.
- **Tiempo real:** actualización de tableros vía push (WebSocket/SSE) alimentado por el flujo de eventos, con read models como respaldo consultable.

---

## 7. Caching

Caching en capas para reducir latencia y descargar los almacenes:

| Capa | Qué cachea | Beneficio | Riesgo/gestión |
|---|---|---|---|
| Borde / CDN | Assets estáticos, binarios de Files/Media | Descarga de origin, latencia baja | Invalidación por versión |
| Aplicación (distribuida) | Read models calientes, KPIs, catálogos, resoluciones de tenant | Menos golpes a DB/read store | Invalidación por evento, TTL |
| Registry de conexión | Cadenas de conexión de tenant resueltas | Evita golpear el Registry en cada request | Invalidación al migrar tenant |
| Consulta | Resultados de consultas frecuentes de dashboards | Latencia y costo de cómputo | Coherencia con actualizaciones |

- **Invalidación dirigida por eventos:** los cambios se propagan por el backbone; las cachés se invalidan/actualizan al recibir el evento correspondiente, manteniendo coherencia razonable con consistencia eventual.
- **Cuidado con la coherencia por tenant:** las cachés se segmentan por `tenant_id` para no cruzar datos entre empresas (control de aislamiento, ver [multi-tenancy.md](./multi-tenancy.md)).

---

## 8. Balanceo de carga y autoscaling

- **Balanceo de carga:** en el borde (API Gateway) y entre réplicas de cada servicio; distribución de consumidores entre particiones del broker; balanceo de conexiones a las DBs (pooling).
- **Autoscaling por métricas de carga real:** además de CPU/memoria, se escala por **señales de carga de negocio**: *lag* de consumidor, profundidad de cola, RPS, latencia p95, profundidad de cola de trabajos (Reports/Sync). Esto reacciona a picos de ingesta antes de que se acumule backlog.
- **Escalado predictivo/programado:** para patrones conocidos (arranques de turno, cierres de mes con reportes masivos) se puede pre-escalar.
- **Backpressure integrado:** cuando el sistema no puede escalar instantáneamente, el backpressure (broker → admisión → buffer del edge) evita la pérdida de datos difiriendo el trabajo (ver [data-ingestion.md](./data-ingestion.md)).

---

## 9. Capacity planning (supuestos de carga)

Ejercicio de dimensionamiento con **supuestos explícitos**. Objetivo: acotar órdenes de magnitud, no fijar compromisos. Las cifras se recalibran con telemetría real de Observability.

### 9.1 Supuestos base

| Supuesto | Valor asumido | Comentario |
|---|---|---|
| Tenants activos | 5.000 | Meta "miles"; se usa una cifra media para dimensionar |
| Plantas por tenant (promedio) | 1–3 | Muchos tenants pequeños, pocos grandes (distribución sesgada) |
| Dispositivos por planta (promedio) | 30 | Cientos de miles de dispositivos en agregado |
| Dispositivos totales | ~300.000 | Consistente con la meta "cientos de miles" |
| Eventos de dominio por dispositivo/día | 100 | Producción/scrap/calidad/paradas/eventos de máquina |
| Lecturas (`reading`) por dispositivo/día | Mucho mayores | Alta frecuencia; van a time-series, no a eventos de dominio |
| Usuarios totales | ~30.000 | Decenas de miles |
| Usuarios concurrentes pico | ~5–10% | Dashboards en tiempo real por turno |

### 9.2 Estimaciones derivadas

| Métrica | Cálculo (orden de magnitud) | Resultado aproximado |
|---|---|---|
| Eventos de dominio/día | 300.000 dispositivos × 100 | ~30 millones/día → **millones/día** (meta cumplida) |
| Eventos de dominio/seg (promedio) | 30M / 86.400 s | ~350 eventos/seg promedio |
| Eventos de dominio/seg (pico) | Factor de pico ×10–20 (arranques/reconexiones) | ~3.500–7.000 eventos/seg pico |
| Lecturas time-series/seg | Depende de frecuencia por tag (órdenes de magnitud superiores) | Dimensiona el motor time-series y el downsampling |
| Jobs de sync ERP/día | Por tenant × frecuencia de sync | Escala workers de Connectors |
| Concurrencia de lectura pico | ~1.500–3.000 usuarios simultáneos | Dimensiona read models + caché + réplicas de Dashboards |

- **Lectura clave:** el **promedio es modesto (~350 ev/s)** pero el **pico manda** (×10–20). El sistema se dimensiona para el pico con broker como amortiguador y autoscaling; sin el broker, los picos serían el principal riesgo de pérdida.
- **Distribución sesgada de tenants:** unos pocos tenants grandes concentran mucha carga. Se los aísla en DBs/infra dedicada; la mayoría de tenants pequeños se agrupan densamente.
- **Time-series domina el volumen bruto:** las lecturas de alta frecuencia superan por órdenes de magnitud a los eventos de dominio; por eso se separan a time-series con downsampling y retención escalonada (sección 5).

---

## 10. Cuellos de botella y mitigaciones

| # | Cuello de botella | Por qué aparece | Mitigación |
|---|---|---|---|
| B-01 | Base de datos compartida | Contención en tablas globales | **No aplica**: DB-per-tenant elimina la base común operativa (sección 3) |
| B-02 | Picos de ingesta | Ráfagas ×10–20 sobre el promedio | Broker como amortiguador, backpressure, store-and-forward, autoscaling por lag (§7, [data-ingestion.md](./data-ingestion.md)) |
| B-03 | API Gateway saturado | Punto de entrada único | Réplicas + balanceo + rate limiting por tenant; evitar lógica pesada en el borde (§8, [architecture.md](./architecture.md)) |
| B-04 | Volumen de lecturas en DB relacional | Sensores de alta frecuencia inflando la base | Separar a time-series con downsampling/retención (§5) |
| B-05 | Dashboards pesados sobre datos crudos | Recomputar KPIs en cada consulta | CQRS con read models materializados + caché (§6, §7) |
| B-06 | Tenant ruidoso (noisy neighbor) | Un tenant consume recursos compartidos | Rate limiting por tenant, particionamiento, aislamiento de tenants grandes (§2, §3) |
| B-07 | Tenant Connection Registry en el camino caliente | Resolver conexión en cada request | Caché de resolución de tenant con invalidación al migrar (§7, [control-plane.md](./control-plane.md)) |
| B-08 | Hot partition en el broker | Clave de partición mal elegida concentra tráfico | Clave por tenant + dispositivo/línea; rebalanceo de particiones (§8, [data-ingestion.md](./data-ingestion.md)) |
| B-09 | Reportes masivos (cierre de mes) | Ráfaga de cómputo pesado | Workers escalables por cola de trabajos + pre-escalado programado (§2, §8) |
| B-10 | Reintentos de sync ERP acumulados | ERP lento/caído genera backlog | Colas por conector, backoff, reintentos idempotentes, aislamiento por conector ([integrations.md](./integrations.md)) |
| B-11 | Costo/límite de aprovisionar miles de DBs | DB-per-tenant multiplica bases | Automatización de provisioning, densificación de tenants pequeños, tiering de infra, **migraciones versionadas/idempotentes por cohortes con *feature flags* (zero-downtime objetivo)** ([control-plane.md](./control-plane.md)) |
| B-12 | Flota de agentes edge | Cientos de miles de dispositivos vía agentes | Envío por lote, compresión, OTA gestionado, telemetría de salud ([devices.md](./devices.md)) |

---

## 11. Referencias cruzadas

- Arquitectura, CQRS, broker y topología: [architecture.md](./architecture.md)
- Multi-tenancy (DB-per-tenant), Registry y aislamiento: [multi-tenancy.md](./multi-tenancy.md)
- Pipeline de ingesta, picos, backpressure y reproceso: [data-ingestion.md](./data-ingestion.md)
- Control Plane, provisioning y distribución de DBs: [control-plane.md](./control-plane.md)
- Roadmap (distribución geográfica, multi-región Enterprise): [roadmap](../roadmap/roadmap.md)

---

## Preguntas abiertas

1. **Factor de pico real:** ¿el ×10–20 asumido para picos de ingesta se sostiene con datos reales? Requiere telemetría de las primeras plantas para recalibrar el dimensionamiento.
2. **Densidad de tenants por clúster de DB:** ¿cuántas DBs de tenant por servidor/clúster antes de degradar? ¿Qué umbral de tamaño/carga dispara el aislamiento en infra dedicada?
3. **Retención por capa y plan:** ¿qué retención concreta (crudo/rollups/eventos/read models) se ofrece por plan comercial y cómo impacta en costo de almacenamiento?
4. **Frecuencia de lecturas por tag:** falta el supuesto de frecuencia de `reading` por tipo de señal para dimensionar con precisión el motor time-series.
5. **Estrategia de multi-región:** ¿cuándo se activa distribución geográfica y qué SLA de latencia/residencia se compromete por región (fase Enterprise)?
6. **Automatización de rebalanceo:** ¿el rebalanceo de tenants entre clústeres es manual/asistido o automático por métricas? ¿Ventanas de mantenimiento para migración?
7. **Presupuesto de costos por tenant:** ¿cuál es el costo objetivo por tenant pequeño vs. grande, para validar la viabilidad económica de DB-per-tenant a escala de miles?
