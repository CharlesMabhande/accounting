#Requires -Version 5.1
<#
.SYNOPSIS
  Builds a complete OFFLINE install bundle:
    dist\AccountingOffline\
      AccountingOfflineSetup.exe   (self-contained single-file wizard)
      AccountingInstaller\     (full API + Setup.exe + scripts)
      redist\README.txt        (what optional MSIs to add for VC++ / SQL LocalDB)

  Also creates dist\Accounting-Offline-Bundle.zip for USB / air-gapped transfer.

  Run Prepare-Offline.ps1 once on a machine WITH Internet to fill .\packages so
  dotnet restore in Build-Setup.ps1 works offline.
#>
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "Building AccountingInstaller (API + Setup.exe)..." -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'Build-Setup.ps1') -Configuration $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$bundleOut = Join-Path $root 'dist\AccountingOffline'
if (Test-Path $bundleOut) {
    Remove-Item -Recurse -Force $bundleOut
}
New-Item -ItemType Directory -Path $bundleOut | Out-Null

$bootstrapProj = Join-Path $root 'src\Accounting.Bootstrapper\Accounting.Bootstrapper.csproj'
Write-Host "Publishing AccountingOfflineSetup.exe..." -ForegroundColor Cyan
dotnet publish $bootstrapProj -c $Configuration -r win-x64 --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:PublishTrimmed=false `
    -o $bundleOut
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$installerSrc = Join-Path $root 'dist\AccountingInstaller'
$installerDest = Join-Path $bundleOut 'AccountingInstaller'
Copy-Item -Recurse -Force $installerSrc $installerDest

$redistDest = Join-Path $bundleOut 'redist'
New-Item -ItemType Directory -Path $redistDest -Force | Out-Null
Copy-Item (Join-Path $root 'install\OfflineDependencies.txt') (Join-Path $redistDest 'README.txt') -Force

$zipPath = Join-Path $root 'dist\Accounting-Offline-Bundle.zip'
if (Test-Path $zipPath) {
    Remove-Item -Force $zipPath
}
Write-Host "Creating $zipPath ..." -ForegroundColor Cyan
Compress-Archive -Path $bundleOut -DestinationPath $zipPath -Force

Write-Host ""
Write-Host "Done." -ForegroundColor Green
Write-Host "  Run offline: $($bundleOut)\AccountingOfflineSetup.exe" -ForegroundColor Yellow
Write-Host "  Or zip: $zipPath" -ForegroundColor Yellow
Write-Host "  Add optional MSIs to $($bundleOut)\redist\ (see README.txt there)" -ForegroundColor Yellow
