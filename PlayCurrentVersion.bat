@echo off
setlocal

set "PROJECT_DIR=%~dp0"
if "%PROJECT_DIR:~-1%"=="\" set "PROJECT_DIR=%PROJECT_DIR:~0,-1%"

set "UNITY_EXE=C:\Program Files\Unity\Hub\Editor\6000.5.7f1\Editor\Unity.exe"
set "LAUNCH_METHOD=GuardsDigital.EditorTools.CurrentPrototypeLauncher.OpenCurrentSceneAndEnterPlayMode"

if not exist "%UNITY_EXE%" (
    echo Unity was not found at:
    echo %UNITY_EXE%
    echo.
    echo Update UNITY_EXE in this file if the editor is installed somewhere else.
    pause
    exit /b 1
)

echo Launching the current Guards Digital prototype...
start "" "%UNITY_EXE%" -projectPath "%PROJECT_DIR%" -executeMethod %LAUNCH_METHOD%
