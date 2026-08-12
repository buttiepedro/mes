using Nexo.BuildingBlocks.Domain;

namespace Nexo.MesApi.Domain;

/// <summary>
/// Nodo de la jerarquía de planta (Planta→Sector→Línea→Estación) como un único árbol.
/// Cámaras y dispositivos de señal se cuelgan de cualquier nodo.
/// </summary>
public sealed class LocationNode : Entity<Guid>
{
    private LocationNode() { }

    public LocationNode(Guid id, Guid? parentId, LocationLevel level, string code, string name)
        : base(id)
    {
        ParentId = parentId;
        Level = level;
        Code = code;
        Name = name;
    }

    public Guid? ParentId { get; private set; }

    public LocationLevel Level { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public void Rename(string name) => Name = name;
}
