var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("admin");
var password = builder.AddParameter("password", secret: true);

var keyCloak = builder
    .AddKeycloak("keycloak", 8080, username, password)
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume()
    .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "realms.json"), "/opt/keycloak/data/import/realms.json", isReadOnly: true)
    .WithEnvironment("KC_DB_URL_PROPERTIES", "?ssl=false")
    .WithEnvironment("KEYCLOAK_IMPORT", "/opt/keycloak/data/import/realms.json")
    .WithEnvironment("KC_HTTP_ENABLED", "true")
    .WithEnvironment("KEYCLOAK_IMPORT_REALM_VALIDATE_SIGNATURE_ALGORITHM", "true")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", username)
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", password);

var api = builder
    .AddProject<Projects.DotNetCleanTemplate_API>("api", launchProfileName:"https")
    .WithExternalHttpEndpoints()
    .WithReference(keyCloak)
    .WaitFor(keyCloak);

var reactApp = builder
    .AddViteApp("react-app", "../react-app")
    .WithNpmPackageInstallation()
    .WithEndpoint(endpointName: "http", endpoint =>
    {
        // This sets the *exposed* port Aspire uses to communicate with the app
        endpoint.Port = 3000; 
    })
    // Also, tell the underlying Vite process to listen on this port
    .WithEnvironment("PORT", "3000")
    // Provide Keycloak configuration to the Vite app via env vars
    .WithEnvironment("VITE_KEYCLOAK_URL", "http://localhost:8080")
    .WithEnvironment("VITE_KEYCLOAK_REALM", "development")
    .WithEnvironment("VITE_KEYCLOAK_CLIENT_ID", "react-app");

builder
    .AddProject<Projects.DotNetCleanTemplate_Gateway>("gateway", launchProfileName: "https")
    .WithExternalHttpEndpoints()
    .WithReference(keyCloak)
    .WithReference(reactApp)
    .WithReference(api);

builder.Build().Run();