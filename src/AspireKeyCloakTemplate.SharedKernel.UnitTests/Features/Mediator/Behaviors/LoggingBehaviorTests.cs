using AspireKeyCloakTemplate.SharedKernel.Features.Mediator;
using AspireKeyCloakTemplate.SharedKernel.Features.Mediator.Behaviors;
using AspireKeyCloakTemplate.SharedKernel.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AspireKeyCloakTemplate.SharedKernel.UnitTests.Features.Mediator.Behaviors;

public class LoggingBehaviorTests
{
    [Fact]
    public async Task Handle_ShouldLogBeforeAndAfterExecution()
    {
        // Arrange
        var loggerMock = Substitute.For<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
        var handlerMock = Substitute.For<IRequestHandler<TestRequest, TestResponse>>();
        handlerMock.Handle(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TestResponse("test"));

        var behavior = new LoggingBehavior<TestRequest, TestResponse>(loggerMock);
        var request = new TestRequest("test");

        // Act
        var response = await behavior.Handle(
            request,
            () => handlerMock.Handle(request, CancellationToken.None),
            CancellationToken.None);

        // Assert
        response.ShouldNotBeNull();
        
        loggerMock.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Handling")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
        
        loggerMock.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Handled")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_WhenHandlerThrows_ShouldLogError()
    {
        // Arrange
        var loggerMock = Substitute.For<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
        var exception = new InvalidOperationException("Test exception");

        var behavior = new LoggingBehavior<TestRequest, TestResponse>(loggerMock);
        var request = new TestRequest("test");

        // Act & Assert
        await Should.ThrowAsync<InvalidOperationException>(async () =>
            await behavior.Handle(
                request,
                () => throw exception,
                CancellationToken.None));

        loggerMock.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Is<Exception>(ex => ex == exception),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_WithMediator_ShouldLogCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        var loggerMock = Substitute.For<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
        
        services.AddScoped<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        services.AddScoped(sp => loggerMock);
        services.AddScoped<IPipelineBehavior<TestRequest, TestResponse>, LoggingBehavior<TestRequest, TestResponse>>();
        services.AddScoped<IMediator, SharedKernel.Features.Mediator.Mediator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(new TestRequest("test"));

        // Assert
        response.ShouldNotBeNull();
        
        loggerMock.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Handling") && o.ToString()!.Contains("TestRequest")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}

