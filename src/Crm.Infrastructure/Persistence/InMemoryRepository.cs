using System.Linq.Expressions;
using Crm.Application.Abstractions;
using Crm.Domain.Common;

namespace Crm.Infrastructure.Persistence;

public sealed class InMemoryRepository<T> : IRepository<T> where T : Entity
{
    private readonly InMemoryDatabase _database;
    private readonly DomainEventCollector _collector;

    public InMemoryRepository(InMemoryDatabase database, DomainEventCollector collector)
    {
        _database = database;
        _collector = collector;
    }

    public Task<T?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = _database.Set<T>().FirstOrDefault(e => e.Id == id);
        if (entity is not null)
        {
            _collector.Track(entity);
        }

        return Task.FromResult(entity);
    }

    public Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<T> query = _database.Set<T>();

        if (predicate is not null)
        {
            query = query.Where(predicate.Compile());
        }

        return Task.FromResult<IReadOnlyList<T>>(query.ToList());
    }

    public Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        _database.Set<T>().Add(entity);
        _collector.Track(entity);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        _collector.Track(entity);
        return Task.CompletedTask;
    }
}
