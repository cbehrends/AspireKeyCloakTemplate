# Mediator Pattern Implementation

This directory contains a custom implementation of the Mediator pattern for the AspireKeyCloakTemplate project.

## Overview

The mediator pattern encapsulates request/response interactions and provides a pipeline for cross-cutting concerns like
validation, logging and caching.

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
- **`CachingBehavior<TRequest, TResponse>`** - Caches responses for cacheable requests

### Caching

- **`ICacheableRequest<TResponse>`** - Interface for requests that can be cached
- **`CacheInvalidationNotification`** - Notification to invalidate a single cache item
- **`CacheGroupInvalidationNotification`** - Notification to invalidate a group of cache items

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
3. **CachingBehavior** - Automatically caches responses for requests implementing `ICacheableRequest<TResponse>`

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

## Caching

The mediator includes a built-in caching behavior that uses `IDistributedCache` to cache responses for cacheable requests.

### Making a Request Cacheable

Implement `ICacheableRequest<TResponse>` to enable caching for a request:

```csharp
using AspireKeyCloakTemplate.SharedKernel.Features.Mediator.Caching;

public record GetUserQuery(string UserId) : ICacheableRequest<UserDto>
{
    public string CacheKey => $"user:{UserId}";
    public string? CacheGroupKey => "users";
    public TimeSpan? AbsoluteExpirationRelativeToNow => TimeSpan.FromMinutes(5);
}
```

### ICacheableRequest Properties

- **`CacheKey`** - Unique key for the cached item
- **`CacheGroupKey`** - Optional group key for invalidating multiple related cache items at once
- **`AbsoluteExpirationRelativeToNow`** - Optional expiration time for the cached item

### Cache Invalidation

#### Invalidating a Single Cache Item

Use `CacheInvalidationNotification` to invalidate a specific cache item:

```csharp
using AspireKeyCloakTemplate.SharedKernel.Features.Mediator.Caching;

// After updating a user
await mediator.Publish(new CacheInvalidationNotification($"user:{userId}"));
```

#### Invalidating a Group of Cache Items

Use `CacheGroupInvalidationNotification` to invalidate all cache items in a group:

```csharp
using AspireKeyCloakTemplate.SharedKernel.Features.Mediator.Caching;

// After a bulk operation that affects all users
await mediator.Publish(new CacheGroupInvalidationNotification("users"));
```

### How Cache Groups Work

When a cacheable request with a `CacheGroupKey` is cached:
1. The response is stored with the `CacheKey`
2. The `CacheKey` is added to a set tracked under the `CacheGroupKey`

When invalidating a group:
1. All cache keys in the group are retrieved
2. Each individual cache item is removed
3. The group tracking set is removed

### Example: Complete Caching Flow

```csharp
// Query with caching
public record GetProductQuery(string ProductId) : ICacheableRequest<ProductDto>
{
    public string CacheKey => $"product:{ProductId}";
    public string? CacheGroupKey => "products";
    public TimeSpan? AbsoluteExpirationRelativeToNow => TimeSpan.FromMinutes(10);
}

// Command that invalidates cache
public class UpdateProductCommandHandler : IRequestHandler<UpdateProductCommand>
{
    private readonly IProductRepository _repository;
    private readonly IMediator _mediator;

    public UpdateProductCommandHandler(IProductRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(UpdateProductCommand request, CancellationToken cancellationToken)
    {
        await _repository.UpdateAsync(request.Product, cancellationToken);
        
        // Invalidate the specific product cache
        await _mediator.Publish(
            new CacheInvalidationNotification($"product:{request.Product.Id}"), 
            cancellationToken);
        
        return Unit.Value;
    }
}

// Command that invalidates entire cache group
public class ImportProductsCommandHandler : IRequestHandler<ImportProductsCommand>
{
    private readonly IProductRepository _repository;
    private readonly IMediator _mediator;

    public ImportProductsCommandHandler(IProductRepository repository, IMediator mediator)
    {
        _repository = repository;
        _mediator = mediator;
    }

    public async Task<Unit> Handle(ImportProductsCommand request, CancellationToken cancellationToken)
    {
        await _repository.BulkInsertAsync(request.Products, cancellationToken);
        
        // Invalidate all product caches
        await _mediator.Publish(
            new CacheGroupInvalidationNotification("products"), 
            cancellationToken);
        
        return Unit.Value;
    }
}
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


