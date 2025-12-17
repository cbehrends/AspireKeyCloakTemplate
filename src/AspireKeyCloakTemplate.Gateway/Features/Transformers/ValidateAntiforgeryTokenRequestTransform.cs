using System.Diagnostics.Metrics;
using Microsoft.AspNetCore.Antiforgery;
using Yarp.ReverseProxy.Transforms;

namespace AspireKeyCloakTemplate.Gateway.Features.Transformers;

/// <summary>
///     Request transform that validates antiforgery (XSRF) tokens on non-safe HTTP methods.
///     Instrumented with OpenTelemetry metrics for Aspire Dashboard integration.
/// </summary>
internal sealed partial class ValidateAntiforgeryTokenRequestTransform : RequestTransform
{
    private readonly IAntiforgery _antiforgery;
    private readonly ILogger<ValidateAntiforgeryTokenRequestTransform> _logger;

    // --- OpenTelemetry Instrumentation ---
    private static readonly Meter Meter = new("AspireKeyCloakTemplate.Gateway", "1.0.0");
    
    private static readonly Counter<long> ValidationFailuresCounter = 
        Meter.CreateCounter<long>(
            "gateway.antiforgery.failures", 
            unit: "{failures}", 
            description: "Number of failed antiforgery validations");

    private static readonly Histogram<double> ValidationDuration = 
        Meter.CreateHistogram<double>(
            "gateway.antiforgery.duration", 
            unit: "ms", 
            description: "Duration of antiforgery validation processing");

    public ValidateAntiforgeryTokenRequestTransform(
        IAntiforgery antiforgery,
        ILogger<ValidateAntiforgeryTokenRequestTransform> logger)
    {
        _antiforgery = antiforgery;
        _logger = logger;
        LogInstanceCreated();
    }

    public override async ValueTask ApplyAsync(RequestTransformContext context)
    {
        var httpContext = context.HttpContext;
        var method = httpContext.Request.Method;
        var path = httpContext.Request.Path.Value ?? string.Empty;

        LogApplyAsyncCalled(method, path);

        if (HttpMethods.IsGet(method) || 
            HttpMethods.IsHead(method) || 
            HttpMethods.IsOptions(method) || 
            HttpMethods.IsTrace(method))
        {
            LogSkippingSafeMethod(method);
            return;
        }

        if (httpContext.Request.Headers.ContentType.Contains("application/x-protobuf"))
        {
            LogSkippingProtobuf();
            return;
        }

        LogValidating(path);
        
        var startTime = System.Diagnostics.Stopwatch.GetTimestamp();
        try
        {
            await _antiforgery.ValidateRequestAsync(httpContext);
            
            // Record success duration
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(startTime);
            ValidationDuration.Record(elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("status", "success"));
        }
        catch (AntiforgeryValidationException ex)
        {
            // Record failure metrics
            var elapsed = System.Diagnostics.Stopwatch.GetElapsedTime(startTime);
            ValidationDuration.Record(elapsed.TotalMilliseconds, new KeyValuePair<string, object?>("status", "failure"));
            
            ValidationFailuresCounter.Add(1, 
                new KeyValuePair<string, object?>("path", path),
                new KeyValuePair<string, object?>("method", method));

            httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            LogValidationFailed(ex, path);
        }
    }

    [LoggerMessage(LogLevel.Information, "Validating antiforgery token for request path: {path}")]
    partial void LogValidating(string path);

    [LoggerMessage(LogLevel.Error, "Antiforgery token validation failed for request path: {path}.")]
    partial void LogValidationFailed(Exception exception, string path);

    [LoggerMessage(LogLevel.Debug, "ValidateAntiforgeryTokenRequestTransform instance created")]
    partial void LogInstanceCreated();

    [LoggerMessage(LogLevel.Debug, "ApplyAsync called for {method} {path}")]
    partial void LogApplyAsyncCalled(string method, string path);

    [LoggerMessage(LogLevel.Debug, "Skipping validation - safe HTTP method: {method}")]
    partial void LogSkippingSafeMethod(string method);

    [LoggerMessage(LogLevel.Debug, "Skipping validation - protobuf content type")]
    partial void LogSkippingProtobuf();
}
