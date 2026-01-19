using System.Net;
using Authy.Presentation.Data;
using Microsoft.EntityFrameworkCore;

namespace Authy.Presentation.Filters;

public class AuthorizeOrgActionFilter(string requiredScope) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var db = context.HttpContext.RequestServices.GetRequiredService<AuthyDbContext>();

        // Check if platform owner
        var allowedIps = configuration.GetSection("PlatformOwner:AllowedIPs").Get<string[]>() ?? [];
        var remoteIp = context.HttpContext.Connection.RemoteIpAddress;
        
        if (remoteIp != null)
        {
            var remoteIpString = remoteIp.ToString();
            if (remoteIp.Equals(IPAddress.IPv6Loopback))
            {
                remoteIpString = "127.0.0.1";
            }
            if (allowedIps.Contains(remoteIpString))
            {
                return await next(context);
            }
        }

        // Check if user belongs to the organization and has the required scope
        var userIdHeader = context.HttpContext.Request.Headers["X-User-Id"].FirstOrDefault();
        if (string.IsNullOrEmpty(userIdHeader) || !Guid.TryParse(userIdHeader, out var userId))
        {
            return Results.Unauthorized();
        }

        if (!context.HttpContext.Request.RouteValues.TryGetValue("orgId", out var orgIdObj) || !Guid.TryParse(orgIdObj?.ToString(), out var orgId))
        {
            return Results.BadRequest("Organization ID is required in route");
        }

        var user = await db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .ThenInclude(r => r!.RoleScopes)
            .ThenInclude(rs => rs.Scope)
            .FirstOrDefaultAsync(u => u.Id == userId && u.OrganizationId == orgId);

        if (user == null)
        {
            return Results.Forbid();
        }

        var hasScope = user.UserRoles
            .SelectMany(ur => ur.Role!.RoleScopes.Select(rs => rs.Scope!.Name))
            .Distinct()
            .Contains(requiredScope);

        if (!hasScope)
        {
            return Results.Forbid();
        }

        return await next(context);
    }
}

public static class AuthorizeOrgActionExtensions
{
    public static RouteHandlerBuilder AuthorizeOrgAction(this RouteHandlerBuilder builder, string scope)
    {
        return builder.AddEndpointFilter(new AuthorizeOrgActionFilter(scope));
    }
}
