#!/usr/bin/env bash
set -euo pipefail

PART3_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WORKSPACE_ROOT="$(cd "$PART3_DIR/.." && pwd)"
PUBLISH_ROOT="$WORKSPACE_ROOT/artifacts/raceday/dist/server"
WEB_PORT="${PORT:-8080}"

cd "$WORKSPACE_ROOT"

: "${RACEDAY_SQLSERVER_CONNECTION:?Configure RACEDAY_SQLSERVER_CONNECTION as a production secret.}"
: "${AZURE_STORAGE_CONNECTION_STRING:?Configure AZURE_STORAGE_CONNECTION_STRING as a production secret.}"

export ASPNETCORE_ENVIRONMENT=Production
export RACEDAY_DATABASE_PROVIDER=SqlServer
export RACEDAY_MEDIA_PROVIDER=Azure
export RaceDay__ApiBaseUrl=http://127.0.0.1:5001/

API_PID=""
WEB_PID=""

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
  exec dotnet "$PUBLISH_ROOT/api/RaceDay.Api.dll" --urls http://127.0.0.1:5001
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

(
  cd "Part 3/RaceDay.Mvc"
  exec dotnet "$PUBLISH_ROOT/mvc/RaceDay.Mvc.dll" --urls "http://0.0.0.0:${WEB_PORT}"
) &
WEB_PID=$!
wait "$WEB_PID"
