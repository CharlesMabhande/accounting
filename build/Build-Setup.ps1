#Requires -Version 5.1
<#
.SYNOPSIS
  Builds the full installer folder: dist\AccountingInstaller\
  - Accounting.Api (self-contained)
  - Setup.exe (self-contained WinForms installer)

  Run from a machine with .NET 8 SDK installed.
#>
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$out = Join-Path $root 'dist\AccountingInstaller'
if (Test-Path $out) {
    Remove-Item -Recurse -Force $out
}
New-Item -ItemType Directory -Path $out | Out-Null

$apiProj = Join-Path $root 'src\Accounting.Api\Accounting.Api.csproj'
$setupProj = Join-Path $root 'src\Accounting.Setup\Accounting.Setup.csproj'

Write-Host "Publishing Accounting.Api..." -ForegroundColor Cyan
dotnet publish $apiProj -c $Configuration -r win-x64 --self-contained true `
    /p:PublishTrimmed=false `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    -o $out
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

Write-Host "Publishing Setup.exe..." -ForegroundColor Cyan
dotnet publish $setupProj -c $Configuration -r win-x64 --self-contained true `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:PublishTrimmed=false `
    -o $out
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

# Copy installer assets (same as Publish-SelfContained.ps1)
$copyItems = @(
    @{ Src = 'install\appsettings.Local.json.example'; Dest = 'appsettings.Local.json.example' },
    @{ Src = 'install\appsettings.Local.SqlAuth.json.example'; Dest = 'appsettings.Local.SqlAuth.json.example' },
    @{ Src = 'install\Install-Accounting.ps1'; Dest = 'Install-Accounting.ps1' },
    @{ Src = 'install\Install-SqlServerDatabase.ps1'; Dest = 'Install-SqlServerDatabase.ps1' },
    @{ Src = 'install\Run-Accounting.cmd'; Dest = 'Run-Accounting.cmd' },
    @{ Src = 'install\README.txt'; Dest = 'README.txt' },
    @{ Src = 'install\SETUP.bat'; Dest = 'SETUP.bat' },
    @{ Src = 'install\REDISTRIBUTABLE.txt'; Dest = 'REDISTRIBUTABLE.txt' },
    @{ Src = 'install\Connect-with-SSMS.txt'; Dest = 'Connect-with-SSMS.txt' },
    @{ Src = 'install\Evolution-Parity.txt'; Dest = 'Evolution-Parity.txt' }
)
foreach ($item in $copyItems) {
    $s = Join-Path $root $item.Src
    if (Test-Path $s) {
        Copy-Item -Force $s (Join-Path $out $item.Dest)
    }
}

$sqlFolder = Join-Path $root 'install\sql'
if (Test-Path $sqlFolder) {
    $destSql = Join-Path $out 'sql'
    New-Item -ItemType Directory -Force -Path $destSql | Out-Null
    Copy-Item -Force (Join-Path $sqlFolder '*') -Destination $destSql
}

Write-Host ""
Write-Host "Done: $out" -ForegroundColor Green
Write-Host '  Run Setup.exe - it runs the PowerShell install and can start the API.' -ForegroundColor Yellow
Write-Host '  Distribute the entire AccountingInstaller folder (or zip it).' -ForegroundColor Yellow
