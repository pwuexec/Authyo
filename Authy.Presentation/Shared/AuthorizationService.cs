using Authy.Presentation.Domain;
using Authy.Presentation.Domain.Organizations.Data;
using Authy.Presentation.Domain.Users.Data;
using Authy.Presentation.Extensions;
using Authy.Presentation.Shared.Abstractions;

namespace Authy.Presentation.Shared;

public class AuthorizationService(
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration,
    IOrganizationRepository organizationRepository,
    IUserRepository userRepository)
    : IAuthorizationService
{
    public async Task<Result> EnsureRootIpOrOwnerAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        var authResult = httpContextAccessor.HttpContext.EnsureRootIp(configuration);
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
        var authResult = httpContextAccessor.HttpContext.EnsureRootIp(configuration);
        if (authResult.IsSuccess)
        {
            return Result.Success();
        }

        if (targetUserId == requestingUserId)
        {
            return Result.Success();
        }

        var targetUser = await userRepository.GetByIdAsync(targetUserId, cancellationToken);
        if (targetUser == null)
        {
            return Result.Failure(DomainErrors.User.NotFound);
        }

        return await EnsureRootIpOrOwnerAsync(targetUser.OrganizationId, requestingUserId, cancellationToken);
    }
}
