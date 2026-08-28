namespace Crm.Bpm.Model;

public sealed class StartTrigger
{
    public TriggerKind Kind { get; init; } = TriggerKind.Manual;
    public string? EntityName { get; init; }
    public string? FieldName { get; init; }
    public string? SignalName { get; init; }
    public string? Cron { get; init; }

    /// Extra guard evaluated against the triggering record before the instance is created.
    public string? Condition { get; init; }
}
