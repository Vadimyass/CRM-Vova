#!/bin/bash
# Общие функции для запуска и остановки. Отдельным файлом, чтобы start и stop
# одинаково понимали, где лежат pid-файлы и как гасить процессы по порту.

API_PORT=5080
WEB_PORT=5173
PROJECT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
LOG_DIR="$PROJECT_ROOT/logs"
PID_DIR="$LOG_DIR"

say() { printf '\033[1m%s\033[0m\n' "$1"; }
warn() { printf '\033[33m%s\033[0m\n' "$1"; }
fail() { printf '\033[31m%s\033[0m\n' "$1" >&2; }

# Homebrew на Apple Silicon умеет поставить x64-пакет SDK - тогда бинарник лежит
# в подпапке x64, а привычный симлинк /usr/local/share/dotnet/dotnet не создаётся.
DOTNET_CANDIDATES=(
  "/usr/local/share/dotnet/dotnet"
  "/usr/local/share/dotnet/x64/dotnet"
  "/opt/homebrew/share/dotnet/dotnet"
  "/opt/homebrew/share/dotnet/x64/dotnet"
  "/opt/homebrew/bin/dotnet"
  "/usr/local/bin/dotnet"
  "$HOME/.dotnet/dotnet"
)

resolve_dotnet() {
  local candidate
  candidate="$(command -v dotnet 2>/dev/null)"
  if [ -n "$candidate" ] && [ -x "$candidate" ]; then
    DOTNET="$candidate"
    return 0
  fi

  for candidate in "${DOTNET_CANDIDATES[@]}"; do
    if [ -x "$candidate" ]; then
      DOTNET="$candidate"
      # SDK вне PATH не найдёт свои же компоненты без DOTNET_ROOT.
      export DOTNET_ROOT="$(dirname "$candidate")"
      export PATH="$DOTNET_ROOT:$PATH"
      return 0
    fi
  done

  return 1
}

resolve_npm() {
  local candidate
  for candidate in "$(command -v npm 2>/dev/null)" \
                   /opt/homebrew/bin/npm \
                   /usr/local/bin/npm; do
    if [ -n "$candidate" ] && [ -x "$candidate" ]; then
      NPM="$candidate"
      return 0
    fi
  done
  return 1
}

# Возвращает 0, если на порту кто-то слушает.
port_busy() {
  lsof -ti "tcp:$1" -sTCP:LISTEN >/dev/null 2>&1
}

# dotnet run и vite порождают дочерние процессы, поэтому гасим по порту,
# а не только по сохранённому pid - иначе сервер переживает остановку.
stop_port() {
  local port="$1" pids
  pids="$(lsof -ti "tcp:$port" -sTCP:LISTEN 2>/dev/null || true)"
  [ -z "$pids" ] && return 0
  echo "$pids" | xargs kill 2>/dev/null || true
  for _ in 1 2 3 4 5 6 7 8 9 10; do
    port_busy "$port" || return 0
    sleep 0.5
  done
  pids="$(lsof -ti "tcp:$port" -sTCP:LISTEN 2>/dev/null || true)"
  [ -n "$pids" ] && echo "$pids" | xargs kill -9 2>/dev/null || true
  return 0
}

wait_for_url() {
  local url="$1" seconds="$2" waited=0
  while [ "$waited" -lt "$seconds" ]; do
    if curl -fsS -o /dev/null --max-time 2 "$url" 2>/dev/null; then
      return 0
    fi
    sleep 1
    waited=$((waited + 1))
  done
  return 1
}
