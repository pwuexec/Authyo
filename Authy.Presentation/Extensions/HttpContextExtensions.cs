using Authy.Presentation.Domain;
using Authy.Presentation.Shared;

namespace Authy.Presentation.Extensions;

public static class HttpContextExtensions
{
    public static Result EnsureRootIp(this HttpContext? httpContext, IConfiguration configuration)
    {
        if (httpContext == null)
        {
            return Result.Failure(DomainErrors.User.UnauthorizedIp);
        }

        var rootIps = configuration.GetSection("RootIps").Get<string[]>() ?? Array.Empty<string>();
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
