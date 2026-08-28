using Crm.Bpm.Model;

namespace Crm.Bpm.Abstractions;

public interface IProcessDefinitionStore
{
    Task<ProcessDefinition?> GetActiveAsync(string key, CancellationToken cancellationToken = default);

    Task<ProcessDefinition?> GetAsync(string key, int version, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProcessDefinition>> GetActiveByTriggerAsync(TriggerKind kind, string? entityName, CancellationToken cancellationToken = default);
}
