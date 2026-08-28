using Crm.Application.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Crm.Infrastructure.Processes;

public sealed class OutboxHostedService : BackgroundService
{
    private static readonly TimeSpan _idleDelay = TimeSpan.FromMilliseconds(200);
    private const int BatchSize = 50;

    private readonly IOutbox _outbox;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxHostedService> _logger;

    public OutboxHostedService(IOutbox outbox, IServiceScopeFactory scopeFactory, ILogger<OutboxHostedService> logger)
    {
        _outbox = outbox;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var batch = await _outbox.DequeueBatchAsync(BatchSize, stoppingToken);

            if (batch.Count == 0)
            {
                await Task.Delay(_idleDelay, stoppingToken);
                continue;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dispatcher = scope.ServiceProvider.GetRequiredService<IDomainEventDispatcher>();
                await dispatcher.DispatchAsync(batch, stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to dispatch a batch of {Count} domain events", batch.Count);
            }
        }
    }
}
