using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Nexo.BuildingBlocks.Application;
using Nexo.BuildingBlocks.Domain;
using Nexo.BuildingBlocks.Messaging;
using Nexo.BuildingBlocks.MultiTenancy;
using Nexo.MasterData.Application;
using Nexo.MasterData.Domain;
using Nexo.MasterData.Infrastructure.Configurations;

namespace Nexo.MasterData.Infrastructure;

/// <summary>
/// EF Core DbContext for the Master Data slice. Also acts as the Application persistence port
/// (<see cref="IMasterDataDbContext"/>) and the <see cref="IUnitOfWork"/>. On save it converts
/// aggregate domain events into <see cref="OutboxMessage"/> rows (Transactional Outbox), stamping
/// the canonical event <c>Type</c> and the current tenant id.
/// </summary>
public sealed class MasterDataDbContext : DbContext, IMasterDataDbContext, IUnitOfWork
{
    public const string DomainSchema = "master";
    public const string PlatformSchema = "platform";

    private const string ItemsTable = DomainSchema + ".items";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly ITenantContext _tenantContext;

    public MasterDataDbContext(DbContextOptions<MasterDataDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantContext = tenantContext;
    }

    public DbSet<Uom> Uoms => Set<Uom>();

    public DbSet<Item> Items => Set<Item>();

    public DbSet<Person> People => Set<Person>();

    public DbSet<Customer> Customers => Set<Customer>();

    public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

    // --- Units of measure -------------------------------------------------------------------

    public Task<Uom?> FindUomAsync(Guid uomId, CancellationToken cancellationToken = default)
        => Uoms.FirstOrDefaultAsync(uom => uom.Id == uomId, cancellationToken);

    public Task<Uom?> FindUomByCodeAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = (code ?? string.Empty).Trim();

        return Uoms.FirstOrDefaultAsync(uom => uom.Code == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<Uom>> ListUomsAsync(
        UomMagnitude? magnitude,
        MasterStatus? status,
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        var query = Uoms.AsNoTracking().AsQueryable();

        if (magnitude.HasValue)
        {
            query = query.Where(uom => uom.Magnitude == magnitude.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(uom => uom.Status == status.Value);
        }

        return await query
            .OrderBy(uom => uom.Code)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    // --- Items ------------------------------------------------------------------------------

    public Task<Item?> FindItemAsync(Guid itemId, CancellationToken cancellationToken = default)
        => Items.FirstOrDefaultAsync(item => item.Id == itemId, cancellationToken);

    public Task<bool> ItemCodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = (code ?? string.Empty).Trim();

        return Items.AnyAsync(item => item.Code == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<Item>> ListItemsAsync(
        ItemRole? role,
        MasterStatus? status,
        string? search,
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        // The role filter runs as `<role> = ANY(roles)` over the text[] column (backed by the GIN
        // index ix_items_roles). It is expressed in SQL because `Roles` is a value-converted
        // collection, which LINQ cannot translate into array containment on its own. The soft-delete
        // query filter and the paging below still compose on top of this source.
        var query = role.HasValue
            ? Items.FromSqlRaw($"SELECT * FROM {ItemsTable} WHERE {{0}} = ANY(roles)", ItemRoleDbValues.ToDbValue(role.Value))
                .AsNoTracking()
            : Items.AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(item => item.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.Code, pattern) || EF.Functions.ILike(item.Name, pattern));
        }

        return await query
            .OrderBy(item => item.Code)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    // --- People -----------------------------------------------------------------------------

    public Task<Person?> FindPersonAsync(Guid personId, CancellationToken cancellationToken = default)
        => People.FirstOrDefaultAsync(person => person.Id == personId, cancellationToken);

    public Task<bool> PersonCodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = (code ?? string.Empty).Trim();

        return People.AnyAsync(person => person.Code == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> ListPeopleAsync(
        MasterStatus? status,
        string? search,
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        var query = People.AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(person => person.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(person =>
                EF.Functions.ILike(person.Code, pattern) || EF.Functions.ILike(person.FullName, pattern));
        }

        return await query
            .OrderBy(person => person.Code)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    // --- Customers --------------------------------------------------------------------------

    public Task<Customer?> FindCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
        => Customers.FirstOrDefaultAsync(customer => customer.Id == customerId, cancellationToken);

    public Task<bool> CustomerCodeExistsAsync(string code, CancellationToken cancellationToken = default)
    {
        var normalized = (code ?? string.Empty).Trim();

        return Customers.AnyAsync(customer => customer.Code == normalized, cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> ListCustomersAsync(
        MasterStatus? status,
        string? search,
        int limit,
        int offset,
        CancellationToken cancellationToken = default)
    {
        var query = Customers.AsNoTracking().AsQueryable();

        if (status.HasValue)
        {
            query = query.Where(customer => customer.Status == status.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(customer =>
                EF.Functions.ILike(customer.Code, pattern) || EF.Functions.ILike(customer.LegalName, pattern));
        }

        return await query
            .OrderBy(customer => customer.Code)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    // --- Writes -----------------------------------------------------------------------------

    public void AddUom(Uom uom) => Uoms.Add(uom);

    public void AddItem(Item item) => Items.Add(item);

    public void AddPerson(Person person) => People.Add(person);

    public void AddCustomer(Customer customer) => Customers.Add(customer);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(DomainSchema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MasterDataDbContext).Assembly);
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
                var integrationEvent = MasterDataIntegrationEventMapper.Map(domainEvent);
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
