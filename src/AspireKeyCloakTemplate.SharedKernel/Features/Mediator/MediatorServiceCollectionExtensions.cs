using System.Reflection;
using AspireKeyCloakTemplate.SharedKernel.Features.Mediator.Behaviors;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AspireKeyCloakTemplate.SharedKernel.Features.Mediator;

public static class MediatorServiceCollectionExtensions
{
    /// <summary>
    ///     Registers mediator and handlers from specified assemblies
    /// </summary>
    public static IServiceCollection AddMediator(this IServiceCollection services, params Assembly[] assemblies)
    {
        return services.AddMediator(configuration =>
        {
            configuration.RegisterServicesFromAssemblies(assemblies)
                .AddCachingBehavior();
        });
    }

    /// <summary>
    ///     Registers mediator with configuration
    /// </summary>
    public static IServiceCollection AddMediator(this IServiceCollection services,
        Action<MediatorConfiguration> configuration)
    {
        var config = new MediatorConfiguration(services);
        configuration(config);

        services.AddScoped<IMediator, Mediator>();

        return services;
    }
}

public class MediatorConfiguration
{
    private readonly IServiceCollection _services;

    public MediatorConfiguration(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    ///     Register handlers and validators from assemblies
    /// </summary>
    public MediatorConfiguration RegisterServicesFromAssemblies(params Assembly[] assemblies)
    {
        if (assemblies.Length == 0) throw new ArgumentException("No assemblies provided to scan", nameof(assemblies));

        RegisterHandlers(assemblies);
        RegisterValidators(assemblies);
        RegisterBehaviors();

        return this;
    }

    private void RegisterHandlers(Assembly[] assemblies)
    {
        var handlerTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && !t.ContainsGenericParameters)
            .Select(t => new
            {
                Type = t,
                Interfaces = t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IRequestHandler<,>))
                    .ToList()
            })
            .Where(t => t.Interfaces.Any())
            .ToList();

        foreach (var handlerType in handlerTypes)
        foreach (var @interface in handlerType.Interfaces)
            _services.AddScoped(@interface, handlerType.Type);
    }

    private void RegisterValidators(Assembly[] assemblies)
    {
        var validatorTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && !t.ContainsGenericParameters)
            .Select(t => new
            {
                Type = t,
                Interfaces = t.GetInterfaces()
                    .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IValidator<>))
                    .ToList()
            })
            .Where(t => t.Interfaces.Any())
            .ToList();

        foreach (var validatorType in validatorTypes)
        foreach (var @interface in validatorType.Interfaces)
            _services.AddScoped(@interface, validatorType.Type);
    }

    private void RegisterBehaviors()
    {
        _services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        _services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
    }

    public MediatorConfiguration AddCachingBehavior()
    {
        _services.AddScoped(typeof(IPipelineBehavior<,>), typeof(CachingBehavior<,>));
        return this;
    }
}
