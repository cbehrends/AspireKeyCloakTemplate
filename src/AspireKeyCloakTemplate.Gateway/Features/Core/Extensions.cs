using System.Security.Claims;
using System.Threading.RateLimiting;
using AspireKeyCloakTemplate.Gateway.Features.Transformers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Yarp.ReverseProxy.Transforms;

namespace AspireKeyCloakTemplate.Gateway.Features.Core;

internal static class Extensions
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddReverseProxy()
        {
            builder.Services.AddSingleton<AddBearerTokenToHeadersTransform>();
            builder.Services.AddSingleton<AddAntiforgeryTokenResponseTransform>();
            builder.Services.AddSingleton<ValidateAntiforgeryTokenRequestTransform>();

            builder.Services
                .AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
                .AddTransforms(builderContext =>
                {
                    builderContext.ResponseTransforms.Add(builderContext.Services
                        .GetRequiredService<AddAntiforgeryTokenResponseTransform>());
                    builderContext.RequestTransforms.Add(builderContext.Services
                        .GetRequiredService<ValidateAntiforgeryTokenRequestTransform>());
                    builderContext.RequestTransforms.Add(new RequestHeaderRemoveTransform("Cookie"));

                    if (!string.IsNullOrEmpty(builderContext.Route.AuthorizationPolicy))
                        builderContext.RequestTransforms.Add(builderContext.Services
                            .GetRequiredService<AddBearerTokenToHeadersTransform>());
                })
                .AddServiceDiscoveryDestinationResolver();

            return builder;
        }

        public IHostApplicationBuilder AddAuthenticationSchemes()
        {
            builder.Services.AddAuthentication(options =>
                {
                    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
                })
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
                {
                    options.Cookie.Name = "__AspireKeyCloakTemplate_TOKEN";
                    options.Cookie.SameSite = SameSiteMode.Strict;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                })
                .AddKeycloakOpenIdConnect(
                    "keycloak",
                    "sandbox",
                    options =>
                    {
                        options.ClientId = builder.Configuration.GetSection("ClientId").Value ?? "";
                        options.ClientSecret = builder.Configuration.GetSection("ClientSecret").Value ?? "";
                        options.ResponseType = OpenIdConnectResponseType.Code;
                        options.ResponseMode = OpenIdConnectResponseMode.Query;
                        options.GetClaimsFromUserInfoEndpoint = true;
                        options.SaveTokens = true;
                        options.MapInboundClaims = false;
                        options.CallbackPath = "/signin-oidc";

                        options.TokenValidationParameters = new TokenValidationParameters
                        {
                            NameClaimType = ClaimTypes.NameIdentifier,
                            RoleClaimType = ClaimTypes.Role
                        };

                        options.Scope.Clear();
                        options.Scope.Add("openid");
                        options.Scope.Add("profile");
                        options.Scope.Add("email");
                        options.Scope.Add("offline_access");

                        if (builder.Environment.IsDevelopment()) options.RequireHttpsMetadata = false;
                    });

            builder.Services.AddAuthorization(options =>
            {
                options.DefaultPolicy =
                    new AuthorizationPolicyBuilder(CookieAuthenticationDefaults.AuthenticationScheme)
                        .RequireAuthenticatedUser()
                        .Build();
            });

            return builder;
        }

        public IHostApplicationBuilder AddRateLimiting()
        {
            builder.Services.AddRateLimiter(options =>
            {
                options.AddPolicy("user-or-ip", httpContext =>
                {
                    var partitionKey = httpContext.User.Identity?.IsAuthenticated == true
                        ? httpContext.User.FindFirstValue("name") ?? "anonymous"
                        : httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey,
                        _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 100,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        });
                });

                options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            });

            return builder;
        }
    }

    extension(HttpContext context)
    {
        public string BuildRedirectUrl(string? redirectUrl)
        {
            if (string.IsNullOrEmpty(redirectUrl)) redirectUrl = "/";
            if (redirectUrl.StartsWith('/'))
                redirectUrl = context.Request.Scheme + "://" + context.Request.Host + context.Request.PathBase +
                              redirectUrl;
            return redirectUrl;
        }
    }
}
