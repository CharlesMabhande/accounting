#Requires -Version 5.1
<#
.SYNOPSIS
  Creates an empty SQL Server database (same step the API runs on first start).
  Supports Windows Authentication or SQL Server authentication (login + password).

.PARAMETER Server
  SQL Server instance, e.g. (localdb)\mssqllocaldb, .\SQLEXPRESS, or tcp:server.database.windows.net,1433

.PARAMETER Database
  Database name (letters, digits, underscore only).

.PARAMETER UserName
  SQL login (SQL Server authentication). Omit for Windows Authentication (-E).

.PARAMETER Password
  Password for SQL login. Avoid ; and = in passwords, or use -PasswordFile.

.PARAMETER PasswordFile
  Path to a UTF-8 text file whose first line is the password (trimmed). Safer for scripted installs than -Password in command history.

.EXAMPLE
  .\Install-SqlServerDatabase.ps1 -Server '(localdb)\mssqllocaldb' -Database AccountingDb

.EXAMPLE
  .\Install-SqlServerDatabase.ps1 -Server '.\SQLEXPRESS' -Database AccountingDb -UserName 'sa' -Password 'YourStrong!Pass'
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Server,
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[A-Za-z0-9_]+$')]
    [string]$Database,
    [string]$UserName = '',
    [string]$Password = '',
    [string]$PasswordFile = ''
)

$ErrorActionPreference = 'Stop'

function Test-Command($Name) {
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

if ($PasswordFile) {
    if (-not (Test-Path -LiteralPath $PasswordFile)) {
        throw "PasswordFile not found: $PasswordFile"
    }
    $Password = (Get-Content -LiteralPath $PasswordFile -Raw -Encoding UTF8).Split([Environment]::NewLine, 2)[0].Trim()
}

$useSqlAuth = $false
if ($UserName) {
    $useSqlAuth = $true
    if ([string]::IsNullOrWhiteSpace($Password)) {
        throw "SQL authentication requires -Password or -PasswordFile with a non-empty first line when -UserName is set."
    }
}

if ($Server -match 'localdb' -and (Test-Command 'sqllocaldb')) {
    Write-Host "Starting SQL Server LocalDB instance..." -ForegroundColor Cyan
    try { sqllocaldb start 'mssqllocaldb' 2>$null | Out-Null } catch { }
}

if (-not (Test-Command 'sqlcmd')) {
    throw "sqlcmd not found. Install SQL Server Command Line Tools, SQL Server Express (includes sqlcmd), or SQL Server Management Studio (SSMS includes sqlcmd). Alternatively skip this script — Accounting.Api creates the database on first run if your login has permission."
}

$escaped = $Database.Replace(']', ']]')
$literal = $Database.Replace("'", "''")
$sql = @"
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'$literal')
    CREATE DATABASE [$escaped];
"@

if ($useSqlAuth) {
    $sqlcmdArgs = @('-S', $Server, '-b', '-U', $UserName, '-P', $Password, '-Q', $sql)
}
else {
    $sqlcmdArgs = @('-S', $Server, '-b', '-E', '-Q', $sql)
}

Write-Host "Creating database [$Database] on $Server (if missing)..." -ForegroundColor Green
if ($useSqlAuth) {
    Write-Host "Using SQL Server authentication (user: $UserName)." -ForegroundColor DarkGray
}
else {
    Write-Host "Using Windows authentication." -ForegroundColor DarkGray
}

& sqlcmd @sqlcmdArgs
if ($LASTEXITCODE -ne 0) {
    throw "sqlcmd failed with exit code $LASTEXITCODE"
}
Write-Host "Done. Start Accounting.Api to create tables (EF EnsureCreated) and seed data." -ForegroundColor Green
