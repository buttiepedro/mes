namespace Nexo.BuildingBlocks.Application;

/// <summary>Commits all pending changes made within the current scope as a single transaction.</summary>
public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
