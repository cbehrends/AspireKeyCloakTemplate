using AspireKeyCloakTemplate.BFF.Features.Core;
using AspireKeyCloakTemplate.ServiceDefaults.Features.Endpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

namespace AspireKeyCloakTemplate.BFF.Features.Users.Endpoints;

internal class LogoutEndpoint : IEndpoint
{
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder builder)
    {
        builder.MapPost("/logout", (string? redirectUrl, HttpContext context) =>
        {
            var properties = new AuthenticationProperties
            {
                RedirectUri = context.BuildRedirectUrl(redirectUrl)
            };

            return TypedResults.SignOut(properties,
                [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]);
        });

        return builder;
    }
}

