using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.BuildingBlocks.Messaging;
using Nexo.BuildingBlocks.MultiTenancy;
using Nexo.WorkModel.Application;
using Nexo.WorkModel.Domain;

namespace Nexo.WorkModel.Infrastructure;

/// <summary>
/// EF Core DbContext for the Work Model slice. Also acts as the Application persistence port
/// (<see cref="IWorkModelDbContext"/>) and the <see cref="IUnitOfWork"/>. On save it converts
/// aggregate domain events into <see cref="OutboxMessage"/> rows (Transactional Outbox), stamping
/// the canonical event <c>Type</c> and the current tenant id.
/// </summary>
/// <remarks>
/// The version reads (<see cref="FindVersionAsync"/>, <see cref="FindPublishedVersionAsync"/>,
/// <see cref="FindLatestVersionAsync"/>) bring the <b>whole graph</b> — tasks, their inputs and the
/// precedences — because the aggregate cannot validate a DAG it can only half see. The outbox lives in
/// this service's own <c>work</c> schema (<c>work.outbox_messages</c>), not in a shared
/// <c>platform</c> one, so migration ownership never collides with the other services of the tenant.
/// </remarks>
public sealed class WorkModelDbContext : DbContext, IWorkModelDbContext, IUnitOfWork
{
    public const string DomainSchema = "work";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ITenantContext _tenantContext;

    public WorkModelDbContext(DbContextOptions<WorkModelDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Process> Processes => Set<Process>();

    public DbSet<ProcessVersion> ProcessVersions => Set<ProcessVersion>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    // --- Processes ----------------------------------------------------------------------------

    public Task<Process?> FindProcessAsync(Guid processId, CancellationToken cancellationToken = default)
        => Processes.FirstOrDefaultAsync(process => process.Id == processId, cancellationToken);

    public Task<bool> ProcessCodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = (code ?? string.Empty).Trim();

        return Processes.AnyAsync(process => process.Code == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<Process>> ListProcessesAsync(
        ProcessProfile? profile,
        ProcessStatus? status,
        string? search,
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        var query = Processes.AsNoTracking().AsQueryable();

        if (profile.HasValue)
        {
            query = query.Where(process => process.Profile == profile.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(process => process.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(process =>
                EF.Functions.ILike(process.Code, pattern) || EF.Functions.ILike(process.Name, pattern));
        }

        return await query
            .OrderBy(process => process.Code)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    // --- Versions -----------------------------------------------------------------------------

    public Task<ProcessVersion?> FindVersionAsync(Guid versionId, CancellationToken cancellationToken = default)
        => VersionsWithGraph().FirstOrDefaultAsync(version => version.Id == versionId, cancellationToken);

    public Task<ProcessVersion?> FindPublishedVersionAsync(Guid processId, CancellationToken cancellationToken = default)
        => VersionsWithGraph().FirstOrDefaultAsync(
            version => version.ProcessId == processId && version.State == ProcessVersionState.Published,
            cancellationToken);

    public Task<ProcessVersion?> FindLatestVersionAsync(Guid processId, CancellationToken cancellationToken = default)
        => VersionsWithGraph()
            .Where(version => version.ProcessId == processId)
            .OrderByDescending(version => version.VersionMajor)
            .ThenByDescending(version => version.VersionMinor)
            .ThenByDescending(version => version.VersionPatch)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<ProcessVersion>> ListVersionsAsync(
        Guid processId,
        CancellationToken cancellationToken = default)
        => await ProcessVersions
            .AsNoTracking()
            .Where(version => version.ProcessId == processId)
            .OrderByDescending(version => version.VersionMajor)
            .ThenByDescending(version => version.VersionMinor)
            .ThenByDescending(version => version.VersionPatch)
            .ToListAsync(cancellationToken);

    public Task<bool> VersionNumberExistsAsync(
        Guid processId,
        string versionNo,
        CancellationToken cancellationToken = default)
    {
        var normalized = (versionNo ?? string.Empty).Trim();

        return ProcessVersions.AnyAsync(
            version => version.ProcessId == processId && version.VersionNo == normalized,
            cancellationToken);
    }

    // --- Writes -------------------------------------------------------------------------------

    public void AddProcess(Process process) => Processes.Add(process);

    public void AddVersion(ProcessVersion version) => ProcessVersions.Add(version);

    /// <summary>Loads a version together with its complete graph: tasks, task inputs and precedences.</summary>
    private IQueryable<ProcessVersion> VersionsWithGraph()
        => ProcessVersions
            .Include(version => version.Tasks)
            .ThenInclude(task => task.Inputs)
            .Include(version => version.Dependencies);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DomainSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkModelDbContext).Assembly);
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
                var integrationEvent = WorkModelIntegrationEventMapper.Map(domainEvent);
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
