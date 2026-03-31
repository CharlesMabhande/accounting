@echo off
setlocal EnableDelayedExpansion
cd /d "%~dp0"

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
  echo ERROR: .NET 8 SDK required for building the offline bundle.
  pause
  exit /b 1
)

echo [1/2] Optional: restore packages for offline build (run once online^)
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build\Prepare-Offline.ps1"
if errorlevel 1 exit /b 1

echo.
echo [2/2] Building offline bundle (AccountingOfflineSetup.exe + payload^)...
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build\Build-OfflineBundle.ps1" -Configuration Release
if errorlevel 1 (
  echo Build failed.
  pause
  exit /b 1
)

echo.
echo SUCCESS
echo   dist\AccountingOffline\AccountingOfflineSetup.exe  — run on target PC (no Internet^)
echo   dist\Accounting-Offline-Bundle.zip                    — copy to USB
echo   Put VC++ / SQL LocalDB installers in dist\AccountingOffline\redist\ if needed
echo.
pause
