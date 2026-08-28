namespace Crm.Bpm.Model;

public sealed class StartEvent : ProcessElement
{
    public StartTrigger Trigger { get; init; } = new();
}
