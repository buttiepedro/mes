#!/usr/bin/env python3
"""
edge-vision — Agente EDGE (por planta) de VISION por computadora.

Captura las camaras, corre inferencia (deteccion de objetos/acciones) y emite OBSERVACIONES a la nube
(RulesEngine /v1/observations), igual que edge-signals. No manda a HEXA directo. Hace pull de las
camaras/clases desde el config-bundle de Nexo.MesApi.

Detector PLUGGABLE (env DETECTOR):
  - stub : reproduce un escenario (probar el pipeline sin CV).
  - cv   : detector REAL de OpenCV (CV clasica: color+contornos) sobre un frame (imagen/video/RTSP).
El detector DL real (ONNX/torch) se enchufa implementando la misma interfaz: leer frame -> inferir ->
detecciones. La captura + el postproceso + el envio de observaciones son identicos.
"""
import json
import os
import time
import urllib.request

BUNDLE_URL = os.environ.get("BUNDLE_URL", "http://localhost:5085/v1/config-bundle")
OBSERVATIONS_URL = os.environ.get("OBSERVATIONS_URL", "http://localhost:5086/v1/observations")
DETECTOR = os.environ.get("DETECTOR", "stub")      # stub | cv
SOURCE = os.environ.get("SOURCE", "")               # imagen/video/RTSP para el detector cv
CAMERA = os.environ.get("CAMERA", "")               # code de la camara a la que se atribuyen las detecciones
SCENARIO_FILE = os.environ.get("SCENARIO_FILE", "")


def http_get(url):
    with urllib.request.urlopen(url, timeout=10) as r:
        return json.loads(r.read().decode("utf-8"))


def emit(obs):
    try:
        data = json.dumps(obs).encode("utf-8")
        req = urllib.request.Request(OBSERVATIONS_URL, data=data,
                                     headers={"Content-Type": "application/json"}, method="POST")
        with urllib.request.urlopen(req, timeout=10) as r:
            r.read()
        print(f"[edge-vision] obs cam={obs['camera_id']} {obs['kind']}:{obs['class']} score={obs['score']}", flush=True)
    except Exception as e:  # TODO: buffer store-and-forward
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
    """Reproduce un escenario de detecciones (sin CV)."""

    def __init__(self, scenario):
        self.scenario = scenario

    def run(self, cameras):
        for step in self.scenario:
            time.sleep(step.get("delay_ms", 800) / 1000.0)
            emit({
                "obs_type": "vision", "camera_id": step["camera"], "zones": step.get("zones", []),
                "kind": step.get("kind", "object"), "class": step["class"],
                "score": step.get("score", 0.9), "track_id": step.get("track_id", "t-stub"),
            })


class CvDetector:
    """
    Detector REAL de OpenCV (CV clasica): detecta regiones de 'defecto' por color (rojo) + contornos
    en un frame real. Lee de imagen / video / RTSP. El mismo flujo (frame -> deteccion -> observacion)
    aloja un modelo ONNX/DL como reemplazo.
    """

    def __init__(self, camera_code, source):
        self.camera_code = camera_code
        self.source = source

    def read_frame(self):
        import cv2
        if self.source and not self.source.lower().startswith(("rtsp", "http")):
            frame = cv2.imread(self.source)
            if frame is not None:
                return frame
        cap = cv2.VideoCapture(self.source if self.source else 0)
        ok, frame = cap.read()
        cap.release()
        return frame if ok else None

    def detect(self, frame):
        import cv2
        hsv = cv2.cvtColor(frame, cv2.COLOR_BGR2HSV)
        # El rojo cruza el 0 en HSV: dos rangos.
        mask = cv2.bitwise_or(
            cv2.inRange(hsv, (0, 120, 70), (10, 255, 255)),
            cv2.inRange(hsv, (170, 120, 70), (180, 255, 255)))
        contours, _ = cv2.findContours(mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        h, w = frame.shape[:2]
        detections = []
        for c in contours:
            area = cv2.contourArea(c)
            if area < 0.004 * w * h:      # ignora ruido chico
                continue
            x, y, bw, bh = cv2.boundingRect(c)
            score = round(min(0.99, 0.6 + area / (w * h)), 2)
            detections.append({"class": "defecto", "kind": "object", "score": score,
                               "bbox": [round(x / w, 3), round(y / h, 3), round(bw / w, 3), round(bh / h, 3)]})
        return detections

    def run(self, cameras):
        frame = self.read_frame()
        if frame is None:
            print(f"[edge-vision] CV: no se pudo leer el frame de '{self.source}'", flush=True)
            return
        detections = self.detect(frame)
        print(f"[edge-vision] CV: {len(detections)} deteccion(es) reales en '{self.source}' (cam {self.camera_code})", flush=True)
        for d in detections:
            emit({
                "obs_type": "vision", "camera_id": self.camera_code, "zones": [],
                "kind": d["kind"], "class": d["class"], "score": d["score"], "track_id": "cv-0",
            })


def load_scenario(camera_codes):
    if SCENARIO_FILE and os.path.exists(SCENARIO_FILE):
        with open(SCENARIO_FILE, encoding="utf-8") as f:
            return json.load(f)
    if camera_codes:
        return [{"camera": camera_codes[0], "kind": "object", "class": "defecto", "score": 0.85}]
    return []


def main():
    cameras, _classes = load_config()
    camera_codes = list(cameras)

    if DETECTOR == "cv":
        camera = CAMERA or (camera_codes[0] if camera_codes else "CAM-1")
        detector = CvDetector(camera, SOURCE)
    else:
        detector = StubDetector(load_scenario(camera_codes))

    detector.run(cameras)

    while True:       # un detector real corre en loop sobre los frames
        time.sleep(30)


if __name__ == "__main__":
    main()
