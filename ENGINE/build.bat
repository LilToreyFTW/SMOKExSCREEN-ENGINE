@echo off
title SmokeScreen ENGINE Builder
echo ========================================
echo Installing Dependencies...
echo ========================================

call dotnet add SmokeScreenEngineGUI.csproj package System.Management --version 8.0.0
call dotnet add SmokeScreenEngineGUI.csproj package Newtonsoft.Json --version 13.0.3

echo.
echo ========================================
echo Building SmokeScreen ENGINE GUI...
echo ========================================

call dotnet restore SmokeScreenEngineGUI.csproj
call dotnet build SmokeScreenEngineGUI.csproj --configuration Release

if %errorlevel% equ 0 (
    echo.
    echo [SUCCESS] Build Complete.
    echo EXE Location: bin\Release\net8.0-windows\SmokeScreenEngine.exe
    echo.
    set /p launch="Launch ENGINE now? (y/n): "
    if /i "%launch%"=="y" start "" "bin\Release\net8.0-windows\SmokeScreenEngine.exe"
) else (
    echo.
    echo [ERROR] Build failed. Make sure you have the .NET 8 SDK installed.
    pause
)
