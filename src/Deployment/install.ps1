# DR MyDesk - Install & Setup (Self-Elevating)
# =============================================
# This script self-elevates to Administrator and deploys MyDesk to IIS on port 80.
# Usage: .\install.ps1

# Self-elevate if not running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

if (-not $isAdmin) {
    Write-Host "Requesting Administrator privileges..." -ForegroundColor Yellow
    $arguments = "-NoProfile -ExecutionPolicy Bypass -File `"$PSCommandPath`""
    Start-Process powershell -Verb RunAs -ArgumentList $arguments
    exit
}

# Running as Administrator now
$ErrorActionPreference = "Stop"
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $scriptDir

Write-Host ""
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "  DR MyDesk - Install & Setup" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""

# 1. Verify IIS is installed
Write-Host "[1/6] Checking IIS..." -NoNewline
$appCmd = "$env:windir\system32\inetsrv\appcmd.exe"
if (-not (Test-Path $appCmd)) {
    Write-Host " FAIL" -ForegroundColor Red
    Write-Host ""
    Write-Host "IIS is not installed. Installing IIS features..." -ForegroundColor Yellow
    
    $features = @(
        "IIS-WebServerRole",
        "IIS-WebServer",
        "IIS-CommonHttpFeatures",
        "IIS-HttpErrors",
        "IIS-HttpRedirect",
        "IIS-ApplicationDevelopment",
        "IIS-NetFxExtensibility45",
        "IIS-HealthAndDiagnostics",
        "IIS-HttpLogging",
        "IIS-Security",
        "IIS-RequestFiltering",
        "IIS-Performance",
        "IIS-WebServerManagementTools",
        "IIS-ManagementConsole",
        "IIS-IIS6ManagementCompatibility",
        "IIS-Metabase",
        "IIS-StaticContent",
        "IIS-DefaultDocument",
        "IIS-DirectoryBrowsing"
    )
    
    foreach ($feature in $features) {
        Enable-WindowsOptionalFeature -Online -FeatureName $feature -NoRestart -ErrorAction SilentlyContinue | Out-Null
    }
    Write-Host "      IIS features installed" -ForegroundColor Green
} else {
    Write-Host " OK" -ForegroundColor Green
}

# 2. Check .NET 8 Hosting Bundle
Write-Host "[2/6] Checking .NET 8 Hosting Bundle..." -NoNewline
$hostingBundle = Get-ChildItem "HKLM:\SOFTWARE\WOW6432Node\Microsoft\Updates\.NET Core\" -ErrorAction SilentlyContinue | 
                 Where-Object { $_.Name -match "Microsoft .NET.*Windows Server Hosting" }
if (-not $hostingBundle) {
    Write-Host " MISSING" -ForegroundColor Yellow
    Write-Host "      Download: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    Write-Host "      Install 'ASP.NET Core Runtime 8.0.x - Windows Hosting Bundle'" -ForegroundColor Yellow
} else {
    Write-Host " OK" -ForegroundColor Green
}

# 3. Build the application
Write-Host "[3/6] Building application..." -NoNewline
$projectPath = Join-Path $scriptDir "..\MyDesk.Web"
$publishPath = Join-Path $scriptDir "publish"

if (Test-Path $publishPath) {
    Remove-Item $publishPath -Recurse -Force
}

$buildOutput = & dotnet publish $projectPath -c Release -o $publishPath --nologo 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host " FAIL" -ForegroundColor Red
    Write-Host $buildOutput -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host " OK" -ForegroundColor Green

# 4. Stop existing site/pool if running
Write-Host "[4/6] Preparing IIS..." -NoNewline
$siteName = "MyDesk"
$appPoolName = "MyDesk"
$physicalPath = "C:\inetpub\wwwroot\MyDesk"

# Stop and remove default site on port 80 if it exists and conflicts
$defaultSite = & $appCmd list site "Default Web Site" 2>$null
if ($defaultSite -match "Default Web Site") {
    & $appCmd stop site /site.name:"Default Web Site" 2>$null | Out-Null
}

# Remove existing MyDesk site/pool
$existingSite = & $appCmd list site $siteName 2>$null
if ($existingSite -match $siteName) {
    & $appCmd stop site /site.name:$siteName 2>$null | Out-Null
    & $appCmd delete site $siteName 2>$null | Out-Null
}

$existingPool = & $appCmd list apppool $appPoolName 2>$null
if ($existingPool -match $appPoolName) {
    & $appCmd stop apppool /apppool.name:$appPoolName 2>$null | Out-Null
    & $appCmd delete apppool $appPoolName 2>$null | Out-Null
}

Start-Sleep -Seconds 2
Write-Host " OK" -ForegroundColor Green

# 5. Deploy files
Write-Host "[5/6] Deploying files to $physicalPath..." -NoNewline
if (-not (Test-Path $physicalPath)) {
    New-Item -ItemType Directory -Path $physicalPath -Force | Out-Null
}
# Clear existing files (except Logs)
Get-ChildItem $physicalPath -Exclude "Logs" | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
# Copy new files
robocopy $publishPath $physicalPath /E /NJH /NJS /NP /XD Logs | Out-Null

# Grant IIS_IUSRS permissions
$acl = Get-Acl $physicalPath
$permission = "IIS_IUSRS", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
$acl.SetAccessRule($accessRule)
Set-Acl $physicalPath $acl

Write-Host " OK" -ForegroundColor Green

# 6. Create and start IIS site
Write-Host "[6/6] Creating IIS site..." -NoNewline

# Create app pool (No Managed Code for .NET Core)
& $appCmd add apppool /name:$appPoolName /managedRuntimeVersion:"" | Out-Null
& $appCmd set apppool $appPoolName /startMode:AlwaysRunning | Out-Null
& $appCmd set apppool $appPoolName /processModel.identityType:ApplicationPoolIdentity | Out-Null

# Create site on port 80
& $appCmd add site /name:$siteName /physicalPath:$physicalPath /bindings:"http/*:80:" | Out-Null
& $appCmd set site /site.name:$siteName /[path='/'].applicationPool:$appPoolName | Out-Null

# Start services
& $appCmd start apppool /apppool.name:$appPoolName 2>$null | Out-Null
& $appCmd start site /site.name:$siteName 2>$null | Out-Null

Write-Host " OK" -ForegroundColor Green

# Summary
Write-Host ""
Write-Host "================================================================================" -ForegroundColor Green
Write-Host "  DEPLOYMENT SUCCESSFUL!" -ForegroundColor Green
Write-Host "================================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "  URL:       http://localhost" -ForegroundColor White
Write-Host "  Site:      $siteName" -ForegroundColor White
Write-Host "  App Pool:  $appPoolName" -ForegroundColor White
Write-Host "  Path:      $physicalPath" -ForegroundColor White
Write-Host ""
Write-Host "================================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Opening browser..." -ForegroundColor Yellow
Start-Sleep -Seconds 2
Start-Process "http://localhost"

Write-Host ""
Read-Host "Press Enter to close this window"
