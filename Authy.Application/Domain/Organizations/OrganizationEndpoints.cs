using Authy.Application.Extensions;
using Authy.Application.Shared.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Authy.Application.Domain.Organizations;

public static class OrganizationEndpoints
{
    public record PostOrganizationRequest(string Name);

    public static IEndpointRouteBuilder MapOrganizationEndpoints(this IEndpointRouteBuilder app)
    {
        var orgGroup = app.MapGroup("/organization")
            .WithTags("Organization")
            .WithDefaultApiResponses();

        orgGroup.MapPost("", async (IDispatcher dispatcher,
            [FromBody] PostOrganizationRequest request, CancellationToken cancellationToken) =>
        {
            var command = new CreateOrganizationCommand(request.Name);
            var result = await dispatcher.DispatchAsync(command, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem();
        })
        .WithName("CreateOrganization")
        .WithSummary("Creates a new organization")
        .Produces<Organization>(StatusCodes.Status200OK);

        orgGroup.MapGet("", async (IDispatcher dispatcher, CancellationToken cancellationToken) =>
        {
            var query = new GetOrganizationsQuery();
            var result = await dispatcher.DispatchAsync(query, cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : result.ToProblem();
        })
        .WithName("GetOrganizations")
        .WithSummary("Retrieves all organizations")
        .Produces<List<GetOrganizationsOutput>>(StatusCodes.Status200OK);

        return app;
    }
}

