namespace Crm.Bpm.Model;

public sealed class EndEvent : ProcessElement
{
    /// Terminate ends the whole instance, killing sibling tokens; the default only ends this branch.
    public bool IsTerminate { get; init; }
}
