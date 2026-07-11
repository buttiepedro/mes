# Mockups (Wireframes descriptivos)

> **Documento:** `specs/specs/mockups.md` · **Estado:** Borrador v0.1 · **Actualizado:** 2026-07-11
> **Roles:** Product Manager · Software Architect · UX Designer
> **Relacionados:** [ui-ux.md](./ui-ux.md) · [dashboards.md](./dashboards.md) · [users-permissions.md](./users-permissions.md) · [production.md](./production.md) · [quality.md](./quality.md) · [scrap.md](./scrap.md) · [downtime.md](./downtime.md) · [devices.md](./devices.md) · [integrations.md](./integrations.md) · [rules-engine.md](./rules-engine.md) · [notifications.md](./notifications.md)

## Resumen ejecutivo

Este documento contiene los **wireframes descriptivos** (no imágenes) de las pantallas núcleo de **Nexo**, listos para trasladar a Figma sin ambigüedad. Cada pantalla se especifica con: **objetivo**, **usuario y superficie**, **layout con pseudo-wireframe ASCII**, **componentes**, **estados** (cargando/vacío/error/offline/sin permiso), **interacciones** y **comportamiento responsive** (tablet industrial / desktop / mobile). Son la materialización concreta de los principios y flujos definidos en [ui-ux.md](./ui-ux.md) y respetan el filtrado por rol y alcance de [users-permissions.md](./users-permissions.md).

El criterio de corte entre superficies es el de [ui-ux.md](./ui-ux.md): **tablet de piso** para capturar (touch grande, guantes, alto contraste, offline), **desktop** para supervisar/analizar/configurar (densidad, jerarquía), **mobile** para enterarse y reaccionar (alertas, KPIs). Por eso cada pantalla no describe "una" interfaz sino su **adaptación por superficie**, señalando qué se prioriza en cada una y por qué.

Los wireframes usan una notación ASCII consistente (§0). No definen estilo visual final (color, tipografía, sombras) —eso vive en el Design System de Figma— sino **estructura, jerarquía, contenido y comportamiento**. Cada pantalla enlaza a su módulo funcional para que el equipo cruce el "cómo se ve" con el "qué hace".

---

## 0. Convenciones de wireframe

```
┌─┐ └─┘ │ ─   Contenedor / marco de región
[ Botón ]      Botón de acción
( ◉ ) ( ○ )    Radio seleccionado / no seleccionado
[x] [ ]        Checkbox marcado / vacío
[____]         Campo de texto / entrada
▼              Desplegable (select)
◀ ▶ ▲ ▼        Controles de navegación / paginación
●              Indicador de estado (color semántico: verde/ámbar/rojo/gris)
KPI            Tarjeta de indicador
▓▓▓░░          Barra de progreso / gauge
« Tab »        Pestaña activa
≡              Menú / lista
🔍 🔔 👤 ⚙     Íconos (buscar / alertas / usuario / config) — referenciales
{estado}       Nota de estado o variante
```

> Regla de color: en los wireframes el color se anota como `●verde/●ámbar/●rojo/●gris/●azul` con su significado semántico fijo (ver [ui-ux.md](./ui-ux.md) §2.1). En Figma siempre acompañado de ícono + texto (nunca solo color).

---

## 1. Login

- **Objetivo:** autenticar al usuario, resolver su **tenant** y llevarlo a su experiencia según rol/superficie.
- **Usuario:** todos. **Superficie:** las tres, con variantes fuertes (piso usa login rápido).
- **Enlaza a:** [users-permissions.md](./users-permissions.md) (SSO/MFA), [ui-ux.md](./ui-ux.md).

### 1.1 Layout — Desktop (login corporativo)

```
┌───────────────────────────── Nexo ─────────────────────────────┐
│                                                                 │
│                     [ logo del tenant ]                         │
│                                                                 │
│              Ingresá a  empresa.nexo.app                        │
│                                                                 │
│      Usuario / Email  [______________________________]          │
│      Contraseña       [______________________________]          │
│                       [x] Recordar este dispositivo             │
│                                                                 │
│                     [   Ingresar   ]                            │
│                                                                 │
│      ──────────────  o  ──────────────                          │
│      [  Ingresar con SSO corporativo (OIDC/SAML)  ]             │
│                                                                 │
│      ¿Olvidaste tu contraseña?                                  │
│      ● Conectado a: empresa.nexo.app   (tenant resuelto)        │
└─────────────────────────────────────────────────────────────────┘
        → paso siguiente si aplica: [ Verificación MFA ]
```

### 1.2 Layout — MFA (step / step-up)

```
┌──────────────── Verificación en dos pasos ─────────────────┐
│  Ingresá el código de tu app de autenticación              │
│           [ _ ][ _ ][ _ ] [ _ ][ _ ][ _ ]                  │
│           [ Verificar ]   ¿Usar código de respaldo?        │
│  {step-up}: se pide de nuevo antes de acciones críticas    │
└─────────────────────────────────────────────────────────────┘
```

### 1.3 Layout — Tablet de piso (login rápido kiosco)

```
┌──────────── Estación: LÍNEA 3 · Turno Mañana ──────────────┐
│                                                            │
│      Acercá tu credencial   [   📶 NFC / Badge   ]         │
│                    — o —                                   │
│      Ingresá tu PIN                                        │
│               ┌───┬───┬───┐                                │
│               │ 1 │ 2 │ 3 │   PIN: ● ● ● _                 │
│               ├───┼───┼───┤                                │
│               │ 4 │ 5 │ 6 │                                │
│               ├───┼───┼───┤                                │
│               │ 7 │ 8 │ 9 │                                │
│               ├───┼───┼───┤                                │
│               │ ← │ 0 │ ✓ │                                │
│               └───┴───┴───┘                                │
│  ● En línea    Últ. sincronización: hace 2 min             │
└─────────────────────────────────────────────────────────────┘
```

- **Componentes:** logo tenant, campos usuario/clave, botón SSO, teclado PIN grande, lector NFC/badge, banner de tenant resuelto, banner de conexión, bloque MFA.
- **Estados:** cargando (spinner en botón), error de credencial (mensaje inline, sin revelar cuál campo falló), cuenta bloqueada (mensaje + contacto admin), offline en piso (permite login con credencial cacheada del dispositivo confiable + aviso), tenant no resuelto (pide seleccionar/host inválido).
- **Interacciones:** Enter envía; SSO redirige al IdP; en piso, PIN de N dígitos con confirmación (✓) y borrado (←); tras 3 fallos, backoff.
- **Responsive:**
  - **Tablet piso:** teclado numérico gigante, NFC prioritario, sin campos largos; **por qué:** velocidad y guantes ([ui-ux.md](./ui-ux.md) §6).
  - **Desktop:** formulario completo + SSO; foco en teclado.
  - **Mobile:** formulario vertical, SSO destacado, soporte de biometría del teléfono como conveniencia (no reemplaza MFA de roles sensibles).

---

## 2. Dashboard principal

- **Objetivo:** dar estado en tiempo real de la operación y los KPIs clave según rol y alcance.
- **Usuario:** todos (contenido filtrado). **Superficie:** desktop (completo), tablet supervisor (resumen), mobile (KPIs/alertas).
- **Enlaza a:** [dashboards.md](./dashboards.md) (KPIs y fórmulas), [ui-ux.md](./ui-ux.md).
- **Prioridad MVP (caso estrella PRD-02):** junto con Producción (§3), esta pantalla es el corazón de la demo del MVP: **producción manual → dashboard en tiempo real → Odoo** ([tablero de decisiones](../open-questions-board.md)).

### 2.1 Layout — Desktop (vista de planta, tiempo real)

```
┌ Nexo ── ●En línea ── Alcance: [Planta Norte ▼][Línea 3 ▼] ─ 🔔3 ─ 👤 ┐
├───────────┬───────────────────────────────────────────────────────────┤
│ ≡ Inicio  │  Planta Norte · Turno Mañana · 08:00–16:00                  │
│  Producc. │  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐                │
│  Calidad  │  │ OEE    │ │ Dispon.│ │ Rendim.│ │ Calidad│                │
│  Scrap    │  │ ●ámbar │ │ ●verde │ │ ●ámbar │ │ ●verde │                │
│  Paradas  │  │  62%   │ │  91%   │ │  74%   │ │  92%   │                │
│  Trazab.  │  │ ▓▓▓░░  │ │ ▓▓▓▓░  │ │ ▓▓▓░░  │ │ ▓▓▓▓░  │                │
│  Disposit.│  └────────┘ └────────┘ └────────┘ └────────┘                │
│  Integ.   │  Producción vs meta         Scrap del turno                 │
│  Reglas   │  ┌──────────────────┐       ┌──────────────────┐            │
│  Alertas  │  │  ▂▃▅▆▇▆▅ (línea)  │       │ ●rojo 3,2% (meta 2%)│         │
│  Reportes │  │  1.240 / 1.800 u  │       │ Top motivo: Rebaba │         │
│  Config   │  └──────────────────┘       └──────────────────┘            │
│           │  Estado de líneas            Alertas activas                │
│           │  L1 ●verde  L2 ●rojo(parada) │ ●rojo Parada L2 12min         │
│           │  L3 ●verde  L4 ●ámbar        │ ●ámbar Scrap L3 > meta        │
│           │                              │ [ Ver todas ]                 │
└───────────┴───────────────────────────────────────────────────────────┘
```

### 2.2 Layout — Desktop (vista ejecutiva Gerencia, multi-planta)

```
┌ Vista ejecutiva ── Alcance: Todas las plantas ── Periodo: [Hoy ▼] ──────┐
│  Planta        OEE     Dispon.  Rendim.  Calidad  Scrap%   Estado       │
│  Norte        ●ámbar62  91      74       92        3.2     ● operando    │
│  Sur          ●verde78  95      86       95        1.1     ● operando    │
│  Oeste        ●rojo41   60      70       97        4.8     ● 2 paradas   │
│  [ Drill-down ▶ ]                     [ Programar reporte ] [Exportar]   │
└─────────────────────────────────────────────────────────────────────────┘
```

### 2.3 Layout — Mobile (KPIs + alertas)

```
┌ Nexo ─ Planta Norte ▼ ─ 🔔3 ┐
│ ┌─────────┐ ┌─────────┐      │
│ │OEE ●ámbar│ │Scrap ●rojo│    │
│ │  62%    │ │  3,2%    │      │
│ └─────────┘ └─────────┘      │
│ Alertas                       │
│ ●rojo Parada L2 · 12 min      │
│    [Reconocer] [Ver]          │
│ ●ámbar Scrap L3 > meta        │
│    [Reconocer] [Ver]          │
├──────────────────────────────┤
│ 🔔Alertas  📊KPIs  🔍  👤     │
└──────────────────────────────┘
```

- **Componentes:** selector de alcance, tarjetas KPI (con color semántico + valor + gauge + meta), mini-gráficos de tendencia, mapa de estado de líneas, panel de alertas activas, controles de periodo, botón exportar/programar (Gerencia).
- **Estados:** cargando (esqueleto de tarjetas), sin dato aún en el turno (mensaje "aún no hay registros del turno"), offline (banner + "datos al corte de hace X min"), error de read model (reintento), sin permiso (oculta ramas no autorizadas — no muestra tarjetas vacías).
- **Interacciones:** cambiar alcance recalcula todo; clic en KPI abre drill-down; clic en línea abre su detalle; clic en alerta abre pantalla Alertas (§11).
- **Responsive:**
  - **Desktop:** grilla completa multi-KPI + panel lateral de alertas; **por qué:** densidad para monitoreo ([ui-ux.md](./ui-ux.md) §7).
  - **Tablet supervisor:** tarjetas grandes, 2–4 KPIs visibles, estado de líneas de su alcance.
  - **Mobile:** 2 KPIs prioritarios + lista de alertas accionables; **por qué:** enterarse y reaccionar ([ui-ux.md](./ui-ux.md) §8).

---

## 3. Producción

- **Objetivo:** registrar producción (piso) y gestionar/analizar registros y órdenes (desktop).
- **Usuario:** Operario (captura), Supervisor/Producción (gestión). **Superficie:** tablet piso + desktop.
- **Enlaza a:** [production.md](./production.md).
- **Prioridad MVP (caso estrella PRD-02 / modo híbrido PRD-03):** la **captura manual desde tablet es de primera clase**, no un fallback; en el MVP la carga es **manual + datalogger/CSV** (los protocolos de captura automática llegan en V1). Esta pantalla abre el flujo estrella **producción manual → dashboard → Odoo**.

### 3.1 Layout — Tablet piso (captura guiada, flujo estrella)

```
┌ LÍNEA 3 · OP: J. Pérez · Turno Mañana ── ●En línea ── ⏱ 10:24 ──────┐
│                                                                      │
│   ¿Qué querés registrar?                                             │
│   ┌───────────────┐ ┌───────────────┐                               │
│   │   ▶ PRODUCIR  │ │    ✕ SCRAP    │                               │
│   │   (grande)    │ │   (grande)    │                               │
│   └───────────────┘ └───────────────┘                               │
│   ┌───────────────┐ ┌───────────────┐                               │
│   │  ⏸ PARADA     │ │  ✓ CALIDAD    │                               │
│   └───────────────┘ └───────────────┘                               │
│                                                                      │
│   Orden activa: MO-2048 · Producto: Perfil U 40mm                    │
└──────────────────────────────────────────────────────────────────────┘
        │  (toca PRODUCIR)
        ▼
┌ Registrar producción · MO-2048 · Perfil U 40mm ─────────────────────┐
│   Cantidad producida                                                 │
│        ┌─────────────────────────┐                                   │
│        │        1 2 5            │  ← visor grande                    │
│        └─────────────────────────┘                                   │
│        ┌───┬───┬───┐   Unidad: piezas ▼                              │
│        │ 1 │ 2 │ 3 │                                                 │
│        ├───┼───┼───┤   [ + Buenas ] [ - Rechazadas ]                │
│        │ 4 │ 5 │ 6 │                                                 │
│        ├───┼───┼───┤                                                 │
│        │ 7 │ 8 │ 9 │   ┌───────────────────────┐                     │
│        ├───┼───┼───┤   │   ✓  CONFIRMAR         │  (botón enorme)    │
│        │ ← │ 0 │ 00│   └───────────────────────┘                     │
│        └───┴───┴───┘                                                 │
│   {offline}: ● Se guardará y enviará al recuperar conexión           │
└──────────────────────────────────────────────────────────────────────┘
        │  (confirma)
        ▼   Toast: "✓ Registrado" + háptico
```

### 3.2 Layout — Desktop (lista + detalle de registros / órdenes)

```
┌ Producción ▸ Registros ── Alcance: Planta Norte / Línea 3 ───────────────┐
│ « Registros » | Órdenes | Turnos | Productividad/OEE     [+ Registro]     │
│ Filtros: [Turno ▼][Orden ▼][Estado ▼][Fecha ▼]        🔍 [_________]      │
│ ┌───────┬──────────┬────────┬───────┬─────────┬────────┬──────────────┐  │
│ │ Hora  │ Orden    │ Prod.  │ Cant. │ Operario│ Estado │ Sync ERP     │  │
│ ├───────┼──────────┼────────┼───────┼─────────┼────────┼──────────────┤  │
│ │ 10:24 │ MO-2048  │ Perfil │ 125   │ J.Pérez │●azul   │ ●gris pend.  │  │
│ │       │          │        │       │         │borrador│              │  │
│ │ 09:50 │ MO-2048  │ Perfil │ 200   │ J.Pérez │●verde  │ ●verde OK    │  │
│ │       │          │        │       │         │confirm.│              │  │
│ └───────┴──────────┴────────┴───────┴─────────┴────────┴──────────────┘  │
│  Seleccionado ▸ Detalle:  MO-2048 · 10:24 · 125 pz                       │
│  [ Confirmar ] [ Corregir ] [ Anular (con motivo) ]  Historial ▸ timeline│
└───────────────────────────────────────────────────────────────────────────┘
```

- **Componentes:** panel de acciones (piso), visor numérico + teclado grande, selector de unidad, toggle buenas/rechazadas, botón confirmar; (desktop) tabla filtrable, badges de estado de dato y de sync, panel de detalle, acciones confirmar/corregir/anular, timeline.
- **Estados:** cargando; sin orden activa (piso: bloquea con "no hay orden asignada a esta línea, avisá a tu supervisor"); offline (**banner de conexión siempre visible**; encola con badge **pendiente**, luego **sincronizando** y **enviado**, sin duplicar por `dedup_key`); error de validación (cantidad > lo esperado → pide confirmación, D7); sin permiso (Operario no ve confirmar/anular).
- **Interacciones:** confirmar es acción exclusiva del Supervisor (P4/[users-permissions.md](./users-permissions.md)); corregir sujeto a ventana ABAC; anular exige motivo; registros sincronizados quedan de solo lectura.
- **Responsive:** piso = wizard de captura minimalista; desktop = master-detail denso; mobile = solo consulta de registros (no captura masiva).

---

## 4. Calidad

- **Objetivo:** ejecutar inspecciones (piso), definir planes/checklists y decidir disposiciones (desktop).
- **Usuario:** Operario (ejecuta asignadas), Calidad (gobierna). **Superficie:** tablet + desktop.
- **Enlaza a:** [quality.md](./quality.md).
- **Carga manual completa desde tablet (PRD-03):** la ejecución de inspecciones y checklists se completa manualmente desde tablet como experiencia de primera clase.

### 4.1 Layout — Tablet piso (ejecutar checklist)

```
┌ Inspección · Plan "Perfil U · dimensional" · MO-2048 ── 3 de 5 ────┐
│  Ítem 3: Ancho del ala (mm)   Tolerancia: 39,5 – 40,5              │
│        Valor medido  [ 40,2 ]   Unidad: mm                         │
│        Resultado:  ( ◉ OK )  ( ○ NO OK )                           │
│        [ 📷 Adjuntar foto ]                                        │
│  ────────────────────────────────────────────────────────────     │
│  Progreso  ▓▓▓░░  [ ◀ Anterior ]        [ Siguiente ▶ ]            │
│  {al finalizar}         [   ✓ Enviar inspección   ]               │
└─────────────────────────────────────────────────────────────────────┘
```

### 4.2 Layout — Desktop (planes, SPC, disposición)

```
┌ Calidad ── Alcance: Planta Norte ─────────────────────────────────────────┐
│ Inspecciones | « Planes/Checklists » | Defectos | Disposiciones | SPC/FPY  │
│ ┌ Planes ────────────────┐  ┌ SPC · Ancho del ala ──────────────────────┐ │
│ │ ● Perfil U dimensional  │  │  LSC ─────────────────────────  ●ámbar    │ │
│ │ ● Soldadura visual      │  │      ·  ·   · ·  ·· ·  · (dentro control) │ │
│ │ ● Pintura               │  │  LIC ─────────────────────────            │ │
│ │ [+ Nuevo plan]          │  │  FPY: 94%   Cpk: 1,12                     │ │
│ └─────────────────────────┘  └───────────────────────────────────────────┘ │
│ Disposición pendiente ▸ Lote L-5567 (NO conforme)                          │
│  Defecto: fuera de tolerancia (ancho)   Cant: 40 pz                        │
│  Decisión:  ( ○ Aceptar )  ( ◉ Reprocesar )  ( ○ Rechazar→Scrap )          │
│  Justificación [__________________________]   [ Confirmar disposición ]    │
└─────────────────────────────────────────────────────────────────────────────┘
```

- **Componentes:** stepper de checklist (un ítem por pantalla en piso), campo de valor con tolerancia visible, radios OK/NO OK grandes, adjuntar foto; (desktop) lista de planes, gráfico SPC con límites de control, KPIs FPY/Cpk, panel de disposición con justificación obligatoria.
- **Estados:** cargando; sin inspección asignada (piso: "no tenés inspecciones pendientes"); valor fuera de tolerancia (marca ●rojo + fuerza resultado NO OK o pide confirmación); disposición sin justificación (bloquea confirmar, D7); sin permiso (Operario no ve pestaña Disposiciones — P4).
- **Interacciones:** disposición es exclusiva de Calidad; NO OK puede disparar creación de defecto y sugerir scrap; envío offline se encola con **banner de conexión visible** y estados **pendiente → sincronizando → enviado**.
- **Responsive:** piso = un ítem grande por paso; desktop = análisis SPC + gestión; mobile = consulta de resultados y alertas de calidad.

---

## 5. Scrap

- **Objetivo:** registrar descarte con motivo y evidencia (piso); analizar costos y causas (desktop).
- **Usuario:** Operario (registra), Supervisor/Producción/Calidad (analizan/clasifican). **Superficie:** tablet + desktop.
- **Enlaza a:** [scrap.md](./scrap.md).
- **Carga manual completa desde tablet (PRD-03):** el registro de scrap con motivo, cantidad y evidencia se completa manualmente desde tablet (experiencia de primera clase).

### 5.1 Layout — Tablet piso (grilla de motivos)

```
┌ Registrar scrap · Línea 3 · MO-2048 ───────────────────────────────┐
│  1) Elegí el motivo                                                 │
│  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐                       │
│  │ 🔧     │ │ 🎨     │ │ 📏     │ │ ⚙      │                       │
│  │ Rebaba │ │Pintura │ │Medida  │ │ Setup  │                       │
│  └────────┘ └────────┘ └────────┘ └────────┘                       │
│  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐                       │
│  │ 🔥     │ │ 💧     │ │ ❓     │ │  +     │                       │
│  │Térmico │ │Humedad │ │ Otro   │ │  más   │                       │
│  └────────┘ └────────┘ └────────┘ └────────┘                       │
│  2) Cantidad  [ 12 ]  piezas ▼    3) [ 📷 Foto (opcional) ]         │
│              ┌───────────────────────────┐                          │
│              │      ✕ CONFIRMAR SCRAP     │                          │
│              └───────────────────────────┘                          │
│  ● Se enviará al recuperar conexión {offline}                       │
└──────────────────────────────────────────────────────────────────────┘
```

### 5.2 Layout — Desktop (análisis de scrap)

```
┌ Scrap ── Alcance: Planta Norte ── Periodo [Turno ▼] ──────────────────────┐
│ « Registros » | Motivos (catálogo) | Costos                               │
│ Pareto de motivos                    Costo de scrap                       │
│ ┌───────────────────────────┐        ┌──────────────────────────┐         │
│ │ Rebaba   ▓▓▓▓▓▓▓ 42%       │        │ Total turno: $ 18.400     │        │
│ │ Medida   ▓▓▓▓ 25%          │        │ ●rojo 3,2% (meta 2,0%)    │        │
│ │ Pintura  ▓▓ 14%            │        │ Tendencia ▂▃▅▄▆           │        │
│ │ Otros    ▓ 19%             │        └──────────────────────────┘         │
│ └───────────────────────────┘                                            │
│ Registros: [tabla: hora·línea·motivo·cant·costo·operario·estado·sync]     │
│ [ Clasificar por calidad ] (rol Calidad)   [ Exportar ]                   │
└─────────────────────────────────────────────────────────────────────────────┘
```

- **Componentes:** grilla visual de motivos (íconos + texto), campo cantidad, foto opcional, botón confirmar; (desktop) Pareto de motivos, tarjeta de costo/rate vs meta, tabla de registros, acción "clasificar por calidad".
- **Estados:** motivo no seleccionado (bloquea confirmar — el motivo es obligatorio); "Otro" pide texto; offline encola con **banner de conexión siempre visible** y badge **pendiente → sincronizando → enviado** (sin duplicar, `dedup_key`); sin permiso (clasificar por calidad solo rol Calidad).
- **Interacciones:** seleccionar motivo → cantidad → confirmar (≤ 3 toques, D2); "más" expande el catálogo completo si hay muchos motivos; el rate compara contra meta con color semántico.
- **Responsive:** piso = grilla táctil; desktop = Pareto + costos + tabla; mobile = KPI de scrap y alerta si supera meta.

---

## 6. Paradas

- **Objetivo:** registrar y cerrar paradas con motivo y tiempo real (piso); analizar MTBF/MTTR (desktop).
- **Usuario:** Operario/Supervisor (registran), Mantenimiento (gestiona/analiza). **Superficie:** tablet + desktop.
- **Enlaza a:** [downtime.md](./downtime.md).
- **Carga manual completa desde tablet (PRD-03):** el registro y cierre de paradas con motivo y tiempo real se completa manualmente desde tablet.

### 6.1 Layout — Tablet piso (cronómetro + motivo)

```
┌ Parada · Línea 3 ─────────────────────────────────── ●rojo DETENIDA ─┐
│                                                                       │
│                 ⏱  00:12:38   (corriendo)                            │
│                                                                       │
│   Motivo de la parada                                                 │
│   ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐                        │
│   │Cambio  │ │ Falla  │ │ Falta  │ │Limpieza│                        │
│   │utillaje│ │mecánica│ │material│ │        │                        │
│   └────────┘ └────────┘ └────────┘ └────────┘                        │
│   ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐                        │
│   │Eléctr. │ │ Ajuste │ │Descanso│ │ Otro   │                        │
│   └────────┘ └────────┘ └────────┘ └────────┘                        │
│   Clasificación: ( ◉ No programada ) ( ○ Programada )                 │
│              ┌───────────────────────────┐                            │
│              │   ▶ REANUDAR / CERRAR      │                           │
│              └───────────────────────────┘                            │
└───────────────────────────────────────────────────────────────────────┘
```

### 6.2 Layout — Desktop (análisis de paradas)

```
┌ Paradas ── Alcance: Planta Norte ─────────────────────────────────────────┐
│ « Eventos » | Motivos (catálogo) | MTBF/MTTR                              │
│ ┌ KPIs ─────────────────┐  Línea de tiempo de paradas (hoy)               │
│ │ MTBF  4,2 h            │  L1 ──███───────██──────────  ●ámbar           │
│ │ MTTR  22 min           │  L2 ────────█████████████───  ●rojo (activa)   │
│ │ Paradas hoy: 7         │  L3 ──█──────────────────────  ●verde          │
│ └───────────────────────┘  [leyenda: █ parada]                           │
│ Eventos: [tabla: inicio·fin·dur·línea·motivo·clasif·estado·responsable]   │
│  Seleccionado ▸ L2 · Falla mecánica · 12min ·  [ Asignar Mantenim. ]      │
│  [ Registrar causa técnica y reparación ] (rol Mantenimiento)             │
└─────────────────────────────────────────────────────────────────────────────┘
```

- **Componentes:** cronómetro grande en vivo, grilla de motivos, toggle programada/no programada, botón cerrar; (desktop) tarjetas MTBF/MTTR, línea de tiempo por línea, tabla de eventos, panel de causa técnica.
- **Estados:** parada en curso (color ●rojo persistente + cronómetro corriendo); motivo obligatorio para cerrar; offline encola con marca de tiempo local, **banner de conexión visible** y estado **pendiente → sincronizando → enviado**; sin permiso (registrar causa técnica solo Mantenimiento).
- **Interacciones:** iniciar parada arranca cronómetro automático (tiempo real, no estimado); cerrar exige motivo; Mantenimiento añade causa técnica y reparación (alimenta MTTR).
- **Responsive:** piso = cronómetro + motivos táctiles; desktop = timeline + análisis; mobile = alerta de parada activa con [Reconocer]/[Ver] y duración en vivo.

---

## 7. Dispositivos

- **Objetivo:** inventariar dispositivos/sensores, monitorear salud y gestionar firmware/OTA.
- **Usuario:** Mantenimiento (gestiona), Supervisor/Producción (consultan), Admin. **Superficie:** desktop (principal) + tablet (diagnóstico en piso).
- **Enlaza a:** [devices.md](./devices.md).

### 7.1 Layout — Desktop (inventario + salud)

```
┌ Dispositivos ── Alcance: Planta Norte ────────────────────────────────────┐
│ « Inventario » | Sensores/Señales | Salud/Diagnóstico | Firmware/OTA       │
│ Filtros [Tipo ▼][Línea ▼][Estado ▼]        🔍 [__________]  [+ Dispositivo]│
│ ┌──────────────┬─────────┬────────┬─────────┬──────────┬─────────────────┐ │
│ │ Dispositivo  │ Tipo    │ Línea  │ Salud   │ Firmware │ Última lectura  │ │
│ ├──────────────┼─────────┼────────┼─────────┼──────────┼─────────────────┤ │
│ │ PLC-S7-L3    │ PLC     │ L3     │ ●verde  │ v2.4.1   │ hace 3 s        │ │
│ │ DL-Temp-07   │Datalog. │ L3     │ ●ámbar  │ v1.9.0 ⬆ │ hace 12 s       │ │
│ │ ESP32-Bal-02 │ ESP32   │ L4     │ ●rojo   │ v0.8.3   │ hace 6 min ⚠    │ │
│ │ GW-Norte-01  │ Gateway │ —      │ ●verde  │ v3.1.0   │ hace 1 s        │ │
│ └──────────────┴─────────┴────────┴─────────┴──────────┴─────────────────┘ │
│  Seleccionado ▸ ESP32-Bal-02  ● Sin conexión hace 6 min                    │
│  Señales: peso(kg) ●gris · estado ●gris   [ Diagnóstico ] [ Reintentar ]   │
│  [ Programar OTA v0.9.0 ] {activo crítico → doble confirmación}            │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 7.2 Layout — Tablet piso (diagnóstico rápido)

```
┌ Dispositivo · ESP32-Bal-02 · Línea 4 ──────────── ●rojo Sin conexión ─┐
│  Última lectura: hace 6 min                                            │
│  Señal peso: —   Señal estado: —                                       │
│  [ 🔄 Reintentar conexión ]   [ 📶 Ver señal ]                         │
│  [ Reportar a Mantenimiento ]                                          │
└─────────────────────────────────────────────────────────────────────────┘
```

- **Componentes:** tabla de inventario con salud (color semántico + ícono), badge de firmware con indicador de actualización disponible (⬆), panel de detalle con señales/tags, acciones diagnóstico/reintentar/OTA; (piso) tarjeta de dispositivo con reintento.
- **Estados:** dispositivo sin conexión (●rojo + tiempo desde última lectura); firmware desactualizado (⬆); OTA en curso (barra de progreso + advertencia de no cortar); OTA sobre activo crítico (modal de doble confirmación + step-up, [users-permissions.md](./users-permissions.md)); sin permiso (OTA solo Mantenimiento).
- **Interacciones:** seleccionar → ver señales/salud; reintentar conexión; programar OTA con confirmación reforzada según criticidad (ABAC).
- **Responsive:** desktop = inventario denso + detalle; tablet = diagnóstico puntual; mobile = alerta de dispositivo caído.

---

## 8. Integraciones

- **Objetivo:** configurar y operar conectores con ERP (Odoo), mapeos/ACL y monitorear jobs de sincronización.
- **Usuario:** Integraciones, Administrador. **Superficie:** desktop.
- **Enlaza a:** [integrations.md](./integrations.md).

### 8.1 Layout — Desktop (conectores + jobs)

```
┌ Integraciones ── Alcance: Tenant ─────────────────────────────────────────┐
│ « Conectores » | Mapeos/ACL | Jobs de sync | Errores                      │
│ ┌ Conectores ─────────────────────────────────────────────────────────┐  │
│ │ ● Odoo (Producción)      Estado: ●verde Activo   Últ. sync: hace 1min│  │
│ │ ● Odoo (Inventario)      Estado: ●ámbar Degradado  reintentos: 3     │  │
│ │ [+ Nuevo conector]                                                    │  │
│ └───────────────────────────────────────────────────────────────────────┘  │
│ Cola de Jobs de sincronización                                             │
│ ┌───────┬──────────────┬─────────┬──────────┬────────────────────────────┐ │
│ │ Hora  │ Entidad      │ Dirección│ Estado   │ Detalle                    │ │
│ ├───────┼──────────────┼─────────┼──────────┼────────────────────────────┤ │
│ │ 10:22 │ Producción   │ →Odoo   │ ●verde OK│ MO-2048 · 125 pz           │ │
│ │ 10:20 │ Producción   │ →Odoo   │ ●rojo Err│ Mapeo producto no hallado  │ │
│ │       │              │         │          │ [ Ver ] [ Reintentar ]     │ │
│ └───────┴──────────────┴─────────┴──────────┴────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 8.2 Layout — Desktop (editor de mapeo / ACL)

```
┌ Mapeo · Odoo (Producción) ────────────────────────────────────────────────┐
│  Campo Nexo                 ↔   Campo ERP (Odoo)         Transformación     │
│  Registro.cantidad          →   mrp.production.qty       (1:1)              │
│  Registro.producto (SKU)    →   product.default_code     lookup             │
│  Registro.turno             →   —  (no mapeado) ⚠                            │
│  Registro.timestamp         →   date_planned             tz: UTC-3          │
│  [ + Agregar mapeo ]                                                       │
│  ─────────────────────────────────────────────────────────────────────    │
│  [ Probar con registro de ejemplo ]  → resultado: ●verde OK                │
│  [ Guardar borrador ]     [ Activar mapeo ] {requiere prueba OK}           │
└─────────────────────────────────────────────────────────────────────────────┘
```

- **Componentes:** lista de conectores con estado/salud, cola de jobs con estado y reintento, bandeja de errores, editor de mapeo campo-a-campo con transformación y prueba, referencias de credenciales (nunca valores en claro).
- **Estados:** conector degradado (●ámbar + reintentos); job con error (●rojo + causa legible + reintentar); credencial vencida (bloquea + pide renovar); mapeo incompleto (⚠ campos sin mapear); activar sin prueba OK (bloqueado, D7); sin permiso (Operario no accede — no aparece en menú).
- **Interacciones:** probar antes de activar (obligatorio); reintentar job individual o en lote; ver payload normalizado del evento; los registros ya sincronizados no se reeditan (se ajusta con evento compensatorio).
- **Responsive:** desktop-only en profundidad; mobile solo consulta el estado de sincronización y alertas de fallo de conector; **por qué:** configurar mapeos en un teléfono es inviable y riesgoso ([ui-ux.md](./ui-ux.md) §8).

---

## 9. Configuración

- **Objetivo:** definir estructura (plantas/sectores/líneas/máquinas), catálogos, turnos, usuarios y preferencias del tenant.
- **Usuario:** Administrador (principal); dueños de dominio editan sus catálogos. **Superficie:** desktop.
- **Enlaza a:** [users-permissions.md](./users-permissions.md), [multi-tenancy.md](./multi-tenancy.md).

### 9.1 Layout — Desktop (estructura + secciones)

```
┌ Configuración ── Tenant: Empresa S.A. ────────────────────────────────────┐
│ ≡ Estructura │  Estructura de planta                                       │
│   Catálogos  │  ┌ Árbol ──────────────┐  ┌ Detalle: Línea 3 ────────────┐ │
│   Usuarios   │  │ ▸ Planta Norte       │  │ Nombre  [ Línea 3        ]   │ │
│   Turnos     │  │   ▸ Sector Corte     │  │ Sector  [ Corte        ▼ ]   │ │
│   Auditoría  │  │     • Línea 1        │  │ Máquinas: 3   Estado: activa │ │
│   Preferenc. │  │     • Línea 3  ◀     │  │ Takt objetivo [ 45 ] s       │ │
│              │  │   ▸ Sector Pintura   │  │ [ Guardar ]  [ Desactivar ]  │ │
│              │  │ ▸ Planta Sur         │  │                              │ │
│              │  │ [+ Planta][+ Línea]  │  └──────────────────────────────┘ │
│              │  └──────────────────────┘                                   │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 9.2 Layout — Desktop (Usuarios y permisos)

```
┌ Configuración ▸ Usuarios y permisos ──────────────────────────── [+ Invitar]┐
│ ┌───────────────┬──────────────┬──────────────────┬───────┬──────────────┐  │
│ │ Usuario       │ Rol          │ Alcance          │ MFA   │ Estado       │  │
│ ├───────────────┼──────────────┼──────────────────┼───────┼──────────────┤  │
│ │ J. Pérez      │ Operario     │ P.Norte / L3     │ PIN   │ ●verde Activo│  │
│ │ M. Gómez      │ Supervisor   │ P.Norte          │ ●OK   │ ●verde Activo│  │
│ │ A. Ruiz       │ Calidad      │ P.Norte, P.Sur   │ ●OK   │ ●ámbar Susp. │  │
│ │ svc-odoo      │ Integraciones│ Tenant           │ n/a   │ ●verde Activo│  │
│ └───────────────┴──────────────┴──────────────────┴───────┴──────────────┘  │
│  Seleccionado ▸ A. Ruiz   [ Editar rol/alcance ] [ Reset MFA ] [ Reactivar ] │
│  ⚠ SoD: revisar combinación Producción + disposición de Calidad (si aplica)  │
└─────────────────────────────────────────────────────────────────────────────────┘
```

- **Componentes:** navegación por secciones, árbol de estructura editable, formularios de detalle con validación, tabla de usuarios con rol/alcance/MFA/estado, invitación, editor de rol+alcance, avisos de SoD, acceso a auditoría (lectura).
- **Estados:** cambios sin guardar (aviso al salir); acción irreversible (confirmación); límite de licencia alcanzado al invitar (bloqueo + mensaje del plan, [control-plane.md](./control-plane.md)); sin permiso (solo Administrador ve "Usuarios"); conflicto de SoD (advertencia no bloqueante para revisión).
- **Interacciones:** un usuario puede tener múltiples asignaciones rol+alcance; invitar dispara flujo de onboarding; reset MFA auditado; editar estructura respeta jerarquía Planta→Sector→Línea→Máquina.
- **Responsive:** desktop-only; **por qué:** la configuración es infrecuente, compleja y de alto impacto; se privilegia claridad y validación sobre movilidad ([ui-ux.md](./ui-ux.md) §7/§8).

---

## 10. Reglas

- **Objetivo:** definir reglas trigger-condición-acción en tiempo real y ver su historial de disparos.
- **Usuario:** Producción, Calidad, Mantenimiento (según dominio), Administrador. **Superficie:** desktop.
- **Enlaza a:** [rules-engine.md](./rules-engine.md), [notifications.md](./notifications.md).

### 10.1 Layout — Desktop (constructor de regla)

```
┌ Reglas ── Alcance: Planta Norte ──────────────────────── [+ Nueva regla] ──┐
│ « Reglas » | Historial de disparos                                         │
│ ┌ Lista ───────────────────┐  ┌ Constructor: "Scrap sobre meta" ────────┐ │
│ │ ● Scrap sobre meta  ●on  │  │ CUANDO (trigger)                         │ │
│ │ ● Parada > 15 min   ●on  │  │   [ Registro de scrap ▼ ]                │ │
│ │ ● Temp fuera rango  ●off │  │ SI (condición)                           │ │
│ │ [+ Nueva]                │  │   [ scrap_rate ] [ > ] [ 2,0 ] [ % ]     │ │
│ │                          │  │   [ + AND / OR ]                         │ │
│ │                          │  │ ENTONCES (acción)                        │ │
│ │                          │  │   [x] Notificar → Supervisor de línea    │ │
│ │                          │  │   [ ] Crear alerta ●ámbar                │ │
│ │                          │  │   [ ] Registrar evento                   │ │
│ │                          │  │ Alcance [ Línea 3 ▼ ]  Cooldown [10] min │ │
│ │                          │  │ [ Probar ] [ Guardar borrador ] [Activar]│ │
│ └──────────────────────────┘  └──────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 10.2 Layout — Desktop (historial de disparos)

```
┌ Reglas ▸ Historial de disparos ───────────────────────────────────────────┐
│ [tabla: hora · regla · disparó por · acción ejecutada · resultado]         │
│ 10:31 · Scrap sobre meta · L3 3,2% · Notificó a M.Gómez · ●verde entregada │
│ 09:58 · Parada > 15 min · L2 · Creó alerta ●rojo · ●verde ok               │
└─────────────────────────────────────────────────────────────────────────────┘
```

- **Componentes:** lista de reglas con toggle on/off, constructor trigger-condición-acción (bloques), selector de acciones (notificar/alertar/registrar), alcance, cooldown, botones probar/guardar/activar, historial de disparos.
- **Estados:** regla en borrador vs activa; condición incompleta (bloquea activar); prueba con datos de ejemplo (muestra si dispararía); cooldown para evitar tormenta de alertas (anti alert-fatigue); sin permiso (cada dominio ve/edita sus reglas — Calidad no toca reglas de Mantenimiento salvo Admin).
- **Interacciones:** construir por bloques; probar antes de activar (D7); el historial permite auditar por qué y cuándo disparó; las acciones enlazan con [notifications.md](./notifications.md).
- **Responsive:** desktop-only para construir; mobile consulta el historial y recibe las alertas resultantes.

---

## 11. Alertas

- **Objetivo:** centralizar alertas activas, permitir reconocer/escalar y gestionar suscripciones.
- **Usuario:** todos (según suscripción/alcance). **Superficie:** las tres, con foco en mobile.
- **Enlaza a:** [notifications.md](./notifications.md), [rules-engine.md](./rules-engine.md).

### 11.1 Layout — Desktop (bandeja de alertas)

```
┌ Alertas ── Alcance: Planta Norte ── [Activas ▼] ──────────────────────────┐
│ « Bandeja » | Suscripciones | Plantillas/Canales (Admin)                  │
│ ┌─────┬──────────────────────────┬───────────┬───────────┬──────────────┐ │
│ │ Sev │ Alerta                   │ Origen    │ Estado    │ Acciones     │ │
│ ├─────┼──────────────────────────┼───────────┼───────────┼──────────────┤ │
│ │●rojo│ Parada L2 > 15 min       │ Regla     │ Sin reconc│[Reconocer]   │ │
│ │     │ (12 min y contando)      │ "Parada.."│           │[Escalar]     │ │
│ │●ámbar│ Scrap L3 sobre meta 3,2%│ Regla     │Reconocida │[Ver] M.Gómez │ │
│ │●rojo│ ESP32-Bal-02 sin conexión│ Umbral    │ Sin reconc│[Reconocer]   │ │
│ │●azul│ Sync Odoo degradado      │ Conector  │ Info      │[Ver]         │ │
│ └─────┴──────────────────────────┴───────────┴───────────┴──────────────┘ │
│  Seleccionada ▸ Parada L2 · timeline: disparó 10:19 · notificó 10:19      │
└─────────────────────────────────────────────────────────────────────────────┘
```

### 11.2 Layout — Mobile (alerta accionable + push)

```
┌ Push (bloqueo) ───────────────┐   ┌ Bandeja móvil ───────────────┐
│ 🔔 Nexo · ●rojo               │   │ ●rojo Parada L2 · 12 min      │
│ Parada L2 > 15 min            │   │   [Reconocer]  [Escalar]      │
│ Planta Norte · Línea 2        │   │ ●ámbar Scrap L3 · 3,2%        │
│ [ Reconocer ]  [ Abrir ]      │   │   [Reconocer]  [Ver]          │
└───────────────────────────────┘   │ ●rojo ESP32-Bal-02 caído      │
                                     │   [Reconocer]  [Ver]          │
                                     ├──────────────────────────────┤
                                     │ 🔔Alertas 📊KPIs 🔍 👤        │
                                     └──────────────────────────────┘
```

- **Componentes:** bandeja con severidad (color semántico + texto), origen (regla/umbral/conector), estado (sin reconocer/reconocida/resuelta), acciones reconocer/escalar/ver, timeline de la alerta, gestión de suscripciones, plantillas/canales (Admin), push accionable.
- **Estados:** sin alertas (estado vacío positivo: "Todo en orden ●verde"); alerta crítica sin reconocer (persistente + reintento de notificación/escalado); alerta reconocida (muestra quién y cuándo); offline (últimas alertas en caché + aviso); sin permiso (Plantillas/Canales solo Admin).
- **Interacciones:** reconocer detiene el escalado; escalar sube al siguiente responsable; el cooldown de reglas evita duplicados; desde push se reconoce sin abrir la app (reduce tiempo de reacción, [ui-ux.md](./ui-ux.md) §8).
- **Responsive:** mobile prioritario (accionable con el pulgar); desktop = bandeja de gestión y análisis; tablet supervisor = alertas de su alcance con acción rápida.

---

## 12. Trazabilidad entre mockups y módulos (mapa rápido)

| Pantalla | Módulo(s) | Superficie primaria | Rol(es) principal(es) |
|----------|-----------|----------------------|------------------------|
| Login | Identity & Access | Todas | Todos |
| Dashboard principal | Dashboards/Analytics | Desktop / Mobile | Supervisor, Gerencia |
| Producción | Production | Tablet piso / Desktop | Operario, Supervisor, Producción |
| Calidad | Quality | Tablet piso / Desktop | Operario, Calidad |
| Scrap | Scrap | Tablet piso / Desktop | Operario, Supervisor, Calidad |
| Paradas | Downtime | Tablet piso / Desktop | Operario, Supervisor, Mantenimiento |
| Dispositivos | Devices | Desktop / Tablet | Mantenimiento |
| Integraciones | Connectors/Integrations | Desktop | Integraciones, Admin |
| Configuración | Identity + estructura/catálogos | Desktop | Administrador |
| Reglas | Rules Engine | Desktop | Producción, Calidad, Mantenimiento, Admin |
| Alertas | Notifications + Rules | Mobile / Desktop | Todos |

---

## 13. Preguntas abiertas

1. **Densidad de la grilla de motivos (scrap/parada):** ¿cuántos motivos entran cómodos en piso antes de necesitar paginación/categorías? Depende del tamaño de tablet objetivo ([ui-ux.md](./ui-ux.md) PA1).
2. **Confirmación de cantidades atípicas:** ¿qué umbral dispara la confirmación "¿seguro?" al registrar producción/scrap fuera de rango esperado? Coordinar con [production.md](./production.md).
3. ✅ **Resuelto (2026-07-11):** el uso offline es una decisión cerrada (offline-first con store-and-forward y `dedup_key`); la captura, los catálogos frecuentes y la consulta de órdenes/checklists operan offline con historial acotado, y el banner de estado de conexión (que comunica el "dato al corte de hace X") permanece siempre visible — ver [tablero de decisiones](../open-questions-board.md).
4. **Editor de mapeo ERP:** ¿ofrecemos plantillas de mapeo preconfiguradas por ERP (Odoo) para acelerar el onboarding? Coordinar con [integrations.md](./integrations.md).
5. **Constructor de reglas — nivel de complejidad:** ¿hasta qué punto exponemos AND/OR anidados y funciones sin volverlo inusable para un no técnico? Coordinar con [rules-engine.md](./rules-engine.md).
6. **Acciones desde push:** ¿qué acciones son seguras de ejecutar desde una notificación sin abrir la app (reconocer sí; escalar sí; disponer calidad no)? Coordinar con [notifications.md](./notifications.md) y [users-permissions.md](./users-permissions.md).
7. **Multi-idioma en wireframes:** ¿los textos de piso (verbos de acción) necesitan versiones cortas para idiomas que expanden la longitud? Impacta el ancho de botones grandes.
8. **Personalización de KPIs del dashboard por rol:** ¿el usuario puede reordenar/elegir sus tarjetas, o el set es fijo por rol para consistencia? Coordinar con [dashboards.md](./dashboards.md).
