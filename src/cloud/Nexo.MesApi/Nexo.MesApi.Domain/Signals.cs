using Nexo.BuildingBlocks.Domain;

namespace Nexo.MesApi.Domain;

/// <summary>Fuente industrial (MQTT en el MVP), ubicada en un nodo de la planta.</summary>
public sealed class SignalDevice : Entity<Guid>
{
    private SignalDevice() { }

    public SignalDevice(Guid id, Guid locationNodeId, string code, string name, SignalProtocol protocol)
        : base(id)
    {
        LocationNodeId = locationNodeId;
        Code = code;
        Name = name;
        Protocol = protocol;
    }

    public Guid LocationNodeId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public SignalProtocol Protocol { get; private set; }

    /// <summary>JSON: broker, tópico base, referencia al secreto (nunca credenciales inline).</summary>
    public string? Config { get; set; }
}

/// <summary>Tag/variable que expone un dispositivo de señal.</summary>
public sealed class Signal : Entity<Guid>
{
    private Signal() { }

    public Signal(Guid id, Guid deviceId, string code, string name, string mqttTopic, SignalValueType valueType)
        : base(id)
    {
        DeviceId = deviceId;
        Code = code;
        Name = name;
        MqttTopic = mqttTopic;
        ValueType = valueType;
    }

    public Guid DeviceId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string MqttTopic { get; private set; } = string.Empty;

    /// <summary>Si el payload es JSON, el path del valor (null = payload directo).</summary>
    public string? JsonPath { get; set; }

    public SignalValueType ValueType { get; private set; }
    public string? Unit { get; set; }
    public SignalPersistence Persistence { get; set; } = SignalPersistence.EventsOnly;
}
