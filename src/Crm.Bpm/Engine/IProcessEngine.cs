using Crm.Bpm.Runtime;

namespace Crm.Bpm.Engine;

public interface IProcessEngine
{
    Task<ProcessInstance> StartAsync(
        string definitionKey,
        EntityReference? subject = null,
        IDictionary<string, object?>? variables = null,
        CancellationToken cancellationToken = default);

    Task<ProcessInstance> ResumeAsync(
        Guid tokenId,
        IDictionary<string, object?>? outputs = null,
        CancellationToken cancellationToken = default);

    Task<ProcessInstance> CancelAsync(Guid instanceId, string reason, CancellationToken cancellationToken = default);
}
