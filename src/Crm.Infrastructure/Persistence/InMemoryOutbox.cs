using System.Collections.Concurrent;
using Crm.Application.Abstractions;
using Crm.Domain.Common;

namespace Crm.Infrastructure.Persistence;

public sealed class InMemoryOutbox : IOutbox
{
    private readonly ConcurrentQueue<IDomainEvent> _queue = new();

    public Task EnqueueAsync(IReadOnlyCollection<IDomainEvent> events, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in events)
        {
            _queue.Enqueue(domainEvent);
        }

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<IDomainEvent>> DequeueBatchAsync(int maxCount, CancellationToken cancellationToken = default)
    {
        var batch = new List<IDomainEvent>();

        while (batch.Count < maxCount && _queue.TryDequeue(out var domainEvent))
        {
            batch.Add(domainEvent);
        }

        return Task.FromResult<IReadOnlyList<IDomainEvent>>(batch);
    }
}
