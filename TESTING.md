# Testing Guide

This document explains the test strategy for AI agents and developers.
AI agents should read this before making changes that could affect test outcomes,
and should consult `deployment-log/latest.md` for the current state of known failures.

---

## Two-Tier Test Strategy

### Tier 1 — Smoke Tests (automatic, fast)

- **Trigger:** every push and pull request
- **Workflow:** `.github/workflows/playwright-tests.yml`
- **What runs:** 3 critical Playwright tests tagged `[Category("Smoke")]`
  - `Login_Page_Loads_Successfully` — app is up and serves the login page
  - `Login_With_Valid_Credentials_Succeeds` — authentication works end-to-end
  - `Dashboard_Loads_With_KPI_Cards` — post-login core UI renders correctly
- **Expected duration:** 3–5 minutes
- **Blocks merge:** yes — a smoke failure must be fixed before merging

### Tier 2 — Full Suite (manual, non-blocking)

- **Trigger:** GitHub Actions → **Playwright Full Suite** → Run workflow
- **Workflow:** `.github/workflows/playwright-full.yml`
- **What runs:** all ~50 Playwright tests across all modules
- **Expected duration:** 20–30 minutes
- **Blocks merge:** no — results are informational and stored in `deployment-log/`

---

## Tagging a Test as Smoke

Add `[Category("Smoke")]` to any NUnit test method that should run in CI:

```csharp
[Test, Category("Smoke")]
public async Task MyTest_ShouldPass()
```

Keep the smoke suite small (under 10 tests). It must complete in under 5 minutes.

---

## deployment-log/

After every full suite run the workflow commits two files:

| File | Purpose |
|------|---------|
| `deployment-log/latest.md` | Most recent full suite result — always up to date |
| `deployment-log/YYYY-MM-DD-HHmm-full-suite.md` | Dated history entry |

### AI Agent Instructions

When asked to investigate test failures or self-heal:

1. **Read `deployment-log/latest.md`** — it lists every failed test, its error message, and the GitHub Actions run URL.
2. **Find the failing test file** in `tests/MyDesk.PlaywrightTests/Tests/`.
3. **Find the component or page being tested** in `src/MyDesk.Web/Components/Pages/`.
4. **Diagnose the root cause** — is it a broken selector, a missing element, a changed route, or a real bug?
5. **Fix the underlying issue** in production code (prefer fixing the code over fixing the test).
6. **If the test is stale** (testing something that no longer exists), update or remove the test.
7. **Run smoke tests** by pushing a commit — watch CI. A green smoke run means the fix is safe.
8. **Trigger a full suite run** via Actions → Playwright Full Suite to confirm all failures are resolved.
9. **Do not commit a fix that makes smoke tests pass but leaves full suite failures unrelated to your change** — document them in a PR comment instead.

---

## Test Files

```
tests/MyDesk.PlaywrightTests/
├── BaseTest.cs                  — shared setup, login helper, server reachability check
├── BaseModuleCrudTest.cs        — shared CRUD test helpers
├── TestSettings.cs              — base URL, credentials (from env/appsettings)
└── Tests/
    ├── LoginTests.cs            ← Smoke: Login_Page_Loads_Successfully, Login_With_Valid_Credentials_Succeeds
    ├── DashboardTests.cs        ← Smoke: Dashboard_Loads_With_KPI_Cards
    ├── NavigationTests.cs
    ├── ContactsTests.cs
    ├── CompaniesTests.cs
    ├── InvoicesTests.cs
    ├── QuotesTests.cs
    ├── ProductsTests.cs
    ├── JobOrdersTests.cs
    ├── PurchaseOrdersTests.cs
    ├── AskAITests.cs
    ├── ProfileTests.cs
    ├── AccessibilityTests.cs
    ├── EndToEndWorkflowTests.cs
    ├── DemoTenantTests.cs
    └── Crud/ModulesCrudTests.cs
```

---

## CI Environment

| Setting | Value |
|---------|-------|
| Runner | `windows-latest` |
| Database | LocalDB `MSSQLLocalDB` → `Techlight_MyDesk` (created fresh each run) |
| App URL | `http://localhost:5237` |
| `ASPNETCORE_ENVIRONMENT` | `Development` |
| Health check | `GET /login` — must return HTTP < 500 within 120 s |

---

## Triggering the Full Suite

1. Go to **GitHub → Actions → Playwright Full Suite**
2. Click **Run workflow** → **Run workflow**
3. Wait 20–30 min — it does NOT block any PR
4. Results appear in `deployment-log/latest.md` within a minute of completion
