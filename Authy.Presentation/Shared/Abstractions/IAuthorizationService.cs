using Authy.Presentation.Shared;

namespace Authy.Presentation.Shared.Abstractions;

public interface IAuthorizationService
{
    Task<Result> EnsureRootIpOrOwnerAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken);
}
