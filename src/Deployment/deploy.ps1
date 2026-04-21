# Techlight MyDesk - Blazor Server Deployment Script
# ===================================================
param([string]$Environment = "Production")

# ── Check elevation ─────────────────────────────────────────────────────────
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (!$isAdmin) {
    Write-Host "ERROR: This script must run as Administrator to manage IIS." -ForegroundColor Red
    Write-Host "Right-click PowerShell → 'Run as Administrator', then re-run:" -ForegroundColor Yellow
    Write-Host "  cd '$PSScriptRoot'" -ForegroundColor Gray
    Write-Host "  .\deploy.ps1" -ForegroundColor Gray
    exit 1
}

Write-Host "`nTechlight MyDesk - Deploy`n" -ForegroundColor Cyan

# ── Paths ───────────────────────────────────────────────────────────────────
$src    = Join-Path $PSScriptRoot "..\MyDesk.Web"
$pub    = Join-Path $PSScriptRoot "publish"
$site   = "Techlight.MyDesk"
$path   = "C:\inetpub\wwwroot\Techlight.MyDesk"
$appcmd = "$env:SystemRoot\System32\inetsrv\appcmd.exe"

# ── Build ───────────────────────────────────────────────────────────────────
Write-Host "  Building $Environment..." -NoNewline
$build = dotnet publish $src -c $Environment -o $pub --nologo 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host " FAILED" -ForegroundColor Red
    $build | Write-Host
    exit 1
}
Write-Host " OK" -ForegroundColor Green

# ── IIS (using appcmd - works in all PowerShell versions) ──────────────────
Write-Host "  IIS..." -NoNewline

# App Pool
& $appcmd list apppool /name:$site | Out-Null
if ($LASTEXITCODE -ne 0) {
    & $appcmd add apppool /name:$site /managedRuntimeVersion:"" | Out-Null
}

# Site
& $appcmd list site /name:$site | Out-Null
if ($LASTEXITCODE -ne 0) {
    & $appcmd add site /name:$site /physicalPath:$path /bindings:http/*:80: | Out-Null
    & $appcmd set site /site.name:$site /[path='/'].applicationPool:$site | Out-Null
} else {
    # Ensure path and pool updated
    & $appcmd set site /site.name:$site /[path='/'].physicalPath:$path | Out-Null
    & $appcmd set site /site.name:$site /[path='/'].applicationPool:$site | Out-Null
}

Write-Host " OK" -ForegroundColor Green

# ── Deploy ─────────────────────────────────────────────────────────────────
Write-Host "  Deploy..." -NoNewline

# Stop pool
& $appcmd stop apppool /apppool.name:$site | Out-Null

# Ensure directory exists
if (!(Test-Path $path)) {
    New-Item -ItemType Directory -Path $path -Force | Out-Null
}

# Robocopy mirror (exclude Logs to preserve them)
& robocopy $pub $path /MIR /XD Logs /NJH /NJS /NP | Out-Null

# Start pool
& $appcmd start apppool /apppool.name:$site | Out-Null

Write-Host " OK" -ForegroundColor Green

Write-Host "`nReady: http://localhost`n" -ForegroundColor Cyan
exit 0
