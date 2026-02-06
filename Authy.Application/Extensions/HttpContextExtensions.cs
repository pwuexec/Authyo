using System.Security.Claims;

namespace Authy.Application.Extensions;

public static class HttpContextExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(value, out var result) ? result : null;
    }

    public static Result EnsureRootIp(this HttpContext? httpContext, string[] rootIps)
    {
        if (httpContext == null)
        {
            return Result.Failure(DomainErrors.User.UnauthorizedIp);
        }

        var remoteIp = httpContext.Connection.RemoteIpAddress;
        
        if (remoteIp == null)
        {
            return Result.Failure(DomainErrors.User.UnauthorizedIp);
        }

        var remoteIpString = remoteIp.ToString();
        if (remoteIp.IsIPv4MappedToIPv6)
        {
            remoteIpString = remoteIp.MapToIPv4().ToString();
        }

        if (!rootIps.Contains(remoteIpString))
        {
            return Result.Failure(DomainErrors.User.UnauthorizedIp);
        }

        return Result.Success();
    }
}

