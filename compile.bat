@echo off
setlocal EnableDelayedExpansion
cd /d "%~dp0"

REM Prefer dotnet on PATH; else use default install location
where dotnet >nul 2>&1
if errorlevel 1 (
  if exist "%ProgramFiles%\dotnet\dotnet.exe" (
    set "PATH=%ProgramFiles%\dotnet;%PATH%"
  ) else if exist "%LocalAppData%\Microsoft\dotnet\dotnet.exe" (
    set "PATH=%LocalAppData%\Microsoft\dotnet;%PATH%"
  )
)

where dotnet >nul 2>&1
if errorlevel 1 (
  echo.
  echo ERROR: .NET SDK not found.
  echo Install ".NET 8 SDK" for Windows x64 from:
  echo   https://dotnet.microsoft.com/download/dotnet/8.0
  echo Then run this file again.
  echo.
  pause
  exit /b 1
)

echo dotnet version:
dotnet --version
echo.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build\Build-Setup.ps1" -Configuration Release
if errorlevel 1 (
  echo Build failed.
  pause
  exit /b 1
)

echo.
echo SUCCESS - Output folder:
echo   %~dp0dist\AccountingInstaller
echo.
echo Main executables:
echo   Setup.exe          - installer wizard
echo   Accounting.Api.exe - web API server
echo.
pause
