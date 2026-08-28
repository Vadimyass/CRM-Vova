using Crm.Domain.Common;

namespace Crm.Domain.Processes;

/// A step of a running process that waits for a person. The token stays parked until this is completed.
public class UserTask : AuditableEntity
{
    public Guid ProcessInstanceId { get; set; }
    public Guid TokenId { get; set; }
    public string ElementId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public Guid? AssigneeId { get; set; }
    public string? RoleCode { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public string? FormKey { get; set; }

    public string? SubjectEntityName { get; set; }
    public Guid? SubjectEntityId { get; set; }

    public UserTaskStatus Status { get; private set; } = UserTaskStatus.Pending;
    public DateTimeOffset? CompletedOn { get; private set; }
    public Guid? CompletedById { get; private set; }
    public Dictionary<string, object?> Result { get; private set; } = [];

    public void Complete(Guid? completedById, Dictionary<string, object?>? result)
    {
        Status = UserTaskStatus.Done;
        CompletedOn = DateTimeOffset.UtcNow;
        CompletedById = completedById;
        Result = result ?? [];
    }
}
