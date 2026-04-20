# 05 — Dashboard

Status: **IN REVIEW** — verified against source.

The Dashboard is the landing page after login. It replaces the legacy `Portal.asp` / `PortalFrame.asp` home pages. Lives at `/Clients/SalesEngineTL/Dashboard.asp` and pulls in a modular `Dashboard_Data.asp` + a dozen widget partials under `Dashboard/Widgets/`.

## 1. Files

| File | Role |
|---|---|
| `Dashboard.asp` | Entry page. Includes data-loader + widgets. Shown inside the main frame after login. |
| `Dashboard/Dashboard_Data.asp` | **Data fetcher** — populates all `dash_*` variables used by the widgets (KPI totals, exceptions, chart arrays). |
| `Dashboard/Widgets/Welcome.asp` | Welcome header + Quick-Nav bar |
| `Dashboard/Widgets/QuickActions.asp` | Action tile grid |
| `Dashboard/Widgets/KPI_Cards.asp` | 4-card KPI row (Director-only) |
| `Dashboard/Widgets/Charts.asp` | Sales/invoices charts (Director-only) |
| `Dashboard/Widgets/ChartScripts.asp` | Chart.js bootstrapping |
| `Dashboard/Widgets/Exceptions.asp` | Overdue quotes / unpaid invoices / PO approvals card |
| `Dashboard/Widgets/PriorityTasks.asp` | Follow-up items card |
| `Dashboard/Widgets/Sidebar.asp` | Right-hand sidebar (recent activity / unread TMail) |
| `Dashboard/Widgets/ActivityTable.asp` | Main activity feed (recent quotes/invoices/POs) |
| `Dashboard/Widgets/HelpResources.asp` | Help & support tile block |
| `Portal.asp` (sibling, legacy) | Older quick-actions dashboard. Still reachable via `/Portal.asp` redirect. |
| `PortalFrame.asp` (root-level, legacy) | Older multi-section home with cookie/SQL-driven approval lists. Still linked from some flows. |

## 2. URL and access

- **URL**: `/Clients/SalesEngineTL/Dashboard.asp`
- **Access**: logged-in users. Pulls identity from `Session` first, falls back to `Cookies("UserSettings")` (see `Dashboard_Data.asp` lines 60–100).
- **Query params**: `Msg` (optional) — shown as a banner above the widgets.
- **Assets**:
  - `Style_Techlight.css`, `Style_Modern.css`
  - `fonts.googleapis.com/Inter`
  - `/System/js/chart.min.js` (only loaded for Directors)

## 3. Layout

```
┌──────────────────────────────────────────────────────┐
│ Header.asp (top nav - see 03-Navigation-Header.md)    │
├──────────────────────────────────────────────────────┤
│ .tl-main                                              │
│  ┌──── Welcome.asp                                 ─┐ │
│  │  Welcome <Name> • role pill • date • Msg banner  │ │
│  │  Quick-Nav inline bar (ID + Type + Go)           │ │
│  └──────────────────────────────────────────────────┘ │
│  ┌──── QuickActions.asp                            ─┐ │
│  │  4×n card grid: Contacts / Quotes / Invoices /   │ │
│  │  Purchases / Call Reports / Jobs / (Admin: Users │ │
│  │  + Setup)                                        │ │
│  └──────────────────────────────────────────────────┘ │
│  ┌──── KPI_Cards.asp (Director only)               ─┐ │
│  │  4 stat cards: MTD Quotes Won, MTD Invoice $,    │ │
│  │  YTD Quotes Won $, YTD vs Last YTD %             │ │
│  └──────────────────────────────────────────────────┘ │
│  ┌──── Charts.asp (Director only)                  ─┐ │
│  │  Monthly Quotes Won (this vs last year)          │ │
│  │  Monthly Invoices (this year)                    │ │
│  └──────────────────────────────────────────────────┘ │
│  ┌──── Exceptions.asp                              ─┐ │
│  │  Overdue Quotes, Overdue Invoices, POs Pending   │ │
│  │  Approval                                        │ │
│  └──────────────────────────────────────────────────┘ │
│  .tl-dashboard (2-col)                                │
│  ┌── Sidebar.asp ──┐  ┌── ActivityTable.asp ──┐        │
│  │ Priority Tasks   │  │ Recent Quotes/Invoices │      │
│  │ Unread TMail     │  │ /POs (last 10)         │      │
│  │ 14-day Notices   │  └───────────────────────┘        │
│  └─────────────────┘  ┌── HelpResources.asp ──┐        │
│                       │  KB + support tiles     │       │
│                       └────────────────────────┘        │
└──────────────────────────────────────────────────────┘
ChartScripts.asp injects Chart.js code at the bottom for Directors
```

## 4. Visibility rules

### 4.1 Director-only sections

Determined by `dash_isDirector` in `Dashboard_Data.asp`:

```asp
dash_isDirector = (dash_userTypeId = "1" Or dash_userTypeId = "5" Or dash_userTypeId = "6")
```

Director = `UserTypeId` 1, 5, or 6 (Director / Senior roles).

Only Directors see: **KPI_Cards**, **Charts**, **ChartScripts**, **Exceptions** (though Exceptions also renders for `dash_isManager = true`).

### 4.2 Admin-only tiles

QuickActions renders the "Users" and "Setup" tiles only when `Session("Admin") = true`.

## 5. Widget specifications

### 5.1 `Welcome.asp`

- **H1**: "Welcome, <`Session("Name")`>" with an "Administrator" pill if `Session("Admin")`.
- **Subtitle**: date + "You have successfully logged into Techlight MyDesk."
- **Optional message banner**: shown when `?Msg=…` present (uses `.tl-alert .tl-alert-info`).
- **Quick-Nav form** (inline): fields `ID` + `Type` (Quote | PurchaseOrder | Invoice | Contact) posting GET to `QuickNav.asp`.

### 5.2 `QuickActions.asp`

4-column responsive grid of `.tl-feature-card` tiles:

| Tile | Link | Always visible? |
|---|---|---|
| Contacts | `<WD>/Contacts/` | Yes |
| Quotes | `<WD>/Quotes/` | Yes |
| Invoices | `<WD>/Invoices/` | Yes |
| Purchasing | `<WD>/Purchasing/` | Yes |
| Call Reports | `<WD>/CallReports/` | Yes |
| Jobs | `<WD>/Jobs/` | Yes |
| Users | `<WD>/Users/` | `Session("Admin")` |
| Setup | `<WD>/Setup/` | `Session("Admin")` |

Each tile has an inline-SVG icon, a title, and a short description.

### 5.3 `KPI_Cards.asp` (Director only)

Four `.tl-kpi-card`s across a 4-column grid. Numbers populated from `dash_*` variables computed in `Dashboard_Data.asp`:

| Card | Primary metric | Sub-metric |
|---|---|---|
| **MTD Quotes Won** | `dash_thisMonthQuotesWon` | vs `dash_lastMonthQuotesWon` (arrow + %) |
| **MTD Invoice Value** | `FormatCurrency(dash_thisMonthInvoiceValue)` | vs last month invoice count |
| **YTD Quotes Won** | count `dash_ytdQuotesWon` / `FormatCurrency(dash_ytdQuotesValue)` | vs `dash_lastYearYTDQuotesValue` |
| **Exceptions summary** | `dash_pendingQuotesOver30Days + dash_invoicesOverdue + dash_pendingApprovalPOs` | link to Exceptions section |

### 5.4 `Charts.asp` + `ChartScripts.asp` (Director only)

Two `<canvas>` elements rendered by Chart.js (`/System/js/chart.min.js`):

- **Monthly Quotes Won (value)** — line chart, two series:
  - This year → `dash_monthlyQuotesThisYear(1..12)`
  - Last year → `dash_monthlyQuotesLastYear(1..12)`
  Only quotes with `QuoteStatusId ∈ {4, 10}` are counted.
- **Monthly Invoices (value)** — bar chart:
  - This year → `dash_monthlyInvoicesThisYear(1..12)` (sum of `Invoices.NettPriceTotal`).

X-axis: months Jan–Dec. Y-axis: AUD. Chart configuration lives in `ChartScripts.asp` and is emitted at the bottom of the page.

### 5.5 `Exceptions.asp`

A `.tl-card` listing three counters:

| Row | SQL | Link |
|---|---|---|
| Overdue Quotes | `SELECT COUNT(*) FROM Quotes WHERE QuoteStatusId IN (1,2) AND QuoteDate < DateAdd('d', -30, Now())` | `Quotes/Default.asp?Filter=Overdue` |
| Overdue Invoices | `SELECT COUNT(*) FROM Invoices WHERE InvoiceStatusId = 2 AND InvoiceDate < DateAdd('d', -30, Now())` | `Invoices/Default.asp?Filter=Overdue` |
| POs Pending Approval | `SELECT COUNT(*) FROM PurchaseOrders WHERE POStatusId = 2` | `PurchaseOrders/Default.asp?Filter=PendingApproval` |

Each row is a `.tl-exception-item` (red-tinted left border, hover translates right 5 px). Clicking the row drills into the list.

> **Status code reference** (as read by the Dashboard, cross-check with `QuoteStatus` / `InvoiceStatus` / `PurchaseOrderStatus` tables):
> - Quotes: 1 = Draft, 2 = Issued, 3 = …, 4 = Won, 9 = Pending Approval, 10 = Won (alt).
> - Invoices: 2 = Issued / Unpaid (used by the overdue calc).
> - POs: 2 = Pending Approval, 3 = Approved, 9 = …

### 5.6 `PriorityTasks.asp`

- **Source**: `Comments` table joined with `Tables` — `WHERE FromCode = <Session("Code")> AND FollowUpComplete = 0 AND FollowUpRequired = -1 ORDER BY FollowUpDate`.
- **Rendering**: each item is a `.tl-priority-item` with:
  - Follow-up date (colour-coded: red if overdue, navy if ≤14 days, gray otherwise)
  - Area label (from `Tables.TableDesc`)
  - Comment text
  - Actions: **View** (opens the originating record via `parent.View*` — see `Global.js` helpers) + **Mark Complete** (`TableComments/Mark_FollowUpComplete_Proc.asp?CommentId=…`).

### 5.7 `Sidebar.asp`

Rendered as the left column of a two-column block below the main grid. Contains:
- **Priority Tasks** (via include of `PriorityTasks.asp` — or equivalent)
- **Unread TMail** — `SELECT TMail.*, Users.Name FROM TMail INNER JOIN Users ON Users.Code = TMail.FromCode WHERE ToCode = <my code> AND Read = 0 ORDER BY Date DESC`; rows have **Reply** / **Mark Read** / **Delete** links.
- **Recent Notices** — `SELECT Noticeboard.*, Users.Name FROM Noticeboard INNER JOIN Users ON Users.Code = Noticeboard.Code WHERE DateExpires >= Now() AND DateDiff('d', DateEntered, Now()) <= 14 ORDER BY DateEntered DESC`.

### 5.8 `ActivityTable.asp`

Main activity feed — recent records across Quotes, Invoices, and POs (typically last 10 of each, filtered to the user's division-visible list). Each row is a link that calls the matching `View*` helper from `Global.js`.

### 5.9 `HelpResources.asp`

Static block of tiles linking to:
- KB articles
- Contact support (`mailto:info@digitalresponse.com.au`, tel)
- Release notes (`Documentation/Release-Notes-20260415.md` equivalent)

## 6. Data loader — `Dashboard/Dashboard_Data.asp`

A single prelude include that fetches everything the widgets need. Lives under `/Clients/SalesEngineTL/Dashboard/Dashboard_Data.asp`.

### 6.1 Auth & identity rehydration

- Checks `Session("LoggedIn")` first, then `Request.Cookies("LoggedIn")`.
- Populates `dash_userName`, `dash_userCode`, `dash_userRole` (Administrator / Manager / User) from Session, with cookie fallback (`UserSettings("Name")`, `…("Code")`, `…("Admin")`).
- Derives `dash_userTypeId` from `UserSettings("UserTypeID")` cookie, falling back to `Session("UserTypeId")`.
- Sets `dash_isDirector` for visibility gates (see §4.1).
- Sets `dash_strWorkingDir` — defaults to `/Clients/SalesEngineTL`.

### 6.2 Metrics (only run when `dash_isDirector = true`)

All queries use `SafeExecute` + `CloseRS`. Defaults are `0` when the query fails or returns null.

| Variable | Query |
|---|---|
| `dash_thisMonthQuotes` | `COUNT(*) FROM Quotes WHERE Month(QuoteDate) = <cur> AND Year(QuoteDate) = <year>` |
| `dash_thisMonthQuotesWon` | Same + `SUM(IIf(QuoteStatusId IN (4,10), 1, 0))` |
| `dash_thisMonthQuotesValue` | Same + `SUM(IIf(QuoteStatusId IN (4,10), NettPriceTotal, 0))` |
| `dash_lastMonthQuotesWon` | Same shape, previous month |
| `dash_thisMonthInvoices`, `dash_thisMonthInvoiceValue` | `COUNT(*)`, `SUM(NettPriceTotal)` FROM Invoices for current month |
| `dash_lastMonthInvoices` | Previous month |
| `dash_ytdQuotesWon`, `dash_ytdQuotesValue` | FROM Quotes WHERE `QuoteStatusId IN (4,10)` AND `Year(QuoteDate) = <year>` |
| `dash_lastYearYTDQuotesValue` | Same for last year, capped to `Month(QuoteDate) <= <current month>` |
| `dash_pendingQuotesOver30Days` | `COUNT(*) FROM Quotes WHERE QuoteStatusId IN (1,2) AND QuoteDate < DateAdd('d', -30, Now())` |
| `dash_invoicesOverdue` | `COUNT(*) FROM Invoices WHERE InvoiceStatusId = 2 AND InvoiceDate < DateAdd('d', -30, Now())` |
| `dash_pendingApprovalPOs` | `COUNT(*) FROM PurchaseOrders WHERE POStatusId = 2` |
| `dash_monthlyQuotesThisYear(1..12)` | 12 queries — `SUM(NettPriceTotal)` by month, this year, won only |
| `dash_monthlyQuotesLastYear(1..12)` | Same, last year |
| `dash_monthlyInvoicesThisYear(1..12)` | 12 queries — `SUM(NettPriceTotal)` by month, this year |

> **Performance observation** (baseline): the chart loops run **36 separate `SafeExecute` calls** per Director pageview. These could be collapsed into 1 grouped query each. This is the current behaviour — noted for future optimisation.

### 6.3 No-director path

When `dash_isDirector = false`, Dashboard_Data only runs the auth/identity block. The widgets that depend on the metrics render empty placeholders or don't render at all (guarded inline in each widget).

## 7. Actions available on the Dashboard

| Action | Trigger | Target |
|---|---|---|
| Go to a module | Click tile | `<WD>/<Module>/` (Contacts, Quotes, …) |
| Quick-Jump by ID | Fill ID + Type, press Go | `QuickNav.asp` → View page |
| Open a record | Link in recent/priority/exception rows | `parent.ViewXxx(...)` from `Global.js` |
| Mark follow-up complete | "Mark Complete" link | `TableComments/Mark_FollowUpComplete_Proc.asp?CommentId=…` |
| Reply to TMail | "Reply" link | `TMail/Reply.asp?TMailId=…` |
| Mark TMail read | "Mark Read" link | `TMail/MarkRead_Proc.asp?TMailId=…` |
| Delete TMail | "Delete" link | `TMail/Del_Proc.asp?TMailId=…&Portal=True` |
| Submit an expense | Quick-access button (in `Portal.asp` sidebar) | `Expenses/Add.asp` |
| Log a Call Report | Quick-access button | `CallReports/Add.asp` |
| Create new Quote | Quick-access button | `Quotes/Add.asp` |

## 8. Legacy equivalents (reference only)

- `Portal.asp` (Clients/SalesEngineTL/Portal.asp) — older dashboard. Renders a Welcome banner, Quick Actions, a Recent Activity card, a System Status card, and the three legacy data tables (unread TMail, 14-day Noticeboard, overdue Follow-Ups).
- `PortalFrame.asp` (site root) — legacy pre-frameset portal. Contains heavy approvals logic: PO Approvals, PO Approvals Days Lapsed, Quote Approvals, Quote Approvals Days Lapsed, TMail, Noticeboard, and a Follow-Ups table with colour-coded overdue banding (red ≥ 0 days late, navy ≤ 14 days, gray > 14 days). Flagged as legacy — see `99-Deprecated-Legacy.md`, but approval logic there is still canonical for the approvals workflow reference.

## 9. UX conventions

- Header nav stays pinned to the top (via `Header.asp`).
- All widget cards use the modern `tl-card` look (white background, rounded 12 px, shadow, subtle border top accent).
- Status pills use `.tl-status`, `.tl-status-issued`, `.tl-status-approved`, `.tl-status-declined`, etc.
- Buttons in widgets use `.tl-btn`, `.tl-btn-primary`, `.tl-btn-secondary`, `.tl-btn-danger`; the "Log Out" micro-button uses `.tl-btn-logout`.
- Exceptions and priority items use translate-on-hover to feel interactive.
- Colours: red/orange for exceptions (`#fed7d7`, `#feb2b2`, `#fbd38d`, `#f6ad55`); teal/blue for success metrics.
