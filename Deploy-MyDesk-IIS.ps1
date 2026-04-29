# Complete IIS Deployment Script for MyDesk
# Run this script as Administrator

param(
    [string]$SourcePath = "C:\temp\MyDesk-publish",
    [string]$DestinationPath = "C:\inetpub\wwwroot\MyDesk",
    [string]$SiteName = "MyDesk",
    [string]$AppPoolName = "MyDesk"
)

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  MyDesk IIS Deployment Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Check if running as Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Host "ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

# Stop IIS and kill any w3wp processes
Write-Host "Stopping IIS and releasing file locks..." -ForegroundColor Yellow
try {
    iisreset /stop
} catch {
    Write-Host "  iisreset failed, trying alternative method..." -ForegroundColor Yellow
}

# Kill w3wp processes to release locks
Get-Process w3wp -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 3
Write-Host "  IIS stopped and locks released." -ForegroundColor Green

# Import WebAdministration module
Import-Module WebAdministration -ErrorAction Stop

# Publish if source doesn't exist
if (-not (Test-Path $SourcePath)) {
    Write-Host "Published files not found. Publishing..." -ForegroundColor Yellow
    dotnet publish "$PSScriptRoot\src\MyDesk.Web\MyDesk.Web.csproj" -c Release -o $SourcePath
    if (-not (Test-Path $SourcePath)) {
        Write-Host "ERROR: Publish failed!" -ForegroundColor Red
        exit 1
    }
    Write-Host "  Publish completed." -ForegroundColor Green
}

# Remove existing destination and recreate
Write-Host "Preparing destination folder..." -ForegroundColor Yellow
if (Test-Path $DestinationPath) {
    try {
        Remove-Item -Path $DestinationPath -Recurse -Force -ErrorAction Stop
        Write-Host "  Removed existing folder." -ForegroundColor Green
    } catch {
        Write-Host "  Could not remove folder, trying to clear contents..." -ForegroundColor Yellow
        Get-ChildItem -Path $DestinationPath -Recurse | Remove-Item -Force -Recurse -ErrorAction SilentlyContinue
    }
}

# Create fresh destination folder
New-Item -ItemType Directory -Path $DestinationPath -Force | Out-Null
Write-Host "  Destination folder created." -ForegroundColor Green

# Copy published files
Write-Host "Copying files to IIS folder..." -ForegroundColor Yellow
try {
    Copy-Item -Path "$SourcePath\*" -Destination $DestinationPath -Recurse -Force -ErrorAction Stop
    Write-Host "  Files copied successfully." -ForegroundColor Green
} catch {
    Write-Host "  ERROR copying files: $_" -ForegroundColor Red
    exit 1
}

# Set permissions on the folder
Write-Host "Setting folder permissions..." -ForegroundColor Yellow
$acl = Get-Acl $DestinationPath
$rule = New-Object System.Security.AccessControl.FileSystemAccessRule("IIS_IUSRS", "FullControl", "ContainerInherit,ObjectInherit", "None", "Allow")
$acl.SetAccessRule($rule)
Set-Acl $DestinationPath $acl
Write-Host "  Permissions set." -ForegroundColor Green

# Create or update Application Pool
Write-Host "Configuring Application Pool..." -ForegroundColor Yellow
if (Test-Path "IIS:\AppPools\$AppPoolName") {
    Write-Host "  Application Pool '$AppPoolName' already exists." -ForegroundColor Yellow
} else {
    New-WebAppPool -Name $AppPoolName -Force | Out-Null
    Write-Host "  Application Pool '$AppPoolName' created." -ForegroundColor Green
}

# Set app pool to No Managed Code (for .NET Core)
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name managedRuntimeVersion -Value ""
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name enable32BitAppOnWin64 -Value $false
Set-ItemProperty "IIS:\AppPools\$AppPoolName" -Name processModel.identityType -Value 4  # ApplicationPoolIdentity
Write-Host "  Application Pool configured." -ForegroundColor Green

# Create or update Website
Write-Host "Configuring Website..." -ForegroundColor Yellow
if (Test-Path "IIS:\Sites\$SiteName") {
    Write-Host "  Website '$SiteName' already exists. Updating..." -ForegroundColor Yellow
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name physicalPath -Value $DestinationPath
    Set-ItemProperty "IIS:\Sites\$SiteName" -Name applicationPool -Value $AppPoolName
} else {
    New-Website -Name $SiteName -PhysicalPath $DestinationPath -ApplicationPool $AppPoolName -Port 80 -Force | Out-Null
    Write-Host "  Website '$SiteName' created on port 80." -ForegroundColor Green
}

# Start Application Pool
Write-Host "Starting Application Pool..." -ForegroundColor Yellow
Start-WebAppPool -Name $AppPoolName
Start-Sleep -Seconds 2
Write-Host "  Application Pool started." -ForegroundColor Green

# Start Website
Write-Host "Starting Website..." -ForegroundColor Yellow
Start-Website -Name $SiteName
Write-Host "  Website started." -ForegroundColor Green

# Start IIS
Write-Host "Starting IIS..." -ForegroundColor Yellow
iisreset /start
Write-Host "  IIS started." -ForegroundColor Green

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  Deployment Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Access the site at:" -ForegroundColor Cyan
Write-Host "  http://localhost" -ForegroundColor White
Write-Host "  http://$env:COMPUTERNAME" -ForegroundColor White
Write-Host ""
Write-Host "Application Pool: $AppPoolName" -ForegroundColor Yellow
Write-Host "Physical Path: $DestinationPath" -ForegroundColor Yellow
Write-Host ""
