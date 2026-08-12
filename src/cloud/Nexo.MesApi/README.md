# Nexo.MesApi (cloud · .NET) — WIP

Config del dominio (**planta, cámaras, dispositivos de señal, catálogos de objetos/acciones/señales, reglas**) + **tablero** embebible + BFF + **ingesta HTTPS de observaciones del edge** → Kafka. Valida el **JWT de HEXA** (HEXA es el IdP — ver `BuildingBlocks.Web/HexaAuthentication.cs`).

- **Semilla:** `src/Services/Nexo.EventEngine` (consumer Kafka + tablero) se migra/renombra acá.
- **Entradas:** HTTPS del edge (observaciones, auth por tenant) · UI/HEXA (JWT usuario).
- **Salidas:** Kafka `obs.vision.*` / `obs.signal.*`.
