namespace Authy.Application.Shared.Abstractions;

public interface IAuthorizationService
{
    Task<Result> EnsureRootIpOrOwnerAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken);
    Task<Result> EnsureCanManageUserAsync(Guid targetUserId, Guid requestingUserId, CancellationToken cancellationToken);
}

