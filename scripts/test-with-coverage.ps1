# Script to run tests with code coverage locally (Windows)
# This script mimics what the GitHub Actions workflow does
# Usage: .\scripts\test-with-coverage.ps1

param(
    [switch]$Clean = $false
)

$ErrorActionPreference = "Stop"

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootDir = Split-Path -Parent $scriptDir

Write-Host "🧪 Running tests with code coverage..." -ForegroundColor Cyan
Write-Host "📂 Working directory: $rootDir" -ForegroundColor Cyan
Write-Host ""

# Step 1: Clean previous coverage reports
if ($Clean) {
    Write-Host "Step 1: Cleaning previous coverage reports..." -ForegroundColor Blue
    Get-ChildItem -Path $rootDir -Recurse -Filter "coverage.opencover.xml" | Remove-Item -Force
    Get-ChildItem -Path $rootDir -Recurse -Directory -Filter "TestResults" | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "✓ Cleaned" -ForegroundColor Green
    Write-Host ""
}

# Step 2: Restore dependencies
Write-Host "Step 2: Restoring dependencies..." -ForegroundColor Blue
Push-Location $rootDir
dotnet restore
Pop-Location
Write-Host "✓ Restored" -ForegroundColor Green
Write-Host ""

# Step 3: Build solution
Write-Host "Step 3: Building solution..." -ForegroundColor Blue
dotnet build --configuration Release --no-restore
Write-Host "✓ Built" -ForegroundColor Green
Write-Host ""

# Step 4: Run tests with coverage
Write-Host "Step 4: Running tests with code coverage..." -ForegroundColor Blue
dotnet test `
    --configuration Release `
    --no-build `
    --logger "console;verbosity=normal" `
    --collect:"XPlat Code Coverage" `
    --settings "src/AspireKeyCloakTemplate.Gateway.UnitTests/.runsettings" `
    /p:CollectCoverage=true `
    /p:CoverletOutputFormat=opencover
Write-Host "✓ Tests completed" -ForegroundColor Green
Write-Host ""

# Step 5: Locate coverage report
Write-Host "Step 5: Locating coverage report..." -ForegroundColor Blue
$coverageFile = Get-ChildItem -Path $rootDir -Recurse -Filter "coverage.opencover.xml" | Select-Object -First 1

if ($null -eq $coverageFile) {
    Write-Host "⚠ Warning: No coverage report found" -ForegroundColor Yellow
}
else {
    Write-Host "✓ Coverage report: $($coverageFile.FullName)" -ForegroundColor Green
    Write-Host ""
    Write-Host "📊 Coverage Summary:" -ForegroundColor Yellow
    Write-Host "   Location: $($coverageFile.FullName)"
    Write-Host "   Size: $([math]::Round($coverageFile.Length / 1KB, 2)) KB"
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Blue
    Write-Host "   1. Upload to SonarQube Cloud via GitHub Actions"
    Write-Host "   2. View detailed reports in SonarQube Cloud project dashboard"
    Write-Host "   3. For local analysis, install dotnet-sonarscanner and run:"
    Write-Host "      dotnet tool install --global dotnet-sonarscanner"
}

Write-Host ""
Write-Host "✓ All tests completed with coverage!" -ForegroundColor Green

