@echo off
cd /d "%~dp0"
for /f %%I in ('powershell.exe -NoProfile -Command "[guid]::NewGuid().ToString('N')"') do set "GUARDS_CAPTURE_ID=%%I"
if not defined GUARDS_CAPTURE_ID exit /b 1
set "GUARDS_LOG_DIR=Logs\launches\%GUARDS_CAPTURE_ID%"
mkdir "%GUARDS_LOG_DIR%" || exit /b 1
start "Guards Digital" GuardsDigitalSmoke.exe -guardsPrivateLobby -logFile "%GUARDS_LOG_DIR%\Player.log"
