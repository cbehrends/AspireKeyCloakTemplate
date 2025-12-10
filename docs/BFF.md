# Gateway as BFF — Design & Implementation (overview)

This document explains how the YARP Gateway doubles as a Backend-for-Frontend (BFF) for the SPA in this solution, the rationale, and recommended implementation details.

Goals
- Keep long-lived/secrets out of the browser.
- Centralize authentication and token management at the edge.
- Provide SPA-friendly endpoints that aggregate/shape downstream data.
- Protect against CSRF and reduce CORS surface area.

High-level architecture
- Browser <-> Gateway (BFF) <-> AspireKeyCloakTemplate.API
- Gateway handles OIDC interactions with Keycloak and stores tokens/sessions server-side.
- SPA communicates with Gateway endpoints only (e.g., /bff/user, /api/* proxied through YARP), minimizing direct calls to backend services.

Authentication flow (recommended)
1. SPA initiates login by redirecting to Gateway login endpoint (e.g., /bff/login).
2. Gateway redirects to Keycloak (OIDC authorization code flow). When used as a confidential backend client, Gateway exchanges the authorization code for tokens server-side.
3. Gateway stores access/refresh tokens in a secure server-side session (cookie references the session). The browser receives a secure, HttpOnly cookie scoped to the Gateway.
4. SPA calls /bff/* or /api/* endpoints; Gateway attaches access tokens when proxying to downstream APIs.
5. Token refresh: Gateway uses refresh token to obtain new access tokens server-side. If refresh fails, Gateway redirects SPA to re-authenticate.

Session & cookie guidance
- Set session cookie flags: Secure, HttpOnly, SameSite=Lax (or Strict where appropriate).
- Cookie should contain only a session identifier or encrypted metadata — not raw tokens accessible to JS.
- Session storage options:
  - In-memory (dev only)
  - Distributed cache (Redis) for multi-instance deployments
  - Database-backed session store for long-lived sessions
- Store tokens encrypted at rest and limit lifetime. Rotate session keys using standard key management.

CSRF & anti-forgery
- Because the SPA is using a cookie-based session, enable anti-forgery protections for state-changing endpoints.
- Techniques:
  - Use SameSite cookies (Lax/Strict) to prevent cross-site POSTs.
  - Expose a /bff/antiforgery-token endpoint that returns an anti-forgery token to the SPA (delivered in JSON), then require that token in a custom header for unsafe requests.
  - Validate Origin/Referer on sensitive endpoints.

YARP routing & BFF endpoints
- Keep YARP routes for backend APIs (e.g., /api/* -> AspireKeyCloakTemplate.API).
- Implement BFF endpoints inside the Gateway project (e.g., /bff/login, /bff/logout, /bff/user, /bff/proxy or aggregate endpoints).
- Example responsibilities for /bff/* endpoints:
  - /bff/login — start OIDC login
  - /bff/callback — OIDC callback (server-side code exchange)
  - /bff/logout — clear server session and optionally trigger Keycloak logout
  - /bff/user — return minimal user info (claims) to SPA
  - /bff/aggregate/widget-data — call several downstream APIs, compose responses, return single payload to SPA

Token use & downstream calls
- Gateway uses the server-side access token to call APIs on behalf of the user.
- Prefer authorization header forwarding (Bearer <access_token>) when proxying to internal APIs.
- Optionally strip/transform response content for SPA-specific needs.

Aggregation & composition
- BFF endpoints can consolidate multiple API calls, reduce chattiness, and tailor payloads to UI needs.
- Keep business rules and heavy composition at the BFF only when it’s UI-specific; otherwise prefer the API.

Scaling & resiliency
- Use distributed session storage (Redis) if running multiple Gateway instances.
- Enforce sensible access token caching and refresh backoff to minimize token endpoint load.
- Monitor 401/403 rates to detect token expiry or config issues.

Security considerations
- Use TLS everywhere.
- Protect client credentials and limit OAuth client scopes.
- Implement rate limiting on Gateway to protect downstream services.
- Validate and whitelist redirect URIs with Keycloak.

Implementation hints & libraries
- In .NET Gateway:
  - Use OpenIdConnect (Microsoft.AspNetCore.Authentication.OpenIdConnect) + Cookie authentication for server-side flows.
  - Configure Cookie authentication to be HttpOnly and Secure.
  - Use IDistributedCache for session/token storage if scaling.
  - Expose minimal JSON endpoints for SPA to read user metadata (/bff/user).
  - Use YARP to proxy API routes; add a delegating handler/middleware to inject access token when forwarding.
- For Keycloak:
  - Create a confidential client for the Gateway (client secret stored in Gateway config/secret store).
  - Configure allowed redirect URIs to the Gateway callback.

Examples (conceptual)
- YARP route: /api/{**catch-all} -> http://aspire-api:5000
- BFF endpoint: /bff/user -> Gateway reads session and returns { name, email, roles }
- Proxy behavior: Gateway adds Authorization: Bearer <access_token> when forwarding to /api/*

Operational tips
- Provide health endpoints on Gateway and API.
- Log token refresh failures and surface metrics for session counts.
- Add a test Keycloak realm/config used by integration tests and document it in a future developer-ops doc.

Next steps (practical)
- Add a small middleware in Gateway to centralize token attach logic for YARP (delegating handler).
- Implement /bff endpoints and a session provider (start with in-memory, swap to Redis later).
- Add tests: unit tests for BFF endpoints and Playwright tests for auth flows.

This doc is a concise reference; add code samples and configuration snippets in the repository when you want runnable examples.

