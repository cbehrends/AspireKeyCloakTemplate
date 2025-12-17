using Microsoft.AspNetCore.Routing;
using System.Reflection;

namespace AspireKeyCloakTemplate.ServiceDefaults.Features.Endpoints;

public static class EndpointExtensions
{
    /// <summary>
    /// Scans the specified assembly for classes implementing IEndpoint and maps their endpoints.
    /// </summary>
    /// <param name="builder">The endpoint route builder</param>
    /// <param name="assembly">The assembly to scan for IEndpoint implementations</param>
    /// <returns>The endpoint route builder for chaining</returns>
    public static IEndpointRouteBuilder MapEndpointsFromAssembly(
        this IEndpointRouteBuilder builder, 
        Assembly assembly)
    {
        var endpointType = typeof(IEndpoint);
        var endpointTypes = assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && endpointType.IsAssignableFrom(t))
            .ToList();

        foreach (var type in endpointTypes)
        {
            var instance = Activator.CreateInstance(type) as IEndpoint;
            if (instance != null)
            {
                instance.MapEndpoints(builder);
            }
        }

        return builder;
    }

    /// <summary>
    /// Scans the calling assembly for classes implementing IEndpoint and maps their endpoints.
    /// </summary>
    /// <param name="builder">The endpoint route builder</param>
    /// <returns>The endpoint route builder for chaining</returns>
    public static IEndpointRouteBuilder MapEndpointsFromCallingAssembly(
        this IEndpointRouteBuilder builder)
    {
        var callingAssembly = Assembly.GetCallingAssembly();
        return builder.MapEndpointsFromAssembly(callingAssembly);
    }
}

