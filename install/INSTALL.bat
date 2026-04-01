@echo off
REM One-click: launches CharlzTech Accounting setup. Paste your SSMS connection string when asked.
title CharlzTech Accounting — Install
cd /d "%~dp0"
if not exist "%~dp0CharlzTech.exe" (
  echo ERROR: CharlzTech.exe not found in this folder.
  echo Use the full AccountingInstaller folder from the build ^(contains CharlzTech.exe, Accounting.Api.exe, Desktop\, scripts^).
  pause
  exit /b 1
)
start "" "%~dp0CharlzTech.exe"
