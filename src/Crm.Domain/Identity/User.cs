using Crm.Domain.Common;

namespace Crm.Domain.Identity;

public class User : AuditableEntity
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public Guid? OrgUnitId { get; set; }
    public OrgUnit? OrgUnit { get; set; }
    public ICollection<Role> Roles { get; set; } = [];
}
