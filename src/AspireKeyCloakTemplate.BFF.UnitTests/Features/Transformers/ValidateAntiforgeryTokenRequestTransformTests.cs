using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Shouldly;
using Xunit;
using Yarp.ReverseProxy.Transforms;
using ValidateAntiforgeryTokenRequestTransform = AspireKeyCloakTemplate.BFF.Features.Transformers.ValidateAntiforgeryTokenRequestTransform;

namespace AspireKeyCloakTemplate.BFF.UnitTests.Features.Transformers;

public class ValidateAntiforgeryTokenRequestTransformTests
{
    private readonly IAntiforgery _antiforgerySubstitute;
    private readonly FakeLogger<ValidateAntiforgeryTokenRequestTransform> _fakeLogger;
    private readonly ValidateAntiforgeryTokenRequestTransform _transform;

    public ValidateAntiforgeryTokenRequestTransformTests()
    {
        _antiforgerySubstitute = Substitute.For<IAntiforgery>();
        _fakeLogger = new FakeLogger<ValidateAntiforgeryTokenRequestTransform>();
        _transform = new ValidateAntiforgeryTokenRequestTransform(_antiforgerySubstitute, _fakeLogger);
    }

    [Fact]
    public async Task ApplyAsync_WithGetRequest_ShouldSkipValidation()
    {
        // Arrange
        var context = CreateRequestTransformContext(HttpMethod.Get.Method);

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        await _antiforgerySubstitute.DidNotReceive().ValidateRequestAsync(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task ApplyAsync_WithHeadRequest_ShouldSkipValidation()
    {
        // Arrange
        var context = CreateRequestTransformContext(HttpMethod.Head.Method);

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        await _antiforgerySubstitute.DidNotReceive().ValidateRequestAsync(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task ApplyAsync_WithOptionsRequest_ShouldSkipValidation()
    {
        // Arrange
        var context = CreateRequestTransformContext(HttpMethod.Options.Method);

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        await _antiforgerySubstitute.DidNotReceive().ValidateRequestAsync(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task ApplyAsync_WithTraceRequest_ShouldSkipValidation()
    {
        // Arrange
        var context = CreateRequestTransformContext(HttpMethod.Trace.Method);

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        await _antiforgerySubstitute.DidNotReceive().ValidateRequestAsync(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task ApplyAsync_WithProtobufContentType_ShouldSkipValidation()
    {
        // Arrange
        var context = CreateRequestTransformContext(
            HttpMethod.Post.Method,
            "application/x-protobuf"
        );

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        await _antiforgerySubstitute.DidNotReceive().ValidateRequestAsync(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task ApplyAsync_WithPostRequestAndValidToken_ShouldValidateSuccessfully()
    {
        // Arrange
        var context = CreateRequestTransformContext(
            HttpMethod.Post.Method,
            "application/json"
        );

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        await _antiforgerySubstitute.Received(1)
            .ValidateRequestAsync(Arg.Is<HttpContext>(ctx => ctx == context.HttpContext));
        context.HttpContext.Response.StatusCode.ShouldNotBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task ApplyAsync_WithPutRequest_ShouldValidateToken()
    {
        // Arrange
        var context = CreateRequestTransformContext(HttpMethod.Put.Method);

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        await _antiforgerySubstitute.Received(1).ValidateRequestAsync(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task ApplyAsync_WithPatchRequest_ShouldValidateToken()
    {
        // Arrange
        var context = CreateRequestTransformContext(HttpMethod.Patch.Method);

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        await _antiforgerySubstitute.Received(1).ValidateRequestAsync(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task ApplyAsync_WithDeleteRequest_ShouldValidateToken()
    {
        // Arrange
        var context = CreateRequestTransformContext(HttpMethod.Delete.Method);

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        await _antiforgerySubstitute.Received(1).ValidateRequestAsync(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task ApplyAsync_WhenValidationThrowsAntiforgeryException_ShouldSetStatusTo400()
    {
        // Arrange
        var exception = new AntiforgeryValidationException("Invalid token");
        _antiforgerySubstitute.ValidateRequestAsync(Arg.Any<HttpContext>()).Throws(exception);

        var context = CreateRequestTransformContext(HttpMethod.Post.Method);

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        context.HttpContext.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task ApplyAsync_WhenValidationFails_ShouldLogError()
    {
        // Arrange
        var exception = new AntiforgeryValidationException("Invalid token");
        _antiforgerySubstitute.ValidateRequestAsync(Arg.Any<HttpContext>()).Throws(exception);

        const string requestPath = "/api/users";
        var context = CreateRequestTransformContext(
            HttpMethod.Post.Method,
            requestPath: requestPath
        );

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        var errorLogs = _fakeLogger.Collector.GetSnapshot().Where(l => l.Level == LogLevel.Error).ToList();
        errorLogs.ShouldNotBeEmpty();
        errorLogs.First().Message.ShouldContain("Antiforgery token validation failed");
        errorLogs.First().Message.ShouldContain(requestPath);
    }

    [Fact]
    public async Task ApplyAsync_WithValidPostRequest_ShouldNotSetErrorStatus()
    {
        // Arrange
        var context = CreateRequestTransformContext(HttpMethod.Post.Method);

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        context.HttpContext.Response.StatusCode.ShouldNotBe(StatusCodes.Status400BadRequest);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("PATCH")]
    [InlineData("DELETE")]
    public async Task ApplyAsync_WithNonSafeMethod_ShouldAttemptValidation(string method)
    {
        // Arrange
        var context = CreateRequestTransformContext(method);

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        await _antiforgerySubstitute.Received(1).ValidateRequestAsync(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task ApplyAsync_WithNullRequestPath_ShouldStillValidateToken()
    {
        // Arrange
        var context = CreateRequestTransformContext(
            HttpMethod.Post.Method,
            requestPath: null
        );

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        await _antiforgerySubstitute.Received(1)
            .ValidateRequestAsync(Arg.Is<HttpContext>(ctx => ctx == context.HttpContext));
    }

    [Fact]
    public async Task ApplyAsync_ShouldCallApplyAsyncWithLogging()
    {
        // Arrange
        const string requestPath = "/api/data";
        var context = CreateRequestTransformContext(
            HttpMethod.Post.Method,
            requestPath: requestPath
        );

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        var logs = _fakeLogger.Collector.GetSnapshot();
        var applyCalled = logs.Any(l => l.Message.Contains("ApplyAsync called"));
        applyCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task ApplyAsync_WithPostRequestAndProtobufContentType_ShouldSkipValidation()
    {
        // Arrange
        var context = CreateRequestTransformContext(
            HttpMethod.Post.Method,
            "application/x-protobuf"
        );

        // Act
        await _transform.ApplyAsync(context);

        // Assert
        await _antiforgerySubstitute.DidNotReceive().ValidateRequestAsync(Arg.Any<HttpContext>());
    }

    [Fact]
    public async Task ApplyAsync_WhenExceptionOccurs_ShouldCatchAntiforgeryValidationException()
    {
        // Arrange
        var exception = new AntiforgeryValidationException("Token mismatch");
        _antiforgerySubstitute.ValidateRequestAsync(Arg.Any<HttpContext>()).Throws(exception);

        var context = CreateRequestTransformContext(HttpMethod.Post.Method);

        // Act & Assert - Should not throw
        await _transform.ApplyAsync(context);
        context.HttpContext.Response.StatusCode.ShouldBe(StatusCodes.Status400BadRequest);
    }

    // Helper methods

    private static RequestTransformContext CreateRequestTransformContext(
        string method,
        string? contentType = null,
        string? requestPath = "/test")
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = method;
        if (requestPath != null) httpContext.Request.Path = new PathString(requestPath);
        if (contentType != null) httpContext.Request.Headers.ContentType = contentType;

        var proxyRequest = new HttpRequestMessage();
        return new RequestTransformContext { HttpContext = httpContext, ProxyRequest = proxyRequest };
    }
}
