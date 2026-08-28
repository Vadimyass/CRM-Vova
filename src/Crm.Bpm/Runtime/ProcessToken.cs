namespace Crm.Bpm.Runtime;

public sealed class ProcessToken
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required Guid InstanceId { get; init; }
    public Guid? ParentTokenId { get; init; }
    public required string ElementId { get; set; }
    public TokenStatus Status { get; set; } = TokenStatus.Active;
    public WaitKind WaitKind { get; set; } = WaitKind.None;
    public string? WaitKey { get; set; }
    public string? Error { get; set; }
    public int Attempts { get; set; }
    public DateTimeOffset CreatedOn { get; init; } = DateTimeOffset.UtcNow;
}
