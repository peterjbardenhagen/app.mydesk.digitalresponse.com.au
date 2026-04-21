# Deployment script for MyDesk to local IIS
# Run this script as Administrator

Write-Host "Deploying MyDesk to local IIS..." -ForegroundColor Cyan

# Stop the IIS application pool
Write-Host "Stopping IIS application pool 'DR.MyDesk'..." -ForegroundColor Yellow
try {
    Import-Module WebAdministration
    Stop-WebAppPool -Name 'DR.MyDesk' -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    Write-Host "Application pool stopped." -ForegroundColor Green
} catch {
    Write-Host "Could not stop app pool (may not exist or already stopped): $_" -ForegroundColor Yellow
}

# Stop the IIS site
Write-Host "Stopping IIS site 'DR.MyDesk'..." -ForegroundColor Yellow
try {
    Stop-Website -Name 'DR.MyDesk' -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 1
    Write-Host "Site stopped." -ForegroundColor Green
} catch {
    Write-Host "Could not stop site (may not exist or already stopped): $_" -ForegroundColor Yellow
}

# Clear any file locks by stopping w3wp
Write-Host "Clearing file locks..." -ForegroundColor Yellow
Get-Process w3wp -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep -Seconds 2

# Copy files
$source = "C:\temp\DR.MyDesk-publish"
$destination = "C:\inetpub\wwwroot\DR.MyDesk"

Write-Host "Copying files from $source to $destination..." -ForegroundColor Yellow
if (Test-Path $source) {
    if (Test-Path $destination) {
        Remove-Item "$destination\*" -Recurse -Force -ErrorAction SilentlyContinue
    } else {
        New-Item -ItemType Directory -Path $destination -Force | Out-Null
    }
    Copy-Item -Path "$source\*" -Destination $destination -Recurse -Force
    Write-Host "Files copied successfully." -ForegroundColor Green
} else {
    Write-Host "ERROR: Source folder $source not found. Please run 'dotnet publish' first." -ForegroundColor Red
    exit 1
}

# Start the IIS application pool
Write-Host "Starting IIS application pool 'DR.MyDesk'..." -ForegroundColor Yellow
try {
    Import-Module WebAdministration
    Start-WebAppPool -Name 'DR.MyDesk' -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    Write-Host "Application pool started." -ForegroundColor Green
} catch {
    Write-Host "Could not start app pool: $_" -ForegroundColor Yellow
}

# Start the IIS site
Write-Host "Starting IIS site 'DR.MyDesk'..." -ForegroundColor Yellow
try {
    Start-Website -Name 'DR.MyDesk' -ErrorAction SilentlyContinue
    Write-Host "Site started." -ForegroundColor Green
} catch {
    Write-Host "Could not start site: $_" -ForegroundColor Yellow
}

Write-Host "`nDeployment complete!" -ForegroundColor Green
Write-Host "Access the site at: http://localhost/DR.MyDesk" -ForegroundColor Cyan
