using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.BuildingBlocks.Messaging;
using Nexo.BuildingBlocks.MultiTenancy;
using Nexo.Execution.Application;
using Nexo.Execution.Domain;

namespace Nexo.Execution.Infrastructure;

/// <summary>
/// EF Core DbContext for the Execution slice. Also acts as the Application persistence port
/// (<see cref="IExecutionDbContext"/>) and the <see cref="IUnitOfWork"/>. On save it converts aggregate
/// domain events into <see cref="OutboxMessage"/> rows (Transactional Outbox), stamping the canonical
/// event <c>Type</c> and the current tenant id.
/// </summary>
/// <remarks>
/// The execution reads (<see cref="FindExecutionAsync"/>, <see cref="FindExecutionByTaskRunAsync"/>) bring
/// the <b>whole graph</b> — task runs with their frozen precedences, input consumptions and evidence —
/// because the aggregate cannot enforce the DAG and the close checklist over a graph it can only half see.
/// The outbox lives in this service's own <c>execution</c> schema (<c>execution.outbox_messages</c>), not
/// in a shared <c>platform</c> one, so migration ownership never collides with the other services of the
/// tenant.
/// </remarks>
public sealed class ExecutionDbContext : DbContext, IExecutionDbContext, IUnitOfWork
{
    public const string DomainSchema = "execution";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ITenantContext _tenantContext;

    public ExecutionDbContext(DbContextOptions<ExecutionDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Domain.Execution> Executions => Set<Domain.Execution>();

    public DbSet<TaskRun> TaskRuns => Set<TaskRun>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    // --- Reads --------------------------------------------------------------------------------

    public Task<Domain.Execution?> FindExecutionAsync(Guid executionId, CancellationToken cancellationToken = default)
        => ExecutionsWithGraph().FirstOrDefaultAsync(execution => execution.Id == executionId, cancellationToken);

    public Task<Domain.Execution?> FindExecutionByTaskRunAsync(Guid taskRunId, CancellationToken cancellationToken = default)
        => ExecutionsWithGraph().FirstOrDefaultAsync(
            execution => execution.TaskRuns.Any(run => run.Id == taskRunId),
            cancellationToken);

    public Task<bool> ExecutionCodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = (code ?? string.Empty).Trim();

        return Executions.AnyAsync(execution => execution.Code == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<Domain.Execution>> ListExecutionsAsync(
        ExecutionFlavor? flavor,
        ExecutionStatus? status,
        Guid? processId,
        DateTimeOffset? dueBefore,
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        // The header list carries WorkedTimeSeconds, which the aggregate sums from its task runs, so the
        // run graph is included even though the consumptions/evidence are not.
        var query = Executions
            .AsNoTracking()
            .Include(execution => execution.TaskRuns)
            .AsQueryable();

        if (flavor.HasValue)
        {
            query = query.Where(execution => execution.Flavor == flavor.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(execution => execution.Status == status.Value);
        }

        if (processId.HasValue)
        {
            query = query.Where(execution => execution.ProcessId == processId.Value);
        }

        if (dueBefore.HasValue)
        {
            query = query.Where(execution =>
                execution.CommittedDate != null && execution.CommittedDate <= dueBefore.Value);
        }

        return await query
            .OrderByDescending(execution => execution.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<TaskRunImputationRow>> ListPendingImputationAsync(
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        // The imputation backlog (E24): task runs whose work is done (or in progress) but not yet imputed
        // to a person. Nothing is ever discarded — the orphan fact waits in this tray.
        var query =
            from run in TaskRuns.AsNoTracking()
            join execution in Executions.AsNoTracking() on run.ExecutionId equals execution.Id
            where run.AssignedPersonId == null
                && (run.Status == TaskRunStatus.Completed || run.Status == TaskRunStatus.InProgress)
            orderby run.ActualEndAt descending
            select new
            {
                run.Id,
                run.ExecutionId,
                ExecutionCode = execution.Code,
                execution.Flavor,
                run.Name,
                run.Status,
                run.AssignedRoleId,
                WorkedTimeSeconds = run.ActualSetupSeconds + run.ActualExecSeconds + run.ActualWaitSeconds
                    + run.ActualControlSeconds + run.ActualClosingSeconds,
                run.ActualEndAt
            };

        var rows = await query.Skip(offset).Take(limit).ToListAsync(cancellationToken);

        return rows
            .Select(row => new TaskRunImputationRow(
                row.Id,
                row.ExecutionId,
                row.ExecutionCode,
                row.Flavor.ToWireValue(),
                row.Name,
                row.Status.ToWireValue(),
                row.AssignedRoleId,
                row.WorkedTimeSeconds,
                row.ActualEndAt))
            .ToList();
    }

    // --- Writes -------------------------------------------------------------------------------

    public void AddExecution(Domain.Execution execution) => Executions.Add(execution);

    /// <summary>Loads an execution together with its complete graph: task runs (their owned precedences
    /// travel automatically), input consumptions and evidence.</summary>
    private IQueryable<Domain.Execution> ExecutionsWithGraph()
        => Executions
            .Include(execution => execution.TaskRuns)
            .Include(execution => execution.InputConsumptions)
            .Include(execution => execution.Evidence);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DomainSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ExecutionDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public override int SaveChanges()
    {
        ConvertDomainEventsToOutbox();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ConvertDomainEventsToOutbox();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ConvertDomainEventsToOutbox();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ConvertDomainEventsToOutbox();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ConvertDomainEventsToOutbox()
    {
        var aggregates = ChangeTracker
            .Entries<AggregateRoot<Guid>>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0)
            .ToList();

        var tenantId = _tenantContext.IsResolved ? _tenantContext.TenantId : Guid.Empty;

        foreach (var aggregate in aggregates)
        {
            foreach (var domainEvent in aggregate.DomainEvents)
            {
                var integrationEvent = ExecutionIntegrationEventMapper.Map(domainEvent);
                if (integrationEvent is null)
                {
                    continue;
                }

                OutboxMessages.Add(new OutboxMessage
                {
                    Id = integrationEvent.EventId,
                    TenantId = tenantId,
                    Type = integrationEvent.Type,
                    Payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType(), SerializerOptions),
                    OccurredOn = integrationEvent.OccurredOn
                });
            }

            aggregate.ClearDomainEvents();
        }
    }
}
