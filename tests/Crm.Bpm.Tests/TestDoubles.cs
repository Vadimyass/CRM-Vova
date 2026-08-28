using Crm.Bpm.Abstractions;
using Crm.Bpm.Runtime;

namespace Crm.Bpm.Tests;

public sealed class FixedClock(DateTimeOffset now) : IClock
{
    public DateTimeOffset UtcNow { get; set; } = now;
}

public sealed class RecordingUserTaskGateway : IUserTaskGateway
{
    public List<UserTaskRequest> Created { get; } = [];

    public Task<Guid> CreateAsync(UserTaskRequest request, CancellationToken cancellationToken = default)
    {
        Created.Add(request);
        return Task.FromResult(Guid.CreateVersion7());
    }
}

public sealed class RecordingTimerScheduler : ITimerScheduler
{
    public List<(Guid InstanceId, Guid TokenId, DateTimeOffset FireAt)> Scheduled { get; } = [];
    public List<Guid> Cancelled { get; } = [];

    public Task ScheduleAsync(Guid instanceId, Guid tokenId, DateTimeOffset fireAt, CancellationToken cancellationToken = default)
    {
        Scheduled.Add((instanceId, tokenId, fireAt));
        return Task.CompletedTask;
    }

    public Task CancelAsync(Guid tokenId, CancellationToken cancellationToken = default)
    {
        Cancelled.Add(tokenId);
        return Task.CompletedTask;
    }
}

public sealed class DelegateServiceTaskHandler(
    string key,
    Func<ServiceTaskContext, IReadOnlyDictionary<string, object?>?> body) : IServiceTaskHandler
{
    public string Key { get; } = key;

    public int Calls { get; private set; }

    public Task<IReadOnlyDictionary<string, object?>?> ExecuteAsync(ServiceTaskContext context, CancellationToken cancellationToken = default)
    {
        Calls++;
        return Task.FromResult(body(context));
    }
}
