namespace Crm.Bpm.Abstractions;

public interface ITimerScheduler
{
    Task ScheduleAsync(Guid instanceId, Guid tokenId, DateTimeOffset fireAt, CancellationToken cancellationToken = default);

    Task CancelAsync(Guid tokenId, CancellationToken cancellationToken = default);
}
