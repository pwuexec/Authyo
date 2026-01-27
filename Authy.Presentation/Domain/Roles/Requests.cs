namespace Authy.Presentation.Domain.Roles;

public record PutRoleRequest(string Name, List<string> Scopes);
