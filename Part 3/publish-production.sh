#!/usr/bin/env bash
set -euo pipefail

PART3_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WORKSPACE_ROOT="$(cd "$PART3_DIR/.." && pwd)"
PUBLISH_ROOT="$WORKSPACE_ROOT/artifacts/raceday/dist/server"

cd "$WORKSPACE_ROOT"
dotnet restore "Part 2/RaceDay.sln"
dotnet publish "Part 2/RaceDay.Api/RaceDay.Api.csproj" \
  --configuration Release \
  --no-restore \
  --output "$PUBLISH_ROOT/api" \
  /p:UseAppHost=false
dotnet publish "Part 3/RaceDay.Mvc/RaceDay.Mvc.csproj" \
  --configuration Release \
  --no-restore \
  --output "$PUBLISH_ROOT/mvc" \
  /p:UseAppHost=false
