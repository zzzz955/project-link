@echo off
setlocal

node "%~dp0gen-data.js"
if %ERRORLEVEL% neq 0 (
    exit /b %ERRORLEVEL%
)

endlocal
