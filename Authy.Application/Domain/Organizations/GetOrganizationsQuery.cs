using Authy.Application.Domain.Organizations.Data;
using Authy.Application.Extensions;
using Authy.Application.Shared.Abstractions;

namespace Authy.Application.Domain.Organizations;

public record GetOrganizationsQuery() : IQuery<Result<List<GetOrganizationsOutput>>>;

public record GetOrganizationsOutput(Guid Id, string Name);

public class GetOrganizationsQueryHandler(
    IOrganizationRepository organizationRepository,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration)
    : IQueryHandler<GetOrganizationsQuery, Result<List<GetOrganizationsOutput>>>
{
    public async Task<Result<List<GetOrganizationsOutput>>> HandleAsync(GetOrganizationsQuery query, CancellationToken cancellationToken)
    {
        var authResult = httpContextAccessor.HttpContext.EnsureRootIp(configuration);
        if (authResult.IsFailure)
        {
            return Result.Failure<List<GetOrganizationsOutput>>(authResult.Error);
        }

        var organizations = await organizationRepository.GetAllAsync(cancellationToken);

        var dtos = organizations
            .Select(o => new GetOrganizationsOutput(o.Id, o.Name))
            .ToList();

        return Result.Success(dtos);
    }
}

