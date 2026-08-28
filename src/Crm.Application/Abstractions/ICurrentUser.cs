using Crm.Domain.Identity;

namespace Crm.Application.Abstractions;

public interface ICurrentUser
{
    Guid? UserId { get; }
    Guid? OrgUnitId { get; }
    IReadOnlyCollection<string> Permissions { get; }
    RecordAccessScope Scope { get; }
}
