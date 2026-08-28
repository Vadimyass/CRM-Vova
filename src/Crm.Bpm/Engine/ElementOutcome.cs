using Crm.Bpm.Runtime;

namespace Crm.Bpm.Engine;

internal readonly record struct ElementOutcome(
    OutcomeKind Kind,
    IReadOnlyList<string> Targets,
    WaitKind WaitKind,
    string? WaitKey,
    bool Terminate,
    string? Error)
{
    public static ElementOutcome Continue(IReadOnlyList<string> targets) =>
        new(OutcomeKind.Continue, targets, WaitKind.None, null, false, null);

    public static ElementOutcome Wait(WaitKind kind, string? key) =>
        new(OutcomeKind.Wait, [], kind, key, false, null);

    public static ElementOutcome Finish(bool terminate = false) =>
        new(OutcomeKind.Finish, [], WaitKind.None, null, terminate, null);

    public static ElementOutcome Fail(string error) =>
        new(OutcomeKind.Fail, [], WaitKind.None, null, false, error);
}
