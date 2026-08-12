using Nexo.BuildingBlocks.Domain;

namespace Nexo.MesApi.Domain;

/// <summary>Fuente de visión, ubicada en un nodo de la planta.</summary>
public sealed class Camera : Entity<Guid>
{
    private Camera() { }

    public Camera(Guid id, Guid locationNodeId, string code, string name, string streamUrl, CameraTransport transport)
        : base(id)
    {
        LocationNodeId = locationNodeId;
        Code = code;
        Name = name;
        StreamUrl = streamUrl;
        Transport = transport;
    }

    public Guid LocationNodeId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string StreamUrl { get; private set; } = string.Empty;
    public CameraTransport Transport { get; private set; }
    public int Fps { get; set; } = 10;
    public string? Resolution { get; set; }
    public CameraStatus Status { get; set; } = CameraStatus.Active;

    /// <summary>JSON: ids de cámaras vecinas — preparado para cross-cámara futuro (§8 rules-and-events).</summary>
    public string? AdjacentCameras { get; set; }
}

/// <summary>Zona (región de interés poligonal) dentro del campo de una cámara.</summary>
public sealed class Zone : Entity<Guid>
{
    private Zone() { }

    public Zone(Guid id, Guid cameraId, string code, string name, string polygon)
        : base(id)
    {
        CameraId = cameraId;
        Code = code;
        Name = name;
        Polygon = polygon;
    }

    public Guid CameraId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;

    /// <summary>JSON: lista de puntos [x,y] normalizados 0..1 (independiente de la resolución).</summary>
    public string Polygon { get; private set; } = "[]";

    public string? Purpose { get; set; }
}
