using System.Collections.Concurrent;
using Crm.Bpm.Abstractions;

namespace Crm.Infrastructure.Processes;

/// Timers are polled by TimerHostedService. In production this becomes a Hangfire job so that
/// a restarted host does not lose pending waits.
public sealed class InMemoryTimerScheduler : ITimerScheduler
{
    private readonly ConcurrentDictionary<Guid, (Guid InstanceId, DateTimeOffset FireAt)> _timers = new();

    public Task ScheduleAsync(Guid instanceId, Guid tokenId, DateTimeOffset fireAt, CancellationToken cancellationToken = default)
    {
        _timers[tokenId] = (instanceId, fireAt);
        return Task.CompletedTask;
    }

    public Task CancelAsync(Guid tokenId, CancellationToken cancellationToken = default)
    {
        _timers.TryRemove(tokenId, out _);
        return Task.CompletedTask;
    }

    public IReadOnlyList<Guid> TakeDue(DateTimeOffset now)
    {
        var due = _timers.Where(pair => pair.Value.FireAt <= now).Select(pair => pair.Key).ToList();

        foreach (var tokenId in due)
        {
            _timers.TryRemove(tokenId, out _);
        }

        return due;
    }
}
