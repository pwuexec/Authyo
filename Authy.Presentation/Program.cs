using Authy.Presentation.Shared;
using Authy.Presentation.Extensions;
using Authy.Presentation.Domain.Organizations;
using Authy.Presentation.Domain.Roles;
using Authy.Presentation.Domain.Scopes;
using Authy.Presentation.Shared.Abstractions;
using Authy.Presentation.Shared.Behaviors;
using Microsoft.AspNetCore.Mvc;
using Authy.Presentation.Domain.Users;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IOrganizationRepository, MockRepository>();
builder.Services.AddScoped<IRoleRepository, MockRepository>();
builder.Services.AddScoped<IScopeRepository, MockRepository>();
builder.Services.AddScoped<IUserRepository, MockRepository>();
builder.Services.AddScoped<IUnitOfWork, MockRepository>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<IJwtService, JwtService>();

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
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapPost("/login", async (IDispatcher dispatcher, 
    [FromBody] LoginRequest request, CancellationToken cancellationToken) =>
{
    var command = new GenerateTokenCommand(request.UserId);
    var result = await dispatcher.DispatchAsync(command, cancellationToken);
    
    return result.IsSuccess 
        ? Results.Ok(new { Token = result.Value })
        : result.ToProblem();
});

app.MapPost("/organization", async (IDispatcher dispatcher, 
    [FromBody] PostOrganizationRequest request, CancellationToken cancellationToken) =>
{
    var command = new CreateOrganizationCommand(request.Name);
    var result = await dispatcher.DispatchAsync(command, cancellationToken);
    
    return result.IsSuccess 
        ? Results.Ok(result.Value)
        : result.ToProblem();
});

app.MapGet("/organization", async (IDispatcher dispatcher, CancellationToken cancellationToken) =>
{
    var query = new GetOrganizationsQuery();
    var result = await dispatcher.DispatchAsync(query, cancellationToken);
    
    return result.IsSuccess 
        ? Results.Ok(result.Value)
        : result.ToProblem();
});

app.MapPut("/organization/{id:guid}/role", async (IDispatcher dispatcher, 
    Guid id, ClaimsPrincipal user, [FromBody] PutRoleRequest request, CancellationToken cancellationToken) =>
{
    var userId = user.GetUserId();
    var command = new UpsertRoleCommand(id, request.Name, request.Scopes, userId ?? Guid.Empty);
    var result = await dispatcher.DispatchAsync(command, cancellationToken);
    
    return result.IsSuccess 
        ? Results.Ok(result.Value)
        : result.ToProblem();
}).AddEndpointFilter<RootOrAuthenticatedFilter>();

app.MapPut("/organization/{id:guid}/scope", async (IDispatcher dispatcher, 
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
}).AddEndpointFilter<RootOrAuthenticatedFilter>();

app.MapGet("/organization/{id:guid}/role", async (IDispatcher dispatcher, 
    Guid id, ClaimsPrincipal user, CancellationToken cancellationToken) =>
{
    var userId = user.GetUserId();

    var query = new GetRolesQuery(id, userId ?? Guid.Empty);
    var result = await dispatcher.DispatchAsync(query, cancellationToken);
    
    return result.IsSuccess 
        ? Results.Ok(result.Value)
        : result.ToProblem();
}).AddEndpointFilter<RootOrAuthenticatedFilter>();

app.MapGet("/organization/{id:guid}/scope", async (IDispatcher dispatcher, 
    Guid id, ClaimsPrincipal user, CancellationToken cancellationToken) =>
{
    var userId = user.GetUserId();

    var query = new GetScopesQuery(id, userId ?? Guid.Empty);
    var result = await dispatcher.DispatchAsync(query, cancellationToken);
    
    return result.IsSuccess 
        ? Results.Ok(result.Value)
        : result.ToProblem();
}).AddEndpointFilter<RootOrAuthenticatedFilter>();

app.Run();

public record LoginRequest(Guid UserId);
public record PostOrganizationRequest(string Name);
public record PutRoleRequest(string Name, List<string> Scopes);
public record PutScopeRequest(string Name);
