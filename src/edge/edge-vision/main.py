#!/usr/bin/env python3
"""
edge-vision — Agente EDGE (por planta) de VISION por computadora.

Captura las camaras (RTSP/USB), corre inferencia (deteccion de objetos + reconocimiento de acciones)
y emite OBSERVACIONES a la nube (RulesEngine /v1/observations), igual que edge-signals. No manda a HEXA
directo. Hace pull de las camaras/clases desde el config-bundle de Nexo.MesApi.

El detector es PLUGGABLE. Este slice trae un StubDetector (reproduce un escenario) para probar el
pipeline completo sin GPU/modelo; el detector real (OpenCV captura + ONNX/torch inferencia) se enchufa
implementando la misma interfaz Detector.run(cameras, emit).
"""
import json
import os
import time
import urllib.request

BUNDLE_URL = os.environ.get("BUNDLE_URL", "http://localhost:5085/v1/config-bundle")
OBSERVATIONS_URL = os.environ.get("OBSERVATIONS_URL", "http://localhost:5086/v1/observations")
SCENARIO_FILE = os.environ.get("SCENARIO_FILE", "")


def http_get(url):
    with urllib.request.urlopen(url, timeout=10) as r:
        return json.loads(r.read().decode("utf-8"))


def emit(obs):
    """Publica una observacion de vision a la nube."""
    try:
        data = json.dumps(obs).encode("utf-8")
        req = urllib.request.Request(OBSERVATIONS_URL, data=data,
                                     headers={"Content-Type": "application/json"}, method="POST")
        with urllib.request.urlopen(req, timeout=10) as r:
            r.read()
        print(f"[edge-vision] obs cam={obs['camera_id']} {obs['kind']}:{obs['class']} score={obs['score']}", flush=True)
    except Exception as e:  # TODO: buffer store-and-forward ante cortes
        print(f"[edge-vision] POST observacion fallo: {e}", flush=True)


def load_config():
    try:
        bundle = http_get(BUNDLE_URL)
        cameras = {c["code"]: c for c in bundle.get("cameras", [])}
        classes = [dc["code"] for dc in bundle.get("detectionClasses", [])]
        print(f"[edge-vision] config: {len(cameras)} camara(s) {list(cameras)}; clases={classes}", flush=True)
        return cameras, classes
    except Exception as e:
        print(f"[edge-vision] no se pudo cargar la config desde {BUNDLE_URL}: {e}", flush=True)
        return {}, []


class StubDetector:
    """Reproduce un escenario de detecciones (para probar sin GPU/modelo). Referencia camaras por CODE."""

    def __init__(self, scenario):
        self.scenario = scenario

    def run(self, cameras):
        for step in self.scenario:
            time.sleep(step.get("delay_ms", 800) / 1000.0)
            emit({
                "obs_type": "vision",
                "camera_id": step["camera"],          # observaciones y reglas referencian la camara por CODE
                "zones": step.get("zones", []),
                "kind": step.get("kind", "object"),   # object | action
                "class": step["class"],
                "score": step.get("score", 0.9),
                "track_id": step.get("track_id", "t-stub"),
            })


def load_scenario(camera_codes):
    if SCENARIO_FILE and os.path.exists(SCENARIO_FILE):
        with open(SCENARIO_FILE, encoding="utf-8") as f:
            return json.load(f)
    # Escenario demo: una deteccion de 'defecto' en la primera camara.
    if camera_codes:
        return [{"camera": camera_codes[0], "kind": "object", "class": "defecto", "score": 0.85}]
    return []


def main():
    cameras, _classes = load_config()
    scenario = load_scenario(list(cameras))
    if not scenario:
        print("[edge-vision] sin camaras/escenario; en espera (config pull cada 30s).", flush=True)

    detector = StubDetector(scenario)      # <- el detector real (OpenCV+ONNX) se enchufa aca
    detector.run(cameras)

    # Mantener vivo (un detector real corre en loop sobre los frames).
    while True:
        time.sleep(30)


if __name__ == "__main__":
    main()
