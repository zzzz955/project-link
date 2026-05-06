@echo off
setlocal

node "%~dp0gen-orm.js"
if %ERRORLEVEL% neq 0 (
    exit /b %ERRORLEVEL%
)

endlocal
