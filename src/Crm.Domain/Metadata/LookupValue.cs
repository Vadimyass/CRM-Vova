using Crm.Domain.Common;

namespace Crm.Domain.Metadata;

public class LookupValue : Entity
{
    public Guid LookupId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }
    public bool IsActive { get; set; } = true;
}
