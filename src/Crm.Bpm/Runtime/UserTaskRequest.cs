namespace Crm.Bpm.Runtime;

public sealed record UserTaskRequest(
    Guid InstanceId,
    Guid TokenId,
    string ElementId,
    string Title,
    Guid? AssigneeId,
    string? RoleCode,
    DateTimeOffset? DueDate,
    string? FormKey,
    EntityReference? Subject);
