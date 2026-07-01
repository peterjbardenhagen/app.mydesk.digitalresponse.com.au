<#
.SYNOPSIS
    Builds and packages MyDesk for xcopy deployment to IIS on svr1.digitalresponse.com.au.

.DESCRIPTION
    Run this on your dev machine (pb-legion). It publishes the app as a
    framework-dependent win-x64 build and creates mydesk-publish.zip ready to
    upload and extract on the production server.

    On svr1, extract the zip to the site's physical path and restart the app pool.

.PARAMETER OutDir
    Folder to write the publish output into. Default: <repo>\artifacts\publish

.PARAMETER ZipPath
    Path for the output zip. Default: <repo>\artifacts\mydesk-publish.zip

.EXAMPLE
    .\Build-IISPackage.ps1
    .\Build-IISPackage.ps1 -ZipPath "C:\Drops\mydesk-publish.zip"
#>
param(
    [string]$OutDir  = "",
    [string]$ZipPath = ""
)

$ErrorActionPreference = "Stop"
$REPO = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$WEB  = Join-Path $REPO "src\MyDesk.Web"

if (-not $OutDir)  { $OutDir  = Join-Path $REPO "artifacts\publish" }
if (-not $ZipPath) { $ZipPath = Join-Path $REPO "artifacts\mydesk-publish.zip" }

function Write-Step($msg) { Write-Host "[BUILD] $msg" -ForegroundColor Cyan }
function Write-OK($msg)   { Write-Host "  [OK] $msg"   -ForegroundColor Green }

Write-Host ""
Write-Host "============================================" -ForegroundColor Cyan
Write-Host "  MyDesk — Build IIS Package for svr1" -ForegroundColor Cyan
Write-Host "============================================" -ForegroundColor Cyan
Write-Host ""

# Clean output directory
Write-Step "Cleaning output directory..."
if (Test-Path $OutDir) { Remove-Item $OutDir -Recurse -Force }
New-Item -ItemType Directory -Path $OutDir | Out-Null

# Publish
Write-Step "Publishing (win-x64 framework-dependent)..."
Push-Location $WEB
try {
    dotnet publish -c Release -r win-x64 --self-contained false -o $OutDir
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit $LASTEXITCODE)" }
    Write-OK "Publish succeeded"
} finally { Pop-Location }

# Remove development-only files from publish output
Write-Step "Cleaning development artefacts from publish output..."
@("appsettings.Development.json") | ForEach-Object {
    $f = Join-Path $OutDir $_
    if (Test-Path $f) { Remove-Item $f -Force; Write-Host "  Removed: $_" -ForegroundColor Gray }
}

# Zip
Write-Step "Creating zip package..."
if (Test-Path $ZipPath) { Remove-Item $ZipPath -Force }
Compress-Archive -Path "$OutDir\*" -DestinationPath $ZipPath
$sizeMB = [Math]::Round((Get-Item $ZipPath).Length / 1MB, 1)
Write-OK "Package: $ZipPath  ($sizeMB MB)"

Write-Host ""
Write-Host "Next steps on svr1.digitalresponse.com.au:" -ForegroundColor Yellow
Write-Host "  1. Upload mydesk-publish.zip to the server" -ForegroundColor White
Write-Host "  2. Stop the MyDesk app pool in IIS Manager" -ForegroundColor White
Write-Host "  3. Extract the zip over the existing site folder" -ForegroundColor White
Write-Host "  4. Start the app pool" -ForegroundColor White
Write-Host ""
Write-Host "  Or run the one-liner on svr1 (adjust paths):" -ForegroundColor White
Write-Host '  Expand-Archive -Path C:\Drops\mydesk-publish.zip -DestinationPath "C:\inetpub\mydesk" -Force' -ForegroundColor Gray
Write-Host ""
