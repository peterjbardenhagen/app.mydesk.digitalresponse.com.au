# =============================================================================
# Techlight MyDesk - Blazor App Launcher
# =============================================================================
# Runs the Techlight.MyDesk.Web Blazor Server application.
#
# Usage:
#   .\Run.ps1              # Normal run
#   .\Run.ps1 -Watch       # Hot reload (rebuilds on file changes)
#   .\Run.ps1 -NoBrowser   # Do not auto-open the browser
# =============================================================================

[CmdletBinding()]
param(
    [switch]$Watch,
    [switch]$NoBrowser
)

$ErrorActionPreference = 'Stop'

$ScriptDir  = Split-Path -Parent $MyInvocation.MyCommand.Path
$WebProject = Join-Path $ScriptDir 'Techlight.MyDesk.Web'
$AppUrl     = 'http://localhost:5235'

function Write-Header($text) {
    Write-Host ''
    Write-Host ('=' * 70) -ForegroundColor Cyan
    Write-Host $text -ForegroundColor Cyan
    Write-Host ('=' * 70) -ForegroundColor Cyan
}

Write-Header 'Techlight MyDesk - Blazor Server'

# --- Pre-flight checks -------------------------------------------------------

Write-Host "[1/4] Checking .NET SDK..." -ForegroundColor Yellow
try {
    $dotnetVersion = (dotnet --version) 2>$null
    Write-Host "      .NET SDK $dotnetVersion detected" -ForegroundColor Green
} catch {
    Write-Host "      ERROR: .NET SDK not found. Install .NET 8 SDK from https://dotnet.microsoft.com/download" -ForegroundColor Red
    exit 1
}

Write-Host "[2/4] Checking LocalDB..." -ForegroundColor Yellow
try {
    $localDbInfo = (sqllocaldb info MSSQLLocalDB) 2>$null
    if ($localDbInfo -match 'State:\s+Stopped') {
        Write-Host "      LocalDB is stopped - starting it..." -ForegroundColor Yellow
        sqllocaldb start MSSQLLocalDB | Out-Null
    }
    Write-Host "      LocalDB (MSSQLLocalDB) is running" -ForegroundColor Green
} catch {
    Write-Host "      WARNING: LocalDB check failed. App may fail to connect to the database." -ForegroundColor Yellow
}

Write-Host "[3/4] Checking project..." -ForegroundColor Yellow
if (-not (Test-Path $WebProject)) {
    Write-Host "      ERROR: Web project not found at $WebProject" -ForegroundColor Red
    exit 1
}
Write-Host "      Project: $WebProject" -ForegroundColor Green

Write-Host "[4/4] Stopping any running instance..." -ForegroundColor Yellow
$running = Get-Process -Name 'Techlight.MyDesk.Web' -ErrorAction SilentlyContinue
if ($running) {
    Write-Host "      Found $($running.Count) running instance(s) - stopping..." -ForegroundColor Yellow
    $running | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Milliseconds 800
    Write-Host "      Stopped" -ForegroundColor Green
} else {
    Write-Host "      No running instance" -ForegroundColor Green
}

# Also kill anything bound to port 5235
$portOwners = Get-NetTCPConnection -LocalPort 5235 -ErrorAction SilentlyContinue |
    Select-Object -ExpandProperty OwningProcess -Unique
foreach ($procId in $portOwners) {
    try {
        $proc = Get-Process -Id $procId -ErrorAction Stop
        Write-Host "      Killing $($proc.ProcessName) (PID $procId) holding port 5235" -ForegroundColor Yellow
        Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
    } catch { }
}

# Wait briefly for file locks to release
$exePath = Join-Path $WebProject 'bin\Debug\net8.0\Techlight.MyDesk.Web.exe'
for ($i = 0; $i -lt 10; $i++) {
    if (-not (Test-Path $exePath)) { break }
    try {
        $fs = [System.IO.File]::Open($exePath, 'Open', 'Read', 'None')
        $fs.Close()
        break
    } catch {
        Start-Sleep -Milliseconds 300
    }
}

# --- Launch ------------------------------------------------------------------

Write-Header 'Starting application...'
Write-Host "URL:   $AppUrl" -ForegroundColor Green
Write-Host "Press Ctrl+C to stop" -ForegroundColor Green
Write-Host ''

if (-not $NoBrowser) {
    Start-Job -ScriptBlock {
        param($url)
        Start-Sleep -Seconds 5
        Start-Process $url
    } -ArgumentList $AppUrl | Out-Null
}

Push-Location $WebProject
try {
    if ($Watch) {
        dotnet watch run
    } else {
        dotnet run
    }
} finally {
    Pop-Location
}
