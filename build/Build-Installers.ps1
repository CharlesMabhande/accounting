#Requires -Version 5.1
<#
.SYNOPSIS
  1) Publishes dist\AccountingInstaller\ (API, Desktop, CharlzTech.exe, scripts).
  2) Builds CharlzTech.msi (WiX) from that folder with product icon.
  3) Copies to dist\ and to the repository base folder (next to Accounting.sln):
     - CharlzTech.exe — graphical installer (CharlzTech logo as application icon)
     - CharlzTech.msi — Windows Installer (same icon in Programs and Features)

  Requires: .NET 8 SDK, WiX Toolset SDK (restored via NuGet when building the .wixproj).
#>
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

Write-Host "Step 1: Publish AccountingInstaller folder..." -ForegroundColor Cyan
& (Join-Path $PSScriptRoot 'Build-Setup.ps1') -Configuration $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$exeSrc = Join-Path $root 'dist\AccountingInstaller\CharlzTech.exe'
if (-not (Test-Path -LiteralPath $exeSrc)) {
    Write-Error "Expected installer not found: $exeSrc"
}

Write-Host ""
Write-Host "Step 2: Build MSI (WiX) -> CharlzTech.msi..." -ForegroundColor Cyan
$msiProj = Join-Path $PSScriptRoot 'Accounting.Msi\Accounting.Msi.wixproj'
dotnet build $msiProj -c $Configuration
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$msiBuilt = Join-Path $PSScriptRoot "Accounting.Msi\bin\$Configuration\CharlzTech.msi"
if (-not (Test-Path -LiteralPath $msiBuilt)) {
    Write-Error "MSI not found at $msiBuilt"
}

$dist = Join-Path $root 'dist'
New-Item -ItemType Directory -Force -Path $dist | Out-Null
$msiDist = Join-Path $dist 'CharlzTech.msi'
$exeDist = Join-Path $dist 'CharlzTech.exe'
Copy-Item -Force $msiBuilt $msiDist
Copy-Item -Force $exeSrc $exeDist

# Repository base folder (same directory as Accounting.sln)
$baseExe = Join-Path $root 'CharlzTech.exe'
$baseMsi = Join-Path $root 'CharlzTech.msi'
Copy-Item -Force $exeSrc $baseExe
Copy-Item -Force $msiBuilt $baseMsi

$installBatSrc = Join-Path $dist 'AccountingInstaller\INSTALL.bat'
if (Test-Path -LiteralPath $installBatSrc) {
    Copy-Item -Force $installBatSrc (Join-Path $dist 'INSTALL.bat')
}
$readmeFirst = Join-Path $dist 'AccountingInstaller\README-FIRST.txt'
if (Test-Path -LiteralPath $readmeFirst) {
    Copy-Item -Force $readmeFirst (Join-Path $dist 'README-FIRST.txt')
}

Write-Host ""
Write-Host "Done." -ForegroundColor Green
Write-Host "  CharlzTech.exe (wizard)     : $exeDist" -ForegroundColor Yellow
Write-Host "  CharlzTech.msi (installer): $msiDist" -ForegroundColor Yellow
Write-Host "  Same files in base folder   : $baseExe" -ForegroundColor Yellow
Write-Host "                              : $baseMsi" -ForegroundColor Yellow
Write-Host "  Full portable folder        : $(Join-Path $dist 'AccountingInstaller')" -ForegroundColor Yellow
