using Crm.Bpm.Abstractions;
using Crm.Bpm.Model;

namespace Crm.Bpm.Storage;

/// Development and test store. Production uses the EF Core implementation in Crm.Infrastructure.
public sealed class InMemoryProcessDefinitionStore : IProcessDefinitionStore
{
    private readonly Dictionary<(string Key, int Version), ProcessDefinition> _definitions = [];
    private readonly Dictionary<string, int> _activeVersions = [];
    private readonly Dictionary<string, StartTrigger> _triggers = [];

    public void Publish(ProcessDefinition definition, bool makeActive = true)
    {
        var errors = definition.Validate();
        if (errors.Count > 0)
        {
            throw new InvalidOperationException($"Process '{definition.Key}' is invalid: {string.Join("; ", errors)}");
        }

        _definitions[(definition.Key, definition.Version)] = definition;
        _triggers[$"{definition.Key}:{definition.Version}"] = definition.Start.Trigger;

        if (makeActive)
        {
            _activeVersions[definition.Key] = definition.Version;
        }
    }

    public Task<ProcessDefinition?> GetActiveAsync(string key, CancellationToken cancellationToken = default) =>
        Task.FromResult(_activeVersions.TryGetValue(key, out var version)
            ? _definitions.GetValueOrDefault((key, version))
            : null);

    public Task<ProcessDefinition?> GetAsync(string key, int version, CancellationToken cancellationToken = default) =>
        Task.FromResult(_definitions.GetValueOrDefault((key, version)));

    public Task<IReadOnlyList<ProcessDefinition>> GetActiveByTriggerAsync(TriggerKind kind, string? entityName, CancellationToken cancellationToken = default)
    {
        var matches = _activeVersions
            .Select(pair => _definitions.GetValueOrDefault((pair.Key, pair.Value)))
            .OfType<ProcessDefinition>()
            .Where(definition => definition.Start.Trigger.Kind == kind
                && (entityName is null || string.Equals(definition.Start.Trigger.EntityName, entityName, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        return Task.FromResult<IReadOnlyList<ProcessDefinition>>(matches);
    }
}
