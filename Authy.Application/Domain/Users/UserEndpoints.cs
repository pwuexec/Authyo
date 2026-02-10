using System.Security.Claims;
using Authy.Application.Extensions;
using Authy.Application.Shared.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Authy.Application.Domain.Users;

public static class UserEndpoints
{
    public record LoginRequest(Guid UserId);
    public record RefreshTokenRequest(string AccessToken, string RefreshToken);
    public record CreateOrganizationUserRequest(string Name);
    public record UpdateOrganizationUserRequest(string Name);

    extension(IEndpointRouteBuilder app)
    {
        public IEndpointRouteBuilder MapAuthenticationEndpoints()
        {
            var authGroup = app.MapGroup("/")
                .WithTags("Authentication")
                .WithDefaultApiResponses();

            authGroup.MapPost("/login", async (IDispatcher dispatcher, HttpContext httpContext,
                    [FromBody] LoginRequest request, CancellationToken cancellationToken) =>
                {
                    var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                    var userAgent = httpContext.Request.Headers.UserAgent.ToString();

                    var command = new GenerateTokenCommand(request.UserId, ipAddress, userAgent);
                    var result = await dispatcher.DispatchAsync(command, cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.ToProblem();
                })
                .WithName("Login")
                .WithSummary("Logs in a user by generating a token")
                .Produces<RefreshTokenCommandOutput>(StatusCodes.Status200OK);

            authGroup.MapPost("/refresh", async (IDispatcher dispatcher, HttpContext httpContext,
                    [FromBody] RefreshTokenRequest request, CancellationToken cancellationToken) =>
                {
                    var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                    var userAgent = httpContext.Request.Headers.UserAgent.ToString();

                    var command = new RefreshTokenCommand(request.AccessToken, request.RefreshToken, ipAddress, userAgent);
                    var result = await dispatcher.DispatchAsync(command, cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.ToProblem();
                })
                .WithName("RefreshToken")
                .WithSummary("Refreshes the access token using a refresh token")
                .Produces<RefreshTokenCommandOutput>(StatusCodes.Status200OK);

            return app;
        }

        public IEndpointRouteBuilder MapUserEndpoints()
        {
            var usersGroup = app.MapGroup("/users")
                .WithTags("Users")
                .WithDefaultApiResponses();

            usersGroup.MapGet("/{userId:guid}/sessions", async (IDispatcher dispatcher,
                    Guid userId, ClaimsPrincipal user, CancellationToken cancellationToken) =>
                {
                    var requestingUserId = user.GetUserId() ?? Guid.Empty;
                    var query = new GetSessionsQuery(userId, requestingUserId);
                    var result = await dispatcher.DispatchAsync(query, cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.ToProblem();
                })
                .WithName("GetUserSessions")
                .WithSummary("Gets active sessions for a specific user")
                .Produces<List<RefreshToken>>(StatusCodes.Status200OK);

            var organizationGroup = app.MapGroup("/organization")
                .WithTags("Organization")
                .WithDefaultApiResponses();

            organizationGroup.MapGet("/{id:guid}/users", async (IDispatcher dispatcher,
                    Guid id, ClaimsPrincipal user, CancellationToken cancellationToken) =>
                {
                    var requestingUserId = user.GetUserId() ?? Guid.Empty;
                    var query = new GetOrganizationUsersQuery(id, requestingUserId);
                    var result = await dispatcher.DispatchAsync(query, cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.ToProblem();
                })
                .WithName("GetOrganizationUsers")
                .WithSummary("Gets users for a specific organization")
                .Produces<List<OrganizationUserOutput>>(StatusCodes.Status200OK);

            organizationGroup.MapPost("/{id:guid}/users", async (IDispatcher dispatcher,
                    Guid id, ClaimsPrincipal user, [FromBody] CreateOrganizationUserRequest request,
                    CancellationToken cancellationToken) =>
                {
                    var requestingUserId = user.GetUserId() ?? Guid.Empty;
                    var command = new CreateOrganizationUserCommand(id, request.Name, requestingUserId);
                    var result = await dispatcher.DispatchAsync(command, cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.ToProblem();
                })
                .WithName("CreateOrganizationUser")
                .WithSummary("Creates a user within an organization")
                .Produces<OrganizationUserOutput>(StatusCodes.Status200OK);

            organizationGroup.MapPut("/{id:guid}/users/{userId:guid}", async (IDispatcher dispatcher,
                    Guid id, Guid userId, ClaimsPrincipal user, [FromBody] UpdateOrganizationUserRequest request,
                    CancellationToken cancellationToken) =>
                {
                    var requestingUserId = user.GetUserId() ?? Guid.Empty;
                    var command = new UpdateOrganizationUserCommand(id, userId, request.Name, requestingUserId);
                    var result = await dispatcher.DispatchAsync(command, cancellationToken);

                    return result.IsSuccess
                        ? Results.Ok(result.Value)
                        : result.ToProblem();
                })
                .WithName("UpdateOrganizationUser")
                .WithSummary("Updates a user within an organization")
                .Produces<OrganizationUserOutput>(StatusCodes.Status200OK);

            organizationGroup.MapDelete("/{id:guid}/users/{userId:guid}", async (IDispatcher dispatcher,
                    Guid id, Guid userId, ClaimsPrincipal user, CancellationToken cancellationToken) =>
                {
                    var requestingUserId = user.GetUserId() ?? Guid.Empty;
                    var command = new DeleteOrganizationUserCommand(id, userId, requestingUserId);
                    var result = await dispatcher.DispatchAsync(command, cancellationToken);

                    return result.IsSuccess
                        ? Results.NoContent()
                        : result.ToProblem();
                })
                .WithName("DeleteOrganizationUser")
                .WithSummary("Deletes a user within an organization")
                .Produces(StatusCodes.Status204NoContent);

            return app;
        }

        public IEndpointRouteBuilder MapSessionEndpoints()
        {
            var sessionsGroup = app.MapGroup("/sessions")
                .WithTags("Sessions")
                .WithDefaultApiResponses();

            sessionsGroup.MapDelete("/{id:guid}", async (IDispatcher dispatcher,
                    Guid id, ClaimsPrincipal user, CancellationToken cancellationToken) =>
                {
                    var requestingUserId = user.GetUserId() ?? Guid.Empty;
                    var command = new RevokeSessionCommand(id, requestingUserId);
                    var result = await dispatcher.DispatchAsync(command, cancellationToken);

                    return result.IsSuccess
                        ? Results.NoContent()
                        : result.ToProblem();
                })
                .WithName("RevokeSession")
                .WithSummary("Revokes a specific session")
                .Produces(StatusCodes.Status204NoContent);

            return app;
        }
    }
}
