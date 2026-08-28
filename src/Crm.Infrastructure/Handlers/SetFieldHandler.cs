using Crm.Application.Abstractions;
using Crm.Bpm.Abstractions;
using Crm.Domain.Sales;

namespace Crm.Infrastructure.Handlers;

/// Writes a value into the record the process is running on. Parameters: field, value.
public sealed class SetFieldHandler : IServiceTaskHandler
{
    private readonly IRepository<Opportunity> _opportunities;
    private readonly IRepository<Lead> _leads;
    private readonly IUnitOfWork _unitOfWork;

    public SetFieldHandler(IRepository<Opportunity> opportunities, IRepository<Lead> leads, IUnitOfWork unitOfWork)
    {
        _opportunities = opportunities;
        _leads = leads;
        _unitOfWork = unitOfWork;
    }

    public string Key => "SetField";

    public async Task<IReadOnlyDictionary<string, object?>?> ExecuteAsync(ServiceTaskContext context, CancellationToken cancellationToken = default)
    {
        if (context.Subject is not { } subject)
        {
            throw new InvalidOperationException("SetField requires a process started on a record.");
        }

        var field = context.Parameters.GetValueOrDefault("field")?.ToString()
            ?? throw new InvalidOperationException("SetField requires a 'field' parameter.");
        var value = context.Parameters.GetValueOrDefault("value");

        switch (subject.EntityName)
        {
            case nameof(Opportunity):
                var opportunity = await _opportunities.GetAsync(subject.EntityId, cancellationToken)
                    ?? throw new InvalidOperationException($"Opportunity '{subject.EntityId}' not found.");
                opportunity.CustomData[field] = value;
                await _opportunities.UpdateAsync(opportunity, cancellationToken);
                break;

            case nameof(Lead):
                var lead = await _leads.GetAsync(subject.EntityId, cancellationToken)
                    ?? throw new InvalidOperationException($"Lead '{subject.EntityId}' not found.");
                lead.CustomData[field] = value;
                await _leads.UpdateAsync(lead, cancellationToken);
                break;

            default:
                throw new InvalidOperationException($"SetField does not support entity '{subject.EntityName}'.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return null;
    }
}
