#Requires -Version 5.1
<#
.SYNOPSIS
  Downloads all NuGet dependencies into .\packages (run once while online).
  After this, dotnet restore/build works without internet using the local folder.
#>
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "Restoring packages into $root\packages ..." -ForegroundColor Cyan
dotnet restore "$root\Accounting.sln" --verbosity minimal

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Done. Copy the entire repository including the 'packages' folder for offline installation." -ForegroundColor Green
