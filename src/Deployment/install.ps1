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

$localDbPrivateInstance = "MSSQLLocalDB"
$localDbSharedInstance = "MyDeskShared"
$iisConnectionString = "Server=(localdb)\.\$localDbSharedInstance;Database=Techlight_MyDesk;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False;"

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

# 2. Check ASP.NET Core Hosting Bundle / runtime for .NET 10.
# The old registry-based check was too brittle and often missed preview or newer
# servicing updates. A successful IIS host needs BOTH:
#   - the ASP.NET Core Module V2 for IIS, and
#   - the matching Microsoft.AspNetCore.App runtime installed.
Write-Host "[2/6] Checking ASP.NET Core Hosting Bundle (.NET 10)..." -NoNewline

$aspNetCoreModule = Join-Path ${env:ProgramFiles} "IIS\Asp.Net Core Module\V2\aspnetcorev2.dll"
$runtimes = & dotnet --list-runtimes 2>$null
$hasAspNet10Runtime = $false
if ($runtimes) {
    $hasAspNet10Runtime = $runtimes -match '^Microsoft\.AspNetCore\.App\s+10\.'
}

if ((Test-Path $aspNetCoreModule) -and $hasAspNet10Runtime) {
    Write-Host " OK" -ForegroundColor Green
} else {
    Write-Host " MISSING" -ForegroundColor Yellow
    Write-Host "      Expected IIS hosting support for ASP.NET Core 10.x." -ForegroundColor Yellow
    Write-Host "      Download: https://dotnet.microsoft.com/download/dotnet/10.0" -ForegroundColor Yellow
    Write-Host "      Install 'ASP.NET Core Runtime 10.0.x - Windows Hosting Bundle'" -ForegroundColor Yellow
    Write-Host "      Current runtimes:" -ForegroundColor Yellow
    if ($runtimes) {
        $runtimes | ForEach-Object { Write-Host "        $_" -ForegroundColor DarkYellow }
    } else {
        Write-Host "        (could not read 'dotnet --list-runtimes')" -ForegroundColor DarkYellow
    }
    Write-Host "      IIS module present: $(Test-Path $aspNetCoreModule)" -ForegroundColor Yellow
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

# Share Peter's LocalDB instance so IIS (NetworkService) can connect to it.
Write-Host "[3a/6] Preparing shared LocalDB instance for IIS..." -NoNewline
& sqllocaldb start $localDbPrivateInstance | Out-Null
$shareOutput = & sqllocaldb share $localDbPrivateInstance $localDbSharedInstance 2>&1
if ($LASTEXITCODE -ne 0 -and ($shareOutput -join "`n") -notmatch "already exists") {
    Write-Host " FAIL" -ForegroundColor Red
    Write-Host $shareOutput -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}
Write-Host " OK" -ForegroundColor Green

# Stamp IIS-safe production connection string into the published output.
# IIS cannot use the private per-user LocalDB instance directly; it must use
# the admin-created shared LocalDB alias above.
$prodSettingsPath = Join-Path $publishPath "appsettings.Production.json"
if (Test-Path $prodSettingsPath) {
    $prod = Get-Content $prodSettingsPath -Raw | ConvertFrom-Json
    if (-not $prod.ConnectionStrings) {
        $prod | Add-Member -MemberType NoteProperty -Name ConnectionStrings -Value ([pscustomobject]@{})
    }
    $prod.ConnectionStrings.TechlightDb = $iisConnectionString
    $prod | ConvertTo-Json -Depth 20 | Set-Content $prodSettingsPath -Encoding UTF8
}

# Keep startup SQL migrations available in the deployed site.
$sourceMigrationPath = Join-Path $scriptDir "Migration"
$publishMigrationPath = Join-Path $publishPath "Deployment\Migration"
if (Test-Path $sourceMigrationPath) {
    New-Item -ItemType Directory -Path $publishMigrationPath -Force | Out-Null
    robocopy $sourceMigrationPath $publishMigrationPath *.sql /NJH /NJS /NP | Out-Null
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

Get-Process dotnet,w3wp -ErrorAction SilentlyContinue |
    Where-Object {
        ($_.Path -and $_.Path -like "*dotnet*") -or $_.ProcessName -eq "w3wp"
    } |
    ForEach-Object {
        try { Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue } catch { }
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

# Grant IIS_IUSRS + NETWORK SERVICE permissions
$acl = Get-Acl $physicalPath
$permission = "IIS_IUSRS", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
$accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule $permission
$acl.SetAccessRule($accessRule)

$networkServicePermission = "NETWORK SERVICE", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow"
$networkServiceRule = New-Object System.Security.AccessControl.FileSystemAccessRule $networkServicePermission
$acl.SetAccessRule($networkServiceRule)

Set-Acl $physicalPath $acl

Write-Host " OK" -ForegroundColor Green

Write-Host "Verifying IIS /login..." -NoNewline
$iisReady = $false
for ($i = 0; $i -lt 10; $i++) {
    Start-Sleep -Seconds 2
    try {
        $response = Invoke-WebRequest -Uri "http://localhost/login" -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            $iisReady = $true
            break
        }
    } catch {
    }
}
if ($iisReady) {
    Write-Host " OK" -ForegroundColor Green
} else {
    Write-Host " WARN" -ForegroundColor Yellow
    Write-Host "      IIS started, but /login did not return HTTP 200 yet. Check C:\inetpub\wwwroot\MyDesk\Logs." -ForegroundColor Yellow
}

# 6. Create and start IIS site
Write-Host "[6/6] Creating IIS site..." -NoNewline

# Create app pool (No Managed Code for ASP.NET Core hosted behind IIS)
& $appCmd add apppool /name:$appPoolName /managedRuntimeVersion:"" | Out-Null
& $appCmd set apppool $appPoolName /startMode:AlwaysRunning | Out-Null

# IMPORTANT: use NetworkService so Integrated Security / LocalDB access happens
# under the machine account context expected by this environment.
& $appCmd set apppool $appPoolName /processModel.identityType:NetworkService | Out-Null

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
Write-Host "  Identity:  NETWORK SERVICE" -ForegroundColor White
Write-Host "  SQL:       $iisConnectionString" -ForegroundColor White
Write-Host "  Path:      $physicalPath" -ForegroundColor White
Write-Host ""
Write-Host "================================================================================" -ForegroundColor Green
Write-Host ""
Write-Host "Opening browser..." -ForegroundColor Yellow
Start-Sleep -Seconds 2
Start-Process "http://localhost"

Write-Host ""
Read-Host "Press Enter to close this window"
