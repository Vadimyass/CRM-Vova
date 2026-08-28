using Crm.Domain.Common;

namespace Crm.Application.Abstractions;

/// Domain events are handed over here inside the same transaction as the data change and are
/// dispatched by a background worker. Running processes inline with the request would let a slow
/// or failing process break the record being saved - and lose the event on a crash.
public interface IOutbox
{
    Task EnqueueAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<IDomainEvent>> DequeueBatchAsync(int maxCount, CancellationToken cancellationToken = default);
}
