using Authy.Presentation.Domain.Scopes.Data;
using Authy.Presentation.Shared;
using Authy.Presentation.Shared.Abstractions;

namespace Authy.Presentation.Domain.Scopes;

public record RoleOutput(Guid Id, string Name);
public record GetScopesOutput(Guid Id, string Name, List<RoleOutput> Roles);

public record GetScopesQuery(Guid OrganizationId, Guid UserId) : IQuery<Result<List<GetScopesOutput>>>;

public class GetScopesQueryHandler(
    IAuthorizationService authorizationService,
    IScopeRepository scopeRepository)
    : IQueryHandler<GetScopesQuery, Result<List<GetScopesOutput>>>
{
    public async Task<Result<List<GetScopesOutput>>> HandleAsync(GetScopesQuery query, CancellationToken cancellationToken)
    {
        var authResult = await authorizationService.EnsureRootIpOrOwnerAsync(
            query.OrganizationId, 
            query.UserId, 
            cancellationToken);

        if (authResult.IsFailure)
        {
            return Result.Failure<List<GetScopesOutput>>(authResult.Error);
        }

        var scopes = await scopeRepository.GetByOrganizationIdAsync(query.OrganizationId, cancellationToken);
        
        var output = scopes.Select(s => new GetScopesOutput(
            s.Id, 
            s.Name, 
            s.Roles.Select(r => new RoleOutput(r.Id, r.Name)).ToList()))
            .ToList();
        
        return Result.Success(output);
    }
}
