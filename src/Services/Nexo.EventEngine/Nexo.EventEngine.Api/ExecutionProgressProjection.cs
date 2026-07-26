using System.Collections.Concurrent;
using System.Text.Json;
using Nexo.BuildingBlocks.Messaging;

namespace Nexo.EventEngine.Api;

/// <summary>Read model of one execution's progress, derived from its events.</summary>
public sealed record ExecutionProgressDto(
    Guid ExecutionId,
    string Code,
    string Flavor,
    string Status,
    int TotalTasks,
    int CompletedTasks,
    int StartedTasks,
    decimal ProgressPct,
    DateTimeOffset? LastEventAt);

/// <summary>
/// The Capa 4 (event engine) minimal projection: an <b>in-memory</b> read model of execution progress
/// rebuilt from the <c>nexo.execution.*</c> / <c>nexo.task.*</c> event stream. Progress is derived from
/// facts (completed/skipped task-runs over the total frozen in the run), never estimated.
/// </summary>
/// <remarks>
/// In-memory on purpose (M2 minimal): the consumer replays the Kafka log from the beginning on every
/// start, so the projection reconstructs itself. Persisting it to a read-model table is deferred (M4/M5).
/// The projection is tolerant of cross-topic reordering: a task event for a still-unknown execution
/// creates a placeholder whose total is filled in when <c>execution.created</c> arrives.
/// </remarks>
public sealed class ExecutionProgressProjection
{
    private readonly ConcurrentDictionary<Guid, State> _executions = new();

    /// <summary>Applies one event (already parsed as JSON) to the projection.</summary>
    public void Apply(string type, JsonElement evt)
    {
        if (!TryGuid(evt, "executionId", out var executionId))
        {
            return;
        }

        var state = _executions.GetOrAdd(executionId, id => new State(id));

        state.Mutate(s =>
        {
            s.LastEventAt = DateTimeOffset.UtcNow;

            switch (type)
            {
                case EventTypes.Execution_Created:
                    s.Code = GetString(evt, "code");
                    s.Flavor = GetString(evt, "flavor");
                    if (evt.TryGetProperty("taskRunCount", out var count) && count.TryGetInt32(out var total))
                    {
                        s.TotalTasks = total;
                    }
                    if (s.Status == "unknown")
                    {
                        s.Status = "created";
                    }
                    break;

                case EventTypes.Execution_Started:
                    s.Status = "started";
                    break;

                case EventTypes.Execution_Closed:
                    s.Status = "closed";
                    break;

                case EventTypes.Execution_Cancelled:
                    s.Status = "cancelled";
                    break;

                case EventTypes.Task_Completed:
                case EventTypes.Task_Skipped:
                    // Both resolve a task-run: it will not run again, so it counts as done for progress.
                    if (TryGuid(evt, "taskRunId", out var resolved))
                    {
                        s.Completed.Add(resolved);
                    }
                    break;

                case EventTypes.Task_Started:
                    if (TryGuid(evt, "taskRunId", out var started))
                    {
                        s.Started.Add(started);
                    }
                    break;
            }
        });
    }

    public IReadOnlyList<ExecutionProgressDto> All()
        => _executions.Values.Select(state => state.ToDto()).OrderBy(dto => dto.Code).ToList();

    public ExecutionProgressDto? Get(Guid executionId)
        => _executions.TryGetValue(executionId, out var state) ? state.ToDto() : null;

    private static bool TryGuid(JsonElement element, string property, out Guid value)
    {
        value = Guid.Empty;
        return element.TryGetProperty(property, out var prop)
            && prop.ValueKind == JsonValueKind.String
            && Guid.TryParse(prop.GetString(), out value);
    }

    private static string GetString(JsonElement element, string property)
        => element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString() ?? string.Empty
            : string.Empty;

    /// <summary>Mutable per-execution state, guarded by its own lock (consumer writes, requests read).</summary>
    private sealed class State
    {
        private readonly object _lock = new();

        public State(Guid executionId) => ExecutionId = executionId;

        public Guid ExecutionId { get; }
        public string Code { get; set; } = string.Empty;
        public string Flavor { get; set; } = string.Empty;
        public string Status { get; set; } = "unknown";
        public int TotalTasks { get; set; }
        public HashSet<Guid> Completed { get; } = new();
        public HashSet<Guid> Started { get; } = new();
        public DateTimeOffset? LastEventAt { get; set; }

        public void Mutate(Action<State> mutation)
        {
            lock (_lock)
            {
                mutation(this);
            }
        }

        public ExecutionProgressDto ToDto()
        {
            lock (_lock)
            {
                var progressPct = TotalTasks > 0
                    ? Math.Round((decimal)Completed.Count / TotalTasks * 100m, 1)
                    : 0m;

                return new ExecutionProgressDto(
                    ExecutionId, Code, Flavor, Status, TotalTasks, Completed.Count, Started.Count, progressPct, LastEventAt);
            }
        }
    }
}
