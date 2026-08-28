using Crm.Bpm.Model;

namespace Crm.Infrastructure.Seed;

/// Two processes that exercise every element the engine currently supports.
/// They are seeded so the API is demonstrable before the BPMN designer exists.
public static class DemoProcesses
{
    public static ProcessDefinition LeadFollowUp() => new("lead-created", 1, "Обработка нового лида", [
        new StartEvent
        {
            Id = "start",
            Trigger = new StartTrigger { Kind = TriggerKind.RecordCreated, EntityName = "Lead" },
            Outgoing = [Flow("call")]
        },
        new ServiceTaskElement
        {
            Id = "call",
            Name = "Поставить звонок",
            HandlerKey = "CreateActivity",
            Parameters =
            {
                ["title"] = "'Позвонить новому лиду'",
                ["type"] = "'Call'",
                ["dueInMinutes"] = "60"
            },
            Outgoing = [Flow("wait")]
        },
        new TimerElement
        {
            Id = "wait",
            Name = "Пауза перед проверкой",
            Delay = TimeSpan.FromMinutes(2),
            Outgoing = [Flow("recheck")]
        },
        new ServiceTaskElement
        {
            Id = "recheck",
            Name = "Поставить задачу на проверку",
            HandlerKey = "CreateActivity",
            Parameters =
            {
                ["title"] = "'Проверить статус лида'",
                ["type"] = "'Task'"
            },
            Outgoing = [Flow("end")]
        },
        new EndEvent { Id = "end" }
    ]);

    public static ProcessDefinition StageApproval() => new("opportunity-stage-changed", 1, "Согласование смены стадии", [
        new StartEvent
        {
            Id = "start",
            Trigger = new StartTrigger { Kind = TriggerKind.FieldChanged, EntityName = "Opportunity", FieldName = "StageId" },
            Outgoing = [Flow("review")]
        },
        new UserTaskElement
        {
            Id = "review",
            Name = "Проверка сделки",
            TitleTemplate = "Проверить документы по сделке",
            RoleCode = "SalesManager",
            DueInMinutes = 60 * 24,
            Outgoing = [Flow("gate")]
        },
        new ExclusiveGateway
        {
            Id = "gate",
            Name = "Согласовано?",
            Outgoing =
            [
                Flow("approved", "vars.approved == true"),
                Flow("rejected", isDefault: true)
            ]
        },
        new ServiceTaskElement
        {
            Id = "approved",
            HandlerKey = "SetField",
            Parameters = { ["field"] = "'reviewResult'", ["value"] = "'approved'" },
            Outgoing = [Flow("end")]
        },
        new ServiceTaskElement
        {
            Id = "rejected",
            HandlerKey = "Log",
            Parameters = { ["message"] = "'Смена стадии не согласована'" },
            Outgoing = [Flow("end")]
        },
        new EndEvent { Id = "end" }
    ]);

    private static SequenceFlow Flow(string target, string? condition = null, bool isDefault = false) => new()
    {
        Id = $"to-{target}",
        TargetElementId = target,
        Condition = condition,
        IsDefault = isDefault
    };
}
