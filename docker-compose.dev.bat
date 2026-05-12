@echo off
setlocal

cd /d "%~dp0"

if not exist ".env.dev" (
  echo [docker-compose.dev] ERROR: .env.dev not found.
  exit /b 1
)

docker compose --env-file .env.dev -f docker-compose.yml -f docker-compose.dev.yml up -d %*
exit /b %ERRORLEVEL%
