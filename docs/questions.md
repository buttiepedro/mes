# Definición del MES — decisiones y preguntas abiertas

> **Documento:** `docs/questions.md` · **Creado:** 2026-08-11 · **Estado:** en definición.
> Relacionado: [plan de arquitectura](./design/hexa-integration/README.md) · [brief HEXA](../HEXA-INTEGRATION.md).
> Formato: cada pregunta abierta trae **Rec:** (mi recomendación). Respondé en línea (✅ + tu decisión).

---

## ✅ Ya decidido (rondas 1–3)

| # | Tema | Decisión |
|---|---|---|
| D1 | Casos de uso del piloto | **Los 4**: conteo/producción, calidad/defectos, seguridad/EPP/zonas, paradas/estado de máquina |
| D2 | Fuentes | **Visión (cámaras) + MQTT/IoT** (PLC directo S7/Modbus y datalogger quedan para después) |
| D3 | Complejidad de reglas | **Completa**: simple → combinada (AND/OR multi-fuente) → temporal (secuencia/duración/debounce) → agregación (contar N en ventana) |
| D4 | Frontera MES↔HEXA | **Eventos crudos**: el MES emite `defecto_detectado`/`contador+1`/`maquina_detenida`; **HEXA** les da significado de negocio |
| D5 | Jerarquía de planta | **Planta → Sector → Línea → Estación** (fija, 4 niveles); cámaras/dispositivos se cuelgan de cualquier nivel |
| D6 | Zonas de cámara | **Zonas poligonales** por cámara (ROI nombradas que las reglas referencian) |
| D7 | Catálogo objetos/acciones | **Base compartido + extensible por tenant** (clases genéricas + custom por cliente) |
| D8 | Reconocimiento de acciones | **Modelo dedicado** de action-recognition (temporal) — implica dataset + entrenamiento por acción |
| D9 | Señales MQTT | **Config topic → señal** (con path JSON + tipo num/bool/string); el edge se suscribe y normaliza |
| D10 | Persistencia | **Configurable por señal/cámara** (default: solo eventos; time-series opcional donde se justifique) |
| D11 | Evidencia | **Configurable por regla** (default: snapshot + metadatos; clip donde se pida, p. ej. seguridad) |
| D12 | Entrega a HEXA | **Tiempo real por evento** (webhook firmado + reintentos + idempotencia) |

---

## 🔲 Pendiente de responder

### A · Motor de reglas — dónde corre y latencia
- **A1.** ¿El motor de reglas corre en la **nube**, en el **edge**, o **ambos**? · **Rec:** ambos (mismo binario) — edge para reglas de baja latencia/offline, nube para reglas que cruzan fuentes o sitios.
- **A2.** ¿**Latencia objetivo** observación → evento → HEXA? (¿<1 s para trabar una máquina? ¿<5 s para el resto?) · **Rec:** <1 s en edge para acciones críticas, <5 s end-to-end para el resto.
- **A3.** ¿Qué reglas exigen edge (no toleran el viaje a la nube)? · **Rec:** las que trabar/parar algo físico.

### B · Offline / resiliencia del edge
- **B1.** Si el edge pierde conexión con la nube: ¿**sigue evaluando reglas y generando eventos localmente** (buffer + reenvío), o se detiene? · **Rec:** sigue local (buffer + store-and-forward); es el punto de un edge.
- **B2.** ¿Cuánto **buffer local** garantizado (tiempo/tamaño) ante un corte largo? · **Rec:** 24–72 h de eventos + evidencia, con política de descarte (prioriza eventos sobre observaciones crudas).
- **B3.** ¿Qué pasa con la **evidencia (frames/clips)** durante el corte? ¿se guarda local y sube después? · **Rec:** sí, se guarda local y sube al reconectar.

### C · Esquema de la REGLA (la pieza más delicada — falta bajar a detalle)
- **C1.** Estructura concreta de una regla: **condiciones** sobre observaciones (objeto/acción/señal + cámara/zona) + **operadores temporales** (secuencia `A→B en T`, duración, ventana, debounce) + **agregación** (contar N en T). ¿Confirmamos estos bloques? · **Rec:** sí; te propongo un esquema JSON + 3–4 ejemplos para validar antes de codear.
- **C2.** ¿Cómo se **referencia una fuente** dentro de una regla? (por `cámara+zona`, por `señal`, por `estación`) · **Rec:** por referencia tipada a la entidad de config (cámara/zona/señal), no por texto libre.
- **C3.** ¿La regla tiene **estado** (contador acumulado, timer de duración)? ¿dónde vive (memoria del motor + checkpoint)? · **Rec:** sí, estado en el motor con checkpoint para sobrevivir reinicios.
- **C4.** ¿El **tipo de evento** que emite una regla es **catálogo libre por tenant** (la regla lo nombra) + severidad + payload? · **Rec:** sí, catálogo abierto por tenant; la regla define `event_type`, `severity`, y qué campos van en el payload.
- **C5.** ¿Se necesita **prioridad/orden** entre reglas o supresión (una regla silencia otra)? · **Rec:** empezar sin prioridades; sumar supresión si aparece ruido.

### D · Visión — modelos, tracking, provisioning
- **D1.** ¿Quién **provee los modelos** (detección de objetos + acciones)? ¿los entrenamos nosotros, el cliente trae datos, o pre-entrenados? · **Rec:** pre-entrenados para clases genéricas + pipeline de entrenamiento nuestro para clases/acciones custom con datos del cliente.
- **D2.** ¿Cómo se **agrega una clase/acción custom** por tenant (dataset → etiquetado → entrenamiento → despliegue del modelo al edge)? · **Rec:** flujo asistido; el modelo se versiona y se baja al edge como config.
- **D3.** ¿Necesitamos **tracking de objetos** (IDs persistentes entre frames) para conteo y acciones? · **Rec:** sí (casi obligatorio para no contar dos veces y para acciones).
- **D4.** ¿**Pose estimation** para acciones de personas (EPP, gestos)? · **Rec:** sí para seguridad/EPP; evaluar por caso.
- **D5.** ¿**Framerate/resolución** objetivo por cámara y **cuántas cámaras por edge/GPU**? · **Rec:** definir con el hardware (E) y el caso; típico 5–15 fps para detección.

### E · Hardware del edge
- **E1.** ¿Qué **hardware** corre el edge? ¿lo **proveemos** (appliance con GPU) o el cliente pone la caja? · **Rec:** appliance provisto recomendado (control de compatibilidad/soporte); opción "software sobre hardware del cliente".
- **E2.** ¿**GPU objetivo** (Jetson Orin, RTX, etc.) y cuántas cámaras por GPU? · **Rec:** Jetson Orin para sitios chicos, RTX para muchos streams; dimensionar por cámaras.
- **E3.** ¿Cómo es la **red del sitio** (cámaras en qué VLAN, dónde vive el broker MQTT, salida a internet)? · **Rec:** relevar por planta.

### F · Tenancy, aprovisionamiento y actualización
- **F1.** ¿Cómo se **aprovisiona un tenant nuevo** (edge + nube)? ¿**zero-touch** (el edge se registra con un token) o asistido? · **Rec:** el tenant/planta se crea en la nube (disparado por HEXA al activar el módulo); el edge se enrola con un token de un solo uso (zero-touch asistido).
- **F2.** ¿Un tenant puede tener **varias plantas/sitios** (varios edges)? ¿la config es por planta o por tenant? · **Rec:** sí, varios sitios por tenant; config por planta, catálogos por tenant.
- **F3.** ¿Cómo **autentica el edge** contra la nube (token por sitio, rotación, revocación)? · **Rec:** credencial por sitio, rotable y revocable desde la nube.
- **F4.** ¿La **config** (planta/cámaras/zonas/reglas) se edita en la nube y el edge la **baja (pull)**? ¿cada cuánto / push en cambios? · **Rec:** fuente de verdad en la nube; el edge la sincroniza (pull periódico + push en cambios).
- **F5.** ¿**Actualización del software** del edge: **OTA** (auto-update supervisada) o manual? · **Rec:** OTA con canary + rollback (como estaba en el roadmap Nexo).

### G · Contrato de eventos (payload) hacia HEXA
- **G1.** **Campos exactos** del evento canónico: `event_type`, `tenant/company_id`, `source {cámara/zona/señal/estación}`, `timestamp`, `severity`, `values`, `evidence_ref`, `dedup_key`. ¿Confirmamos? · **Rec:** sí; lo fijo en §4.3 del brief HEXA.
- **G2.** ¿**Idempotencia**: `dedup_key` para que HEXA no procese dos veces? · **Rec:** sí.
- **G3.** ¿HEXA **confirma recepción (ack)**? ¿reintentos hasta ack? · **Rec:** sí; reintentos con backoff hasta ack, luego dead-letter.
- **G4.** ¿El MES expone además una **API de consulta** (no solo webhook) para el tablero y el agente IA de HEXA? · **Rec:** sí (ya previsto: `event-gateway` + `mes-api`).

### H · Quién configura y con qué UI
- **H1.** ¿Quién configura planta/cámaras/zonas/objetos/acciones/reglas: el **cliente (self-service)** o **nosotros (implementadores)**? · **Rec:** implementador al onboarding + self-service acotado (el cliente ajusta reglas simples).
- **H2.** ¿**Editor visual de zonas** (dibujar polígonos sobre un frame de la cámara)? · **Rec:** sí, es casi imprescindible para zonas.
- **H3.** ¿Editor de reglas **no-code (visual)** o **DSL** para técnicos? · **Rec:** ambos sobre el mismo modelo (visual para todos, DSL para casos avanzados) — pero **arrancar con uno**: ¿cuál primero?

### I · Stack (confirmar)
- **I1.** **edge-vision**: Python + ¿**PyTorch / ONNX Runtime / DeepStream / Triton**? · **Rec:** Python + ONNX Runtime/TensorRT para inferencia; DeepStream si son muchos streams.
- **I2.** **edge-signals**: ¿**.NET o Python**? (cliente MQTT + adapters) · **Rec:** .NET (reusa BuildingBlocks y el patrón de eventos) — a confirmar según comodidad del equipo.
- **I3.** **nube**: **.NET** para `mes-api`/`rules-engine`/`event-gateway`. ¿confirmado? · **Rec:** sí.

### J · MVP / alcance / secuencia
- **J1.** ¿Cuál es el **primer entregable demostrable**? · **Rec:** tajada vertical fina — **1 cámara → detecta 1 objeto → 1 regla simple → 1 evento en HEXA** — antes de abrir los 4 casos.
- **J2.** ¿**Cuál de los 4 casos** se ataca primero (conteo / calidad / seguridad / paradas)? · **Rec:** el más simple de detectar con modelo genérico (suele ser **seguridad/EPP** o **conteo**), para validar la tubería.
- **J3.** ¿Hay **cliente/planta piloto real** ya identificado? (define cámaras, red, MQTT, casos reales).

---

## Próximo paso sugerido
Con C (esquema de regla) y G (payload de evento) cerrados, puedo **proponer el esquema de datos de V-A** (config: planta/cámaras/zonas/objetos/acciones/señales/reglas) + el **contrato de evento** en JSON, para revisarlo antes de codear.
