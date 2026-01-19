using Authy.Presentation.Models;

namespace Authy.Presentation.Authorization;

public static class DefaultRoles
{
    private static readonly string[] AdminScopes =
    [
        Scopes.UsersCreate, Scopes.UsersRead, Scopes.UsersEdit, Scopes.UsersDelete,
        Scopes.RolesCreate, Scopes.RolesRead, Scopes.RolesEdit, Scopes.RolesDelete,
        Scopes.OrganizationsRead, Scopes.OrganizationsEdit,
        Scopes.ScopesRead, Scopes.ScopesEdit, Scopes.ScopesDelete
    ];

    private static readonly string[] MemberScopes = [Scopes.UsersRead];

    public static (List<Role> Roles, List<RoleScope> RoleScopes) CreateDefaultRoles(
        Organization organization,
        Dictionary<string, Scope> scopeEntities)
    {
        var adminRole = new Role(organization)
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Name = "Admin",
            Description = "Organization administrator with full access",
            CreatedAt = DateTime.UtcNow
        };

        var memberRole = new Role(organization)
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Name = "Member",
            Description = "Standard member with read-only access",
            CreatedAt = DateTime.UtcNow
        };

        var roleScopes = new List<RoleScope>();

        // Add admin scopes
        foreach (var scopeName in AdminScopes)
        {
            if (scopeEntities.TryGetValue(scopeName, out var scope))
            {
                roleScopes.Add(new RoleScope { RoleId = adminRole.Id, ScopeId = scope.Id, Role = adminRole, Scope = scope });
            }
        }

        // Add member scopes
        foreach (var scopeName in MemberScopes)
        {
            if (scopeEntities.TryGetValue(scopeName, out var scope))
            {
                roleScopes.Add(new RoleScope { RoleId = memberRole.Id, ScopeId = scope.Id, Role = memberRole, Scope = scope });
            }
        }

        return ([adminRole, memberRole], roleScopes);
    }
}
