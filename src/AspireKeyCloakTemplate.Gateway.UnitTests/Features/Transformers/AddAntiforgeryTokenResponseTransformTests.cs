using AspireKeyCloakTemplate.Gateway.Features.Transformers;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using Shouldly;
using Xunit;
using Yarp.ReverseProxy.Transforms;

namespace AspireKeyCloakTemplate.Gateway.UnitTests.Features.Transformers;

public class AddAntiforgeryTokenResponseTransformTests
{
    private readonly IAntiforgery _antiforgerySubstitute;
    private readonly FakeLogger<AddAntiforgeryTokenResponseTransform> _fakeLogger;
    private readonly AddAntiforgeryTokenResponseTransform _transform;

    public AddAntiforgeryTokenResponseTransformTests()
    {
        _antiforgerySubstitute = Substitute.For<IAntiforgery>();
        _fakeLogger = new FakeLogger<AddAntiforgeryTokenResponseTransform>();
        _transform = new AddAntiforgeryTokenResponseTransform(_antiforgerySubstitute, _fakeLogger);
    }

    [Fact]
    public async Task ApplyAsync_WhenCatchAllRouteNotPresent_ShouldNotProcessToken()
    {
        // Arrange
        var context = CreateResponseTransformContext(
            routeValues: new Dictionary<string, object?>(),
            contentType: "text/html"
        );

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        _antiforgerySubstitute.DidNotReceive().GetAndStoreTokens(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task ApplyAsync_WhenContentTypeNotHtml_ShouldNotProcessToken()
    {
        // Arrange
        var context = CreateResponseTransformContext(
            routeValues: new Dictionary<string, object?> { { "catch-all", "value" } },
            contentType: "application/json"
        );

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        _antiforgerySubstitute.DidNotReceive().GetAndStoreTokens(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task ApplyAsync_WhenContentTypeIsNull_ShouldNotProcessToken()
    {
        // Arrange
        var context = CreateResponseTransformContext(
            routeValues: new Dictionary<string, object?> { { "catch-all", "value" } },
            contentType: null
        );

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        _antiforgerySubstitute.DidNotReceive().GetAndStoreTokens(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task ApplyAsync_WhenConditionsMetAndCatchAllPresent_ShouldRetrieveAndStoreTokens()
    {
        // Arrange
        var tokenSet = CreateAntiforgeryTokenSet();
        _antiforgerySubstitute.GetAndStoreTokens(Arg.Any<HttpContext>()).Returns(tokenSet);

        var context = CreateResponseTransformContext(
            routeValues: new Dictionary<string, object?> { { "catch-all", "value" } },
            contentType: "text/html; charset=utf-8"
        );

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        _antiforgerySubstitute.Received(1).GetAndStoreTokens(Arg.Is<HttpContext>(ctx => ctx == context.HttpContext));
    }

    [Theory]
    [InlineData("text/html")]
    [InlineData("text/html; charset=utf-8")]
    [InlineData("text/html; charset=UTF-8")]
    public async Task ApplyAsync_WithVariousHtmlContentTypes_ShouldProcessToken(string contentType)
    {
        // Arrange
        var tokenSet = CreateAntiforgeryTokenSet();
        _antiforgerySubstitute.GetAndStoreTokens(Arg.Any<HttpContext>()).Returns(tokenSet);

        var context = CreateResponseTransformContext(
            routeValues: new Dictionary<string, object?> { { "catch-all", "value" } },
            contentType: contentType
        );

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        _antiforgerySubstitute.Received(1).GetAndStoreTokens(Arg.Is<HttpContext>(ctx => ctx == context.HttpContext));
    }

    [Fact]
    public async Task ApplyAsync_WhenRequestPathIsNull_ShouldStillProcessToken()
    {
        // Arrange
        var tokenSet = CreateAntiforgeryTokenSet();
        _antiforgerySubstitute.GetAndStoreTokens(Arg.Any<HttpContext>()).Returns(tokenSet);

        var context = CreateResponseTransformContext(
            routeValues: new Dictionary<string, object?> { { "catch-all", "value" } },
            contentType: "text/html",
            requestPath: null
        );

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        _antiforgerySubstitute.Received(1).GetAndStoreTokens(Arg.Is<HttpContext>(ctx => ctx == context.HttpContext));
    }

    [Fact]
    public async Task ApplyAsync_WhenRequestPathHasValue_ShouldLogTokenAddition()
    {
        // Arrange
        var tokenSet = CreateAntiforgeryTokenSet();
        _antiforgerySubstitute.GetAndStoreTokens(Arg.Any<HttpContext>()).Returns(tokenSet);

        const string requestPath = "/index.html";
        var context = CreateResponseTransformContext(
            routeValues: new Dictionary<string, object?> { { "catch-all", "value" } },
            contentType: "text/html",
            requestPath: requestPath
        );

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        var log = _fakeLogger.Collector.GetSnapshot().Single();
        log.Level.ShouldBe(LogLevel.Information);
        log.Message.ShouldBe("XSRF token added to response for request path: /index.html");
        // log.StructuredState.ShouldContainKey("Path");
        // log.StructuredState["Path"].ShouldBe(requestPath);
    }

    [Fact]
    public void ApplyAsync_ShouldReturnCompletedValueTask()
    {
        // Arrange
        var tokenSet = CreateAntiforgeryTokenSet();
        _antiforgerySubstitute.GetAndStoreTokens(Arg.Any<HttpContext>()).Returns(tokenSet);

        var context = CreateResponseTransformContext(
            routeValues: new Dictionary<string, object?> { { "catch-all", "value" } },
            contentType: "text/html"
        );

        // Act
        var result = _transform.ApplyAsync(context);

        // Assert
        result.IsCompleted.ShouldBeTrue();
    }

    [Fact]
    public void ApplyAsync_WhenConditionsNotMet_ShouldReturnCompletedValueTask()
    {
        // Arrange
        var context = CreateResponseTransformContext(
            routeValues: new Dictionary<string, object?>(),
            contentType: "application/json"
        );

        // Act
        var result = _transform.ApplyAsync(context);

        // Assert
        result.IsCompleted.ShouldBeTrue();
    }

    [Fact]
    public async Task ApplyAsync_WithMultipleCatchAllRouteValues_ShouldProcessToken()
    {
        // Arrange
        var tokenSet = CreateAntiforgeryTokenSet();
        _antiforgerySubstitute.GetAndStoreTokens(Arg.Any<HttpContext>()).Returns(tokenSet);

        var routeValues = new Dictionary<string, object?>
        {
            { "catch-all", "path/to/resource" },
            { "controller", "Home" },
            { "action", "Index" }
        };

        var context = CreateResponseTransformContext(
            routeValues: routeValues,
            contentType: "text/html"
        );

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        _antiforgerySubstitute.Received(1).GetAndStoreTokens(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task ApplyAsync_WithEmptyStringCatchAllValue_ShouldProcessToken()
    {
        // Arrange
        var tokenSet = CreateAntiforgeryTokenSet();
        _antiforgerySubstitute.GetAndStoreTokens(Arg.Any<HttpContext>()).Returns(tokenSet);

        var context = CreateResponseTransformContext(
            routeValues: new Dictionary<string, object?> { { "catch-all", string.Empty } },
            contentType: "text/html"
        );

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        _antiforgerySubstitute.Received(1).GetAndStoreTokens(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task ApplyAsync_WhenCatchAllKeyIsNullValue_ShouldProcessToken()
    {
        // Arrange
        var tokenSet = CreateAntiforgeryTokenSet();
        _antiforgerySubstitute.GetAndStoreTokens(Arg.Any<HttpContext>()).Returns(tokenSet);

        var context = CreateResponseTransformContext(
            routeValues: new Dictionary<string, object?> { { "catch-all", null } },
            contentType: "text/html"
        );

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        _antiforgerySubstitute.Received(1).GetAndStoreTokens(Arg.Any<HttpContext>());
    }

    // Helper methods

    private static ResponseTransformContext CreateResponseTransformContext(
        Dictionary<string, object?> routeValues,
        string? contentType,
        string? requestPath = "/test")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues = new RouteValueDictionary(routeValues);
        httpContext.Request.Path = new PathString(requestPath);
        if (contentType != null)
        {
            httpContext.Response.ContentType = contentType;
        }

        return new ResponseTransformContext { HttpContext = httpContext };
    }

    private static AntiforgeryTokenSet CreateAntiforgeryTokenSet(string? requestToken = "test-token", string? formToken = "form-token")
    {
        return new AntiforgeryTokenSet(requestToken, formToken, "header-name", "form-field-name");
    }
}

