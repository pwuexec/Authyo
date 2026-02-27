using System.Diagnostics;
using Authy.Application.Shared.Abstractions;
using Authy.Application.Shared.Diagnostics;

namespace Authy.Application.Shared.Behaviors;

public class MetricsBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TResponse : Result
{
    public async Task<TResponse> HandleAsync(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next();
            RecordMetrics(request, response, sw.Elapsed.TotalMilliseconds);
            return response;
        }
        catch (Exception)
        {
            RecordMetrics(request, null, sw.Elapsed.TotalMilliseconds);
            throw;
        }
    }

    private static void RecordMetrics(TRequest request, TResponse? response, double durationMs)
    {
        var requestType = typeof(TRequest);
        var tags = new TagList
        {
            { "request_type", requestType.Name },
            { "is_success", response?.IsSuccess ?? false }
        };

        if (request is ICommand<TResponse>)
        {
            Telemetry.CommandDuration.Record(durationMs, tags);
        }
        else if (request is IQuery<TResponse>)
        {
            Telemetry.QueryDuration.Record(durationMs, tags);
        }
    }
}
