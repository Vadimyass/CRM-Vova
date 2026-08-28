using Crm.Bpm.Runtime;

namespace Crm.Bpm.Abstractions;

public sealed class ServiceTaskContext
{
    public required Guid InstanceId { get; init; }
    public required string ElementId { get; init; }
    public EntityReference? Subject { get; init; }
    public required IReadOnlyDictionary<string, object?> Parameters { get; init; }
    public required IDictionary<string, object?> Variables { get; init; }
}
