using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Scalar.AspNetCore;
using Authy.Application.Domain.Organizations;
using Authy.Application.Domain.Roles;
using Authy.Application.Domain.Scopes;
using Authy.Application.Domain.Users;
using Authy.Application.Data;

namespace Authy.Presentation.Extensions;

public static class PresentationExtensions
{
    public static IServiceCollection AddPresentation(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOpenApi()
            .AddHttpContextAccessor();
        
        services.AddAuthentication("Bearer")
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)),
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        return services;
    }

    public static WebApplication UsePresentation(this WebApplication app)
    {
        app.ApplyMigrations();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }

        app.UseHttpsRedirection()
            .UseAuthentication()
            .UseAuthorization();

        app.MapPresentationEndpoints();

        return app;
    }

    public static IEndpointRouteBuilder MapPresentationEndpoints(this IEndpointRouteBuilder app)
    {
        app
            .MapAuthenticationEndpoints()
            .MapUserEndpoints()
            .MapSessionEndpoints()
            .MapOrganizationEndpoints()
            .MapRoleEndpoints()
            .MapScopeEndpoints();

        return app;
    }
}
