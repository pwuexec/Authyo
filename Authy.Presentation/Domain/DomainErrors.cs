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
    }
}
