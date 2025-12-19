using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Logging;

namespace AspireKeyCloakTemplate.SharedKernel.Features.Mediator.Behaviors;

/// <summary>
/// OpenTelemetry metrics for mediator request tracking
/// </summary>
internal static class MediatorMetrics
{
    private static readonly Meter Meter = new("AspireKeyCloakTemplate.Mediator", "1.0.0");
    
    public static readonly Counter<long> RequestCounter = Meter.CreateCounter<long>(
        "mediator.requests",
        unit: "{request}",
        description: "Total number of mediator requests");
    
    public static readonly Histogram<double> RequestDuration = Meter.CreateHistogram<double>(
        "mediator.request.duration",
        unit: "ms",
        description: "Duration of mediator request processing");
    
    public static readonly Counter<long> ErrorCounter = Meter.CreateCounter<long>(
        "mediator.errors",
        unit: "{error}",
        description: "Total number of mediator request errors");
}

/// <summary>
/// Pipeline behavior that logs request execution and tracks OpenTelemetry metrics
/// </summary>
public sealed partial class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();
        
        // Increment request counter
        MediatorMetrics.RequestCounter.Add(1, new KeyValuePair<string, object?>("request.name", requestName));
        
        LogHandlingRequest(logger, requestName);

        try
        {
            var response = await next();
            
            stopwatch.Stop();
            
            // Record successful request duration
            MediatorMetrics.RequestDuration.Record(stopwatch.Elapsed.TotalMilliseconds, 
                new KeyValuePair<string, object?>("request.name", requestName),
                new KeyValuePair<string, object?>("status", "success"));
            
            LogHandledRequest(logger, requestName);
            
            return response;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Increment error counter and record failed request duration
            MediatorMetrics.ErrorCounter.Add(1, 
                new KeyValuePair<string, object?>("request.name", requestName),
                new KeyValuePair<string, object?>("exception.type", ex.GetType().Name));
            
            MediatorMetrics.RequestDuration.Record(stopwatch.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("request.name", requestName),
                new KeyValuePair<string, object?>("status", "error"),
                new KeyValuePair<string, object?>("exception.type", ex.GetType().Name));
            
            LogErrorHandlingRequest(logger, ex, requestName);
            throw;
        }
    }

    [LoggerMessage(LogLevel.Information, "Handling {RequestName}")]
    static partial void LogHandlingRequest(ILogger<LoggingBehavior<TRequest, TResponse>> logger, string requestName);

    [LoggerMessage(LogLevel.Information, "Handled {RequestName}")]
    static partial void LogHandledRequest(ILogger<LoggingBehavior<TRequest, TResponse>> logger, string requestName);

    [LoggerMessage(LogLevel.Error, "Error handling {RequestName}")]
    static partial void LogErrorHandlingRequest(ILogger<LoggingBehavior<TRequest, TResponse>> logger, Exception ex, string requestName);
}

