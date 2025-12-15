#!/bin/bash

# Script to run tests with code coverage locally
# This script mimics what the GitHub Actions workflow does
# Usage: ./scripts/test-with-coverage.sh

set -e  # Exit on error

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(dirname "$SCRIPT_DIR")"

echo "🧪 Running tests with code coverage..."
echo "📂 Working directory: $ROOT_DIR"
echo ""

# Colors for output
GREEN='\033[0;32m'
BLUE='\033[0;34m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Step 1: Clean previous coverage reports
echo -e "${BLUE}Step 1: Cleaning previous coverage reports...${NC}"
find "$ROOT_DIR" -name "coverage.opencover.xml" -delete
find "$ROOT_DIR" -type d -name "TestResults" -exec rm -rf {} + 2>/dev/null || true
echo -e "${GREEN}✓ Cleaned${NC}"
echo ""

# Step 2: Restore dependencies
echo -e "${BLUE}Step 2: Restoring dependencies...${NC}"
cd "$ROOT_DIR"
dotnet restore
echo -e "${GREEN}✓ Restored${NC}"
echo ""

# Step 3: Build solution
echo -e "${BLUE}Step 3: Building solution...${NC}"
dotnet build --configuration Release --no-restore
echo -e "${GREEN}✓ Built${NC}"
echo ""

# Step 4: Run tests with coverage
echo -e "${BLUE}Step 4: Running tests with code coverage...${NC}"
dotnet test \
  --configuration Release \
  --no-build \
  --logger "console;verbosity=normal" \
  --collect:"XPlat Code Coverage" \
  --settings "src/AspireKeyCloakTemplate.Gateway.UnitTests/.runsettings" \
  /p:CollectCoverage=true \
  /p:CoverletOutputFormat=opencover
echo -e "${GREEN}✓ Tests completed${NC}"
echo ""

# Step 5: Locate coverage report
echo -e "${BLUE}Step 5: Locating coverage report...${NC}"
COVERAGE_FILE=$(find "$ROOT_DIR" -name "coverage.opencover.xml" | head -1)

if [ -z "$COVERAGE_FILE" ]; then
  echo -e "${YELLOW}⚠ Warning: No coverage report found${NC}"
else
  echo -e "${GREEN}✓ Coverage report: ${NC}$COVERAGE_FILE"
  echo ""
  echo -e "${YELLOW}📊 Coverage Summary:${NC}"
  echo "   Location: $COVERAGE_FILE"
  echo "   Size: $(du -h "$COVERAGE_FILE" | cut -f1)"
  echo ""
  echo -e "${BLUE}Next steps:${NC}"
  echo "   1. Upload to SonarQube Cloud via GitHub Actions"
  echo "   2. View detailed reports in SonarQube Cloud project dashboard"
  echo "   3. For local analysis, install dotnet-sonarscanner and run:"
  echo "      dotnet tool install --global dotnet-sonarscanner"
fi

echo ""
echo -e "${GREEN}✓ All tests completed with coverage!${NC}"

