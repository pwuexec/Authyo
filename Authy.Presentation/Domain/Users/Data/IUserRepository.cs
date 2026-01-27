using Authy.Presentation.Entitites;

namespace Authy.Presentation.Domain.Users.Data;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
}
