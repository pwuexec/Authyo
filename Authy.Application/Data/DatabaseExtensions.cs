using Authy.Application.Domain.Organizations.Data;
using Authy.Application.Domain.Roles.Data;
using Authy.Application.Domain.Scopes.Data;
using Authy.Application.Domain.Users.Data;
using Authy.Application.Shared.Abstractions;
using Authy.Application.Extensions;
using Authy.Application.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Authy.Application.Data;

public static class DatabaseExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration["Persistence:Provider"];

        if (provider == "Sqlite")
        {
            var connectionString = configuration["Persistence:ConnectionString"] ?? "Data Source=authy.db";
            
            services.AddDbContext<AuthyDbContext>(options =>
                options.UseSqlite(connectionString));

            services
                .AddScoped<IOrganizationRepository, OrganizationRepository>()
                .AddScoped<IRoleRepository, RoleRepository>()
                .AddScoped<IScopeRepository, ScopeRepository>()
                .AddScoped<IUserRepository, UserRepository>()
                .AddScoped<IRefreshTokenRepository, RefreshTokenRepository>()
                .AddScoped<IUnitOfWork, UnitOfWork>();
        }
        else
        {
            services.AddMocks();
        }

        return services;
    }

    public static T EnsureDatabaseCreated<T>(this T app) where T : IApplicationBuilder
    {
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetService<AuthyDbContext>();
        context?.Database.EnsureCreated();
        return app;
    }

    public static T ApplyMigrations<T>(this T app) where T : IApplicationBuilder
    {
        using var scope = app.ApplicationServices.CreateScope();
        var context = scope.ServiceProvider.GetService<AuthyDbContext>();
        context?.Database.Migrate();
        return app;
    }
}

