using System.Security.Claims;
using AspireKeyCloakTemplate.BFF.Features.Users.Model;
using AspireKeyCloakTemplate.ServiceDefaults.Features.Endpoints;

namespace AspireKeyCloakTemplate.BFF.Features.Users.Endpoints;

internal class GetUserEndpoint : IEndpoint
{
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/user", (ClaimsPrincipal principal) =>
        {
            var user = principal switch
            {
                { Identity.IsAuthenticated: true } => new User
                {
                    IsAuthenticated = true,
                    Name = principal.FindFirstValue("name"),
                    Claims = principal.Claims.Select(c => new UserClaim { Type = c.Type, Value = c.Value })
                },
                _ => new User
                {
                    IsAuthenticated = false,
                    Name = null
                }
            };

            return TypedResults.Ok(user);
        });

        return builder;
    }
}

