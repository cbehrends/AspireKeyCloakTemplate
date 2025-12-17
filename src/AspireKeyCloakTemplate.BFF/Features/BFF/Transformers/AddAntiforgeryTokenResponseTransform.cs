using Microsoft.AspNetCore.Antiforgery;
using Yarp.ReverseProxy.Transforms;

namespace AspireKeyCloakTemplate.BFF.Features.BFF.Transformers;

/// <summary>
///     Response transform that adds an antiforgery (XSRF) token cookie for HTML responses
///     routed to the SPA catch-all route.
/// </summary>
/// <remarks>
///     This transform checks that the current request is served by the SPA "catch-all"
///     route and that the response content type contains "text/html". When those
///     conditions are met it obtains an antiforgery token via <see cref="IAntiforgery" />
///     and appends it to the response cookies under the name "__AspireKeyCloakTemplate-X-XSRF-TOKEN".
///     The cookie is writable from JavaScript (<see cref="CookieOptions.HttpOnly" /> = false),
///     marked secure and SameSite.Strict to limit cross-site usage.
/// </remarks>
internal sealed partial class AddAntiforgeryTokenResponseTransform(
    IAntiforgery antiforgery,
    ILogger<AddAntiforgeryTokenResponseTransform> logger) : ResponseTransform
{
    /// <summary>
    ///     Applies the transform to the outgoing response. Adds the XSRF cookie when the
    ///     response is HTML and the request matched the SPA catch-all route.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="ResponseTransformContext" /> containing the current HTTP context and proxy
    ///     response.
    /// </param>
    /// <returns>A completed <see cref="ValueTask" /> when processing is finished.</returns>
    public override ValueTask ApplyAsync(ResponseTransformContext context)
    {
        if (!context.HttpContext.Request.RouteValues.ContainsKey("catch-all") ||
            context.HttpContext.Response.ContentType?.Contains("text/html", StringComparison.Ordinal) != true)
            return ValueTask.CompletedTask;

        var tokenSet = antiforgery.GetAndStoreTokens(context.HttpContext);
        ArgumentNullException.ThrowIfNull(tokenSet.RequestToken);
        if (context.HttpContext.Request.Path.Value != null)
            LogXsrfTokenAddedToResponseForRequestPathRequestpath(logger, context.HttpContext.Request.Path.Value);
        return ValueTask.CompletedTask;
    }

    /// <summary>
    ///     Logs that an XSRF token was added to the response for the provided request path.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="requestPath">The request path for which the token was added.</param>
    [LoggerMessage(LogLevel.Information, "XSRF token added to response for request path: {requestPath}")]
    static partial void LogXsrfTokenAddedToResponseForRequestPathRequestpath(
        ILogger<AddAntiforgeryTokenResponseTransform> logger, string requestPath);
}
