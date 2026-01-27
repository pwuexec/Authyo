using Authy.Presentation.Domain.Users.Data;
using Authy.Presentation.Entitites;
using Authy.Presentation.Shared;
using Authy.Presentation.Shared.Abstractions;

namespace Authy.Presentation.Domain.Users;

public record RefreshTokenCommandOutput(string AccessToken, string RefreshToken);

public record RefreshTokenCommand(string AccessToken, string RefreshToken, string IpAddress = "", string UserAgent = "") : ICommand<Result<RefreshTokenCommandOutput>>;

public class RefreshTokenCommandHandler(
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IJwtService jwtService,
    TimeProvider timeProvider)
    : ICommandHandler<RefreshTokenCommand, Result<RefreshTokenCommandOutput>>
{
    public async Task<Result<RefreshTokenCommandOutput>> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var validationFailure = Validate(command).FailureOrNull<RefreshTokenCommandOutput>();
        if (validationFailure is not null)
        {
            return validationFailure;
        }

        var storedRefreshToken = await refreshTokenRepository.GetByTokenAsync(command.RefreshToken, cancellationToken);
        
        if (storedRefreshToken == null)
        {
            return Result.Failure<RefreshTokenCommandOutput>(DomainErrors.RefreshToken.Invalid);
        }

        var utcNow = timeProvider.GetUtcNow().UtcDateTime;

        if (storedRefreshToken.IsRevoked)
        {
             return Result.Failure<RefreshTokenCommandOutput>(DomainErrors.RefreshToken.Revoked);
        }

        if (storedRefreshToken.IsExpired(utcNow))
        {
             return Result.Failure<RefreshTokenCommandOutput>(DomainErrors.RefreshToken.Expired);
        }

        var handler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        if (!handler.CanReadToken(command.AccessToken))
        {
             return Result.Failure<RefreshTokenCommandOutput>(DomainErrors.AccessToken.Invalid);
        }

        var jwtToken = handler.ReadJwtToken(command.AccessToken);
        var jti = jwtToken.Claims.FirstOrDefault(c => c.Type == System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Jti)?.Value;

        if (jti != storedRefreshToken.JwtId)
        {
             return Result.Failure<RefreshTokenCommandOutput>(DomainErrors.RefreshToken.Mismatch);
        }

        var user = await userRepository.GetByIdAsync(storedRefreshToken.UserId, cancellationToken);
        if (user == null)
        {
             return Result.Failure<RefreshTokenCommandOutput>(DomainErrors.User.NotFound);
        }

        storedRefreshToken.RevokedOn = utcNow;
        
        var scopes = user.Roles
            .SelectMany(r => r.Scopes)
            .Select(s => s.Name)
            .Distinct()
            .ToList();

        var (newAccessToken, newJti) = jwtService.GenerateToken(user.Id, user.Name, scopes);

        var newRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
            JwtId = newJti,
            CreatedOn = utcNow,
            ExpiresOn = utcNow.AddDays(7),
            IpAddress = command.IpAddress,
            UserAgent = command.UserAgent
        };
        
        storedRefreshToken.ReplacedByTokenId = newRefreshToken.Id;

        await refreshTokenRepository.UpdateAsync(storedRefreshToken, cancellationToken);
        await refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        return Result.Success(new RefreshTokenCommandOutput(newAccessToken, newRefreshToken.Token));
    }

    private static List<Error> Validate(RefreshTokenCommand command)
    {
        var errors = new List<Error>();

        if (string.IsNullOrWhiteSpace(command.AccessToken))
        {
            errors.Add(DomainErrors.AccessToken.Invalid);
        }

        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            errors.Add(DomainErrors.RefreshToken.Invalid);
        }

        return errors;
    }
}
