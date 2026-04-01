#Requires -Version 5.1
<#
.SYNOPSIS
  Publishes Accounting.Api as a self-contained Windows x64 app (no .NET runtime install required on target PC).
  Output: ..\dist\AccountingApi\
#>
param(
    [string]$Configuration = 'Release',
    [string]$OutputPath = ''
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

if (-not $OutputPath) {
    $OutputPath = Join-Path $root 'dist\AccountingApi'
}

$proj = Join-Path $root 'src\Accounting.Api\Accounting.Api.csproj'

Write-Host "Publishing self-contained to $OutputPath ..." -ForegroundColor Cyan

dotnet publish $proj `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    /p:PublishTrimmed=false `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    -o $OutputPath

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$example = Join-Path $root 'install\appsettings.Local.json.example'
if (Test-Path $example) {
    Copy-Item -Force $example (Join-Path $OutputPath 'appsettings.Local.json.example')
}

$exampleSqlAuth = Join-Path $root 'install\appsettings.Local.SqlAuth.json.example'
if (Test-Path $exampleSqlAuth) {
    Copy-Item -Force $exampleSqlAuth (Join-Path $OutputPath 'appsettings.Local.SqlAuth.json.example')
}

$installScript = Join-Path $root 'install\Install-Accounting.ps1'
if (Test-Path $installScript) {
    Copy-Item -Force $installScript (Join-Path $OutputPath 'Install-Accounting.ps1')
}

$dbScript = Join-Path $root 'install\Install-SqlServerDatabase.ps1'
if (Test-Path $dbScript) {
    Copy-Item -Force $dbScript (Join-Path $OutputPath 'Install-SqlServerDatabase.ps1')
}

$evolutionTxt = Join-Path $root 'install\Evolution-Parity.txt'
if (Test-Path $evolutionTxt) {
    Copy-Item -Force $evolutionTxt (Join-Path $OutputPath 'Evolution-Parity.txt')
}

$runCmd = Join-Path $root 'install\Run-Accounting.cmd'
if (Test-Path $runCmd) {
    Copy-Item -Force $runCmd (Join-Path $OutputPath 'Run-Accounting.cmd')
}

$installBat = Join-Path $root 'install\INSTALL.bat'
if (Test-Path $installBat) {
    Copy-Item -Force $installBat (Join-Path $OutputPath 'INSTALL.bat')
}
$readmeFirst = Join-Path $root 'install\README-FIRST.txt'
if (Test-Path $readmeFirst) {
    Copy-Item -Force $readmeFirst (Join-Path $OutputPath 'README-FIRST.txt')
}

$uninstallCmd = Join-Path $root 'install\Uninstall-Accounting.cmd'
if (Test-Path $uninstallCmd) {
    Copy-Item -Force $uninstallCmd (Join-Path $OutputPath 'Uninstall-Accounting.cmd')
}
$uninstallPs1 = Join-Path $root 'install\Uninstall-Accounting.ps1'
if (Test-Path $uninstallPs1) {
    Copy-Item -Force $uninstallPs1 (Join-Path $OutputPath 'Uninstall-Accounting.ps1')
}

$readme = Join-Path $root 'install\README.txt'
if (Test-Path $readme) {
    Copy-Item -Force $readme (Join-Path $OutputPath 'README.txt')
}

$setupBat = Join-Path $root 'install\SETUP.bat'
if (Test-Path $setupBat) {
    Copy-Item -Force $setupBat (Join-Path $OutputPath 'SETUP.bat')
}

$redistTxt = Join-Path $root 'install\REDISTRIBUTABLE.txt'
if (Test-Path $redistTxt) {
    Copy-Item -Force $redistTxt (Join-Path $OutputPath 'REDISTRIBUTABLE.txt')
}

$ssmsTxt = Join-Path $root 'install\Connect-with-SSMS.txt'
if (Test-Path $ssmsTxt) {
    Copy-Item -Force $ssmsTxt (Join-Path $OutputPath 'Connect-with-SSMS.txt')
}

$sqlFolder = Join-Path $root 'install\sql'
if (Test-Path $sqlFolder) {
    $destSql = Join-Path $OutputPath 'sql'
    New-Item -ItemType Directory -Force -Path $destSql | Out-Null
    Copy-Item -Force (Join-Path $sqlFolder '*') -Destination $destSql
}

Write-Host "Done. Default: SQL Server (SSMS — see Connect-with-SSMS.txt). Optional: Provider Sqlite." -ForegroundColor Green
Write-Host "Copy dist\AccountingApi or run build\Build-Redistributable.ps1 for a zip. Offline OK." -ForegroundColor Yellow
