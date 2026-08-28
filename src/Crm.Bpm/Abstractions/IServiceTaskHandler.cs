namespace Crm.Bpm.Abstractions;

public interface IServiceTaskHandler
{
    /// Matches ServiceTaskElement.HandlerKey.
    string Key { get; }

    /// Returned values are merged into the instance variables.
    Task<IReadOnlyDictionary<string, object?>?> ExecuteAsync(ServiceTaskContext context, CancellationToken cancellationToken = default);
}
