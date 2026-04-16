# Techlight MyDesk - Local IIS Setup Script
# Run as Administrator
# This script automates the setup of IIS for the Techlight MyDesk ASP Classic/.NET application

param(
    [string]$SiteName = "TechlightMyDesk",
    [string]$SitePath = "C:\Development\Techlight.digitalresponse.com.au",
    [string]$DatabasePath = "C:\Database",
    [int]$Port = 80,
    [switch]$SkipIISInstall = $false,
    [switch]$SkipPrerequisites = $false,
    [switch]$Force = $false
)

# Requires -RunAsAdministrator

$ErrorActionPreference = "Stop"
$ProgressPreference = "Continue"

# Colors
$Green = "Green"
$Yellow = "Yellow"
$Red = "Red"
$Cyan = "Cyan"

function Write-Status($Message, $Color = $Green) {
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] $Message" -ForegroundColor $Color
}

function Write-ErrorStatus($Message) {
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] ERROR: $Message" -ForegroundColor $Red
}

function Write-WarningStatus($Message) {
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] WARNING: $Message" -ForegroundColor $Yellow
}

function Test-Administrator {
    $currentUser = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($currentUser)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Install-IISFeatures {
    Write-Status "Installing IIS Features..." $Cyan
    
    $features = @(
        "IIS-WebServerRole",
        "IIS-WebServer",
        "IIS-CommonHttpFeatures",
        "IIS-HttpErrors",
        "IIS-HttpRedirect",
        "IIS-ApplicationDevelopment",
        "IIS-ASP",
        "IIS-ASPNET45",
        "IIS-NetFxExtensibility45",
        "IIS-ISAPIExtensions",
        "IIS-ISAPIFilter",
        "IIS-HealthAndDiagnostics",
        "IIS-HttpLogging",
        "IIS-Security",
        "IIS-RequestFiltering",
        "IIS-WindowsAuthentication",
        "IIS-Performance",
        "IIS-WebServerManagementTools",
        "IIS-ManagementConsole",
        "IIS-ManagementScriptingTools",
        "IIS-StaticContent",
        "IIS-DefaultDocument",
        "IIS-DirectoryBrowsing"
    )
    
    foreach ($feature in $features) {
        try {
            $state = Get-WindowsOptionalFeature -Online -FeatureName $feature -ErrorAction SilentlyContinue
            if ($state.State -eq "Enabled") {
                Write-Status "  $feature already enabled" $Green
            } else {
                Write-Status "  Enabling $feature..." $Yellow
                Enable-WindowsOptionalFeature -Online -FeatureName $feature -All -NoRestart | Out-Null
            }
        }
        catch {
            Write-WarningStatus "Failed to enable $feature : $_"
        }
    }
    
    Write-Status "IIS Features installation complete!" $Green
}

function Test-ASPPHandler {
    Write-Status "Checking ASP Handler Configuration..." $Cyan
    
    $aspDllPath = "C:\Windows\System32\inetsrv\asp.dll"
    $appCmd = "$env:SystemRoot\System32\inetsrv\appcmd.exe"
    
    # Check if asp.dll exists
    if (-not (Test-Path $aspDllPath)) {
        Write-WarningStatus "ASP DLL not found at $aspDllPath"
        Write-Status "Re-enabling IIS-ASP feature to restore missing files..." $Yellow
        
        # Force re-enable ASP feature
        dism /online /enable-feature /featurename:IIS-ASP /all /limitaccess | Out-Null
        
        # Also try ApplicationDevelopment feature
        dism /online /enable-feature /featurename:IIS-ApplicationDevelopment /all /limitaccess | Out-Null
        
        # Check again after reinstall
        if (-not (Test-Path $aspDllPath)) {
            Write-ErrorStatus "ASP DLL still missing after feature re-enable. Windows may need repair."
            Write-Status "Try: sfc /scannow and DISM /Online /Cleanup-Image /RestoreHealth" $Yellow
            return $false
        }
        
        Write-Status "ASP DLL restored successfully" $Green
    } else {
        Write-Status "ASP DLL found at $aspDllPath" $Green
    }
    
    # Check handler mapping
    $handlers = & $appCmd list config /section:handlers 2>$null
    $hasAspHandler = $handlers | Select-String -Pattern "asp.*\.dll|ASPClassic"
    
    if (-not $hasAspHandler) {
        Write-WarningStatus "ASP handler mapping not found. Adding..." $Yellow
        
        # Add ASP handler mapping for .asp files
        & $appCmd set config /section:system.webServer/handlers /+"[name='ASPClassic',path='*.asp',verb='GET,POST,HEAD',modules='IsapiModule',scriptProcessor='$env:SystemRoot\system32\inetsrv\asp.dll',resourceType='File',requireAccess='Script',preCondition='bitness32']" 2>$null
        
        if ($LASTEXITCODE -eq 0) {
            Write-Status "ASP handler mapping added successfully" $Green
        } else {
            Write-WarningStatus "Failed to add ASP handler mapping automatically"
            Write-Status "Manual fix: Open IIS Manager > Handler Mappings > Add Script Map" $Yellow
            Write-Status "  Path: *.asp, Executable: %windir%\system32\inetsrv\asp.dll" $Yellow
        }
    } else {
        Write-Status "ASP handler mapping found" $Green
    }
    
    return $true
}

function Test-DotNet48 {
    Write-Status "Checking .NET Framework 4.8..." $Cyan
    
    $dotNetKey = Get-ChildItem 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP' -Recurse |
        Get-ItemProperty -Name Version -ErrorAction SilentlyContinue |
        Where-Object { $_.Version -like '4.8*' } |
        Select-Object -First 1
    
    if ($dotNetKey) {
        Write-Status ".NET Framework 4.8 is installed (Version: $($dotNetKey.Version))" $Green
        return $true
    }
    
    Write-WarningStatus ".NET Framework 4.8 NOT detected"
    Write-Status "Please download and install from:" $Yellow
    Write-Status "https://dotnet.microsoft.com/download/dotnet-framework/net48" $Cyan
    return $false
}

function Test-AccessDatabaseEngine {
    Write-Status "Checking Access Database Engine..." $Cyan
    
    $accessEnginePaths = @(
        "HKLM:\SOFTWARE\Microsoft\Office\ClickToRun\REGISTRY\MACHINE\Software\Microsoft\Office\16.0\Access Connectivity Engine\Engines\ACE",
        "HKLM:\SOFTWARE\Wow6432Node\Microsoft\Office\ClickToRun\REGISTRY\MACHINE\Software\Microsoft\Office\16.0\Access Connectivity Engine\Engines\ACE"
    )
    
    $found = $false
    foreach ($path in $accessEnginePaths) {
        if (Test-Path $path) {
            $found = $true
            break
        }
    }
    
    if ($found) {
        Write-Status "Access Database Engine is installed" $Green
        return $true
    }
    
    Write-WarningStatus "Access Database Engine NOT detected"
    Write-Status "Please download and install from:" $Yellow
    Write-Status "https://www.microsoft.com/en-us/download/details.aspx?id=54920" $Cyan
    return $false
}

function New-TechlightAppPool {
    param(
        [string]$Name,
        [string]$RuntimeVersion = "",
        [bool]$Enable32Bit = $true
    )
    
    Write-Status "Creating Application Pool: $Name..." $Cyan
    
    $appCmd = "$env:SystemRoot\System32\inetsrv\appcmd.exe"
    
    # Check if app pool exists using appcmd
    $poolList = & $appCmd list apppool /name:$Name 2>$null
    if ($poolList -and $poolList -match $Name) {
        if ($Force) {
            Write-Status "  Removing existing app pool..." $Yellow
            & $appCmd delete apppool /apppool.name:$Name 2>$null
        } else {
            Write-WarningStatus "Application pool '$Name' already exists. Use -Force to recreate."
            return
        }
    }
    
    # Create and configure app pool using appcmd
    try {
        & $appCmd add apppool /name:$Name 2>$null
        
        if ($RuntimeVersion -eq "") {
            & $appCmd set apppool /apppool.name:$Name /managedRuntimeVersion:"" 2>$null
        } else {
            & $appCmd set apppool /apppool.name:$Name /managedRuntimeVersion:$RuntimeVersion 2>$null
        }
        
        & $appCmd set apppool /apppool.name:$Name /enable32BitAppOnWin64:$Enable32Bit 2>$null
        & $appCmd set apppool /apppool.name:$Name /processModel.identityType:ApplicationPoolIdentity 2>$null
        & $appCmd set apppool /apppool.name:$Name /processModel.loadUserProfile:true 2>$null
        
        Write-Status "  Application pool '$Name' created successfully" $Green
    }
    catch {
        Write-ErrorStatus "Failed to create application pool: $_"
        throw
    }
}

function New-TechlightWebsite {
    Write-Status "Creating Website: $SiteName..." $Cyan
    
    $appCmd = "$env:SystemRoot\System32\inetsrv\appcmd.exe"
    
    # Check if website exists
    $siteList = & $appCmd list site /name:$SiteName 2>$null
    if ($siteList -and $siteList -match $SiteName) {
        if ($Force) {
            Write-Status "  Removing existing website..." $Yellow
            & $appCmd stop site /site.name:$SiteName 2>$null
            & $appCmd delete site /site.name:$SiteName 2>$null
        } else {
            Write-WarningStatus "Website '$SiteName' already exists. Use -Force to recreate."
            return
        }
    }
    
    # Ensure physical path exists
    if (-not (Test-Path $SitePath)) {
        Write-ErrorStatus "Site path does not exist: $SitePath"
        throw "Site path not found"
    }
    
    # Create website using appcmd
    try {
        $binding = "http://*:${Port}:"
        & $appCmd add site /name:$SiteName /bindings:$binding /physicalPath:$SitePath 2>$null
        & $appCmd set site /site.name:$SiteName /applicationDefaults.applicationPool:TechlightMyDesk 2>$null
        
        # Configure default documents
        Write-Status "  Configuring default documents..." $Yellow
        & $appCmd set config "$SiteName" /section:defaultDocument /+files.[value='Default.asp'] 2>$null
        & $appCmd set config "$SiteName" /section:defaultDocument /+files.[value='Default.aspx'] 2>$null
        & $appCmd set config "$SiteName" /section:defaultDocument /+files.[value='index.asp'] 2>$null
        
        # Enable directory browsing
        & $appCmd set config "$SiteName" /section:directoryBrowse /enabled:true 2>$null
        
        # Start the site
        & $appCmd start site /site.name:$SiteName 2>$null
        
        Write-Status "  Website '$SiteName' created successfully" $Green
    }
    catch {
        Write-ErrorStatus "Failed to create website: $_"
        throw
    }
}

function New-MyDeskASPNetApplication {
    Write-Status "Creating MyDeskASPNet Application..." $Cyan
    
    $appCmd = "$env:SystemRoot\System32\inetsrv\appcmd.exe"
    $physicalPath = Join-Path $SitePath "MyDeskASPNet"
    
    # Check if application exists
    $appList = & $appCmd list app /site.name:$SiteName /path:/MyDeskASPNet 2>$null
    if ($appList -and $appList -match "MyDeskASPNet") {
        if ($Force) {
            Write-Status "  Removing existing application..." $Yellow
            & $appCmd delete app "/$SiteName/MyDeskASPNet" 2>$null
        } else {
            Write-WarningStatus "Application 'MyDeskASPNet' already exists. Use -Force to recreate."
            return
        }
    }
    
    # Check physical path exists
    if (-not (Test-Path $physicalPath)) {
        Write-ErrorStatus "MyDeskASPNet path does not exist: $physicalPath"
        return
    }
    
    # Create application using appcmd
    try {
        & $appCmd add app /site.name:$SiteName /path:/MyDeskASPNet /physicalPath:$physicalPath 2>$null
        & $appCmd set app "/$SiteName/MyDeskASPNet" /applicationPool:TechlightMyDeskNet 2>$null
        
        Write-Status "  MyDeskASPNet application created successfully" $Green
    }
    catch {
        Write-ErrorStatus "Failed to create application: $_"
    }
}

function New-MyDeskMCPApplication {
    Write-Status "Creating MyDeskMCP Application..." $Cyan
    
    $appCmd = "$env:SystemRoot\System32\inetsrv\appcmd.exe"
    $physicalPath = Join-Path $SitePath "MyDeskMCP"
    
    # Check if application exists
    $appList = & $appCmd list app /site.name:$SiteName /path:/MyDeskMCP 2>$null
    if ($appList -and $appList -match "MyDeskMCP") {
        if ($Force) {
            Write-Status "  Removing existing MCP application..." $Yellow
            & $appCmd delete app "/$SiteName/MyDeskMCP" 2>$null
        } else {
            Write-WarningStatus "Application 'MyDeskMCP' already exists. Use -Force to recreate."
            return
        }
    }
    
    # Check physical path exists
    if (-not (Test-Path $physicalPath)) {
        Write-ErrorStatus "MyDeskMCP path does not exist: $physicalPath"
        return
    }
    
    # Create application using appcmd - sharing the ASPNet application pool
    try {
        & $appCmd add app /site.name:$SiteName /path:/MyDeskMCP /physicalPath:$physicalPath 2>$null
        & $appCmd set app "/$SiteName/MyDeskMCP" /applicationPool:TechlightMyDeskNet 2>$null
        
        Write-Status "  MyDeskMCP application created successfully" $Green
    }
    catch {
        Write-ErrorStatus "Failed to create MCP application: $_"
    }
}

function Set-WebsitePermissions {
    Write-Status "Setting Directory Permissions..." $Cyan
    
    $identity = "IIS_IUSRS"
    $rights = "Modify"
    $inheritance = "ContainerInherit,ObjectInherit"
    $propagation = "None"
    $type = "Allow"
    
    # Site path
    if (Test-Path $SitePath) {
        Write-Status "  Granting $rights to $identity on $SitePath..." $Yellow
        $acl = Get-Acl $SitePath
        $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule($identity, $rights, $inheritance, $propagation, $type)
        
        $hasRule = $false
        foreach ($rule in $acl.Access) {
            if ($rule.IdentityReference -eq $identity -and $rule.FileSystemRights -eq $rights) {
                $hasRule = $true
                break
            }
        }
        
        if (-not $hasRule) {
            $acl.SetAccessRule($accessRule)
            Set-Acl $SitePath $acl
        }
    }
    
    # Database path
    if (Test-Path $DatabasePath) {
        Write-Status "  Granting $rights to $identity on $DatabasePath..." $Yellow
        $acl2 = Get-Acl $DatabasePath
        $accessRule2 = New-Object System.Security.AccessControl.FileSystemAccessRule($identity, $rights, $inheritance, $propagation, $type)
        
        $hasRule2 = $false
        foreach ($rule in $acl2.Access) {
            if ($rule.IdentityReference -eq $identity -and $rule.FileSystemRights -eq $rights) {
                $hasRule2 = $true
                break
            }
        }
        
        if (-not $hasRule2) {
            $acl2.SetAccessRule($accessRule2)
            Set-Acl $DatabasePath $acl2
        }
    } else {
        Write-WarningStatus "Database path does not exist: $DatabasePath"
        Write-Status "Creating database directory..." $Yellow
        New-Item -ItemType Directory -Path $DatabasePath -Force | Out-Null
        $acl3 = Get-Acl $DatabasePath
        $accessRule3 = New-Object System.Security.AccessControl.FileSystemAccessRule($identity, $rights, $inheritance, $propagation, $type)
        $acl3.SetAccessRule($accessRule3)
        Set-Acl $DatabasePath $acl3
    }
    
    Write-Status "  Permissions configured successfully" $Green
}

function Set-Authentication {
    Write-Status "Configuring Authentication..." $Cyan
    
    $appCmd = "$env:SystemRoot\System32\inetsrv\appcmd.exe"
    
    # Enable Anonymous Authentication
    & $appCmd set config "$SiteName" /section:anonymousAuthentication /enabled:true 2>$null
    
    # Disable Windows Authentication
    & $appCmd set config "$SiteName" /section:windowsAuthentication /enabled:false 2>$null
    
    Write-Status "  Authentication configured" $Green
}

function Test-Website {
    Write-Status "Testing Website..." $Cyan
    
    $urls = @(
        "http://localhost/",
        "http://localhost/MyDeskASPNet/"
    )
    
    foreach ($url in $urls) {
        try {
            Write-Status "  Testing $url..." $Yellow
            $response = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 10 -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                Write-Status "    OK: $url is responding (HTTP 200)" $Green
            } else {
                Write-WarningStatus "  $url returned HTTP $($response.StatusCode)"
            }
        }
        catch {
            Write-WarningStatus "  Could not reach $url : $_"
        }
    }
}

function Add-HostsEntry {
    Write-Status "Configuring Hosts File..." $Cyan
    
    $hostsFile = "C:\Windows\System32\drivers\etc\hosts"
    $entries = @(
        "127.0.0.1    techlight.local",
        "127.0.0.1    techlight.digitalresponse.com.au.local"
    )
    
    if (Test-Path $hostsFile) {
        $hostsContent = Get-Content $hostsFile -Raw
        
        foreach ($entry in $entries) {
            if ($hostsContent -notmatch [regex]::Escape($entry)) {
                Write-Status "  Adding hosts entry: $entry" $Yellow
                Add-Content -Path $hostsFile -Value $entry -Force
            } else {
                Write-Status "  Hosts entry already exists: $entry" $Green
            }
        }
    }
}

function New-GitIgnore {
    Write-Status "Creating .gitignore file..." $Cyan
    
    $gitignorePath = Join-Path $SitePath ".gitignore"
    
    if (Test-Path $gitignorePath) {
        Write-Status "  .gitignore already exists" $Green
        return
    }
    
    $gitignoreContent = @"
# Build artifacts
obj/
bin/
*.log

# Windows files
Thumbs.db
Desktop.ini
ehthumbs.db
*.stackdump

# IDE
.vs/
*.user
*.suo
*.cache

# Temp files
*.tmp
*.temp
*.pidb
*.resources

# Database backups
*.mdb.bak
*.ldb

# Generated PDFs
*/Quotes/Files/*.pdf
*/Invoices/Files/*.pdf
*/PurchaseOrders/Files/*.pdf
*/RFQ/Files/*.pdf

# Security
web.config.bak
*.config.bak
"@
    
    Set-Content -Path $gitignorePath -Value $gitignoreContent
    Write-Status "  .gitignore file created successfully" $Green
}

function Show-Summary {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "   SETUP COMPLETE!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Website:        http://localhost/" -ForegroundColor White
    Write-Host "                 http://localhost/Clients/SalesEngineTL/" -ForegroundColor White
    Write-Host "MyDeskASPNet:   http://localhost/MyDeskASPNet/" -ForegroundColor White
    Write-Host "MyDeskMCP:      http://localhost/MyDeskMCP/" -ForegroundColor White
    Write-Host "Local domains:  http://techlight.local/" -ForegroundColor White
    Write-Host ""
    Write-Host "Physical Path:  $SitePath" -ForegroundColor Gray
    Write-Host "Database Path:  $DatabasePath" -ForegroundColor Gray
    Write-Host ""
    Write-Host "NEXT STEPS:" -ForegroundColor Yellow
    Write-Host "1. Copy Techlight2.mdb to $DatabasePath" -ForegroundColor White
    Write-Host "2. Update connection strings in /System/ssi_dbConn_open_TL.inc if needed" -ForegroundColor White
    Write-Host "3. Open browser and test: http://localhost/Clients/SalesEngineTL/Portal/Validate.asp" -ForegroundColor White
    Write-Host "4. For PDF generation, ensure ABCpdf is installed (see documentation)" -ForegroundColor White
    Write-Host ""
    Write-Host "TROUBLESHOOTING:" -ForegroundColor Yellow
    Write-Host "- If 404.3 errors: ASP handler not configured. Re-run Setup.ps1 or:" -ForegroundColor White
    Write-Host "    appcmd set config /section:system.webServer/handlers /+[name='ASPClassic',path='*.asp'...]" -ForegroundColor Gray
    Write-Host "- If DB errors: Grant IIS_IUSRS Modify permissions to $DatabasePath" -ForegroundColor White
    Write-Host "- If 500 errors: Enable detailed errors in IIS `- Site `- Error Pages" -ForegroundColor White
    Write-Host ""
}

# ============================================
# MAIN EXECUTION
# ============================================

Write-Host ""
Write-Host "Techlight MyDesk - Local IIS Setup" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

# Check admin rights
if (-not (Test-Administrator)) {
    Write-ErrorStatus "This script must be run as Administrator!"
    Write-Status "Right-click PowerShell and select 'Run as Administrator'" $Yellow
    exit 1
}

# Check prerequisites
if (-not $SkipPrerequisites) {
    $hasDotNet = Test-DotNet48
    $hasAccessEngine = Test-AccessDatabaseEngine
    
    if (-not $hasDotNet -or -not $hasAccessEngine) {
        Write-ErrorStatus "Prerequisites not met. Please install missing components."
        Write-Status "Run with -SkipPrerequisites to skip these checks (not recommended)" $Yellow
        exit 1
    }
}

# Install IIS features
if (-not $SkipIISInstall) {
    Install-IISFeatures
    
    # Verify ASP handler is properly configured
    $aspConfigured = Test-ASPPHandler
    if (-not $aspConfigured) {
        Write-WarningStatus "ASP configuration incomplete. Site may not work properly."
    }
}

# Create Application Pools
New-TechlightAppPool -Name "TechlightMyDesk" -RuntimeVersion "" -Enable32Bit $true
New-TechlightAppPool -Name "TechlightMyDeskNet" -RuntimeVersion "v4.0" -Enable32Bit $true

# Set permissions before creating site
Set-WebsitePermissions

# Create Website
New-TechlightWebsite

# Create MyDeskASPNet Application
New-MyDeskASPNetApplication

# Create MyDeskMCP Application
New-MyDeskMCPApplication

# Configure Authentication
Set-Authentication

# Add hosts file entries
Add-HostsEntry

# Create .gitignore
New-GitIgnore

# Test the website
Test-Website

# Show summary
Show-Summary

Write-Status "Setup complete!" $Green

