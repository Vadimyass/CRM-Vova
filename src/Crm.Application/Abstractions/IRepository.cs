using System.Linq.Expressions;
using Crm.Domain.Common;

namespace Crm.Application.Abstractions;

public interface IRepository<T> where T : Entity
{
    Task<T?> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<T>> ListAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

    Task AddAsync(T entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(T entity, CancellationToken cancellationToken = default);
}
