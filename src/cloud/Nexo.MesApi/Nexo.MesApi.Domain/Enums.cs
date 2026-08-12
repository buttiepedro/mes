namespace Nexo.MesApi.Domain;

/// <summary>Niveles de la jerarquía física (V-A). Un solo árbol, coherente con el nivel del padre.</summary>
public enum LocationLevel { Site, Area, Line, Station }

public enum CameraTransport { Rtsp, Usb }

public enum CameraStatus { Active, Inactive }

/// <summary>Protocolo de la fuente de señal. En el MVP: MQTT (S7/OPC-UA/Modbus después).</summary>
public enum SignalProtocol { Mqtt, OpcUa, Modbus, S7 }

public enum SignalValueType { Number, Bool, String }

/// <summary>Qué se persiste de una señal (D10, configurable por señal).</summary>
public enum SignalPersistence { EventsOnly, Timeseries }

public enum DetectionKind { Object, Action }

/// <summary>Alcance del catálogo: clase base compartida vs custom del tenant (D7).</summary>
public enum DetectionScope { Shared, Tenant }

public enum VisionModelKind { ObjectDetection, ActionRecognition, Pose }
