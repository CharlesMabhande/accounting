#Requires -Version 5.1
<#
.SYNOPSIS
  Writes appsettings.Local.json for portable (SQLite) or SQL Server deployment.

  DEFAULT: SQL Server (use SSMS on the same connection — see Connect-with-SSMS.txt)

  SQL Server authentication: pass -SqlUser and -SqlPassword (or -SqlPasswordFile).

  Portable option: -Provider Sqlite (single file, SSMS does not apply)

  Run from the folder that contains Accounting.Api.exe (e.g. dist\AccountingApi\)

  Self-contained publish: no .NET install on the target PC.
  Run PowerShell as Administrator if you want the firewall rule; otherwise use -SkipFirewall.

  -SkipSqlProbe: do not run sqlcmd to test the SQL Server connection.
  -AutoInstallSsms: if SSMS is missing, attempt winget install Microsoft.SQLServerManagementStudio (optional).
#>
param(
    [string]$ApiPath = '',
    [ValidateSet('Sqlite', 'SqlServer')]
    [string]$Provider = 'SqlServer',
    [string]$SqliteDataPath = 'Data/Accounting.db',
    [string]$Server = '(localdb)\mssqllocaldb',
    [string]$Database = 'AccountingDb',
    [string]$SqlUser = '',
    [string]$SqlPassword = '',
    [string]$SqlPasswordFile = '',
    [int]$HttpPort = 8080,
    [switch]$SkipFirewall,
    [switch]$Launch,
    [switch]$CreateDatabase,
    [switch]$SkipSqlProbe,
    [switch]$AutoInstallSsms
)

$ErrorActionPreference = 'Stop'

function Test-Command($Name) {
    return [bool](Get-Command $Name -ErrorAction SilentlyContinue)
}

function Test-SsmsInstalled {
    $paths = @(
        "${env:ProgramFiles(x86)}\Microsoft SQL Server Management Studio 20\Common7\IDE\Ssms.exe",
        "${env:ProgramFiles}\Microsoft SQL Server Management Studio 20\Common7\IDE\Ssms.exe",
        "${env:ProgramFiles(x86)}\Microsoft SQL Server Management Studio 19\Common7\IDE\Ssms.exe",
        "${env:ProgramFiles}\Microsoft SQL Server Management Studio 19\Common7\IDE\Ssms.exe"
    )
    foreach ($p in $paths) {
        if (Test-Path -LiteralPath $p) { return $true }
    }
    return $false
}

function Test-SqlServerConnection {
    param(
        [string]$Server,
        [string]$SqlUser,
        [string]$SqlPassword
    )
    if (-not (Test-Command 'sqlcmd')) {
        Write-Warning "sqlcmd not found — cannot verify SQL Server connectivity. Install SQL Server (Express), SSMS, or SQL CMD tools; then re-run or use -CreateDatabase."
        return $false
    }
    $probe = "SELECT 1"
    if ($SqlUser) {
        $sqlcmdArgs = @('-S', $Server, '-b', '-U', $SqlUser, '-P', $SqlPassword, '-Q', $probe)
    }
    else {
        $sqlcmdArgs = @('-S', $Server, '-b', '-E', '-Q', $probe)
    }
    & sqlcmd @sqlcmdArgs 2>$null | Out-Null
    return ($LASTEXITCODE -eq 0)
}

function Install-SsmsWithWinget {
    if (-not (Test-Command 'winget')) {
        Write-Warning "winget not found. Install App Installer from the Microsoft Store to use -AutoInstallSsms, or install SSMS manually."
        return
    }
    Write-Host "Installing SQL Server Management Studio via winget (Microsoft.SQLServerManagementStudio)..." -ForegroundColor Cyan
    & winget install --id Microsoft.SQLServerManagementStudio --accept-package-agreements --accept-source-agreements
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "winget SSMS install exited with code $LASTEXITCODE"
    }
}

function Escape-DbConnectionStringValue([string]$value) {
    if ([string]::IsNullOrEmpty($value)) { return '""' }
    if ($value -match '[;=]' -or $value.Contains('"')) {
        return '"' + ($value.Replace('"', '""')) + '"'
    }
    return $value
}

function Build-SqlServerConnectionString {
    param(
        [string]$Server,
        [string]$Database,
        [string]$SqlUser,
        [string]$SqlPassword
    )
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.Append("Server=").Append((Escape-DbConnectionStringValue $Server)).Append(';')
    [void]$sb.Append("Database=").Append((Escape-DbConnectionStringValue $Database)).Append(';')
    if ($SqlUser) {
        [void]$sb.Append("User Id=").Append((Escape-DbConnectionStringValue $SqlUser)).Append(';')
        [void]$sb.Append("Password=").Append((Escape-DbConnectionStringValue $SqlPassword)).Append(';')
    }
    else {
        [void]$sb.Append("Trusted_Connection=True;")
    }
    [void]$sb.Append("TrustServerCertificate=True;MultipleActiveResultSets=true")
    return $sb.ToString()
}

if ($SqlPasswordFile) {
    if (-not (Test-Path -LiteralPath $SqlPasswordFile)) {
        Write-Error "SqlPasswordFile not found: $SqlPasswordFile"
    }
    $SqlPassword = (Get-Content -LiteralPath $SqlPasswordFile -Raw -Encoding UTF8).Split([Environment]::NewLine, 2)[0].Trim()
}

if ($SqlUser -and -not $SqlPassword) {
    Write-Error "SqlUser is set but no SqlPassword or SqlPasswordFile was provided."
}

if (-not $ApiPath) {
    if ($PSScriptRoot -and (Test-Path (Join-Path $PSScriptRoot 'Accounting.Api.exe'))) {
        $ApiPath = $PSScriptRoot
    }
    elseif (Test-Path (Join-Path (Get-Location) 'Accounting.Api.exe')) {
        $ApiPath = (Get-Location).Path
    }
    else {
        $candidate = Join-Path (Split-Path $PSScriptRoot -Parent) 'dist\AccountingApi'
        if (Test-Path (Join-Path $candidate 'Accounting.Api.exe')) {
            $ApiPath = $candidate
        }
    }
}

if (-not $ApiPath) {
    Write-Error "Could not resolve the API folder. Pass -ApiPath to the folder that contains Accounting.Api.exe."
}

$exe = Join-Path $ApiPath 'Accounting.Api.exe'
if (-not (Test-Path $exe)) {
    Write-Error "Accounting.Api.exe not found in '$ApiPath'. Run build\Publish-SelfContained.ps1 first, then run this script from dist\AccountingApi or pass -ApiPath."
}

$localJson = Join-Path $ApiPath 'appsettings.Local.json'

if ($Provider -eq 'SqlServer') {
    if ($Server -match 'localdb' -and (Test-Command 'sqllocaldb')) {
        Write-Host "Ensuring SQL Server LocalDB is running..." -ForegroundColor Cyan
        try {
            sqllocaldb start 'mssqllocaldb' 2>$null | Out-Null
        } catch { }
    }

    if (-not $SkipSqlProbe) {
        Write-Host "Checking SQL Server connection to '$Server'..." -ForegroundColor Cyan
        if (-not (Test-SqlServerConnection -Server $Server -SqlUser $SqlUser -SqlPassword $SqlPassword)) {
            Write-Warning "Could not connect to SQL Server. Install SQL Server Express / LocalDB, fix the server name, or use SQL auth (-SqlUser/-SqlPassword). The API may still fail until the engine is reachable."
        }
        else {
            Write-Host "SQL Server connection OK." -ForegroundColor Green
        }
    }

    if (-not (Test-SsmsInstalled)) {
        Write-Host "SSMS (SQL Server Management Studio) is not detected in default install paths." -ForegroundColor Yellow
        Write-Host "  SSMS is optional for running the app but useful for inspecting AccountingDb. Download: https://aka.ms/ssmsfullsetup" -ForegroundColor DarkGray
        if ($AutoInstallSsms) {
            Install-SsmsWithWinget
        }
    }
    else {
        Write-Host "SSMS appears to be installed." -ForegroundColor Green
    }

    $conn = Build-SqlServerConnectionString -Server $Server -Database $Database -SqlUser $SqlUser -SqlPassword $SqlPassword
    $config = @{
        Api = @{
            EnableSwagger = $true
        }
        Database = @{
            Provider = 'SqlServer'
        }
        ConnectionStrings = @{
            DefaultConnection = $conn
        }
    }
    Write-Host "Using SQL Server provider (SQL Server + SSMS). Database is created on first API run, or use -CreateDatabase with sqlcmd." -ForegroundColor Green

    if ($CreateDatabase) {
        $dbScript = Join-Path $PSScriptRoot 'Install-SqlServerDatabase.ps1'
        if (Test-Path $dbScript) {
            Write-Host "Running Install-SqlServerDatabase.ps1 ..." -ForegroundColor Cyan
            if ($SqlUser) {
                if ($SqlPasswordFile) {
                    & $dbScript -Server $Server -Database $Database -UserName $SqlUser -PasswordFile $SqlPasswordFile
                }
                else {
                    & $dbScript -Server $Server -Database $Database -UserName $SqlUser -Password $SqlPassword
                }
            }
            else {
                & $dbScript -Server $Server -Database $Database
            }
        }
        else {
            Write-Warning "Install-SqlServerDatabase.ps1 not found next to this script; skipping -CreateDatabase."
        }
    }
}
else {
    $config = @{
        Api = @{
            EnableSwagger = $true
        }
        Database = @{
            Provider = 'Sqlite'
            SqlitePath = $SqliteDataPath
        }
    }
    Write-Host "Using SQLite (portable). No SQL Server required. Database file: $SqliteDataPath (next to the app folder)." -ForegroundColor Green
}

$json = $config | ConvertTo-Json -Depth 6
Set-Content -Path $localJson -Value $json -Encoding UTF8
Write-Host "Wrote $localJson" -ForegroundColor Green

if (-not $SkipFirewall) {
    $ruleName = "Accounting API (port $HttpPort)"
    try {
        $existing = Get-NetFirewallRule -DisplayName $ruleName -ErrorAction SilentlyContinue
        if (-not $existing) {
            Write-Host "Adding Windows Firewall rule: $ruleName" -ForegroundColor Cyan
            New-NetFirewallRule -DisplayName $ruleName -Direction Inbound -Action Allow -Protocol TCP -LocalPort $HttpPort | Out-Null
        } else {
            Write-Host "Firewall rule already exists: $ruleName" -ForegroundColor Yellow
        }
    } catch {
        Write-Host "Could not add firewall rule (run PowerShell as Administrator or use -SkipFirewall): $_" -ForegroundColor Yellow
    }
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Start the API (self-contained): double-click Run-Accounting.cmd or run Accounting.Api.exe"
Write-Host "  2. Open http://localhost:$HttpPort/swagger (Swagger is enabled in appsettings.Local.json)"
if ($Provider -eq 'SqlServer') {
    Write-Host ""
    Write-Host 'SSMS (SQL Server Management Studio) — use the same server and database as the app:' -ForegroundColor Cyan
    Write-Host ('  Server name:     ' + $Server)
    if ($SqlUser) {
        Write-Host '  Authentication:  SQL Server Authentication'
        Write-Host ('  Login:           ' + $SqlUser)
    }
    else {
        Write-Host '  Authentication:  Windows Authentication'
    }
    Write-Host ('  Database:        ' + $Database)
    Write-Host '  (After the API starts once, tables appear. See Connect-with-SSMS.txt.)'
    Write-Host ''
    Write-Host '  If connection fails: install SQL Server Express LocalDB or SQL Server, or install SSMS from Microsoft.'
    Write-Host '  LocalDB: run  sqllocaldb start mssqllocaldb  if the server name is (localdb)\mssqllocaldb'
}
Write-Host ""

if ($Launch) {
    $env:ASPNETCORE_ENVIRONMENT = 'Production'
    Start-Process -FilePath $exe -WorkingDirectory $ApiPath
}
