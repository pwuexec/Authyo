using Authy.Presentation.Domain;

namespace Authy.Presentation.Domain.Roles;

public interface IRoleRepository
{
    Task AddAsync(Role role, CancellationToken cancellationToken);
}
