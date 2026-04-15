#Requires -Version 5.1
#Requires -RunAsAdministrator
<#
.SYNOPSIS
    Install prerequisites for Techlight MyDesk development environment
.DESCRIPTION
    Automates installation of IIS with ASP Classic support, .NET Framework 4.8, and Visual Studio Build Tools with MSBuild.
    Must be run as Administrator.
.PARAMETER InstallIIS
    Install IIS with ASP Classic support (default: true)
.PARAMETER InstallDotNet
    Install .NET Framework 4.8 (default: true)
.PARAMETER InstallBuildTools
    Install Visual Studio Build Tools with MSBuild (default: true)
.PARAMETER Force
    Force reinstallation even if components exist
.EXAMPLE
    .\Install.ps1
    .\Install.ps1 -Force
#>

[CmdletBinding()]
param(
    [bool]$InstallIIS = $true,
    [bool]$InstallDotNet = $true,
    [bool]$InstallBuildTools = $true,
    [switch]$Force
)

# Constants
$script:DotNet48InstallerUrl = "https://go.microsoft.com/fwlink/?linkid=2088631"
$script:BuildToolsUrl = "https://aka.ms/vs/17/release/vs_buildtools.exe"
$script:TempDir = "$env:TEMP\TechlightInstall"
$script:LogFile = "$env:TEMP\TechlightInstall.log"

# Color output helpers
function Write-Status([string]$Message, [string]$Color = "White") {
    Write-Host $Message -ForegroundColor $Color
    Add-Content -Path $script:LogFile -Value "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] $Message" -ErrorAction SilentlyContinue
}

function Write-Success([string]$Message) {
    Write-Host "✅ $Message" -ForegroundColor Green
    Add-Content -Path $script:LogFile -Value "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] SUCCESS: $Message" -ErrorAction SilentlyContinue
}

function Write-Warning([string]$Message) {
    Write-Host "⚠️  $Message" -ForegroundColor Yellow
    Add-Content -Path $script:LogFile -Value "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] WARNING: $Message" -ErrorAction SilentlyContinue
}

function Write-Error([string]$Message) {
    Write-Host "❌ $Message" -ForegroundColor Red
    Add-Content -Path $script:LogFile -Value "[$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')] ERROR: $Message" -ErrorAction SilentlyContinue
}

# Initialize log
function Initialize-Log {
    $null = New-Item -ItemType Directory -Path $script:TempDir -Force -ErrorAction SilentlyContinue
    "Installation started at $(Get-Date)" | Out-File -FilePath $script:LogFile -Encoding utf8
    Write-Status "Log file: $script:LogFile" -Color "Gray"
}

# Test if running as admin
function Test-Administrator {
    $currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
    return $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# Check .NET Framework version
function Test-DotNet48 {
    Write-Status "Checking .NET Framework 4.8..." -Color "Cyan"
    
    try {
        $dotNetKey = "HKLM:SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\"
        if (Test-Path $dotNetKey) {
            $dotNetInfo = Get-ItemProperty $dotNetKey -ErrorAction Stop
            $version = $dotNetInfo.Version
            $release = $dotNetInfo.Release
            
            Write-Status "Current .NET Framework: $version (Release: $release)" -Color "Gray"
            
            # Release 528040 = .NET 4.8
            if ($release -ge 528040 -or ($version -and [version]$version -ge [version]"4.8")) {
                Write-Success ".NET Framework 4.8+ is already installed"
                return $true
            }
        }
        
        Write-Warning ".NET Framework 4.8 not detected"
        return $false
    }
    catch {
        Write-Error "Failed to check .NET Framework version: $_"
        return $false
    }
}

# Install .NET Framework 4.8
function Install-DotNet48 {
    Write-Status "" -Color "White"
    Write-Status "========================================" -Color "Cyan"
    Write-Status "Installing .NET Framework 4.8" -Color "Cyan"
    Write-Status "========================================" -Color "Cyan"
    
    $installerPath = "$script:TempDir\ndp48-x86-x64-allos-enu.exe"
    
    # Download
    Write-Status "Downloading .NET Framework 4.8 installer..." -Color "Yellow"
    try {
        $progressPreference = 'SilentlyContinue'
        Invoke-WebRequest -Uri $script:DotNet48InstallerUrl -OutFile $installerPath -UseBasicParsing -ErrorAction Stop
        Write-Success "Downloaded .NET Framework 4.8 installer"
    }
    catch {
        Write-Error "Failed to download .NET Framework 4.8: $_"
        Write-Status "Please download manually from: https://dotnet.microsoft.com/download/dotnet-framework/net48" -Color "Yellow"
        return $false
    }
    finally {
        $progressPreference = 'Continue'
    }
    
    # Install
    Write-Status "Installing .NET Framework 4.8 (this may take a few minutes)..." -Color "Yellow"
    Write-Status "Please wait - installation in progress..." -Color "Gray"
    
    try {
        $process = Start-Process -FilePath $installerPath -ArgumentList "/q", "/norestart", "/log $script:TempDir\dotnet48_install.log" -Wait -PassThru -ErrorAction Stop
        
        if ($process.ExitCode -eq 0 -or $process.ExitCode -eq 3010) {
            # 3010 = success, reboot required
            Write-Success ".NET Framework 4.8 installed successfully"
            if ($process.ExitCode -eq 3010) {
                Write-Warning "A system restart is required to complete .NET Framework installation"
            }
            return $true
        }
        else {
            Write-Error ".NET Framework 4.8 installation failed (Exit code: $($process.ExitCode))"
            Write-Status "Check log: $script:TempDir\dotnet48_install.log" -Color "Yellow"
            return $false
        }
    }
    catch {
        Write-Error "Failed to install .NET Framework 4.8: $_"
        return $false
    }
}

# Find existing MSBuild
function Find-MSBuild {
    Write-Status "Checking for existing MSBuild..." -Color "Cyan"
    
    $possiblePaths = @(
        # VS 2022
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
        # VS 2019
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
    )
    
    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            Write-Success "Found MSBuild: $path"
            return $path
        }
    }
    
    # Try vswhere
    $vswherePath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswherePath) {
        try {
            $vsPath = & $vswherePath -latest -products * -requires Microsoft.Component.MSBuild -property installationPath 2>$null
            if ($vsPath) {
                $msbuildPath = Join-Path $vsPath "MSBuild\Current\Bin\MSBuild.exe"
                if (Test-Path $msbuildPath) {
                    Write-Success "Found MSBuild via vswhere: $msbuildPath"
                    return $msbuildPath
                }
            }
        }
        catch {
            # Continue to search
        }
    }
    
    Write-Warning "MSBuild not found"
    return $null
}

# Install Visual Studio Build Tools
function Install-BuildTools {
    Write-Status "" -Color "White"
    Write-Status "========================================" -Color "Cyan"
    Write-Status "Installing Visual Studio Build Tools" -Color "Cyan"
    Write-Status "========================================" -Color "Cyan"
    
    $installerPath = "$script:TempDir\vs_buildtools.exe"
    
    # Download
    Write-Status "Downloading Visual Studio Build Tools installer..." -Color "Yellow"
    try {
        $progressPreference = 'SilentlyContinue'
        Invoke-WebRequest -Uri $script:BuildToolsUrl -OutFile $installerPath -UseBasicParsing -ErrorAction Stop
        Write-Success "Downloaded Build Tools installer"
    }
    catch {
        Write-Error "Failed to download Build Tools: $_"
        return $false
    }
    finally {
        $progressPreference = 'Continue'
    }
    
    # Install with required components
    Write-Status "Installing Build Tools with web development components..." -Color "Yellow"
    Write-Status "This will take 10-20 minutes depending on your connection..." -Color "Gray"
    Write-Status "" -Color "White"
    
    $installArgs = @(
        "--quiet"
        "--wait"
        "--norestart"
        "--add", "Microsoft.VisualStudio.Workload.MSBuildTools"
        "--add", "Microsoft.VisualStudio.Workload.WebBuildTools"
        "--add", "Microsoft.VisualStudio.Component.WebDeploy"
        "--add", "Microsoft.VisualStudio.Component.NETFramework.TargetingPack_4.8"
        "--add", "Microsoft.VisualStudio.Component.Web"
        "--add", "Microsoft.VisualStudio.Web.BuildTools.ComponentGroup"
        "--add", "Microsoft.VisualStudio.Component.NuGet"
        "--add", "Microsoft.VisualStudio.Component.Roslyn.Compiler"
        "--add", "Microsoft.Net.Component.4.8.SDK"
        "--add", "Microsoft.Net.Component.4.8.TargetingPack"
        "--add", "Microsoft.VisualStudio.Component.AspNet"
        "--add", "Microsoft.VisualStudio.Component.AspNet45"
    )
    
    if ($Force) {
        $installArgs += "--force"
    }
    
    try {
        Write-Status "Starting installation (please wait, this may take a while)..." -Color "Yellow"
        
        $process = Start-Process -FilePath $installerPath -ArgumentList $installArgs -Wait -PassThru -ErrorAction Stop
        
        if ($process.ExitCode -eq 0 -or $process.ExitCode -eq 3010) {
            Write-Success "Visual Studio Build Tools installed successfully"
            if ($process.ExitCode -eq 3010) {
                Write-Warning "A system restart is required to complete the installation"
            }
            return $true
        }
        elseif ($process.ExitCode -eq 5004) {
            Write-Warning "Build Tools may already be installed or another instance is running"
            return $true
        }
        else {
            Write-Error "Build Tools installation failed (Exit code: $($process.ExitCode))"
            return $false
        }
    }
    catch {
        Write-Error "Failed to install Build Tools: $_"
        return $false
    }
}

# Detect if running on Windows Server or Windows 10/11
function Test-IsWindowsServer {
    $osInfo = Get-CimInstance -ClassName Win32_OperatingSystem
    # ProductType: 1 = Workstation (Windows 10/11), 2 = Domain Controller, 3 = Server
    return $osInfo.ProductType -eq 2 -or $osInfo.ProductType -eq 3
}

# Check if IIS is installed
function Test-IIS {
    Write-Status "Checking IIS installation..." -Color "Cyan"
    
    try {
        if (Test-IsWindowsServer) {
            $iisFeature = Get-WindowsFeature -Name Web-Server -ErrorAction Stop
            if ($iisFeature.InstallState -eq "Installed") {
                Write-Success "IIS is already installed"
                return $true
            }
            else {
                Write-Warning "IIS is not installed"
                return $false
            }
        }
        else {
            # Windows 10/11
            $iisFeature = Get-WindowsOptionalFeature -Online -FeatureName IIS-WebServer -ErrorAction Stop
            if ($iisFeature.State -eq "Enabled") {
                Write-Success "IIS is already installed"
                return $true
            }
            else {
                Write-Warning "IIS is not installed"
                return $false
            }
        }
    }
    catch {
        Write-Error "Failed to check IIS status: $_"
        return $false
    }
}

# Install IIS with all ASP Classic features
function Install-IIS {
    Write-Status "" -Color "White"
    Write-Status "========================================" -Color "Cyan"
    Write-Status "Installing IIS with ASP Classic Support" -Color "Cyan"
    Write-Status "========================================" -Color "Cyan"
    
    $isServer = Test-IsWindowsServer
    
    if ($isServer) {
        Write-Status "Detected Windows Server - using Server Manager cmdlets" -Color "Gray"
    }
    else {
        Write-Status "Detected Windows 10/11 - using DISM cmdlets" -Color "Gray"
    }
    
    Write-Status "Installing IIS and all required features..." -Color "Yellow"
    Write-Status "This may take several minutes..." -Color "Gray"
    
    try {
        if ($isServer) {
            # Windows Server - use Install-WindowsFeature
            $iisFeatures = @(
                "Web-Server",
                "Web-WebServer",
                "Web-Common-Http",
                "Web-Default-Doc",
                "Web-Dir-Browsing",
                "Web-Http-Errors",
                "Web-Static-Content",
                "Web-Http-Redirect",
                "Web-Health",
                "Web-Http-Logging",
                "Web-Performance",
                "Web-Stat-Compression",
                "Web-Security",
                "Web-Filtering",
                "Web-App-Dev",
                "Web-Net-Ext",
                "Web-ASP",
                "Web-ASP-Net45",
                "Web-ISAPI-Ext",
                "Web-ISAPI-Filter",
                "Web-Includes",
                "Web-WebSockets",
                "Web-Mgmt-Tools",
                "Web-Mgmt-Console",
                "Web-Mgmt-Compat",
                "Web-Metabase",
                "Web-Lgcy-Mgmt-Compat",
                "Web-Lgcy-Scripting",
                "Web-WMI",
                "Web-Scripting-Tools",
                "Web-Mgmt-Service",
                "Web-Ftp-Server",
                "Web-Ftp-Service",
                "Web-Ftp-Ext"
            )
            
            Write-Status "Enabling IIS features via Install-WindowsFeature..." -Color "Yellow"
            $installResult = Install-WindowsFeature -Name $iisFeatures -IncludeManagementTools -ErrorAction Stop
            
            if ($installResult.Success -eq $true) {
                Write-Success "IIS and all features installed successfully"
            }
            else {
                Write-Error "IIS installation failed"
                Write-Status "Restart the computer and try again" -Color "Yellow"
                return $false
            }
        }
        else {
            # Windows 10/11 - use Enable-WindowsOptionalFeature
            $iisFeatures = @(
                "IIS-WebServer",
                "IIS-WebServerRole",
                "IIS-CommonHttpFeatures",
                "IIS-DefaultDocument",
                "IIS-DirectoryBrowsing",
                "IIS-HttpErrors",
                "IIS-StaticContent",
                "IIS-HttpRedirect",
                "IIS-HealthAndDiagnostics",
                "IIS-HttpLogging",
                "IIS-Performance",
                "IIS-HttpCompressionStatic",
                "IIS-Security",
                "IIS-RequestFiltering",
                "IIS-ApplicationDevelopment",
                "IIS-NetFxExtensibility45",
                "IIS-ASPNET45",
                "IIS-ISAPIExtensions",
                "IIS-ISAPIFilter",
                "IIS-ServerSideIncludes",
                "IIS-WebSockets",
                "IIS-ManagementConsole",
                "IIS-BasicAuthentication",
                "IIS-WindowsAuthentication",
                "IIS-StaticCompression",
                "IIS-ManagementService",
                "IIS-FTPServer",
                "IIS-FTPService"
            )
            
            # ASP Classic features for Windows 10/11
            $aspFeatures = @(
                "IIS-ASP",
                "IIS-ASPNET45"
            )
            
            Write-Status "Enabling IIS features via Enable-WindowsOptionalFeature..." -Color "Yellow"
            
            foreach ($feature in $iisFeatures) {
                Write-Status "Enabling $feature..." -Color "Gray"
                try {
                    Enable-WindowsOptionalFeature -Online -FeatureName $feature -NoRestart -ErrorAction SilentlyContinue | Out-Null
                }
                catch {
                    $errorMsg = $_.Exception.Message
                    Write-Warning ("Could not enable " + $feature + ": " + $errorMsg)
                }
            }
            
            # Enable ASP Classic separately as it requires parent features
            Write-Status "Enabling ASP Classic features..." -Color "Yellow"
            foreach ($feature in $aspFeatures) {
                Write-Status "Enabling $feature..." -Color "Gray"
                try {
                    Enable-WindowsOptionalFeature -Online -FeatureName $feature -NoRestart -ErrorAction SilentlyContinue | Out-Null
                }
                catch {
                    $errorMsg = $_.Exception.Message
                    Write-Warning ("Could not enable " + $feature + ": " + $errorMsg)
                }
            }
            
            Write-Success "IIS and all features installed successfully"
        }
        
        # Enable ASP Classic error sending to browser
        Write-Status "Configuring ASP Classic error settings..." -Color "Yellow"
        try {
            # Enable script error messages to be sent to browser
            Set-WebConfigurationProperty -Filter "/system.webServer/httpErrors" -Name "errorMode" -Value "Detailed" -ErrorAction SilentlyContinue
            Set-WebConfigurationProperty -Filter "/system.webServer/asp" -Name "scriptErrorSentToBrowser" -Value "true" -ErrorAction SilentlyContinue
            Set-WebConfigurationProperty -Filter "/system.webServer/httpErrors" -Name "existingResponse" -Value "PassThrough" -ErrorAction SilentlyContinue
            
            Write-Success "ASP Classic error settings configured"
        }
        catch {
            Write-Warning "Could not configure ASP Classic error settings: $_"
        }
        
        # Enable detailed error messages for IIS
        Write-Status "Enabling detailed IIS error messages..." -Color "Yellow"
        try {
            Set-WebConfigurationProperty -Filter "system.webServer/httpErrors" -Name "errorMode" -Value "Detailed" -ErrorAction SilentlyContinue
            Write-Success "Detailed IIS error messages enabled"
        }
        catch {
            Write-Warning "Could not enable detailed IIS error messages: $_"
        }
        
        return $true
    }
    catch {
        Write-Error "Failed to install IIS: $_"
        return $false
    }
}

# Verify installation
function Test-Installation {
    Write-Status "" -Color "White"
    Write-Status "========================================" -Color "Cyan"
    Write-Status "Verifying Installation" -Color "Cyan"
    Write-Status "========================================" -Color "Cyan"
    
    $allGood = $true
    
    # Check .NET
    $hasDotNet = Test-DotNet48
    if (-not $hasDotNet) {
        Write-Error ".NET Framework 4.8 verification failed"
        $allGood = $false
    }
    
    # Check MSBuild
    $msbuildPath = Find-MSBuild
    if ($msbuildPath) {
        try {
            $version = & $msbuildPath /version /nologo 2>&1 | Select-Object -First 1
            Write-Success "MSBuild version: $version"
        }
        catch {
            Write-Warning "Could not determine MSBuild version"
        }
    }
    else {
        Write-Error "MSBuild not found after installation"
        $allGood = $false
    }
    
    return $allGood
}

# Test build
function Test-BuildCapability {
    param([string]$MSBuildPath)
    
    Write-Status "" -Color "White"
    Write-Status "========================================" -Color "Cyan"
    Write-Status "Testing Build Capability" -Color "Cyan"
    Write-Status "========================================" -Color "Cyan"
    
    $solutionPath = "MyDeskASPNet\MyDeskASPNet.sln"
    
    if (-not (Test-Path $solutionPath)) {
        Write-Warning "Solution file not found at expected path: $solutionPath"
        Write-Status "Build test skipped" -Color "Yellow"
        return $true
    }
    
    Write-Status "Attempting to restore NuGet packages..." -Color "Yellow"
    
    # Try to restore packages first
    $nugetPath = Get-Command "nuget" -ErrorAction SilentlyContinue
    if (-not $nugetPath) {
        $nugetPath = Get-Command "dotnet" -ErrorAction SilentlyContinue
    }
    
    if ($nugetPath) {
        try {
            if ($nugetPath.Name -eq "dotnet") {
                & dotnet restore $solutionPath 2>&1 | Out-Null
            }
            else {
                & $nugetPath.Source restore $solutionPath 2>&1 | Out-Null
            }
            Write-Success "NuGet packages restored"
        }
        catch {
            Write-Warning "NuGet restore failed (this is OK for initial test)"
        }
    }
    
    Write-Status "Build environment is ready!" -Color "Green"
    Write-Status "" -Color "White"
    Write-Status "Next step: Run .\Build.ps1 to build the MyDeskASPNet project" -Color "Yellow"
    
    return $true
}

# Cleanup
function Clear-InstallationFiles {
    Write-Status "" -Color "White"
    Write-Status "Cleaning up temporary files..." -Color "Gray"
    
    try {
        if (Test-Path $script:TempDir) {
            Remove-Item -Path $script:TempDir -Recurse -Force -ErrorAction SilentlyContinue
            Write-Status "Temporary files cleaned up" -Color "Gray"
        }
    }
    catch {
        Write-Warning "Could not clean up all temporary files: $_"
    }
}

# Main installation
function Start-Installation {
    Clear-Host
    Write-Status "========================================" -Color "Cyan"
    Write-Status "  Techlight MyDesk Prerequisites" -Color "Cyan"
    Write-Status "========================================" -Color "Cyan"
    Write-Status ""
    
    # Initialize
    Initialize-Log
    
    # Check admin rights
    if (-not (Test-Administrator)) {
        Write-Error "This script must be run as Administrator"
        Write-Status "Please right-click PowerShell and select 'Run as Administrator'" -Color "Yellow"
        exit 1
    }
    
    Write-Success "Running as Administrator"
    Write-Status ""
    
    $needsReboot = $false
    $success = $true
    
    # Install IIS
    if ($InstallIIS) {
        $hasIIS = Test-IIS
        if (-not $hasIIS -or $Force) {
            $iisResult = Install-IIS
            if (-not $iisResult) {
                $success = $false
            }
            else {
                $needsReboot = $true
            }
        }
    }
    
    # Install .NET Framework 4.8
    if ($InstallDotNet) {
        $hasDotNet = Test-DotNet48
        if (-not $hasDotNet -or $Force) {
            $dotNetResult = Install-DotNet48
            if (-not $dotNetResult) {
                $success = $false
            }
            else {
                $needsReboot = $true
            }
        }
    }
    
    # Install Build Tools
    if ($InstallBuildTools) {
        $existingMsBuild = Find-MSBuild
        if (-not $existingMsBuild -or $Force) {
            $buildToolsResult = Install-BuildTools
            if (-not $buildToolsResult) {
                $success = $false
            }
            else {
                $needsReboot = $true
            }
        }
    }
    
    # Verify
    $verifyResult = Test-Installation
    if (-not $verifyResult) {
        $success = $false
    }
    
    # Test build capability
    $msbuildFinal = Find-MSBuild
    if ($msbuildFinal) {
        Test-BuildCapability -MSBuildPath $msbuildFinal | Out-Null
    }
    
    # Cleanup
    Clear-InstallationFiles
    
    # Final summary
    Write-Status "" -Color "White"
    Write-Status "========================================" -Color "Cyan"
    Write-Status "  Installation Summary" -Color "Cyan"
    Write-Status "========================================" -Color "Cyan"
    
    if ($success) {
        Write-Success "Prerequisites installation completed!"
    }
    else {
        Write-Warning "Installation completed with some issues"
    }
    
    if ($needsReboot) {
        Write-Status "" -Color "White"
        Write-Warning "A system restart is REQUIRED to complete the installation"
        Write-Status "Please restart your computer before running Build.ps1" -Color "Yellow"
        
        $restart = Read-Host "Restart now? (y/n)"
        if ($restart -eq 'y' -or $restart -eq 'Y') {
            Write-Status "Restarting in 10 seconds..." -Color "Yellow"
            Start-Sleep -Seconds 10
            Restart-Computer -Force
        }
    }
    else {
        Write-Status "" -Color "White"
        Write-Status "You can now run .\Build.ps1 to build the MyDeskASPNet project" -Color "Green"
    }
    
    Write-Status "" -Color "White"
    Write-Status "Log file: $script:LogFile" -Color "Gray"
    Write-Status "" -Color "White"
    
    if ($success) {
        exit 0
    }
    else {
        exit 1
    }
}

# Run installation
try {
    Start-Installation
}
catch {
    Write-Error "Installation failed with error: $_"
    exit 1
}
