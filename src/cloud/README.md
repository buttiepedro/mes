# Plano NUBE del MES (.NET)

Se despliega **una sola vez**, es **multi-tenant**, y se comunica con **HEXA**. Recibe las observaciones de todos los edges (HTTPS → ingesta → Kafka **interna**), corre el motor de reglas y entrega los eventos a HEXA.

- **`Nexo.MesApi`** — config (planta/cámaras/dispositivos/catálogos/reglas) + tablero embebible + BFF + **ingesta HTTPS** de observaciones. Valida el JWT de HEXA (IdP). *(Semilla: `src/Services/Nexo.EventEngine`, se migra acá.)*
- **`Nexo.RulesEngine`** — observaciones × reglas → **Eventos canónicos**.
- **`Nexo.EventGateway`** — eventos → HEXA (webhooks firmados + API de lectura).

Bus: **Kafka interno** (`obs.*`, `evt.*`). DB: **Neon por tenant**. Storage: **S3/MinIO** (evidencia).
Compose: [`docker-compose.cloud.yml`](../../docker-compose.cloud.yml).
