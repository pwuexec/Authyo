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

app.MapAuthenticationEndpoints();
app.MapUserEndpoints();
app.MapSessionEndpoints();
app.MapOrganizationEndpoints();
app.MapRoleEndpoints();
app.MapScopeEndpoints();

app.Run();
