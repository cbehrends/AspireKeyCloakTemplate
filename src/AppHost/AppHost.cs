using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var username = builder.AddParameter("username", "admin");
var password = builder.AddParameter("password", "password", secret: true);

var keyCloak = builder
    .AddKeycloak("keycloak", 8080, username, password)
    .WithLifetime(ContainerLifetime.Session)
    .WithDataVolume()
    .WithBindMount(Path.Combine(AppContext.BaseDirectory, "realms.json"),
        "/opt/keycloak/data/import/realms.json", true)
    .WithEnvironment("KC_DB_URL_PROPERTIES", "?ssl=false")
    .WithEnvironment("KEYCLOAK_IMPORT", "/opt/keycloak/data/import/realms.json")
    .WithEnvironment("KC_HTTP_ENABLED", "true")
    .WithEnvironment("KEYCLOAK_IMPORT_REALM_VALIDATE_SIGNATURE_ALGORITHM", "true")
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_USERNAME", username)
    .WithEnvironment("KC_BOOTSTRAP_ADMIN_PASSWORD", password);

var api = builder
    .AddProject<AspireKeyCloakTemplate_API>("api", "https")
    .WithExternalHttpEndpoints()
    .WithReference(keyCloak)
    .WaitFor(keyCloak);

var reactApp = builder
    .AddViteApp("react-app", "../react-app")
    .WithPnpmPackageInstallation()
    .WithEndpoint("http", endpoint =>
    {
        // This sets the *exposed* port Aspire uses to communicate with the app
        endpoint.Port = 3000;
    })
    // Also, tell the underlying Vite process to listen on this port
    .WithEnvironment("PORT", "3000");

builder
    .AddProject<AspireKeyCloakTemplate_BFF>("bff", "https")
    .WithExternalHttpEndpoints()
    .WithReference(keyCloak)
    .WithReference(reactApp)
    .WithReference(api);

await builder.Build().RunAsync();
