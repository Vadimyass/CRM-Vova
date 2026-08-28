using Crm.Application.Processes;
using Crm.Bpm.Engine;
using Crm.Bpm.Runtime;
using Crm.Bpm.Storage;
using Crm.Contracts;

namespace Crm.Api.Endpoints;

public static class ProcessEndpoints
{
    public static IEndpointRouteBuilder MapProcessEndpoints(this IEndpointRouteBuilder app)
    {
        var tasks = app.MapGroup("/api/tasks");

        tasks.MapGet("/", async (UserTaskService service, CancellationToken ct) =>
            Results.Ok(await service.ListPendingAsync(ct)));

        tasks.MapPost("/{id:guid}/complete", async (Guid id, CompleteTaskRequest request, UserTaskService service, CancellationToken ct) =>
            await service.CompleteAsync(id, request.Result, ct)
                ? Results.NoContent()
                : Results.Problem("Задача не найдена или уже закрыта.", statusCode: StatusCodes.Status409Conflict));

        var processes = app.MapGroup("/api/processes");

        processes.MapGet("/", (InMemoryProcessInstanceStore store) =>
            Results.Ok(store.All
                .OrderByDescending(i => i.StartedOn)
                .Select(Map)
                .ToList()));

        processes.MapGet("/{id:guid}/log", (Guid id, InMemoryProcessLogWriter log) =>
            Results.Ok(log.Entries
                .Where(e => e.InstanceId == id)
                .Select(e => new ProcessLogDto(e.ElementId, e.Event, e.Details, e.Timestamp))
                .ToList()));

        processes.MapPost("/{key}/start", async (string key, StartProcessRequest request, IProcessEngine engine, CancellationToken ct) =>
        {
            var instance = await engine.StartAsync(
                key,
                new EntityReference(request.EntityName, request.EntityId),
                request.Variables,
                ct);

            return Results.Ok(Map(instance));
        });

        return app;
    }

    private static ProcessInstanceDto Map(ProcessInstance instance) => new(
        instance.Id,
        instance.DefinitionKey,
        instance.DefinitionVersion,
        instance.Status.ToString(),
        instance.Subject?.EntityName,
        instance.Subject?.EntityId,
        instance.StartedOn,
        instance.CompletedOn,
        instance.Error);
}
