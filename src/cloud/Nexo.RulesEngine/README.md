# Nexo.RulesEngine (cloud · .NET) — WIP

Consume observaciones (`obs.vision.*`, `obs.signal.*`), evalúa las **reglas** (fuente × objeto/acción/señal × condición espacio-temporal) y emite **Eventos canónicos** (`evt.*`).

- Reusa `BuildingBlocks`.
- **Mismo binario desplegable en el edge** para reglas de baja latencia (p. ej. trabar una máquina en <1 s); en la nube para reglas que cruzan fuentes/sitios.
