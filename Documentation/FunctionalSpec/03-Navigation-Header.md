# 03 — Navigation, Header & Global UI Chrome

Status: **IN REVIEW** — verified against source.

Covers the top navigation band (`Header.asp`), the embedded Global Search modal, the Ask AI launcher, and the ancillary root-level pages that form the persistent chrome.

---

## 1. Files

| File | Role |
|---|---|
| `Clients/SalesEngineTL/Header.asp` | Modern branded header + top navigation — injected by `Default.asp` (top frame) and by every module page that takes over `_top`. |
| `System/ssi_Header.inc` | Legacy JS include — disables right-click (except on dev + FilesLibrary) and emits server/local time as HTML comments. |
| `System/ssi_Header_Techlight.inc` | Sets `strWorkingDir = TL_WORKING_DIR`, defines `IsActive(pageName)` and `IsDirector()` VBScript helpers. Pulled in at the very top of `Header.asp`. |
| `Clients/SalesEngineTL/QuickNav.asp` | Quick-ID jump dispatcher — accepts `Type` + `ID` and redirects to the correct View page. |
| `Clients/SalesEngineTL/LastUpdated.asp` | Small "data last updated at…" badge (used by refresh buttons). |
| `Clients/SalesEngineTL/Updating.asp` | Shown when the DB file is momentarily unreachable (redirected to from `ssi_dbConn_open.inc`). |
| `Clients/SalesEngineTL/NoRecords.asp` | Empty-grid filler used inside list iframes. |

## 2. Header — `Clients/SalesEngineTL/Header.asp`

### 2.1 Purpose

Persistent 70-px-high top strip providing:
- Brand identity (Techlight logo)
- Decorative SVG band
- User panel (name, role, circular avatar with initial)
- Primary navigation
- Global search modal launcher
- "Ask AI" launcher (pop-up)
- Logout shortcut

It also **logs every non-asset page visit** into `UserHistory` (see §2.5).

### 2.2 Visibility rules

The whole nav bar only renders **if the user is logged in** (`Request.Cookies("LoggedIn") = true`).

```asp
If Request.Cookies("LoggedIn") <> "" Then
  If CBool(Request.Cookies("LoggedIn")) Then … render nav …
```

Before that gate, the header still renders the logo + decorative strip but no nav/user panel.

### 2.3 Structure (top → bottom)

```
<header class="tl-header">
  <div class="tl-header-top">                        [top band, dark gradient]
    <a class="tl-logo">…techlight-logo.svg…</a>      [links to Default.asp target="_parent"]
    <div class="tl-header-decor">                    [decorative SVG curves + dots]
    <div class="tl-user-panel">
      <span class="tl-user-name">                    [= userName]
      <span class="tl-user-role">                    ["Administrator" or "User"]
      <div class="tl-user-avatar">                   [first letter of name]
    </div>
  </div>
  <nav class="tl-nav">                               [only if logged in]
    <ul class="tl-nav-list">
      …nav items (see 2.4)…
    </ul>
  </nav>
</header>
<div id="searchModal" class="tl-modal">              [hidden by default]
  <form action=".../GlobalSearch.asp" method="GET" target="_top">
    <input name="q" id="searchModalInput" required>
    <button>Search</button>
  </form>
</div>
```

### 2.4 Navigation items

Nav is rendered as `<li class="tl-nav-item">` entries inside `.tl-nav-list` (flex-centered).

| # | Label | Icon | Href | Target | Active marker |
|---|---|---|---|---|---|
| 1 | **Home** | house | `strWorkingDir & "/Dashboard.asp"` | `MainFrame` | `IsActive("Dashboard")` |
| 2 | **Contacts** | people | `strWorkingDir & "/Contacts/"` | `_top` | `IsActive("contacts")` |
| 3 | **Quotes** | file-with-lines | `strWorkingDir & "/Quotes/"` | `_top` | `IsActive("quotes")` |
| 4 | **Invoices** | table-doc | `strWorkingDir & "/Invoices/"` | `_top` | `IsActive("invoices")` |
| 5 | **Purchases** | shopping-bag | `strWorkingDir & "/PurchaseOrders/"` | `_top` | `IsActive("PurchaseOrders")` |
| 6 | **Setup** | cog | `strWorkingDir & "/Setup/"` | `_top` | `IsActive("setup")` |
| 7 | **Users** | people | `strWorkingDir & "/Users/"` | `_top` | `IsActive("users")` |
| 8 | **Admin** (only if `IsDirector()`) | stack | `strWorkingDir & "/Admin/"` | `_top` | `IsActive("admin")` |
| 9 | **Search** (button) | magnifier | — opens `#searchModal` | — | — |
| 10 | **AI** (pill button) | chat-bubble | `javascript:openAskAI()` → `window.open(strWorkingDir & "/AskAI.asp", "AskAI", "width=450,height=600,…")` | new window | — |
| 11 | **Logout** (right) | power-off | `strWorkingDir & "/Portal/LogOff.asp"` | `_top` | — |

- `IsActive(name)` returns the CSS class `"active"` iff `currentPage` (lower-cased `SCRIPT_NAME`) contains `name`. See `ssi_Header_Techlight.inc`.
- `IsDirector()` returns true only when `UserTypeID == 1` (Director).

### 2.5 User-activity logging

At the top of `Header.asp`:

```asp
On Error Resume Next
If CBool(Request.Cookies("LoggedIn")) Then
    histUrl = Request.ServerVariables("URL")
    If Request.QueryString <> "" Then histUrl = histUrl & "?" & Request.QueryString
    histCode = Request.Cookies("UserSettings")("Code")
    ' Skip _proc / .inc / .js / .css / .svg / .png
    If <not an asset/processor> And dbConn.State = 1 Then
        SafeExecute "INSERT INTO UserHistory (UserCode, PageUrl, PageTitle) VALUES ('…','…','')"
    End If
End If
```

Note: the `PageTitle` column is always written blank here; the title is populated elsewhere if at all. The table is used by the "Last pages visited" widget and admin audit screens.

### 2.6 Search modal

- **Trigger**: the `Search` nav item (`onclick="openSearchModal(event)"`).
- **Input**: single text field `q` (placeholder "Search ID, Name, Keyword..."), required.
- **Form target**: `GlobalSearch.asp` (`target="_top"`, method `GET`).
- **Hotkey**: pressing `Escape` anywhere closes the modal (via a global keydown listener).
- **Click-outside**: clicking the dim backdrop closes the modal.
- Help text underneath: "Search across Contacts, Quotes, Invoices, and Purchase Orders by keyword or reference ID."

### 2.7 JavaScript helpers defined in the header

| Function | Purpose |
|---|---|
| `openSearchModal(e)` | Opens `#searchModal`, focuses the input after 100 ms. |
| `closeSearchModal()` | Hides the modal. |
| `toggleDropdown(e)` | Toggles an `.active` class on the parent `.tl-nav-dropdown` (reserved for future multi-item dropdowns). |
| `openAskAI()` | Opens `AskAI.asp` in a 450×600 pop-up (window name `AskAI`, scrollable, resizable). |
| Global `click` listener | Closes any open `.tl-nav-dropdown` when clicking outside it; closes the search modal on backdrop click. |
| Global `keydown` listener | Closes the search modal on `Escape`. |

### 2.8 Style overrides (inline in header)

```css
.tl-nav-link          { color: #fff !important; }
.tl-nav-link:hover    { color: #fff !important; background: rgba(255,255,255,0.1) !important; }
.tl-nav-item          { display: flex; align-items: center; }
button.tl-nav-link:focus,
button.tl-nav-link:active { outline: none; background: rgba(255,255,255,0.1) !important; color: white !important; }
```

The rest of the look-and-feel comes from `/System/Style_Modern.css` (primary) and `/System/Style_Techlight.css` (module-specific tokens).

---

## 3. QuickNav — `Clients/SalesEngineTL/QuickNav.asp`

A tiny router used by the legacy "Quick navigation" widget in `PortalFrame.asp` (and any bookmark that wants to jump to a record by ID).

### URL & parameters

- **URL**: `/Clients/SalesEngineTL/QuickNav.asp`
- **Query params**:
  - `ID` (numeric) — required
  - `Type` ∈ `Quote | PurchaseOrder | Invoice | Contact`

### Behaviour

```
Type = "Quote"          → Redirect Quotes/View.asp?Qid=<ID>
Type = "PurchaseOrder"  → Redirect PurchaseOrders/View.asp?POid=<ID>
Type = "Invoice"        → Redirect Invoices/View.asp?InvoiceId=<ID>
Type = "Contact"        → Redirect Contacts/Edit.asp?ContactId=<ID>
anything else           → Redirect Dashboard.asp?Msg=Invalid+Quick+Nav+Type
Non-numeric ID          → Redirect Dashboard.asp?Msg=Please+enter+a+valid+numeric+ID
```

### Business rules

- No permission check — relies on downstream View/Edit pages to enforce their own gates.
- No DB access.

---

## 4. Global Search — `Clients/SalesEngineTL/GlobalSearch.asp`

Covered in detail in `41-GlobalSearch-AskAI.md`. Summary here for completeness:

- **URL**: `/Clients/SalesEngineTL/GlobalSearch.asp?q=<term>`
- **Auth gate**: `If Not Request.Cookies("UserSettings")("LoggedIn") Then Response.Redirect("/Default.asp")`
- **Search scope**:
  - `Companies` — `Company LIKE '%<q>%'`
  - `Contacts` (via view `Contacts_WithCustomersAndSuppliers_V2`) — `Name LIKE '%<q>%' OR Email LIKE '%<q>%'`
  - **If `q` is numeric** also searches:
    - `Quotes_WithCustomersAndSuppliers` by `Qid`
    - `Invoices` by `InvoiceId` OR `Qid`
    - `PurchaseOrders` (joined with `PurchaseOrderStatus` + contacts view) by `POid`
- **Rendering**: each hit is an `.search-item` card (icon + title + meta badges). Cards link with `target="_parent"` so clicking navigates the top frame to the record.
- **No results state**: empty-state panel with magnifier icon and friendly message.
- **Empty query**: renders "Please enter a search term."

---

## 5. Ask AI — `Clients/SalesEngineTL/AskAI.asp`

Covered in detail in `41-GlobalSearch-AskAI.md`. Summary here:

- **URL**: `/Clients/SalesEngineTL/AskAI.asp`
- **Opened via**: `openAskAI()` from the header, as a 450×600 pop-up named `AskAI`.
- **UI**: dark-themed chat pane (header "Ask AI — Powered by Azure OpenAI", scrollable message list, rounded input with send button).
- **Model**: Azure OpenAI `gpt-4o` (deployment at `https://techlight-ai.openai.azure.com/…`).
- **Tools**: fetches the MCP tool list from `http://localhost/MyDeskMCP/mcp/v1/tools/list` (sends `X-User-Code: <Session("Code")>`), surfaces them to OpenAI as `tools`, and executes tool calls via `http://localhost/MyDeskMCP/mcp/v1/tools/call` in a loop until the model produces a text answer.
- **Credentials**: API key is hard-coded in `AskAI.asp` (`AZURE_OPENAI_KEY = "B2R5..."`). This is the as-is baseline — flag as a security concern.
- **Initial greeting**: "Hello \<Session("Name")\>! I'm your Techlight MyDesk AI assistant…"
- **Formatting**: response markdown is minimally post-processed client-side (`**bold**` → `<strong>`, `\n` → `<br>`).

---

## 6. Utility pages

### 6.1 `LastUpdated.asp`

Renders a small inline timestamp indicating when data was last refreshed. Used as a hint/badge in list pages. Reads server time via `Now()` (converted through `ServerToEST`).

### 6.2 `Updating.asp`

Reached when `ssi_dbConn_open.inc` fails to find the MDB (or can't open it). Displays a "System is being updated, please try again shortly" message styled with the Techlight palette. No navigation (the header can't render — no DB).

### 6.3 `NoRecords.asp`

Tiny fragment (276 bytes) used inside list grid iframes when the query returns no rows. Renders a single centered message (e.g. "No matching records.") styled to match the grid.

### 6.4 `Del_Proc.asp` (root-level)

Generic delete handler used by `Global.js:deleteRecord(id)` when the caller doesn't have a module-specific `Del_Proc.asp` in context. Accepts `?Id=…` and `?Table=…` (or equivalent), runs the delete via `SafeExecute`, then returns.

*(Note: most modules have their own `Del_Proc.asp`; this root variant is a fallback.)*

---

## 7. Frame-breaking / navigation conventions

- Logo click: `target="_parent"` — if the parent is already `Default.asp`, a JS guard calls `parent.location.reload()` instead of reloading the frameset.
- Top-level module links use `target="_top"` to replace the frameset with a full-page module view. Each module then re-injects `Header.asp` inline at the top of its own `Default.asp`.
- Home link uses `target="MainFrame"` to stay inside the original frameset (so the header does not have to reload).
- Logout uses `target="_top"` — `LogOff.asp` then redirects back to `/Default.asp` (login) with a success message.

This inconsistency (some links replace the frameset, some don't) is intentional — it lets the Dashboard keep its fast single-frame experience while module pages take over the full viewport for grids/forms.
