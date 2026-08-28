using Crm.Application.Abstractions;

namespace Crm.Infrastructure.Persistence;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly DomainEventCollector _collector;
    private readonly IOutbox _outbox;

    public UnitOfWork(DomainEventCollector collector, IOutbox outbox)
    {
        _collector = collector;
        _outbox = outbox;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var events = _collector.Drain();

        if (events.Count > 0)
        {
            await _outbox.EnqueueAsync(events, cancellationToken);
        }

        return events.Count;
    }
}
