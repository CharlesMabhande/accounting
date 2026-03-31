@echo off
cd /d "%~dp0"
set ASPNETCORE_ENVIRONMENT=Production
start "" "%~dp0Accounting.Api.exe"
