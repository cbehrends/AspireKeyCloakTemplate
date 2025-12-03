var builder = DistributedApplication.CreateBuilder(args);

var api = builder
    .AddProject<Projects.DotNetCleanTemplate_API>("api")
    .WithExternalHttpEndpoints()
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

var gateway = builder
    .AddProject<Projects.DotNetCleanTemplate_Gateway>("gateway")
    .WithExternalHttpEndpoints()
    .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development");

var reactApp = builder
    .AddViteApp("react-app", "../react-app")
    .WithNpmPackageInstallation()
    .WithEnvironment("PORT", "3000")
    .WaitFor(gateway);


builder.Build().Run();