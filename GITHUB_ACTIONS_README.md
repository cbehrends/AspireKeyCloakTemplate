# GitHub Actions Build Pipeline

This document describes the automated build, test, and packaging pipeline for the AspireKeyCloakTemplate project.

## Overview

The GitHub Actions workflow (`build.yml`) automates:

1. **Semantic Versioning** — Automatic version calculation using `GitVersion` based on branching strategy
2. **React App Build** — Compiles the Vite + React frontend and copies output to the Gateway's `wwwroot/`
3. **.NET Unit Tests** — Runs unit tests for the Gateway project
4. **Docker Image Builds** — Builds multi-stage Docker images for the API and Gateway services

## Workflow Triggers

The pipeline is triggered by:

- **Push to `main` branch** — Creates stable releases (e.g., `v1.2.3`)
- **Push to `develop` branch** — Creates pre-release versions (e.g., `v1.2.3-rc.1`)
- **Pull Requests** — Runs tests and builds without publishing

## Versioning Strategy

### GitVersion

The project uses `GitVersion` to automatically calculate versions based on the branching strategy:

- **main branch**: Stable releases (e.g., `1.2.3`) with no pre-release suffix
- **develop branch**: Pre-release versions with `-rc` suffix (e.g., `1.2.3-rc.1`)
- **feature branches**: Alpha versions with `-alpha` suffix (e.g., `1.2.3-alpha.1`)
- **pull requests**: PR versions with `-pr` suffix (e.g., `1.2.3-pr.1`)

GitVersion uses the commit history and branch structure to determine version increments automatically. The versioning is configured in `GitVersion.yml` at the repository root.

## GitVersion Configuration

The `GitVersion.yml` file at the repository root controls versioning behavior:

- **mode**: `ContinuousDelivery` for main branch, `ContinuousDeployment` for others
- **branches**: Defines version increment strategy per branch
  - `main`: Releases with patch increment
  - `develop`: Pre-release with minor increment and `-rc` tag
  - `feature`: Alpha versions with `-alpha` tag
  - `pull-request`: PR versions with `-pr` tag

### Version Calculation

GitVersion calculates version numbers by:
1. Finding the last tag in the repository
2. Analyzing commits since the last tag
3. Applying branch-specific rules to determine the next version
4. Adding pre-release tags based on branch configuration

### Local Testing

To test GitVersion locally:

```bash
# Install GitVersion (via dotnet tool)
dotnet tool install --global GitVersion.Tool

# Calculate version
dotnet-gitversion

# Display detailed info
dotnet-gitversion /showvariable FullSemVer
```

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

### 1. `gitversion`

- Uses GitVersion to calculate the version based on branch and commit history
- Outputs version information for use by subsequent jobs

**Runs on**: Ubuntu 22.04

**Outputs**:
- `semVer` — Semantic version (e.g., `1.2.3-rc.1`)
- `fullSemVer` — Full semantic version with metadata (e.g., `1.2.3-rc.1+5`)
- `majorMinorPatch` — Base version without pre-release or metadata (e.g., `1.2.3`)

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

- Depends on: `gitversion`, `build-react`, `test-dotnet` (ensures they succeed before building)
- Downloads React build artifact
- Uses version from GitVersion job output
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

### GitVersion doesn't calculate expected version

- Check `GitVersion.yml` configuration matches your branch names
- Ensure repository has at least one tag (GitVersion needs a starting point)
- Run `dotnet-gitversion` locally to debug version calculation
- Verify `fetch-depth: 0` is set in checkout action (needed for full git history)

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

- [GitVersion Documentation](https://gitversion.net/)
- [GitVersion Configuration](https://gitversion.net/docs/reference/configuration)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)
- [Docker Best Practices](https://docs.docker.com/develop/dev-best-practices/)

