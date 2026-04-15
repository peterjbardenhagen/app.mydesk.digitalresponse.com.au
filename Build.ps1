#Requires -Version 5.1
<#
.SYNOPSIS
    Build script for Techlight MyDeskASPNet project
.DESCRIPTION
    Builds the MyDeskASPNet solution and creates the binaries needed for deployment.
    This script locates MSBuild, restores NuGet packages, and compiles the solution.
.PARAMETER Configuration
    Build configuration: Debug or Release (default: Release)
.PARAMETER Clean
    Clean the build before compiling
.PARAMETER SkipRestore
    Skip NuGet package restore
.EXAMPLE
    .\Build.ps1
    .\Build.ps1 -Configuration Debug
    .\Build.ps1 -Clean
#>

[CmdletBinding()]
param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    
    [switch]$Clean,
    
    [switch]$SkipRestore
)

# Configuration
$script:ProjectName = "MyDeskASPNet"
$script:SolutionFile = "MyDeskASPNet\MyDeskASPNet.sln"
$script:ProjectFile = "MyDeskASPNet\MyDeskASPNet.csproj"
$script:OutputPath = "MyDeskASPNet\bin\$Configuration"

# Color output helpers
function Write-Status([string]$Message, [string]$Color = "White") {
    Write-Host $Message -ForegroundColor $Color
}

function Write-Success([string]$Message) {
    Write-Host "✅ $Message" -ForegroundColor Green
}

function Write-Warning([string]$Message) {
    Write-Host "⚠️  $Message" -ForegroundColor Yellow
}

function Write-Error([string]$Message) {
    Write-Host "❌ $Message" -ForegroundColor Red
}

# Find MSBuild
function Find-MSBuild {
    Write-Status "Locating MSBuild..." -Color "Cyan"
    
    # Check PATH first
    $msbuildInPath = Get-Command "msbuild" -ErrorAction SilentlyContinue
    if ($msbuildInPath) {
        Write-Success "Found MSBuild in PATH: $($msbuildInPath.Source)"
        return $msbuildInPath.Source
    }
    
    # Common MSBuild locations
    $possiblePaths = @(
        # VS 2022 (17.0+)
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
        "${env:ProgramFiles}\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"
        # VS 2019 (16.0)
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Enterprise\MSBuild\Current\Bin\MSBuild.exe"
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Professional\MSBuild\Current\Bin\MSBuild.exe"
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\MSBuild.exe"
        # VS 2017 (15.0)
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2017\BuildTools\MSBuild\15.0\Bin\MSBuild.exe"
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2017\Enterprise\MSBuild\15.0\Bin\MSBuild.exe"
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2017\Professional\MSBuild\15.0\Bin\MSBuild.exe"
        "${env:ProgramFiles(x86)}\Microsoft Visual Studio\2017\Community\MSBuild\15.0\Bin\MSBuild.exe"
    )
    
    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            Write-Success "Found MSBuild: $path"
            return $path
        }
    }
    
    # Try vswhere (VS 2017+ installer)
    $vswherePath = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
    if (Test-Path $vswherePath) {
        $vsPath = & $vswherePath -latest -products * -requires Microsoft.Component.MSBuild -property installationPath 2>$null
        if ($vsPath) {
            $msbuildPath = Join-Path $vsPath "MSBuild\Current\Bin\MSBuild.exe"
            if (Test-Path $msbuildPath) {
                Write-Success "Found MSBuild via vswhere: $msbuildPath"
                return $msbuildPath
            }
        }
    }
    
    Write-Error "MSBuild not found!"
    Write-Status "Please install Visual Studio Build Tools or Visual Studio 2019/2022" -Color "Yellow"
    Write-Status "See: 20250415/Install-DotNet48-MSBuild.md for instructions" -Color "Yellow"
    return $null
}

# Find NuGet
function Find-NuGet {
    Write-Status "Locating NuGet..." -Color "Cyan"
    
    # Check PATH
    $nugetInPath = Get-Command "nuget" -ErrorAction SilentlyContinue
    if ($nugetInPath) {
        return $nugetInPath.Source
    }
    
    # Check common locations
    $possiblePaths = @(
        "${env:ProgramFiles}\NuGet\nuget.exe"
        "${env:LOCALAPPDATA}\Microsoft\VisualStudio\NuGet\nuget.exe"
    )
    
    foreach ($path in $possiblePaths) {
        if (Test-Path $path) {
            return $path
        }
    }
    
    # Use dotnet nuget if available
    $dotnet = Get-Command "dotnet" -ErrorAction SilentlyContinue
    if ($dotnet) {
        Write-Warning "NuGet.exe not found, will use 'dotnet restore'"
        return "dotnet"
    }
    
    return $null
}

# Check .NET Framework 4.8
function Test-DotNet48 {
    Write-Status "Checking .NET Framework 4.8..." -Color "Cyan"
    
    $dotNetKey = "HKLM:SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\"
    if (Test-Path $dotNetKey) {
        $dotNetInfo = Get-ItemProperty $dotNetKey
        $version = $dotNetInfo.Version
        $release = $dotNetInfo.Release
        
        Write-Status ".NET Framework Version: $version (Release: $release)" -Color "Gray"
        
        # Release 528040 = .NET 4.8
        if ($release -ge 528040 -or $version -ge "4.8") {
            Write-Success ".NET Framework 4.8+ is installed"
            return $true
        } else {
            Write-Warning ".NET Framework 4.8 not detected (found $version)"
            return $false
        }
    } else {
        Write-Error ".NET Framework registry key not found"
        return $false
    }
}

# Restore NuGet packages
function Restore-Packages {
    param(
        [string]$NuGetPath,
        [string]$SolutionPath
    )
    
    Write-Status "Restoring NuGet packages..." -Color "Cyan"
    
    if ($NuGetPath -eq "dotnet") {
        # Use dotnet restore
        $restoreOutput = & dotnet restore $SolutionPath 2>&1
        $exitCode = $LASTEXITCODE
    } else {
        # Use nuget.exe
        $restoreOutput = & $NuGetPath restore $SolutionPath 2>&1
        $exitCode = $LASTEXITCODE
    }
    
    if ($exitCode -eq 0) {
        Write-Success "NuGet packages restored successfully"
        return $true
    } else {
        Write-Error "NuGet package restore failed"
        Write-Status $restoreOutput -Color "Red"
        return $false
    }
}

# Clean build artifacts
function Invoke-Clean {
    param(
        [string]$MSBuildPath,
        [string]$SolutionPath
    )
    
    Write-Status "Cleaning solution..." -Color "Cyan"
    
    $cleanOutput = & $MSBuildPath $SolutionPath /t:Clean /p:Configuration=$Configuration /v:minimal /nologo 2>&1
    $exitCode = $LASTEXITCODE
    
    # Show output on error
    if ($exitCode -ne 0) {
        $cleanOutput | ForEach-Object { Write-Status "  $_" -Color "Red" }
    }
    
    if ($exitCode -eq 0) {
        Write-Success "Clean completed"
        
        # Also clean obj folder
        $objPath = "MyDeskASPNet\obj"
        if (Test-Path $objPath) {
            Remove-Item -Path $objPath -Recurse -Force -ErrorAction SilentlyContinue
            Write-Status "Removed obj folder" -Color "Gray"
        }
        
        return $true
    } else {
        Write-Error "Clean failed"
        return $false
    }
}

# Build solution
function Invoke-Build {
    param(
        [string]$MSBuildPath,
        [string]$SolutionPath
    )
    
    Write-Status "Building solution ($Configuration)..." -Color "Cyan"
    Write-Status "Command: $MSBuildPath $SolutionPath /p:Configuration=$Configuration /p:Platform='Any CPU' /v:minimal" -Color "Gray"
    
    $buildOutput = & $MSBuildPath $SolutionPath /p:Configuration=$Configuration /p:Platform="Any CPU" /v:minimal /nologo 2>&1
    $exitCode = $LASTEXITCODE
    
    # Show output
    $buildOutput | ForEach-Object { Write-Status "  $_" -Color "Gray" }
    
    if ($exitCode -eq 0) {
        Write-Success "Build completed successfully"
        return $true
    } else {
        Write-Error "Build failed with exit code $exitCode"
        return $false
    }
}

# Verify build output
function Test-BuildOutput {
    Write-Status "Verifying build output..." -Color "Cyan"
    
    $binPath = "MyDeskASPNet\bin"
    $configBinPath = $OutputPath
    
    if (-not (Test-Path $binPath)) {
        Write-Error "bin folder not found"
        return $false
    }
    
    Write-Status "Checking for required binaries in $configBinPath..." -Color "Gray"
    
    $requiredFiles = @(
        "MyDeskASPNet.dll",
        "MyDeskASPNet.pdb",
        "GenerateQuote.aspx",
        "GenerateInvoice.aspx",
        "GenerateDeliveryNote.aspx",
        "GeneratePurchaseOrder.aspx",
        "ScrapeToPDF.aspx",
        "Web.config"
    )
    
    $foundFiles = @()
    $missingFiles = @()
    
    foreach ($file in $requiredFiles) {
        $filePath = Join-Path $configBinPath $file
        if (Test-Path $filePath) {
            $foundFiles += $file
            $fileInfo = Get-Item $filePath
            Write-Status "  ✓ $file ($($fileInfo.Length) bytes)" -Color "Green"
        } else {
            $missingFiles += $file
            Write-Status "  ✗ $file (not found)" -Color "Red"
        }
    }
    
    if ($missingFiles.Count -eq 0) {
        Write-Success "All required files found"
        return $true
    } else {
        Write-Warning "Some files are missing but build may still be valid"
        return $true
    }
}

# Main build process
function Start-BuildProcess {
    Write-Status "========================================" -Color "Cyan"
    Write-Status "  Techlight MyDeskASPNet Build Script" -Color "Cyan"
    Write-Status "========================================" -Color "Cyan"
    Write-Status ""
    
    # Get script location
    $scriptPath = $PSScriptRoot
    if (-not $scriptPath) {
        $scriptPath = Get-Location
    }
    
    Set-Location $scriptPath
    Write-Status "Working directory: $(Get-Location)" -Color "Gray"
    Write-Status "Configuration: $Configuration" -Color "Gray"
    Write-Status ""
    
    # Verify project exists
    if (-not (Test-Path $SolutionFile)) {
        Write-Error "Solution file not found: $SolutionFile"
        exit 1
    }
    
    # Check .NET Framework
    Test-DotNet48 | Out-Null
    Write-Status ""
    
    # Find MSBuild
    $msbuildPath = Find-MSBuild
    if (-not $msbuildPath) {
        exit 1
    }
    Write-Status ""
    
    # Find NuGet
    $nugetPath = $null
    if (-not $SkipRestore) {
        $nugetPath = Find-NuGet
        if (-not $nugetPath) {
            Write-Warning "NuGet not found, skipping package restore"
        }
        Write-Status ""
    }
    
    # Clean if requested
    if ($Clean) {
        $cleanResult = Invoke-Clean -MSBuildPath $msbuildPath -SolutionPath $SolutionFile
        if (-not $cleanResult) {
            Write-Error "Clean failed, aborting build"
            exit 1
        }
        Write-Status ""
    }
    
    # Restore packages
    if (-not $SkipRestore -and $nugetPath) {
        $restoreResult = Restore-Packages -NuGetPath $nugetPath -SolutionPath $SolutionFile
        if (-not $restoreResult) {
            Write-Error "Package restore failed, aborting build"
            exit 1
        }
        Write-Status ""
    }
    
    # Build
    $buildResult = Invoke-Build -MSBuildPath $msbuildPath -SolutionPath $SolutionFile
    if (-not $buildResult) {
        exit 1
    }
    Write-Status ""
    
    # Verify output
    Test-BuildOutput
    Write-Status ""
    
    # Summary
    Write-Status "========================================" -Color "Cyan"
    Write-Status "  Build Summary" -Color "Cyan"
    Write-Status "========================================" -Color "Cyan"
    Write-Success "Build completed successfully!"
    Write-Status "Output location: $(Resolve-Path $OutputPath)" -Color "Gray"
    Write-Status ""
    Write-Status "Next steps:" -Color "Yellow"
    Write-Status "  1. Copy the contents of $OutputPath to your IIS web server" -Color "White"
    Write-Status "  2. Ensure IIS is configured for ASP.NET 4.8" -Color "White"
    Write-Status "  3. Verify Web.config settings for your environment" -Color "White"
    Write-Status ""
}

# Run the build
try {
    Start-BuildProcess
    exit 0
} catch {
    Write-Error "An error occurred during the build process:"
    Write-Error $_.Exception.Message
    exit 1
}
