# Build Android APK for Digital Response MyDesk
# Requires: Android SDK, Java JDK 17, Gradle

$ErrorActionPreference = "Stop"
$projectRoot = "DigitalResponseMyDesk"
$outputDir = "..\..\..\artifacts"
$apkName = "DigitalResponseMyDesk-v1.0.0.apk"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Building Digital Response MyDesk APK" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan

# Check prerequisites
$javaVersion = java -version 2>&1
if (-not $?) {
    Write-Host "ERROR: Java JDK 17+ is required. Install from https://adoptium.net/" -ForegroundColor Red
    exit 1
}

$androidHome = $env:ANDROID_HOME
if (-not $androidHome) {
    $androidHome = "$env:LOCALAPPDATA\Android\Sdk"
    if (-not (Test-Path $androidHome)) {
        Write-Host "ERROR: Android SDK not found. Install Android Studio and set ANDROID_HOME." -ForegroundColor Red
        exit 1
    }
    $env:ANDROID_HOME = $androidHome
}

Write-Host "Android SDK: $androidHome" -ForegroundColor Green
Write-Host "Java: $(java -version 2>&1 | Select-Object -First 1)" -ForegroundColor Green

# Navigate to project
Push-Location $projectRoot

try {
    # Clean previous builds
    if (Test-Path "app\build") {
        Remove-Item -Recurse -Force "app\build"
    }

    # Build the APK
    Write-Host "`nBuilding APK..." -ForegroundColor Yellow
    
    # Use gradlew if available, otherwise use gradle
    if (Test-Path "gradlew.bat") {
        & .\gradlew.bat assembleRelease --no-daemon
    } else {
        gradle assembleRelease --no-daemon
    }

    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Build failed with exit code $LASTEXITCODE" -ForegroundColor Red
        exit 1
    }

    # Find the built APK
    $apkPath = Get-ChildItem -Path "app\build\outputs\apk\release" -Filter "*.apk" | Select-Object -First 1
    
    if (-not $apkPath) {
        Write-Host "ERROR: APK not found after build" -ForegroundColor Red
        exit 1
    }

    # Create output directory
    if (-not (Test-Path $outputDir)) {
        New-Item -ItemType Directory -Path $outputDir -Force
    }

    # Copy APK to output
    $outputPath = Join-Path $outputDir $apkName
    Copy-Item $apkPath.FullName $outputPath -Force
    
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "APK built successfully!" -ForegroundColor Green
    Write-Host "Location: $outputPath" -ForegroundColor Green
    Write-Host "Size: $([math]::Round((Get-Item $outputPath).Length / 1MB, 2)) MB" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Cyan

    # Copy to OneDrive
    $oneDrivePath = "C:\Users\PeterBardenhagen\OneDrive - digitalresponse\DigitalResponseMyDesk.apk"
    Copy-Item $outputPath $oneDrivePath -Force
    Write-Host "`nCopied to OneDrive: $oneDrivePath" -ForegroundColor Green

} finally {
    Pop-Location
}