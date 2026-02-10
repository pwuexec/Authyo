using Authy.Application.Domain.Users.Data;
using Authy.Application.Shared.Abstractions;

namespace Authy.Application.Domain.Users;

public record OrganizationUserOutput(Guid Id, string Name, Guid OrganizationId);

public record GetOrganizationUsersQuery(Guid OrganizationId, Guid RequestingUserId)
    : IQuery<Result<List<OrganizationUserOutput>>>;

public class GetOrganizationUsersQueryHandler(
    IAuthorizationService authorizationService,
    IUserRepository userRepository)
    : IQueryHandler<GetOrganizationUsersQuery, Result<List<OrganizationUserOutput>>>
{
    public async Task<Result<List<OrganizationUserOutput>>> HandleAsync(
        GetOrganizationUsersQuery query,
        CancellationToken cancellationToken)
    {
        var authResult = await authorizationService.EnsureRootIpOrOwnerAsync(
            query.OrganizationId,
            query.RequestingUserId,
            cancellationToken);

        if (authResult.IsFailure)
        {
            return Result.Failure<List<OrganizationUserOutput>>(authResult.Error);
        }

        var users = await userRepository.GetByOrganizationIdAsync(query.OrganizationId, cancellationToken);

        var dtos = users
            .Select(u => new OrganizationUserOutput(u.Id, u.Name, u.OrganizationId))
            .ToList();

        return Result.Success(dtos);
    }
}
