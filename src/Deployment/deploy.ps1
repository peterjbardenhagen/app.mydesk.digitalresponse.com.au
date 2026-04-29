param(
    [string]$SiteName = "MyDeskV3",
    [string]$AppPoolName = "MyDeskV3",
    [string]$PublishPath = "",
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

$ROOT = Split-Path -Parent $PSScriptRoot
$SRC = Join-Path $ROOT "src"
$WEB = Join-Path $SRC "MyDesk.Web"
$DEPLOY = Join-Path $ROOT "Deployment\publish"

$APPCMD = "$env:SystemRoot\System32\inetsrv\appcmd.exe"

function Write-Step($msg) { Write-Host "[DEPLOY] $msg" -ForegroundColor Cyan }
function Write-Success($msg) { Write-Host "[OK] $msg" -ForegroundColor Green }
function Write-Error($msg) { Write-Host "[ERROR] $msg" -ForegroundColor Red }

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  MyDeskV3 IIS Deployment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

if (-not $PublishPath) { $PublishPath = $DEPLOY }

# Step 1: Build
Write-Step "Building application..."
Push-Location $WEB
try {
    dotnet publish -c Release -r win-x64 --self-contained -o $DEPLOY -p:PublishSingleFile=false
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
    & $APPCMD add apppool /name:$AppPoolName /managedRuntimeVersion:v4.0 /managedPipelineMode:Integrated
    & $APPCMD set apppool /name:$AppPoolName /processModel.identityType:ApplicationPoolIdentity
}

# Step 4: Create Site if needed
Write-Step "Checking Site..."
$site = & $APPCMD list site $SiteName 2>$null
if (-not $site) {
    Write-Step "Creating Site..."
    $port = 8080
    & $APPCMD add site /name:$SiteName /id:3 /physicalPath:$DEPLOY /bindings:"http/*:$port"
    & $APPCMD set apppool /name:$AppPoolName
    Write-Host "      Site created on port $port" -ForegroundColor Yellow
}

# Step 5: Set physical path
Write-Step "Updating physical path..."
& $APPCMD set vdir "$SiteName/" /physicalPath:$DEPLOY

# Step 6: Start AppPool
Write-Step "Starting Application Pool..."
& $APPCMD start apppool $AppPoolName

# Step 7: Start Site
Write-Step "Starting Site..."
& $APPCMD start site $SiteName

Write-Host ""
Write-Success "Deployment complete!"
Write-Host "      URL: http://localhost:8080" -ForegroundColor Yellow
Write-Host ""