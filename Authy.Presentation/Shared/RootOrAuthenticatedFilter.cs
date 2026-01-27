using Authy.Presentation.Extensions;

namespace Authy.Presentation.Shared;

public class RootOrAuthenticatedFilter(IConfiguration configuration) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        // 1. Check if authenticated via JWT
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            return await next(context);
        }

        // 2. Check if Root IP
        var rootIpResult = context.HttpContext.EnsureRootIp(configuration);
        if (rootIpResult.IsSuccess)
        {
            return await next(context);
        }

        return Results.Unauthorized();
    }
}
