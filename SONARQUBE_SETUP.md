# SonarQube Cloud Setup Guide

This guide provides instructions for setting up SonarQube Cloud code coverage analysis for the AspireKeyCloakTemplate project.

## Overview

This project integrates with SonarQube Cloud to automatically analyze code quality and measure code coverage using:
- **xUnit** for unit testing
- **Coverlet** for code coverage collection (OpenCover format)
- **GitHub Actions** for CI/CD automation
- **SonarQube Cloud** for code quality analysis

## Prerequisites

- GitHub repository access
- SonarQube Cloud account
- GitHub organization or personal account
- Basic understanding of GitHub Secrets

## Initial Setup (One-time)

### 1. Create SonarQube Cloud Project

1. Go to [SonarQube Cloud](https://sonarcloud.io)
2. Sign up or sign in with your GitHub account
3. Create a new organization (or use existing one)
4. Click "Analyze new project"
5. Select your GitHub repository
6. Note down:
   - **Organization Key** (e.g., `your-org-key`)
   - **Project Key** (e.g., `AspireKeyCloakTemplate`)

### 2. Generate SonarQube Token

1. In SonarQube Cloud, go to **Account Settings** > **Security**
2. Generate a new token with a descriptive name (e.g., "GitHub Actions")
3. Copy the token immediately (you won't be able to see it again)

### 3. Configure GitHub Secrets

In your GitHub repository settings:

1. Go to **Settings** > **Secrets and variables** > **Actions**
2. Add the following secrets:
   - **`SONAR_TOKEN`**: Your SonarQube Cloud token (from step 2)
   - **`SONAR_ORGANIZATION`**: Your organization key
   - **`SONAR_PROJECT_KEY`**: Your project key
   - **`SONAR_HOST_URL`**: `https://sonarcloud.io` (default for SonarQube Cloud)

### 4. Configure Code Coverage Collection

The project includes:
- **`.runsettings`** file in `src/AspireKeyCloakTemplate.Gateway.UnitTests/` - Configures coverlet to generate OpenCover format reports
- **`sonar-project.properties`** - Configures SonarQube analysis parameters

## Running Tests Locally with Coverage

To generate code coverage reports locally and review them:

### Build and Test with Coverage

```bash
cd /Users/coreybehrends/Projects/DotNetCleanTemplate

# Run tests with coverage collection
dotnet test \
  --configuration Release \
  --collect:"XPlat Code Coverage" \
  --settings "src/AspireKeyCloakTemplate.Gateway.UnitTests/.runsettings"
```

### View Coverage Reports

After running tests, coverage reports are generated in:
```
src/AspireKeyCloakTemplate.Gateway.UnitTests/bin/Release/net10.0/coverage.opencover.xml
```

You can view the XML report directly or upload it to a coverage visualization tool.

### Run with SonarQube Scanner Locally (Optional)

For local SonarQube analysis:

```bash
# Install SonarQube Scanner
dotnet tool install --global dotnet-sonarscanner

# Run tests with coverage
dotnet test \
  --configuration Release \
  --collect:"XPlat Code Coverage" \
  --settings "src/AspireKeyCloakTemplate.Gateway.UnitTests/.runsettings"

# Begin analysis
dotnet sonarscanner begin \
  /k:AspireKeyCloakTemplate \
  /o:your-org-key \
  /d:sonar.host.url=https://sonarcloud.io \
  /d:sonar.login=YOUR_SONAR_TOKEN \
  /d:sonar.cs.opencover.reportsPaths="**/coverage.opencover.xml"

# Build
dotnet build --configuration Release

# End analysis
dotnet sonarscanner end /d:sonar.login=YOUR_SONAR_TOKEN
```

## CI/CD Integration

The GitHub Actions workflow (`.github/workflows/sonarqube-cloud.yml`) automatically:

1. **Checkout** the code with full history (for better analysis)
2. **Setup .NET 10** runtime
3. **Restore** NuGet dependencies
4. **Build** in Release configuration
5. **Run tests** with XPlat code coverage collection
6. **Analyze** code using SonarQube Scanner
7. **Upload** coverage reports as artifacts

### Workflow Triggers

The workflow runs on:
- **Push** to `main` or `develop` branches
- **Pull Requests** to `main` or `develop` branches

### Viewing Results

After the workflow completes:
1. Go to [SonarQube Cloud](https://sonarcloud.io)
2. View your project dashboard
3. Check code coverage metrics, quality gates, and issues

## Configuration Details

### coverlet Configuration (`.runsettings`)

The `.runsettings` file configures coverlet to:
- Generate **OpenCover** format (required by SonarQube)
- Exclude test projects (`*.UnitTests.*`, `*.Tests.*`)
- Exclude service defaults and generated code
- Exclude code marked with `[ExcludeFromCodeCoverage]`
- Generate a single merged report file

### SonarQube Configuration (`sonar-project.properties`)

Key settings:
- **`sonar.projectKey`**: Project identifier in SonarQube
- **`sonar.coverageReportPaths`**: Path to OpenCover XML files
- **`sonar.exclusions`**: Directories to exclude from analysis (tests, AppHost, etc.)
- **`sonar.coverage.exclusions`**: Directories to exclude from coverage metrics
- **`sonar.language`**: Language set to C# (`cs`)

## Best Practices

### Coverage Thresholds

Consider setting quality gate thresholds in SonarQube Cloud:
- **Overall Code Coverage**: 75%+
- **New Code Coverage**: 80%+
- **Branch Coverage**: 70%+

Configure these in SonarQube Cloud project settings > Quality Gates.

### What Gets Measured

**Included in Coverage:**
- API controllers and services (`AspireKeyCloakTemplate.API`)
- Gateway components (`AspireKeyCloakTemplate.Gateway`)
- Business logic and domain models

**Excluded from Coverage:**
- Unit test projects (`*.UnitTests`, `*.Tests`)
- AppHost (orchestration/setup)
- ServiceDefaults (infrastructure)
- Frontend code (`react-app` - has separate Vitest coverage)

### Expanding Coverage

To add coverage for new test projects:

1. Add the test project to `.runsettings` `<Exclude>` if it's a test assembly
2. Update `sonar.tests` in `sonar-project.properties` if adding new test locations
3. Re-run the GitHub Actions workflow

## Troubleshooting

### Coverage Not Showing in SonarQube

**Symptoms**: SonarQube shows 0% coverage

**Solutions**:
1. Verify `.runsettings` is in the correct location
2. Check that tests are running: `dotnet test --collect:"XPlat Code Coverage"`
3. Confirm `sonar.cs.opencover.reportsPaths` in workflow matches generated file location
4. Check GitHub Actions logs for build/test failures

### SonarQube Token Issues

**Symptoms**: "Authentication failed" in workflow logs

**Solutions**:
1. Regenerate token in SonarQube Cloud (Account > Security)
2. Update `SONAR_TOKEN` secret in GitHub
3. Verify organization and project keys are correct

### Workflow Failures

**Check**:
- .NET 10 is available
- All dependencies are specified in `Directory.Packages.props`
- Test projects build successfully: `dotnet build`

## Additional Resources

- [SonarQube Cloud Documentation](https://docs.sonarsource.com/sonarqube-cloud/)
- [Code Coverage Overview](https://docs.sonarsource.com/sonarqube-cloud/enriching/test-coverage/overview/)
- [.NET Code Coverage](https://docs.sonarsource.com/sonarqube-cloud/enriching/test-coverage/csharp/)
- [Coverlet Documentation](https://github.com/coverlet-coverage/coverlet)
- [GitHub Actions Setup](https://github.com/SonarSource/sonarcloud-github-action)

## Questions?

For questions about SonarQube setup, refer to the [Contributing Guide](CONTRIBUTING.md) or check the project documentation.

