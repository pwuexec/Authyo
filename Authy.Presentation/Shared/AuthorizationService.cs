using Authy.Presentation.Domain;
using Authy.Presentation.Domain.Organizations;
using Authy.Presentation.Extensions;
using Authy.Presentation.Shared.Abstractions;

namespace Authy.Presentation.Shared;

public class AuthorizationService : IAuthorizationService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IConfiguration _configuration;
    private readonly IOrganizationRepository _organizationRepository;

    public AuthorizationService(
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        IOrganizationRepository organizationRepository)
    {
        _httpContextAccessor = httpContextAccessor;
        _configuration = configuration;
        _organizationRepository = organizationRepository;
    }

    public async Task<Result> EnsureRootIpOrOwnerAsync(Guid organizationId, Guid userId, CancellationToken cancellationToken)
    {
        var authResult = _httpContextAccessor.HttpContext.EnsureRootIp(_configuration);
        if (authResult.IsSuccess)
        {
            return Result.Success();
        }

        var org = await _organizationRepository.GetByIdAsync(organizationId, cancellationToken);
        if (org == null || !org.Owners.Any(o => o.Id == userId))
        {
            return Result.Failure(DomainErrors.User.UnauthorizedOwner);
        }

        return Result.Success();
    }
}
