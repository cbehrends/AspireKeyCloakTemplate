using AspireKeyCloakTemplate.BFF.Features.Core;
using AspireKeyCloakTemplate.BFF.Features.Users.Endpoints;
using AspireKeyCloakTemplate.ServiceDefaults;
using Duende.AccessTokenManagement.OpenIdConnect;
using Microsoft.AspNetCore.Antiforgery;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddReverseProxy();
builder.AddAuthenticationSchemes();
builder.AddRateLimiting();

builder.Services.AddDistributedMemoryCache();
builder.Services.AddOpenIdConnectAccessTokenManagement();

builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-XSRF-TOKEN";
    options.Cookie.Name = "X-XSRF-TOKEN";
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseStatusCodePages();
app.UseExceptionHandler();
app.UseAntiforgery();

app.UseAuthentication();
app.UseRateLimiter();
app.UseAuthorization();

var bffGroup = app.MapGroup("bff");
bffGroup.MapUserEndpoints();

bffGroup.MapGet("/csrf", (IAntiforgery antiforgery, HttpContext context) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    return Results.Ok(new { token = tokens.RequestToken });
});

app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();
