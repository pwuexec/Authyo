using Authy.Presentation.Domain.Users.Data;
using Authy.Presentation.Shared;
using Authy.Presentation.Shared.Abstractions;

namespace Authy.Presentation.Domain.Users;

public record RevokeSessionCommand(Guid RefreshTokenId, Guid RequestingUserId) : ICommand<Result>;

public class RevokeSessionCommandHandler(
    IAuthorizationService authorizationService,
    IRefreshTokenRepository refreshTokenRepository,
    TimeProvider timeProvider)
    : ICommandHandler<RevokeSessionCommand, Result>
{
    public async Task<Result> HandleAsync(RevokeSessionCommand command, CancellationToken cancellationToken)
    {
        var validationFailure = Validate(command).FailureOrNull();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var refreshToken = await refreshTokenRepository.GetByIdAsync(command.RefreshTokenId, cancellationToken);
        if (refreshToken == null)
        {
            return Result.Failure(DomainErrors.RefreshToken.NotFound);
        }

        var authResult = await authorizationService.EnsureCanManageUserAsync(
            refreshToken.UserId, 
            command.RequestingUserId, 
            cancellationToken);

        if (authResult.IsFailure)
        {
            return authResult;
        }

        refreshToken.RevokedOn = timeProvider.GetUtcNow().UtcDateTime;
        
        await refreshTokenRepository.UpdateAsync(refreshToken, cancellationToken);

        return Result.Success();
    }

    private static List<Error> Validate(RevokeSessionCommand command)
    {
        var errors = new List<Error>();

        if (command.RefreshTokenId == Guid.Empty)
        {
            errors.Add(DomainErrors.RefreshToken.Invalid);
        }

        return errors;
    }
}
