using Crm.Domain.Common;

namespace Crm.Domain.Sales;

public class StageHistoryItem : Entity
{
    public Guid OpportunityId { get; set; }
    public Guid? FromStageId { get; set; }
    public Guid ToStageId { get; set; }
    public DateTimeOffset ChangedOn { get; set; } = DateTimeOffset.UtcNow;
    public Guid? ChangedById { get; set; }
    public TimeSpan? TimeInPreviousStage { get; set; }
}
