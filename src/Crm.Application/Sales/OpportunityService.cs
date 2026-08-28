using Crm.Application.Abstractions;
using Crm.Contracts;
using Crm.Domain.Sales;

namespace Crm.Application.Sales;

public sealed class OpportunityService
{
    private readonly IRepository<Opportunity> _opportunities;
    private readonly IRepository<OpportunityStage> _stages;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public OpportunityService(
        IRepository<Opportunity> opportunities,
        IRepository<OpportunityStage> stages,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _opportunities = opportunities;
        _stages = stages;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<OpportunityDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var opportunities = await _opportunities.ListAsync(cancellationToken: cancellationToken);
        var stages = (await _stages.ListAsync(cancellationToken: cancellationToken)).ToDictionary(s => s.Id);

        return opportunities
            .Select(o => Map(o, stages.GetValueOrDefault(o.StageId)?.Name ?? "—"))
            .ToList();
    }

    public async Task<IReadOnlyList<StageDto>> ListStagesAsync(CancellationToken cancellationToken = default)
    {
        var stages = await _stages.ListAsync(cancellationToken: cancellationToken);
        return stages
            .OrderBy(s => s.Order)
            .Select(s => new StageDto(s.Id, s.Name, s.Order, s.Probability, s.IsFinal, s.IsWon, s.Color))
            .ToList();
    }

    public async Task<OpportunityDto?> MoveStageAsync(Guid id, Guid stageId, CancellationToken cancellationToken = default)
    {
        var opportunity = await _opportunities.GetAsync(id, cancellationToken);
        if (opportunity is null)
        {
            return null;
        }

        var stages = await _stages.ListAsync(cancellationToken: cancellationToken);
        var stage = stages.FirstOrDefault(s => s.Id == stageId)
            ?? throw new InvalidOperationException($"Stage '{stageId}' does not exist.");

        opportunity.MoveToStage(stage.Id, _currentUser.UserId);
        await _opportunities.UpdateAsync(opportunity, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(opportunity, stage.Name);
    }

    private static OpportunityDto Map(Opportunity opportunity, string stageName) => new(
        opportunity.Id,
        opportunity.Title,
        opportunity.Amount,
        opportunity.Currency,
        opportunity.StageId,
        stageName,
        opportunity.CloseDate,
        opportunity.OwnerId,
        opportunity.StageEnteredOn);
}
