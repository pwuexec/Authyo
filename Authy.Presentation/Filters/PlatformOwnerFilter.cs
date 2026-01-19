using System.Net;

namespace Authy.Presentation.Filters;

public class PlatformOwnerFilter(IConfiguration configuration) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var allowedIps = configuration.GetSection("PlatformOwner:AllowedIPs").Get<string[]>() ?? [];
        var remoteIp = context.HttpContext.Connection.RemoteIpAddress;

        if (remoteIp == null)
        {
            return Results.Forbid();
        }

        var remoteIpString = remoteIp.ToString();

        // Handle IPv6 loopback (::1) and IPv4 loopback (127.0.0.1)
        if (remoteIp.Equals(IPAddress.IPv6Loopback))
        {
            remoteIpString = "127.0.0.1";
        }

        if (!allowedIps.Contains(remoteIpString))
        {
            return Results.Forbid();
        }

        return await next(context);
    }
}
