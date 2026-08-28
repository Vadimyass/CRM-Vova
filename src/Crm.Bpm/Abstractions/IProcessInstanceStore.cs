using Crm.Bpm.Runtime;

namespace Crm.Bpm.Abstractions;

public interface IProcessInstanceStore
{
    Task AddAsync(ProcessInstance instance, CancellationToken cancellationToken = default);

    Task<ProcessInstance?> GetAsync(Guid instanceId, CancellationToken cancellationToken = default);

    Task<ProcessInstance?> GetByTokenAsync(Guid tokenId, CancellationToken cancellationToken = default);

    Task SaveAsync(ProcessInstance instance, CancellationToken cancellationToken = default);
}
