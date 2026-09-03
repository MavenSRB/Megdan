@echo off
cd /d "%~dp0"
start "Guards Digital" GuardsDigitalSmoke.exe -guardsPrivateLobby -logFile "playtest-%USERNAME%.log"
