using Crm.Application.Abstractions;
using Crm.Domain.Identity;

namespace Crm.Infrastructure.Persistence;

/// Placeholder until JWT authentication is wired in; keeps every layer above honest about
/// where the acting user comes from.
public sealed class DemoCurrentUser : ICurrentUser
{
    public static readonly Guid DemoUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public Guid? UserId => DemoUserId;
    public Guid? OrgUnitId => null;
    public IReadOnlyCollection<string> Permissions => ["*"];
    public RecordAccessScope Scope => RecordAccessScope.All;
}
