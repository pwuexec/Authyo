using System.Security.Claims;
using Authy.Presentation.Domain.Roles.Data;
using Authy.Presentation.Entitites;
using Authy.Presentation.Extensions;
using Authy.Presentation.Shared;
using Authy.Presentation.Shared.Abstractions;
using Authy.Presentation.Shared.Behaviors;
using Microsoft.AspNetCore.Mvc;

namespace Authy.Presentation.Domain.Roles;

public static class RoleEndpoints
{
    public record PutRoleRequest(string Name, List<string> Scopes);

    public static void MapRoleEndpoints(this IEndpointRouteBuilder app)
    {
        var orgGroup = app.MapGroup("/organization")
            .WithTags("Organization")
            .WithDefaultApiResponses();

        orgGroup.MapPut("/{id:guid}/role", async (IDispatcher dispatcher,
            Guid id, ClaimsPrincipal user, [FromBody] PutRoleRequest request, CancellationToken cancellationToken) =>
        {
            var userId = user.GetUserId();
            var command = new UpsertRoleCommand(id, request.Name, request.Scopes, userId ?? Guid.Empty);
            var result = await dispatcher.DispatchAsync(command, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem();
        })
        .AddEndpointFilter<RootOrAuthenticatedFilter>()
        .WithName("UpsertRole")
        .WithSummary("Creates or updates a role within an organization")
        .Produces<Role>(StatusCodes.Status200OK);

        orgGroup.MapGet("/{id:guid}/role", async (IDispatcher dispatcher,
            Guid id, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var userId = user.GetUserId();

            var query = new GetRolesQuery(id, userId ?? Guid.Empty);
            var result = await dispatcher.DispatchAsync(query, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem();
        })
        .AddEndpointFilter<RootOrAuthenticatedFilter>()
        .WithName("GetRoles")
        .WithSummary("Retrieves all roles for an organization")
        .Produces<List<GetRolesOutput>>(StatusCodes.Status200OK);
    }
}
