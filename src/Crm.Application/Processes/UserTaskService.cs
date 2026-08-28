using Crm.Application.Abstractions;
using Crm.Bpm.Engine;
using Crm.Contracts;
using Crm.Domain.Processes;

namespace Crm.Application.Processes;

public sealed class UserTaskService
{
    private readonly IRepository<UserTask> _tasks;
    private readonly IProcessEngine _engine;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public UserTaskService(
        IRepository<UserTask> tasks,
        IProcessEngine engine,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _tasks = tasks;
        _engine = engine;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<UserTaskDto>> ListPendingAsync(CancellationToken cancellationToken = default)
    {
        var tasks = await _tasks.ListAsync(t => t.Status == UserTaskStatus.Pending, cancellationToken);
        return tasks.OrderBy(t => t.CreatedOn).Select(Map).ToList();
    }

    public async Task<bool> CompleteAsync(Guid taskId, Dictionary<string, object?>? result, CancellationToken cancellationToken = default)
    {
        var task = await _tasks.GetAsync(taskId, cancellationToken);
        if (task is null || task.Status != UserTaskStatus.Pending)
        {
            return false;
        }

        task.Complete(_currentUser.UserId, result);
        await _tasks.UpdateAsync(task, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _engine.ResumeAsync(task.TokenId, result, cancellationToken);
        return true;
    }

    private static UserTaskDto Map(UserTask task) => new(
        task.Id,
        task.ProcessInstanceId,
        task.Title,
        task.RoleCode,
        task.AssigneeId,
        task.DueDate,
        task.Status.ToString(),
        task.SubjectEntityName,
        task.SubjectEntityId,
        task.CreatedOn);
}
