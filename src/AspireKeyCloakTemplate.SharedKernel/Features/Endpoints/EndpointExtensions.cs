using System.Reflection;
using Microsoft.AspNetCore.Routing;

namespace AspireKeyCloakTemplate.SharedKernel.Features.Endpoints;

public static class EndpointExtensions
{
    /// <param name="builder">The endpoint route builder</param>
    extension(IEndpointRouteBuilder builder)
    {
        /// <summary>
        ///     Scans the specified assembly for classes implementing IEndpoint and maps their endpoints.
        /// </summary>
        /// <param name="assembly">The assembly to scan for IEndpoint implementations</param>
        /// <returns>The endpoint route builder for chaining</returns>
        public IEndpointRouteBuilder MapEndpointsFromAssembly(Assembly assembly)
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
        /// <returns>The endpoint route builder for chaining</returns>
        public IEndpointRouteBuilder MapEndpointsFromCallingAssembly()
        {
            var callingAssembly = Assembly.GetCallingAssembly();
            return builder.MapEndpointsFromAssembly(callingAssembly);
        }
    }
}
