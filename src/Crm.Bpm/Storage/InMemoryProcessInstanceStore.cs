using Crm.Bpm.Abstractions;
using Crm.Bpm.Runtime;

namespace Crm.Bpm.Storage;

public sealed class InMemoryProcessInstanceStore : IProcessInstanceStore
{
    private readonly Dictionary<Guid, ProcessInstance> _instances = [];

    public IReadOnlyCollection<ProcessInstance> All => _instances.Values;

    public Task AddAsync(ProcessInstance instance, CancellationToken cancellationToken = default)
    {
        _instances[instance.Id] = instance;
        return Task.CompletedTask;
    }

    public Task<ProcessInstance?> GetAsync(Guid instanceId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_instances.GetValueOrDefault(instanceId));

    public Task<ProcessInstance?> GetByTokenAsync(Guid tokenId, CancellationToken cancellationToken = default) =>
        Task.FromResult(_instances.Values.FirstOrDefault(i => i.Tokens.Any(t => t.Id == tokenId)));

    public Task SaveAsync(ProcessInstance instance, CancellationToken cancellationToken = default)
    {
        _instances[instance.Id] = instance;
        return Task.CompletedTask;
    }
}
