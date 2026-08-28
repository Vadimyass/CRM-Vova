using Crm.Domain.Common;

namespace Crm.Domain.Identity;

public class Role : Entity
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = [];
}
