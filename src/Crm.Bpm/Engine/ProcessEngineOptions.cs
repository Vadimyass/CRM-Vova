namespace Crm.Bpm.Engine;

public sealed class ProcessEngineOptions
{
    /// Guard against a process looping forever inside a single run.
    public int MaxStepsPerRun { get; set; } = 1000;
}
