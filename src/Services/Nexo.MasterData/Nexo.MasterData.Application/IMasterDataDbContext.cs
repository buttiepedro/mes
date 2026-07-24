using Nexo.MasterData.Domain;

namespace Nexo.MasterData.Application;

/// <summary>
/// Persistence port for the Master Data slice (implemented by <c>MasterDataDbContext</c> in Infrastructure).
/// </summary>
/// <remarks>
/// The port is intentionally EF-free so the Application layer does not depend on Entity Framework
/// (Clean Architecture; the Application csproj references only MediatR + FluentValidation, mirroring
/// <c>IProductionDbContext</c>). Filtering and paging are therefore expressed as explicit port
/// operations instead of leaking <c>IQueryable</c>. Every read excludes soft-deleted rows.
/// </remarks>
public interface IMasterDataDbContext
{
    // --- Units of measure -------------------------------------------------------------------

    Task<Uom?> FindUomAsync(Guid uomId, CancellationToken cancellationToken = default);

    Task<Uom?> FindUomByCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Uom>> ListUomsAsync(
        UomMagnitude? magnitude,
        MasterStatus? status,
        int limit,
        int offset,
        CancellationToken cancellationToken = default);

    // --- Items ------------------------------------------------------------------------------

    Task<Item?> FindItemAsync(Guid itemId, CancellationToken cancellationToken = default);

    Task<bool> ItemCodeExistsAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Item>> ListItemsAsync(
        ItemRole? role,
        MasterStatus? status,
        string? search,
        int limit,
        int offset,
        CancellationToken cancellationToken = default);

    // --- People -----------------------------------------------------------------------------

    Task<Person?> FindPersonAsync(Guid personId, CancellationToken cancellationToken = default);

    Task<bool> PersonCodeExistsAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Person>> ListPeopleAsync(
        MasterStatus? status,
        string? search,
        int limit,
        int offset,
        CancellationToken cancellationToken = default);

    // --- Customers --------------------------------------------------------------------------

    Task<Customer?> FindCustomerAsync(Guid customerId, CancellationToken cancellationToken = default);

    Task<bool> CustomerCodeExistsAsync(string code, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> ListCustomersAsync(
        MasterStatus? status,
        string? search,
        int limit,
        int offset,
        CancellationToken cancellationToken = default);

    // --- Writes -----------------------------------------------------------------------------

    void AddUom(Uom uom);

    void AddItem(Item item);

    void AddPerson(Person person);

    void AddCustomer(Customer customer);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
