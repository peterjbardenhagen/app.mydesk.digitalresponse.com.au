# DevOps Nightly Task

Reusable autonomous workflow for end-to-end repository hygiene, validation, build, test, PR management, deployment, and self-healing.

## When to Use

- Nightly CI/CD pipeline automation
- Pre-deployment validation
- Repository maintenance and cleanup
- Automated PR triage and merge

## Pre-flight Checks

```bash
# 1. Verify environment
dotnet --version
git --version
gh --version
pwsh --version

# 2. Verify authentication
gh auth status
git config user.email
git config user.name
```

If any tool is missing, log error and abort with actionable message.

## Step 1: Repository Health Check

```bash
# Check for uncommitted changes
git status --porcelain

# Check for merge conflicts
git diff --name-only --diff-filter=U

# Check branch status
git branch -vv
```

**Self-heal:** If merge conflicts exist, attempt auto-resolution:
```bash
git merge origin/main --strategy-option ours || git merge --abort
```

## Step 2: Standards Enforcement

Run all checks from `CODE-GUIDELINES.md`, `DELIVERY-CHECKLIST.md`, and `SECURITY.md`:

### 2.1 Build Validation
```bash
dotnet build MyDesk.slnx --configuration Release --no-restore
```
- Must succeed with zero warnings and zero errors
- No new analyzer warnings introduced
- Self-heal: If build fails, identify error, fix if auto-fixable, else log and abort

### 2.2 Code Formatting
```bash
dotnet format MyDesk.slnx --verify-no-changes
```
- All code must be formatted per `.editorconfig` / IDE defaults
- Self-heal: Run `dotnet format` and auto-commit fixes

### 2.3 Security Scan
```bash
# Check for hardcoded secrets
git diff --cached | grep -iE "(password|secret|key|token|connectionstring)" | grep -v "example\|sample\|test" || true

# Check for SQL injection patterns in changed files
git diff --cached --name-only | xargs grep -l "SELECT.*FROM" | xargs grep -n "string\.Format\|Concat\|+ '" || true
```

**Self-heal:** Flag violations, create issue, do not block merge for false positives.

### 2.4 Tenant Isolation Review
```bash
# Verify all SQL queries filter by TenantId
git diff --cached --name-only -- "*.cs" | xargs grep -l "TenantId" | xargs grep -c "WHERE.*TenantId" || true
```

## Step 3: Test Execution

### 3.1 Unit Tests
```bash
dotnet test tests/MyDesk.UnitTests/MyDesk.UnitTests.csproj --configuration Release --no-build --verbosity minimal
```

### 3.2 Smoke Tests (Playwright)
```bash
# Start app in background
$proc = Start-Process dotnet -ArgumentList "run","--project","src/MyDesk.Web","--no-build","--configuration","Release" -NoNewWindow -PassThru

# Wait for readiness (poll /login for 120s)
# ... (see playwright-tests.yml pattern)

# Run smoke tests
dotnet test tests/MyDesk.PlaywrightTests/MyDesk.PlaywrightTests.csproj --configuration Release --filter "TestCategory=Smoke"

# Cleanup
Stop-Process -Id $proc.Id -Force
```

**Self-heal:**
- If smoke tests fail: Read `deployment-log/latest.md`, diagnose, fix code, re-run
- If app fails to start: Check logs, auto-restart, retry once
- If database missing: Apply migrations from `src/Deployment/Migration/`

## Step 4: PR Management

### 4.1 Auto-approve Safe PRs
```bash
# Approve dependabot and trivial PRs
gh pr list --state open --json number,author,title --jq '.[] | select(.author | test("dependabot|renovate")) | .number' | while read pr; do
  gh pr review "$pr" --approve --body "Auto-approved: dependency update"
done
```

### 4.2 Merge Conflict Resolution
```bash
# For each open PR
for pr in $(gh pr list --state open --json number --jq '.[].number'); do
  state=$(gh pr view "$pr" --json mergeStateStatus --jq '.mergeStateStatus')
  if [ "$state" = "CONFLICTING" ]; then
    echo "PR #$pr has conflicts - attempting auto-resolution"
    gh pr checkout "$pr"
    git fetch origin main
    git merge origin/main --strategy-option ours || git merge --abort
    git push origin HEAD
    gh pr close "$pr" --comment "Auto-closed due to unresolvable conflicts. Please rebase."
  fi
done
```

### 4.3 Merge Ready PRs
```bash
gh pr list --state open --json number,mergeable,mergeStateStatus --jq '.[] | select(.mergeable == "MERGEABLE" and .mergeStateStatus == "CLEAN") | .number' | while read pr; do
  gh pr merge "$pr" --merge --subject "Auto-merge: $(gh pr view "$pr" --json title --jq '.title')"
done
```

**Self-heal:** If merge fails, attempt rebase or squash, retry once.

## Step 5: Cleanup

### 5.1 Stale Branches
```bash
# List branches not merged to main for >30 days
for branch in $(git branch -r --format='%(refname:short)' | grep -v "main\|master\|develop"); do
  last_commit=$(git log -1 --format=%ci "$branch" 2>/dev/null)
  if [ -n "$last_commit" ]; then
    age_days=$(( ( $(date +%s) - $(date -d "$last_commit" +%s) ) / 86400 ))
    if [ $age_days -gt 30 ]; then
      echo "Stale branch: $branch (${age_days}d)"
      # Optional: delete if not protected
      # git push origin --delete "$branch"
    fi
  fi
done
```

### 5.2 Artifact Cleanup
```bash
# Remove old publish artifacts
find . -type d -name "artifacts" -o -name "publish" | while read dir; do
  find "$dir" -type f -mtime +7 -delete 2>/dev/null || true
done

# Remove old test results
find deployment-log -type f -mtime +30 -delete 2>/dev/null || true
```

### 5.3 Dependency Cleanup
```bash
# Check for unused packages (nuget)
dotnet list package --unused 2>/dev/null | grep "warning" || true

# Check for vulnerable packages
dotnet list package --vulnerable 2>/dev/null || true
```

**Self-heal:** Create issues for findings, auto-fix formatting, auto-update if minor version.

## Step 6: Build and Deploy

### 6.1 Publish
```bash
dotnet publish src/MyDesk.Web/MyDesk.Web.csproj -c Release -r win-x64 --self-contained false -o ./artifacts/publish
```

### 6.2 Package
```bash
Compress-Archive -Path ./artifacts/publish/* -DestinationPath ./artifacts/mydesk-nightly-$(Get-Date -Format 'yyyyMMdd').zip -Force
```

### 6.3 Deploy (if on main branch)
```bash
if [ "$(git branch --show-current)" = "main" ]; then
  # Deploy using deploy.ps1
  pwsh src/Deployment/deploy.ps1 -Target local -SkipBuild
fi
```

**Self-heal:**
- If publish fails: Clean bin/obj, retry once
- If deploy fails: Auto-rollback, alert via GitHub issue
- If IIS down: Start IIS, retry deployment

## Step 7: Documentation Sync

```bash
# Verify documentation references current code
grep -r "Migration 0" docs/ --include="*.md" | while read line; do
  migration=$(echo "$line" | grep -oE "Migration [0-9]+" | head -1)
  if [ -n "$migration" ]; then
    num=$(echo "$migration" | grep -oE "[0-9]+")
    if [ ! -f "src/Deployment/Migration/${num}_*.sql" ]; then
      echo "WARNING: Referenced migration not found: $migration"
    fi
  fi
done
```

## Step 8: Reporting

Create nightly report at `deployment-log/nightly-$(Get-Date -Format 'yyyy-MM-dd').md`:

```markdown
# DevOps Nightly Report — $(date)

## Build Status
- Build: PASSED/FAILED
- Tests: X passed, Y failed
- Formatting: PASSED/FAILED

## PR Activity
- Merged: PR #X, #Y
- Approved: PR #Z
- Conflicts: PR #A (auto-closed)

## Cleanup
- Stale branches removed: N
- Artifacts cleaned: M MB

## Security
- Vulnerabilities: N
- Hardcoded secrets: N

## Deployment
- Target: main
- Status: SUCCESS/FAILED
- Package: mydesk-nightly-YYYYMMDD.zip

## Issues Created
- #X: description
```

## Execution

```bash
#!/bin/bash
set -euo pipefail

# Run all steps
preflight_checks
repository_health_check
standards_enforcement
test_execution
pr_management
cleanup
build_and_deploy
documentation_sync
reporting
```

## Error Handling & Self-Healing

| Failure | Auto-Action | Escalation |
|---------|-------------|------------|
| Build fails | Clean + retry once | Create GitHub issue |
| Tests fail | Auto-fix if trivial, else skip | Log to deployment-log, create issue |
| Merge conflict | Attempt resolution, else close PR | Comment on PR |
| Deploy fails | Rollback, retry once | Create incident issue |
| Lint fails | Auto-format, commit, retry | Create issue if persists |

## Configuration

Environment variables:
- `DEPLOY_TARGET`: `local`, `svr1`, or `both` (default: `local`)
- `SKIP_TESTS`: Set to `1` to skip test execution
- `SKIP_DEPLOY`: Set to `1` to skip deployment
- `AUTO_MERGE`: Set to `1` to enable auto-merging (default: `1`)
- `AUTO_CLEANUP`: Set to `1` to enable cleanup (default: `1`)

## Scheduling

GitHub Actions schedule (add to `.github/workflows/devops-nightly.yml`):

```yaml
on:
  schedule:
    - cron: '0 2 * * *'  # 2 AM UTC daily
  workflow_dispatch:
```

## References

- `CODE-GUIDELINES.md` - Code standards
- `DELIVERY-CHECKLIST.md` - Pre-deployment checklist
- `TESTING.md` - Test strategy
- `SECURITY.md` - Security requirements
- `CLAUDE.md` - Development workflow
- `deploy.ps1` - Deployment script
- `Run.ps1` - Local development runner
