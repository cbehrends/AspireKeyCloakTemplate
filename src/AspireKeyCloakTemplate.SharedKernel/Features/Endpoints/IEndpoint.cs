using Microsoft.AspNetCore.Routing;

namespace AspireKeyCloakTemplate.SharedKernel.Features.Endpoints;

public interface IEndpoint
{
    IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder builder);
}
