<#
.SYNOPSIS
    Publish and deploy MyDesk to local IIS and/or svr1 via W: drive.

.PARAMETER Target
    local  – deploy to C:\inetpub\wwwroot\app.mydesk.digitalresponse.com.au
    svr1   – deploy to W:\app.mydesk.digitalresponse.com.au  (W: = \\svr1\wwwroot)
    both   – deploy to both

.PARAMETER SkipBuild
    Skip dotnet publish and reuse the last publish output in %TEMP%\mydesk-publish.

.EXAMPLE
    .\deploy.ps1                   # local only
    .\deploy.ps1 -Target svr1
    .\deploy.ps1 -Target both
    .\deploy.ps1 -Target svr1 -SkipBuild
#>

[CmdletBinding()]
param(
    [ValidateSet("local","svr1","both")]
    [string]$Target = "local",

    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"

# ── Paths ──────────────────────────────────────────────────────────
$RepoRoot   = $PSScriptRoot
$Project    = Join-Path $RepoRoot "src\MyDesk.Web\MyDesk.Web.csproj"
$PublishDir = Join-Path $env:TEMP "mydesk-publish"

$IisPaths = @{
    "local" = "C:\inetpub\wwwroot\app.mydesk.digitalresponse.com.au"
    "svr1"  = "W:\app.mydesk.digitalresponse.com.au"
}

# ── Helpers ────────────────────────────────────────────────────────
function Write-Step([string]$msg) {
    Write-Host "`n► $msg" -ForegroundColor Cyan
}
function Write-Ok([string]$msg) {
    Write-Host "  ✓ $msg" -ForegroundColor Green
}
function Write-Warn([string]$msg) {
    Write-Host "  ⚠ $msg" -ForegroundColor Yellow
}

# ── Pre-flight checks ──────────────────────────────────────────────
if ($Target -in "svr1","both") {
    if (-not (Test-Path "W:\")) {
        throw "W: drive is not mapped. Map \\svr1\wwwroot to W: before deploying to svr1."
    }
    if (-not (Test-Path $IisPaths["svr1"])) {
        throw "Destination not found: $($IisPaths['svr1'])`nCheck that W: points to the right share and the site folder exists."
    }
}
if ($Target -in "local","both") {
    if (-not (Test-Path $IisPaths["local"])) {
        throw "Destination not found: $($IisPaths['local'])`nCreate the IIS site folder first."
    }
}

# ── Build / Publish ────────────────────────────────────────────────
if ($SkipBuild) {
    if (-not (Test-Path $PublishDir)) {
        throw "-SkipBuild specified but no publish output found at: $PublishDir"
    }
    Write-Warn "Skipping build — reusing $PublishDir"
} else {
    Write-Step "Publishing $Project → $PublishDir"
    if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }

    dotnet publish $Project -c Release -o $PublishDir --nologo
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed (exit code $LASTEXITCODE)" }
    Write-Ok "Publish complete"
}

# ── Deploy function ────────────────────────────────────────────────
function Deploy-Site([string]$name, [string]$dest) {
    Write-Step "Deploying to $name  ($dest)"

    $offline = Join-Path $dest "app_offline.htm"

    try {
        # 1. Take site offline — ANCM detects this and shuts the process down
        Set-Content $offline `
            -Value "<html><body style='font-family:sans-serif;padding:2em'><h2>&#9881; Updating — back in a moment.</h2></body></html>" `
            -Encoding UTF8
        Start-Sleep -Seconds 3    # give ANCM time to exit the app process

        # 2. Robocopy — /MIR mirrors (adds+updates+deletes), skips app_offline.htm and log folders
        $roboArgs = @(
            $PublishDir, $dest,
            "/MIR",          # mirror: add new, update changed, delete removed
            "/R:3",          # retry 3 times on locked files
            "/W:2",          # wait 2s between retries
            "/NFL","/NDL","/NJH","/NJS","/NC","/NS","/NP",  # quiet output
            "/XF","app_offline.htm",  # never overwrite the offline page we put there
            "/XD","logs","DataProtection-Keys"  # preserve log files and DPAPI keys
        )
        robocopy @roboArgs

        # robocopy exit codes: 0=no change, 1=copied ok, 2=extra files deleted, 3=both
        # codes 4+ indicate warnings/errors
        if ($LASTEXITCODE -ge 8) {
            throw "robocopy reported errors (exit code $LASTEXITCODE)"
        }

        Write-Ok "$name deployment complete"
    }
    catch {
        Write-Host "  ✗ $name deployment FAILED: $_" -ForegroundColor Red
        throw
    }
    finally {
        # Always remove app_offline.htm so the site comes back up
        if (Test-Path $offline) {
            Remove-Item $offline -Force
            Write-Ok "$name is back online"
        }
    }
}

# ── Run deployments ────────────────────────────────────────────────
$deployList = if ($Target -eq "both") { @("local","svr1") } else { @($Target) }

foreach ($t in $deployList) {
    Deploy-Site $t $IisPaths[$t]
}

# ── Cleanup publish dir ────────────────────────────────────────────
# Keep it so -SkipBuild works on the next run; it's in TEMP so OS cleans it eventually.
Write-Host "`n✓ All done." -ForegroundColor Green
