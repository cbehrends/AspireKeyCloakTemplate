using System.Reflection;
using AspireKeyCloakTemplate.SharedKernel.Features.Mediator;
using AspireKeyCloakTemplate.SharedKernel.UnitTests.Helpers;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AspireKeyCloakTemplate.SharedKernel.UnitTests.Features.Mediator;

public class MediatorServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMediator_WithAssemblies_ShouldRegisterMediator()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddMediator(assembly);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetService<IMediator>();
        mediator.ShouldNotBeNull();
        mediator.ShouldBeOfType<SharedKernel.Features.Mediator.Mediator>();
    }

    [Fact]
    public void AddMediator_WithAssemblies_ShouldRegisterHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddMediator(assembly);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var handler = serviceProvider.GetService<IRequestHandler<TestRequest, TestResponse>>();
        handler.ShouldNotBeNull();
        handler.ShouldBeOfType<TestRequestHandler>();
    }

    [Fact]
    public void AddMediator_WithAssemblies_ShouldRegisterValidators()
    {
        // Arrange
        var services = new ServiceCollection();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddMediator(assembly);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var validators = serviceProvider.GetServices<IValidator<TestRequest>>().ToList();
        validators.ShouldNotBeEmpty();
        validators.ShouldContain(v => v.GetType() == typeof(TestRequestValidator));
    }

    [Fact]
    public void AddMediator_WithAssemblies_ShouldRegisterBehaviors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        var assembly = Assembly.GetExecutingAssembly();

        // Act
        services.AddMediator(assembly);

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TestRequest, TestResponse>>().ToList();
        behaviors.ShouldNotBeEmpty();
        behaviors.Count.ShouldBeGreaterThanOrEqualTo(2); // LoggingBehavior and ValidationBehavior
    }

    [Fact]
    public void AddMediator_WithNoAssemblies_ShouldThrowArgumentException()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act & Assert
        Should.Throw<ArgumentException>(() => services.AddMediator());
    }

    [Fact]
    public void AddMediator_WithConfiguration_ShouldRegisterMediator()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddMediator(config => { config.RegisterServicesFromAssemblies(Assembly.GetExecutingAssembly()); });

        // Assert
        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetService<IMediator>();
        mediator.ShouldNotBeNull();
    }

    [Fact]
    public async Task AddMediator_IntegrationTest_ShouldWorkEndToEnd()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMediator(Assembly.GetExecutingAssembly());

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();

        // Act
        var response = await mediator.Send(new TestRequest("integration test"));

        // Assert
        response.ShouldNotBeNull();
        response.Result.ShouldBe("Handled: integration test");
    }

    [Fact]
    public void AddMediator_ShouldRegisterHandlersAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(Assembly.GetExecutingAssembly());

        // Assert
        var handlerDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IRequestHandler<TestRequest, TestResponse>));

        handlerDescriptor.ShouldNotBeNull();
        handlerDescriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddMediator_ShouldRegisterValidatorsAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(Assembly.GetExecutingAssembly());

        // Assert
        var validatorDescriptor = services.FirstOrDefault(d =>
            d.ServiceType == typeof(IValidator<TestRequest>));

        validatorDescriptor.ShouldNotBeNull();
        validatorDescriptor.Lifetime.ShouldBe(ServiceLifetime.Scoped);
    }

    [Fact]
    public void AddMediator_ShouldRegisterBehaviorsAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator(Assembly.GetExecutingAssembly());

        // Assert
        var behaviorDescriptors = services.Where(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>)).ToList();

        behaviorDescriptors.ShouldNotBeEmpty();
        behaviorDescriptors.ShouldAllBe(d => d.Lifetime == ServiceLifetime.Scoped);
    }
}
