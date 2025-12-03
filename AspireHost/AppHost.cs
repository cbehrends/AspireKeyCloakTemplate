var builder = DistributedApplication.CreateBuilder(args);

builder.AddProject<Projects.DotNetCleanTemplate_API>("dotnetcleantemplate-api");

builder.Build().Run();
