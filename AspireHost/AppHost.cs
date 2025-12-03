var builder = DistributedApplication.CreateBuilder(args);

var api = builder
    .AddProject<Projects.DotNetCleanTemplate_API>("dotnetcleantemplate-api");

builder.AddProject<Projects.DotNetCleanTemplate_Gateway>("dotnetcleantemplate-gateway")
    .WithReference(api);

builder.Build().Run();
