using Authy.Presentation.Shared.Abstractions;

namespace Authy.Presentation.Shared;

public class Dispatcher(IServiceProvider serviceProvider) : IDispatcher
{
    public async Task<TResult> DispatchAsync<TResult>(ICommand<TResult> command, CancellationToken cancellationToken)
    {
        var commandType = command.GetType();
        var resultType = typeof(TResult);
        
        // Resolve handler
        var handlerType = typeof(ICommandHandler<,>).MakeGenericType(commandType, resultType);
        var handler = serviceProvider.GetRequiredService(handlerType);

        // Resolve behaviors
        var behaviorType = typeof(IPipelineBehavior<,>).MakeGenericType(commandType, resultType);
        var behaviors = serviceProvider.GetServices(behaviorType).Cast<object>().ToList();

        // Create the handler delegate
        RequestHandlerDelegate<TResult> handlerDelegate = () =>
        {
            var method = handlerType.GetMethod(nameof(ICommandHandler<ICommand<TResult>, TResult>.HandleAsync));
            return (Task<TResult>)method!.Invoke(handler, new object[] { command, cancellationToken })!;
        };

        // Chain behaviors in reverse order
        var pipeline = behaviors.Aggregate(
            handlerDelegate,
            (next, behavior) => () =>
            {
                var method = behavior.GetType().GetMethod("HandleAsync");
                return (Task<TResult>)method!.Invoke(behavior, new object[] { command, next, cancellationToken })!;
            });

        return await pipeline();
    }
}
