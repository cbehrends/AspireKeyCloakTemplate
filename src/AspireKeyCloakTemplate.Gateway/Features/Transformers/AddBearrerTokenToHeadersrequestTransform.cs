using System.Net.Http.Headers;
using Duende.AccessTokenManagement.OpenIdConnect;
using Yarp.ReverseProxy.Transforms;

namespace AspireKeyCloakTemplate.Gateway.Features.Transformers;

/// <summary>
/// Request transform that appends a Bearer access token to the outgoing proxy request
/// when the current user is authenticated.
/// </summary>
/// <remarks>
/// The transform uses Duende.AccessTokenManagement to retrieve the current user's access token
/// (this will handle refreshes if necessary). If an access token cannot be obtained the request
/// is left unchanged and an error is logged. When a token is available it is set on the
/// <see cref="HttpRequestMessage.Headers"/> Authorization header as a Bearer token.
/// </remarks>
internal sealed partial class AddBearerTokenToHeadersTransform(ILogger<AddBearerTokenToHeadersTransform> logger) : RequestTransform
{
    /// <summary>
    /// Applies the transform to the outgoing proxy request. If the current user is authenticated
    /// it attempts to obtain an access token and, when successful, sets it as the Authorization header.
    /// </summary>
    /// <param name="context">The <see cref="RequestTransformContext"/> containing the current HTTP context and proxy request.</param>
    /// <returns>A <see cref="ValueTask"/> that completes when the header has been set or skipped.</returns>
    public override async ValueTask ApplyAsync(RequestTransformContext context)
    {
        if (context.HttpContext.User.Identity is not { IsAuthenticated: true })
        {
            return;
        }

        // This also handles token refreshes
        var accessToken = await context.HttpContext.GetUserAccessTokenAsync();
        // Caching
        if (!accessToken.Succeeded)
        {
            LogCouldNotGetAccessToken(logger,
                accessToken.FailedResult.Error,
                context.HttpContext.Request.Path.Value ?? string.Empty,
                accessToken.FailedResult.ErrorDescription ?? string.Empty);
            return;
        }

        LogAddingBearerTokenToRequestHeaders(logger, context.HttpContext.Request.Path.Value ?? string.Empty);
        context.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken.Token.AccessToken);
    }

    /// <summary>
    /// Logs a failure to retrieve an access token for the current user.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="getUserAccessTokenError">Short error code returned by the access token retrieval API.</param>
    /// <param name="requestPath">The request path for which the token was requested.</param>
    /// <param name="error">Optional descriptive error message.</param>
    [LoggerMessage(LogLevel.Error, "Could not get access token: {getUserAccessTokenError} for request path: {requestPath}. {error}")]
    static partial void LogCouldNotGetAccessToken(
        ILogger<AddBearerTokenToHeadersTransform> logger,
        string getUserAccessTokenError,
        string? requestPath,
        string? error);

    /// <summary>
    /// Logs that a bearer token is being added to the outgoing proxy request headers.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="requestPath">The request path for which the header is being set.</param>
    [LoggerMessage(LogLevel.Information, "Adding bearer token to request headers for request path: {requestPath}")]
    static partial void LogAddingBearerTokenToRequestHeaders(
        ILogger<AddBearerTokenToHeadersTransform> logger,
        string? requestPath);
}