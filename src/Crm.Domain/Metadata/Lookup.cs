using Crm.Domain.Common;

namespace Crm.Domain.Metadata;

public class Lookup : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ICollection<LookupValue> Values { get; set; } = [];
}
