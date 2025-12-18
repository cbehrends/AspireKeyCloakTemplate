# AspireKeyCloakTemplate

Opinionated, end-to-end .NET + React template integrating:

- Aspire libraries and patterns
- Keycloak for authentication (OIDC)
- YARP as a gateway / reverse-proxy
- A Vite + React frontend
  This template is designed to be extended with other frontends (Vue, Angular, etc.) in the future.

What this repository provides

- Multiple .NET projects (API, Gateway, AppHost, ServiceDefaults)
- Unit and integration test projects (xUnit + Playwright for E2E)
- A Vite + React frontend at `src/react-app`
- Conventions for adding additional frontends under `src/frontends/*`

Note: This README focuses on content and project responsibilities. Detailed build & run instructions will be
added/expanded later.

---

## Prerequisites (brief)

- .NET 10 SDK (dotnet) — projects target `net10.0`.
- Node.js >= 20.19.0 — required by the React app per `src/react-app/package.json`.
- npm (or other Node package manager).
  Optional:
- Playwright (used by integration tests) — integration tests reference `Microsoft.Playwright`.

---

## Solution layout and project responsibilities

This section explains each project and its intended role so you can reason about where functionality belongs.

- src/AspireKeyCloakTemplate.API — API
    - Purpose: The backend REST API that implements application functionality and exposes endpoints consumed by
      frontends and other services.
    - Responsibilities: Controllers, DTOs, domain/service layer, business rules, authorization policies, and JWT
      validation for Keycloak-issued tokens.
    - Should be agnostic to Gateway concerns; rely on shared ServiceDefaults for common DI and configuration.

- src/AspireKeyCloakTemplate.Gateway — Gateway (YARP)
    - Purpose: Edge reverse-proxy using YARP that routes client traffic to downstream services (the API) and handles
      auth concerns at the edge.
    - Responsibilities: Route configuration, token validation / OIDC middleware integration with Keycloak, response
      shaping, rate-limiting and any transformation/aggregation that belongs to the network boundary.
    - Also acts as a BFF (Backend-for-Frontend) for the SPA: handle SPA-specific authentication flows and token
      exchange, store/refresh tokens or session data securely on the server side (so the browser does not hold
      long-lived secrets), aggregate and compose multiple downstream calls into single responses for the frontend, and
      provide protections for cookies/CSRF when needed.
    - Contains glue code to forward authenticated requests to the API, and to expose public assets for SPAs where
      appropriate.
    - For implementation details on how the Gateway functions as the SPA BFF (auth flows, token handling, session
      storage, sample routes and middleware), see: docs/BFF.md

- src/AppHost — Composite host
    - Purpose: A host project that can compose and run multiple pieces together (for local dev, integration scenarios,
      or packaging).
    - Responsibilities: Build-time/launch-time composition of API and Gateway projects; convenience host configuration
      for easier local runs or single-process debugging runs.

- src/AspireKeyCloakTemplate.SharedKernel — Shared defaults & extensions
    - Purpose: Shared DI extensions, common configuration keys, logging / telemetry defaults, and helper types used
      across API and Gateway.
    - Responsibilities: Centralize common setup so each project can remain thin and consistent (e.g., AddAspireDefaults
      extension methods, common settings classes).

- AspireKeyCloakTemplate.Gateway.UnitTests — Unit tests
    - Purpose: Fast-running unit tests for Gateway (and related libraries) using xUnit and test doubles.
    - Responsibilities: Validate routing logic, configuration binding, small behaviors in middleware and helpers.

- AspireKeyCloakTemplate.Gateway.IntegrationTests — Integration / E2E tests
    - Purpose: End-to-end integration tests that exercise the Gateway + API + frontend interactions.
    - Responsibilities: Use test host processes and Playwright (for browser-based flows) to validate authentication
      flows (Keycloak OIDC), routing, and end-to-end scenarios.

- src/react-app — Frontend (Vite + React)
    - Purpose: Single-page application that authenticates via Keycloak (OIDC) and interacts with the Gateway.
    - Responsibilities: OIDC client setup, token management (acquire/refresh), API calls through the Gateway, and UI
      components for the sample app.
    - Scripts: `dev`, `build`, `serve`, `test` (see package.json).

Frontends & future frameworks

- Conventions: Place frontends under `src/frontends/<framework>-app` or keep the current `src/react-app`. Example future
  locations:
    - `src/frontends/react-app` (or existing `src/react-app`)
    - `src/frontends/vue-app`
    - `src/frontends/angular-app`
- The goal is to provide a common Gateway + API surface so multiple frontends can be mounted and tested against the same
  backend stack.

---

## Building (minimal)

From the repository root:

```bash
dotnet build
```

This restores NuGet packages and compiles projects targeting `net10.0`.

(We will add more detailed per-project build/run steps later.)

---

## Tests

Run all tests:

```bash
dotnet test
```

For Playwright-based integration tests, you may need to install browsers:

```bash
npx playwright install
```

---

## React frontend (src/react-app)

The React app is a Vite project. Quick start:

```bash
cd src/react-app
npm install
npm run dev
# Open http://localhost:3000
```

Common scripts:

- `npm run dev` — start Vite dev server
- `npm run build` — production build
- `npm run serve` — preview build
- `npm run test` — run unit tests (vitest)

---

## Notes and troubleshooting

- Keycloak: Provide Keycloak server configuration and client credentials when exercising auth-protected features.
- SDK versions: If builds fail due to SDK mismatch, ensure .NET 10 is installed and check any `global.json`.
- Playwright: If integration tests fail due to missing browsers, run `npx playwright install`.

---


---

## Next steps (ideas)

- Expand the README with per-project run instructions and example local Keycloak docker-compose configuration.
- Add a VS Code launch configuration or Rider run profiles for AppHost and Gateway.
- Add CI workflows (GitHub Actions) to build, test, and publish demo artifacts.
- Add example Vue/Angular frontends under `src/frontends/*`.

Contributions welcome — open issues or PRs. Keep changes small and include tests for behavioral changes.
