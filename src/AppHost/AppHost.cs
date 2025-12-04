var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("admin");
var password = builder.AddParameter("password", secret: true);

var keyCloak = builder
    .AddKeycloak("keycloak", 8080, username, password)
    .WithDataVolume();

var api = builder
    .AddProject<Projects.DotNetCleanTemplate_API>("api")
    .WithExternalHttpEndpoints()
    .WithReference(keyCloak)
    .WaitFor(keyCloak)
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

var gateway = builder
    .AddProject<Projects.DotNetCleanTemplate_Gateway>("gateway")
    .WithExternalHttpEndpoints()
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

var reactApp = builder
    .AddViteApp("react-app", "../react-app")
    .WithNpmPackageInstallation()
    .WithEnvironment("PORT", "3000")
    .WaitFor(keyCloak)
    .WaitFor(api)
    .WaitFor(gateway);


builder.Build().Run();