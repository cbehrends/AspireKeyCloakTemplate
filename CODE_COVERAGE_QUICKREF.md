# Code Coverage Quick Reference

## TL;DR - Running Tests with Coverage

### macOS/Linux
```bash
./scripts/test-with-coverage.sh
```

### Windows
```powershell
.\scripts\test-with-coverage.ps1
```

Or manually:
```bash
dotnet test --configuration Release --collect:"XPlat Code Coverage" --settings "src/AspireKeyCloakTemplate.Gateway.UnitTests/.runsettings"
```

## Files Added for Code Coverage

| File | Purpose |
|------|---------|
| `src/AspireKeyCloakTemplate.Gateway.UnitTests/.runsettings` | Configures coverlet coverage collection in OpenCover format |
| `sonar-project.properties` | SonarQube Cloud configuration and analysis settings |
| `.github/workflows/sonarqube-cloud.yml` | GitHub Actions workflow for automated code coverage analysis |
| `SONARQUBE_SETUP.md` | Detailed setup and configuration guide |
| `scripts/test-with-coverage.sh` | Bash script for local testing with coverage (macOS/Linux) |
| `scripts/test-with-coverage.ps1` | PowerShell script for local testing with coverage (Windows) |

## Coverage Report Location

After running tests, coverage reports are generated at:
```
src/AspireKeyCloakTemplate.Gateway.UnitTests/bin/Release/net10.0/coverage.opencover.xml
```

## Key Concepts

### What's Being Measured
- **API**: `AspireKeyCloakTemplate.API` controllers and services
- **Gateway**: `AspireKeyCloakTemplate.Gateway` routing and middleware
- **Test Project**: `AspireKeyCloakTemplate.Gateway.UnitTests` (excluded from coverage metrics)

### What's Excluded
- Test assemblies (`*.UnitTests`, `*.Tests`)
- AppHost (orchestration)
- ServiceDefaults (infrastructure)
- Frontend code (`react-app` - measured separately with Vitest)

## Next Steps

1. **Local Testing**: Run `./scripts/test-with-coverage.sh` to verify coverage locally
2. **Setup SonarQube Cloud**: Follow [SONARQUBE_SETUP.md](SONARQUBE_SETUP.md) for GitHub integration
3. **Configure Secrets**: Add GitHub Secrets as described in the setup guide
4. **Review Results**: Check SonarQube Cloud dashboard after first workflow run

## Troubleshooting

| Issue | Solution |
|-------|----------|
| `coverage.opencover.xml` not generated | Ensure `.runsettings` file exists and tests are running |
| SonarQube shows 0% coverage | Verify workflow secrets are set correctly |
| Tests fail during coverage collection | Check that `coverlet.collector` NuGet package is installed |
| Script won't run (Linux/macOS) | Run `chmod +x ./scripts/test-with-coverage.sh` |

## Documentation

- **Full Setup Guide**: See [SONARQUBE_SETUP.md](SONARQUBE_SETUP.md)
- **Testing Guidelines**: See [TESTING_GUIDELINES.md](TESTING_GUIDELINES.md)
- **SonarQube Docs**: https://docs.sonarsource.com/sonarqube-cloud/

