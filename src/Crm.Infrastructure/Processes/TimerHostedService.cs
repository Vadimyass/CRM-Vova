using Crm.Bpm.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Crm.Infrastructure.Processes;

public sealed class TimerHostedService : BackgroundService
{
    private static readonly TimeSpan _pollInterval = TimeSpan.FromSeconds(1);

    private readonly InMemoryTimerScheduler _scheduler;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TimerHostedService> _logger;

    public TimerHostedService(
        InMemoryTimerScheduler scheduler,
        IServiceScopeFactory scopeFactory,
        ILogger<TimerHostedService> logger)
    {
        _scheduler = scheduler;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            foreach (var tokenId in _scheduler.TakeDue(DateTimeOffset.UtcNow))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var engine = scope.ServiceProvider.GetRequiredService<IProcessEngine>();
                    await engine.ResumeAsync(tokenId, cancellationToken: stoppingToken);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to resume token {TokenId} after its timer fired", tokenId);
                }
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }
}
