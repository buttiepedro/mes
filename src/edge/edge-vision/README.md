# edge-vision (Python + GPU) — WIP

Captura RTSP/USB **y** corre inferencia (**detección de objetos** + **reconocimiento de acciones**) en GPU local, emitiendo **detecciones** (bbox / clase / score / timestamp + snapshot de evidencia a storage), **nunca frames crudos**. Captura y análisis son **módulos internos** (comparten frames en memoria) — escalás agregando instancias, 1 por cámara/grupo.

- Stack sugerido: OpenCV + PyTorch / ONNX Runtime (o DeepStream).
- Salida: **HTTPS → nube** (`mes-api` ingesta). Buffer local (`EDGE_BUFFER_DIR`) para cortes.
