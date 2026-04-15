# Install .NET Framework 4.8 and MSBuild

## Option 1: Install Visual Studio Build Tools (Recommended)

This is the easiest way to get MSBuild with .NET Framework 4.8 support.

### Step 1: Download Build Tools
```powershell
# Download from Microsoft
$url = "https://aka.ms/vs/17/release/vs_buildtools.exe"
$output = "$env:TEMP\vs_buildtools.exe"
Invoke-WebRequest -Uri $url -OutFile $output
```

### Step 2: Install with Required Components
```powershell
# Install Build Tools with .NET Framework 4.8 and Web development components
& $env:TEMP\vs_buildtools.exe --quiet --wait --add Microsoft.VisualStudio.Workload.MSBuildTools --add Microsoft.VisualStudio.Component.WebDeploy --add Microsoft.VisualStudio.Component.NETFramework.TargetingPack_4.8 --add Microsoft.VisualStudio.Component.Web --add Microsoft.VisualStudio.Component.NuGet --add Microsoft.VisualStudio.Component.Roslyn.Compiler
```

**Or use the interactive installer:**
1. Run `vs_buildtools.exe`
2. Select **"Web development build tools"** workload
3. Ensure **.NET Framework 4.8 targeting pack** is selected
4. Install

---

## Option 2: Install Full Visual Studio 2022 Community (Free)

### Download
Visit: https://visualstudio.microsoft.com/vs/community/

### Required Workloads
During installation, select:
- [x] **ASP.NET and web development**
- [x] **.NET desktop development**

### Individual Components (ensure these are checked):
- [x] .NET Framework 4.8 SDK
- [x] .NET Framework 4.8 targeting pack
- [x] MSBuild
- [x] NuGet package manager

---

## Option 3: Install via Chocolatey (If Available)

```powershell
# Run as Administrator
choco install dotnetfx -y
choco install visualstudio2022buildtools -y
```

---

## Verify Installation

### Check .NET Framework 4.8
```powershell
# Check if .NET 4.8 is installed
Get-ItemProperty "HKLM:SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\" | Select Version, Release

# Should show Version 4.8 or higher
```

### Check MSBuild
```powershell
# Find MSBuild
Get-ChildItem -Path "C:\Program Files\Microsoft Visual Studio" -Recurse -Filter "msbuild.exe" -ErrorAction SilentlyContinue | Select-Object FullName

# Or check if in PATH
msbuild /version
```

---

## Build the Project

Once installed, build from PowerShell:

```powershell
cd C:\Development\Techlight.digitalresponse.com.au\MyDeskASPNet

# Option A: If MSBuild is in PATH
msbuild MyDeskASPNet.sln /p:Configuration=Release /p:Platform="Any CPU"

# Option B: Use full path (adjust version as needed)
& "C:\Program Files\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\MSBuild.exe" MyDeskASPNet.sln /p:Configuration=Release

# Option C: Restore packages first, then build
nuget restore MyDeskASPNet.sln
msbuild MyDeskASPNet.sln /p:Configuration=Release
```

---

## Common Issues

### Issue 1: "MSB4019: Microsoft.WebApplication.targets was not found"
**Solution:** Install "Web development build tools" workload

### Issue 2: Missing NuGet packages
**Solution:** 
```powershell
# Install NuGet CLI if needed
choco install nuget.commandline -y

# Restore packages
nuget restore MyDeskASPNet.sln
```

### Issue 3: ABCpdf license errors during build
**Solution:** This is expected - ABCpdf requires a license. The project will build but may show warnings.

---

## Quick Check Script

Save this as `Check-BuildEnvironment.ps1`:

```powershell
Write-Host "Checking Build Environment..." -ForegroundColor Cyan

# Check .NET Framework
$dotnet = Get-ItemProperty "HKLM:SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\" -ErrorAction SilentlyContinue
if ($dotnet.Version -ge "4.8") {
    Write-Host "✅ .NET Framework $($dotnet.Version) installed" -ForegroundColor Green
} else {
    Write-Host "❌ .NET Framework 4.8+ not found" -ForegroundColor Red
}

# Check MSBuild
$msbuild = Get-Command msbuild -ErrorAction SilentlyContinue
if ($msbuild) {
    Write-Host "✅ MSBuild found: $($msbuild.Source)" -ForegroundColor Green
    & msbuild /version
} else {
    Write-Host "❌ MSBuild not in PATH" -ForegroundColor Red
    # Try to find it
    $msbuildPaths = Get-ChildItem -Path "C:\Program Files\Microsoft Visual Studio" -Recurse -Filter "msbuild.exe" -ErrorAction SilentlyContinue
    if ($msbuildPaths) {
        Write-Host "Found MSBuild at:" -ForegroundColor Yellow
        $msbuildPaths | ForEach-Object { Write-Host "  $($_.FullName)" }
    }
}

# Check project
$projPath = "C:\Development\Techlight.digitalresponse.com.au\MyDeskASPNet\MyDeskASPNet.sln"
if (Test-Path $projPath) {
    Write-Host "✅ Project found: $projPath" -ForegroundColor Green
} else {
    Write-Host "❌ Project not found at expected location" -ForegroundColor Red
}
```

Run with:
```powershell
.\Check-BuildEnvironment.ps1
```

---

## After Installation

Once .NET 4.8 and MSBuild are installed, the project should build successfully with:

```powershell
cd C:\Development\Techlight.digitalresponse.com.au\MyDeskASPNet
msbuild MyDeskASPNet.sln /p:Configuration=Release
```

This will create the compiled output in `bin\Release\` folder.
