## Plan: Migrate Gateway to Aspire YARP Integration

**TL;DR:** Replace manual YARP configuration with Aspire's integrated YARP component builder, which will streamline proxy configuration, eliminate the appsettings.json ReverseProxy section, and better leverage Aspire's service discovery. Custom transformers for antiforgery, bearer tokens, and rate limiting remain unchanged, but registration moves to Aspire's builder pattern. AppHost.cs will define the gateway + routes in one place rather than split between AppHost and appsettings.json.

### Steps

1. **Update [Gateway.csproj](file:///Users/coreybehrends/Projects/DotNetCleanTemplate/src/DotNetCleanTemplate.Gateway/DotNetCleanTemplate.Gateway.csproj) dependencies** – Add `Aspire.YARP.ReverseProxy` and remove manual `Microsoft.Extensions.ServiceDiscovery.Yarp` if redundant.

2. **Refactor [Extensions.cs](file:///Users/coreybehrends/Projects/DotNetCleanTemplate/src/DotNetCleanTemplate.Gateway/Features/Core/Extensions.cs) AddReverseProxy() method** – Convert `LoadFromConfig()` to use Aspire's builder pattern (likely builder methods like `AddReverseProxy()` or route configuration via code), and integrate service discovery at registration.

3. **Simplify [Program.cs](file:///Users/coreybehrends/Projects/DotNetCleanTemplate/src/DotNetCleanTemplate.Gateway/Program.cs)** – Remove or rename `AddReverseProxy()` call if Aspire provides a built-in extension; ensure transformers still register correctly.

4. **Update [appsettings.json](file:///Users/coreybehrends/Projects/DotNetCleanTemplate/src/DotNetCleanTemplate.Gateway/appsettings.json)** – Remove ReverseProxy section entirely; move routes and clusters to AppHost configuration.

5. **Configure proxy in [AppHost.cs](file:///Users/coreybehrends/Projects/DotNetCleanTemplate/src/AppHost/AppHost.cs)** – Use Aspire's YARP builder to define gateway routes/clusters pointing to API and React references, replacing hardcoded localhost URLs and appsettings config.

6. **Validate custom transformers** – Confirm `AddBearerTokenToHeaders`, `AddAntiforgeryToken`, and `ValidateAntiforgeryToken` transforms still integrate with Aspire's YARP builder, adjusting registration if needed.

### Further Considerations

1. **Transformation integration approach** – Does Aspire's YARP builder expose the same `.AddTransforms()` method as raw YARP, or does it wrap the transform context differently? You may need to extend Aspire's builder or keep hybrid registration.

2. **Service references and discovery** – Aspire can automatically wire API and React service addresses; verify that your AppHost references are set up with `.WithReference()` calls so the gateway dynamically resolves destinations instead of hardcoded URLs.

3. **Testing impact** – Your integration tests ([DotnetCleanTemplate.Gateway.IntegrationTests](file:///Users/coreybehrends/Projects/DotNetCleanTemplate/DotnetCleanTemplate.Gateway.IntegrationTests)) and unit tests may need fixture updates if the transformer registration or proxy configuration API changes.

### Current State Summary

**Gateway Components:**
- `Program.cs` – Initializes authentication, rate limiting, antiforgery, and calls `AddReverseProxy()` extension
- `Extensions.cs` – Contains `AddReverseProxy()`, `AddAuthenticationSchemes()`, `AddRateLimiting()` builder extensions
- `appsettings.json` – Defines ReverseProxy routes and clusters with hardcoded addresses (localhost:3000 for React, localhost:5002 for API)
- `Features/Transformers/` – Three custom YARP transformers:
  - `AddBearerTokenToHeadersTransform` – Appends user's access token to outbound requests
  - `AddAntiforgeryTokenResponseTransform` – Injects XSRF token cookie into HTML responses
  - `ValidateAntiforgeryTokenRequestTransform` – Validates XSRF tokens on non-safe HTTP methods
- `Features/Users/Endpoints/` – BFF endpoints for user-specific operations (e.g., `/bff/user/login`)

**AppHost Setup:**
- Currently registers gateway with `AddProject()` but does not wire routes or configure YARP
- API is registered and can serve as a reference
- React app is commented out pending SPA integration

**Aspire YARP Integration Goals:**
- Move route/cluster definitions from `appsettings.json` into AppHost configuration code
- Use Aspire's service discovery to automatically resolve API and React addresses
- Maintain custom transformers and BFF patterns without rewrite
- Simplify Configuration and reduce split responsibility between files

