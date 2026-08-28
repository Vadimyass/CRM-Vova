using Crm.Bpm.Model;

namespace Crm.Bpm.Tests;

public static class Flows
{
    public static SequenceFlow To(string target, string? condition = null, bool isDefault = false) => new()
    {
        Id = $"flow-{Guid.NewGuid():N}"[..12],
        TargetElementId = target,
        Condition = condition,
        IsDefault = isDefault
    };
}
