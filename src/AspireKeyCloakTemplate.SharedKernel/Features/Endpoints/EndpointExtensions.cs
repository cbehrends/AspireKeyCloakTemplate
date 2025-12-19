using System.Reflection;
using Microsoft.AspNetCore.Routing;

namespace AspireKeyCloakTemplate.SharedKernel.Features.Endpoints;

public static class EndpointExtensions
{
    /// <summary>
    ///     Scans the specified assembly for classes implementing IEndpoint and maps their endpoints.
    /// </summary>
    /// <param name="builder">The endpoint route builder</param>
    /// <param name="assembly">The assembly to scan for IEndpoint implementations</param>
    /// <returns>The endpoint route builder for chaining</returns>
    public static IEndpointRouteBuilder MapEndpointsFromAssembly(this IEndpointRouteBuilder builder, Assembly assembly)
    {
        var endpointType = typeof(IEndpoint);
        var endpointTypes = assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && endpointType.IsAssignableFrom(t))
            .ToList();

        foreach (var instance in endpointTypes.Select(Activator.CreateInstance).OfType<IEndpoint>())
            instance.MapEndpoints(builder);

        return builder;
    }

    /// <summary>
    ///     Scans the calling assembly for classes implementing IEndpoint and maps their endpoints.
    /// </summary>
    /// <param name="builder">The endpoint route builder</param>
    /// <returns>The endpoint route builder for chaining</returns>
    public static IEndpointRouteBuilder MapEndpointsFromCallingAssembly(this IEndpointRouteBuilder builder)
    {
        var callingAssembly = Assembly.GetCallingAssembly();
        return builder.MapEndpointsFromAssembly(callingAssembly);
    }
}
