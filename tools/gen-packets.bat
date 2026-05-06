@echo off
setlocal

node "%~dp0gen-packets.js"
if %ERRORLEVEL% neq 0 (
    exit /b %ERRORLEVEL%
)

endlocal
