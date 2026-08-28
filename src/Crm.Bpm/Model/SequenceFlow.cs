namespace Crm.Bpm.Model;

public sealed class SequenceFlow
{
    public required string Id { get; init; }
    public required string TargetElementId { get; init; }

    /// Evaluated only on exclusive gateways. Null means unconditional.
    public string? Condition { get; init; }

    public bool IsDefault { get; init; }
}
