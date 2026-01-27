using System.Security.Claims;
using Authy.Presentation;
using Authy.Presentation.Domain.Organizations;
using Authy.Presentation.Domain.Organizations.Data;
using Authy.Presentation.Domain.Roles;
using Authy.Presentation.Domain.Roles.Data;
using Authy.Presentation.Domain.Scopes;
using Authy.Presentation.Domain.Scopes.Data;
using Authy.Presentation.Domain.Users;
using Authy.Presentation.Domain.Users.Data;
using Authy.Presentation.Entitites;
using Authy.Presentation.Extensions;
using Authy.Presentation.Shared;
using Authy.Presentation.Shared.Abstractions;
using Authy.Presentation.Shared.Behaviors;
using Microsoft.AspNetCore.Mvc;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IOrganizationRepository, MockRepository>();
builder.Services.AddScoped<IRoleRepository, MockRepository>();
builder.Services.AddScoped<IScopeRepository, MockRepository>();
builder.Services.AddScoped<IUserRepository, MockRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, MockRepository>(); // Added
builder.Services.AddScoped<IUnitOfWork, MockRepository>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddSingleton(TimeProvider.System);

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));

builder.Services.AddScoped<IDispatcher, Dispatcher>();
builder.Services.AddHandlers(typeof(Program).Assembly);

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

var authGroup = app.MapGroup("/").WithTags("Authentication");

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
.Produces<RefreshTokenCommandOutput>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status400BadRequest);

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
.Produces<RefreshTokenCommandOutput>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status400BadRequest);

var usersGroup = app.MapGroup("/users").WithTags("Users");

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
.AddEndpointFilter<RootOrAuthenticatedFilter>()
.WithName("GetUserSessions")
.WithSummary("Gets active sessions for a specific user")
.Produces<List<RefreshToken>>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status403Forbidden);

var sessionsGroup = app.MapGroup("/sessions").WithTags("Sessions");

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
.AddEndpointFilter<RootOrAuthenticatedFilter>()
.WithName("RevokeSession")
.WithSummary("Revokes a specific session")
.Produces(StatusCodes.Status204NoContent)
.ProducesProblem(StatusCodes.Status403Forbidden);

var orgGroup = app.MapGroup("/organization").WithTags("Organization");

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
.Produces<Organization>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status400BadRequest);

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
.Produces<Role>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status403Forbidden);

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
.Produces<Scope>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status403Forbidden);

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
.Produces<List<GetRolesOutput>>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status403Forbidden);

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
.Produces<List<GetScopesOutput>>(StatusCodes.Status200OK)
.ProducesProblem(StatusCodes.Status403Forbidden);

app.Run();

namespace Authy.Presentation
{
    public record LoginRequest(Guid UserId);
    public record PostOrganizationRequest(string Name);
    public record PutRoleRequest(string Name, List<string> Scopes);
    public record PutScopeRequest(string Name);
    public record RefreshTokenRequest(string AccessToken, string RefreshToken);

    // Placeholder response records if not available in Shared
    // I am assuming they are returned by the handlers, but I need to check where they are defined to use them in Produces<T>
    // However, I don't see them imported. I'll need to check the codebase for the return types of the commands/queries.
}