namespace Authy.Presentation.Domain.Roles;

public interface IRoleRepository
{
    Task AddAsync(Role role, CancellationToken cancellationToken);
    Task UpdateAsync(Role role, CancellationToken cancellationToken);
    Task<List<Role>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken);
    Task<Role?> GetByNameAsync(Guid organizationId, string name, CancellationToken cancellationToken);
}
