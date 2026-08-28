#!/bin/bash
# Ловит секреты до того, как они уедут в публичный репозиторий:
# запрещённые файлы в индексе и характерные шаблоны в содержимом отслеживаемых файлов.
set -uo pipefail
cd "$(dirname "${BASH_SOURCE[0]}")/.."

FAILED=0

report() {
  echo "ПРОВАЛ: $1"
  FAILED=1
}

# --- 1. Файлы, которых в репозитории быть не должно ---
FORBIDDEN=(
  ".env"
  "*.pfx" "*.p12" "*.pem" "*.key" "*.jks" "*.keystore"
  "secrets.json"
  "appsettings.Local.json" "appsettings.*.Local.json"
  "*.sql" "*.dump" "*.bak"
  "id_rsa" "id_ed25519"
)

for pattern in "${FORBIDDEN[@]}"; do
  matches="$(git ls-files -- "$pattern" 2>/dev/null)"
  if [ -n "$matches" ]; then
    report "в индексе есть файлы по запрещённому шаблону '$pattern':"
    echo "$matches" | sed 's/^/    /'
  fi
done

# --- 2. Шаблоны в содержимом ---
# Исключаем сам скрипт и образец переменных: там эти строки по делу.
FILES="$(git ls-files | grep -v -E '^(scripts/check-secrets\.sh|\.env\.example|package-lock\.json|frontend/package-lock\.json|SECURITY\.md)$' || true)"
[ -z "$FILES" ] && exit 0

scan() {
  local name="$1" pattern="$2"
  local hits
  hits="$(echo "$FILES" | xargs -r grep -nIE "$pattern" 2>/dev/null || true)"
  if [ -n "$hits" ]; then
    report "$name"
    echo "$hits" | head -10 | sed 's/^/    /'
  fi
}

scan "приватный ключ в файле" 'BEGIN [A-Z ]*PRIVATE KEY'
scan "ключ доступа AWS" 'AKIA[0-9A-Z]{16}'
scan "токен GitHub" '(ghp|gho|ghs|ghu)_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,}'
scan "токен Slack" 'xox[baprs]-[A-Za-z0-9-]{10,}'
scan "ключ OpenAI или подобный" 'sk-[A-Za-z0-9_-]{32,}'
scan "пароль в строке подключения" '(Password|Pwd)[[:space:]]*=[[:space:]]*[^;"'"'"'[:space:]]{4,}'
scan "секрет в конфигурации" '"(ClientSecret|ApiKey|SigningKey|AccessToken|PrivateKey)"[[:space:]]*:[[:space:]]*"[^"]{8,}"'

if [ "$FAILED" -eq 0 ]; then
  echo "Секретов в отслеживаемых файлах не найдено."
fi

exit "$FAILED"
