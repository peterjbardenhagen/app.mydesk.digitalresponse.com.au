# Techlight MyDesk (Classic ASP) — Functional Specification

**Baseline documenting the current state of the production Classic ASP application.**

| Property | Value |
|---|---|
| Application | Techlight MyDesk |
| Platform | Classic ASP (VBScript) + ASP.NET 4.8 interop |
| Database | Microsoft Access (`Techlight2.mdb`) via ODBC |
| Host | IIS on Windows VM |
| Production URL | https://techlight.digitalresponse.com.au |
| Active client folder | `/Clients/SalesEngineTL/` |
| ASP.NET interop | `/MyDeskASPNet/` |
| Repo root | `c:\Development\Techlight.digitalresponse.com.au` |
| Document date | April 2026 |
| Document owner | Techlight / Digital Response |

> This specification describes **current behaviour** (the "as-is" baseline), not desired future behaviour. Any item explicitly marked *Deprecated* or *Legacy* is retained in the codebase but not part of the current active user flow.

---

## How this spec is organised

This folder (`Documentation/FunctionalSpec/`) contains a set of files. Start here, then drill into any module.

| File | Content |
|---|---|
| [`README.md`](./README.md) | This index — purpose, conventions, sitemap, cross-cutting rules |
| [`01-Architecture.md`](./01-Architecture.md) | Runtime architecture, framesets, interop, folder layout |
| [`02-Authentication-Portal.md`](./02-Authentication-Portal.md) | Login, validate, logoff, password change, cookies & session |
| [`03-Navigation-Header.md`](./03-Navigation-Header.md) | Header, top nav, global search modal, Ask AI |
| [`04-Shared-System-Includes.md`](./04-Shared-System-Includes.md) | `/System/*` includes, `ssi_Functions.asp` function reference, Global.js, date utilities, alerts, errors |
| [`05-Dashboard.md`](./05-Dashboard.md) | Dashboard + Dashboard widgets (KPI, charts, activity, priority tasks, exceptions) |
| [`10-Quotes.md`](./10-Quotes.md) | Quotes module (list, view, edit, add, email/fax, approve/decline, copy, transporter, PDF) |
| [`11-Invoices.md`](./11-Invoices.md) | Invoices + Delivery Notes + Despatch Notes + Transporter + MYOB export |
| [`12-PurchaseOrders.md`](./12-PurchaseOrders.md) | Purchase Orders (incl. approval workflow, RFQ→PO, invoice matching) |
| [`13-RFQ.md`](./13-RFQ.md) | Requests for Quotation and supplier comparison |
| [`14-DeliveryNotes.md`](./14-DeliveryNotes.md) | Root `/DeliveryNotes/` module (if distinct from invoices) |
| [`15-JobOrders.md`](./15-JobOrders.md) | Job Orders |
| [`16-SalesProjects-Projects.md`](./16-SalesProjects-Projects.md) | Sales Projects & Projects |
| [`20-Contacts.md`](./20-Contacts.md) | Contacts (people), address popups, email/fax pickers, bulk contact copy |
| [`21-Companies.md`](./21-Companies.md) | Companies / Customers / Suppliers |
| [`22-Products-PartCodes.md`](./22-Products-PartCodes.md) | Product catalogue, Part codes, product pickers |
| [`23-CallReports.md`](./23-CallReports.md) | Sales call activity reporting |
| [`30-Users-Roles.md`](./30-Users-Roles.md) | Users, User Roles, Hierarchy, password changes |
| [`31-Divisions-Locations.md`](./31-Divisions-Locations.md) | Divisions, Locations |
| [`32-Setup-Admin.md`](./32-Setup-Admin.md) | Setup, Admin, MYOB data, Maintenance, SQL Query tool |
| [`33-Parameters-TableComments-TableFiles.md`](./33-Parameters-TableComments-TableFiles.md) | System parameters, record-level comments & files, follow-ups |
| [`34-Ancillary.md`](./34-Ancillary.md) | ActivityTypes, QuoteCOS, FilesLibrary, FilesCategories, CurrencyRates, Employment, Expenses, ExpenseTypes, Timesheets, Noticeboard, TMail, ImportData, Processes |
| [`40-Reports.md`](./40-Reports.md) | Reports module (sales reports, charts, PO by month/division, module `Report.asp` pages) |
| [`41-GlobalSearch-AskAI.md`](./41-GlobalSearch-AskAI.md) | Global cross-module search + Ask AI assistant pop-up |
| [`50-ASPNet-Interop.md`](./50-ASPNet-Interop.md) | `MyDeskASPNet/` PDF generation, ScrapeToPDF, thumbnails, email sending |
| [`90-Sitemap.md`](./90-Sitemap.md) | Full URL map + navigation graph |
| [`91-File-Inventory.md`](./91-File-Inventory.md) | Canonical list of active `.asp` pages and their classification |
| [`99-Deprecated-Legacy.md`](./99-Deprecated-Legacy.md) | Known legacy / orphaned / empty files flagged as out-of-scope |

> Per-page detail level: **Purpose · URL · Query/Form parameters · Visual structure · Fields · Validation · Actions & Buttons · Permissions · Business rules · Side-effects · Related pages.** Helper-only pages (e.g. per-module `IFrame.asp`, `NavBar.asp`, trivial `Del_Proc.asp`) are covered succinctly under their parent page.

---

## Conventions used throughout this spec

### 1. Page naming conventions

| Suffix / Pattern | Role |
|---|---|
| `Default.asp` | Module landing page — usually a list/grid plus action bar |
| `IFrame.asp` | Inner data grid / AJAX-style listing loaded inside a `Default.asp` iframe |
| `View.asp` | Read-only record detail (also used as the PDF scrape target via `?email=true`) |
| `Add.asp`, `Add2.asp` | Create wizard (Add2 = step 2 where used) |
| `Edit.asp` | Edit form |
| `*_Proc.asp` | Form handler / processor — server-side side-effects, then redirect |
| `Del_Proc.asp` | Delete processor |
| `Email.asp` / `Email_Proc.asp` | Email composition form + send handler |
| `Fax.asp` / `Fax_Proc.asp` | Fax composition form + send handler |
| `NavBar.asp` | Action button strip rendered inside `View.asp` / `Edit.asp` |
| `Report.asp` | Module-level reporting / filter interface |
| `Generate*.asp` | Classic ASP shim that redirects to the matching `.aspx` in `/MyDeskASPNet/` for PDF generation |
| `Transporter*.asp` | Cross-module record movers (e.g. Quote → Invoice, Quote → PO) |
| `UpdateStatus.asp` / `_Proc.asp` | Status change form + handler |
| `Approve.asp` / `Decline.asp` | Approval workflow endpoints |

### 2. CRUD skeleton

Almost every business module follows the same skeleton. Wherever a module is described as "standard CRUD", assume:

- `Default.asp` — list with filter controls, "Add" button, pagination, sort; hosts `IFrame.asp`.
- `IFrame.asp` — server-rendered grid rows (HTML table). Accepts sort / filter / page params, refreshed by parent via JS.
- `Add.asp` — blank form → posts to `Add_Proc.asp`.
- `Add_Proc.asp` — validates, `INSERT`s, redirects back to `Default.asp` (or the new record's `View.asp`).
- `Edit.asp?Id=n` — pre-populated form → posts to `Edit_Proc.asp`.
- `Edit_Proc.asp` — validates, `UPDATE`s, redirects.
- `Del_Proc.asp?Id=n` — deletes (usually no confirmation on the server, relies on JS `confirm()` upstream).
- `View.asp?Id=n` — read-only detail.

Variations from this skeleton are called out explicitly in each module file.

### 3. Permission / role model

Authorisation uses a mixture of Session values and Cookie values (both populated at login by `Portal/Validate.asp`). Primary gates:

| Flag | Source | Meaning |
|---|---|---|
| `LoggedIn` | Cookie | Gate for all secured pages via `/System/ssi_Security.inc` |
| `Session("Admin")` / `Request.Cookies("UserSettings")("Admin")` | Both | Shows Admin navigation, Setup extras, Users module |
| `IsDirector()` | Function in `ssi_Functions.asp` | Shows "Admin" top-nav link |
| `Session("Code")` / `Request.Cookies("UserSettings")("Code")` | Both | Current user code — used for ownership filters, audit history |
| `DivisionIdsAccess` cookie dict | Cookie | Per-module access (`Quotes`, `RFQ`, `PurchaseOrders`, `Payroll`), plus `ArrDivisionIds` (visible divisions) and `ArrDivisionIdsManager` (manager rights) |
| `UsersAccess` table | DB | Source of truth for division-level permissions, loaded into cookies at login |

**Standard permission gates seen in code:**
- `If Not CBool(Request.Cookies("LoggedIn")) Then Response.Redirect("/")` — on every secured page via `ssi_Security.inc`.
- `If Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then …` — module gate.
- `If Session("Admin") Then …` — admin-only UI.
- `If IsDirector() Then …` — director-level UI.

See [`02-Authentication-Portal.md`](./02-Authentication-Portal.md) for the complete cookie/session schema.

### 4. Cross-cutting rules

Applied to nearly every page:

1. **No-cache headers** (added by each page):
   ```vbscript
   Response.AddHeader "Pragma", "No-Store"
   Response.AddHeader "cache-control", "no-store, private, must-revalidate"
   Response.Expires = -1
   Response.ExpiresAbsolute = DateAdd("Y", -10, Now())
   ```
2. **Security check** via `<!--#include virtual="/System/ssi_Security.inc"-->` — redirects unauthenticated users to `/`.
3. **Core includes** in this order: `ssi_Security.inc` → `ssi_Functions.asp` → `ssi_dbConn_open.inc` → `ssi_Dates.inc`.
4. **SQL string sanitisation**: single quotes escaped via `Replace(value, "'", "''")` — no parameterised queries. (This is the de-facto injection guard across the codebase.)
5. **Date handling**: all persisted dates are server time; display uses `FormatDateU` / `FormatDateAU` helpers (see `ssi_Dates.inc`). Time-zone helper `ServerToEST()` is referenced in legacy code but most new code stores/reads in server-local time.
6. **Connection lifecycle**: every page that opens a DB connection closes it via `<!--#include virtual="/System/ssi_dbConn_close.inc"-->` at the foot of the page.
7. **User activity logging**: `Header.asp` inserts a row into `UserHistory (UserCode, PageUrl, PageTitle)` on every non-processing page load.
8. **Error handling**: IIS is configured in `web.config` to route errors to `/Errors/500-100.asp`.

### 5. Visual / UX conventions

- **Layout model**: the app is a **two-row HTML frameset** — top frame `Header.asp` (70px), main frame `MainFrame` — defined in `/Clients/SalesEngineTL/Default.asp`.
- **Modern stylesheet**: `/System/Style_Modern.css` (teal/charcoal Techlight palette). Some legacy pages still reference `Style.css` / `grid.css`.
- **Design tokens** (from `DefaultFrame.asp` / Style_Modern.css):
  - Primary teal `#00a8b5`, primary light `#00c4d3`, primary dark `#008a94`
  - Accent tan `#d4a574`
  - Dark `#1a1f2e`, dark-secondary `#242b3d`
  - Success `#38a169`, Error `#e53e3e`
  - Radius `12px`, standard shadows `--tl-shadow`, `--tl-shadow-lg`
- **Typography**: `Inter` from Google Fonts, with Font-Awesome 6.4 icons on login; inline SVG (Lucide-style) for app navigation icons.
- **Component classes** (used across modules): `.tl-card`, `.tl-card-header`, `.tl-card-body`, `.tl-grid`, `.tl-grid-2`, `.tl-grid-4`, `.tl-btn`, `.tl-btn-primary`, `.tl-btn-secondary`, `.tl-input`, `.tl-alert`, `.tl-status`, `.tl-status-issued`, `.tl-feature-card`, `.tl-page-header`, `.tl-page-title`, `.tl-page-subtitle`, `.tl-header`, `.tl-nav`, `.tl-nav-item`, `.tl-nav-link`, `.tl-modal`.
- **Grids**: list pages use a server-rendered HTML table inside an iframe, styled with light-beige zebra rows (historical `#ebeadb` / `#ffffff`) on some screens and `tl-card`-styled tables on modernised screens.
- **Action buttons**: CRUD forms render a right-aligned action bar with Cancel / Save / Delete. View screens use a `NavBar.asp` strip with context actions (Edit, Email, Fax, Print, Copy, Approve, etc.).

### 6. PDF generation / email / fax pattern

Every document-producing module (Quotes, Invoices, POs, RFQs, Delivery Notes, Despatch Notes) follows the same pattern:

```
Module/View.asp?Id=…              (HTML record view, used as PDF source)
Module/Email.asp?Id=…             (compose form — subject, body, recipients)
Module/Fax.asp?Id=…               (compose form — cover sheet, fax number)
Module/Generate<Doc>.asp          (Classic ASP shim)
        ↓ Response.Redirect
/MyDeskASPNet/Generate<Doc>.aspx  (.NET / ABCpdf11 — scrapes View.asp, writes PDF
                                   to <module>/Files/, then redirects back)
        ↓ Response.Redirect
Module/Email_Proc.asp             (attaches PDF, sends via CDO/SMTP)
```

Details and per-document specifics are in [`50-ASPNet-Interop.md`](./50-ASPNet-Interop.md).

---

## Top-level sitemap

```
/                                                     ← entry redirect
└── /Clients/SalesEngineTL/                           ← active app
    ├── Default.asp                                   ← frameset (Header + MainFrame)
    ├── DefaultFrame.asp                              ← standalone sign-in page (fallback)
    ├── Portal/
    │   ├── Validate.asp                              ← login POST handler
    │   ├── LogOff.asp                                ← logout → clears cookies → redirect /
    │   ├── ChangePassword.asp / _Proc.asp            ← self-service password change
    │   ├── AccessDenied.asp                          ← permission-denied screen
    │   └── Error.asp                                 ← generic error screen
    ├── Header.asp                                    ← top nav + search modal + Ask AI launcher
    ├── Dashboard.asp / Dashboard/…                   ← landing page & widgets
    ├── Portal.asp                                    ← legacy landing (still linked by some flows)
    ├── PortalFrame.asp                               ← legacy frame wrapper
    ├── GlobalSearch.asp                              ← cross-module search results
    ├── AskAI.asp                                     ← AI assistant pop-up
    ├── QuickNav.asp                                  ← quick-nav helper
    ├── LastUpdated.asp / Updating.asp / NoRecords.asp← utility pages
    │
    ├── Quotes/            Invoices/        PurchaseOrders/    RFQ/
    ├── DeliveryNotes/     JobOrders/       Projects/          SalesProjects/
    ├── Contacts/          CopyContacts/    Companies/
    ├── Products/          PartCodes/       CallReports/
    ├── Users/             UserRoles/       Divisions/         Locations/
    ├── ActivityTypes/     QuoteCOS/        Parameters/        TableComments/
    ├── TableFiles/        FilesLibrary/    FilesCategories/
    ├── Expenses/          ExpenseTypes/    ExpenseTypeGroups/
    ├── Employment/        Timesheets/      Noticeboard/       TMail/
    ├── CurrencyRates/     ImportData/      Processes/
    ├── Reports/           Purchasing/      Dashboard/
    ├── Setup/             Admin/           SQLQuery/          System/
    └── Portal/  (see above)

/MyDeskASPNet/                                        ← .NET PDF/email worker
    ├── GenerateQuote.aspx            ← PDF + email send for Quotes
    ├── GenerateInvoice.aspx          ← PDF + email for Invoices
    ├── GenerateDeliveryNote.aspx
    ├── GeneratePurchaseOrder.aspx
    ├── ScrapeToPDF.aspx              ← generic HTML→PDF via ABCpdf
    ├── MakeThumbnails.aspx
    └── UnitTests.aspx

/System/                                              ← shared includes, JS, CSS
    ├── ssi_Functions.asp             ← ★ core function library
    ├── ssi_Header.inc                ← common JS/CSS head block
    ├── ssi_Header_Techlight.inc      ← Techlight-branded head
    ├── ssi_Security.inc              ← login gate
    ├── ssi_dbConn_open*.inc          ← DB connection openers
    ├── ssi_dbConn_close.inc
    ├── ssi_Dates.inc                 ← date helpers
    ├── ssi_Alerts.asp                ← alert/toast rendering
    ├── ssi_Errors.asp                ← error-page rendering
    ├── Consts.asp / Consts_Fax.asp   ← constants
    ├── Var.asp                       ← client prefix/variables
    ├── Global.js                     ← shared client-side helpers
    ├── Grid.js / Grid2.js / aw.js    ← grid library (ActiveWidgets)
    ├── cal2.js / cal_conf2.js        ← calendar/date picker
    ├── Style_Modern.css              ← primary stylesheet
    ├── Style.css / Style2.css / grid.css / PL_Style.css
    ├── ADOVBS.inc                    ← ADO constants
    └── IFrame.asp                    ← generic iframe content
```

See [`90-Sitemap.md`](./90-Sitemap.md) for the full URL graph including every module's pages.

---

## Module matrix

Full catalog of active modules, their primary responsibility, and where they're documented. Modules flagged **L** are known legacy (see `99-Deprecated-Legacy.md`).

| # | Module folder | Purpose | Doc |
|---|---|---|---|
|   | `Portal/` | Authentication | 02 |
|   | `Header.asp` | Top nav | 03 |
|   | `Dashboard.asp` + `Dashboard/` | KPI / activity landing page | 05 |
| Core business | | | |
|   | `Quotes/` | Customer quotations | 10 |
|   | `Invoices/` | Customer invoices + Delivery Notes + Despatch Notes + MYOB export | 11 |
|   | `PurchaseOrders/` | Supplier POs, approvals, invoice matching | 12 |
|   | `RFQ/` | Requests For Quotation | 13 |
|   | `DeliveryNotes/` | Stand-alone delivery note module (if active) | 14 |
|   | `JobOrders/` | Project/job orders | 15 |
|   | `Projects/`, `SalesProjects/` | Project records | 16 |
| CRM | | | |
|   | `Contacts/` | People | 20 |
|   | `CopyContacts/` | Bulk contact copy utility | 20 |
|   | `Companies/` | Organisations | 21 |
|   | `Products/`, `PartCodes/` | Product catalogue | 22 |
|   | `CallReports/` | Customer interactions log | 23 |
| People / Access | | | |
|   | `Users/`, `UserRoles/` | User admin + RBAC | 30 |
|   | `Divisions/`, `Locations/` | Org structure | 31 |
| Admin / Setup | | | |
|   | `Setup/`, `Admin/`, `SQLQuery/` | System administration | 32 |
|   | `Parameters/` | Global parameters | 33 |
|   | `TableComments/`, `TableFiles/` | Record-level comments & file attachments | 33 |
|   | `ActivityTypes/`, `QuoteCOS/` | Lookup tables | 34 |
|   | `FilesLibrary/`, `FilesCategories/` | Central document library | 34 |
|   | `CurrencyRates/` | FX rates | 34 |
|   | `Employment/`, `Expenses/`, `ExpenseTypes/`, `ExpenseTypeGroups/`, `Timesheets/` | HR / finance | 34 |
|   | `Noticeboard/` **L** / `TMail/` **L** | Internal messaging | 34 |
|   | `ImportData/`, `Processes/` | Data import + scheduled processes | 34 |
| Reporting | | | |
|   | `Reports/` | Cross-module reports | 40 |
|   | `Purchasing/` | Purchasing dashboard | 40 |
| Search / AI | | | |
|   | `GlobalSearch.asp` | Cross-module search | 41 |
|   | `AskAI.asp` | AI assistant pop-up | 41 |
| Interop | | | |
|   | `MyDeskASPNet/` | PDF generation + email sending | 50 |

---

## Document status

| Section | Status |
|---|---|
| README (this file), conventions, top-level sitemap, module matrix | Complete |
| Architecture, Auth, Header, Shared includes, Dashboard, Quotes, Invoices, POs, RFQ, Contacts, Companies, Products, Users, Setup, Reports, Interop, Full sitemap, File inventory, Deprecated | **In progress — being written incrementally**; see each file's header for status |

Each module file carries a status banner at the top:
- **DRAFT** — structure only, details pending.
- **IN REVIEW** — substantive content complete, needs SME review.
- **BASELINE** — confirmed as an accurate as-is baseline.
