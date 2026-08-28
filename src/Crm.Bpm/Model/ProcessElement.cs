namespace Crm.Bpm.Model;

public abstract class ProcessElement
{
    public required string Id { get; init; }
    public string? Name { get; init; }
    public List<SequenceFlow> Outgoing { get; init; } = [];
}
