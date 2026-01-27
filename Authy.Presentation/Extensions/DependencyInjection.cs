using System.Reflection;
using Authy.Presentation.Shared.Abstractions;

namespace Authy.Presentation.Extensions;

public static class DependencyInjection
{
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
