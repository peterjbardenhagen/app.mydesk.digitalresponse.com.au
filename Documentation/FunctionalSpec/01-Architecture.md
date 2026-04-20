# 01 — Architecture

Status: **IN REVIEW** — verified against source.

## 1. Runtime stack

| Layer | Technology |
|---|---|
| Web server | IIS (Windows Server) — Classic ASP ISAPI (`asp.dll`) + ASP.NET 4.8 |
| Language | VBScript (Classic ASP) + C#/VB (ASP.NET) |
| Script engine | `%windir%\system32\inetsrv\asp.dll` (configured in `web.config`) |
| Default documents | `Default.asp`, `Default.htm`, `index.htm`, `index.html`, `iisstart.htm`, `default.aspx` |
| Database | Microsoft Access 2000 (`Techlight2.mdb`) — Jet engine via ODBC `Driver={Microsoft Access Driver (*.mdb)}` |
| DB connection mode | `ADODB.Connection`, Mode 3 (read/write shared), connection timeout 15s, command timeout 30s (`TL_DB_TIMEOUT`, `TL_CMD_TIMEOUT`) |
| PDF engine | WebSupergoo ABCpdf 11 (inside `/MyDeskASPNet/`) |
| Email | CDO/SMTP via `SendMail` helper (see `ssi_Functions_Core.asp`) |
| Session store | In-proc ASP Session (cookieless=false) — supplemented by persistent cookies for remember-me and legacy values |

## 2. Top-level folder layout

```
c:\Development\Techlight.digitalresponse.com.au\
├── Default.asp                  ← public sign-in page
├── Default2.asp                 ← legacy alt sign-in
├── AutoLogin.asp                ← secure-link login (for "Forgot Password")
├── ForgotPassword_Proc.asp      ← processes forgot-password modal
├── SetCookies.asp               ← post-login redirector
├── CheckLogs.asp                ← diagnostic log viewer
├── Timezone.asp                 ← ServerToEST() helper (legacy include target)
├── Portal.asp / PortalFrame.asp ← legacy root-level portal (still referenced)
├── RunDDLToken.asp              ← one-time DDL/migration token runner
├── web.config                   ← IIS handler mapping + custom errors
├── Build.ps1 / Install.ps1 / Setup.ps1  ← deployment tooling
├── Global.asa                   ← minimal (Session_OnStart only)
├── appsettings.json / .Development.json ← .NET-style config (used by MCP worker)
├── favicon.svg, Loading.html, index.asp (73-byte redirect)
│
├── Clients/
│   ├── SalesEngineTL/           ← ★ ACTIVE APPLICATION (Techlight)
│   └── SalesEngine/             ← legacy pre-TL folder (see 99-Deprecated)
│
├── System/                      ← shared includes, CSS, JS, function libraries
├── MyDeskASPNet/                ← .NET 4.8 interop (PDF, email, thumbnails)
├── MyDeskMCP/                   ← AI/Model-Context-Protocol worker (separate process)
│
├── Database/                    ← dev Access MDB location (prod uses C:\Database)
├── Documentation/               ← spec + analysis docs (this folder)
├── Errors/                      ← 500-100.asp custom error page
├── Logs/                        ← runtime logs
├── aspnet_client/               ← legacy ASP.NET resources
├── images/                      ← shared image assets (techlight-logo.svg, etc.)
├── src/ tests/ node_modules/    ← Playwright test harness
├── Properties/                  ← .NET project properties
├── package.json, playwright.config.js
└── Techlight.MyDesk.slnx        ← solution file
```

## 3. Active application folder (`Clients/SalesEngineTL/`)

The app is **single-tenant Techlight**; all URLs resolve under `/Clients/SalesEngineTL/`. See `91-File-Inventory.md` for the complete file list. Business modules (each a sub-folder):

```
ActivityTypes/   Admin/           CallReports/       Companies/
Contacts/        CopyContacts/    CurrencyRates/     Dashboard/
DeliveryNotes/   Divisions/       Employment/        Expenses/
ExpenseTypes/    ExpenseTypeGroups/ FilesCategories/ FilesLibrary/
ImportData/      Invoices/        JobOrders/         Jobs/
Locations/       Noticeboard/     Parameters/        PartCodes/
Portal/          Processes/       Products/          Projects/
PurchaseOrders/  Purchasing/      QuoteCOS/          Quotes/
Reports/         RFQ/             SalesProjects/     Setup/
SQLQuery/        System/          TableComments/     TableFiles/
Timesheets/      TMail/           UserRoles/         Users/
```

Plus the following application-level pages at the root of `SalesEngineTL/`:

| File | Role |
|---|---|
| `Default.asp` | Frameset: Header (70px) + MainFrame. If `?Page=` is supplied, loads that URL into MainFrame; otherwise loads `Dashboard.asp`. |
| `DefaultFrame.asp` | Self-contained sign-in page (used when landing inside a frame). Submits to `Portal/Validate.asp`. |
| `Header.asp` | Top navigation (see `03-Navigation-Header.md`). |
| `Dashboard.asp` | Home page — see `05-Dashboard.md`. |
| `Portal.asp` | Legacy dashboard layout, retained and partly still used (reads `Session("Admin")`, `Session("Code")`, unread `TMail`, 14-day `Noticeboard`, outstanding `Comments` follow-ups). |
| `PortalFrame.asp` | Legacy cookies/frameset portal — superseded by `Header.asp + Dashboard.asp` but still linked from some flows. |
| `GlobalSearch.asp` | Cross-module search — see `41-GlobalSearch-AskAI.md`. |
| `AskAI.asp` | AI assistant pop-up (opened as 450×600 window). |
| `QuickNav.asp` | Quick ID-nav helper used by `PortalFrame.asp`. |
| `LastUpdated.asp`, `Updating.asp`, `NoRecords.asp` | Utility pages (update timestamp badge, DB-missing landing, empty-grid row). |
| `Dashboard.asp`, `Dashboard/Dashboard_Data.asp`, `Dashboard/Widgets/*.asp` | Modern dashboard + widget partials. |
| `Del_Proc.asp` (root) | Generic delete handler for cross-module deletions (used by the Global Search results). |
| `ssi_Security.inc` (legacy stub) | Two-liner that redirects to `Portal/Logoff.asp` if the `LoggedIn` cookie is missing. **Note:** the current canonical security include lives at `/System/ssi_Security.inc` and is what's used by new pages. |

## 4. UI layout model

The application renders inside a **classic HTML frameset** defined in `Clients/SalesEngineTL/Default.asp`:

```html
<frameset rows="70,*" frameborder=0 framespacing=0>
  <frame src="Header.asp" id="HeaderFrame" scrolling="no">
  <frame src="Dashboard.asp" id="MainFrame" scrolling="yes">
</frameset>
```

- **HeaderFrame** (top 70px): global navigation, logo, user panel, search modal, Ask AI — see `03-Navigation-Header.md`.
- **MainFrame**: every module page loads into this frame unless the link uses `target="_top"` (modules like Contacts, Quotes, Invoices, PurchaseOrders, Setup, Users use `_top` to take over the whole viewport, then re-render their own `Default.asp` within that module).
- Many list pages (`Default.asp`) host a nested **iframe** (`<iframe src="IFrame.asp"…>`) that contains the data grid. This is the historic pattern for refreshing a grid without reloading the toolbar.
- Pop-ups (Email/Fax compose, AddNewWin, AskAI, product selector) open via `window.open(...)` with fixed dimensions.

### Targets in navigation

| Link target | Used for |
|---|---|
| `target="MainFrame"` | Home/Dashboard (stays inside frameset) |
| `target="_top"` | Module links (Contacts, Quotes, Invoices, Purchases, Setup, Users, Admin) — replaces the frameset with the module's own pages. Each module then injects `Header.asp` at the top of its own `Default.asp`. |
| `target="_parent"` | Logo returns to the frameset root |
| `target="_blank"` / `window.open` | Compose windows, Ask AI, product pickers |

## 5. ASP Classic ↔ ASP.NET interop

PDF generation, image thumbnailing, and bulk email use an ASP.NET 4.8 application running under a **separate application pool** (to keep the 32-bit Classic ASP pool clean of .NET assemblies).

```
┌────────────────────────┐  Redirect   ┌────────────────────────┐ Redirect ┌────────────────────────┐
│  Classic ASP           │──────────▶  │  ASP.NET handler       │─────────▶│  Classic ASP           │
│  (Module/Email.asp,    │             │  (/MyDeskASPNet/       │          │  (Module/Email_Proc.asp│
│   GenerateXxx.asp)     │             │   GenerateXxx.aspx)    │          │   → SendMail)          │
└────────────────────────┘             └────────────────────────┘          └────────────────────────┘
      │ updates DB status                 │ ABCpdf.AddImageUrl                │ attaches PDF
      │ (Draft → Issued)                  │ (scrapes View.asp?email=true)     │ sends via SMTP
      │                                   │ writes to <Module>/Files/*.pdf    │
```

Canonical pairs:

| Classic ASP shim | .NET handler | Output file path |
|---|---|---|
| `Quotes/GenerateQuote.asp` | `MyDeskASPNet/GenerateQuote.aspx(.cs)` | `Clients/SalesEngineTL/Quotes/Files/Q<Qid>.pdf` |
| `Invoices/GenerateInvoice.asp` | `MyDeskASPNet/GenerateInvoice.aspx(.cs)` | `Clients/SalesEngineTL/Invoices/Files/I<Iid>.pdf` |
| `Invoices/GenerateDeliveryNote.asp` | `MyDeskASPNet/GenerateDeliveryNote.aspx(.cs)` | `Clients/SalesEngineTL/Invoices/Files/DN<Iid>.pdf` |
| `PurchaseOrders/GeneratePO.asp` | `MyDeskASPNet/GeneratePurchaseOrder.aspx(.cs)` | `Clients/SalesEngineTL/PurchaseOrders/Files/PO<POid>.pdf` |
| `RFQ/Generate*.asp` | `Clients/SalesEngineTL/RFQ/GenerateRFQ.aspx(.vb)` (in-place .NET) | `Clients/SalesEngineTL/RFQ/Files/…` |

Full details: `50-ASPNet-Interop.md`.

## 6. Bootstrap / include order (standard page)

The canonical page skeleton (as codified in `System/ARCHITECTURE.md` and used by Default.asp, Dashboard.asp, all `*_Proc.asp` handlers):

```asp
<%
Option Explicit
%>
<!--#include virtual="/System/Constants.asp"-->            ' Layer 0 — TL_* constants
<!--#include virtual="/System/ssi_ResponseHeaders.inc"-->  ' No-cache headers
<!--#include virtual="/System/ssi_dbConn_open.inc"-->      ' Opens dbConn → Techlight2.mdb
<!--#include virtual="/System/ssi_Functions.asp"-->        ' Pulls in all function modules
<!--#include virtual="/System/ssi_Security.inc"-->         ' Login gate (redirects if not logged in)
<!--#include virtual="/System/ssi_Dates.inc"-->            ' Date helpers (FormatDateU, FormatDateAU, etc.)
<%
'  ... page code ...
%>
<!--#include virtual="/System/ssi_dbConn_close.inc"-->
```

Historical pages (many) still use the older order with inline cache headers instead of `ssi_ResponseHeaders.inc`. Both are accepted and functionally equivalent.

### Include dependency graph

```
Constants.asp                 (Layer 0 — sets TL_* constants; has include-guard via ConstantsIncluded)
   └── ssi_LegacyCompat.asp   (Layer 1 — sets strWorkingDir, ClientSettings cookies from TL_* for old code)
          └── ssi_Functions.asp  (Layer 2 — master aggregator)
                 ├── ssi_Errors.asp           ← error page helpers
                 ├── ssi_SafeExecute.inc      ← SafeExecute(sql), CloseRS(rs) wrappers
                 ├── /Timezone.asp            ← ServerToEST(), ESTToServer()
                 ├── ssi_Alerts.asp           ← AlertPurchasingManager(...)
                 ├── ssi_Functions_Core.asp   ← generic helpers: MyRedirect, SendMail, FormatCurrency, NewCode, etc.
                 ├── ssi_Functions_User.asp   ← user/role/permission helpers
                 ├── ssi_Functions_Quote.asp  ← quote-specific helpers (GetQuoteNextLineApprover, etc.)
                 ├── ssi_Functions_PO.asp     ← PO helpers (GetPONextLineApprover, etc.)
                 ├── ssi_Functions_UI.asp     ← UI rendering helpers
                 ├── ssi_Functions_Activity.asp ← logging user activity
                 └── ssi_Functions_Files.asp  ← file attachment helpers

ssi_dbConn_open.inc   (opens ADODB.Connection to TL_DATABASE_PATH\TL_DB_FILENAME; redirects to /Updating.asp if MDB missing)
ssi_dbConn_close.inc  (closes and releases dbConn)

ssi_Security.inc      (System/ — modern; reads "LoggedIn" cookie with type-safe CBool; redirects to /Default.asp on failure)
ssi_Security.inc      (Clients/SalesEngineTL/ — legacy stub; redirects to Portal/LogOff.asp)

ssi_Header.inc         (client-side right-click disabler + comment block)
ssi_Header_Techlight.inc (sets strWorkingDir from TL_WORKING_DIR; defines IsActive(pageName) and IsDirector() helpers)
```

## 7. Configuration / constants

All "static" configuration lives in `/System/Constants.asp`:

| Constant | Value | Purpose |
|---|---|---|
| `TL_WORKING_DIR` | `/Clients/SalesEngineTL` | Root of the application |
| `TL_SYSTEM_PATH` | `/System` | Shared includes |
| `TL_DATABASE_PATH` | `/Database` | Server-relative path to MDB |
| `TL_PREFIX` | `TL` | Client prefix (historic multi-tenant remnant) |
| `TL_STATE` | `AUS` | Country/state code |
| `TL_COMPANY_NAME` | `Techlight` | Display name |
| `TL_COLOR_PRIMARY` | `#00a8b5` | Brand teal |
| `TL_COLOR_PRIMARY_DARK` | `#008a94` | |
| `TL_COLOR_HOME` | `#005b89` | Home accent |
| `TL_STYLESHEET` | `Style.css` | Legacy stylesheet reference |
| `TL_DB_FILENAME` | `Techlight2.mdb` | Database file |
| `TL_DB_TIMEOUT` | `15` | Connection timeout (s) |
| `TL_CMD_TIMEOUT` | `30` | Command timeout (s) |
| `TL_APPROVAL_PASSWORD` | `approveme` | **Hard-coded** PO/quote approval secret (stored in `ApprovalPassword` cookie) — flagged as a known security risk. |

The build process or IIS bindings may override these via deployment, but in code they're compile-time constants.

## 8. Error handling

- `web.config` redirects HTTP 501 errors to `/Errors/500-100.asp` (`ExecuteURL` mode).
- Classic ASP errors that escape include handlers are rendered inline by `ssi_Errors.asp` helpers.
- Many pages wrap risky code in `On Error Resume Next` blocks with `Err.Clear` after, particularly around cookie/Session access.
- `SafeExecute(sql)` (`ssi_SafeExecute.inc`) is a helper wrapper that returns Nothing on failure instead of raising — used in `Portal/Validate.asp` and throughout DB calls.

## 9. Logging

- **Page-hit logging**: `Header.asp` writes every non-_Proc/non-asset URL load into `UserHistory (UserCode, PageUrl, PageTitle)`.
- **Diagnostic logs**: `CheckLogs.asp` (root) and `/Logs/` folder used for runtime file-based logging via `ssi_Logging.asp` helpers.
- **Audit trails**: business tables have per-table audit tables (`QuoteAudit`, `PurchaseOrderAudit`, etc.) written by their respective `*_Proc.asp` handlers on status changes.

## 10. Deployment

- `Install.ps1`, `Setup.ps1`, `Build.ps1` provision IIS app pools, virtual directories, ODBC drivers, and folder permissions.
- The Access DB in production lives outside the web root at `C:\Database\Techlight2.mdb` (dev: `Database\Techlight.mdb`).
- `migration.log` tracks one-off SQL migration history (applied via `RunDDLToken.asp`).
- `playwright.config.js` and `tests/` provide a Playwright-based smoke-test harness (documented separately under `Documentation/` root).

See `Documentation/Setup.md` and `Documentation/Install-DotNet48-MSBuild.md` for the operational specifics.
