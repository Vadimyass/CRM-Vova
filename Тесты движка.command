#!/bin/bash
cd "$(dirname "$0")" || exit 1
bash ./scripts/test.sh
echo
read -r -p "Нажми Enter, чтобы закрыть окно… " _
