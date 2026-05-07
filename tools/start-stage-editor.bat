@echo off
setlocal

cd /d "%~dp0stage-tool"

echo [stage-editor] Starting stage editor...
echo [stage-editor] UI   : http://127.0.0.1:5174
echo [stage-editor] API  : http://127.0.0.1:5178
echo [stage-editor] CRUD logs appear below.
echo.

npm run dev

echo.
echo [stage-editor] Server stopped.
pause
endlocal
