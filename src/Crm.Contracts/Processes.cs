namespace Crm.Contracts;

public sealed record UserTaskDto(
    Guid Id,
    Guid ProcessInstanceId,
    string Title,
    string? RoleCode,
    Guid? AssigneeId,
    DateTimeOffset? DueDate,
    string Status,
    string? SubjectEntityName,
    Guid? SubjectEntityId,
    DateTimeOffset CreatedOn);

public sealed record CompleteTaskRequest(Dictionary<string, object?>? Result);

public sealed record ProcessInstanceDto(
    Guid Id,
    string DefinitionKey,
    int DefinitionVersion,
    string Status,
    string? SubjectEntityName,
    Guid? SubjectEntityId,
    DateTimeOffset StartedOn,
    DateTimeOffset? CompletedOn,
    string? Error);

public sealed record ProcessLogDto(string ElementId, string Event, string? Details, DateTimeOffset Timestamp);

public sealed record StartProcessRequest(string EntityName, Guid EntityId, Dictionary<string, object?>? Variables);
