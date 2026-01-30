using System.Security.Claims;
using Authy.Application.Extensions;
using Authy.Application.Shared.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace Authy.Application.Domain.Users;

public static class UserEndpoints
{
    public record LoginRequest(Guid UserId);
    public record RefreshTokenRequest(string AccessToken, string RefreshToken);

    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder app)
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

    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
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

        return app;
    }

    public static IEndpointRouteBuilder MapSessionEndpoints(this IEndpointRouteBuilder app)
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

