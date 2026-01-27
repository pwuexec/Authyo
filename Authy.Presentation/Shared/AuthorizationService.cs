using Authy.Presentation.Domain;
using Authy.Presentation.Domain.Organizations;
using Authy.Presentation.Extensions;
using Authy.Presentation.Shared.Abstractions;

namespace Authy.Presentation.Shared;

public class AuthorizationService(
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration,
    IOrganizationRepository organizationRepository)
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
}
