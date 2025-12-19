# Mediator Pattern Implementation

This directory contains a custom implementation of the Mediator pattern, similar to MediatR, for the AspireKeyCloakTemplate project.

## Overview

The mediator pattern encapsulates request/response interactions and provides a pipeline for cross-cutting concerns like validation and logging.

## Key Components

### Core Interfaces

- **`IRequest<TResponse>`** - Marker interface for requests that return a response
- **`IRequest`** - Marker interface for requests with no return value (void)
- **`IRequestHandler<TRequest, TResponse>`** - Handler interface for processing requests
- **`IMediator`** - Main mediator interface for sending requests
- **`Unit`** - Represents a void return type

### Pipeline Behaviors

- **`IPipelineBehavior<TRequest, TResponse>`** - Interface for creating pipeline behaviors
- **`ValidationBehavior<TRequest, TResponse>`** - Validates requests using FluentValidation
- **`LoggingBehavior<TRequest, TResponse>`** - Logs request execution

## Usage

### 1. Register the Mediator

In your `Program.cs`, add the mediator and scan assemblies for handlers:

```csharp
using System.Reflection;
using AspireKeyCloakTemplate.SharedKernel.Features.Mediator;

// Register mediator and scan current assembly for handlers
builder.Services.AddMediator(Assembly.GetExecutingAssembly());
```

### 2. Create a Request

```csharp
using AspireKeyCloakTemplate.SharedKernel.Features.Mediator;

public record GetUserQuery(string UserId) : IRequest<UserDto>;

public record UserDto(string Id, string Name, string Email);
```

### 3. Create a Handler

```csharp
using AspireKeyCloakTemplate.SharedKernel.Features.Mediator;

public class GetUserQueryHandler : IRequestHandler<GetUserQuery, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetUserQueryHandler> _logger;

    public GetUserQueryHandler(
        IUserRepository userRepository,
        ILogger<GetUserQueryHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<UserDto> Handle(GetUserQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        
        if (user == null)
        {
            throw new NotFoundException($"User {request.UserId} not found");
        }

        return new UserDto(user.Id, user.Name, user.Email);
    }
}
```

### 4. Create a Validator (Optional)

```csharp
using FluentValidation;

public class GetUserQueryValidator : AbstractValidator<GetUserQuery>
{
    public GetUserQueryValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("UserId is required");
    }
}
```

### 5. Send Requests

In your endpoints or controllers:

```csharp
app.MapGet("/users/{id}", async (string id, IMediator mediator) =>
{
    var user = await mediator.Send(new GetUserQuery(id));
    return Results.Ok(user);
});
```

### Void Requests (Commands)

For requests that don't return a value:

```csharp
// Request
public record DeleteUserCommand(string UserId) : IRequest;

// Handler
public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
{
    private readonly IUserRepository _userRepository;

    public DeleteUserCommandHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<Unit> Handle(DeleteUserCommand request, CancellationToken cancellationToken)
    {
        await _userRepository.DeleteAsync(request.UserId, cancellationToken);
        return Unit.Value;
    }
}

// Usage
await mediator.Send(new DeleteUserCommand("user-123"));
```

## Pipeline Behaviors

### Built-in Behaviors

1. **LoggingBehavior** - Automatically logs when requests are handled
2. **ValidationBehavior** - Automatically validates requests using FluentValidation

### Execution Order

Behaviors are executed in the order they are registered:

```
LoggingBehavior (before)
  → ValidationBehavior (before)
    → Handler
  → ValidationBehavior (after)
LoggingBehavior (after)
```

### Creating Custom Behaviors

```csharp
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        
        var response = await next();
        
        stopwatch.Stop();
        
        if (stopwatch.ElapsedMilliseconds > 500)
        {
            _logger.LogWarning(
                "Long running request: {RequestName} took {ElapsedMilliseconds}ms",
                typeof(TRequest).Name,
                stopwatch.ElapsedMilliseconds);
        }
        
        return response;
    }
}

// Register in Program.cs
services.AddScoped(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
```

## Service Lifetime

All components are registered as **Scoped** services:
- Mediator
- Request Handlers
- Validators
- Pipeline Behaviors

This ensures proper lifecycle management within HTTP request boundaries.

## Testing

See the `AspireKeyCloakTemplate.SharedKernel.UnitTests` project for comprehensive test examples including:
- Unit tests for the mediator
- Pipeline behavior tests
- Validation behavior tests
- Logging behavior tests
- Integration tests

## Architecture Patterns

### CQRS (Command Query Responsibility Segregation)

Use this pattern to separate read (queries) and write (commands) operations:

```csharp
// Queries - Read operations
public record GetUserQuery(string UserId) : IRequest<UserDto>;
public record GetUsersQuery(int PageSize, int PageNumber) : IRequest<PagedResult<UserDto>>;

// Commands - Write operations
public record CreateUserCommand(string Name, string Email) : IRequest<string>; // Returns UserId
public record UpdateUserCommand(string UserId, string Name) : IRequest;
public record DeleteUserCommand(string UserId) : IRequest;
```

### Vertical Slice Architecture

Organize features by use case rather than technical layer:

```
Features/
├── Users/
│   ├── Commands/
│   │   ├── CreateUser/
│   │   │   ├── CreateUserCommand.cs
│   │   │   ├── CreateUserCommandHandler.cs
│   │   │   └── CreateUserCommandValidator.cs
│   │   └── DeleteUser/
│   │       ├── DeleteUserCommand.cs
│   │       └── DeleteUserCommandHandler.cs
│   └── Queries/
│       └── GetUser/
│           ├── GetUserQuery.cs
│           ├── GetUserQueryHandler.cs
│           └── GetUserQueryValidator.cs
```

## Benefits

- **Decoupling**: Controllers/endpoints don't need to know about handler implementations
- **Single Responsibility**: Each handler focuses on one specific use case
- **Cross-cutting Concerns**: Pipeline behaviors handle validation, logging, etc. consistently
- **Testability**: Handlers and behaviors can be tested in isolation
- **Maintainability**: Clear separation of concerns and predictable patterns

## Differences from MediatR

This implementation is a simplified version of MediatR focusing on:
- Request/Response pattern
- Pipeline behaviors
- Validation and logging

Not included (compared to full MediatR):
- Notification/Event publishing (INotification)
- Stream requests (IStreamRequest)
- Pre/Post processors
- Request/Response pre/post processors

These features can be added later if needed.

