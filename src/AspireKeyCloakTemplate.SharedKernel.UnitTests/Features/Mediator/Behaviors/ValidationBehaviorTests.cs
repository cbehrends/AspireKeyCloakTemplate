using AspireKeyCloakTemplate.SharedKernel.Features.Mediator;
using AspireKeyCloakTemplate.SharedKernel.Features.Mediator.Behaviors;
using AspireKeyCloakTemplate.SharedKernel.UnitTests.Helpers;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace AspireKeyCloakTemplate.SharedKernel.UnitTests.Features.Mediator.Behaviors;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithValidRequest_ShouldCallNext()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidator<TestRequest>, AlwaysValidValidator>();
        services.AddScoped<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<IMediator, SharedKernel.Features.Mediator.Mediator>();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var request = new TestRequest("valid");

        // Act
        var response = await mediator.Send(request);

        // Assert
        response.ShouldNotBeNull();
        response.Result.ShouldBe("Handled: valid");
    }

    [Fact]
    public async Task Handle_WithInvalidRequest_ShouldThrowValidationException()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidator<TestRequest>, TestRequestValidator>();
        services.AddScoped<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<IMediator, SharedKernel.Features.Mediator.Mediator>();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var request = new TestRequest(""); // Empty string - invalid

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(async () =>
            await mediator.Send(request));

        exception.Errors.ShouldNotBeEmpty();
        exception.Errors.ShouldContain(e => e.ErrorMessage == "Value cannot be empty");
    }

    [Fact]
    public async Task Handle_WithMultipleValidationErrors_ShouldThrowWithAllErrors()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidator<TestRequest>, TestRequestValidator>();
        services.AddScoped<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<IMediator, SharedKernel.Features.Mediator.Mediator>();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var request = new TestRequest("ab"); // Too short - violates MinimumLength rule

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(async () =>
            await mediator.Send(request));

        exception.Errors.Count().ShouldBeGreaterThan(0);
        exception.Errors.ShouldContain(e => e.ErrorMessage.Contains("at least 3 characters"));
    }

    [Fact]
    public async Task Handle_WithNoValidators_ShouldCallNext()
    {
        // Arrange
        var services = new ServiceCollection();
        // No validators registered
        services.AddScoped<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
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
    public async Task Handle_WithMultipleValidators_ShouldRunAllValidators()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddScoped<IValidator<TestRequest>, TestRequestValidator>();
        services.AddScoped<IValidator<TestRequest>>(sp => new SecondValidator());
        services.AddScoped<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<IMediator, SharedKernel.Features.Mediator.Mediator>();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var request = new TestRequest("invalid_value"); // Contains underscore - invalid per SecondValidator

        // Act & Assert
        var exception = await Should.ThrowAsync<ValidationException>(async () =>
            await mediator.Send(request));

        exception.Errors.ShouldContain(e => e.ErrorMessage == "Value cannot contain underscores");
    }

    [Fact]
    public async Task Handle_WithCancellationToken_ShouldPassToValidator()
    {
        // Arrange
        var services = new ServiceCollection();
        var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        services.AddScoped<IValidator<TestRequest>>(sp => new AsyncValidator());
        services.AddScoped<IRequestHandler<TestRequest, TestResponse>, TestRequestHandler>();
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddScoped<IMediator, SharedKernel.Features.Mediator.Mediator>();

        var serviceProvider = services.BuildServiceProvider();
        var mediator = serviceProvider.GetRequiredService<IMediator>();
        var request = new TestRequest("test");

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await mediator.Send(request, cancellationTokenSource.Token));
    }

    private class SecondValidator : AbstractValidator<TestRequest>
    {
        public SecondValidator()
        {
            RuleFor(x => x.Value)
                .Must(value => !value.Contains('_'))
                .WithMessage("Value cannot contain underscores");
        }
    }

    private class AsyncValidator : AbstractValidator<TestRequest>
    {
        public AsyncValidator()
        {
            RuleFor(x => x.Value)
                .MustAsync(async (value, cancellationToken) =>
                {
                    await Task.Delay(100, cancellationToken);
                    return true;
                });
        }
    }
}
