namespace Crm.Bpm.Runtime;

public sealed record ProcessLogEntry(
    Guid InstanceId,
    Guid? TokenId,
    string ElementId,
    string Event,
    string? Details,
    DateTimeOffset Timestamp);
