using AspireKeyCloakTemplate.SharedKernel.Features.Mediator;
using AspireKeyCloakTemplate.SharedKernel.UnitTests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AspireKeyCloakTemplate.SharedKernel.UnitTests.Features.Mediator;

public class PipelineBehaviorTests
{
    [Fact]
    public async Task Pipeline_WithSingleBehavior_ShouldCallBehavior()
    {
        // Arrange
        var services = new ServiceCollection();
        var behavior = new TestPipelineBehavior<TestRequest, TestResponse>();
        
        services.AddScoped<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        services.AddScoped<IPipelineBehavior<TestRequest, TestResponse>>(sp => behavior);
        services.AddScoped<IMediator, SharedKernel.Features.Mediator.Mediator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var request = new TestRequest("test");

        // Act
        var response = await mediator.Send(request);

        // Assert
        behavior.WasCalled.ShouldBeTrue();
        behavior.CapturedRequest.ShouldBe(request);
        response.Result.ShouldBe("Handled: test");
    }

    [Fact]
    public async Task Pipeline_WithMultipleBehaviors_ShouldCallInCorrectOrder()
    {
        // Arrange
        var services = new ServiceCollection();
        var executionOrder = new List<string>();
        
        services.AddScoped<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        
        services.AddScoped<IPipelineBehavior<TestRequest, TestResponse>>(sp => 
            new OrderTrackingBehavior(executionOrder, "First"));
        services.AddScoped<IPipelineBehavior<TestRequest, TestResponse>>(sp => 
            new OrderTrackingBehavior(executionOrder, "Second"));
        
        services.AddScoped<IMediator, SharedKernel.Features.Mediator.Mediator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        await mediator.Send(new TestRequest("test"));

        // Assert
        executionOrder.Count.ShouldBe(4); // 2 behaviors before + 2 behaviors after
        executionOrder[0].ShouldBe("First-Before");
        executionOrder[1].ShouldBe("Second-Before");
        executionOrder[2].ShouldBe("Second-After");
        executionOrder[3].ShouldBe("First-After");
    }

    [Fact]
    public async Task Pipeline_WithModifyingBehavior_ShouldModifyResponse()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        services.AddScoped<IPipelineBehavior<TestRequest, TestResponse>, ModifyingBehavior>();
        services.AddScoped<IMediator, SharedKernel.Features.Mediator.Mediator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(new TestRequest("test"));

        // Assert
        response.Result.ShouldBe("Modified: Handled: test");
    }

    [Fact]
    public async Task Pipeline_WithShortCircuitBehavior_ShouldNotCallHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        services.AddScoped<IPipelineBehavior<TestRequest, TestResponse>, ShortCircuitBehavior>();
        services.AddScoped<IMediator, SharedKernel.Features.Mediator.Mediator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(new TestRequest("test"));

        // Assert
        response.Result.ShouldBe("Short-circuited");
    }

    [Fact]
    public async Task Pipeline_WithNoBehaviors_ShouldStillCallHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        services.AddScoped<IMediator, SharedKernel.Features.Mediator.Mediator>();
        
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(new TestRequest("test"));

        // Assert
        response.Result.ShouldBe("Handled: test");
    }

    private class OrderTrackingBehavior : IPipelineBehavior<TestRequest, TestResponse>
    {
        private readonly List<string> _executionOrder;
        private readonly string _name;

        public OrderTrackingBehavior(List<string> executionOrder, string name)
        {
            _executionOrder = executionOrder;
            _name = name;
        }

        public async Task<TestResponse> Handle(
            TestRequest request,
            RequestHandlerDelegate<TestResponse> next,
            CancellationToken cancellationToken)
        {
            _executionOrder.Add($"{_name}-Before");
            var response = await next();
            _executionOrder.Add($"{_name}-After");
            return response;
        }
    }
}
