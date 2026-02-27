using System.Diagnostics;
using Authy.Application.Domain.Users.Data;
using Authy.Application.Shared.Abstractions;
using Authy.Application.Shared.Diagnostics;

namespace Authy.Application.Domain.Users;

public record GenerateTokenCommand(Guid UserId, string IpAddress = "", string UserAgent = "") : ICommand<Result<RefreshTokenCommandOutput>>;

public class GenerateTokenCommandHandler(
    IUserRepository userRepository,
    IJwtService jwtService,
    IRefreshTokenRepository refreshTokenRepository,
    TimeProvider timeProvider)
    : ICommandHandler<GenerateTokenCommand, Result<RefreshTokenCommandOutput>>
{
    public async Task<Result<RefreshTokenCommandOutput>> HandleAsync(GenerateTokenCommand command, CancellationToken cancellationToken)
    {
        var validationFailure = Validate(command).FailureOrNull<RefreshTokenCommandOutput>();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var user = await userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user == null)
        {
            Telemetry.AuthenticationAttempts.Add(1, new TagList { { "is_success", false }, { "error", "user_not_found" } });
            return Result.Failure<RefreshTokenCommandOutput>(DomainErrors.User.NotFound);
        }

        Telemetry.AuthenticationAttempts.Add(1, new TagList { { "is_success", true } });

        // Aggregate all scopes from all roles
        var scopes = await userRepository.GetScopesAsync(user.Id, cancellationToken);

        var (accessToken, jti) = jwtService.GenerateToken(user.Id, user.Name, scopes);
        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()), // Simple random token
            JwtId = jti,
            CreatedOn = utcNow,
            ExpiresOn = utcNow.AddDays(7), // Configurable?
            IpAddress = command.IpAddress,
            UserAgent = command.UserAgent
        };

        await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);

        return Result.Success(new RefreshTokenCommandOutput(accessToken, refreshToken.Token));
    }

    private static List<Error> Validate(GenerateTokenCommand command)
    {
        var errors = new List<Error>();

        if (command.UserId == Guid.Empty)
        {
            errors.Add(DomainErrors.User.NotFound);
        }

        return errors;
    }
}

