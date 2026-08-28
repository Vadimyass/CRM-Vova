namespace Crm.Bpm.Abstractions;

public sealed class ServiceTaskRegistry : IServiceTaskRegistry
{
    private readonly Dictionary<string, IServiceTaskHandler> _handlers;

    public ServiceTaskRegistry(IEnumerable<IServiceTaskHandler> handlers)
    {
        _handlers = handlers.ToDictionary(h => h.Key, StringComparer.OrdinalIgnoreCase);
    }

    public IServiceTaskHandler Resolve(string key) =>
        _handlers.TryGetValue(key, out var handler)
            ? handler
            : throw new InvalidOperationException($"No service task handler registered for key '{key}'.");
}
