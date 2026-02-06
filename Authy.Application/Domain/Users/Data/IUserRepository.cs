namespace Authy.Application.Domain.Users.Data;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<Guid?> GetOrganizationIdAsync(Guid userId, CancellationToken cancellationToken);
    Task<List<string>> GetScopesAsync(Guid userId, CancellationToken cancellationToken);
}

