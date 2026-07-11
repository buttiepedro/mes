# UI / UX

> **Documento:** `specs/specs/ui-ux.md` · **Estado:** Borrador v0.1 · **Actualizado:** 2026-07-11
> **Roles:** Product Manager · Software Architect · UX Designer
> **Relacionados:** [mockups.md](./mockups.md) · [dashboards.md](./dashboards.md) · [users-permissions.md](./users-permissions.md) · [production.md](./production.md) · [quality.md](./quality.md) · [scrap.md](./scrap.md) · [downtime.md](./downtime.md) · [devices.md](./devices.md) · [integrations.md](./integrations.md) · [rules-engine.md](./rules-engine.md) · [notifications.md](./notifications.md) · [glossary.md](./glossary.md)

## Resumen ejecutivo

Este documento define los **principios de diseño, la navegación, la estructura de pantallas y los flujos de trabajo por persona** de **Nexo**, la capa única de captura de datos industriales entre la planta y el ERP. El punto de partida no es la estética sino el **contexto físico** en el que se usa el producto: un operario a pie de línea con guantes, ruido, poca luz o luz cegadora, cortes de red intermitentes y presión de ritmo de producción, no puede usar la misma interfaz que un gerente que analiza OEE desde una laptop en su oficina. Por eso Nexo se diseña como **una plataforma con tres experiencias deliberadamente distintas** —**tablet industrial**, **desktop** y **mobile**— que comparten sistema de diseño, lenguaje y datos, pero optimizan superficies, densidad y modos de interacción para su usuario y su entorno.

La tesis de diseño es que **la carga de datos manual es el enemigo a vencer** (es la promesa de valor del producto), así que cada decisión de UX se juzga por cuánto reduce fricción, error y tiempo de captura sin sacrificar la trazabilidad. En piso, eso se traduce en **objetivos táctiles grandes, alto contraste, flujos de pocos toques, feedback inmediato y operación offline con store-and-forward**; en escritorio, en **densidad informativa, análisis comparativo y configuración segura**; en mobile, en **estar al tanto** (alertas y KPIs) más que en operar.

Todo el documento se rige por dos reglas transversales: (1) **la interfaz se adapta al rol y a su alcance** —lo que ve un usuario está determinado por su rol y scoping según [users-permissions.md](./users-permissions.md); no se muestran acciones que no puede ejecutar—; y (2) **cada decisión de UX se justifica por su porqué**, no solo se describe. Los wireframes descriptivos que materializan estas decisiones viven en [mockups.md](./mockups.md); los tableros y KPIs, en [dashboards.md](./dashboards.md).

---

## 1. Principios de diseño (y su porqué)

Cada principio es una regla de decisión, no un eslogan. Cuando dos opciones de diseño compiten, gana la que mejor honra estos principios en orden.

| # | Principio | Qué exige en la práctica | Por qué (justificación) |
|---|-----------|---------------------------|--------------------------|
| D1 | **El contexto manda sobre la consistencia visual** | La misma acción se ve y se comporta distinto en tablet de piso vs. desktop. | Un botón "correcto" en desktop puede ser inusable con guantes; forzar consistencia píxel a píxel entre superficies penaliza al usuario más vulnerable (el operario). |
| D2 | **Menos toques, menos errores** | Los flujos de captura frecuente (producción, scrap, parada) se resuelven en ≤ 3 interacciones desde el inicio. | El operario repite la acción cientos de veces por turno; cada toque de más es tiempo perdido y una oportunidad de error multiplicada por la frecuencia. |
| D3 | **Feedback inmediato y sin ambigüedad** | Toda acción confirma visual y (opcionalmente) hápticamente en < 100 ms percibidos, incluso offline. | En un entorno ruidoso y a ritmo alto, la duda "¿se guardó?" lleva a doble carga o a abandono; la confianza en el sistema es condición para que dejen de usar papel. |
| D4 | **Offline es un estado de primera clase, no un error** | La UI funciona sin red: encola, muestra estado "pendiente de envío" y reconcilia. | Las plantas tienen cortes de red; si la app "se cae" sin conexión, el operario vuelve al papel y se pierde el dato. Store-and-forward es requisito de arquitectura (edge-first). |
| D5 | **Mostrar solo lo que este rol puede hacer aquí** | La navegación y las acciones se filtran por rol + alcance ([users-permissions.md](./users-permissions.md)). | Reducir la superficie cognitiva y de error; un operario no debe siquiera ver "Integraciones". Seguridad por diseño y simplicidad se refuerzan. |
| D6 | **El dato crudo se acompaña de su significado** | Los números vienen con contexto (meta, tendencia, umbral, color semántico). | Un "OEE 62 %" sin referencia no decide nada; el valor del producto es contextualizar el dato, no solo mostrarlo. Coherente con las fórmulas canónicas de [dashboards.md](./dashboards.md). |
| D7 | **Prevenir el error antes que corregirlo** | Validación en el momento, valores por defecto inteligentes, confirmaciones solo para lo irreversible. | Corregir un dato ya sincronizado con el ERP es caro; es más barato y menos frustrante impedir el error de entrada (coherente con reglas ABAC de edición). |
| D8 | **Densidad adecuada a la distancia y a la tarea** | Piso: pocos elementos grandes vistos a distancia. Escritorio: alta densidad para análisis. | La distancia ojo-pantalla y la naturaleza de la tarea (capturar vs. analizar) fijan cuánta información cabe sin abrumar. |
| D9 | **Accesible por defecto** | Contraste alto, targets amplios, texto escalable, no depender solo del color, es-AR claro. | La planta es un entorno hostil a la percepción (luz, guantes, cascos, protección visual); accesibilidad aquí es *usabilidad para todos*, no un extra de cumplimiento. |
| D10 | **Trazabilidad visible** | El usuario ve el estado del dato: borrador / confirmado / sincronizado / con error. | La confianza en un MES depende de saber qué pasó con lo que cargó; la opacidad genera cargas duplicadas y desconfianza. |
| D11 | **Recuperación siempre posible** | Deshacer donde se pueda; caminos de salida claros; ninguna pantalla sin salida. | El operario no debe temer "romper algo"; el miedo frena la adopción. |
| D12 | **Rendimiento percibido como feature** | Cargas optimistas, esqueletos, precarga de catálogos frecuentes. | A escala de millones de eventos, la latencia es inevitable a veces; la percepción de velocidad se diseña (optimistic UI) para que el ritmo de piso no se corte. |

---

## 2. Sistema de diseño (fundamentos transversales)

Un único **Design System "Nexo DS"** garantiza coherencia entre superficies mientras permite las divergencias de D1. Vive conceptualmente aquí y se materializa en [mockups.md](./mockups.md).

### 2.1 Tokens y escalas
- **Color semántico** (no decorativo): estados de máquina y de dato tienen color con significado fijo — **verde** = operando/OK, **ámbar** = atención/advertencia, **rojo** = parada/error, **gris** = sin dato/apagado, **azul** = información/en proceso. **Por qué semántico y no de marca:** en piso el color comunica estado de un vistazo; si el rojo a veces es "botón de marca" y a veces "alarma", se pierde el canal más rápido de comunicación.
- **Nunca solo color:** todo estado se refuerza con **ícono + texto + forma** (D9). **Por qué:** ~8 % de los hombres tiene daltonismo; en planta hay muchos operarios varones; el color solo es discriminatorio y peligroso.
- **Tipografía:** familia legible de alto rendimiento a distancia; **dos escalas base** — "piso" (mínimo 18–20 px de cuerpo, títulos y números grandes) y "escritorio" (densidad estándar). **Por qué dos escalas:** la distancia de lectura en una tablet montada a 60–80 cm es mayor que en una laptop a 40 cm.
- **Espaciado y objetivos táctiles:** grilla de 8 px; **target mínimo 48×48 px en desktop y ≥ 64×64 px en tablet de piso**, con separación generosa. **Por qué 64 px:** un dedo con guante industrial cubre bastante más superficie que un dedo desnudo; targets chicos generan toques errados y frustración (base de D2/D9).
- **Elevación y foco:** foco de teclado siempre visible; estados hover/press/active claros. **Por qué:** parte del uso es con teclado (desktop) y con toque impreciso (guante); el foco visible es navegabilidad y accesibilidad.

### 2.2 Componentes base compartidos
Botón, campo, selector de catálogo, tarjeta de KPI, tabla/lista, badge de estado, stepper/wizard, hoja modal (sheet), banner de estado de conexión, toast de confirmación, teclado numérico grande, selector de motivo (grid de causas), cronómetro de parada, indicador de sincronización. Cada uno tiene variantes **piso** y **escritorio** (D1).

### 2.3 Estados universales de pantalla
Toda vista define explícitamente: **cargando** (esqueleto), **vacío** (con acción sugerida), **error** (con causa y reintento), **offline** (banner + cola), **sin permiso** (mensaje claro, no una pantalla en blanco). **Por qué obligatorio:** los estados no felices son el 30–40 % de la experiencia real en planta; diseñarlos evita callejones sin salida (D11) y desconfianza (D10).

---

## 3. Navegación y menú

### 3.1 Modelo de navegación por superficie (y su porqué)
- **Tablet de piso (kiosco):** navegación **plana y orientada a tarea**, no a módulos. La pantalla de inicio es un **panel de acciones grandes** ("Producir", "Scrap", "Parada", "Calidad"), no un árbol de menús. **Por qué:** el operario piensa en *lo que va a hacer*, no en la arquitectura de módulos; enterrar "registrar scrap" bajo tres niveles de menú es inaceptable a ritmo de piso (D2).
- **Desktop:** navegación **por módulos** con barra lateral jerárquica persistente + breadcrumbs. **Por qué:** supervisión, análisis y configuración requieren moverse entre dominios y saber "dónde estoy"; el árbol da modelo mental y acceso directo.
- **Mobile:** navegación **por prioridad**, con **barra inferior** de 3–5 destinos (Alertas, KPIs, Buscar, Perfil). **Por qué:** el pulgar alcanza la parte inferior; el uso es de consulta rápida, no de exploración profunda.

### 3.2 Árbol de navegación (desktop, superconjunto filtrado por rol)

> El árbol siguiente es el **superconjunto**. Cada rol ve solo las ramas que su permiso+alcance habilitan (D5, [users-permissions.md](./users-permissions.md)). Lo que un rol no puede, **no aparece**.

```
Nexo
├── Inicio / Dashboard
│   ├── Vista de planta (tiempo real)
│   ├── Vista ejecutiva (multi-planta)      [Gerencia]
│   └── Mis pendientes / Mi turno
├── Producción
│   ├── Registros de producción
│   ├── Órdenes de producción (MO)          [Producción/Supervisor]
│   ├── Turnos
│   └── Productividad / OEE
├── Calidad
│   ├── Inspecciones
│   ├── Checklists y planes                  [Calidad]
│   ├── Defectos
│   ├── Disposiciones                        [Calidad]
│   └── SPC / FPY
├── Scrap
│   ├── Registros de scrap
│   ├── Motivos (catálogo)                   [config]
│   └── Costos de scrap
├── Paradas
│   ├── Eventos de parada
│   ├── Motivos (catálogo)                   [config]
│   └── MTBF / MTTR
├── Trazabilidad
│   ├── Genealogía lote/serie
│   └── Historial de eventos (inmutable)
├── Dispositivos
│   ├── Inventario de dispositivos
│   ├── Sensores / señales (tags)
│   ├── Salud y diagnóstico
│   └── Firmware / OTA                       [Mantenimiento]
├── Integraciones
│   ├── Conectores (Odoo, …)                 [Integraciones/Admin]
│   ├── Mapeos / ACL
│   ├── Jobs de sincronización
│   └── Registro de errores de sync
├── Reglas
│   ├── Reglas (trigger-condición-acción)
│   └── Historial de disparos
├── Alertas y notificaciones
│   ├── Bandeja de alertas
│   ├── Suscripciones
│   └── Plantillas / canales                 [Admin]
├── Reportes
│   ├── Reportes on-demand
│   └── Reportes programados
└── Configuración
    ├── Estructura (Plantas, Sectores, Líneas, Máquinas)
    ├── Catálogos (productos, motivos, tolerancias, unidades)
    ├── Usuarios y permisos                   [Administrador]
    ├── Turnos y calendario
    ├── Auditoría                             [lectura]
    └── Preferencias del tenant
```

### 3.3 Elementos persistentes
- **Selector de alcance (planta/línea):** visible siempre para roles multi-alcance; fija el contexto de todo lo que se ve. **Por qué:** ambigüedad de alcance = decisiones sobre la planta equivocada; hacerlo explícito y persistente previene errores caros (D7).
- **Indicador de conexión/sincronización global:** banner discreto que informa online/offline y cuántos registros esperan envío. **Por qué:** materializa D4/D10; el usuario nunca queda a ciegas sobre el estado del dato.
- **Buscar** (desktop): buscador global de órdenes, lotes, dispositivos, registros. **Por qué:** a partir de cierto volumen, navegar por árbol no escala; buscar es más rápido.

---

## 4. Estructura de pantallas y vistas (patrones)

Nexo reutiliza **patrones de pantalla** para consistencia y velocidad de diseño/construcción. Cada patrón se elige por la tarea que resuelve.

| Patrón | Para qué | Superficie típica | Por qué este patrón |
|--------|----------|-------------------|----------------------|
| **Panel de acciones (kiosco)** | Elegir qué capturar | Tablet piso | Convierte módulos en verbos grandes; minimiza toques (D2). |
| **Captura guiada (wizard/stepper)** | Registrar producción, scrap, parada, inspección | Tablet piso | Divide una tarea en pasos claros con validación por paso; reduce carga cognitiva y error (D7). |
| **Lista + detalle (master-detail)** | Revisar/editar registros, órdenes, dispositivos | Desktop | Permite escanear muchos ítems y profundizar sin perder contexto (D8). |
| **Dashboard (tiempo real)** | Monitorear estado y KPIs | Desktop / tablet supervisor / mobile | Densidad informativa con color semántico (D6); ver [dashboards.md](./dashboards.md). |
| **Formulario de configuración** | Definir estructura, catálogos, reglas, mapeos | Desktop | Tareas infrecuentes, complejas y críticas: privilegia claridad y validación sobre velocidad (D7). |
| **Bandeja (inbox)** | Alertas, pendientes, errores de sync | Todas | Modelo mental conocido (bandeja) para "cosas que requieren mi atención". |
| **Detalle inmutable (timeline)** | Trazabilidad, auditoría, historial de evento | Desktop | Comunica que el dato no se edita, solo se lee/anexa (D10, P6 de permisos). |

**Anatomía común de pantalla (desktop):** barra superior (identidad de tenant, alcance, conexión, usuario) → barra lateral (navegación filtrada por rol) → área de trabajo (patrón) → panel contextual/acciones. **Por qué esta anatomía:** separa *dónde estoy* (lateral), *en qué contexto* (superior) y *qué hago* (centro), un modelo probado que reduce desorientación.

---

## 5. Flujos de trabajo por persona

Cada flujo describe **objetivo, superficie, pasos y el porqué de las decisiones**. Los roles y sus permisos son los de [users-permissions.md](./users-permissions.md).

### 5.1 Operario — registrar producción (flujo estrella)
- **Objetivo:** dejar registrada la cantidad producida con el mínimo esfuerzo y sin papel.
- **Superficie:** tablet en kiosco a pie de línea.
- **Flujo:** iniciar turno (una vez) → tocar "Producir" en panel → confirmar línea/orden ya precargada por contexto → ingresar cantidad con teclado numérico grande → confirmar → toast + háptico "Registrado". Si no hay red, se encola con badge "pendiente".
- **Porqués:** la orden/línea vienen **precargadas** por el contexto del kiosco (qué línea, qué turno, qué orden activa) para eliminar selección manual (D2); el teclado numérico es grande y dedicado porque escribir cantidades es la acción más repetida; el feedback háptico compensa el ruido ambiente (D3); la cola offline evita el retorno al papel (D4).

### 5.2 Operario — registrar scrap
- **Objetivo:** registrar descarte con su motivo, para costeo y mejora.
- **Flujo:** "Scrap" → seleccionar **motivo** en una **grilla visual de causas** (íconos + texto) → cantidad → foto opcional de evidencia → confirmar.
- **Porqués:** el motivo se elige de una grilla y no de un desplegable largo porque tocar un ícono grande es más rápido y menos propenso a error que buscar en una lista (D2/D8); la foto es opcional y de un toque porque la evidencia mejora la trazabilidad sin obligar (equilibrio fricción/valor); el motivo es obligatorio porque scrap sin causa no sirve para mejorar (D6).

### 5.3 Operario — registrar parada
- **Objetivo:** documentar por qué se detuvo la máquina, con tiempo preciso.
- **Flujo:** "Parada" → se inicia **cronómetro** automático → seleccionar motivo (grilla) → al reanudar, "Cerrar parada" → confirma duración.
- **Porqués:** el cronómetro arranca solo para que el tiempo sea real y no estimado a mano (precisión de MTBF/MTTR, D6); cerrar es un solo toque grande porque suele hacerse con urgencia al reanudar.

### 5.4 Operario — ejecutar inspección de calidad asignada
- **Objetivo:** completar el checklist de calidad que le tocó.
- **Flujo:** ver "Mis pendientes" → abrir checklist → responder ítem por ítem (OK/NO OK/valor) con controles grandes → adjuntar foto si aplica → enviar.
- **Porqués:** un ítem por pantalla (o pocos, grandes) evita errores de "marcar el renglón equivocado" a ritmo (D2/D8); la disposición final (aceptar/rechazar) **no** aparece porque el operario no dispone (P4 de permisos, D5).

### 5.5 Supervisor — cerrar turno y validar dato
- **Objetivo:** revisar y confirmar el dato del turno antes de que se consolide/sincronice.
- **Superficie:** tablet (piso) o desktop (fin de turno).
- **Flujo:** ver tablero de turno → detectar registros en borrador o anómalos → corregir/confirmar → cerrar turno.
- **Porqués:** el supervisor **confirma** (acción exclusiva, P4) porque separar captura de confirmación mejora la calidad del dato; las anomalías se resaltan con color semántico para que no haya que buscarlas (D6).

### 5.6 Calidad — definir plan y decidir disposición
- **Objetivo:** gobernar el control de calidad y decidir el destino de un lote no conforme.
- **Superficie:** desktop (planes, SPC) + tablet (inspección en piso).
- **Flujo:** crear/editar plan y checklist → monitorear resultados/SPC → ante no conformidad, abrir el caso → decidir **disposición** (aceptar/rechazar/reprocesar) con justificación.
- **Porqués:** la disposición exige justificación registrada porque es una decisión con impacto de costo y trazabilidad (D10, P6); SPC se presenta como tendencia, no solo dato puntual, para decidir sobre proceso, no sobre anécdota (D6).

### 5.7 Producción — planificar y analizar OEE
- **Objetivo:** gestionar órdenes y entender productividad.
- **Superficie:** desktop.
- **Flujo:** revisar órdenes (sincronizadas con ERP) → asignar a líneas/turnos → seguir OEE y sus factores (Disponibilidad × Rendimiento × Calidad) → ajustar.
- **Porqués:** OEE se descompone siempre en sus tres factores para que la acción sea diagnóstica ("¿pierdo por paradas, por ritmo o por calidad?"), coherente con las fórmulas canónicas ([dashboards.md](./dashboards.md), D6).

### 5.8 Mantenimiento — gestionar paradas y salud de dispositivos
- **Objetivo:** reducir paradas y mantener sanos los dispositivos.
- **Superficie:** tablet (intervención) + desktop (análisis).
- **Flujo:** recibir alerta de parada/salud → diagnosticar → registrar causa técnica y reparación → seguir MTBF/MTTR → planificar OTA/firmware cuando corresponda.
- **Porqués:** la ejecución de OTA sobre activo crítico pide doble confirmación (ABAC de criticidad, D7) porque un firmware fallido detiene la línea; la salud se muestra con color semántico + tendencia para anticipar la falla, no solo constatarla.

### 5.9 Gerencia — decidir con datos
- **Objetivo:** comparar plantas y detectar dónde intervenir.
- **Superficie:** desktop (dashboards ejecutivos) + mobile (KPIs/alertas en movimiento).
- **Flujo:** abrir vista ejecutiva multi-planta → comparar OEE/scrap/disponibilidad → drill-down → programar reporte.
- **Porqués:** solo lectura por diseño (P4/D5); comparabilidad entre plantas con las mismas fórmulas para que las cifras sean discutibles y no "cada planta mide distinto".

### 5.10 Administrador — configurar el tenant
- **Objetivo:** dejar la instancia lista y gobernada.
- **Superficie:** desktop.
- **Flujo:** definir estructura (plantas/líneas/turnos) → catálogos → usuarios/roles/alcances → integraciones y reglas → auditar.
- **Porqués:** las tareas de configuración usan formularios con validación fuerte y confirmaciones para lo irreversible (D7); el impacto de un error de configuración es amplio, así que se privilegia seguridad sobre velocidad.

### 5.11 Integraciones — conectar con el ERP
- **Objetivo:** que producción/scrap/calidad fluyan hacia Odoo/ERP sin carga manual.
- **Superficie:** desktop.
- **Flujo:** elegir conector → configurar credenciales (referenciadas) y mapeos/ACL → probar → activar → monitorear jobs y reintentar errores.
- **Porqués:** el mapeo se presenta como correspondencia visual campo-a-campo con validación y **prueba antes de activar** (D7) porque un mapeo errado corrompe datos en el ERP; los errores de sync se muestran en una bandeja accionable con reintento (D10/D11).

---

## 6. Experiencia TABLET industrial (el corazón del producto)

La tablet de piso es donde se gana o se pierde la promesa de "eliminar la carga manual". Cada decisión aquí prioriza al operario en su entorno real.

### 6.1 Modo kiosco
- **Qué:** la tablet arranca directo en Nexo, a pantalla completa, sin acceso al SO ni a otras apps, anclada a una **línea/estación**.
- **Por qué:** evita distracciones y usos indebidos, asegura que el dispositivo esté siempre "listo para capturar", y permite precargar el contexto (línea, turno, orden) para minimizar selección (D2). También protege el dispositivo compartido de configuraciones accidentales.

### 6.2 Touch con guantes y objetivos grandes
- **Qué:** targets ≥ 64 px, separación amplia, gestos simples (tocar, no gestos finos como pellizcar), teclados numéricos grandes dedicados.
- **Por qué:** el guante industrial reduce precisión táctil y capacitancia; los gestos finos son inviables; la solución es *grande y separado*. Además, a ritmo de producción no hay tiempo para apuntar con precisión (D2/D9).

### 6.3 Alto contraste y legibilidad a distancia
- **Qué:** tema de alto contraste, tipografía grande, color semántico reforzado con ícono/texto, brillo adaptable.
- **Por qué:** las plantas tienen iluminación extrema (naves oscuras o sol directo, polvo, vapor); una interfaz de bajo contraste es ilegible. La tablet suele estar montada y se lee a 60–80 cm, no en la mano (D8/D9).

### 6.4 Operación offline / store-and-forward
- **Qué:** la app captura, valida localmente, **encola** y muestra "pendiente de envío"; al recuperar red, sincroniza en orden y reconcilia con **dedup_key** para no duplicar. Banner de conexión siempre visible.
- **Por qué:** la conectividad de planta es intermitente; el requisito arquitectónico es edge-first con store-and-forward. Si la app dependiera de red, el operario volvería al papel en el primer corte y se perdería el dato (D4/D10). La deduplicación evita el doble registro cuando el operario reintenta ante la duda.

### 6.5 Feedback multicanal
- **Qué:** confirmación **visual + háptica + sonora opcional** de cada acción; estados de dato claros (borrador/pendiente/enviado).
- **Por qué:** el ruido de planta anula el feedback sonoro solo; el háptico atraviesa el ruido y confirma "ya está" sin mirar fijo la pantalla (D3).

### 6.6 Sesión y multi-operario en dispositivo compartido
- **Qué:** login rápido por PIN/badge/NFC, cambio de operario ágil, cierre de sesión por inactividad, atribución de cada registro a su operario.
- **Por qué:** una tablet la comparten varios operarios por turno; el login debe ser de segundos (no romper el ritmo), pero cada dato debe quedar atribuido para trazabilidad (D10, coherente con MFA de kiosco en [users-permissions.md](./users-permissions.md)).

### 6.7 Tolerancia al error físico
- **Qué:** confirmaciones para lo irreversible, deshacer inmediato en lo posible, bloqueo de acciones peligrosas tras validación.
- **Por qué:** el toque accidental es frecuente con guante y movimiento; el diseño asume el error físico y lo amortigua (D7/D11).

---

## 7. Experiencia DESKTOP (supervisión, gerencia, configuración)

El escritorio es para **entender, decidir y configurar**, no para capturar a alta frecuencia.

- **Densidad informativa alta:** tablas, dashboards multi-KPI, comparativas. **Por qué:** el usuario está sentado, concentrado, a corta distancia; puede procesar más información por pantalla (D8).
- **Navegación por módulos con jerarquía y breadcrumbs:** **por qué** el trabajo de supervisión/config cruza dominios; el usuario necesita mapa mental y ubicación.
- **Multi-ventana / paneles:** ver dashboard y detalle a la vez. **Por qué** el análisis es comparativo por naturaleza.
- **Atajos de teclado y acciones masivas:** selección múltiple, filtros, exportación. **Por qué** la eficiencia del poweruser (supervisor/analista) se mide en operaciones por minuto sobre muchos ítems.
- **Configuración segura:** formularios con validación fuerte, vista previa, confirmación de cambios de alto impacto, historial de cambios. **Por qué** el error de configuración es amplio y caro; se privilegia prevención (D7).
- **Dashboards ejecutivos (Gerencia):** multi-planta, drill-down, exportables. **Por qué** la decisión estratégica compara y profundiza; ver [dashboards.md](./dashboards.md).

---

## 8. Experiencia MOBILE (alertas y consulta)

El teléfono es para **estar al tanto en movimiento**, no para operar la planta.

- **Foco en alertas y KPIs:** bandeja de alertas con acción rápida (reconocer/escalar) y tarjetas de KPI. **Por qué:** el supervisor o gerente en movimiento necesita enterarse y reaccionar, no configurar; una app móvil que intente replicar el desktop fracasa por espacio y contexto (D8).
- **Notificaciones push accionables:** desde la notificación se puede reconocer o abrir el detalle. **Por qué:** reducir el tiempo entre "algo pasó" y "alguien reacciona" es el valor central en movilidad; coherente con [notifications.md](./notifications.md).
- **Navegación por pulgar (barra inferior):** destinos clave alcanzables con una mano. **Por qué** el uso móvil es frecuentemente unimanual y de pie.
- **Consulta, no captura masiva:** se permite una carga puntual de emergencia, pero no es el caso de uso principal. **Por qué** capturar a alta frecuencia en un teléfono es lento y propenso a error; ese trabajo es de la tablet (D1).
- **Modo lectura offline ligero:** últimos KPIs y alertas en caché. **Por qué** la señal celular en planta también falla (D4).

### 8.1 Cuadro comparativo de superficies (síntesis)

| Dimensión | Tablet piso | Desktop | Mobile |
|-----------|-------------|---------|--------|
| Usuario principal | Operario, Supervisor | Supervisor, Producción, Calidad, Mantenimiento, Gerencia, Admin, Integraciones | Supervisor, Gerencia, Mantenimiento |
| Tarea dominante | Capturar | Analizar / configurar | Enterarse / reaccionar |
| Navegación | Plana, por acción | Por módulos, jerárquica | Por prioridad, barra inferior |
| Densidad | Baja, grande | Alta | Media, tarjetas |
| Interacción | Toque grande, guante | Mouse + teclado | Pulgar |
| Offline | Crítico (store-and-forward) | Deseable | Caché de consulta |
| Justificación | Ritmo, ruido, guantes, cortes | Concentración, comparación | Movilidad, inmediatez |

---

## 9. Accesibilidad

Accesibilidad en Nexo es **usabilidad para el peor caso**, que en planta es el caso común (guantes, casco, protección visual, ruido, prisa). Se toma **WCAG 2.1 AA** como piso, con extensiones industriales.

- **Contraste ≥ 4.5:1 (texto) y ≥ 3:1 (componentes/estados);** en piso se apunta a AAA donde se pueda. **Por qué:** la iluminación de planta degrada cualquier contraste bajo hasta la ilegibilidad.
- **No depender solo del color (D9):** estado = color + ícono + texto. **Por qué:** daltonismo frecuente y condiciones de luz que alteran la percepción del color.
- **Objetivos táctiles amplios:** ya definidos (≥ 64 px piso). **Por qué:** motricidad reducida por guantes/EPP.
- **Texto escalable y jerarquía clara:** el usuario puede aumentar el tamaño sin romper el layout. **Por qué:** edad y condiciones visuales variadas en la dotación.
- **Navegación por teclado completa (desktop) y foco visible:** **por qué** inclusión de usuarios que no usan mouse y eficiencia de poweruser.
- **Compatibilidad con lectores de pantalla en desktop/mobile:** etiquetas semánticas. **Por qué:** cumplimiento e inclusión en las superficies donde aplica.
- **Lenguaje claro es-AR:** términos del [glossary.md](./glossary.md), sin jerga innecesaria, frases cortas. **Por qué:** dotación heterogénea; la claridad reduce error y capacitación.
- **Feedback no solo sonoro:** háptico + visual. **Por qué:** ruido de planta y usuarios con hipoacusia.
- **Tolerancia temporal:** sin timeouts agresivos en captura; la sesión de kiosco cierra por inactividad, pero no interrumpe una carga en curso. **Por qué:** el ritmo de piso es variable; penalizar la lentitud puntual genera pérdida de dato.

---

## 10. Rendimiento percibido, estados y microinteracciones

- **Optimistic UI en captura:** la acción se confirma de inmediato y sincroniza detrás. **Por qué:** a ritmo de piso, esperar la confirmación del servidor rompe el flujo (D12); la reconciliación posterior maneja el caso raro de rechazo.
- **Esqueletos y precarga:** catálogos frecuentes (motivos, productos) precargados en el dispositivo. **Por qué:** evita esperas en la acción más repetida y habilita offline (D4/D12).
- **Microcopys que explican el estado:** "Guardado localmente, se enviará al recuperar la conexión". **Por qué:** transparencia genera confianza (D10) y evita cargas duplicadas.
- **Animaciones funcionales, no decorativas:** transiciones que orientan (de dónde viene, a dónde va), breves. **Por qué:** en industrial, la animación gratuita cansa y ralentiza; la funcional reduce desorientación.

---

## 11. Internacionalización, marca y personalización por tenant

- **es-AR primero, arquitectura i18n desde el día uno:** textos externalizados. **Por qué:** el mercado inicial es es-AR pero la escala objetivo (miles de empresas) implica otros idiomas; retrofitear i18n es caro.
- **Personalización ligera por tenant:** logo y color de marca en zonas no semánticas (encabezado, login). **Por qué:** el cliente enterprise espera ver su identidad, pero **el color semántico de estado no se toca** para no romper el canal de comunicación crítico de piso (§2.1).
- **Unidades y formatos por tenant/planta:** unidades de medida, formato numérico/fecha. **Por qué:** consistencia con el dominio de cada cliente y con el ERP.

---

## 12. Preguntas abiertas

1. **Hardware de tablet objetivo:** ¿qué gama de tablets industriales soportamos (tamaño, sistema, resistencia IP)? Define límites reales de layout, contraste y modo kiosco.
2. **Login de piso definitivo:** ¿PIN, badge/NFC, biometría? Impacta directamente el flujo estrella y se coordina con MFA de [users-permissions.md](./users-permissions.md).
3. **Grado de personalización por tenant:** ¿hasta dónde llega el white-label sin comprometer la semántica de estado ni la mantenibilidad del Design System?
4. **Densidad configurable en desktop:** ¿ofrecemos modos "cómodo/compacto" para powerusers o fijamos una densidad? Trade-off entre flexibilidad y consistencia.
5. **Alcance del uso offline:** ¿qué módulos, además de captura, deben operar offline (consulta de órdenes, checklists)? ¿Cuánto historial se cachea en el dispositivo?
6. **Estándar de accesibilidad comprometido:** ¿fijamos WCAG 2.1 AA contractualmente para todas las superficies, con extensiones industriales, o AAA en piso? Impacta esfuerzo y QA.
7. **Multi-operario simultáneo en una estación:** ¿una tablet = un operario por vez, o soporta captura de varios operarios en una misma estación/turno? Impacta modelo de sesión y atribución.
8. **Notificaciones móviles y ruido de alertas:** ¿cómo evitamos la fatiga de alertas (alert fatigue) en mobile sin ocultar lo crítico? Coordinar con [rules-engine.md](./rules-engine.md) y [notifications.md](./notifications.md).
