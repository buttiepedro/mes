using Nexo.BuildingBlocks.Domain;

namespace Nexo.MesApi.Domain;

/// <summary>Clase reconocible por visión (objeto o acción). Base compartida o custom del tenant (D7).</summary>
public sealed class DetectionClass : Entity<Guid>
{
    private DetectionClass() { }

    public DetectionClass(Guid id, DetectionKind kind, string code, string name, DetectionScope scope)
        : base(id)
    {
        Kind = kind;
        Code = code;
        Name = name;
        Scope = scope;
    }

    public DetectionKind Kind { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public DetectionScope Scope { get; private set; }
}

/// <summary>Artefacto de inferencia versionado que corre en el edge y provee clases de detección.</summary>
public sealed class VisionModel : Entity<Guid>
{
    private VisionModel() { }

    public VisionModel(Guid id, VisionModelKind kind, string version, string artifactRef)
        : base(id)
    {
        Kind = kind;
        Version = version;
        ArtifactRef = artifactRef;
    }

    public VisionModelKind Kind { get; private set; }
    public string Version { get; private set; } = string.Empty;

    /// <summary>Ref al artefacto en storage/registry (ONNX/TensorRT).</summary>
    public string ArtifactRef { get; private set; } = string.Empty;

    /// <summary>JSON: códigos de DetectionClass que reconoce.</summary>
    public string? ProvidesClasses { get; set; }
}
