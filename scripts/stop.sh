#!/bin/bash
set -uo pipefail
source "$(dirname "${BASH_SOURCE[0]}")/lib.sh"

say "CRM Vova — остановка"
stop_port "$API_PORT"
stop_port "$WEB_PORT"
rm -f "$PID_DIR/api.pid" "$PID_DIR/web.pid" 2>/dev/null || true
say "Остановлено."
sleep 1
