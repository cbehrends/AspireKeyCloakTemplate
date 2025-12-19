using AspireKeyCloakTemplate.SharedKernel.Features.Mediator;
using AspireKeyCloakTemplate.SharedKernel.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Xunit;

namespace AspireKeyCloakTemplate.SharedKernel.UnitTests.Features.Mediator;

public class MediatorTests
{
    [Fact]
    public async Task Send_WithValidRequest_ShouldCallHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        services.AddScoped<IMediator, SharedKernel.Features.Mediator.Mediator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var request = new TestRequest("test");

        // Act
        var response = await mediator.Send(request);

        // Assert
        response.ShouldNotBeNull();
        response.Result.ShouldBe("Handled: test");
    }

    [Fact]
    public async Task Send_WithVoidRequest_ShouldReturnUnit()
    {
        // Arrange
        var services = new ServiceCollection();
        var handler = new TestVoidRequestHandler();
        services.AddScoped<IRequestHandler<TestVoidRequest, Unit>>(sp => handler);
        services.AddScoped<IMediator, SharedKernel.Features.Mediator.Mediator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var request = new TestVoidRequest("test");

        // Act
        var response = await mediator.Send(request);

        // Assert
        response.ShouldBe(Unit.Value);
        handler.WasCalled.ShouldBeTrue();
    }

    [Fact]
    public async Task Send_WithNullRequest_ShouldThrowArgumentNullException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IMediator, SharedKernel.Features.Mediator.Mediator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act & Assert
        await Should.ThrowAsync<ArgumentNullException>(async () =>
            await mediator.Send<TestResponse>(null!));
    }

    [Fact]
    public async Task Send_WithoutRegisteredHandler_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IMediator, SharedKernel.Features.Mediator.Mediator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var request = new TestRequest("test");

        // Act & Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(async () =>
            await mediator.Send(request));
        
        exception.Message.ShouldContain("Handler not found");
    }

    [Fact]
    public async Task Send_WithMultipleRequests_ShouldCallCorrectHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        services.AddScoped<IRequestHandler<GenericRequest<string>, string>, GenericRequestHandler<string>>();
        services.AddScoped<IMediator, SharedKernel.Features.Mediator.Mediator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        var response1 = await mediator.Send(new TestRequest("test1"));
        var response2 = await mediator.Send(new GenericRequest<string>("test2"));

        // Assert
        response1.Result.ShouldBe("Handled: test1");
        response2.ShouldBe("test2");
    }

    [Fact]
    public async Task Send_WithCancellationToken_ShouldPassToHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        var handlerMock = Substitute.For<IRequestHandler<TestRequest, TestResponse>>();
        handlerMock.Handle(Arg.Any<TestRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TestResponse("mocked"));
        
        services.AddScoped(sp => handlerMock);
        services.AddScoped<IMediator, SharedKernel.Features.Mediator.Mediator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var request = new TestRequest("test");
        var cancellationToken = new CancellationToken();

        // Act
        await mediator.Send(request, cancellationToken);

        // Assert
        await handlerMock.Received(1).Handle(request, cancellationToken);
    }

    [Fact]
    public async Task Send_WithGenericTypes_ShouldWorkCorrectly()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IRequestHandler<GenericRequest<int>, int>, GenericRequestHandler<int>>();
        services.AddScoped<IMediator, SharedKernel.Features.Mediator.Mediator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(new GenericRequest<int>(42));

        // Assert
        response.ShouldBe(42);
    }
}

