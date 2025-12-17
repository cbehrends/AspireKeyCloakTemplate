using System.Net.Http.Headers;
using System.Security.Claims;
using Duende.AccessTokenManagement;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using Shouldly;
using Xunit;
using Yarp.ReverseProxy.Transforms;
using AddBearerTokenToHeadersTransform = AspireKeyCloakTemplate.BFF.Features.BFF.Transformers.AddBearerTokenToHeadersTransform;

namespace AspireKeyCloakTemplate.BFF.UnitTests.Features.BFF.Transformers;

/// <summary>
///     Unit tests for AddBearerTokenToHeadersTransform.
///     These tests focus on the authentication check logic and HttpContext setup.
///     Testing approach:
///     The GetUserAccessTokenAsync extension method from Duende.AccessTokenManagement internally
///     uses IUserTokenManager service. These tests mock IUserTokenManager to achieve full coverage
///     of all code paths without requiring complex service chain setup.
///     This approach follows testing best practices by:
///     - Testing authentication gate-keeping logic in isolation
///     - Mocking the direct dependency (IUserTokenManager) rather than the entire service chain
///     - Verifying correct behavior for both success and failure scenarios
///     - Using DefaultHttpContext with proper DI configuration for realistic test setup
/// </summary>
public class AddBearerTokenToHeadersTransformTests
{
    private readonly FakeLogger<AddBearerTokenToHeadersTransform> _fakeLogger;
    private readonly AddBearerTokenToHeadersTransform _transform;

    public AddBearerTokenToHeadersTransformTests()
    {
        _fakeLogger = new FakeLogger<AddBearerTokenToHeadersTransform>();
        _transform = new AddBearerTokenToHeadersTransform(_fakeLogger);
    }

    [Fact]
    public async Task ApplyAsync_WhenUserNotAuthenticated_ShouldSkipTokenAddition()
    {
        // Arrange
        var context = CreateRequestTransformContext(false);

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        context.ProxyRequest.Headers.Authorization.ShouldBeNull();

        // Verify no logs were written since we exited early
        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.ShouldBeEmpty();
    }

    [Fact]
    public async Task ApplyAsync_WhenUserIdentityIsNull_ShouldSkipTokenAddition()
    {
        // Arrange
        var context = CreateRequestTransformContext(false, hasIdentity: false);

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        context.ProxyRequest.Headers.Authorization.ShouldBeNull();

        // Verify no logs were written since we exited early
        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.ShouldBeEmpty();
    }

    [Fact]
    public async Task ApplyAsync_WhenIdentityNotAuthenticatedWithNullAuthType_ShouldSkipTokenAddition()
    {
        // Arrange - Identity with no authentication type (IsAuthenticated = false)
        var context = CreateRequestTransformContext(false, hasIdentity: true);

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        context.ProxyRequest.Headers.Authorization.ShouldBeNull();

        // Verify no processing occurred
        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.ShouldBeEmpty();
    }

    [Fact]
    public async Task ApplyAsync_WhenUserAuthenticatedButNoTokenManager_ShouldThrowInvalidOperationException()
    {
        // Arrange - Authenticated user but no IUserTokenManager service configured
        // This simulates the case where GetUserAccessTokenAsync will fail
        var context = CreateRequestTransformContext(true, "/api/test");

        // Act & Assert - Should throw InvalidOperationException when service is not registered
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await _transform.ApplyAsync(context);
        });

        exception.Message.ShouldContain("IUserTokenManager");
    }

    [Fact]
    public async Task ApplyAsync_WithNullRequestPath_ShouldAttemptTokenRetrieval()
    {
        // Arrange
        var context = CreateRequestTransformContext(
            true,
            null);

        // Act & Assert - Will throw because IUserTokenManager is not configured
        await Should.ThrowAsync<InvalidOperationException>(async () => { await _transform.ApplyAsync(context); });
    }

    [Fact]
    public async Task ApplyAsync_WithEmptyRequestPath_ShouldAttemptTokenRetrieval()
    {
        // Arrange
        var context = CreateRequestTransformContext(
            true,
            "");

        // Act & Assert - Will throw because IUserTokenManager is not configured
        await Should.ThrowAsync<InvalidOperationException>(async () => { await _transform.ApplyAsync(context); });
    }

    [Fact]
    public async Task ApplyAsync_WithAuthenticatedUserWithClaims_ShouldAttemptTokenRetrieval()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "user123"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Email, "test@example.com")
        };

        var context = CreateRequestTransformContext(
            true,
            claims: claims,
            requestPath: "/api/users");

        // Act & Assert - Will throw because IUserTokenManager is not configured
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await _transform.ApplyAsync(context);
        });

        exception.Message.ShouldContain("IUserTokenManager");
    }

    [Theory]
    [InlineData("/api/users")]
    [InlineData("/api/products/123")]
    [InlineData("/")]
    public async Task ApplyAsync_WithDifferentRequestPaths_ShouldAttemptTokenRetrieval(string requestPath)
    {
        // Arrange
        var context = CreateRequestTransformContext(
            true,
            requestPath);

        // Act & Assert - Will throw because IUserTokenManager is not configured
        await Should.ThrowAsync<InvalidOperationException>(async () => { await _transform.ApplyAsync(context); });
    }

    [Fact]
    public async Task ApplyAsync_WithMultipleAuthenticatedCalls_ShouldAttemptTokenRetrievalForEach()
    {
        // Arrange
        var context1 = CreateRequestTransformContext(true, "/api/first");
        var context2 = CreateRequestTransformContext(true, "/api/second");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () => await _transform.ApplyAsync(context1));
        await Should.ThrowAsync<InvalidOperationException>(async () => await _transform.ApplyAsync(context2));
    }

    [Fact]
    public async Task ApplyAsync_WithMixedAuthenticationStates_ShouldOnlyProcessAuthenticated()
    {
        // Arrange
        var unauthenticatedContext = CreateRequestTransformContext(false);
        var authenticatedContext = CreateRequestTransformContext(true, "/api/test");

        // Act - Unauthenticated should not throw
        await _transform.ApplyAsync(unauthenticatedContext);

        // Assert - Authenticated should throw
        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await _transform.ApplyAsync(authenticatedContext);
        });

        // Verify unauthenticated call produced no logs
        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.ShouldBeEmpty();
    }

    [Fact]
    public void Transform_ShouldBeConstructedWithLogger()
    {
        // Arrange & Act
        var logger = new FakeLogger<AddBearerTokenToHeadersTransform>();
        var transform = new AddBearerTokenToHeadersTransform(logger);

        // Assert
        transform.ShouldNotBeNull();
    }

    [Fact]
    public async Task ApplyAsync_AuthenticationCheckCoverage_DocumentsBehavior()
    {
        // This test documents the authentication check coverage and behavior
        // When user is not authenticated or identity is null, the transform
        // exits early without attempting token retrieval.

        // Scenario 1: No identity
        var context1 = CreateRequestTransformContext(false, hasIdentity: false);
        await _transform.ApplyAsync(context1);
        context1.ProxyRequest.Headers.Authorization.ShouldBeNull();

        // Scenario 2: Identity exists but not authenticated
        var context2 = CreateRequestTransformContext(false, hasIdentity: true);
        await _transform.ApplyAsync(context2);
        context2.ProxyRequest.Headers.Authorization.ShouldBeNull();

        // Scenario 3: Authenticated - would proceed to token retrieval
        // (but throws InvalidOperationException due to missing IUserTokenManager service)
        var context3 = CreateRequestTransformContext(true);
        await Should.ThrowAsync<InvalidOperationException>(async () => { await _transform.ApplyAsync(context3); });

        // All three scenarios behave as expected
        _fakeLogger.Collector.GetSnapshot().ShouldBeEmpty();
    }

    [Fact]
    public async Task ApplyAsync_ShouldNotModifyHttpContextUser()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.Name, "TestUser") };
        var context = CreateRequestTransformContext(true, claims: claims);
        var originalPrincipal = context.HttpContext.User;

        // Act & Assert - Will throw, but we can check user wasn't modified before the throw
        try
        {
            await _transform.ApplyAsync(context);
        }
        catch (InvalidOperationException)
        {
            // Expected - service not configured
        }

        // Assert - User should remain unchanged
        context.HttpContext.User.ShouldBe(originalPrincipal);
        context.HttpContext.User.Identity?.IsAuthenticated.ShouldBeTrue();
    }

    [Fact]
    public async Task ApplyAsync_ShouldNotModifyHttpContextRequest()
    {
        // Arrange
        const string requestPath = "/api/test";
        var context = CreateRequestTransformContext(true, requestPath);

        // Act & Assert - Will throw, but we can check request wasn't modified before the throw
        try
        {
            await _transform.ApplyAsync(context);
        }
        catch (InvalidOperationException)
        {
            // Expected - service not configured
        }

        // Assert - Request should remain unchanged
        context.HttpContext.Request.Path.Value.ShouldBe(requestPath);
    }

    [Fact]
    public async Task ApplyAsync_WhenTokenRetrievalFails_ShouldLogErrorAndNotAddHeader()
    {
        // Arrange
        const string requestPath = "/api/test";
        const string errorCode = "token_expired";
        const string errorDescription = "The access token has expired";

        var userTokenManager = Substitute.For<IUserTokenManager>();
        var context = CreateRequestTransformContextWithServices(
            true,
            requestPath,
            configureServices: services => services.AddSingleton(userTokenManager));

        // Configure the mock to return a failed result
        var failedResult = new FailedResult(errorCode, errorDescription);
        TokenResult<UserToken> tokenResultFailed = failedResult;

        userTokenManager
            .GetAccessTokenAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<UserTokenRequestParameters?>(),
                Arg.Any<CancellationToken>())
            .Returns(tokenResultFailed);

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        context.ProxyRequest.Headers.Authorization.ShouldBeNull();

        var logs = _fakeLogger.Collector.GetSnapshot();
        var errorLogs = logs.Where(l => l.Level == LogLevel.Error).ToList();
        errorLogs.ShouldNotBeEmpty();
        errorLogs.First().Message.ShouldContain("Could not get access token");
        errorLogs.First().Message.ShouldContain(errorCode);
        errorLogs.First().Message.ShouldContain(requestPath);
        errorLogs.First().Message.ShouldContain(errorDescription);
    }

    [Fact]
    public async Task ApplyAsync_WhenTokenRetrievalSucceeds_ShouldAddBearerTokenToHeaders()
    {
        // Arrange
        const string expectedToken = "test-access-token-12345";
        const string requestPath = "/api/users";

        var userTokenManager = Substitute.For<IUserTokenManager>();
        var context = CreateRequestTransformContextWithServices(
            true,
            requestPath,
            configureServices: services => services.AddSingleton(userTokenManager));

        // Configure the mock to return a successful result
        var userToken = new UserToken
        {
            AccessToken = AccessToken.Parse(expectedToken),
            ClientId = ClientId.Parse("test-client"),
            AccessTokenType = AccessTokenType.Parse("Bearer"),
            Expiration = DateTimeOffset.UtcNow.AddHours(1)
        };
        TokenResult<UserToken> tokenResultSuccess = userToken;

        userTokenManager
            .GetAccessTokenAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<UserTokenRequestParameters?>(),
                Arg.Any<CancellationToken>())
            .Returns(tokenResultSuccess);

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        context.ProxyRequest.Headers.Authorization.ShouldNotBeNull();
        context.ProxyRequest.Headers.Authorization!.Scheme.ShouldBe("Bearer");
        context.ProxyRequest.Headers.Authorization.Parameter.ShouldBe(expectedToken);

        var logs = _fakeLogger.Collector.GetSnapshot();
        var infoLogs = logs.Where(l => l.Level == LogLevel.Information).ToList();
        infoLogs.ShouldNotBeEmpty();
        infoLogs.First().Message.ShouldContain("Adding bearer token to request headers");
        infoLogs.First().Message.ShouldContain(requestPath);
    }

    [Fact]
    public async Task ApplyAsync_WhenTokenRetrievalFailsWithNullPath_ShouldLogErrorWithEmptyPath()
    {
        // Arrange
        const string errorCode = "service_unavailable";

        var userTokenManager = Substitute.For<IUserTokenManager>();
        var context = CreateRequestTransformContextWithServices(
            true,
            null,
            configureServices: services => services.AddSingleton(userTokenManager));

        var failedResult = new FailedResult(errorCode, "Service temporarily unavailable");
        TokenResult<UserToken> tokenResultFailed = failedResult;

        userTokenManager
            .GetAccessTokenAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<UserTokenRequestParameters?>(),
                Arg.Any<CancellationToken>())
            .Returns(tokenResultFailed);

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        context.ProxyRequest.Headers.Authorization.ShouldBeNull();

        var logs = _fakeLogger.Collector.GetSnapshot();
        var errorLogs = logs.Where(l => l.Level == LogLevel.Error).ToList();
        errorLogs.ShouldNotBeEmpty();
        errorLogs.First().Message.ShouldContain("Could not get access token");
        errorLogs.First().Message.ShouldContain(errorCode);
    }

    [Fact]
    public async Task ApplyAsync_WhenExistingAuthHeaderPresent_ShouldReplaceWithBearerToken()
    {
        // Arrange
        const string newToken = "new-bearer-token";

        var userTokenManager = Substitute.For<IUserTokenManager>();
        var context = CreateRequestTransformContextWithServices(
            true,
            configureServices: services => services.AddSingleton(userTokenManager));

        // Set an existing authorization header
        context.ProxyRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", "old-credentials");

        var userToken = new UserToken
        {
            AccessToken = AccessToken.Parse(newToken),
            ClientId = ClientId.Parse("test-client"),
            AccessTokenType = AccessTokenType.Parse("Bearer"),
            Expiration = DateTimeOffset.UtcNow.AddHours(1)
        };
        TokenResult<UserToken> tokenResultSuccess = userToken;

        userTokenManager
            .GetAccessTokenAsync(Arg.Any<ClaimsPrincipal>(), Arg.Any<UserTokenRequestParameters?>(),
                Arg.Any<CancellationToken>())
            .Returns(tokenResultSuccess);

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        context.ProxyRequest.Headers.Authorization.ShouldNotBeNull();
        context.ProxyRequest.Headers.Authorization!.Scheme.ShouldBe("Bearer");
        context.ProxyRequest.Headers.Authorization.Parameter.ShouldBe(newToken);
    }

    // Helper methods

    private static RequestTransformContext CreateRequestTransformContext(
        bool isAuthenticated,
        string? requestPath = "/test",
        bool hasIdentity = true,
        Claim[]? claims = null)
    {
        var httpContext = new DefaultHttpContext();

        if (hasIdentity)
        {
            var identity = new ClaimsIdentity(
                claims ?? [],
                isAuthenticated ? "TestAuthType" : null);
            httpContext.User = new ClaimsPrincipal(identity);
        }

        httpContext.Request.Path = new PathString(requestPath);

        // Provide an empty service provider to prevent NullReferenceException
        // when GetUserAccessTokenAsync is called
        var serviceCollection = new ServiceCollection();
        httpContext.RequestServices = serviceCollection.BuildServiceProvider();

        var proxyRequest = new HttpRequestMessage();
        return new RequestTransformContext
        {
            HttpContext = httpContext,
            ProxyRequest = proxyRequest
        };
    }

    private static RequestTransformContext CreateRequestTransformContextWithServices(
        bool isAuthenticated,
        string? requestPath = "/test",
        bool hasIdentity = true,
        Claim[]? claims = null,
        Action<IServiceCollection>? configureServices = null)
    {
        var httpContext = new DefaultHttpContext();

        if (hasIdentity)
        {
            var identity = new ClaimsIdentity(
                claims ?? [],
                isAuthenticated ? "TestAuthType" : null);
            httpContext.User = new ClaimsPrincipal(identity);
        }

        httpContext.Request.Path = new PathString(requestPath);

        // Configure services if provided
        var serviceCollection = new ServiceCollection();
        configureServices?.Invoke(serviceCollection);
        httpContext.RequestServices = serviceCollection.BuildServiceProvider();

        var proxyRequest = new HttpRequestMessage();
        return new RequestTransformContext
        {
            HttpContext = httpContext,
            ProxyRequest = proxyRequest
        };
    }
}
