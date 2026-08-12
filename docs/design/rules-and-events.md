# Esquema de Reglas y Eventos (propuesta para revisar)

> **Documento:** `docs/design/rules-and-events.md` · **Creado:** 2026-08-11 · **Estado:** propuesta.
> Cierra los grupos **C** (esquema de la regla) y **G** (payload del evento) de [questions.md](../questions.md).
> Base para el modelo de datos de **V-A** (config) y **V-C** (motor de reglas).

Decisiones que respeta: fuentes **visión + señal**; reglas de complejidad **completa** (simple → combinada → temporal → agregación); **eventos crudos** (HEXA decide); catálogo de `event_type` **libre por tenant** (la regla lo nombra); entrega **tiempo real**.

---

## 1. Observación (lo que el motor consume)

Todo se normaliza a una **observación**. Hay dos clases; ambas viajan al motor por el mismo stream.

```jsonc
// Observación de VISIÓN (la emite edge-vision)
{
  "obs_type": "vision",
  "camera_id": "cam-1",
  "zones": ["salida"],            // zonas (poligonales) en las que cae la detección
  "kind": "object",              // "object" | "action"
  "class": "caja",               // clase del catálogo (objeto o acción)
  "score": 0.92,
  "track_id": "t-123",           // id persistente del objeto entre frames (tracking)
  "bbox": [x, y, w, h],
  "attrs": { "fill_pct": 0.8 },  // atributos opcionales (pose, %llenado, color…)
  "at": "2026-08-11T12:00:00.123Z"
}

// Observación de SEÑAL (la emite edge-signals desde MQTT)
{
  "obs_type": "signal",
  "signal_id": "sig-temp-horno",
  "value": 83.2,
  "vtype": "number",             // "number" | "bool" | "string"
  "at": "2026-08-11T12:00:00.100Z"
}
```

> **Nota (tracking + zonas):** el edge manda detecciones con `track_id` y las `zones` donde cae. El motor **deriva** `zone_enter` / `zone_exit` observando las transiciones de un `track_id` entre zonas (no lo tiene que calcular el edge). Esto habilita el conteo por cruce sin doble conteo.

---

## 2. Regla

Una regla = **disparador** (árbol de condiciones sobre observaciones) → **emitir** un evento, con **cooldown** (debounce).

```jsonc
{
  "id": "rule-...",
  "name": "Persona sin casco en zona peligrosa",
  "enabled": true,
  "scope": { "line_id": "linea-2" },     // opcional: acota dónde aplica (planta/sector/línea/estación)
  "trigger": { /* NODO — ver §3 */ },
  "emit": {
    "event_type": "riesgo_epp",          // catálogo libre por tenant; lo nombra la regla
    "severity": "critical",              // info | warning | critical
    "payload": { /* plantilla; puede referenciar la observación que disparó, p.ej. {{obs.class}} */ },
    "evidence": { "snapshot": true, "clip": false }   // qué se guarda (config por regla)
  },
  "cooldown_seconds": 30                 // no re-disparar la misma regla+clave antes de N s
}
```

---

## 3. Nodos del disparador (la gramática)

Árbol componible. Los **hojas** matchean una observación; los **combinadores** y **operadores temporales** arman los 4 niveles.

| Nodo | Forma | Nivel |
|---|---|---|
| **match** (visión) | `{ "op":"match", "source":{"camera_id":"cam-1","zone_id":"salida"}, "kind":"object", "class":"caja", "event":"present\|zone_enter\|zone_exit", "where":{"score_gte":0.6} }` | simple |
| **signal** | `{ "op":"signal", "signal_id":"sig-1", "cmp":">=", "value":80 }` | simple |
| **and / or** | `{ "op":"and", "of":[ NODO, NODO ] }` | combinada |
| **not** | `{ "op":"not", "of": NODO }` | combinada |
| **sustained** | `{ "op":"sustained", "for_seconds":300, "of": NODO }` (la condición se mantiene continua N s) | temporal |
| **sequence** | `{ "op":"sequence", "within_seconds":10, "steps":[ NODO, NODO ] }` (A luego B dentro de T) | temporal |
| **count** | `{ "op":"count", "n":5, "window_seconds":3600, "of": NODO }` (N matches en ventana) | agregación |

- Las hojas referencian la **fuente por id tipado** (cámara+zona, o señal), no por texto libre.
- El motor mantiene **estado** por regla (timers de `sustained`, buffers de `sequence`/`count`), con checkpoint para sobrevivir reinicios.

---

## 4. Ejemplos (uno por caso de uso)

**① Conteo/producción** — cada caja que cruza a la zona de salida = una unidad:
```jsonc
{ "name":"Conteo salida",
  "trigger": { "op":"match", "source":{"camera_id":"cam-1","zone_id":"salida"},
               "kind":"object", "class":"caja", "event":"zone_enter", "where":{"score_gte":0.6} },
  "emit": { "event_type":"unidad_producida", "severity":"info",
            "payload":{ "track_id":"{{obs.track_id}}" }, "evidence":{"snapshot":false} },
  "cooldown_seconds": 0 }
```

**② Calidad/defectos** — defecto con confianza alta → snapshot de evidencia:
```jsonc
{ "name":"Defecto visual",
  "trigger": { "op":"match", "source":{"camera_id":"cam-2"},
               "kind":"object", "class":"defecto", "event":"present", "where":{"score_gte":0.7} },
  "emit": { "event_type":"defecto_detectado", "severity":"warning",
            "payload":{ "clase":"{{obs.class}}", "score":"{{obs.score}}" },
            "evidence":{"snapshot":true} },
  "cooldown_seconds": 5 }
```

**③ Seguridad/EPP** — persona en zona peligrosa **sin** casco, sostenido 2 s (combinada + temporal):
```jsonc
{ "name":"EPP faltante",
  "trigger": { "op":"sustained", "for_seconds":2, "of":
      { "op":"and", "of":[
        { "op":"match", "source":{"camera_id":"cam-3","zone_id":"peligrosa"}, "kind":"object", "class":"persona" },
        { "op":"not", "of":
          { "op":"match", "source":{"camera_id":"cam-3","zone_id":"peligrosa"}, "kind":"object", "class":"casco" } }
      ] } },
  "emit": { "event_type":"riesgo_epp", "severity":"critical", "evidence":{"snapshot":true,"clip":true} },
  "cooldown_seconds": 60 }
```

**④ Paradas/estado de máquina** — señal "detenida" (o acción de visión) sostenida 5 min:
```jsonc
{ "name":"Parada prolongada",
  "trigger": { "op":"sustained", "for_seconds":300, "of":
      { "op":"signal", "signal_id":"sig-estado-maquina", "cmp":"==", "value":"detenida" } },
  "emit": { "event_type":"parada_prolongada", "severity":"warning" },
  "cooldown_seconds": 300 }
```

**⑤ Agregación** — 5 defectos en 1 h → alerta de calidad degradada:
```jsonc
{ "name":"Calidad degradada",
  "trigger": { "op":"count", "n":5, "window_seconds":3600, "of":
      { "op":"match", "source":{"camera_id":"cam-2"}, "kind":"object", "class":"defecto", "where":{"score_gte":0.7} } },
  "emit": { "event_type":"calidad_degradada", "severity":"warning" },
  "cooldown_seconds": 1800 }
```

**⑥ Combinada multi-fuente** — persona en zona **y** máquina en marcha (visión + señal):
```jsonc
{ "trigger": { "op":"and", "of":[
     { "op":"match", "source":{"camera_id":"cam-4","zone_id":"z1"}, "kind":"object", "class":"persona" },
     { "op":"signal", "signal_id":"sig-run", "cmp":"==", "value":true } ] },
  "emit": { "event_type":"riesgo_persona_maquina", "severity":"critical", "evidence":{"clip":true} } }
```

---

## 5. Evento canónico (payload hacia HEXA)

Lo que emite una regla y entrega el `event-gateway` a HEXA (webhook tiempo real):

```jsonc
{
  "event_id": "uuid",                    // idempotencia
  "dedup_key": "rule-77:track-123:zone_enter",  // dedup por regla+clave
  "event_type": "defecto_detectado",     // libre por tenant, lo nombró la regla
  "severity": "warning",
  "company_id": "uuid",                  // tenant
  "rule_id": "rule-77",
  "occurred_at": "2026-08-11T12:00:00.123Z",  // cuándo se cumplió (tiempo de origen)
  "emitted_at": "2026-08-11T12:00:00.400Z",   // cuándo lo emitió el motor
  "source": {                            // de dónde salió (lo que aplique)
    "station_id": "est-9", "line_id": "linea-2",
    "camera_id": "cam-2", "zone_id": null, "signal_id": null
  },
  "values": { "clase": "rayon", "score": 0.83 },   // payload de la regla
  "evidence": {                          // referencias, nunca el binario inline
    "snapshot_ref": "s3://…/frame.jpg",
    "clip_ref": null,
    "bboxes": [ { "class":"defecto", "bbox":[x,y,w,h], "score":0.83 } ]
  }
}
```

> Contrato con HEXA: firma HMAC por empresa + reintentos con backoff hasta `ack` + dedup por `dedup_key` (ver [HEXA-INTEGRATION.md](../../HEXA-INTEGRATION.md) §4.3). HEXA mapea `event_type` → acción de negocio (trabar/finalizar/no-conformidad).

---

## 6. Sub-decisiones (estado)

1. ✅ **Tracking obligatorio** en edge-vision (para `zone_enter`/conteo y `sustained` sobre el mismo objeto).
2. ✅ **Cooldown key = `rule_id` + `source`**: no se re-dispara la misma regla para la misma fuente antes de `cooldown_seconds`.
3. ✅ **`same_track` configurable por nodo**: `sustained` y `sequence` aceptan `"same_track": true` para exigir que los matches sean del **mismo `track_id`** (vs cualquier match).
4. ✅ **DSL/JSON primero** (este esquema, para el implementador); **editor visual no-code después**, sobre el mismo modelo.
5. ✅ **Plantillas de payload = sustitución simple** (Opción A, §7): placeholders `{{obs.*}}` / `{{count}}` / `{{duration_seconds}}`, sin expresiones. Transformaciones complejas las hace HEXA (que recibe el dato crudo).

## 7. Plantillas de payload (a decidir)

Cuando una regla emite el evento, el `values` puede llevar datos **de la observación que disparó**. La pregunta es cómo el autor de la regla los referencia.

- **Contexto disponible al emitir:** `obs` (la observación que disparó — la que completó la condición en temporales), `count` (en nodos `count`), `duration_seconds` (en `sustained`), `rule`, `source`.
- **Opción A — Sustitución simple (recomendado):** placeholders `{{obs.class}}`, `{{obs.score}}`, `{{count}}`, `{{duration_seconds}}`. Solo reemplazo, sin lógica. Simple, seguro, cubre el ~95%.
- **Opción B — Expresiones:** permitir cálculo (`{{obs.score * 100}}`, concatenación, condicionales). Más potente, pero más complejo y con superficie de seguridad (hay que sandboxear el evaluador).

> Si más adelante hace falta más contexto, se agrega al objeto disponible **sin cambiar la gramática** de la regla.

## 8. Reglas cross-cámara (FUTURO — fuera del MVP, pero se DEBE incluir)

Un objeto que se mueve de `cam-1` a `cam-2` manteniendo identidad (un `track_id` **continuo entre cámaras**) requiere **re-identificación entre cámaras** (matching por apariencia con modelos de Re-ID y/o *handoff* por geometría/solape de campos). Es **caro** (modelos, calibración) y queda **fuera del MVP** — pero **se debe incluir después**, así que el modelo se deja preparado para no migrar:

- **En el MVP:** el `track_id` es **por cámara**; una regla `sequence`/`sustained` **no** cruza cámaras.
- **Dejar preparado desde ahora:** (a) el motor **no** asume que un `track_id` es único global; (b) el futuro agrega un `global_track_id` opcional que resuelve la Re-ID; (c) el modelo de cámaras contempla **adyacencias/solapes** (`adjacent_cameras`) para el *handoff*.
- **Cuando entre:** las reglas podrán referenciar `global_track_id` y correlacionar observaciones de distintas cámaras como el mismo objeto.
