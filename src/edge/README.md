# Plano EDGE del MES (por tenant/planta)

Se despliega **una vez por sitio**, en la planta, cerca de cámaras y PLCs. Captura y manda **observaciones livianas** por **HTTPS** a la nube (autenticado como su tenant). **No** manda video crudo, **no** corre Kafka (buffer local + **store-and-forward** ante cortes). **No** habla con HEXA directo — habla con la nube.

- **`edge-vision`** (Python/GPU) — captura (RTSP/USB) + inferencia (objetos + acciones) → **detecciones**.
- **`edge-signals`** (.NET/Python) — adapters S7/OPC-UA/Modbus/MQTT/datalogger → **lecturas de señal**.

Compose: [`docker-compose.edge.yml`](../../docker-compose.edge.yml).
