using Crm.Application.Sales;
using Crm.Contracts;

namespace Crm.Api.Endpoints;

public static class SalesEndpoints
{
    public static IEndpointRouteBuilder MapSalesEndpoints(this IEndpointRouteBuilder app)
    {
        var leads = app.MapGroup("/api/leads");

        leads.MapGet("/", async (LeadService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(ct)));

        leads.MapGet("/{id:guid}", async (Guid id, LeadService service, CancellationToken ct) =>
            await service.GetAsync(id, ct) is { } lead ? Results.Ok(lead) : Results.NotFound());

        leads.MapPost("/", async (CreateLeadRequest request, LeadService service, CancellationToken ct) =>
        {
            var lead = await service.CreateAsync(request, ct);
            return Results.Created($"/api/leads/{lead.Id}", lead);
        });

        leads.MapPost("/{id:guid}/qualify", async (Guid id, LeadService service, CancellationToken ct) =>
            await service.QualifyAsync(id, ct) is { } opportunity
                ? Results.Ok(opportunity)
                : Results.Problem("Лид не найден или уже квалифицирован.", statusCode: StatusCodes.Status409Conflict));

        leads.MapPost("/{id:guid}/disqualify", async (Guid id, DisqualifyRequest request, LeadService service, CancellationToken ct) =>
            await service.DisqualifyAsync(id, request.Reason, ct) ? Results.NoContent() : Results.NotFound());

        var opportunities = app.MapGroup("/api/opportunities");

        opportunities.MapGet("/", async (OpportunityService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(ct)));

        opportunities.MapPost("/{id:guid}/stage", async (Guid id, MoveStageRequest request, OpportunityService service, CancellationToken ct) =>
            await service.MoveStageAsync(id, request.StageId, ct) is { } opportunity
                ? Results.Ok(opportunity)
                : Results.NotFound());

        app.MapGet("/api/stages", async (OpportunityService service, CancellationToken ct) =>
            Results.Ok(await service.ListStagesAsync(ct)));

        return app;
    }

    public sealed record DisqualifyRequest(string Reason);
}
