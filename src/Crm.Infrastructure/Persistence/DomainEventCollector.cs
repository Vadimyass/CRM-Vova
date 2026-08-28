using Crm.Domain.Common;

namespace Crm.Infrastructure.Persistence;

/// Stands in for the outbox table: entities register here, the unit of work drains them on save.
public sealed class DomainEventCollector
{
    private readonly List<Entity> _tracked = [];

    public void Track(Entity entity)
    {
        if (!_tracked.Contains(entity))
        {
            _tracked.Add(entity);
        }
    }

    public IReadOnlyList<IDomainEvent> Drain()
    {
        var events = _tracked.SelectMany(e => e.DomainEvents).ToList();

        foreach (var entity in _tracked)
        {
            entity.ClearDomainEvents();
        }

        _tracked.Clear();
        return events;
    }
}
