# Nexo — MES · motor de eventos de planta (visión + señales industriales)

**Nexo es el módulo de _generación de eventos + visión por computadora_ de [HEXA](https://github.com/buttiepedro/hexa)** (el ERP). Repo separado por su complejidad (tiempo real, edge, GPU), pero integrado para que sea, de cara al usuario, una parte más de HEXA.

## Qué hace

Nosotros definimos la **planta** y sus **fuentes de eventos**; el MES observa, aplica **reglas** y **emite eventos** que HEXA consume (trabar una orden, finalizar una producción, registrar una no-conformidad, alertar…).

```
Planta ─┬─ Cámara ──────────────────→ (Objeto, Acción) ─┐
        └─ Dispositivo de señal ─────→ Señal/Tag ────────┤─→ Regla → Evento → HEXA
           (PLC/datalogger vía OPC-UA/Modbus/MQTT/S7)     ┘
```

- **Configuración:** planta, cámaras, dispositivos de señal, catálogos de objetos/acciones/señales, y **reglas** (cámara × objeto × acción **y/o** señal × condición → evento).
- **Fuentes:** **visión** (cámaras → detección de objetos + reconocimiento de acciones) y **señales industriales** (PLC/datalogger por protocolo).
- **Motor de reglas** (agnóstico de fuente) → **Evento canónico** → **HEXA** (webhooks/API).

La frontera: HEXA es el **plan y el registro** (órdenes, artículos, producción, identidad, multi-tenant); el MES es **la realidad y los ojos**.

## Estado

> **En reestructuración hacia el nuevo modelo.** Tras el cambio de encuadre (Nexo = módulo de HEXA), se **retiró** el MES de procesos/órdenes (`MasterData`, `WorkModel`, `Execution`, `Production`) — eso es ahora la producción de HEXA.

**Sobrevive y sigue en el repo:**
- `src/BuildingBlocks/*` — plumbing compartido (.NET 8, CQRS, multi-tenancy, messaging, outbox, observabilidad, web).
- `src/Services/Nexo.EventEngine` — **backbone de eventos** (consumer Kafka + tablero) — se reorienta a cámaras/señales/eventos.
- **Seam de identidad con HEXA** (`BuildingBlocks.Web/HexaAuthentication.cs`): el MES valida el JWT de HEXA (HEXA es el IdP). ✅ verificado.

**Pendiente (por construir):** el dominio nuevo — **V-A** configuración (planta/cámaras/dispositivos/objetos/acciones/señales/reglas) · **V-B** fuentes (pipeline de visión + ingesta industrial) · **V-C** motor de reglas · **V-D** salida a HEXA · **V-E** tablero.

## Arquitectura y despliegue

Monorepo, **dos planos**, **dos compose**:

| Plano | Se despliega | Servicios | Compose |
|---|---|---|---|
| ☁️ **Nube** | **una vez**, multi-tenant, habla con HEXA | `Nexo.MesApi` (config+tablero+BFF+ingesta) · `Nexo.RulesEngine` · `Nexo.EventGateway` + infra | `docker-compose.cloud.yml` |
| 🏭 **Edge** | **una por tenant/planta** (en el sitio) | `edge-vision` (Python/GPU) · `edge-signals` (S7/OPC-UA/Modbus/MQTT/datalogger) | `docker-compose.edge.yml` |

`edge (por tenant) → HTTPS observaciones → nube (Kafka interna → reglas → eventos) → HEXA`. El edge bufferea local (store-and-forward) y **nunca** habla con HEXA directo.

```
src/BuildingBlocks/                (.NET compartido)
src/cloud/                         Nexo.MesApi · Nexo.RulesEngine · Nexo.EventGateway   (WIP)
src/edge/                          edge-vision (Python) · edge-signals                 (WIP)
src/Services/Nexo.EventEngine/     semilla de Nexo.MesApi (se migra a src/cloud/)
```

## Documentación

- **[docs/design/hexa-integration/README.md](docs/design/hexa-integration/README.md)** — plan de arquitectura e integración (los dos lados, frontera, contratos, decisiones). **Fuente de verdad.**
- **[HEXA-INTEGRATION.md](HEXA-INTEGRATION.md)** — brief autocontenido para el equipo de HEXA (qué construir del lado ERP).

## Correr local

```powershell
scripts\run-local.ps1 -NoBuild   # infra (Postgres/Redpanda/MinIO/Jaeger) + EventEngine
scripts\stop-local.ps1
```
Requisitos: .NET 8 SDK + Docker. Consolas: Redpanda http://localhost:8080 · MinIO http://localhost:9001 · Jaeger http://localhost:16686.
