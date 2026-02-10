namespace Authy.Application.Domain.Users.Data;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid?> GetOrganizationUserIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<List<string>> GetScopesAsync(Guid userId, CancellationToken cancellationToken);
    Task<List<User>> GetByOrganizationIdAsync(Guid organizationId, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task UpdateAsync(User user, CancellationToken cancellationToken);
    Task DeleteAsync(User user, CancellationToken cancellationToken);
}
