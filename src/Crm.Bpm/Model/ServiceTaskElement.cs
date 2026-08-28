namespace Crm.Bpm.Model;

public sealed class ServiceTaskElement : ProcessElement
{
    /// Key of a registered IServiceTaskHandler: SetField, CreateRecord, SendEmail, Webhook.
    public required string HandlerKey { get; init; }

    /// Values are expressions, evaluated against the instance context at execution time.
    public Dictionary<string, string> Parameters { get; init; } = [];

    public int MaxRetries { get; init; } = 3;
}
