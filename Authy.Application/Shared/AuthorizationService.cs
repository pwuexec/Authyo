using Authy.Application.Domain.Organizations.Data;
using Authy.Application.Domain.Users.Data;
using Authy.Application.Extensions;
using Authy.Application.Shared.Abstractions;
using Microsoft.Extensions.Options;

namespace Authy.Application.Shared;

public class AuthorizationService(
    IHttpContextAccessor httpContextAccessor,
    IOptions<RootIpOptions> rootIpOptions,
    IOrganizationRepository organizationRepository,
    IUserRepository userRepository)
    : IAuthorizationService
{
    public async Task<Result> EnsureRootIpOrOwnerAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        var authResult = httpContextAccessor.HttpContext.EnsureRootIp(rootIpOptions.Value.RootIps);
        if (authResult.IsSuccess)
        {
            return Result.Success();
        }

        var org = await organizationRepository.GetByIdAsync(organizationId, cancellationToken);
        if (org == null || org.Owners.All(o => o.Id != userId))
        {
            return Result.Failure(DomainErrors.User.UnauthorizedOwner);
        }

        return Result.Success();
    }

    public async Task<Result> EnsureCanManageUserAsync(Guid targetUserId, Guid requestingUserId, CancellationToken cancellationToken)
    {
        var authResult = httpContextAccessor.HttpContext.EnsureRootIp(rootIpOptions.Value.RootIps);
        if (authResult.IsSuccess)
        {
            return Result.Success();
        }

        if (targetUserId == requestingUserId)
        {
            return Result.Success();
        }

        var organizationId = await userRepository.GetOrganizationUserIdAsync(targetUserId, cancellationToken);
        if (organizationId == null)
        {
            return Result.Failure(DomainErrors.User.NotFound);
        }

        return await EnsureRootIpOrOwnerAsync(organizationId.Value, requestingUserId, cancellationToken);
    }
}
