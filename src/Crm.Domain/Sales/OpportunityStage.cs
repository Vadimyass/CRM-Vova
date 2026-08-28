using Crm.Domain.Common;

namespace Crm.Domain.Sales;

public class OpportunityStage : Entity
{
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public int Probability { get; set; }
    public bool IsFinal { get; set; }
    public bool IsWon { get; set; }
    public string? Color { get; set; }
}
