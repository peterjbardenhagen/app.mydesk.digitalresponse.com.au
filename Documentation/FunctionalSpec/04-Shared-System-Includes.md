# 04 — Shared System Includes (`/System/`)

Status: **IN REVIEW** — verified against source.

The `/System/` folder contains everything that's shared across modules: constants, connection openers, function libraries, JavaScript helpers, stylesheets. This is the spine of the application — almost every `.asp` page in `Clients/SalesEngineTL/` pulls these in.

## 1. File catalog

### 1.1 Includes (VBScript `.asp` / `.inc`)

| File | Purpose |
|---|---|
| `Constants.asp` | `TL_*` constants (paths, DB filename, timeouts, palette, approval password). Include-guarded via `ConstantsIncluded`. See §2. |
| `Consts.asp` | Legacy scalar constants (pre-Constants.asp). Kept for compatibility. |
| `Consts_Fax.asp` | Fax-specific constants (fax gateway URL, sender credentials). Only included by fax pages. |
| `ssi_Functions.asp` | **Master aggregator** — includes Constants + LegacyCompat + every `ssi_Functions_*` module + Timezone + Alerts. This is the single include a page needs. See §3. |
| `ssi_Functions_Core.asp` | Generic helpers: `MyRedirect`, `SendMail`, `NewCode`, `FormatCurrency`, `PadZero`, etc. |
| `ssi_Functions_User.asp` | User/role/permission helpers. |
| `ssi_Functions_Quote.asp` | Quote-specific helpers: `GetQuoteNextLineApprover`, etc. |
| `ssi_Functions_PO.asp` | Purchase Order helpers: `GetPONextLineApprover`, `PONumberFormat`, etc. |
| `ssi_Functions_UI.asp` | UI-rendering helpers. |
| `ssi_Functions_Activity.asp` | Logs into Activity/audit tables. |
| `ssi_Functions_Files.asp` | File attachment helpers. |
| `ssi_LegacyCompat.asp` | Back-compat shim — sets `strWorkingDir` and `ClientSettings` cookie from `TL_*` constants so old code keeps working. |
| `ssi_Init.asp` | Page-bootstrap helper (used in newer pages to replace the cache-headers + include block). |
| `ssi_ResponseHeaders.inc` | Centralised no-cache response headers. |
| `ssi_Security.inc` | Login-gate include. Reads the `LoggedIn` cookie (type-safe CBool); redirects to `/Default.asp?Msg=Request.Cookies+Expired` on failure. |
| `ssi_dbConn_open.inc` | Opens `ADODB.Connection` to `Techlight2.mdb` (MDB path via `TL_DATABASE_PATH` + `TL_DB_FILENAME`), timeouts from constants. Redirects to `/Updating.asp` if MDB is missing. Exposes `dbConn`. |
| `ssi_dbConn_close.inc` | Closes `dbConn` and releases. |
| `ssi_Dates.inc` | Date helpers (`FormatDateU`, `FormatDateU2/3/Long`, `FormatDBToAmbiguous`, `FormatTimestampDB`, `PadNumber`, `DBDate`, `DayOfWeek`, `DateFileName`, `GetFirstEndOfWeek`, `GetAllWorkingWeeksOfYear…`, `GetNextEndOfWeek…`, `ConvertToTime`, `ConvertToHours`). See §6. |
| `ssi_Alerts.asp` | `AlertPurchasingManager(DivisionId, Subject, Message)` — sends a system email to the division's Purchasing Manager via `SendMail`. |
| `ssi_Errors.asp` | Error rendering + helpers. |
| `ssi_ErrorHandling.inc` | Per-page error handler (wraps SQL calls). |
| `ssi_SafeExecute.inc` | `SafeExecute(sql)` — runs SQL with `On Error Resume Next` and returns `Nothing` on failure. `CloseRS(rs)` — null-safe recordset close/release. |
| `ssi_Logging.asp` | File-based logging into `/Logs/`. |
| `ssi_Header.inc` | Legacy JS: disables right-click except on dev/FilesLibrary; emits server/local time as HTML comment. |
| `ssi_Header_Techlight.inc` | Sets `strWorkingDir = TL_WORKING_DIR`; defines `IsActive(pageName)` and `IsDirector()`. |
| `ADOVBS.inc` | Standard ADO constants (`adOpenStatic`, `adCmdText`, etc.). Included by `ssi_dbConn_open.inc`. |
| `Var.asp` | Minimal per-request cookie/session setup: `ApprovalPassword` cookie + `strWorkingDir`/`strGlobalPrefix`/`strGlobalState` locals for legacy pages. |
| `IFrame.asp` | Generic iframe content page (used for ad-hoc grids). |
| `PageTemplate.asp` | Boilerplate template (reference only). |
| `CurrencySelector.asp` / `CurrencySelector_FaxEmail.asp` | Drop-down rendering the active currency list — included by Quote/Invoice/PO forms and their email/fax compose screens. |
| `freeASPUpload.asp` | Third-party `Upload` class used by every file-upload form. |

### 1.2 Client assets

| File | Purpose |
|---|---|
| `Global.js` | Shared JS helpers — see §5. |
| `Grid2.js`, `aw.js`, `grid.js`, `paging1.js`, `grid.css`, `grid.png`, `icons.png`, `loading.gif`, `gecko.xml` | **ActiveWidgets** data-grid library and themes (used inside the module iframes). |
| `cal2.js`, `cal_conf2.js` | Calendar / date-picker (used by every date input across modules). |
| `Style_Modern.css` | **Primary modern stylesheet** (27 KB) — tokens, cards, grids, buttons, alerts, status pills. Used by the Dashboard, Global Search, Access Denied, modal chrome, and any modernised page. |
| `Style_Login.css` | Login-specific styles used by `/Default.asp`. |
| `Style_Techlight.css` | Techlight-specific token overrides. |
| `Style.css`, `Style2.css`, `Style3.css` | Legacy stylesheets — still referenced by older module pages (`Session("Stylesheet") = "Style.css"`). |
| `Style_Print.css` | Print overrides (used by PDF view pages). |
| `PL_Style.css` | *Pierlite* legacy stylesheet — not actively used but still present. |
| `js/` | Vendor JS (e.g. `chart.min.js` used by the Director KPI charts). |

## 2. `Constants.asp` — application-wide settings

```asp
Const TL_WORKING_DIR      = "/Clients/SalesEngineTL"
Const TL_SYSTEM_PATH      = "/System"
Const TL_DATABASE_PATH    = "/Database"
Const TL_PREFIX           = "TL"
Const TL_STATE            = "AUS"
Const TL_COMPANY_NAME     = "Techlight"
Const TL_COLOR_PRIMARY    = "#00a8b5"
Const TL_COLOR_PRIMARY_DARK = "#008a94"
Const TL_COLOR_HOME       = "#005b89"
Const TL_STYLESHEET       = "Style.css"
Const TL_DB_FILENAME      = "Techlight2.mdb"
Const TL_DB_TIMEOUT       = 15
Const TL_CMD_TIMEOUT      = 30
Const TL_APPROVAL_PASSWORD = "approveme"    ' ⚠ hard-coded
```

Include-guard pattern:

```asp
If IsEmpty(ConstantsIncluded) Then
    ConstantsIncluded = True
Else
    Exit Sub    ' Already included
End If
```

Every page that uses `TL_*` constants must include `Constants.asp` (directly or transitively via `ssi_Functions.asp`).

## 3. `ssi_Functions.asp` — include graph

When a page includes `ssi_Functions.asp`, the following pulls in (in order):

```
Constants.asp                      ' sets TL_*
ssi_LegacyCompat.asp               ' back-compat shims
ssi_Errors.asp                     ' error helpers
ssi_SafeExecute.inc                ' SafeExecute(sql), CloseRS(rs)
/Timezone.asp                      ' ServerToEST(), ESTToServer()
ssi_Alerts.asp                     ' AlertPurchasingManager(...)
ssi_Functions_Core.asp             ' MyRedirect, SendMail, NewCode, FormatCurrency, …
ssi_Functions_User.asp             ' user/role helpers
ssi_Functions_Quote.asp            ' quote helpers
ssi_Functions_PO.asp               ' PO helpers
ssi_Functions_UI.asp               ' UI helpers
ssi_Functions_Activity.asp         ' activity/audit logging
ssi_Functions_Files.asp            ' file attach helpers
```

**Notes**:
- `Option Explicit` is **not** declared in `ssi_Functions.asp` (intentionally commented out) so the shared library can use implicit variables that individual pages might introduce. Individual function modules do declare locals with `Dim`.
- `Timezone.asp` lives at the site root (not `/System/`) — historic quirk.

## 4. Cross-cutting helper functions (reference)

Most-used exports from the shared modules. Full signatures in source.

### 4.1 From `ssi_Functions_Core.asp`

| Function | Returns | Purpose |
|---|---|---|
| `MyRedirect(url)` | — | Breaks out of frames (`window.parent.location.href = …` via emitted JS) then server-side `Response.Redirect`. Used after POST handlers to jump back into `_top`. |
| `SendMail(fromAddr, toAddr, subject, htmlBody)` | Boolean | Sends email via CDO. Used by Email_Proc, ForgotPassword_Proc, alerts. |
| `NewCode(prefix, length)` | String | Generates unique alpha-numeric codes (e.g. supplier code, product code). |
| `FormatCurrency(value)` / `FormatCurrency2(value, decimals)` | String | Formats with `$` prefix and thousands separator. |
| `PadZero(val, width)` | String | Left-pads with zeros. |
| `SafeExecute(sql)` (in `ssi_SafeExecute.inc`) | RS or Nothing | Runs `dbConn.Execute(sql)` wrapped in error-handling. |
| `CloseRS(rs)` | — | Null-safe `rs.Close` + `Set rs = Nothing`. |

### 4.2 From `ssi_Functions_User.asp`

| Function | Purpose |
|---|---|
| `GetUserName(code)` | Look up `Users.Name` by `Code`. |
| `GetUserEmail(code)` | Look up `Users.Email` by `Code`. |
| `GetUserLineManager(code)` | Returns LineManagerCode / Name / Email. |
| `CanApprove(userCode, divisionId)` | Boolean — is user allowed to approve for the division. |

### 4.3 From `ssi_Functions_Quote.asp`

| Function | Purpose |
|---|---|
| `GetQuoteNextLineApprover(Qid)` | Returns the name of the next-in-chain approver based on quote margin and user hierarchy. Used in `PortalFrame.asp` approval lists and in `Approve.asp`. |
| `GetQuoteApproverChain(Qid)` | Full escalation list. |
| `QuoteStatusClass(QuoteStatusId)` | Returns CSS pill class (`.tl-status-draft`, `.tl-status-issued`, `.tl-status-approved`, `.tl-status-declined`). |
| `QuoteStatusName(QuoteStatusId)` | Readable label. |

### 4.4 From `ssi_Functions_PO.asp`

| Function | Purpose |
|---|---|
| `GetPONextLineApprover(POid, HasCapEx)` | Next approver based on total value and CapEx flag. |
| `POStatusName(POStatusId)` | Readable label. |

### 4.5 From `ssi_Alerts.asp`

| Function | Purpose |
|---|---|
| `AlertPurchasingManager(DivisionId, Subject, Message)` | Looks up `Divisions.PurchasingManagerCode`, finds user's email, and calls `SendMail("admin@mydesk.com.au", toEmail, "MyDesk Alert : " & Subject, Message)`. On `dev.*` host the recipient is forced to `MD0025`. |

### 4.6 From `/Timezone.asp`

| Function | Purpose |
|---|---|
| `ServerToEST(dt)` | Converts server-local time (UTC-ish on prod) to AU Eastern time for display. |
| `ESTToServer(dt)` | Reverse. |

### 4.7 From `ssi_Dates.inc` (see §6)

## 5. `Global.js` — shared client-side helpers

Defined at `/System/Global.js` (≈ 350 lines). Loaded by virtually every page.

### 5.1 Record-viewer helpers (all `window.open` by default, but the actual impl uses `document.location.href`)

`ViewRecordStandard(WorkingDir, Path, Id, WinName, W, H, FullScreen)` is the underlying helper. Wrappers:

| Function | Target page |
|---|---|
| `ViewQuote(WD, Id)` | `/Quotes/View.asp?Qid=<Id>` |
| `UpdateQuoteStatus(WD, Id)` | `/Quotes/UpdateStatus.asp?Qid=<Id>` |
| `ViewInvoice(WD, Id)` | `/Invoices/View.asp?InvoiceId=<Id>` |
| `ViewInvoiceDeliveryNote(WD, Id)` | `/Invoices/ViewDeliveryNote.asp?InvoiceId=<Id>` |
| `ViewRFQ(WD, Id)` | `/RFQ/View.asp?RFQid=<Id>` |
| `ViewExpense(WD, Id)` | `/Expenses/View.asp?ExpenseId=<Id>` |
| `ViewTMail(WD, Id)` | `/TMail/View.asp?TMailId=<Id>` |
| `ViewPurchaseOrder(WD, Id)` | `/PurchaseOrders/View.asp?POid=<Id>` |
| `ViewTimesheet(WD, Id)` | `/Timesheets/View.asp?TimesheetId=<Id>` |
| `ViewCallReport(WD, Id)` | `/CallReports/View.asp?CallReportId=<Id>` |
| `ViewContact(WD, Id)` | `/Contacts/View.asp?ContactId=<Id>` |
| `ViewSalesProject(WD, Id)` | `/SalesProjects/View.asp?SalesProjectId=<Id>` |
| `ViewCommentRecord(WD, Id)` | `/TableComments/ViewRecord.asp?CommentId=<Id>` |

### 5.2 Popup forms

| Function | Purpose |
|---|---|
| `CreateNewContact(WorkingDir, Field, ContactType)` | Opens `Contacts/AddNewWin.asp?Field=…&ContactType=…` (640×400) from any parent form that has a Contact dropdown, so the user can add a missing contact without losing their work. |
| `CreateNewContact_UpdateSelect(newValue, newText, Field)` | Called from the pop-up on save — writes the new contact into the parent's `<select>` (handles `ContactId`, `ContactId1`..`ContactId5` variants). |
| `ReplyToComment(WD, QuoteNumber, Qid, QCid, FromCode)` | Opens `/Comments/Reply.asp` (640×400). |
| `ReplyToTableComment(WD, ItemId, TableId, FromCode, CommentId)` | Opens `/TableComments/Reply.asp` (640×400). |
| `GeneratePurchaseOrder(WD, RFQid)` | Navigates parent to `/PurchaseOrders/GenerateFromRFQ.asp?RFQid=…`. |
| `OpenResults()` | Opens `/Loading.html` in a 760×400 popup (used by report generators). |

### 5.3 CRUD helpers

| Function | Behaviour |
|---|---|
| `deleteRecord(id)` | `window.open('Del_Proc.asp?Id=' + id)` then refreshes parent and the grid iframe. |
| `copyRecord(id)` | Same pattern with `Copy_Proc.asp`. |

### 5.4 Refresh helpers

| Function | Behaviour |
|---|---|
| `RefreshIFrame_Global()` | `document.parentWindow.location.reload()` — reloads the whole parent. |
| `RefreshIFrame_Global_Opener()` | From a popup, reloads the opener window's iframe. |
| `RefreshPage_Global()` | Confirms, then reloads (special-cases `/Portal.aspx` by full-URL). |
| `RefreshPage_Global_Opener()` | Calls the opener's `RefreshPage_Global()`. |

### 5.5 Form/input utilities

| Function | Purpose |
|---|---|
| `formatDecimal(num)` | Rounds to 2 decimal places and returns as string. |
| `TrackCount(fieldObj, countFieldName, maxChars)` | Live character countdown for textareas (pastes are truncated). |
| `LimitText(fieldObj, maxChars)` | Hard-limits keystrokes. |
| `isEmail(str)` | Regex email validator (with legacy fallback for browsers without RegExp). |

### 5.6 Query-string helpers

| Function | Purpose |
|---|---|
| `PageQuery(q)` | Strips `Submit2`, `Cache`, `NoCache`, `Page`, `SortIndex`, `SortDirection` params out of the current URL's query string. |
| `getTrackingQS()` | Builds a sorting/paging preservation query from the current `FormTracking` form. |
| `redirect(url)` | Appends `getTrackingQS()` and sets `document.location.href`. |
| `redirectParent(url)` | Same, targeting `parent.document.location.href`. |

### 5.7 Frame / window tweaks

| Function | Purpose |
|---|---|
| `MoveIt()` | Legacy helper that resizes/repositions the popup based on screen height (only fires when `screen.height > 800`). |
| `MM_swapImage` / `MM_swapImgRestore` / `MM_findObj` | Dreamweaver-era image rollovers (used by the legacy `PortalFrame.asp`). |
| `SubmitForm()` | Calls `parent.OpenResults()` — legacy report trigger. |

## 6. Date helpers (`ssi_Dates.inc`)

### 6.1 Formatting

| Function | Output |
|---|---|
| `FormatDateU(dt, incTime)` | `DD-Mmm-YYYY` (AU short) — optional ` at hh:mm` |
| `FormatDateU2(dt, incTime)` | Same but with HTML-comment ISO prefix (for sortable columns) |
| `FormatDateU3(dt)` | Numeric AU `DD-MM-YYYY` with ISO HTML-comment prefix |
| `FormatDateULong(dt, AUOrUSStyle, incTime)` | `<Weekday>, DD Month YYYY [hh:mm]` |
| `FormatDBToAmbiguous(dt, USorAUStyle, incTime)` | `DD/MM/YYYY` (AU) or `MM/DD/YYYY` (US) |
| `FormatTimestampDB(dt)` | `M/D/YYYY` (used for Access DB-native date comparisons) |
| `DBDate(dt)` | `YYYY/MM/DD` |
| `PadNumber(val)` | Zero-pads to 2 digits |

### 6.2 Week / calendar utilities

| Function | Purpose |
|---|---|
| `DayOfWeek(dt, len)` | Monday-based weekday name, optionally truncated |
| `GetFirstEndOfWeek(year)` | First Sunday of the year |
| `GetAllWorkingWeeksOfYear(year)` | Generates 60 weeks of `<option>` tags for select controls |
| `GetAllWorkingWeeksOfYear_ForAddingTimesheets(year, Code)` | Same but filters out weeks already timesheeted |
| `GetAllWorkingWeeksOfYear_ForEditingTimesheets(year, Code, selectedDate)` | Variant for edit mode |
| `GetNextEndOfWeek()` | Finds the next Sunday after today |
| `GetNextEndOfWeekFromDate(date)` | Same starting from a given date |

### 6.3 Misc

| Function | Purpose |
|---|---|
| `ConvertToTime(strVal)` | Converts `"0930"` → `"01/01/2000 09:30"` datetime |
| `ConvertToHours(minutes)` | `"90"` → `"1 hours, 30 minutes"` |
| `DateFileName(filename)` | Generates a timestamp-prefixed unique filename |

## 7. Database connection (`ssi_dbConn_open.inc`)

- Builds `strDbPath = Server.MapPath(TL_DATABASE_PATH) & "\" & TL_DB_FILENAME` → `…/Database/Techlight2.mdb` in dev, `C:\Database\Techlight2.mdb` in prod.
- Verifies the file exists via `Scripting.FileSystemObject`. If missing, redirects to `TL_WORKING_DIR & "/Updating.asp"` and ends.
- Creates `ADODB.Connection`, sets `ConnectionTimeout = TL_DB_TIMEOUT` (15s), `CommandTimeout = TL_CMD_TIMEOUT` (30s), `Mode = 3` (adModeReadWrite), and opens with `Driver={Microsoft Access Driver (*.mdb)};DBQ=<path>;`.
- Exposes the open connection as the **global `dbConn`**. Every subsequent `.Execute` uses this.
- `ADOVBS.inc` is included at the top for ADO constants.

## 8. Security include (`ssi_Security.inc`)

Modern version in `/System/`:

```asp
Dim isLoggedIn
isLoggedIn = False
If Not Request.Cookies("LoggedIn") Is Nothing Then
    If Not IsEmpty(Request.Cookies("LoggedIn")) Then
        Dim cookieValue: cookieValue = Request.Cookies("LoggedIn")
        If IsNumeric(cookieValue) Then isLoggedIn = CBool(cookieValue)
        ElseIf LCase(cookieValue) = "true" Then isLoggedIn = True
    End If
End If
If Not isLoggedIn Then
    Response.Redirect "/Default.asp?Msg=Request.Cookies+Expired"
    Response.End
End If
```

Legacy stub in `Clients/SalesEngineTL/ssi_Security.inc` (2 lines):

```asp
If Not(Request.Cookies("LoggedIn")) Then
    Response.Redirect "Portal/Logoff.asp?Msg=Request.Cookies+Expired"
End If
```

Pages including the legacy stub bounce to `LogOff.asp` (which clears all cookies and redirects back to `/`). Pages including the modern version bounce straight to `/Default.asp` with a message.

## 9. Stylesheet conventions (for module docs)

- **New/refactored pages** reference `/System/Style_Modern.css` + `/System/Style_Techlight.css` and use the `.tl-*` CSS classes.
- **Legacy pages** use `<link rel="stylesheet" href="<%= Session("WorkingDir") %>/System/<%= Session("Stylesheet") %>">` where `Session("Stylesheet")` = `"Style.css"` by default. These pages render the older brown/beige tables with `#ebeadb` header cells.
- **Print/PDF pages** additionally reference `Style_Print.css`.

Each module doc notes which stylesheet set its pages use.
