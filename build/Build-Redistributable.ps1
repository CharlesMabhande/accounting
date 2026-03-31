#Requires -Version 5.1
<#
.SYNOPSIS
  Builds the full installer (Setup.exe + API) and zips it: dist\Accounting-Installer.zip
#>
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

& (Join-Path $PSScriptRoot 'Build-Setup.ps1') -Configuration $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$dist = Join-Path $root 'dist\AccountingInstaller'
$zipPath = Join-Path $root 'dist\Accounting-Installer.zip'

if (Test-Path $zipPath) { Remove-Item -Force $zipPath }

Write-Host "Creating $zipPath ..." -ForegroundColor Cyan
Compress-Archive -Path $dist -DestinationPath $zipPath -Force

Write-Host "Done. Ship dist\Accounting-Installer.zip — extract and run Setup.exe" -ForegroundColor Green
