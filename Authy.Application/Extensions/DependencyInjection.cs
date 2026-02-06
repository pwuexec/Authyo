using System.Reflection;
using Authy.Application.Domain.Organizations.Data;
using Authy.Application.Domain.Roles.Data;
using Authy.Application.Domain.Scopes.Data;
using Authy.Application.Domain.Users.Data;
using Authy.Application.Shared.Abstractions;
using Authy.Application.Shared.Behaviors;

namespace Authy.Application.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RootIpOptions>(configuration);

        services
            .AddScoped<IAuthorizationService, AuthorizationService>()
            .AddScoped<IJwtService, JwtService>()
            .AddSingleton(TimeProvider.System)
            .AddScoped(typeof(IPipelineBehavior<,>), typeof(UnitOfWorkBehavior<,>))
            .AddScoped<IDispatcher, Dispatcher>()
            .AddHandlers(Assembly.GetExecutingAssembly());

        return services;
    }

    public static IServiceCollection AddMocks(this IServiceCollection services)
    {
        services
            .AddScoped<IOrganizationRepository, MockRepository>()
            .AddScoped<IRoleRepository, MockRepository>()
            .AddScoped<IScopeRepository, MockRepository>()
            .AddScoped<IUserRepository, MockRepository>()
            .AddScoped<IRefreshTokenRepository, MockRepository>()
            .AddScoped<IUnitOfWork, MockRepository>();
            
        return services;
    }

    public static IServiceCollection AddHandlers(this IServiceCollection services, Assembly assembly)
    {
        var handlerTypes = new[] 
        { 
            typeof(ICommandHandler<,>), 
            typeof(IQueryHandler<,>) 
        };

        foreach (var handlerInterfaceType in handlerTypes)
        {
            var handlers = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract)
                .SelectMany(t => t.GetInterfaces(), (t, i) => new { Implementation = t, Interface = i })
                .Where(x => x.Interface.IsGenericType && x.Interface.GetGenericTypeDefinition() == handlerInterfaceType)
                .GroupBy(x => x.Interface)
                .ToList();

            foreach (var group in handlers)
            {
                if (group.Count() > 1)
                {
                    var implementationNames = string.Join(", ", group.Select(x => x.Implementation.Name));
                    throw new InvalidOperationException(
                        $"Multiple implementations found for {group.Key.Name}: {implementationNames}. " +
                        "Each command/query must have exactly one handler.");
                }

                var handler = group.First();
                services.AddScoped(handler.Interface, handler.Implementation);
            }
        }

        return services;
    }
}

