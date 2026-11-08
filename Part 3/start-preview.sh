#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

export ASPNETCORE_ENVIRONMENT=Development
export RACEDAY_DATABASE_PROVIDER=InMemory
if [[ -z "${RACEDAY_MEDIA_PROVIDER:-}" ]]; then
  if [[ -n "${AZURE_STORAGE_CONNECTION_STRING:-}" ]]; then
    export RACEDAY_MEDIA_PROVIDER=Azure
  else
    export RACEDAY_MEDIA_PROVIDER=Local
  fi
fi
export RaceDay__InMemoryDatabaseName=RaceDayPreview
export ApiBaseUrl=http://127.0.0.1:5001/

API_PID=""
WEB_PID=""

dotnet build "Part 2/RaceDay.sln" --configuration Release

cleanup() {
  local exit_code=$?
  trap - EXIT INT TERM
  if [[ -n "$WEB_PID" ]]; then
    kill "$WEB_PID" 2>/dev/null || true
  fi
  if [[ -n "$API_PID" ]]; then
    kill "$API_PID" 2>/dev/null || true
  fi
  if [[ -n "$WEB_PID" ]]; then
    wait "$WEB_PID" 2>/dev/null || true
  fi
  if [[ -n "$API_PID" ]]; then
    wait "$API_PID" 2>/dev/null || true
  fi
  exit "$exit_code"
}

trap cleanup EXIT INT TERM

(
  cd "Part 2/RaceDay.Api"
  exec dotnet "bin/Release/net8.0/RaceDay.Api.dll" --urls http://127.0.0.1:5001
) &
API_PID=$!

for _ in $(seq 1 60); do
  if curl --silent --fail http://127.0.0.1:5001/api/healthz >/dev/null; then
    break
  fi
  if ! kill -0 "$API_PID" 2>/dev/null; then
    echo "RaceDay API stopped before it became healthy." >&2
    exit 1
  fi
  sleep 1
done

if ! curl --silent --fail http://127.0.0.1:5001/api/healthz >/dev/null; then
  echo "RaceDay API did not become healthy on port 5001." >&2
  exit 1
fi

WEB_PORT="${PORT:-3000}"
(
  cd "Part 3/RaceDay.Mvc"
  exec dotnet "bin/Release/net8.0/RaceDay.Mvc.dll" --urls "http://0.0.0.0:${WEB_PORT}"
) &
WEB_PID=$!
wait "$WEB_PID"
