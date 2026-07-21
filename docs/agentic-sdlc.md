# MyDesk Autonomous Agentic SDLC

**Version:** 1.0
**Last Updated:** July 2026
**Status:** Active — bind all agent sessions to this lifecycle

---

## Purpose

This document defines the **autonomous, non-stop agentic development lifecycle** for MyDesk. When the user invokes "autonomous / YOLO / work until done" mode, agents operate continuously without waiting for human input, gating only on hard environmental blockers. The goal: complete everything that is *planned and codeable* before the next human check-in.

---

## Operating Principles

1. **No waiting.** Do not pause for the user mid-execution. Continue through the plan. If human input is genuinely required to unblock, record it and move to the next non-blocking task.
2. **TUI YOLO.** Assume full authority to read, edit, create, test, commit, branch, open and merge PRs without asking.
3. **Blockers go to `blockers.md`.** A blocker is a hard environmental gap that cannot be resolved by code — missing credentials, accounts, hardware (Xcode/Mac/iOS SDK/Android SDK), or an undecided external dependency. When blocked: append to `blockers.md`, then pick the next non-blocking task. Never stop the run.
4. **Commit at least every phase.** Each logical phase or workstream completion is committed atomically with a descriptive message. Never accumulate unbounded WIP.
5. **Auto-approve PRs/merges to default branch.** Open PRs for reviewable units of work and auto-merge into the default branch (`main`) without waiting for human approval.
6. **Get latest first.** `git fetch origin` and reconcile with `origin/main` before starting work and before each merge.

---

## Execution Loop (non-stop)

```
while planned_work_remaining:
    task = next_task_from_plan()
    if task.blocked_by_hard_env_gap:
        log_to_blockers_md(task, gap)
        continue
    do_task(task)
    if task.completes_phase_or_workstream:
        commit(f"feat(phaseN): <summary>")
    push_and_open_pr()
    auto_merge_pr_to_default_branch()
reconcile_origin_main()
report(summary + blockers.md)
```

### Reconciliation rules
- Before work: `git fetch origin` then `git pull --rebase origin main` (or merge if rebase conflicts are non-trivial).
- Feature work lands on a branch, becomes a PR, auto-merges to `main`.
- After every merge: ensure local `main` is current with `origin/main`.

---

## Blocker Classification

| Type | Example | Action |
|------|---------|--------|
| Hard env gap | No Apple Developer account, no Xcode/Mac, no Android SDK, no Firebase/Sentry project, missing API keys | Log to `blockers.md`, skip to next task |
| External decision pending | Framework choice not made, third-party model not selected | Log to `blockers.md` with recommended default, proceed with sensible default |
| Codeable | Any feature implementation, tests, docs, CI config | Complete autonomously |

**Never treat a codeable task as a blocker.** Only hardware, accounts, credentials, and undecided external dependencies block.

---

## What "Everything Planned" Means

Scope = phases with a committed plan and start date in `Planning/`:
- Phases 1–6: complete (merged to `main`). Verify, do not rebuild.
- Phase 7 (Mobile Apps): the active planned work. Build all 140 tasks that are codeable; hardware/store-upload/device-test steps are logged to `blockers.md`.
- Phases 8–11: marked "Planned/Future" with no committed plan or start date → out of scope until a plan exists.

---

## Outputs & Reporting

- `blockers.md` at repo root: cumulative list of hard blockers with what's needed to unblock and impact.
- End-of-run summary to the user: what was completed, what was committed/merged, and the blocker list.
- Planning docs (`PHASE-7-MOBILE-APPS.md`, `BACKLOG.md`, etc.) updated to reflect true completion %.

---

## Integration with AGENTS.md

This lifecycle overrides the default "request before acting" stances in `AGENTS.md` and `CLAUDE.md` **only when the user explicitly enables autonomous/YOLO mode**. In normal mode, follow the standard review-and-wait workflow.
