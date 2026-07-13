using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.BuildingBlocks.Messaging;
using Nexo.BuildingBlocks.MultiTenancy;
using Nexo.Production.Application;
using Nexo.Production.Domain;

namespace Nexo.Production.Infrastructure;

/// <summary>
/// EF Core DbContext for the Production slice. Also acts as the Application persistence port
/// (<see cref="IProductionDbContext"/>) and the <see cref="IUnitOfWork"/>. On save it converts
/// aggregate domain events into <see cref="OutboxMessage"/> rows (Transactional Outbox), stamping
/// the canonical event <c>Type</c> and the current tenant id.
/// </summary>
public sealed class ProductionDbContext : DbContext, IProductionDbContext, IUnitOfWork
{
    public const string DomainSchema = "production";
    public const string PlatformSchema = "platform";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ITenantContext _tenantContext;

    public ProductionDbContext(DbContextOptions<ProductionDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<WorkOrder> WorkOrders => Set<WorkOrder>();

    public DbSet<ProductionRun> ProductionRuns => Set<ProductionRun>();

    public DbSet<ProductionRecord> ProductionRecords => Set<ProductionRecord>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    public Task<WorkOrder?> FindWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default)
        => WorkOrders.FirstOrDefaultAsync(w => w.Id == workOrderId, cancellationToken);

    public Task<ProductionRun?> FindRunAsync(Guid runId, CancellationToken cancellationToken = default)
        => ProductionRuns.Include(r => r.Records).FirstOrDefaultAsync(r => r.Id == runId, cancellationToken);

    public void AddWorkOrder(WorkOrder workOrder) => WorkOrders.Add(workOrder);

    public void AddRun(ProductionRun run) => ProductionRuns.Add(run);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DomainSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ProductionDbContext).Assembly);
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
                var integrationEvent = ProductionIntegrationEventMapper.Map(domainEvent);
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
