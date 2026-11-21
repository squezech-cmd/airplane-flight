#!/usr/bin/env bash
# publish.sh — публикация Dear Passengers в репозиторий squezech-cmd/dear-passengers.
#
# Запускать ПОСЛЕ двух шагов на GitHub (один раз):
#   1) Создайте пустой репозиторий  https://github.com/new  → имя: dear-passengers, Public.
#      (без README/.gitignore — он должен быть пустым)
#   2) Добавьте секреты репозитория Settings → Secrets and variables → Actions:
#        UNITY_LICENSE  — содержимое .ulf-файла личной лицензии Unity (game.ci → Personal License)
#        UNITY_EMAIL    — e-mail Unity-аккаунта
#        UNITY_PASSWORD — пароль Unity-аккаунта
#
# После запуска этого скрипта: GitHub Actions соберёт DearPassengers.exe и
# создаст Release v1.0.0 с готовым архивом.
set -euo pipefail

REPO="${1:-https://github.com/squezech-cmd/dear-passengers.git}"

cd "$(dirname "$0")"

echo "==> Публикация в $REPO"
git remote remove deploy 2>/dev/null || true
git remote add deploy "$REPO"

echo "==> Отправка ветки в main…"
git push deploy HEAD:main

echo "==> Отправка тегов (запускает сборку и Release)…"
git push deploy --tags

echo ""
echo "Готово! Следите за сборкой: https://github.com/squezech-cmd/dear-passengers/actions"
echo "Готовый .exe появится в: https://github.com/squezech-cmd/dear-passengers/releases"
