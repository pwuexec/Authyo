namespace Authy.Presentation.Authorization;

public static class Scopes
{
    // User management
    public const string UsersCreate = "users:create";
    public const string UsersRead = "users:read";
    public const string UsersEdit = "users:edit";
    public const string UsersDelete = "users:delete";

    // Role management
    public const string RolesCreate = "roles:create";
    public const string RolesRead = "roles:read";
    public const string RolesEdit = "roles:edit";
    public const string RolesDelete = "roles:delete";

    public const string OrganizationsRead = "organizations:read";
    public const string OrganizationsEdit = "organizations:edit";

    // Scope management
    public const string ScopesRead = "scopes:read";
    public const string ScopesEdit = "scopes:edit";
    public const string ScopesDelete = "scopes:delete";

    public static readonly string[] All =
    [
        UsersCreate, UsersRead, UsersEdit, UsersDelete,
        RolesCreate, RolesRead, RolesEdit, RolesDelete,
        OrganizationsRead, OrganizationsEdit,
        ScopesRead, ScopesEdit, ScopesDelete
    ];
}
