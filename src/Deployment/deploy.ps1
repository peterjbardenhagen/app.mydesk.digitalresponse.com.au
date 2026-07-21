param(
    [string]$SiteName = "MyDesk",
    [string]$AppPoolName = "MyDesk",
    [string]$PublishPath = "",
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

# Determine root path - works whether running from repo root or from Deployment folder
if ((Get-Item $PSScriptRoot).Name -eq "Deployment") {
    $ROOT = Split-Path -Parent $PSScriptRoot
} else {
    $ROOT = $PSScriptRoot
}

$SRC = Join-Path $ROOT "src"
$WEB = Join-Path $SRC "MyDesk.Web"
$DEPLOY = if ($PublishPath) { $PublishPath } else { Join-Path $ROOT "artifacts\publish" }

# Fallback: if publish path doesn't exist, try relative to deployment script
if (-not (Test-Path $DEPLOY)) {
    $DEPLOY = Join-Path $PSScriptRoot "publish"
}

$APPCMD = "$env:SystemRoot\System32\inetsrv\appcmd.exe"

function Write-Step($msg) { Write-Host "[DEPLOY] $msg" -ForegroundColor Cyan }
function Write-Success($msg) { Write-Host "[OK] $msg" -ForegroundColor Green }
function Write-Error($msg) { Write-Host "[ERROR] $msg" -ForegroundColor Red }

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  MyDesk IIS Deployment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (-not $PublishPath) { $PublishPath = $DEPLOY }

# Stop IIS and kill w3wp to release locks
Write-Step "Stopping IIS and releasing file locks..."
try { iisreset /stop } catch {}
Get-Process w3wp -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3

# Step1: Build
Write-Step "Building application..."
Push-Location $WEB
try {
    dotnet publish -c Release -r win-x64 --self-contained false -o $DEPLOY
    if ($LASTEXITCODE -ne 0) { throw "Build failed" }
    Write-Success "Build complete"
}
finally { Pop-Location }

# Step 2: Stop AppPool
Write-Step "Stopping Application Pool..."
& $APPCMD stop apppool $AppPoolName 2>$null

# Step 3: Create AppPool if needed
Write-Step "Checking Application Pool..."
$pool = & $APPCMD list apppool $AppPoolName 2>$null
if (-not $pool) {
    Write-Step "Creating Application Pool..."
    & $APPCMD add apppool /name:$AppPoolName /managedRuntimeVersion:""
    & $APPCMD set apppool /name:$AppPoolName /processModel.identityType:ApplicationPoolIdentity
}

# Step 4: Create Site if needed
Write-Step "Checking Site..."
$site = & $APPCMD list site $SiteName 2>$null
if (-not $site) {
    Write-Step "Creating Site..."
    & $APPCMD add site /name:$SiteName /id:3 /physicalPath:$DEPLOY /bindings:"http/*:80:"
    & $APPCMD set app "$SiteName/" /applicationPool:$AppPoolName
    Write-Host "      Site created on port 80" -ForegroundColor Yellow
}

# Step 5: Set physical path
Write-Step "Updating physical path..."
& $APPCMD set vdir "$SiteName/" /physicalPath:$DEPLOY

# Step 6: Set permissions
Write-Step "Setting folder permissions..."
$acl = Get-Acl $DEPLOY
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($rule)
Set-Acl $DEPLOY $acl

# Step 7: Start AppPool
Write-Step "Starting Application Pool..."
& $APPCMD start apppool $AppPoolName

# Step 8: Start Site
Write-Step "Starting Site..."
& $APPCMD start site $SiteName

# Start IIS
iisreset /start

Write-Host ""
Write-Success "Deployment complete!"
Write-Host "      URL: http://$SiteName" -ForegroundColor Yellow
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  MyDesk IIS Deployment Complete" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""