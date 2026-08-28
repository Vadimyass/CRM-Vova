namespace Crm.Domain.Common;

/// Custom fields live in a single jsonb column instead of generated columns:
/// adding a field changes the field registry, never the database schema.
public interface ICustomFieldOwner
{
    Dictionary<string, object?> CustomData { get; set; }
}
