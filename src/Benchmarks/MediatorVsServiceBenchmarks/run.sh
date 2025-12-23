#!/usr/bin/env zsh
set -euo pipefail

dotnet run -c Release --project "$(dirname "$0")/MediatorVsServiceBenchmarks.csproj" -- --filter *Scenarios*

