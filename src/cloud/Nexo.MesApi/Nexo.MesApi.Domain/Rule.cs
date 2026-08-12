using Nexo.BuildingBlocks.Domain;

namespace Nexo.MesApi.Domain;

/// <summary>
/// Regla: árbol de disparo (observaciones × condición espacio-temporal) → emitir un evento, con cooldown.
/// El árbol (<see cref="Trigger"/>) y el <see cref="Emit"/> se guardan como JSON — la gramática y el
/// payload viven en docs/design/rules-and-events.md; el rules-engine los interpreta.
/// </summary>
public sealed class Rule : Entity<Guid>
{
    private Rule() { }

    public Rule(Guid id, string code, string name, string trigger, string emit)
        : base(id)
    {
        Code = code;
        Name = name;
        Trigger = trigger;
        Emit = emit;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool Enabled { get; set; } = true;

    /// <summary>Acota dónde aplica (null = todo el tenant).</summary>
    public Guid? ScopeLocationNodeId { get; set; }

    /// <summary>JSON: árbol de nodos del disparador (match/signal/and/or/not/sustained/sequence/count).</summary>
    public string Trigger { get; set; } = "{}";

    /// <summary>JSON: event_type / severity / payload (plantilla {{obs.*}}) / evidence.</summary>
    public string Emit { get; set; } = "{}";

    public int CooldownSeconds { get; set; }
}
