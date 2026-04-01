#Requires -Version 5.1
<#
.SYNOPSIS
  Removes CharlzTech Accounting from Programs and Features, startup shortcut, and optionally the install folder.
  Must stay in sync with InstallRegistration.UninstallRegistryKeyRelative in Accounting.Setup.
#>
param(
    [string]$InstallPath = '',
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'

$registryPath = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\{B7F3A1C2-4E8D-4F9A-8B1E-6C2D3E4F5A0B}'

if (-not $InstallPath) {
    if ($PSScriptRoot) {
        $InstallPath = $PSScriptRoot
    }
    else {
        $InstallPath = (Get-Location).Path
    }
}

try {
    $InstallPath = (Resolve-Path -LiteralPath $InstallPath).Path.TrimEnd('\')
}
catch {
    Write-Error "Install path not found: $InstallPath"
    exit 1
}

if (-not $Quiet) {
    Write-Host "Uninstalling CharlzTech Accounting from:" -ForegroundColor Cyan
    Write-Host "  $InstallPath" -ForegroundColor Gray
}

try {
    if (Test-Path -LiteralPath $registryPath) {
        Remove-Item -LiteralPath $registryPath -Recurse -Force
        if (-not $Quiet) { Write-Host "Removed Programs and Features registration." -ForegroundColor Green }
    }
}
catch {
    if (-not $Quiet) { Write-Warning "Registry: $_" }
}

$startup = [Environment]::GetFolderPath('Startup')
$lnk = Join-Path $startup 'Accounting API.lnk'
try {
    if (Test-Path -LiteralPath $lnk) {
        Remove-Item -LiteralPath $lnk -Force
        if (-not $Quiet) { Write-Host "Removed startup shortcut." -ForegroundColor Green }
    }
}
catch {
    if (-not $Quiet) { Write-Warning "Startup shortcut: $_" }
}

if ($Quiet) { return }

$delete = Read-Host "Delete application files under this folder? (y/N)"
if ($delete -match '^[yY]') {
    try {
        Remove-Item -LiteralPath $InstallPath -Recurse -Force
        Write-Host "Removed: $InstallPath" -ForegroundColor Green
    }
    catch {
        Write-Warning "Could not remove all files (close Accounting and retry as Administrator): $_"
    }
}
else {
    Write-Host "Application files were left in place." -ForegroundColor Yellow
}

Write-Host "Done." -ForegroundColor Cyan
