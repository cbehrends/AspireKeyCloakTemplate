# Mediator Pattern - Quick Start Guide

## 🚀 Quick Start

This guide will get you up and running with the mediator pattern in 5 minutes.

## Step 1: Verify Installation

The mediator pattern is already installed in `AspireKeyCloakTemplate.SharedKernel`. Verify by checking:

```bash
# Build the SharedKernel project
dotnet build src/AspireKeyCloakTemplate.SharedKernel/AspireKeyCloakTemplate.SharedKernel.csproj

# Run the tests
dotnet test src/AspireKeyCloakTemplate.SharedKernel.UnitTests/AspireKeyCloakTemplate.SharedKernel.UnitTests.csproj
```

## Step 2: Register in Your Project

In your `Program.cs` (already done in BFF):

```csharp
using System.Reflection;
using AspireKeyCloakTemplate.SharedKernel.Features.Mediator;

// Add this line
builder.Services.AddMediator(Assembly.GetExecutingAssembly());
```

## Step 3: Create Your First Query

Create a new file: `Features/YourFeature/Queries/GetSomething/GetSomethingQuery.cs`

```csharp
using AspireKeyCloakTemplate.SharedKernel.Features.Mediator;

namespace YourNamespace.Features.YourFeature.Queries.GetSomething;

public record GetSomethingQuery(string Id) : IRequest<SomethingDto>;

public record SomethingDto(string Id, string Name);
```

## Step 4: Create the Handler

Create: `Features/YourFeature/Queries/GetSomething/GetSomethingQueryHandler.cs`

```csharp
using AspireKeyCloakTemplate.SharedKernel.Features.Mediator;

namespace YourNamespace.Features.YourFeature.Queries.GetSomething;

public class GetSomethingQueryHandler : IRequestHandler<GetSomethingQuery, SomethingDto>
{
    private readonly ILogger<GetSomethingQueryHandler> _logger;

    public GetSomethingQueryHandler(ILogger<GetSomethingQueryHandler> logger)
    {
        _logger = logger;
    }

    public async Task<SomethingDto> Handle(GetSomethingQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Handling GetSomethingQuery for Id: {Id}", request.Id);
        
        // Your logic here
        var result = new SomethingDto(request.Id, "Sample Name");
        
        return await Task.FromResult(result);
    }
}
```

## Step 5: Use in an Endpoint

Create: `Features/YourFeature/Endpoints/GetSomethingEndpoint.cs`

```csharp
using AspireKeyCloakTemplate.SharedKernel.Features.Endpoints;
using AspireKeyCloakTemplate.SharedKernel.Features.Mediator;
using YourNamespace.Features.YourFeature.Queries.GetSomething;

namespace YourNamespace.Features.YourFeature.Endpoints;

internal class GetSomethingEndpoint : IEndpoint
{
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/something/{id}", async (string id, IMediator mediator) =>
        {
            var result = await mediator.Send(new GetSomethingQuery(id));
            return Results.Ok(result);
        })
        .WithName("GetSomething");

        return builder;
    }
}
```

## Step 6: Add Validation (Optional)

Create: `Features/YourFeature/Queries/GetSomething/GetSomethingQueryValidator.cs`

```csharp
using FluentValidation;

namespace YourNamespace.Features.YourFeature.Queries.GetSomething;

public class GetSomethingQueryValidator : AbstractValidator<GetSomethingQuery>
{
    public GetSomethingQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Id is required")
            .MaximumLength(50)
            .WithMessage("Id must not exceed 50 characters");
    }
}
```

## Step 7: Test It!

Run your application and test the endpoint:

```bash
curl http://localhost:5000/bff/something/123
```

## 📝 Working Example

See the working example in the BFF project:
- Query: `src/AspireKeyCloakTemplate.BFF/Features/Users/Queries/GetCurrentUser/GetCurrentUserQuery.cs`
- Handler: `src/AspireKeyCloakTemplate.BFF/Features/Users/Queries/GetCurrentUser/GetCurrentUserQueryHandler.cs`
- Endpoint: `src/AspireKeyCloakTemplate.BFF/Features/Users/Endpoints/GetCurrentUserEndpoint.cs`

Test the example:
```bash
curl http://localhost:5000/bff/user/current
```

## 🎯 Commands (Write Operations)

For operations that modify data:

```csharp
// Command (no return value)
public record CreateSomethingCommand(string Name) : IRequest;

// Command (with return value)
public record CreateSomethingCommand(string Name) : IRequest<string>; // Returns ID

// Handler
public class CreateSomethingCommandHandler : IRequestHandler<CreateSomethingCommand, string>
{
    public async Task<string> Handle(CreateSomethingCommand request, CancellationToken cancellationToken)
    {
        // Create logic
        var id = Guid.NewGuid().ToString();
        return id;
    }
}

// Usage
var newId = await mediator.Send(new CreateSomethingCommand("New Item"));
```

## 🔍 What You Get Automatically

When you use the mediator, you automatically get:

1. **Validation** - All validators are run before the handler
2. **Logging** - Request handling is logged
3. **Dependency Injection** - Handlers can inject any service
4. **Testability** - Handlers can be tested in isolation
5. **Clean Code** - Separation of concerns

## 🐛 Troubleshooting

### Handler not found
- Make sure handler is in the assembly you registered
- Verify handler implements `IRequestHandler<TRequest, TResponse>`

### Validation not working
- Ensure FluentValidation package is installed
- Validator must inherit `AbstractValidator<TRequest>`
- Validator must be in the registered assembly

### Tests failing
```bash
# Restore and rebuild
dotnet restore
dotnet build

# Run tests
dotnet test src/AspireKeyCloakTemplate.SharedKernel.UnitTests/AspireKeyCloakTemplate.SharedKernel.UnitTests.csproj
```

## 📚 More Information

- Full documentation: `src/AspireKeyCloakTemplate.SharedKernel/Features/Mediator/README.md`
- Implementation summary: `MEDIATOR_IMPLEMENTATION_SUMMARY.md`
- Test examples: `src/AspireKeyCloakTemplate.SharedKernel.UnitTests/`

## ✅ Checklist

- [ ] Mediator registered in Program.cs
- [ ] Created query/command record
- [ ] Created handler class
- [ ] Added validator (optional)
- [ ] Created endpoint
- [ ] Tested the endpoint

Happy coding! 🎉

