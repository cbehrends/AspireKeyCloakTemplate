using AspireKeyCloakTemplate.BFF.Features.Users.Queries.GetCurrentUser;
using AspireKeyCloakTemplate.SharedKernel.Features.Endpoints;
using AspireKeyCloakTemplate.SharedKernel.Features.Mediator;

namespace AspireKeyCloakTemplate.BFF.Features.Users.Endpoints;

internal class GetUserEndpoint : IEndpoint
{
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/user", async (IMediator mediator) =>
        {
            var user = await mediator.Send(new GetCurrentUserQuery());
            return TypedResults.Ok(user);
        });

        return builder;
    }
}
