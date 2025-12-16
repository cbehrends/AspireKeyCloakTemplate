# GitVersion Guide

This guide explains how GitVersion is configured and used in this repository for automated versioning.

## Overview

GitVersion is a tool that automatically calculates semantic version numbers based on your Git repository's branch structure and commit history. It replaces the previous semantic-release setup with a simpler, branch-based versioning strategy.

## Configuration

The GitVersion configuration is stored in `GitVersion.yml` at the repository root. The configuration defines how versions are calculated for different branches:

### Branch Strategies

#### Main Branch
- **Pattern**: `^main$`
- **Mode**: `ContinuousDelivery`
- **Version Label**: None (stable releases)
- **Increment**: `Patch`
- **Example Version**: `1.0.1`

Commits to the main branch produce stable release versions without pre-release labels.

#### Develop Branch
- **Pattern**: `^develop$`
- **Mode**: `ContinuousDeployment`
- **Version Label**: `rc` (release candidate)
- **Increment**: `Minor`
- **Example Version**: `1.1.0-rc.1`

Commits to the develop branch produce pre-release versions with the `-rc` suffix.

#### Feature Branches
- **Pattern**: `^features?[/-]`
- **Mode**: `ContinuousDeployment`
- **Version Label**: `alpha`
- **Increment**: `Inherit` (from parent branch)
- **Example Version**: `1.1.0-alpha.1`

Feature branches produce alpha versions.

#### Pull Request Branches
- **Pattern**: `^(pull|pull\-requests|pr)[/-]`
- **Mode**: `ContinuousDeployment`
- **Version Label**: `pr`
- **Increment**: `Inherit`
- **Example Version**: `1.0.1-pr.1`

Pull request branches produce PR-specific versions.

## GitHub Actions Integration

GitVersion is integrated into the CI/CD pipeline through a dedicated job:

```yaml
jobs:
  gitversion:
    name: Calculate Version
    runs-on: ubuntu-latest
    outputs:
      semVer: ${{ steps.gitversion.outputs.semVer }}
      fullSemVer: ${{ steps.gitversion.outputs.fullSemVer }}
      majorMinorPatch: ${{ steps.gitversion.outputs.majorMinorPatch }}
    steps:
      - name: Checkout
        uses: actions/checkout@v4
        with:
          fetch-depth: 0  # Full history required

      - name: Install GitVersion
        uses: gittools/actions/gitversion/setup@v3.0.0
        with:
          versionSpec: '6.x'

      - name: Determine Version
        id: gitversion
        uses: gittools/actions/gitversion/execute@v3.0.0
        with:
          useConfigFile: true
```

### Version Outputs

The `gitversion` job provides three outputs that other jobs can use:

- **`semVer`**: Semantic version with pre-release label (e.g., `1.2.3-rc.1`)
- **`fullSemVer`**: Full semantic version including build metadata (e.g., `1.2.3-rc.1+5`)
- **`majorMinorPatch`**: Base version without pre-release or metadata (e.g., `1.2.3`)

### Using Version in Other Jobs

Other jobs can depend on the `gitversion` job and use its outputs:

```yaml
build-docker:
  needs: [gitversion, build-react, test-dotnet]
  steps:
    - name: Build API image
      uses: docker/build-push-action@v5
      with:
        tags: |
          api:${{ needs.gitversion.outputs.fullSemVer }}
          api:latest
```

## Local Development

### Installing GitVersion

Install GitVersion as a global .NET tool:

```bash
dotnet tool install --global GitVersion.Tool
```

### Running GitVersion Locally

Calculate the version for your current branch:

```bash
dotnet-gitversion
```

Display specific version variables:

```bash
dotnet-gitversion /showvariable FullSemVer
dotnet-gitversion /showvariable MajorMinorPatch
dotnet-gitversion /showvariable SemVer
```

View the configuration:

```bash
dotnet-gitversion /showconfig
```

## Version Calculation Examples

### Scenario 1: First Release on Main
- **Commit**: Initial commit on main branch
- **Tags**: `v1.0.0` (manually created)
- **Calculated Version**: `1.0.0`

### Scenario 2: Subsequent Commits on Main
- **Previous Tag**: `v1.0.0`
- **New Commits**: 3 commits since tag
- **Calculated Version**: `1.0.1` (patch increment)

### Scenario 3: Development on Develop Branch
- **Base Version**: `1.0.0`
- **Branch**: `develop`
- **Commits**: 2 commits
- **Calculated Version**: `1.1.0-rc.2`

### Scenario 4: Feature Branch
- **Base Version**: `1.0.0`
- **Branch**: `feature/new-auth`
- **Commits**: 1 commit
- **Calculated Version**: `1.0.1-alpha.1`

## Creating Manual Tags

You can manually create tags to set a specific version:

```bash
# Create a tag on main branch
git checkout main
git tag 1.0.0
git push origin 1.0.0
```

GitVersion will use this tag as the base version for future calculations.

## Troubleshooting

### GitVersion doesn't calculate the expected version

**Solution**: Ensure you have the full git history. GitHub Actions needs `fetch-depth: 0`:

```yaml
- name: Checkout
  uses: actions/checkout@v4
  with:
    fetch-depth: 0
```

### Error: "Repository is a shallow clone"

**Solution**: This occurs when git history is incomplete. Either:
- Use `fetch-depth: 0` in checkout actions
- Run `git fetch --unshallow` locally

### Configuration errors

**Solution**: Validate your `GitVersion.yml` syntax:

```bash
dotnet-gitversion /showconfig
```

Check the GitVersion documentation for valid configuration properties.

### Version not incrementing

**Solution**: 
- Ensure you're on the correct branch
- Check that appropriate tags exist in the repository
- Verify the branch matches the regex pattern in `GitVersion.yml`

## Migrating from semantic-release

This repository previously used semantic-release. The migration to GitVersion includes:

### What Changed
1. ✅ Removed `.releaserc.json` configuration
2. ✅ Removed semantic-release dependencies from `package.json`
3. ✅ Removed semantic-release job from GitHub Actions workflow
4. ✅ Added `GitVersion.yml` configuration
5. ✅ Added `gitversion` job to GitHub Actions workflow
6. ✅ Updated Docker build to use GitVersion outputs

### What Stayed the Same
- Conventional commit hooks (commitlint) still work
- Branch structure (main, develop) remains the same
- Docker image tagging follows the same pattern
- CI/CD pipeline structure is mostly unchanged

### Key Differences
- **Version source**: GitVersion uses branch structure instead of commit messages
- **Manual control**: You can manually create tags to set specific versions
- **Simpler setup**: No Node.js dependencies required for versioning
- **Local testing**: Easy to test version calculation locally with `dotnet-gitversion`

## Resources

- [GitVersion Documentation](https://gitversion.net/)
- [GitVersion Configuration Reference](https://gitversion.net/docs/reference/configuration)
- [GitVersion GitHub Actions](https://github.com/GitTools/actions)
- [Semantic Versioning Specification](https://semver.org/)

## Support

For issues or questions about GitVersion in this repository:
1. Check this guide first
2. Review the [GitVersion documentation](https://gitversion.net/docs/)
3. Examine the `.github/workflows/build.yml` workflow
4. Check the `GitVersion.yml` configuration
