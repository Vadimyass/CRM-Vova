using Crm.Domain.Common;

namespace Crm.Domain.Metadata;

/// Registry of user-defined fields. The values live in the owner's CustomData jsonb column,
/// so adding a field never triggers a schema migration.
public class EntityField : AuditableEntity
{
    public string EntityName { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Caption { get; set; } = string.Empty;
    public CustomFieldType Type { get; set; }
    public bool IsRequired { get; set; }
    public Guid? LookupId { get; set; }
    public int Order { get; set; }
    public string? DefaultValue { get; set; }
}
