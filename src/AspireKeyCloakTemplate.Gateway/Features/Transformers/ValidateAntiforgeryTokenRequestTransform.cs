using Microsoft.AspNetCore.Antiforgery;
using Yarp.ReverseProxy.Transforms;

namespace AspireKeyCloakTemplate.Gateway.Features.Transformers;

/// <summary>
/// Request transform that validates antiforgery (XSRF) tokens on non-safe HTTP methods.
/// </summary>
/// <remarks>
/// The transform skips validation for safe HTTP methods (GET/HEAD/OPTIONS/TRACE) and for
/// requests whose content type indicates protobuf payloads. For other requests it invokes
/// <see cref="IAntiforgery.ValidateRequestAsync"/> to ensure a valid antiforgery token is present.
/// If validation fails the response status is set to 400 Bad Request and the failure is logged.
/// </remarks>
internal sealed partial class ValidateAntiforgeryTokenRequestTransform : RequestTransform
{
    private readonly IAntiforgery _antiforgery;
    private readonly ILogger<ValidateAntiforgeryTokenRequestTransform> _logger;

    public ValidateAntiforgeryTokenRequestTransform(IAntiforgery antiforgery, ILogger<ValidateAntiforgeryTokenRequestTransform> logger)
    {
        _antiforgery = antiforgery;
        _logger = logger;
        LogValidateantiforgerytokenrequesttransformInstanceCreated();
    }
    /// <summary>
    /// Applies the transform to the incoming request. Performs antiforgery validation for
    /// non-safe methods and non-protobuf requests.
    /// </summary>
    /// <param name="context">The <see cref="RequestTransformContext"/> containing the current HTTP context.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when validation succeeds or the response is modified on failure.</returns>
    public override async ValueTask ApplyAsync(RequestTransformContext context)
    {
        LogValidateantiforgerytokenrequesttransformApplyasyncCalledForMethodPath(context.HttpContext.Request.Method, context.HttpContext.Request.Path.Value ?? string.Empty);

        if (context.HttpContext.Request.Method == HttpMethod.Get.Method ||
            context.HttpContext.Request.Method == HttpMethod.Head.Method ||
            context.HttpContext.Request.Method == HttpMethod.Options.Method ||
            context.HttpContext.Request.Method == HttpMethod.Trace.Method)
        {
            LogSkippingValidationSafeHttpMethodMethod(context.HttpContext.Request.Method);
            return;
        }

        if (context.HttpContext.Request.Headers.ContentType.Contains("application/x-protobuf"))
        {
            LogSkippingValidationProtobufContentType();
            return;
        }

        LogValidatingAntiforgeryToken(_logger, context.HttpContext.Request.Path.Value ?? string.Empty);

        try
        {
            await _antiforgery.ValidateRequestAsync(context.HttpContext);
        }
        catch (AntiforgeryValidationException ex)
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
            LogAntiforgeryTokenValidationFailed(_logger, ex, context.HttpContext.Request.Path.Value ?? string.Empty);
        }
    }

    /// <summary>
    /// Logs that an antiforgery token validation is being performed for the request path.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="requestPath">The request path being validated.</param>
    [LoggerMessage(LogLevel.Information, "Validating antiforgery token for request path: {requestPath}")]
    static partial void LogValidatingAntiforgeryToken(
        ILogger<ValidateAntiforgeryTokenRequestTransform> logger,
        string? requestPath);

    /// <summary>
    /// Logs that antiforgery token validation failed and the response was set to 400.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="exception">The exception thrown by the antiforgery validation library.</param>
    /// <param name="requestPath">The request path for which validation failed.</param>
    [LoggerMessage(LogLevel.Error, "Antiforgery token validation failed for request path: {requestPath}.")]
    static partial void LogAntiforgeryTokenValidationFailed(
        ILogger<ValidateAntiforgeryTokenRequestTransform> logger,
        Exception exception,
        string? requestPath);

    [LoggerMessage(LogLevel.Information, "ValidateAntiforgeryTokenRequestTransform instance created")]
    partial void LogValidateantiforgerytokenrequesttransformInstanceCreated();

    [LoggerMessage(LogLevel.Information, "ValidateAntiforgeryTokenRequestTransform.ApplyAsync called for {Method} {Path}")]
    partial void LogValidateantiforgerytokenrequesttransformApplyasyncCalledForMethodPath(string Method, string Path);

    [LoggerMessage(LogLevel.Information, "Skipping validation - safe HTTP method: {Method}")]
    partial void LogSkippingValidationSafeHttpMethodMethod(string Method);

    [LoggerMessage(LogLevel.Information, "Skipping validation - protobuf content type")]
    partial void LogSkippingValidationProtobufContentType();
}