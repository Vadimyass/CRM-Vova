using Crm.Application.Abstractions;
using Crm.Bpm.Abstractions;
using Crm.Domain.Sales;

namespace Crm.Infrastructure.Handlers;

/// Parameters: title, type (Task|Call|Meeting|Email), dueInMinutes.
public sealed class CreateActivityHandler : IServiceTaskHandler
{
    private readonly IRepository<Activity> _activities;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public CreateActivityHandler(IRepository<Activity> activities, ICurrentUser currentUser, IUnitOfWork unitOfWork)
    {
        _activities = activities;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public string Key => "CreateActivity";

    public async Task<IReadOnlyDictionary<string, object?>?> ExecuteAsync(ServiceTaskContext context, CancellationToken cancellationToken = default)
    {
        var title = context.Parameters.GetValueOrDefault("title")?.ToString() ?? "Задача из процесса";
        var typeText = context.Parameters.GetValueOrDefault("type")?.ToString() ?? nameof(ActivityType.Task);
        var type = Enum.TryParse<ActivityType>(typeText, ignoreCase: true, out var parsed) ? parsed : ActivityType.Task;

        var activity = new Activity(title, type)
        {
            OwnerId = _currentUser.UserId,
            RelatedEntityName = context.Subject?.EntityName,
            RelatedEntityId = context.Subject?.EntityId
        };

        if (context.Parameters.GetValueOrDefault("dueInMinutes") is { } dueRaw
            && double.TryParse(dueRaw.ToString(), out var minutes))
        {
            activity.DueDate = DateTimeOffset.UtcNow.AddMinutes(minutes);
        }

        await _activities.AddAsync(activity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new Dictionary<string, object?> { ["activityId"] = activity.Id };
    }
}
