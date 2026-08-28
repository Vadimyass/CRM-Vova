#!/bin/bash
set -uo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"
cd "$PROJECT_ROOT"

printf '\033]0;CRM Vova\007'
say "CRM Vova — запуск"
echo

if ! resolve_dotnet; then
  fail "Не найден .NET SDK."
  echo "Поставь .NET 10 SDK: brew install --cask dotnet-sdk"
  echo "или скачай с https://dotnet.microsoft.com/download/dotnet/10.0"
  echo
  read -r -p "Нажми Enter, чтобы закрыть окно… " _
  exit 1
fi

SDK_MAJOR="$("$DOTNET" --list-sdks | awk -F. '{print $1}' | sort -n | tail -1)"
if [ -z "$SDK_MAJOR" ] || [ "$SDK_MAJOR" -lt 10 ]; then
  fail "Нужен .NET SDK 10, найдено: $("$DOTNET" --list-sdks | tr '\n' ' ')"
  echo "Поставь: brew install --cask dotnet-sdk"
  echo
  read -r -p "Нажми Enter, чтобы закрыть окно… " _
  exit 1
fi

if ! resolve_npm; then
  fail "Не найден npm. Поставь Node.js: brew install node"
  echo
  read -r -p "Нажми Enter, чтобы закрыть окно… " _
  exit 1
fi

mkdir -p "$LOG_DIR"

if port_busy "$API_PORT" || port_busy "$WEB_PORT"; then
  warn "Порты $API_PORT / $WEB_PORT заняты — останавливаю предыдущий запуск."
  stop_port "$API_PORT"
  stop_port "$WEB_PORT"
fi

if [ ! -d frontend/node_modules/vite ]; then
  say "Первый запуск: ставлю зависимости фронта (это займёт минуту)…"
  (cd frontend && "$NPM" install) || {
    fail "npm install упал. Смотри вывод выше."
    read -r -p "Нажми Enter, чтобы закрыть окно… " _
    exit 1
  }
  echo
fi

say "Собираю бэкенд…"
if ! "$DOTNET" build CrmVova.slnx -v q --nologo > "$LOG_DIR/build.log" 2>&1; then
  fail "Сборка упала. Полный вывод: logs/build.log"
  tail -25 "$LOG_DIR/build.log"
  echo
  read -r -p "Нажми Enter, чтобы закрыть окно… " _
  exit 1
fi

CLEANED=0
cleanup() {
  [ "$CLEANED" = "1" ] && return 0
  CLEANED=1
  echo
  say "Останавливаю…"
  stop_port "$API_PORT"
  stop_port "$WEB_PORT"
  rm -f "$PID_DIR/api.pid" "$PID_DIR/web.pid" 2>/dev/null || true
  say "Остановлено."
}
trap cleanup EXIT INT TERM HUP

say "Стартую API на http://localhost:$API_PORT …"
ASPNETCORE_ENVIRONMENT=Development \
  ASPNETCORE_URLS="http://localhost:$API_PORT" \
  "$DOTNET" run --project src/Crm.Api --no-build --no-launch-profile \
  > "$LOG_DIR/api.log" 2>&1 &
echo $! > "$PID_DIR/api.pid"

if ! wait_for_url "http://localhost:$API_PORT/api/stages" 90; then
  fail "API не поднялся за 90 секунд. Полный вывод: logs/api.log"
  tail -25 "$LOG_DIR/api.log"
  echo
  read -r -p "Нажми Enter, чтобы закрыть окно… " _
  exit 1
fi

say "Стартую фронт на http://localhost:$WEB_PORT …"
(cd frontend && "$NPM" run dev -- --port "$WEB_PORT" --strictPort) > "$LOG_DIR/web.log" 2>&1 &
echo $! > "$PID_DIR/web.pid"

if ! wait_for_url "http://localhost:$WEB_PORT" 60; then
  fail "Фронт не поднялся за 60 секунд. Полный вывод: logs/web.log"
  tail -25 "$LOG_DIR/web.log"
  echo
  read -r -p "Нажми Enter, чтобы закрыть окно… " _
  exit 1
fi

open "http://localhost:$WEB_PORT" 2>/dev/null || true

echo
say "Готово."
echo "  Интерфейс:  http://localhost:$WEB_PORT"
echo "  API:        http://localhost:$API_PORT"
echo "  Логи:       logs/api.log, logs/web.log"
echo
warn "Чтобы остановить — нажми Ctrl+C или просто закрой это окно."
echo

wait
