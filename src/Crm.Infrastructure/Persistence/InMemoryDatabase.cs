using Crm.Domain.Common;

namespace Crm.Infrastructure.Persistence;

/// Development storage. The EF Core / PostgreSQL implementation replaces this behind the same
/// IRepository ports - nothing above this layer changes when it does.
public sealed class InMemoryDatabase
{
    private readonly Dictionary<Type, object> _sets = [];
    private readonly Lock _gate = new();

    public List<T> Set<T>() where T : Entity
    {
        lock (_gate)
        {
            if (!_sets.TryGetValue(typeof(T), out var set))
            {
                set = new List<T>();
                _sets[typeof(T)] = set;
            }

            return (List<T>)set;
        }
    }
}
