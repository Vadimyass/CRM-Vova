namespace Crm.Bpm.Abstractions;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
