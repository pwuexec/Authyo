using Authy.Presentation.Shared;

namespace Authy.Presentation.Domain;

public static class DomainErrors
{
    public static class User
    {
        public static readonly Error UnauthorizedIp = new(
            "User.UnauthorizedIp",
            "IP is not authorized");

        public static readonly Error UnauthorizedOwner = new(
            "User.UnauthorizedOwner",
            "User is not an owner of the organization");

        public static readonly Error NotFound = new(
            "User.NotFound",
            "User not found");
    }

    public static class Role
    {
        public static readonly Error ScopesRequired = new(
            "Role.ScopesRequired",
            "At least one scope is required for a role");

        public static Error ScopeNotFound(string name) => new(
            "Role.ScopeNotFound",
            $"Scope '{name}' was not found");
    }

    public static class Scope
    {
        public static readonly Error NameEmpty = new(
            "Scope.NameEmpty",
            "Scope name cannot be empty");
    }

    public static class Organization
    {
        public static readonly Error NameEmpty = new(
            "Organization.NameEmpty",
            "Organization name cannot be empty");
    }
}
