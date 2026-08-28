#!/bin/bash
# Поднимает API, гоняет сквозной сценарий, гасит API. Используется в CI и локально.
set -uo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

ASPNETCORE_URLS="http://localhost:5080" dotnet run --project src/Crm.Api \
  --configuration Release --no-build --no-launch-profile > /tmp/ci-api.log 2>&1 &
API_PID=$!

cleanup() {
  kill "$API_PID" 2>/dev/null || true
  wait "$API_PID" 2>/dev/null || true
}
trap cleanup EXIT

if ! python3 scripts/smoke.py "http://localhost:5080"; then
  echo "--- лог API ---"
  tail -40 /tmp/ci-api.log
  exit 1
fi
