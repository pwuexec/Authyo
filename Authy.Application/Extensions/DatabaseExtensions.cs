using Authy.Application.Data;
using Authy.Application.Data.Repositories;
using Authy.Application.Domain.Organizations.Data;
using Authy.Application.Domain.Roles.Data;
using Authy.Application.Domain.Scopes.Data;
using Authy.Application.Domain.Users.Data;
using Authy.Application.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Authy.Application.Extensions;

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
                .AddScoped<IUnitOfWork, UnitOfWork>()
                .AddHostedService<DatabaseInitializer>();
        }
        else
        {
            services.AddMocks();
        }

        return services;
    }
}
