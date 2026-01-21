using Authy.Presentation.Services;
using Authy.Presentation.Domain;
using Authy.Presentation.Shared;
using Authy.Presentation.Extensions;
using Authy.Presentation.Domain.Organizations;
using Authy.Presentation.Domain.Roles;
using Authy.Presentation.Domain.Scopes;
using Authy.Presentation.Shared.Abstractions;
using Authy.Presentation.Shared.Behaviors;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IOrganizationRepository, MockRepository>();
builder.Services.AddScoped<IRoleRepository, MockRepository>();
builder.Services.AddScoped<IScopeRepository, MockRepository>();
builder.Services.AddScoped<IUnitOfWork, MockRepository>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();

builder.Services.AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>));

builder.Services.AddScoped<IDispatcher, Dispatcher>();
builder.Services.AddHandlers(typeof(Program).Assembly);

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();


app.MapPost("/organization", async (IDispatcher dispatcher, 
    [FromBody] PostOrganizationRequest request, CancellationToken cancellationToken) =>
{
    var command = new CreateOrganizationCommand(request.Name);
    var result = await dispatcher.DispatchAsync(command, cancellationToken);
    
    return result.IsSuccess 
        ? Results.Ok(result.Value)
        : result.ToProblem();
});

app.MapPost("/organization/{id:guid}/role", async (IDispatcher dispatcher, 
    Guid id, [FromHeader(Name = "X-User-Id")] Guid userId, [FromBody] PostRoleRequest request, CancellationToken cancellationToken) =>
{
    var command = new CreateRoleCommand(id, request.Name, userId);
    var result = await dispatcher.DispatchAsync(command, cancellationToken);
    
    return result.IsSuccess 
        ? Results.Ok(result.Value)
        : result.ToProblem();
});

app.MapPost("/organization/{id:guid}/scope", async (IDispatcher dispatcher, 
    Guid id, [FromHeader(Name = "X-User-Id")] Guid userId, [FromBody] PostScopeRequest request, CancellationToken cancellationToken) =>
{
    var command = new CreateScopeCommand(id, request.Name, userId);
    var result = await dispatcher.DispatchAsync(command, cancellationToken);
    
    return result.IsSuccess 
        ? Results.Ok(result.Value)
        : result.ToProblem();
});

app.Run();

public record PostOrganizationRequest(string Name);
public record PostRoleRequest(string Name);
public record PostScopeRequest(string Name);
