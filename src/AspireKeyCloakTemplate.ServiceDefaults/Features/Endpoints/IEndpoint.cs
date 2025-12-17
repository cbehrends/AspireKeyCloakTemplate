using Microsoft.AspNetCore.Routing;

namespace AspireKeyCloakTemplate.ServiceDefaults.Features.Endpoints;

public interface IEndpoint
{
    IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder builder);
}
