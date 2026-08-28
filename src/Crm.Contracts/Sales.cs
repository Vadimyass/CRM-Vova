namespace Crm.Contracts;

public sealed record LeadDto(
    Guid Id,
    string Title,
    string? ContactName,
    string? CompanyName,
    string? Phone,
    string? Email,
    decimal? EstimatedAmount,
    string Status,
    Guid? OwnerId,
    DateTimeOffset CreatedOn);

public sealed record CreateLeadRequest(
    string Title,
    string? ContactName,
    string? CompanyName,
    string? Phone,
    string? Email,
    decimal? EstimatedAmount);

public sealed record OpportunityDto(
    Guid Id,
    string Title,
    decimal Amount,
    string Currency,
    Guid StageId,
    string StageName,
    DateOnly? CloseDate,
    Guid? OwnerId,
    DateTimeOffset StageEnteredOn);

public sealed record CreateOpportunityRequest(string Title, decimal Amount, Guid? StageId, Guid? AccountId);

public sealed record MoveStageRequest(Guid StageId);

public sealed record StageDto(Guid Id, string Name, int Order, int Probability, bool IsFinal, bool IsWon, string? Color);
