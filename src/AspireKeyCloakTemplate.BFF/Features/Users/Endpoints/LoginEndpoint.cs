using AspireKeyCloakTemplate.BFF.Features.Core;
using AspireKeyCloakTemplate.ServiceDefaults.Features.Endpoints;
using Microsoft.AspNetCore.Authentication;

namespace AspireKeyCloakTemplate.BFF.Features.Users.Endpoints;

internal class LoginEndpoint : IEndpoint
{
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder builder)
    {
        builder.MapGet("/login", (string? returnUrl, string? claimsChallenge, HttpContext context) =>
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = context.BuildRedirectUrl(returnUrl)
            };

            if (claimsChallenge == null) return TypedResults.Challenge(properties);
            var jsonString = claimsChallenge.Replace("\\", "", StringComparison.Ordinal).Trim(['"']);
            properties.Items["claims"] = jsonString;

            return TypedResults.Challenge(properties);
        }).AllowAnonymous();

        return builder;
    }
}

