#Requires -Version 5.1
<#
.SYNOPSIS
  Builds the full installer folder: dist\AccountingInstaller\
  - Accounting.Api (self-contained)
  - Desktop\Accounting.Desktop.exe (self-contained WPF client)
  - CharlzTech.exe (self-contained WinForms installer, CharlzTech logo icon)

  Run from a machine with .NET 8 SDK installed.
#>
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

$out = Join-Path $root 'dist\AccountingInstaller'
# Do not delete the whole tree (files may be locked by a running API/Desktop). Publish overwrites in place.
New-Item -ItemType Directory -Path $out -Force | Out-Null
Write-Host "Output: $out (close Accounting.Api / Desktop if publish reports file locks)" -ForegroundColor DarkGray

$apiProj = Join-Path $root 'src\Accounting.Api\Accounting.Api.csproj'
$desktopProj = Join-Path $root 'src\Accounting.Desktop\Accounting.Desktop.csproj'
$setupProj = Join-Path $root 'src\Accounting.Setup\Accounting.Setup.csproj'

Write-Host "Publishing Accounting.Api..." -ForegroundColor Cyan
dotnet publish $apiProj -c $Configuration -r win-x64 --self-contained true `
    /p:PublishTrimmed=false `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    -o $out
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$desktopOut = Join-Path $out 'Desktop'
Write-Host "Publishing Accounting.Desktop to Desktop\ ..." -ForegroundColor Cyan
dotnet publish $desktopProj -c $Configuration -r win-x64 --self-contained true `
    /p:PublishTrimmed=false `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    -o $desktopOut
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$desktopAppsettings = Join-Path $desktopOut 'appsettings.json'
if (Test-Path $desktopAppsettings) {
    $j = Get-Content $desktopAppsettings -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($null -eq $j.AccountingApi) {
        $j | Add-Member -NotePropertyName AccountingApi -NotePropertyValue ([pscustomobject]@{ BaseUrl = 'http://127.0.0.1:8080' }) -Force
    }
    else {
        $j.AccountingApi.BaseUrl = 'http://127.0.0.1:8080'
    }
    ($j | ConvertTo-Json -Depth 10) + [Environment]::NewLine | Set-Content -Path $desktopAppsettings -Encoding utf8
}

Write-Host "Publishing CharlzTech.exe (setup installer)..." -ForegroundColor Cyan
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
    @{ Src = 'install\Uninstall-Accounting.cmd'; Dest = 'Uninstall-Accounting.cmd' },
    @{ Src = 'install\Uninstall-Accounting.ps1'; Dest = 'Uninstall-Accounting.ps1' },
    @{ Src = 'install\Run-Accounting-Desktop.cmd'; Dest = 'Run-Accounting-Desktop.cmd' },
    @{ Src = 'install\README.txt'; Dest = 'README.txt' },
    @{ Src = 'install\SETUP.bat'; Dest = 'SETUP.bat' },
    @{ Src = 'install\INSTALL.bat'; Dest = 'INSTALL.bat' },
    @{ Src = 'install\README-FIRST.txt'; Dest = 'README-FIRST.txt' },
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
Write-Host '  Run CharlzTech.exe (or INSTALL.bat) — full wizard; SQL SSMS connection string only.' -ForegroundColor Yellow
Write-Host '  Distribute the entire AccountingInstaller folder (or zip it).' -ForegroundColor Yellow
