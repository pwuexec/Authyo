using System.Security.Claims;
using Authy.Presentation.Domain.Scopes.Data;
using Authy.Presentation.Entitites;
using Authy.Presentation.Extensions;
using Authy.Presentation.Shared;
using Authy.Presentation.Shared.Abstractions;
using Authy.Presentation.Shared.Behaviors;
using Microsoft.AspNetCore.Mvc;

namespace Authy.Presentation.Domain.Scopes;

public static class ScopeEndpoints
{
    public record PutScopeRequest(string Name);

    public static void MapScopeEndpoints(this IEndpointRouteBuilder app)
    {
        var orgGroup = app.MapGroup("/organization")
            .WithTags("Organization")
            .WithDefaultApiResponses();

        orgGroup.MapPut("/{id:guid}/scope", async (IDispatcher dispatcher,
            Guid id, ClaimsPrincipal user, [FromBody] PutScopeRequest request, CancellationToken cancellationToken) =>
        {
            var userId = user.GetUserId();

            var command = new UpsertScopeCommand(
                id,
                request.Name,
                userId ?? Guid.Empty,
                new UpsertScopeFields(request.Name));
            var result = await dispatcher.DispatchAsync(command, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem();
        })
        .AddEndpointFilter<RootOrAuthenticatedFilter>()
        .WithName("UpsertScope")
        .WithSummary("Creates or updates a scope within an organization")
        .Produces<Scope>(StatusCodes.Status200OK);

        orgGroup.MapGet("/{id:guid}/scope", async (IDispatcher dispatcher,
            Guid id, ClaimsPrincipal user, CancellationToken cancellationToken) =>
        {
            var userId = user.GetUserId();

            var query = new GetScopesQuery(id, userId ?? Guid.Empty);
            var result = await dispatcher.DispatchAsync(query, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem();
        })
        .AddEndpointFilter<RootOrAuthenticatedFilter>()
        .WithName("GetScopes")
        .WithSummary("Retrieves all scopes for an organization")
        .Produces<List<GetScopesOutput>>(StatusCodes.Status200OK);
    }
}
