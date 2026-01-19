using Authy.Presentation.Data;
using Microsoft.EntityFrameworkCore;

namespace Authy.Presentation.Filters;

public class RequireScopeFilter(string requiredScope) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var db = context.HttpContext.RequestServices.GetRequiredService<AuthyDbContext>();

        // Get user ID from header
        var userIdHeader = context.HttpContext.Request.Headers["X-User-Id"].FirstOrDefault();
        if (string.IsNullOrEmpty(userIdHeader) || !Guid.TryParse(userIdHeader, out var userId))
        {
            return Results.Unauthorized();
        }

        // Get user's scopes through their roles
        var userScopes = await db.UserRoles
            .Where(ur => ur.UserId == userId)
            .Include(ur => ur.Role)
            .ThenInclude(r => r!.RoleScopes)
            .ThenInclude(rs => rs.Scope)
            .SelectMany(ur => ur.Role!.RoleScopes.Select(rs => rs.Scope!.Name))
            .Distinct()
            .ToListAsync();

        // Check if user has the required scope
        if (!userScopes.Contains(requiredScope))
        {
            return Results.Forbid();
        }

        return await next(context);
    }
}

public static class RequireScopeExtensions
{
    public static RouteHandlerBuilder RequireScope(this RouteHandlerBuilder builder, string scope)
    {
        return builder.AddEndpointFilter(new RequireScopeFilter(scope));
    }
}
