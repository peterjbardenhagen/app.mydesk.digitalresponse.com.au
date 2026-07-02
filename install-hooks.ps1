<#
.SYNOPSIS
    Installs git hooks so that:
      - committing to main  → deploys to local IIS (Pb-legion)
      - pushing to origin   → deploys to svr1 via W: drive

    Run once from the repo root. Hooks are written to .git\hooks\ (not committed).
#>

$hooksDir = Join-Path $PSScriptRoot ".git\hooks"

if (-not (Test-Path $hooksDir)) {
    throw "Not a git repo or .git\hooks doesn't exist: $hooksDir"
}

$repoRoot = $PSScriptRoot -replace '\\','/'   # forward slashes for bash shebang paths

# ── post-commit: deploy local after any commit to main ────────────
$postCommit = @"
#!/bin/sh
# Deploy to local IIS after committing to main
branch=`$(git rev-parse --abbrev-ref HEAD)
if [ "`$branch" = "main" ]; then
    echo "[hook] Deploying to local IIS..."
    powershell.exe -NonInteractive -ExecutionPolicy Bypass \
        -File "$(Join-Path $PSScriptRoot 'deploy.ps1' -Resolve | ForEach-Object { $_ -replace '\\','\\\\' })" \
        -Target local
    if [ `$? -ne 0 ]; then
        echo "[hook] WARNING: local deploy failed — commit still saved."
    fi
fi
"@

# ── pre-push: deploy to svr1 when pushing main to origin ──────────
# Note: pre-push receives lines on stdin: <local-ref> <local-sha> <remote-ref> <remote-sha>
# We deploy BEFORE the push so the server is up-to-date; push always proceeds (exit 0).
$prePush = @"
#!/bin/sh
# Deploy to svr1 via W: when pushing main to origin
remote="`$1"
if [ "`$remote" = "origin" ]; then
    while read local_ref local_sha remote_ref remote_sha; do
        if echo "`$local_ref" | grep -q "refs/heads/main"; then
            echo "[hook] Pushing main → origin. Deploying to svr1..."
            powershell.exe -NonInteractive -ExecutionPolicy Bypass \
                -File "$(Join-Path $PSScriptRoot 'deploy.ps1' -Resolve | ForEach-Object { $_ -replace '\\','\\\\' })" \
                -Target svr1 -SkipBuild
            if [ `$? -ne 0 ]; then
                echo "[hook] WARNING: svr1 deploy failed — push continuing anyway."
            fi
        fi
    done
fi
exit 0
"@

# Write hooks
$postCommitPath = Join-Path $hooksDir "post-commit"
$prePushPath    = Join-Path $hooksDir "pre-push"

Set-Content $postCommitPath -Value $postCommit -Encoding UTF8 -NoNewline
Set-Content $prePushPath    -Value $prePush    -Encoding UTF8 -NoNewline

Write-Host "✓ Hooks installed:" -ForegroundColor Green
Write-Host "    $postCommitPath" -ForegroundColor Gray
Write-Host "    $prePushPath"    -ForegroundColor Gray
Write-Host ""
Write-Host "Behaviour:"
Write-Host "  git commit (on main)  → deploys to local IIS"
Write-Host "  git push origin main  → deploys to svr1 via W: drive (reuses last build)"
Write-Host ""
Write-Host "Note: W: must be mapped to \\svr1\wwwroot before pushing."
