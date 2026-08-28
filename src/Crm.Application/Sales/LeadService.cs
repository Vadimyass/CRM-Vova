using Crm.Application.Abstractions;
using Crm.Contracts;
using Crm.Domain.Sales;

namespace Crm.Application.Sales;

public sealed class LeadService
{
    private readonly IRepository<Lead> _leads;
    private readonly IRepository<Opportunity> _opportunities;
    private readonly IRepository<OpportunityStage> _stages;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;

    public LeadService(
        IRepository<Lead> leads,
        IRepository<Opportunity> opportunities,
        IRepository<OpportunityStage> stages,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork)
    {
        _leads = leads;
        _opportunities = opportunities;
        _stages = stages;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<LeadDto>> ListAsync(CancellationToken cancellationToken = default)
    {
        var leads = await _leads.ListAsync(cancellationToken: cancellationToken);
        return leads.Select(Map).ToList();
    }

    public async Task<LeadDto?> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var lead = await _leads.GetAsync(id, cancellationToken);
        return lead is null ? null : Map(lead);
    }

    public async Task<LeadDto> CreateAsync(CreateLeadRequest request, CancellationToken cancellationToken = default)
    {
        var lead = new Lead(request.Title)
        {
            ContactName = request.ContactName,
            CompanyName = request.CompanyName,
            Phone = request.Phone,
            Email = request.Email,
            EstimatedAmount = request.EstimatedAmount,
            OwnerId = _currentUser.UserId,
            OrgUnitId = _currentUser.OrgUnitId,
            CreatedById = _currentUser.UserId
        };

        await _leads.AddAsync(lead, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(lead);
    }

    public async Task<OpportunityDto?> QualifyAsync(Guid leadId, CancellationToken cancellationToken = default)
    {
        var lead = await _leads.GetAsync(leadId, cancellationToken);
        if (lead is null || lead.Status == LeadStatus.Qualified)
        {
            return null;
        }

        var stages = await _stages.ListAsync(cancellationToken: cancellationToken);
        var firstStage = stages.OrderBy(s => s.Order).FirstOrDefault()
            ?? throw new InvalidOperationException("The sales funnel has no stages configured.");

        var opportunity = new Opportunity(lead.Title, firstStage.Id)
        {
            Amount = lead.EstimatedAmount ?? 0,
            AccountId = lead.AccountId,
            PrimaryContactId = lead.ContactId,
            OwnerId = lead.OwnerId,
            OrgUnitId = lead.OrgUnitId,
            CreatedById = _currentUser.UserId
        };

        await _opportunities.AddAsync(opportunity, cancellationToken);

        lead.Qualify(opportunity.Id);
        await _leads.UpdateAsync(lead, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new OpportunityDto(
            opportunity.Id,
            opportunity.Title,
            opportunity.Amount,
            opportunity.Currency,
            opportunity.StageId,
            firstStage.Name,
            opportunity.CloseDate,
            opportunity.OwnerId,
            opportunity.StageEnteredOn);
    }

    public async Task<bool> DisqualifyAsync(Guid leadId, string reason, CancellationToken cancellationToken = default)
    {
        var lead = await _leads.GetAsync(leadId, cancellationToken);
        if (lead is null)
        {
            return false;
        }

        lead.Disqualify(reason);
        await _leads.UpdateAsync(lead, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static LeadDto Map(Lead lead) => new(
        lead.Id,
        lead.Title,
        lead.ContactName,
        lead.CompanyName,
        lead.Phone,
        lead.Email,
        lead.EstimatedAmount,
        lead.Status.ToString(),
        lead.OwnerId,
        lead.CreatedOn);
}
