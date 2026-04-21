# DR MyDesk - Setup Script
# Requires Administrator privileges

# Check elevation
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (!$isAdmin) {
    Write-Host "ERROR: Please run PowerShell as Administrator" -ForegroundColor Red
    Write-Host "Right-click PowerShell → 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

$base = Split-Path -Parent $MyInvocation.MyCommand.Path

function Show-Menu {
    Clear-Host
    Write-Host ""
    Write-Host "  ╔═══════════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "  ║                                                               ║" -ForegroundColor Cyan
    Write-Host "  ║              DR MyDesk - Setup & Deploy                       ║" -ForegroundColor Cyan
    Write-Host "  ║                                                               ║" -ForegroundColor Cyan
    Write-Host "  ╚═══════════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  [1]  Database - Migrate Access DB to SQL Server" -ForegroundColor White
    Write-Host "  [2]  IIS - Build app and deploy to local IIS" -ForegroundColor White
    Write-Host "  [3]  Tests - Run Playwright E2E tests" -ForegroundColor White
    Write-Host ""
    Write-Host "  [Q]  Quit" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  ═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
}

function Run-DatabaseMigration {
    Write-Host ""
    Write-Host "  ═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  Database Migration - Access to SQL Server" -ForegroundColor Cyan
    Write-Host "  ═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    
    $migrationPath = Join-Path $base "src\Deployment\Migration\Install.ps1"
    if (Test-Path $migrationPath) {
        Write-Host "  Running database installation..." -ForegroundColor Yellow
        & powershell -ExecutionPolicy Bypass -File $migrationPath
    } else {
        Write-Host "  ERROR: Install.ps1 not found at:" -ForegroundColor Red
        Write-Host "  $migrationPath" -ForegroundColor Gray
    }
    
    Write-Host ""
    Read-Host "  Press Enter to return to menu"
}

function Run-IISDeploy {
    Write-Host ""
    Write-Host "  ═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  IIS - Build and Deploy" -ForegroundColor Cyan
    Write-Host "  ═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    
    $deployPath = Join-Path $base "src\Deployment\Deploy.ps1"
    if (Test-Path $deployPath) {
        Write-Host "  Building and deploying to IIS..." -ForegroundColor Yellow
        Write-Host "  This will:" -ForegroundColor Gray
        Write-Host "    • Build Release version" -ForegroundColor Gray
        Write-Host "    • Create IIS App Pool and Site" -ForegroundColor Gray
        Write-Host "    • Deploy to C:\inetpub\wwwroot\Techlight.MyDesk" -ForegroundColor Gray
        Write-Host ""
        
        & powershell -ExecutionPolicy Bypass -File $deployPath
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host ""
            Write-Host "  SUCCESS! Opening http://localhost in browser..." -ForegroundColor Green
            Start-Process "http://localhost"
        } else {
            Write-Host ""
            Write-Host "  Deployment failed. Check errors above." -ForegroundColor Red
        }
    } else {
        Write-Host "  ERROR: Deploy.ps1 not found at:" -ForegroundColor Red
        Write-Host "  $deployPath" -ForegroundColor Gray
    }
    
    Write-Host ""
    Read-Host "  Press Enter to return to menu"
}

function Run-Tests {
    Write-Host ""
    Write-Host "  ═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host "  Running Playwright Tests" -ForegroundColor Cyan
    Write-Host "  ═══════════════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    
    $testPath = Join-Path $base "tests\MyDesk.PlaywrightTests"
    if (Test-Path $testPath) {
        Write-Host "  Running tests..." -ForegroundColor Yellow
        Push-Location $testPath
        dotnet test --logger "console;verbosity=detailed"
        Pop-Location
    } else {
        Write-Host "  ERROR: Test project not found at:" -ForegroundColor Red
        Write-Host "  $testPath" -ForegroundColor Gray
    }
    
    Write-Host ""
    Read-Host "  Press Enter to return to menu"
}

# Main loop
while ($true) {
    Show-Menu
    $choice = Read-Host "  Enter your choice"
    
    switch ($choice) {
        "1" { Run-DatabaseMigration }
        "2" { Run-IISDeploy }
        "3" { Run-Tests }
        "Q" { break }
        "q" { break }
        default {
            Write-Host ""
            Write-Host "  Invalid choice. Press Enter to try again..." -ForegroundColor Red
            Read-Host
        }
    }
}

Clear-Host
Write-Host ""
Write-Host "  Goodbye!" -ForegroundColor Cyan
Write-Host ""
exit 0
</invoke>
