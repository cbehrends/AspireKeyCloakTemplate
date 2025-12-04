
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Yarp.ReverseProxy.Transforms;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddKeycloakOpenIdConnect(
        serviceName: "keycloak",
        realm: "api",
        options =>
        {
            options.ClientId = "StoreWeb";
            options.ResponseType = OpenIdConnectResponseType.Code;
            options.Scope.Add("store:all");
        });

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

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// Map the reverse proxy
app.MapReverseProxy();

// Serve the React app SPA
app.MapFallbackToFile("index.html");

app.Run();
