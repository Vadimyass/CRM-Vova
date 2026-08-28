namespace Crm.Bpm.Model;

public sealed class TimerElement : ProcessElement
{
    public TimeSpan? Delay { get; init; }

    /// Expression returning a DateTimeOffset. Takes precedence over Delay when set.
    public string? DueDateExpression { get; init; }
}
