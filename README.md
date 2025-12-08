# DotNetCleanTemplate

Opinionated .NET + React template repository containing:

- Multiple .NET projects (API, Gateway, AppHost, ServiceDefaults)
- Unit and integration test projects
- A Vite + React frontend at `src/react-app`

This README explains how to build, test, and run the projects locally.

---

## Prerequisites

- .NET 10 SDK (dotnet) — projects target `net10.0`.
- Node.js >= 20.19.0 (required by the React app according to `src/react-app/package.json`).
- npm or your preferred package manager for the React app.

Optional tools:
- Playwright (used by integration tests) — the integration tests reference `Microsoft.Playwright`.

---

## Solution layout

- `src/DotNetCleanTemplate.API` — ASP.NET API project
- `src/DotNetCleanTemplate.Gateway` — Gateway (YARP reverse-proxy + auth)
- `src/AppHost` — Host project that composes API and Gateway
- `src/DotNetCleanTemplate.ServiceDefaults` — shared defaults and extensions
- `DotNetCleanTemplate.Gateway.UnitTests` — unit tests (xUnit)
- `DotnetCleanTemplate.Gateway.IntegrationTests` — integration tests (xUnit + Playwright)
- `src/react-app` — Vite + React frontend

---

## Building the solution

From the repository root you can build the entire solution using the .NET CLI:

```bash
dotnet build
```

This will restore NuGet packages and compile all projects targeting `net10.0`.

---

## Running the API / Gateway locally

To run individual projects use `dotnet run` from the project folder. Example – run the Gateway:

```bash
cd src/DotNetCleanTemplate.Gateway
dotnet run
```

To run the AppHost (which composes projects):

```bash
cd src/AppHost
dotnet run
```

The projects use environment-based configuration files (`appsettings.json`, `appsettings.Development.json`) and may expect additional services (Keycloak, downstream APIs) depending on what features you exercise.

---

## Tests

Run all tests in the repo with:

```bash
dotnet test
```

To run tests for a single test project:

```bash
dotnet test DotNetCleanTemplate.Gateway.UnitTests/DotNetCleanTemplate.Gateway.UnitTests.csproj
```

Integration tests reference Playwright; you may need to install Playwright browsers before running them. If you see Playwright-related errors, install browsers with:

```bash
npx playwright install
```

---

## React frontend (src/react-app)

The React app is a Vite project. Helpful scripts from `src/react-app/package.json`:

- `npm run dev` — start the dev server on port 3000 (Vite)
- `npm run build` — create a production build
- `npm run serve` — preview the production build
- `npm run test` — run unit tests (vitest)

Quick start for the frontend:

```bash
cd src/react-app
npm install
npm run dev
# Open http://localhost:3000
```

Note: package.json specifies Node >= 20.19.0.

---

## Notes and troubleshooting

- The solution uses several Aspire packages and Keycloak integrations. Provide appropriate Keycloak/secret configuration when running auth-protected pieces.
- If building fails due to SDK versions, ensure you have .NET 10 installed and your global.json (if any) matches the SDK.
- If tests report missing browsers for Playwright, run `npx playwright install` from the repository or the test project's directory.

---

## Contributing

Feel free to open issues or PRs. Keep changes small and add tests for behavioral changes.

---

If you want, I can also:
- Add a small `dotnet` launch configuration for VS Code or Rider
- Add a `README` specifically inside `src/react-app` describing the frontend in more detail
- Add CI workflow examples (GitHub Actions) to build and test on push


