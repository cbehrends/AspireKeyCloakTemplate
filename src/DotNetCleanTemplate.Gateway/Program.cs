using DotNetCleanTemplate.Gateway.Features.Core;
using DotNetCleanTemplate.Gateway.Features.Users.Endpoints;
using DotNetCleanTemplate.ServiceDefaults;
using Duende.AccessTokenManagement.OpenIdConnect;

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

app.MapGroup("bff")
    .MapUserEndpoints();

app.MapReverseProxy();
app.MapDefaultEndpoints();

app.Run();