# GitHub Actions Build Pipeline

This document describes the automated build, test, and packaging pipeline for the AspireKeyCloakTemplate project.

## Overview

The GitHub Actions workflow (`build.yml`) automates:

1. **Semantic Versioning** — Automatic version bumping based on conventional commits using `semantic-release`
2. **React App Build** — Compiles the Vite + React frontend and copies output to the Gateway's `wwwroot/`
3. **.NET Unit Tests** — Runs unit tests for the Gateway project
4. **Docker Image Builds** — Builds multi-stage Docker images for the API and Gateway services

## Workflow Triggers

The pipeline is triggered by:

- **Push to `main` branch** — Creates stable releases (e.g., `v1.2.3`)
- **Push to `develop` branch** — Creates pre-release versions (e.g., `v1.2.3-rc.1`)
- **Pull Requests** — Runs tests and builds without publishing

## Versioning Strategy

### Semantic Release

The project uses `semantic-release` to automatically determine the next version based on conventional commits:

- **Patch bump** (e.g., `1.0.0` → `1.0.1`): `fix:` commits
- **Minor bump** (e.g., `1.0.0` → `1.1.0`): `feat:` commits
- **Major bump** (e.g., `1.0.0` → `2.0.0`): `BREAKING CHANGE:` in commit body

### Branch-Based Versioning

- **main branch**: Stable releases tagged as `v1.2.3` (no pre-release suffix)
- **develop branch**: Pre-release versions tagged as `v1.2.3-rc.1`, `v1.2.3-rc.2`, etc.
- **Other branches**: Ad-hoc versions using commit SHA (e.g., `0.0.0-rc.123-abc1234def`)

## Conventional Commits

All commits must follow the [Conventional Commits](https://www.conventionalcommits.org/) specification:

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

### Examples

```bash
# Feature
git commit -m "feat: add user authentication endpoint"

# Bug fix
git commit -m "fix: correct token expiration validation"

# Documentation
git commit -m "docs: update API deployment guide"

# Breaking change
git commit -m "feat: redesign auth middleware

BREAKING CHANGE: auth middleware now requires OIDC provider config"

# Chore / CI
git commit -m "chore(deps): update Keycloak package"
git commit -m "ci: add Docker build caching"
```

### Enforcing Conventional Commits (Optional Local Setup)

To enforce conventional commits locally:

1. **Install Husky and commitlint**:
   ```bash
   npm install --save-dev husky @commitlint/config-conventional @commitlint/cli
   npx husky install
   npx husky add .husky/commit-msg 'npx --no -- commitlint --edit "$1"'
   ```

2. **The `commitlint.config.js` file is already configured** in the repo root.

## Docker Images

### Image Naming

Images are built with the following tags:

```
api:v1.2.3
api:v1.2.3-rc.1
api:latest

gateway:v1.2.3
gateway:v1.2.3-rc.1
gateway:latest
```

### Multi-Stage Builds

Both `Dockerfile.api` and `Dockerfile.gateway` use multi-stage builds:

1. **Builder stage** — Uses `mcr.microsoft.com/dotnet/sdk:10.0` to compile and publish
2. **Runtime stage** — Uses `mcr.microsoft.com/dotnet/aspnet:10.0` (leaner, production-ready)

### Gateway Special Handling

The `Dockerfile.gateway` ensures the React app build output is included:

1. The workflow builds the React app with `npm run build`
2. Output (`src/react-app/dist/*`) is copied to `src/AspireKeyCloakTemplate.Gateway/wwwroot/`
3. The Gateway Dockerfile copies published app files, including the populated `wwwroot/` directory

## Artifacts

The workflow generates the following artifacts (retained for 1–7 days):

- **react-build** (1 day) — Compiled React app (also included in Gateway container)
- **dotnet-test-results** (1 day) — xUnit test result XML files
- **docker-images** (1 day) — Built Docker images (`.tar` format, for manual testing)
- **build-metadata** (7 days) — JSON file with build version, commit SHA, branch, etc.

## Workflow Jobs

### 1. `semantic-release` (main/develop pushes only)

- Analyzes commits and determines version bump
- Updates `CHANGELOG.md` and `package.json`
- Creates Git tag (e.g., `v1.2.3`)
- Outputs version to be used by subsequent jobs

**Permissions**: `contents:write`, `issues:write`, `pull-requests:write`

**Outputs**:
- `version` — Full semantic version (e.g., `v1.2.3`)
- `published` — Boolean indicating if a new version was published

### 2. `build-react`

- Installs Node.js dependencies
- Runs `npm run build` to compile the React/Vite app
- Copies output to Gateway's `wwwroot/` directory
- Uploads artifact for use by the Docker build job

**Runs on**: Ubuntu 22.04

### 3. `test-dotnet`

- Sets up .NET 10 SDK
- Restores NuGet packages
- Runs `dotnet test` for Gateway unit tests
- Uploads test results in TRX format

**Runs on**: Ubuntu 22.04

### 4. `build-docker`

- Depends on: `build-react`, `test-dotnet` (ensures they succeed before building)
- Downloads React build artifact
- Determines version tag (from git tag or commit SHA)
- Builds `api` and `gateway` Docker images using multi-stage Dockerfile
- Uses Docker build cache for faster builds
- Uploads Docker images and build metadata

**Runs on**: Ubuntu 22.04

## Environment & Secrets

### GitHub Secrets (to be configured)

For registry push integration (deferred, add when ready):

- `REGISTRY` — Container registry URL (e.g., `docker.io`, `ghcr.io`)
- `REGISTRY_USERNAME` — Registry username
- `REGISTRY_PASSWORD` — Registry password or token
- `GITHUB_TOKEN` — Auto-provided by GitHub Actions (used for semantic-release Git operations)

### Required GitHub Permissions

Add to repository settings:

- **Workflow permissions**: `Read and write permissions`
- **Workflow execution**: Allow `actions` to create and approve pull requests (for semantic-release)

## Local Testing

### Test React Build

```bash
cd src/react-app
npm install
npm run build
npm run serve  # Preview built app
```

### Test .NET Unit Tests

```bash
dotnet test src/AspireKeyCloakTemplate.Gateway.UnitTests/
```

### Test Docker Image Builds

```bash
docker build -f Dockerfile.api -t api:latest .
docker build -f Dockerfile.gateway -t gateway:latest .
```

## Troubleshooting

### semantic-release doesn't detect version bump

- Ensure commits follow [Conventional Commits](https://www.conventionalcommits.org/)
- Check `.releaserc.json` branch configuration matches your Git branch names
- Verify `GITHUB_TOKEN` has sufficient permissions (`contents:write`)

### React build fails

- Ensure `src/react-app/package-lock.json` (or `pnpm-lock.yaml`) is committed
- Check Node.js version in workflow vs. `package.json` engines field
- Verify Vite build output directory in `vite.config.ts` is `dist/`

### Docker build fails

- Ensure all `.csproj` files and `Directory.Packages.props` are valid XML
- Check multi-stage Dockerfile syntax (builder stage must complete successfully)
- Verify all project references are correctly specified in copy commands

### Gateway doesn't serve React files

- Confirm React build output is copied to `src/AspireKeyCloakTemplate.Gateway/wwwroot/`
- Verify Gateway's `Program.cs` includes `app.UseStaticFiles()`
- Check Gateway Dockerfile includes the populated `wwwroot/` in the final image

## Next Steps: Registry Integration

When ready to publish images:

1. Choose a container registry (Docker Hub, GitHub Container Registry, AWS ECR, etc.)
2. Create `push-to-registry.yml` workflow that:
   - Downloads Docker images and metadata artifacts
   - Logs into registry using secrets
   - Tags and pushes images with semantic version
   - (Optional) Updates Helm charts or deploy configs with new image tags

## Resources

- [Conventional Commits](https://www.conventionalcommits.org/)
- [semantic-release Documentation](https://semantic-release.gitbook.io/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)

