## Plan: Initialize AspireKeyCloakTemplate for Copilot

Create a structured set of configuration and documentation files to help Copilot understand the project's architecture, conventions, coding standards, and best practices. Reuse existing docs (README, BFF.md) and establish comprehensive guidelines for naming conventions, error handling, input validation, and caching patterns.

### Steps
1. Create `.copilot-instructions.md` documenting architecture, project responsibilities, coding patterns, naming conventions, error handling, input validation, and caching strategies (reuse/reference `docs/BFF.md` and README content).
2. Add `CONTRIBUTING.md` with developer guidelines, project structure, contribution workflow, and links to Copilot instructions.
3. Create `.editorconfig` to enforce consistent C# and TypeScript code formatting across projects.
4. Create `NAMING_CONVENTIONS.md` documenting C# namespacing, class/method/variable naming patterns, and TypeScript/React conventions.
5. Create `ERROR_HANDLING.md` detailing exception handling patterns, logging strategy, validation error responses, and BFF-specific error scenarios.
6. Create `CACHING_STRATEGY.md` documenting distributed cache patterns (Redis), token caching, response caching in Gateway, and cache invalidation.
7. Update `README.md` with links to new documentation files and contributor guidelines.

