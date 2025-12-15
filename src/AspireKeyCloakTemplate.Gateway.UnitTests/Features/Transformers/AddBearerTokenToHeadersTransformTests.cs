using System.Security.Claims;
using AspireKeyCloakTemplate.Gateway.Features.Transformers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Shouldly;
using Xunit;
using Yarp.ReverseProxy.Transforms;

namespace AspireKeyCloakTemplate.Gateway.UnitTests.Features.Transformers;

/// <summary>
/// Unit tests for AddBearerTokenToHeadersTransform.
/// These tests focus on the authentication check logic and HttpContext setup.
/// 
/// Note on testing approach and coverage:
/// The GetUserAccessTokenAsync extension method from Duende.AccessTokenManagement internally
/// uses IUserTokenManager which requires proper DI configuration with multiple services
/// (IUserTokenStore, IUserTokenEndpointService, etc.). These unit tests focus on the
/// authentication gate-keeping logic that can be tested in isolation without complex service setup.
/// 
/// Coverage achieved:
/// - Authentication check (lines 27-30): ✓ Fully covered
/// - Token retrieval failure path (lines 35-42): ✗ Requires IUserTokenManager service
/// - Token retrieval success path (lines 44-45): ✗ Requires IUserTokenManager service
/// 
/// The success and failure paths for token retrieval are best tested through:
/// 1. Integration tests with actual Duende services configured
/// 2. End-to-end tests with real authentication flows
/// 
/// This approach follows testing best practices by:
/// - Testing what can be reliably unit tested (authentication gate)
/// - Acknowledging dependencies that require integration testing (token management)
/// - Avoiding brittle mocks of complex third-party service chains
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
        var context = CreateRequestTransformContext(isAuthenticated: false);

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
        var context = CreateRequestTransformContext(isAuthenticated: false, hasIdentity: false);

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
        var context = CreateRequestTransformContext(isAuthenticated: false, hasIdentity: true);

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
        var context = CreateRequestTransformContext(isAuthenticated: true, requestPath: "/api/test");

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
            isAuthenticated: true,
            requestPath: null);

        // Act & Assert - Will throw because IUserTokenManager is not configured
        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await _transform.ApplyAsync(context);
        });
    }

    [Fact]
    public async Task ApplyAsync_WithEmptyRequestPath_ShouldAttemptTokenRetrieval()
    {
        // Arrange
        var context = CreateRequestTransformContext(
            isAuthenticated: true,
            requestPath: "");

        // Act & Assert - Will throw because IUserTokenManager is not configured
        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await _transform.ApplyAsync(context);
        });
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
            isAuthenticated: true,
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
            isAuthenticated: true,
            requestPath: requestPath);

        // Act & Assert - Will throw because IUserTokenManager is not configured
        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await _transform.ApplyAsync(context);
        });
    }

    [Fact]
    public async Task ApplyAsync_WithMultipleAuthenticatedCalls_ShouldAttemptTokenRetrievalForEach()
    {
        // Arrange
        var context1 = CreateRequestTransformContext(isAuthenticated: true, requestPath: "/api/first");
        var context2 = CreateRequestTransformContext(isAuthenticated: true, requestPath: "/api/second");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () => await _transform.ApplyAsync(context1));
        await Should.ThrowAsync<InvalidOperationException>(async () => await _transform.ApplyAsync(context2));
    }

    [Fact]
    public async Task ApplyAsync_WithMixedAuthenticationStates_ShouldOnlyProcessAuthenticated()
    {
        // Arrange
        var unauthenticatedContext = CreateRequestTransformContext(isAuthenticated: false);
        var authenticatedContext = CreateRequestTransformContext(isAuthenticated: true, requestPath: "/api/test");

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
        var context1 = CreateRequestTransformContext(isAuthenticated: false, hasIdentity: false);
        await _transform.ApplyAsync(context1);
        context1.ProxyRequest.Headers.Authorization.ShouldBeNull();
        
        // Scenario 2: Identity exists but not authenticated
        var context2 = CreateRequestTransformContext(isAuthenticated: false, hasIdentity: true);
        await _transform.ApplyAsync(context2);
        context2.ProxyRequest.Headers.Authorization.ShouldBeNull();
        
        // Scenario 3: Authenticated - would proceed to token retrieval
        // (but throws InvalidOperationException due to missing IUserTokenManager service)
        var context3 = CreateRequestTransformContext(isAuthenticated: true);
        await Should.ThrowAsync<InvalidOperationException>(async () =>
        {
            await _transform.ApplyAsync(context3);
        });
        
        // All three scenarios behave as expected
        _fakeLogger.Collector.GetSnapshot().ShouldBeEmpty();
    }

    [Fact]
    public async Task ApplyAsync_ShouldNotModifyHttpContextUser()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.Name, "TestUser") };
        var context = CreateRequestTransformContext(isAuthenticated: true, claims: claims);
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
        var context = CreateRequestTransformContext(isAuthenticated: true, requestPath: requestPath);

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
}
