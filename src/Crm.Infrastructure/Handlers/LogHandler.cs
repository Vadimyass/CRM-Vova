using Crm.Bpm.Abstractions;
using Microsoft.Extensions.Logging;

namespace Crm.Infrastructure.Handlers;

/// Stand-in for SendEmail and Webhook until those integrations exist. Parameters: message.
public sealed class LogHandler : IServiceTaskHandler
{
    private readonly ILogger<LogHandler> _logger;

    public LogHandler(ILogger<LogHandler> logger)
    {
        _logger = logger;
    }

    public string Key => "Log";

    public Task<IReadOnlyDictionary<string, object?>?> ExecuteAsync(ServiceTaskContext context, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Process {InstanceId} at {ElementId}: {Message}",
            context.InstanceId,
            context.ElementId,
            context.Parameters.GetValueOrDefault("message"));

        return Task.FromResult<IReadOnlyDictionary<string, object?>?>(null);
    }
}
