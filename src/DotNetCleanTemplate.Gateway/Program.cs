
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();
builder.Services.AddAuthentication(options =>
    {
        // Use cookies to store the local auth session and OIDC for challenges
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie()
    .AddKeycloakOpenIdConnect(
        serviceName: "keycloak",
        realm: "master",
        options =>
        {
            options.ClientId = "gateway-client";
            options.ClientSecret = "gateway-secret";
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.UsePkce = true;
            options.Authority = "http://keycloak:8080/realms/master";
            options.RequireHttpsMetadata = false;

        });
builder.Services.AddAuthorization();

// Add YARP reverse proxy
// Append a bearer token (from configuration) to outbound requests for the "api" route/cluster
var proxyBearerToken = builder.Configuration.GetValue<string>("ProxyAuth:BearerToken");

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddTransforms(builderContext =>
    {
        builderContext.AddRequestHeader("Authorization", $"Bearer {proxyBearerToken}", append: false);
    });

var app = builder.Build();

app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["Content-Security-Policy"] = "frame-ancestors 'self' http://localhost:8080";
        return Task.CompletedTask;
    });
    await next();
});

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Map the reverse proxy
app.MapReverseProxy();

// Serve the React app SPA
app.MapFallbackToFile("index.html");

app.Run();
