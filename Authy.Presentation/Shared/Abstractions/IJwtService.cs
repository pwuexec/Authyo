namespace Authy.Presentation.Shared.Abstractions;

public interface IJwtService
{
    string GenerateToken(Guid userId, string userName, List<string> scopes);
}
