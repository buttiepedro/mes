# edge-signals (.NET o Python) — WIP

Lee señales industriales por **protocolo** vía **adapters** (plugins): **S7, OPC-UA, Modbus, MQTT, datalogger/CSV**. Normaliza a **lecturas de señal** y las manda por **HTTPS a la nube** (buffer local ante cortes). 1 contenedor por sitio; un protocolo con lib pesada puede aislarse como adapter en su **propio proceso**, pero sigue siendo "el gateway".

- Salida: **HTTPS → nube** (`mes-api` ingesta). Buffer local (`EDGE_BUFFER_DIR`) para cortes.
