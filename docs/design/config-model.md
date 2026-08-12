# V-A · Modelo de datos de configuración (propuesta)

> **Documento:** `docs/design/config-model.md` · **Creado:** 2026-08-11 · **Estado:** propuesta.
> El dominio de **configuración** del MES (lo que "definimos": planta, cámaras, señales, catálogos, reglas).
> Vive en la **tenant DB** (`hexa_{slug}_mes`, Neon). Lo administra `Nexo.MesApi`; lo **consume el edge** (pull) y el **rules-engine**. Deriva de [rules-and-events.md](./rules-and-events.md) y [questions.md](../questions.md).

---

## Entidades

### 1. `LocationNode` — jerarquía de planta (Planta→Sector→Línea→Estación)
Un solo árbol con `level`; cámaras y dispositivos se cuelgan de **cualquier** nodo.

| Campo | Tipo | Notas |
|---|---|---|
| `id` | uuid | |
| `parent_id` | uuid? | null = raíz (Planta) |
| `level` | enum | `site` \| `area` \| `line` \| `station` (coherente con el parent) |
| `code`, `name` | text | |

### 2. `Camera` — fuente de visión
| Campo | Tipo | Notas |
|---|---|---|
| `id` | uuid | |
| `location_node_id` | uuid | dónde está |
| `code`, `name` | text | |
| `stream_url` | text | RTSP/IP; USB por ref local |
| `transport` | enum | `rtsp` \| `usb` |
| `fps`, `resolution` | int/text | objetivo de captura |
| `status` | enum | `active` \| `inactive` |
| `adjacent_cameras` | jsonb | ids de cámaras vecinas — **preparado para cross-cámara futuro** |

### 3. `Zone` — región de interés (polígono) dentro de una cámara
| Campo | Tipo | Notas |
|---|---|---|
| `id` | uuid | |
| `camera_id` | uuid | |
| `code`, `name` | text | p. ej. `salida`, `zona_peligrosa` |
| `polygon` | jsonb | lista de `[x,y]` **normalizados 0..1** (independiente de resolución) |
| `purpose` | text? | etiqueta libre |

### 4. `SignalDevice` — fuente industrial (MQTT en el MVP)
| Campo | Tipo | Notas |
|---|---|---|
| `id` | uuid | |
| `location_node_id` | uuid | |
| `code`, `name` | text | |
| `protocol` | enum | `mqtt` (S7/OPC-UA/Modbus después) |
| `config` | jsonb | broker, tópico base, credencial-ref (secreto, no inline) |

### 5. `Signal` — tag/variable de un dispositivo
| Campo | Tipo | Notas |
|---|---|---|
| `id` | uuid | |
| `device_id` | uuid | |
| `code`, `name` | text | |
| `mqtt_topic` | text | tópico exacto |
| `json_path` | text? | si el payload es JSON, el path del valor |
| `vtype` | enum | `number` \| `bool` \| `string` |
| `unit` | text? | °C, u/min… |
| `persist` | enum | `events_only` \| `timeseries` (D10: configurable por señal) |

### 6. `DetectionClass` — catálogo de objetos/acciones (base + tenant)
| Campo | Tipo | Notas |
|---|---|---|
| `id` | uuid | |
| `kind` | enum | `object` \| `action` |
| `code`, `name` | text | `persona`, `caja`, `defecto`, `coloca_pieza`… |
| `scope` | enum | `shared` (base) \| `tenant` (custom) |

### 7. `VisionModel` — artefacto de inferencia (opcional, versionado)
| Campo | Tipo | Notas |
|---|---|---|
| `id` | uuid | |
| `kind` | enum | `object_detection` \| `action_recognition` \| `pose` |
| `version` | text | |
| `artifact_ref` | text | S3/registry (ONNX/TensorRT) |
| `provides_classes` | jsonb | códigos de `DetectionClass` que reconoce |
| `target` | enum | `edge` |

### 8. `Rule` — regla (trigger → evento)
| Campo | Tipo | Notas |
|---|---|---|
| `id` | uuid | |
| `code`, `name` | text | |
| `enabled` | bool | |
| `scope_location_node_id` | uuid? | acota dónde aplica (null = todo) |
| `trigger` | jsonb | **árbol de nodos** (§3 de rules-and-events) |
| `emit` | jsonb | `event_type` / `severity` / `payload` / `evidence` |
| `cooldown_seconds` | int | debounce (key = `rule_id`+`source`) |

> **`event_type` NO es una tabla** en el MVP: es libre, lo nombra la regla (D4). Si hace falta un registro por tenant para la UI/HEXA, se agrega como vista derivada.

---

## Relaciones

```
LocationNode (árbol: site→area→line→station)
  ├─ Camera ──┬─ Zone (polígono)
  │           └─ (adjacent_cameras → futuro cross-cámara)
  └─ SignalDevice ── Signal
Rule ── scope → LocationNode ; trigger referencia Camera/Zone/Signal/DetectionClass
DetectionClass (catálogo) ← VisionModel.provides_classes
```

## Notas de implementación
- Todo en la **tenant DB** (`hexa_{slug}_mes`); sin `tenant_id` en las tablas (aislamiento físico, como HEXA).
- El **edge** hace *pull* de esta config (cámaras/zonas/señales/modelos/reglas de su sitio) y la cachea; el **rules-engine** consume reglas + catálogo.
- `polygon` normalizado 0..1 para no atarlo a la resolución de la cámara.
- Referencias a HEXA (artículo/estación de negocio) por **id**, sin FK (contexto externo).

## Pendiente antes de codear
- **Nombres de tabla/campos** finales (¿español/inglés? el resto del MES está en inglés técnico). · Rec: inglés técnico (`location_nodes`, `cameras`, `zones`, `signals`, `rules`).
- **Estructura del servicio** `Nexo.MesApi`: ¿Clean Arch en capas (Domain/Application/Infra/Api, como los servicios viejos) o **un proyecto** más liviano (como `EventEngine`)?
