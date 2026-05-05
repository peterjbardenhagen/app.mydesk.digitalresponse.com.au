# Agent Guidelines for Techlight.MyDesk

This file is the **single source of truth** for any agent (AI or human) working on this codebase.
Read it completely before starting any task. Follow every section autonomously — do not ask the user
for permission to run a build, fix a compile error, or restart Kestrel. These are expected steps.

---

## Core Rules

1. **Build must be green.** After every change, run `dotnet build` and fix all errors and warnings before proceeding.
2. **Test Kestrel after significant changes.** Start the server and confirm HTTP 200 on `/login` (see Kestrel section).
3. **No workarounds for missing DB columns.** Add a migration script — never comment out code or use try/catch to silence SQL errors.
4. **Platform settings live in SQL.** Do not edit `appsettings.json` for tenant branding — update `PlatformSettingsEntities` in the database.
5. **Check recent error logs first.** Before writing any code, read `src\MyDesk.Web\Logs\errors-*.log` from the last 24 hours.
6. **Tenant isolation is sacred.** Read the *Tenant Isolation Design* section below before adding any new SQL table, service, or background job. Every tenant-scoped table is protected by SQL Row-Level Security — do not bypass it.
7. **Use Demo tenant for all testing.** All agents and Playwright tests must use **TenantId `33333333-3333-3333-3333-333333333333` (Demo MyDesk)** for testing. This keeps test data isolated from Techlight and Digital Response production tenants. The Demo tenant slug is `demo` and is accessible via `demo.localhost`, `demo.mydesk.local`, or `demo.digitalresponse.com.au`.

---

## Tenant Isolation Design

MyDesk is multi-tenant. Built-in tenants (per `TenantConstants`):

| Tenant | GUID | Default hostnames |
|--------|------|-------------------|
| Techlight | `11111111-1111-1111-1111-111111111111` | `techlight.digitalresponse.com.au`, `localhost` |
| Digital Response | `22222222-2222-2222-2222-222222222222` | `portal.digitalresponse.com.au` |
| Demo MyDesk | `33333333-3333-3333-3333-333333333333` | `demo.localhost`, `demo.mydesk.local`, `demo.digitalresponse.com.au` |

Hostnames live in `TenantHostnames`; the login page resolves the tenant from `Request.Host` so branding is correct before sign-in.

### Defence in depth — four layers

1. **SQL Row-Level Security (the primary enforcer).**
   `TenantIsolationService.EnforceAsync()` runs on startup. For every base table that has a `TenantId UNIQUEIDENTIFIER` column it:
   - Backfills any NULLs to the Techlight tenant (legacy data).
   - Sets the column to **NOT NULL** with `DEFAULT (TRY_CAST(SESSION_CONTEXT(N'TenantId') AS UNIQUEIDENTIFIER))` so any INSERT that omits TenantId still produces correctly-tagged rows.
   - Adds an FK to `Tenants(TenantId)`.
   - Drops + creates a `SECURITY POLICY sp_TenantIsolation_<Table>` with FILTER + BLOCK (BEFORE/AFTER INSERT/UPDATE/DELETE) predicates referencing the shared inline TVF `dbo.fn_TenantPredicate(@TenantId)`.

2. **The predicate function `dbo.fn_TenantPredicate`** allows a row when:
   - `SESSION_CONTEXT(N'BypassTenantIsolation') = 1` (system / migration calls), OR
   - the row's `TenantId` equals `SESSION_CONTEXT(N'TenantId')`.

3. **`DatabaseService` sets the session context on every connection.**
   `ApplyTenantSessionContextAsync` runs `sp_set_session_context` for `TenantId` and `BypassTenantIsolation` keys, sourced from `ICurrentTenantAccessor`. This means **every Dapper / ADO.NET / EF Core query inherits the filter automatically** — there is no path where a tenant-scoped query escapes RLS.

4. **EF Core global query filters** in `MyDeskDbContext.OnModelCreating` (defence in depth — RLS already handles it, but the EF layer makes navigation queries explicit and prevents accidental cross-tenant Includes).

`MyDeskDbContext.SaveChanges` also auto-stamps `TenantId` from the current accessor on Added entities that have the column.

### Tables that opt out of RLS (intentional)

Listed in `TenantIsolationService.OptOutTables`. These need cross-tenant visibility for trusted flows:

- `Tenants`, `TenantHostnames`, `PlatformSettingsEntities` — the tenant catalogue itself (resolved by hostname pre-auth).
- `UserTenants` — the membership map; read on the `/login/select-tenant` page before any tenant claim exists.
- `Users`, `UserTypes`, `UserRoles`, `RolePermissions` — global identity tables.
- `ErrorLog`, `ActivityLog`, `EmailLog`, `AiAudit`, `EntityAudit` — system-wide audit trails.

If you add a table that should be tenant-scoped, **add a `TenantId UNIQUEIDENTIFIER` column** in your migration. `TenantIsolationService` will pick it up automatically on the next startup (no code change required).

If you add a table that legitimately needs cross-tenant visibility, add it to `OptOutTables`.

### `ICurrentTenantAccessor` — three resolution sources

In priority order:

1. **`TenantImpersonation.SystemBypass()`** — explicit AsyncLocal scope. Sets `BypassIsolation = true`. Use only for startup migrations, schema enforcement, and the period before any tenant has been chosen. **Never wrap user-driven code in `SystemBypass`.**
2. **`TenantImpersonation.For(tenantId)`** — explicit tenant override (Hangfire jobs, the demo seeder). Sets the tenant id but `BypassIsolation = false` so RLS still applies for that tenant.
3. **HttpContext claims** — the `tenant_id` claim set by `AuthService.SignInAsync` after login or by `ApiKeyAuthenticationHandler` for REST API callers.

`BypassTenantIsolation` only returns `true` for explicit SystemBypass or when there is genuinely no HttpContext (background services). **Anonymous HTTP does NOT get a bypass** — the login flow uses opt-out tables only and works fine without one.

### Background jobs (Hangfire)

`ScheduledTaskExecutor.RunAsync(taskId, tenantIdRaw, _)` always wraps its body in `using TenantImpersonation.For(tenantId, …)`. The Hangfire registrar (`ScheduledTaskRegistrar`) constructs job ids as `tenant-{TenantId:N}-task-{ScheduledTaskId}` so a tenant cannot accidentally run another's recurring jobs.

### Email-redirect guard (Demo tenant)

`EmailService.GetEmailRedirectTarget()` rewrites every outbound `to`/`bcc` to `peter@bardenhagen.xyz` when the current tenant is Demo MyDesk, or when `Email:RedirectAllTo` is configured. The original recipient is preserved in the subject and a banner at the top of the body so test readers can see what would have happened in production.

### When you add a new feature — checklist

- [ ] Does the new table store user data? → add `TenantId UNIQUEIDENTIFIER` column. `TenantIsolationService` does the rest.
- [ ] Does the new background job touch tenant data? → wrap in `using TenantImpersonation.For(tenantId, ...)`.
- [ ] Does the new service open its own `SqlConnection`? → **don't.** Use `DatabaseService` so the session context is set.
- [ ] Does the new feature need to read across all tenants? → that's an admin/system concern; use `TenantImpersonation.SystemBypass()` and audit the call site carefully.

### Verifying isolation manually

```sql
-- As an authenticated user with Techlight tenant claim, the following must
-- return only Techlight rows even though all 3 tenants' rows exist:
SELECT TenantId, COUNT(*) FROM Quotes GROUP BY TenantId;

-- And the following INSERT must succeed without any TenantId in the column list,
-- because the SESSION_CONTEXT default kicks in:
INSERT INTO Quotes (Reference, ContactId, CompanyId, DivisionId, QuoteStatusId, QuoteDate, NettPriceTotal)
VALUES ('TEST', 1, 1, 1, 1, GETDATE(), 0);

-- An attempt to INSERT a row for a different tenant must fail with:
-- "The attempted operation failed because the target object 'dbo.Quotes' has a
--  block predicate that conflicts with this operation."
```

If either of the first two SELECTs returns rows from another tenant, **stop and investigate immediately** — RLS has been disabled or bypassed somehow.

### Quick references

| Concern | File |
|---------|------|
| Predicate function + policy roll-out | `src\MyDesk.Shared\Services\TenantIsolationService.cs` |
| Session-context plumbing | `src\MyDesk.Shared\Services\DatabaseService.cs` (`ApplyTenantSessionContextAsync`) |
| Tenant claim resolver | `src\MyDesk.Web\Services\CurrentTenantAccessor.cs` |
| AsyncLocal override + SystemBypass | `src\MyDesk.Shared\Services\ICurrentTenantAccessor.cs` |
| EF filters + SaveChanges stamp | `src\MyDesk.Shared\Data\MyDeskDbContext.cs` |
| Built-in tenant constants | `src\MyDesk.Shared\Models\MultiTenantModels.cs` |

---

## Environment

| Item | Value |
|------|-------|
| SQL Server | `(localdb)\MSSQLLocalDB` |
| Database | `Techlight_MyDesk` |
| Kestrel URL | `http://localhost:5237` |
| Solution | `MyDesk.slnx` |
| Web project | `src\MyDesk.Web\MyDesk.Web.csproj` |
| Shared project | `src\MyDesk.Shared\MyDesk.Shared.csproj` |
| Migrations | `src\Deployment\Migration\*.sql` |
| Logs | `src\MyDesk.Web\Logs\` |

---

## Run.bat — Menu Reference

Launch `Run.bat` from the repo root for all common operations:

| Option | Action |
|--------|--------|
| `[1]` | Apply SQL migrations (runs all `*.sql` files in order) |
| `[2]` | Build, publish and deploy to IIS |
| `[3]` | Clean bin/obj and rebuild solution |
| `[4]` | **Launch Kestrel** on `http://localhost:5237` |
| `[5]` | Run Playwright E2E tests (auto-starts Kestrel) |
| `[6]` | System Status |
| `[7]` | Open Logs folder |
| `[8]` | Open Docs folder |
| `[9]` | Kill server on port 5237 |

---

## Starting Kestrel (Autonomous)

When told to "run locally" or "test Kestrel", execute this sequence:

```powershell
# 1. Kill any existing process on the port
Get-NetTCPConnection -LocalPort 5237 -ErrorAction SilentlyContinue |
    ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }
Start-Sleep 2

# 2. Start Kestrel in the background
$log = "$env:TEMP\kestrel.log"
Start-Process dotnet -ArgumentList "run","--urls","http://localhost:5237" `
    -WorkingDirectory "src\MyDesk.Web" -WindowStyle Hidden `
    -RedirectStandardOutput $log

# 3. Poll until ready (up to 30 seconds)
for ($i = 0; $i -lt 15; $i++) {
    Start-Sleep 2
    try {
        $r = Invoke-WebRequest -Uri "http://localhost:5237/login" -UseBasicParsing -TimeoutSec 3
        Write-Host "KESTREL OK - $($r.StatusCode)"; break
    } catch { Write-Host "Attempt $($i+1)..." }
}

# 4. Check log for errors
Get-Content $log -Tail 20 | Where-Object { $_ -match "ERR|FTL|WRN" }
```

> **Important:** Use `--no-build` flag if you just built successfully, to save startup time.
> The `MudBlazor.MudPopoverProvider` debug lines in the log are NORMAL — they indicate SignalR browser connections.
> "Database tables verified successfully" = DB init worked.
> "Now listening on: http://localhost:5237" = Kestrel is ready.

---

## Recent Learnings

These are specific gotchas discovered while extending the multi-tenant platform in May 2026.
Future agents should read these before touching startup, schema upgrades, or the launcher.

### SQL batch parsing bites when ALTERing then referencing a new column

If you `ALTER TABLE ... ADD SomeColumn` and later in the **same SQL batch** reference `SomeColumn`,
SQL Server can still throw `Invalid column name 'SomeColumn'` because it parses the whole batch first.

**Example that broke startup:**
- `ExpenseService.EnsureTableAsync()` added `Currency` / `AmountAud`, then immediately ran
  `UPDATE Expenses SET AmountAud = Amount WHERE AmountAud = 0 AND Currency = 'AUD'` in the same batch.

**Correct fix:**
- Split the work into **separate `ExecuteNonQueryAsync` calls**, or wrap the later DML in `EXEC sp_executesql ...`
  and guard it with `IF EXISTS (...)` checks for the referenced columns.
- Never assume a new column is visible later in the same batch just because an `IF NOT EXISTS` guard would create it.

### Startup DB init must be resilient per-service

Do **not** wrap the entire startup init chain in one big `try/catch` and then stop. A single legacy-schema failure
can block unrelated critical services (`TenantService`, `PermissionService`, etc.) and then login fails with 401.

**Current pattern in `Program.cs`:**
- Use a local helper `SafeInit(label, work)`.
- Wrap **each** `EnsureTableAsync` / `EnsureTablesAsync` call independently.
- Log the failure and continue.

**Why this matters:**
- `ExpenseService` or `FileLibraryService` may hit old-column mismatches on a legacy DB.
- `TenantService` still needs to run afterwards or the user's `UserTenants` rows may never be created/seeded.

### Legacy schemas are real — add ALTER guards, don't just update the INSERT

If you add fields to a model/service, also add the idempotent column migration in the service's startup ensure method
or a dedicated SQL migration file.

Known legacy mismatches we already had to patch:
- `Expenses` missing `Currency`, `HasGst`, `ExchangeRate`, `AmountAud`, `AmountAudSource`
- `FileLibrary` missing `ModifiedAt` (caused DemoDataSeeder to fail when saving seeded files)
- `FileLibrary` may also be missing `IsShared`, `IsPublic`, `CreatedAt`, `FilePath`, etc. — `FileLibraryService.EnsureTableAsync()` now guards these.

Rule of thumb:
- If a service writes to a column, its startup ensure path must guarantee the column exists first.

### Startup migration runner semantics

`MigrationRunnerService` now runs at startup **before** the normal `EnsureTable` chain.

Behaviour:
- Looks in `src\Deployment\Migration\*.sql`
- Sorts by filename
- Splits on standalone `GO` lines
- Executes each batch
- Deletes the file **only if the whole file ran without error**
- Leaves failed files in place for inspection/retry

Important implications:
- Any `.sql` file dropped into that folder is treated as a **one-shot pending migration**.
- Historical files in that folder will also be consumed/deleted if they are still present and succeed.
- If you want to keep archival migration history in-repo, move old files elsewhere or change the convention.

### Run.bat conventions

`Run.bat` was corrected during this session:
- `MIGRATIONS` points to `src\Deployment\Migration` (not `MyDesk.Shared\Database\Migrations`)
- Option `[1]` defaults to:
  - Server: `(localdb)\MSSQLLocalDB`
  - Database: `Techlight_MyDesk`
- Option `[4]` launches explicitly with:
  - `dotnet run --project "%WEB%\MyDesk.Web.csproj" --no-launch-profile --urls "%URL%"`
- Port killing uses PowerShell `Get-NetTCPConnection ... -Unique` to avoid duplicate PID spam.

If you change the launcher again, keep option `[4]` deterministic and avoid relying on launch profile defaults.

### Anonymous HTTP should NOT bypass tenant isolation

`CurrentTenantAccessor.BypassTenantIsolation` was tightened:
- `true` only for explicit `TenantImpersonation.SystemBypass()` or genuinely missing `HttpContext`
- **not** for anonymous HTTP requests

This is safe because the login flow only needs opt-out tables (`Tenants`, `TenantHostnames`, `UserTenants`, etc.),
which are intentionally excluded from RLS.

### Demo tenant safety rails

The Demo tenant (`33333333-3333-3333-3333-333333333333`) is for Playwright, screenshots, and safe experimentation.

Important behaviours:
- `EmailService.GetEmailRedirectTarget()` redirects every outbound email in the Demo tenant to `peter@bardenhagen.xyz`
- `DemoDataSeeder` is idempotent; it skips if `[DEMO]` companies already exist
- The Playwright test config selects the Demo tenant by slug so tests don't operate on Techlight / Digital Response data

### Launcher testing from automation is awkward

Non-interactively driving `Run.bat` from nested shells (`cmd /c`, PowerShell, etc.) can produce misleading menu loops
because of quoting / stdin behaviour in the harness, even when the batch file itself works interactively.

For reliable validation:
- Prefer testing the **exact launch command** option `[4]` uses, rather than trying to fake menu input.
- For example, verify Kestrel with:
  - `dotnet run --project src\MyDesk.Web\MyDesk.Web.csproj --no-launch-profile --urls http://localhost:5237`
  - then poll `http://localhost:5237/login`.

---

## Login Credentials (Local Dev)

| Field | Value |
|-------|-------|
| Username | `peter bardenhagen` OR `TL0025` |
| Password | `fairmont` |
| Tenant | Techlight (auto-selected for dev), Demo (`33333333-3333-3333-3333-333333333333`) for testing |

**Important for automated tests:** Playwright tests and all agent-driven testing must use **TenantId `33333333-3333-3333-3333-333333333333` (Demo MyDesk)**. 
The test config in `tests/MyDesk.PlaywrightTests/appsettings.json` sets `"TenantSlug": "demo"` which resolves to this tenant.

If login fails with "Invalid login credentials", diagnose in this order:
1. Check `UserTenants` — user must have at least one `IsActive=1` row with a valid `TenantId`.
2. Check `Tenants` — TenantId `11111111-1111-1111-1111-111111111111` (Techlight) must exist.
3. Check error logs for the exact SQL exception.

**Quick DB fix for login:**
```sql
-- Verify tenants exist
SELECT TenantId, Name FROM Tenants;

-- Verify user 2 has tenant assignments
SELECT UserId, TenantId, Role, IsActive FROM UserTenants WHERE UserId = 2;

-- Fix: insert Techlight tenant if missing
IF NOT EXISTS (SELECT 1 FROM Tenants WHERE TenantId = '11111111-1111-1111-1111-111111111111')
    INSERT INTO Tenants (TenantId, TenantName, Name, Slug, SubscriptionPlan, MaxUsers,
                         StorageLimitMB, IsTrial, IsActive, IsSuspended, Country, UpdatedAt)
    VALUES ('11111111-1111-1111-1111-111111111111','Techlight','Techlight','techlight',
            'Enterprise',999,10240,0,1,0,'Australia',GETDATE());

-- Fix: assign user 2 to Techlight if missing
IF NOT EXISTS (SELECT 1 FROM UserTenants WHERE UserId=2 AND TenantId='11111111-1111-1111-1111-111111111111')
    INSERT INTO UserTenants (UserId, TenantId, Role, IsDefault, IsActive, AcceptedAt)
    VALUES (2,'11111111-1111-1111-1111-111111111111','Admin',1,1,GETDATE());
```

---

## Known Tenant IDs

| Tenant | GUID |
|--------|------|
| Techlight | `11111111-1111-1111-1111-111111111111` |
| Digital Response | `22222222-2222-2222-2222-222222222222` |

These are hardcoded in `TenantConstants.cs`. The database **must** have matching rows in `Tenants`.

---

## Platform Settings (Branding)

Branding is **stored in SQL**, not `appsettings.json`.  
The `appsettings.json` note says: _"PlatformSettings now live in the PlatformSettingsEntities SQL table, one row per tenant."_

To update branding for Techlight:
```sql
-- Read current settings
SELECT SettingsJson FROM PlatformSettingsEntities WHERE TenantId = '11111111-1111-1111-1111-111111111111';

-- Update settings (use a .sql file to avoid quoting issues)
UPDATE PlatformSettingsEntities
SET SettingsJson = '{"LoginLogoUrl":"/images/techlight-logo.svg",...}',
    UpdatedAt = GETDATE()
WHERE TenantId = '11111111-1111-1111-1111-111111111111';
```

> Always save complex JSON updates to a `.sql` temp file and run with `sqlcmd -i`, 
> because inline quoting in PowerShell causes Msg 102/105 syntax errors.

---

## Diagnosing Startup Failures

When Kestrel fails to start, the error appears in the log before "Application started." The most common:

### DI Scope Violation
```
Cannot consume scoped service 'DatabaseService' from singleton 'PermissionService'
```
**Fix:** Change the singleton to use `IServiceScopeFactory` instead of injecting the scoped service directly. See `PermissionService.cs` for the pattern.

### SQL Schema Mismatch
```
Invalid column name 'Name'   (Msg 207)
Operand type clash: uniqueidentifier is incompatible with int   (Msg 206)
```
**Fix:** The table exists but has wrong columns. Check `INFORMATION_SCHEMA.COLUMNS`, then either:
- Add the missing column via an ALTER TABLE migration
- Or the table has legacy schema — check `TenantService.AddMissingColumnsAsync()` and `MigrateTenantIdTypeAsync()`

### Port Already In Use
```
Failed to bind to address http://127.0.0.1:5237: address already in use
```
**Fix:** Kill the old process before starting:
```powershell
Get-NetTCPConnection -LocalPort 5237 | ForEach-Object { Stop-Process -Id $_.OwningProcess -Force }
```

### FK Constraint Violation (on startup)
```
Column 'Tenants.TenantId' is not the same data type as referencing column
```
**Fix:** The Tenants table has `INT` TenantId from legacy schema. `MigrateTenantIdTypeAsync()` in `TenantService.cs` handles this automatically on next startup — but the FK creation is now wrapped in `BEGIN TRY / END CATCH`.

---

## SQL Migration Workflow

All migrations in `src/Deployment/Migration/*.sql` are **idempotent** (`IF NOT EXISTS` guards).

### Run a single migration
```powershell
sqlcmd -S "(localdb)\MSSQLLocalDB" -d "Techlight_MyDesk" -i "src\Deployment\Migration\018_MultiTenant.sql"
```

### Run all migrations in order
```powershell
Get-ChildItem "src\Deployment\Migration\*.sql" | Sort-Object Name | ForEach-Object {
    Write-Host "=== $($_.Name) ==="
    sqlcmd -S "(localdb)\MSSQLLocalDB" -d "Techlight_MyDesk" -i $_.FullName -b
    if ($LASTEXITCODE -ne 0) { Write-Host "FAILED"; break }
}
```

### Writing a new migration
1. Name it `NNN_Description.sql` (e.g., `019_AddExpenseCategory.sql`).
2. Every statement must be guarded: `IF NOT EXISTS`, `IF COL_LENGTH() IS NULL`, `IF OBJECT_ID() IS NULL`.
3. End with `PRINT 'Migration NNN completed.'`.
4. Run it, verify, commit both the `.sql` file and any C# model changes together.

### Gotcha: Unique constraints on NULL columns
If inserting into a table with a `UNIQUE` constraint on a nullable column fails with "duplicate key NULL":
```sql
-- Find and drop it
SELECT tc.CONSTRAINT_NAME, c.COLUMN_NAME
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
JOIN INFORMATION_SCHEMA.CONSTRAINT_COLUMN_USAGE c ON tc.CONSTRAINT_NAME = c.CONSTRAINT_NAME
WHERE tc.TABLE_NAME = 'Tenants' AND tc.CONSTRAINT_TYPE = 'UNIQUE';

ALTER TABLE Tenants DROP CONSTRAINT UQ__Tenants__XXXXXXXX;
```

---

## Build & Quality Cycle (after every change)

```powershell
# 1. Build — must have 0 errors, 0 warnings
dotnet build MyDesk.slnx --nologo

# 2. Check recent error logs
Get-ChildItem "src\MyDesk.Web\Logs\errors-*.log" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 2 | Get-Content | Select-String "ERR|FTL"

# 3. Start Kestrel and verify /login returns 200
# (use the Kestrel startup block above)

# 4. Run Playwright tests (option 5 in Run.bat, or):
dotnet test tests\MyDesk.PlaywrightTests --nologo --logger "console;verbosity=normal"
```

Repeat up to **5 times** maximum. After 5 attempts without resolution, stop and document what's blocked.

---

## Playwright Tests — What They Need

Tests skip with "Login could not be completed" when:
1. Kestrel isn't running at `http://localhost:5237` — start it first.
2. Test credentials don't work — verify `peter bardenhagen` / `fairmont` works manually.
3. Tenant selection: user 2 has 3 tenants (Techlight, Digital Response, Demo), so the test must handle the `/login/select-tenant` redirect.
   `BaseTest.cs` handles this — it clicks the "Continue" button for the tenant matching `TestSettings.TenantSlug` (default: `demo`).

**All tests must run against Demo tenant** (`33333333-3333-3333-3333-333333333333`) to keep test data isolated from production tenants.

The test config is in `tests\MyDesk.PlaywrightTests\appsettings.json`:
```json
{
  "TestSettings": {
    "BaseUrl": "http://localhost:5237",
    "TestUser": { "Username": "peter bardenhagen", "Password": "fairmont" },
    "TenantSlug": "demo"
  }
}
```

---

## Self-Healing Launcher (AI-powered)

`Run.ps1` has an AI self-healing mode using **Google Gemini 2.5 Flash** (configured in `appsettings.json` under `SelfHealing.ApiKey`).

```powershell
.\Run.ps1 -SelfHealing   # Enable AI error fixing
```

On option `[4] → [1]` (Kestrel), it:
1. Captures stdout/stderr for 30 seconds after startup.
2. If it detects `ERR|Exception|Failed`, it sends the error + source file to Gemini.
3. If confidence ≥ 0.7, it patches the file automatically and restarts.

**Agents should NOT rely on self-healing** — fix errors properly. Self-healing is a safety net.

---

## Common Error → Fix Lookup

| Error | Root Cause | Fix |
|-------|-----------|-----|
| `CS8600` nullable warning | Null ref in model | Add `?` or null check |
| `Cannot consume scoped service from singleton` | DI lifetime mismatch | Use `IServiceScopeFactory` |
| `Msg 207: Invalid column name` | DB schema out of sync | Run migration or `AddMissingColumnsAsync` |
| `Msg 206: Operand type clash` | INT vs UNIQUEIDENTIFIER | Run `MigrateTenantIdTypeAsync` |
| `Msg 2627: Violation of UNIQUE KEY` | Unique constraint on NULL column | Drop the constraint, re-insert |
| `Address already in use` | Old dotnet process on port | Kill with `Get-NetTCPConnection | Stop-Process` |
| `Login → /login?error=1` | User has no tenant memberships | Insert Techlight tenant + UserTenants row |
| Broken logo on login page | `LoginLogoUrl` in SQL wrong | Update `PlatformSettingsEntities.SettingsJson` |
| Dark mode button does nothing | No `body.dark-mode` CSS rules | Add dark mode CSS to `Login.razor` `<style>` block |
| MudBlazor popover debug spam in logs | Normal SignalR circuit activity | Ignore — not an error |

---

## File Locations Reference

| Purpose | Location |
|---------|----------|
| Login page UI + styles | `src/MyDesk.Web/Components/Pages/Login.razor` |
| Tenant service + migration | `src/MyDesk.Shared/Services/TenantService.cs` |
| Permission service | `src/MyDesk.Shared/Services/PermissionService.cs` |
| Auth / cookie sign-in | `src/MyDesk.Web/Services/AuthService.cs` |
| Platform settings service | `src/MyDesk.Web/Services/PlatformSettingsService.cs` |
| DI registrations | `src/MyDesk.Web/Program.cs` |
| Tenant constants (GUIDs) | `src/MyDesk.Shared/Models/MultiTenantModels.cs` |
| wwwroot images (logos) | `src/MyDesk.Web/wwwroot/images/` |
| Playwright base class | `tests/MyDesk.PlaywrightTests/BaseTest.cs` |
| Playwright config | `tests/MyDesk.PlaywrightTests/appsettings.json` |

---

## Code Quality Standards

- No new compiler warnings. The project uses `<Nullable>enable</Nullable>`.
- DI lifetimes: `Singleton` → cannot inject `Scoped`. Use `IServiceScopeFactory` when needed.
- All SQL is written defensively: `ISNULL(col, default)`, `TRY/CATCH` for schema-optional ops.
- `PlatformSettings.Current` is always safe to read — it falls back to `appsettings.json` if DB is unavailable.
- Branding is tenant-specific. Never hardcode "Techlight" or "Digital Response" in Razor files — use `PlatformSettings.Current.CompanyName`.

---

## UI Consistency Rules

- All portals (Staff, Customer, Supplier, Login) use the CSS variables defined by `PlatformSettings`.
- Dark mode is toggled by `body.dark-mode` class. Every page with custom `<style>` must include `body.dark-mode` overrides.
- The login page dark mode preference is persisted in `localStorage` under key `mydesk_dark_mode`.
- `LoginLogoUrl` = hero panel logo (left side, dark background).
- `LoginMarkUrl` = form card mark (right side, light background).

### Popup Dialog Pattern

Use `QuickNavDialog.razor` as the reference design pattern for popup dialogs across MyDesk.

- Open dialogs through `IDialogService.ShowAsync(...)` with explicit `DialogOptions`.
- Default options should be:
  - `CloseButton = false` for command-palette / focused task dialogs unless the dialog is form-heavy
  - `CloseOnEscapeKey = true`
  - `BackdropClick = true`
  - `NoHeader = true` when the dialog supplies its own visual header/search chrome
  - `MaxWidth = MaxWidth.Small` and `FullWidth = true` unless the content genuinely needs more space
- The dialog body should own the interaction model:
  - autofocus the primary input on first render
  - support keyboard navigation (`ArrowUp`, `ArrowDown`, `Enter`, `Escape`)
  - provide a compact footer with keyboard hints when the dialog is command-oriented
- Styling pattern:
  - rounded outer shell (`border-radius: 12px` or larger)
  - a distinct top interaction row (search, title, or filter area)
  - scrollable content region in the middle
  - restrained footer/status bar at the bottom
- Visual behaviour:
  - selected/hovered rows should use a soft background treatment plus a strong left accent or inset highlight
  - avoid browser-default button/input styling inside dialogs; dialog content should look like part of the MyDesk design system
- If a dialog diverges from this pattern, there should be a specific product reason rather than convenience.

---

*Last updated: May 2026 — Powered by Digital Response*
