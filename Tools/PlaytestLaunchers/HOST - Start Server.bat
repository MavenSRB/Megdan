@echo off
cd /d "%~dp0"
start "Guards Digital Host" GuardsDigitalSmoke.exe -guardsPrivateLobby -logFile "host.log"
