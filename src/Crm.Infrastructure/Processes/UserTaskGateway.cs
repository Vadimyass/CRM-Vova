using Crm.Application.Abstractions;
using Crm.Bpm.Abstractions;
using Crm.Bpm.Runtime;
using Crm.Domain.Processes;

namespace Crm.Infrastructure.Processes;

public sealed class UserTaskGateway : IUserTaskGateway
{
    private readonly IRepository<UserTask> _tasks;

    public UserTaskGateway(IRepository<UserTask> tasks)
    {
        _tasks = tasks;
    }

    public async Task<Guid> CreateAsync(UserTaskRequest request, CancellationToken cancellationToken = default)
    {
        var task = new UserTask
        {
            ProcessInstanceId = request.InstanceId,
            TokenId = request.TokenId,
            ElementId = request.ElementId,
            Title = request.Title,
            AssigneeId = request.AssigneeId,
            RoleCode = request.RoleCode,
            DueDate = request.DueDate,
            FormKey = request.FormKey,
            SubjectEntityName = request.Subject?.EntityName,
            SubjectEntityId = request.Subject?.EntityId
        };

        await _tasks.AddAsync(task, cancellationToken);
        return task.Id;
    }
}
