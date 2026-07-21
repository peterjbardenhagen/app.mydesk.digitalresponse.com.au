param(
    [int]$Hours = 672,            # 4 weeks default
    [int]$IntervalSeconds = 300   # 5 min between iterations
)

$ErrorActionPreference = "Continue"
$RepoRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
Set-Location $RepoRoot

$log = Join-Path $RepoRoot "loop.log"
$progress = Join-Path $RepoRoot "PROGRESS.md"
$blockers = Join-Path $RepoRoot "blockers.md"
$sdlc = Join-Path $RepoRoot "agentic-sdlc.md"
$defaultBranch = "main"

function Log($msg) {
    $line = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  $msg"
    Add-Content -Path $log -Value $line
    Write-Host $line
}

function Stop-Loop($reason) {
    Log "[stop] $reason"
    break
}

Log "=== MyDesk autonomous loop started (PID $pid, budget ${Hours}h) ==="
Log "[sdlc] governing doc: $sdlc"

$end = (Get-Date).AddHours($Hours)

while ((Get-Date) -lt $end) {
    # 1. Sync from origin (fast-forward only; never create merge commits)
    git fetch origin 2>&1 | ForEach-Object { Log "[fetch] $_" }
    git pull --ff-only origin $defaultBranch 2>&1 | ForEach-Object { Log "[pull] $_" }
    if ($LASTEXITCODE -ne 0) {
        Log "[pull] not fast-forwardable — branch diverged from origin/$defaultBranch. STOP trigger (divergence)."
        Stop-Loop "branch diverged from origin/$defaultBranch"
    }

    # 2. Stop trigger: DONE marker in PROGRESS.md
    if ((Test-Path $progress) -and (Select-String -Quiet -Pattern 'DONE' -Path $progress)) {
        Stop-Loop "DONE marker found in PROGRESS.md"
    }

    # 3. Stop trigger: blockers-only (no next planned task)
    if ((Test-Path $progress) -and (Select-String -Quiet -Pattern 'BLOCKERS-ONLY' -Path $progress)) {
        Stop-Loop "no next planned task — only blockers remain"
    }

    # 4. Run one iteration as a detached opencode job
    Log "[run] starting opencode iteration"
    $job = Start-Job -Name loop -ScriptBlock {
        param($RepoRoot)
        Set-Location $RepoRoot
        & opencode run "Read PROGRESS.md and blockers.md. Do the next non-blocked task from Planning/PHASE-7-MOBILE-APPS.md and the mobile app under Mobile/. Keep the build green: run 'cd Mobile; npm install; npm run type-check; npm test' before finishing. Write progress to PROGRESS.md, append hard blockers to blockers.md, then git add -A and commit per phase. If PROGRESS.md says DONE or BLOCKERS-ONLY, output that and change nothing. Stop only on agentic-sdlc.md triggers." 2>&1
    } -ArgumentList $RepoRoot

    $done = Wait-Job $job -Timeout 14400
    if (-not $done) {
        Log "[run] iteration timed out after 4h — stopping job"
        Stop-Job $job
    }
    Receive-Job $job | ForEach-Object { Log "[opencode] $_" }
    Remove-Job $job -Force -ErrorAction SilentlyContinue

    # 5. Stop trigger: build failure persisted after iteration
    if ((Test-Path $progress) -and (Select-String -Quiet -Pattern 'BUILD-FAILED' -Path $progress)) {
        Stop-Loop "build/test failed and not recoverable in-iteration"
    }

    Log "[run] iteration complete"
    Start-Sleep -Seconds $IntervalSeconds
}

Log "=== MyDesk autonomous loop finished (PID $pid) ==="
