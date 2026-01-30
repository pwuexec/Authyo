using Authy.Application.Domain.Users.Data;
using Authy.Application.Shared.Abstractions;

namespace Authy.Application.Domain.Users;

public record GetSessionsQuery(Guid TargetUserId, Guid RequestingUserId) : IQuery<Result<List<RefreshToken>>>;

public class GetSessionsQueryHandler(
    IAuthorizationService authorizationService,
    IRefreshTokenRepository refreshTokenRepository)
    : IQueryHandler<GetSessionsQuery, Result<List<RefreshToken>>>
{
    public async Task<Result<List<RefreshToken>>> HandleAsync(GetSessionsQuery query, CancellationToken cancellationToken)
    {
        var validationFailure = Validate(query).FailureOrNull<List<RefreshToken>>();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var authResult = await authorizationService.EnsureCanManageUserAsync(
            query.TargetUserId, 
            query.RequestingUserId, 
            cancellationToken);

        if (authResult.IsFailure)
        {
            return Result.Failure<List<RefreshToken>>(authResult.Error);
        }

        var tokens = await refreshTokenRepository.GetByUserIdAsync(query.TargetUserId, cancellationToken);
        
        return Result.Success(tokens);
    }

    private static List<Error> Validate(GetSessionsQuery query)
    {
        var errors = new List<Error>();

        if (query.TargetUserId == Guid.Empty)
        {
             errors.Add(DomainErrors.User.NotFound);
        }

        return errors;
    }
}

