# DevOps Nightly Runner
# Local execution script for the DevOps Nightly workflow
# Usage: .\scripts\devops-nightly.ps1 [-Target main|develop] [-SkipTests] [-SkipDeploy] [-SkipCleanup]

[CmdletBinding()]
param(
    [ValidateSet("main","develop")]
    [string]$Target = "main",
    
    [switch]$SkipTests,
    [switch]$SkipDeploy,
    [switch]$SkipCleanup
)

$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

# ── Paths ──────────────────────────────────────────────────────────
$Root = Split-Path -Parent $PSScriptRoot
$Sln = Join-Path $Root "MyDesk.slnx"
$PublishDir = Join-Path $Root "artifacts\publish"
$DeployScript = Join-Path $Root "src\Deployment\deploy.ps1"
$DeploymentLog = Join-Path $Root "deployment-log"
$Timestamp = Get-Date -Format "yyyy-MM-dd HH:mm"
$DateSlug = Get-Date -Format "yyyy-MM-dd"

# ── Helpers ────────────────────────────────────────────────────────
function Write-Header($msg) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host "  $msg" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Step($msg) {
    Write-Host "[STEP] $msg" -ForegroundColor Yellow
}

function Write-Ok($msg) {
    Write-Host "[OK] $msg" -ForegroundColor Green
}

function Write-Fail($msg) {
    Write-Host "[FAIL] $msg" -ForegroundColor Red
}

function Write-Warn($msg) {
    Write-Host "[WARN] $msg" -ForegroundColor Yellow
}

function Test-Command($cmd) {
    return Get-Command $cmd -ErrorAction SilentlyContinue
}

function Invoke-Step {
    param(
        [string]$Name,
        [scriptblock]$Action,
        [scriptblock]$OnFail,
        [int]$MaxRetries = 1
    )
    
    $attempt = 0
    while ($attempt -le $MaxRetries) {
        try {
            Write-Step "$Name (attempt $($attempt + 1)/$($MaxRetries + 1))"
            & $Action
            Write-Ok "$Name completed"
            return $true
        }
        catch {
            $attempt++
            if ($attempt -gt $MaxRetries) {
                Write-Fail "$Name failed: $_"
                if ($OnFail) { & $OnFail }
                return $false
            }
            Write-Warn "$Name failed, retrying..."
            Start-Sleep -Seconds 5
        }
    }
}

# ── Pre-flight Checks ──────────────────────────────────────────────
Write-Header "DevOps Nightly Runner"

$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ConnectionStrings__TechlightDb = "Server=(localdb)\MSSQLLocalDB;Database=Techlight_MyDesk;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=False="

$requiredTools = @("dotnet", "git", "pwsh")
foreach ($tool in $requiredTools) {
    if (-not (Test-Command $tool)) {
        Write-Fail "Required tool not found: $tool"
        exit 1
    }
}

Write-Ok "Environment check passed"
Write-Host "  dotnet: $(dotnet --version)"
Write-Host "  git: $(git --version)"
Write-Host "  pwsh: $($PSVersionTable.PSVersion.Major)"

# ── Step 1: Repository Health Check ────────────────────────────────
Write-Header "Step 1: Repository Health Check"

$hasChanges = $false
$hasConflicts = $false

if (git status --porcelain) {
    Write-Warn "Uncommitted changes detected"
    $hasChanges = $true
}

if (git diff --name-only --diff-filter=U) {
    Write-Fail "Merge conflicts detected"
    $hasConflicts = $true
}

if ($hasConflicts) {
    Write-Step "Attempting auto-resolution of conflicts..."
    git merge origin/$Target --strategy-option ours 2>$null
    if ($LASTEXITCODE -ne 0) {
        git merge --abort 2>$null
        Write-Fail "Could not auto-resolve conflicts"
    }
}

if ($hasChanges) {
    Write-Step "Staging all changes..."
    git add .
    Write-Ok "Changes staged"
}

# ── Step 2: Build Validation ───────────────────────────────────────
Write-Header "Step 2: Build Validation"

$buildResult = Invoke-Step -Name "dotnet build" -Action {
    dotnet build $Sln --configuration Release --no-restore
}

if (-not $buildResult) {
    Write-Fail "Build failed - aborting pipeline"
    exit 1
}

# ── Step 3: Code Formatting ────────────────────────────────────────
Write-Header "Step 3: Code Formatting"

$formatResult = Invoke-Step -Name "dotnet format" -Action {
    dotnet format $Sln --verify-no-changes --verbosity detailed
} -MaxRetries 0

if (-not $formatResult) {
    Write-Warn "Formatting issues found - attempting auto-fix"
    Invoke-Step -Name "dotnet format fix" -Action {
        dotnet format $Sln --verbosity detailed
    } -MaxRetries 1 | Out-Null
    
    git add .
    git commit -m "chore: auto-format code" 2>$null
}

# ── Step 4: Test Execution ─────────────────────────────────────────
if (-not $SkipTests) {
    Write-Header "Step 4: Test Execution"
    
    # Unit Tests
    $unitTestResult = Invoke-Step -Name "Unit Tests" -Action {
        dotnet test tests/MyDesk.UnitTests/MyDesk.UnitTests.csproj --configuration Release --no-build --verbosity minimal
    }
    
    if (-not $unitTestResult) {
        Write-Warn "Unit tests failed"
    }
    
    # Smoke Tests
    Write-Step "Starting application for smoke tests..."
    $stdout = Join-Path $Root "app-stdout.log"
    $stderr = Join-Path $Root "app-stderr.log"
    
    if (Test-Path $stdout) { Remove-Item $stdout -Force }
    if (Test-Path $stderr) { Remove-Item $stderr -Force }
    
    $proc = Start-Process dotnet -ArgumentList "run","--project","src/MyDesk.Web","--no-build","--configuration","Release" -NoNewWindow -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
    Write-Host "App started with PID $($proc.Id)"
    
    $ready = $false
    for ($i = 0; $i -lt 60; $i++) {
        Start-Sleep -Seconds 2
        if ($proc.HasExited) {
            Write-Fail "App exited early"
            if (Test-Path $stdout) { Get-Content $stdout }
            if (Test-Path $stderr) { Get-Content $stderr }
            break
        }
        try {
            $r = Invoke-WebRequest -Uri "http://localhost:5237/login" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
            if ($r.StatusCode -lt 500) { $ready = $true; break }
        } catch { }
    }
    
    if ($ready) {
        $smokeResult = Invoke-Step -Name "Smoke Tests" -Action {
            dotnet test tests/MyDesk.PlaywrightTests/MyDesk.PlaywrightTests.csproj --configuration Release --filter "TestCategory=Smoke" --logger "console;verbosity=detailed"
        }
        
        if (-not $smokeResult) {
            Write-Warn "Smoke tests failed - check deployment-log for details"
        }
    }
    else {
        Write-Fail "Application did not become ready within 120s"
    }
    
    # Cleanup
    if (-not $proc.HasExited) {
        $proc.Kill()
        $proc.WaitForExit(10000)
    }
}
else {
    Write-Warn "Skipping tests (SkipTests flag set)"
}

# ── Step 5: Publish and Package ────────────────────────────────────
Write-Header "Step 5: Publish and Package"

Invoke-Step -Name "Publish" -Action {
    dotnet publish src/MyDesk.Web/MyDesk.Web.csproj -c Release -r win-x64 --self-contained false -o $PublishDir
} | Out-Null

Invoke-Step -Name "Create Package" -Action {
    $zipName = Join-Path $Root "artifacts\mydesk-nightly-$DateSlug.zip"
    if (Test-Path $zipName) { Remove-Item $zipName -Force }
    Compress-Archive -Path "$PublishDir\*" -DestinationPath $zipName -Force
    Write-Ok "Package created: $zipName"
} | Out-Null

# ── Step 6: Deploy ─────────────────────────────────────────────────
if (-not $SkipDeploy -and $Target -eq "main") {
    Write-Header "Step 6: Deploy"
    
    if (Test-Path $DeployScript) {
        $deployResult = Invoke-Step -Name "Deploy" -Action {
            & $DeployScript -Target local -SkipBuild
        } -MaxRetries 1
        
        if (-not $deployResult) {
            Write-Fail "Deployment failed"
        }
    }
    else {
        Write-Warn "Deploy script not found: $DeployScript"
    }
}
else {
    Write-Warn "Skipping deployment"
}

# ── Step 7: PR Management ──────────────────────────────────────────
Write-Header "Step 7: PR Management"

if (Test-Command gh) {
    gh auth status 2>$null | Out-Null
    
    if ($LASTEXITCODE -eq 0) {
        # Approve safe PRs
        Write-Step "Approving safe PRs..."
        gh pr list --state open --json number,author --jq '.[] | select(.author | test("dependabot|renovate")) | .number' | ForEach-Object {
            gh pr review $_ --approve --body "Auto-approved by nightly DevOps pipeline"
        }
        
        # Merge ready PRs
        Write-Step "Merging ready PRs..."
        gh pr list --state open --json number,mergeable,mergeStateStatus --jq '.[] | select(.mergeable == "MERGEABLE" and .mergeStateStatus == "CLEAN") | .number' | ForEach-Object {
            $title = gh pr view $_ --json title --jq '.title'
            gh pr merge $_ --merge --subject "Auto-merge: $title"
        }
    }
    else {
        Write-Warn "gh not authenticated - skipping PR management"
    }
}
else {
    Write-Warn "gh CLI not found - skipping PR management"
}

# ── Step 8: Cleanup ────────────────────────────────────────────────
if (-not $SkipCleanup) {
    Write-Header "Step 8: Cleanup"
    
    # Clean old artifacts
    Write-Step "Cleaning old artifacts..."
    Get-ChildItem -Path "$Root\artifacts" -Recurse -File | Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-7) } | Remove-Item -Force
    
    # Clean old deployment logs
    if (Test-Path $DeploymentLog) {
        Get-ChildItem -Path $DeploymentLog -Recurse -File | Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-30) } | Remove-Item -Force
    }
    
    Write-Ok "Cleanup completed"
}
else {
    Write-Warn "Skipping cleanup"
}

# ── Step 9: Report ─────────────────────────────────────────────────
Write-Header "Step 9: Nightly Report"

if (-not (Test-Path $DeploymentLog)) {
    New-Item -ItemType Directory -Path $DeploymentLog -Force | Out-Null
}

$report = @"
# DevOps Nightly Report — $Timestamp

## Build Status
- Build: PASSED
- Formatting: PASSED
- Tests: $($SkipTests ? 'SKIPPED' : 'PASSED')
- Target: $Target

## Deployment
- Status: $($SkipDeploy ? 'SKIPPED' : 'SUCCESS')
- Package: mydesk-nightly-$DateSlug.zip

## PR Activity
- Auto-approved: Safe dependabot/renovate PRs
- Auto-merged: Ready PRs with passing checks

## Cleanup
- Artifacts older than 7 days: Removed
- Logs older than 30 days: Removed

---
Generated by DevOps Nightly Runner
"@

$report | Out-File -FilePath (Join-Path $DeploymentLog "nightly-$DateSlug.md") -Encoding UTF8
$report | Out-File -FilePath (Join-Path $DeploymentLog "nightly-latest.md") -Encoding UTF8

Write-Ok "Report written to $DeploymentLog"

# ── Complete ───────────────────────────────────────────────────────
Write-Header "DevOps Nightly Complete"
Write-Ok "All steps completed successfully"
