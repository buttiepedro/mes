using Nexo.Production.Domain;

namespace Nexo.Production.Application;

/// <summary>
/// Persistence port for the Production slice (implemented by <c>ProductionDbContext</c> in Infrastructure).
/// </summary>
/// <remarks>
/// The port is intentionally EF-free so the Application layer does not depend on Entity Framework
/// (Clean Architecture; the Application csproj references only MediatR + FluentValidation per the
/// scaffold contract §1). The EF <c>DbSet&lt;T&gt;</c> live on the concrete <c>ProductionDbContext</c>,
/// which also implements this abstraction and <c>IUnitOfWork</c>.
/// </remarks>
public interface IProductionDbContext
{
    Task<WorkOrder?> FindWorkOrderAsync(Guid workOrderId, CancellationToken cancellationToken = default);

    Task<ProductionRun?> FindRunAsync(Guid runId, CancellationToken cancellationToken = default);

    void AddWorkOrder(WorkOrder workOrder);

    void AddRun(ProductionRun run);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
