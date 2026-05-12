#!/usr/bin/env sh
set -eu

cd "$(dirname "$0")"

if [ ! -f ".env.prod" ]; then
  echo "[docker-compose.prod] ERROR: .env.prod not found." >&2
  exit 1
fi

docker compose --env-file .env.prod -f docker-compose.yml -f docker-compose.prod.yml up -d "$@"
