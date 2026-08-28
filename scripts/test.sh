#!/bin/bash
set -uo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"
cd "$PROJECT_ROOT"

resolve_dotnet || { fail "Не найден .NET SDK."; exit 1; }
say "Тесты движка процессов"
"$DOTNET" run --project tests/Crm.Bpm.Tests --nologo
