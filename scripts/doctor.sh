#!/bin/bash
set -uo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

say "CRM Vova — проверка окружения"
echo
echo "Архитектура: $(uname -m)"
echo

echo "— .NET —"
if resolve_dotnet; then
  echo "  найден: $DOTNET"
  echo "  SDK:"
  "$DOTNET" --list-sdks 2>/dev/null | sed 's/^/    /'
  SDK_MAJOR="$("$DOTNET" --list-sdks 2>/dev/null | awk -F. '{print $1}' | sort -n | tail -1)"
  if [ -n "$SDK_MAJOR" ] && [ "$SDK_MAJOR" -ge 10 ]; then
    echo "  версия: подходит"
  else
    warn "  нужен SDK 10 или новее"
  fi
else
  fail "  не найден"
  echo "  проверенные пути:"
  for candidate in "${DOTNET_CANDIDATES[@]}"; do
    printf '    %s' "$candidate"
    [ -e "$candidate" ] && echo "  (есть, но не исполняемый)" || echo "  (нет)"
  done
  echo
  echo "  Поищи вручную:  find /usr/local/share /opt/homebrew/share \"$HOME/.dotnet\" -maxdepth 3 -name dotnet -type f 2>/dev/null"
fi
echo

echo "— Node —"
if resolve_npm; then
  echo "  npm:  $NPM ($("$NPM" -v 2>/dev/null))"
  echo "  node: $(command -v node 2>/dev/null || echo 'не в PATH') ($(node -v 2>/dev/null))"
else
  fail "  npm не найден. Поставь: brew install node"
fi
echo

echo "— Порты —"
for port in "$API_PORT" "$WEB_PORT"; do
  if port_busy "$port"; then
    warn "  $port занят (это может быть предыдущий запуск CRM)"
  else
    echo "  $port свободен"
  fi
done
