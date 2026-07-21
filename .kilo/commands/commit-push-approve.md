# Commit, Push, and Approve PRs

Reusable workflow to commit all changes, push to remote, and approve all open pull requests.

## Usage

Run this skill when you need to:
- Commit all pending changes
- Push to the remote repository
- Approve all open pull requests

## Steps

### 1. Commit All Changes

```bash
git status
git add .
git commit -m "chore: commit all pending changes"
```

### 2. Push to Remote

```bash
git push origin HEAD
```

If on main branch specifically:
```bash
git push origin main
```

### 3. Approve All Open PRs

```bash
gh pr list --state open --json number --jq '.[].number' | while read pr; do
  gh pr review "$pr" --approve --body "Approving PR #$pr"
done
```

## Notes

- Requires `gh` CLI to be authenticated
- Will approve all open PRs regardless of author
- For dependabot PRs, consider adding a specific approval message
- Always review PRs before approving in production environments
