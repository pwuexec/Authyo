namespace Authy.Presentation.Domain.Users;

public record LoginRequest(Guid UserId);
public record RefreshTokenRequest(string AccessToken, string RefreshToken);
