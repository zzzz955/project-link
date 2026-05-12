#!/usr/bin/env sh
set -eu

cd "$(dirname "$0")"

status=0
if [ ! -f ".env.prod" ]; then
  echo "[docker-compose.prod] ERROR: .env.prod not found." >&2
  status=1
else
  docker compose --env-file .env.prod -f docker-compose.yml -f docker-compose.prod.yml up -d "$@" || status=$?
fi

if [ "$status" -eq 0 ]; then
  echo "[docker-compose.prod] Finished with exit code 0."
else
  echo "[docker-compose.prod] Finished with exit code $status." >&2
fi

if [ -t 0 ]; then
  printf 'Press Enter to close...'
  read -r _
fi

exit "$status"
