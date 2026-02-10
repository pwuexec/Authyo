namespace Authy.Application.Domain;

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

        public static readonly Error Unauthorized = new(
            "User.Unauthorized",
            "User is not authorized");

        public static readonly Error NameEmpty = new(
            "User.NameEmpty",
            "User name cannot be empty");

        public static readonly Error CannotRemoveSelf = new(
            "User.CannotRemoveSelf",
            "User cannot remove themselves");
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

        public static readonly Error NotFound = new(
            "Organization.NotFound",
            "Organization not found");
    }

    public static class RefreshToken
    {
        public static readonly Error NotFound = new(
            "RefreshToken.NotFound",
            "Refresh token not found");

        public static readonly Error Invalid = new(
            "RefreshToken.Invalid",
            "Invalid refresh token");

        public static readonly Error Revoked = new(
            "RefreshToken.Revoked",
            "Refresh token has been revoked");

        public static readonly Error Expired = new(
            "RefreshToken.Expired",
            "Refresh token has expired");

        public static readonly Error Mismatch = new(
            "RefreshToken.Mismatch",
            "Refresh token does not match the access token");
    }

    public static class AccessToken
    {
        public static readonly Error Invalid = new(
            "AccessToken.Invalid",
            "Invalid access token format");
    }
}
