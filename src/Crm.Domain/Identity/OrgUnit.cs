using Crm.Domain.Common;

namespace Crm.Domain.Identity;

public class OrgUnit : Entity
{
    public string Name { get; set; } = string.Empty;
    public Guid? ParentId { get; set; }
    /// Materialized path ("/root/sales/kyiv/") so subtree checks are a single LIKE instead of a recursive CTE.
    public string Path { get; set; } = "/";
}
