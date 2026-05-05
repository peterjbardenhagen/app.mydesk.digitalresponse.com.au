# DR MyDesk — Project Reorganization

**Date:** April 21, 2026  
**Version:** 3.0.0

---

## Phase 2 of legacy port (3 May 2026)

Final three legacy modules ported into the Blazor app — multi-level approval
chain (Quote/PO), timesheet line approval + missing-timesheets report, and a
charts-driven Sales Reports dashboard. No database schema changes — new state
lives in `List<T>` inside the corresponding services.

### Module A: Multi-level Quote/PO approval
- **NEW** `MyDesk.Shared/Models/ApprovalModels.cs` — `UserApprovalSettings`
  (`LineManagerCode`, `IsCapExApprover`), `ApprovalEntry`, `PendingApprovalItem`,
  `TimesheetMissingDto`, plus the Sales-Reports DTOs.
- **NEW** `MyDesk.Shared/Services/ApprovalService.cs` — in-memory chain
  registry: settings store, approval-entry log, `NextLineManager`,
  `FindCapExApprover`, `PendingFor(userCode)`, `StalledApprovals(hours)`.
  Seeded with 3 demo users (PB → MD, JD → MD, MD = top + CapEx approver).
- **EXTENDED** `QuoteService.cs` — added optional `ApprovalService` ctor dep,
  `GetNextQuoteApproverAsync`, `IsQuoteFullyApprovedAsync`,
  `GetPendingApprovalQuotesAsync`. `ApproveAsync` now records levels and only
  flips status to "Fully Approved" when the chain is complete (else status 9).
- **EXTENDED** `PurchaseOrderService.cs` — same pattern + `IsCapExAsync`
  (true when total > $5,000 OR a line is flagged CapEx) and
  `GetNextPoApproverAsync(poId, userCode, hasCapEx)`.
- **NEW** `MyDesk.Web/Components/Pages/PendingApprovals.razor` (`/approvals/pending`)
  — split view of pending Quote and PO approvals for the signed-in user with
  Approve/Decline actions and links back to the view pages.
- **NEW** "Approvals stalled > 1 day" widget on `Dashboard.razor` — lists items
  approved by someone but not progressed by the next approver in 24h.

### Module B: Timesheet line approval + Missing Timesheets
- **EXTENDED** `TimesheetService.cs` — `ApproveLineAsync(tsId, lineId, code)`
  (in-memory `TimesheetLineApproval` list), `GetLineApprovals`,
  `GetMissingTimesheetsAsync(weekEnding)`, `GetSubmittedForManagerAsync`
  (uses `ApprovalService` to find direct reports).
- **NEW** `MyDesk.Web/Components/Pages/TimesheetApprove.razor`
  (`/timesheets/approve`) — submitted timesheets from direct reports, per-line
  checkboxes, "Approve all" / per-timesheet Approve/Reject, week filter.
- **NEW** `MyDesk.Web/Components/Pages/TimesheetMissing.razor`
  (`/timesheets/missing`) — users with no submitted timesheet for the chosen
  week; "Send reminder" snackbar action per user + bulk "Email all".

### Module C: Sales Reports dashboard
- **NEW** `MyDesk.Shared/Services/SalesReportsService.cs` — five aggregation
  methods returning the Sales Reports DTOs:
  `GetSalesByMonthAsync(monthsBack)`, `GetSalesByRepAsync(from,to)`,
  `GetSalesByDivisionAsync(from,to)`, `GetYearOnYearAsync()`,
  `GetPendingVsWonAsync(from,to)`. Tolerates missing tables (returns empty).
- **NEW** `MyDesk.Web/Components/Pages/Reports/SalesReports.razor`
  (`/reports/sales`) — five MudBlazor charts:
  - Bar: Sales by Month (quotes vs invoices)
  - Pie + side table: Sales by Rep
  - Donut: Sales by Division
  - Line: Year-on-Year (current vs previous calendar year)
  - StackedBar: Pending vs Won quotes (monthly)

### Other changes
- **MODIFIED** `Program.cs` — registered `ApprovalService` and
  `SalesReportsService` as scoped (Phase 2 block).
- **MODIFIED** `NavMenu.razor` — added Operations links: Approve Timesheets,
  Missing Timesheets, Pending Approvals; Insights link: Sales Reports.
- **MODIFIED** `SITEMAP.md` — listed all four new routes.

---

## Phase 1 of legacy port — May 2026

Replaced the placeholder stubs at `/rfq`, `/sales-projects` and `/call-reports`
with first-class Blazor pages backed by in-memory services. No database schema
changes — services use private `static List<T>` storage seeded with example
records.

### RFQ (Request For Quote) — `/rfq`
- **NEW** `MyDesk.Shared/Models/RfqModels.cs` — `Rfq`, `RfqResponse`, `RfqStatus`
  (Draft / Sent / Responded / Awarded / Cancelled).
- **NEW** `MyDesk.Shared/Services/RfqService.cs` — full CRUD + supplier
  responses + winner selection + generate-from-quote.
- **NEW** `MyDesk.Web/Components/Pages/Rfqs/RfqList.razor` (`/rfq`) — MudDataGrid
  list with status badges, supplier/response counts, View / Edit / Compare /
  Delete actions.
- **NEW** `MyDesk.Web/Components/Pages/Rfqs/RfqEdit.razor` (`/rfq/edit/{id?}`).
- **NEW** `MyDesk.Web/Components/Pages/Rfqs/RfqView.razor` (`/rfq/view/{id}`)
  — details + add-supplier-response panel + responses table with Award.
- **NEW** `MyDesk.Web/Components/Pages/Rfqs/RfqCompare.razor`
  (`/rfq/compare/{id}`) — side-by-side ranked comparison with cheapest /
  fastest / average summary cards.

### Sales Projects — `/sales-projects`
- **NEW** `MyDesk.Shared/Models/SalesProjectModels.cs` — `SalesProject`,
  `SalesStage` (Lead / Qualified / Proposal / Negotiation / Won / Lost),
  `WinLossStats`.
- **NEW** `MyDesk.Shared/Services/SalesProjectService.cs` — CRUD + per-stage
  query + win/loss stats.
- **NEW** `MyDesk.Web/Components/Pages/SalesProjects/SalesProjects.razor`
  (`/sales-projects`) — pipeline KPI cards + per-stage summary tiles +
  full project grid.
- **NEW** `MyDesk.Web/Components/Pages/SalesProjects/SalesProjectEdit.razor`.
- **NEW** `MyDesk.Web/Components/Pages/SalesProjects/SalesProjectView.razor` —
  summary, weighted value, linked quotes / invoices / POs as MudChips.

### Call Reports — `/call-reports`
- **NEW** `MyDesk.Shared/Models/CallReportModels.cs` — `CallReport`, `CallType`
  (Phone / Visit / Email / Meeting / Video).
- **NEW** `MyDesk.Shared/Services/CallReportService.cs` — CRUD + by-contact +
  by-date-range + open-follow-ups query.
- **NEW** `MyDesk.Web/Components/Pages/CallReports/CallReports.razor`
  (`/call-reports`) — list with keyword / type / date / open-only filters,
  colour-coded follow-up badge, quick mark-complete action.
- **NEW** `MyDesk.Web/Components/Pages/CallReports/CallReportEdit.razor`.

### Follow-Ups dashboard widget
- **NEW** `MyDesk.Web/Components/Shared/FollowUpsCard.razor` — aggregates
  outstanding follow-ups for the current user (falls back to all users when
  the current user has none); top 8 with red / amber / green colour coding
  for overdue / due soon / future.
- **UPDATED** `Dashboard.razor` — embeds `<FollowUpsCard />` at the top of the
  overview grid.

### Service registration
- `Program.cs` registers `RfqService`, `SalesProjectService` and
  `CallReportService` as scoped services (in-memory backing store is static
  inside each service).

### Documentation
- **UPDATED** `SITEMAP.md` — Sales Module section now lists `/rfq`,
  `/sales-projects` and `/call-reports` route trees.
- **UPDATED** `CHANGELOG.md` — this entry.

### Build
- `dotnet build src/MyDesk.Web` — **0 warnings, 0 errors**.

---

## Phase 1 of legacy port (3 May 2026) — PO Request follow-up

Closes out the Phase 1 legacy port by adding the previously-stubbed
**Vehicle Maintenance PO Request** form. The other Phase 1 modules
(RFQ, Sales Projects, Call Reports, Follow-Ups dashboard widget) were
already shipped earlier in May; this entry covers the remaining piece
plus housekeeping.

### PO Request — `/po-request`
- **NEW** `MyDesk.Web/Components/Pages/PoRequest.razor` — single-page MudForm
  capturing division/state, vehicle rego, vehicle description, maintenance
  type, supplier, estimated amount, required-by date, free-text description
  and requester contact. Submits via the existing `PoRequestService`, which
  routes the request to the appropriate fleet inbox (Traffic Mgmt, NSW, QLD,
  VIC, SA) using `EmailService.SendAsync`. Side card shows the live routing
  table and the most recent submission. Page class is aliased
  (`PoRequestModel = MyDesk.Shared.Models.PoRequest`) to avoid clashing with
  the auto-generated component class name.

### Confirmation of pre-existing Phase 1 deliverables
- `MyDesk.Shared/Models/RfqModels.cs`, `SalesProjectModels.cs`,
  `CallReportModels.cs`, `PoRequestModels.cs` — present.
- `MyDesk.Shared/Services/RfqService.cs`, `SalesProjectService.cs`,
  `CallReportService.cs`, `PoRequestService.cs` — present, in-memory
  `static List<T>` storage with seeded records.
- `Components/Pages/Rfqs/{RfqList,RfqEdit,RfqView,RfqCompare}.razor`,
  `Components/Pages/SalesProjects/{SalesProjects,SalesProjectEdit,SalesProjectView}.razor`,
  `Components/Pages/CallReports/{CallReports,CallReportEdit}.razor` — present.
- `Components/Shared/FollowUpsCard.razor` — present and embedded in
  `Dashboard.razor`.
- `Program.cs` — `RfqService`, `SalesProjectService`, `CallReportService`
  and `PoRequestService` already registered as scoped services.
- `StubRoutes.razor` — `/rfq`, `/sales-projects`, `/call-reports` stubs
  already removed (only `/admin/financial-year` remains).

### Documentation
- **UPDATED** `SITEMAP.md` — Purchasing Module section now lists
  `/po-request`. Stub Routes section corrected: `/rfq` removed (it is now
  fully implemented) and `/admin/financial-year` listed in its place.
- **UPDATED** `CHANGELOG.md` — this entry.

### Build
- `dotnet build src/MyDesk.Web` — **0 warnings, 0 errors**.

---

## Version 3.1.0 — May 2026

### Granular Permission System
- **NEW**: Comprehensive permission system controlling access to every module and function
- **NEW**: `RolePermissions` database table with automatic creation on startup
- **NEW**: `PermissionService` with caching and CRUD operations
- **NEW**: Visual permission matrix UI in Admin > User Roles & Permissions
- **NEW**: 80+ granular permissions defined across all modules
- **NEW**: Default permissions for Director, Administrator, Accounts, and Sales roles
- **NEW**: Director restriction - cannot manage Administrator users (enforced at multiple levels)

### Security Enhancements
- **NEW**: `robots.txt` endpoint blocking all search engine crawlers
- **NEW**: `X-Robots-Tag: noindex` middleware on all responses
- **NEW**: Meta noindex tags for all major search engines and AI crawlers
- **ENHANCED**: All future code must use PermissionService for authorization checks
- **FIXED**: Directors cannot view, edit, add, or delete Administrator users

### Navigation & UI Fixes
- **FIXED**: Removed duplicate "Sales Projects" entries in NavMenu (was repeated 3 times)
- **ENHANCED**: Added Platform Settings to Admin navigation group
- **ENHANCED**: Added User Roles & Permissions link to Admin navigation
- **ENHANCED**: Added complete Reference Data submenu with all admin pages:
  - Divisions, Locations, Quote/Invoice/PO Status, Currency, Part Codes
  - Activity Types, Parameters, Nav Menu, Setup Menu, Brand Assets, AI Audit Log

### Documentation
- **NEW**: PERMISSIONS.md - Complete permission system documentation
- **NEW**: SECURITY.md - Security best practices and hardening guide
- **UPDATED**: README.md - Version 3.1, added security & permissions section
- **UPDATED**: CHANGELOG.md - This entry

### Database
- **NEW**: RolePermissions table (auto-created on startup)
- **NEW**: Automatic default permission seeding

---

## Changes Made

### 1. Eliminated Duplicates ✓

**Removed:**
- `src\Database\` folder (duplicate of `src\Deployment\Migration\`)
- `README-NEW-STRUCTURE.md` (merged into main README)

**Kept (Canonical):**
- `src\Deployment\Migration\` — All database scripts and migration docs

### 2. Consolidated Documentation ✓

**Created:**
- `README.md` — Single source of truth, DR MyDesk focused (no legacy version talk)
- `TESTING.md` — Merged from `tests\MyDesk.PlaywrightTests\USAGE.md` + simplified
- `src\Deployment\README.md` — Complete deployment guide (local IIS → production VM)

**Removed:**
- `README-NEW-STRUCTURE.md` (obsolete)

### 3. Interactive Run.bat Menu ✓

**Root `Run.bat` now provides:**

```
[1] Run DR MyDesk                (Local Development Server)
[2] Run Tests                    (Playwright E2E Tests)
[3] Testing Documentation        (TESTING.md)
[4] Project README               (README.md)
[5] Configuration Files          (appsettings.json, navmenu.json, etc.)

── SQL Database ────────────────────────────────────────────────
[6] Migration - Access to SQL    (Legacy migration scripts)
[7] Install Database             (Install.ps1)
[8] Deploy to IIS                (Deploy.ps1 - requires Admin)

[Q] Quit
```

**Process Flow (Start to Finish):**
1. **Local Development** → Option 1 (Run DR MyDesk)
2. **Testing** → Option 2 (Run Tests)
3. **Database Setup** → Option 7 (Install.ps1)
4. **Local IIS Deploy** → Option 8 (Deploy.ps1)
5. **Production Deploy** → Copy publish folder to VM → Run Deploy.ps1 on server

### 4. Naming Standards ✓

**All folders follow PascalCase:**
- `src\MyDesk.Web\`
- `src\MyDesk.Shared\`
- `src\Deployment\`
- `src\Documentation\`
- `tests\MyDesk.PlaywrightTests\`

**All files follow conventions:**
- Scripts: `Deploy.ps1`, `Install.ps1`, `Run.bat`
- Docs: `README.md`, `TESTING.md`, `CHANGELOG.md`
- Config: `appsettings.json`, `navmenu.json`, `targets.json`

### 5. Fixed Deploy.ps1 ✓

**Issues Resolved:**
- ✓ Added Administrator elevation check
- ✓ Replaced `WebAdministration` module with `appcmd.exe` (universal)
- ✓ Fixed project path (`..\MyDesk.Web` instead of old `..\src\Techlight.MyDesk.Web`)
- ✓ Added `-Force` to directory creation
- ✓ Clear error messages with instructions

---

## File Structure (Current)

```
C:\Development\Techlight.digitalresponse.com.au\
├── src\                              # Main source
│   ├── MyDesk.Web\                   # Blazor app
│   ├── MyDesk.Shared\                # Shared library
│   ├── Deployment\                   # Deployment scripts
│   │   ├── Deploy.ps1                # IIS deployment (FIXED)
│   │   ├── README.md                 # Deployment guide (UPDATED)
│   │   └── Migration\                # SQL migration scripts
│   │       ├── Install.ps1
│   │       ├── PostMigrationFixes.sql
│   │       ├── Cleanup-LegacyTables.sql
│   │       └── README.md
│   ├── Documentation\
│   ├── Run.bat                       # Local dev launcher
│   └── MyDesk.slnx
│
├── tests\                            # Playwright tests
│   └── MyDesk.PlaywrightTests\
│
├── Run.bat                           # Interactive menu (NEW)
├── Run-Tests.bat                     # Test runner
├── README.md                         # Main docs (UPDATED)
├── TESTING.md                        # Test docs (NEW)
└── CHANGELOG.md                      # This file (NEW)
```

---

## What Was Removed

- ❌ `src\Database\` (duplicate)
- ❌ `README-NEW-STRUCTURE.md` (obsolete)
- ❌ Old `Run.bat` (replaced with menu version)

---

## Next Steps

1. **Run the app:**
   ```batch
   .\Run.bat
   # Choose option 1
   ```

2. **Deploy to local IIS:**
   ```batch
   .\Run.bat
   # Choose option 8
   ```

3. **Run tests:**
   ```batch
   .\Run.bat
   # Choose option 2
   ```

---

**All changes committed:** April 21, 2026  
**Status:** ✓ Complete
