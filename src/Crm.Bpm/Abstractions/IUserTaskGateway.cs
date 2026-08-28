using Crm.Bpm.Runtime;

namespace Crm.Bpm.Abstractions;

public interface IUserTaskGateway
{
    /// Returns the id of the created task; the token waits on it until the task is completed.
    Task<Guid> CreateAsync(UserTaskRequest request, CancellationToken cancellationToken = default);
}
