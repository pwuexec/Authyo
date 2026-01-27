using Authy.Presentation.Shared;
using Authy.Presentation.Shared.Abstractions;

namespace Authy.Presentation.Domain.Roles;

public record ScopeOutput(Guid Id, string Name);
public record GetRolesOutput(Guid Id, string Name, List<ScopeOutput> Scopes);

public record GetRolesQuery(Guid OrganizationId, Guid UserId) : IQuery<Result<List<GetRolesOutput>>>;

public class GetRolesQueryHandler(
    IAuthorizationService authorizationService,
    IRoleRepository roleRepository)
    : IQueryHandler<GetRolesQuery, Result<List<GetRolesOutput>>>
{
    public async Task<Result<List<GetRolesOutput>>> HandleAsync(GetRolesQuery query, CancellationToken cancellationToken)
    {
        var authResult = await authorizationService.EnsureRootIpOrOwnerAsync(
            query.OrganizationId, 
            query.UserId, 
            cancellationToken);

        if (authResult.IsFailure)
        {
            return Result.Failure<List<GetRolesOutput>>(authResult.Error);
        }

        var roles = await roleRepository.GetByOrganizationIdAsync(query.OrganizationId, cancellationToken);
        
        var roleDtos = roles.Select(r => new GetRolesOutput(
            r.Id, 
            r.Name, 
            r.Scopes.Select(s => new ScopeOutput(s.Id, s.Name)).ToList()))
            .ToList();
        
        return Result.Success(roleDtos);
    }
}
