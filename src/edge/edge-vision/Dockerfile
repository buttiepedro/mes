# edge-vision — slice con StubDetector (stdlib). El detector real agregaría OpenCV/ONNX (ver requirements.txt).
FROM python:3.12-slim
WORKDIR /app
COPY main.py .
# Detector real:
# COPY requirements.txt .
# RUN pip install --no-cache-dir -r requirements.txt
CMD ["python", "main.py"]
