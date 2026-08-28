namespace Crm.Application.Abstractions;

public interface IUnitOfWork
{
    /// Persists pending changes and hands collected domain events to the outbox in the same transaction.
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
