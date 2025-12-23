using Projects;

var builder = DistributedApplication.CreateBuilder(args);

var seq = builder.AddSeq("seq")
    .ExcludeFromManifest()
    .WithLifetime(ContainerLifetime.Persistent)
    .WithEnvironment("ACCEPT_EULA", "Y");

var username = builder.AddParameter("username", "admin");
var password = builder.AddParameter("password", "password", secret: true);

// Determine realms.json path: use env var if set, else default to /cfg/keycloak/realms.json at repo root
var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../.."));
var defaultRealmsJsonPath = Path.Combine(repoRoot, "cfg", "keycloak", "realms.json");
var realmsJsonPath = Environment.GetEnvironmentVariable("REALMS_JSON_PATH") ?? defaultRealmsJsonPath;

var keyCloak = builder
    .AddKeycloak("keycloak", 8080, username, password)
    .WithReference(seq)
    .WaitFor(seq)
    .WithLifetime(ContainerLifetime.Session)
    .WithDataVolume()
    .WithBindMount(realmsJsonPath,
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
    .WithReference(seq)
    .WaitFor(seq)
    .WithReference(keyCloak)
    .WaitFor(keyCloak);

var reactApp = builder
    .AddViteApp("react-app", "../react-app")
    .WithPnpmPackageInstallation()
    .WithReference(seq)
    .WaitFor(seq)
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
    .WithReference(seq)
    .WaitFor(seq)
    .WithReference(keyCloak)
    .WithReference(reactApp)
    .WithReference(api);

await builder.Build().RunAsync();
