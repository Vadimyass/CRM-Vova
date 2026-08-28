namespace Crm.Bpm.Runtime;

public sealed class ProcessInstance
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string DefinitionKey { get; init; }

    /// Pinned at start: editing a process publishes a new version, running instances keep playing the old one.
    public required int DefinitionVersion { get; init; }

    public EntityReference? Subject { get; init; }
    public ProcessInstanceStatus Status { get; set; } = ProcessInstanceStatus.Running;
    public Dictionary<string, object?> Variables { get; init; } = [];
    public List<ProcessToken> Tokens { get; init; } = [];
    public DateTimeOffset StartedOn { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? CompletedOn { get; set; }
    public string? Error { get; set; }

    public bool HasActiveTokens => Tokens.Any(t => t.Status == TokenStatus.Active);
    public bool HasWaitingTokens => Tokens.Any(t => t.Status == TokenStatus.Waiting);
}
