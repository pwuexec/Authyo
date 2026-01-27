namespace Authy.Presentation.Shared.Abstractions;

public interface IJwtService
{
    (string Token, string Jti) GenerateToken(Guid userId, string userName, List<string> scopes);
}
