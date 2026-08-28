namespace Crm.Bpm.Model;

public sealed class UserTaskElement : ProcessElement
{
    public string TitleTemplate { get; init; } = string.Empty;
    public string? AssigneeExpression { get; init; }
    public string? RoleCode { get; init; }
    public int? DueInMinutes { get; init; }
    public string? FormKey { get; init; }
}
