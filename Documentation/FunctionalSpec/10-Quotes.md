# 10 — Quotes

Status: **IN REVIEW** — verified against source in `Clients/SalesEngineTL/Quotes/`.

Customer-facing sales quotations: raise, cost, price, route through a multi-stage approval chain, issue (print/email), and convert to invoice or purchase order. This is the most business-critical module.

---

## 1. Files

| File | Role |
|---|---|
| `Default.asp` | Filter/list page (modern `.tl-*` styling, nested `MyIFrame`). |
| `IFrame.asp` | The grid iframe — renders the filtered quote list (HTML table, not ActiveWidgets in this folder). |
| `Add.asp` | Stage-1 "choose a division" gate. **Currently short-circuits** with `Response.Redirect("Add2.asp?DivisionId=1")` — i.e. division is forced to 1. |
| `Add2.asp` | Stage-2 full quote form (header, line items, third-party items, totals). Submits to `Add_Proc.asp`. |
| `Add_Proc.asp` | Insert handler: creates Quote + QuoteContents + QuoteThirdPartyContents + QuoteAudit rows, auto-approves if user's margin authority allows. |
| `Edit.asp` | Edit form (22 KB) — same shape as Add2 but with pre-populated line items and approval affordances. |
| `Edit_Proc.asp` | Update handler: wipes & re-inserts line items, server-side recalculates totals/margin, writes audit trail, emails next approver when `QuoteStatusId = 9`. |
| `View.asp` | Read-only printable/PDF-friendly view. Embeds `NavBar.asp`. On `?Email=true` or `?Print=true` forces status to Issued. |
| `NavBar.asp` | Top action bar rendered inside `View.asp` — Approve/Decline/Invoice/PO/Email/Print buttons gated by quote status. |
| `Approve.asp` | One-click approval (approver role). Auto-promotes to status 10 when approver is final-in-chain. |
| `Decline.asp` | One-click decline (approver role). Emails owner. |
| `UpdateStatus.asp` | Form: choose new status from filtered `QuoteStatus` dropdown. |
| `UpdateStatus_Proc.asp` | Writes new status + audit + emails; if new status is Accepted (4), prompts "Invoice now?". |
| `Copy_Proc.asp` | Deep-copy a quote (header + line items), returns new Qid. |
| `Del_Proc.asp` | Delete quote + cascade (Comments, QuoteContents, QuoteThirdPartyContents, QuoteAudit, Quotes). |
| `Email.asp` | Compose screen — recipient, attention, notes. Posts to `GenerateQuote.asp`. |
| `Email_Proc.asp` | Legacy email sender (sleeps 5s, uses CDO) — superseded by the ASP.NET path. |
| `GenerateQuote.asp` | Thin shim: redirects to `/MyDeskASPNet/GenerateQuote.aspx?Mode=&Qid=&Attention=&ToEmail=&FromFax=&ToFax=&Notes=&WorkingDir=…` for PDF generation. |
| `ViewHistory.asp` | Renders `QuoteAudit` log + all `Comments` for this quote. |
| `Report.asp` | Filterable aggregate printable report (same filter params as Default). |
| `Transporter.asp` | One-liner: navigate parent to `JobOrders/Add.asp?Qid=…` (Quote → Job Order). |
| `Transporter_QuoteToInvoice.asp` | One-liner: navigate to `Invoices/Add.asp?Qid=…&DivisionId=…`. |
| `Transporter_QuoteToPO.asp` | One-liner: navigate to `PurchaseOrders/Add2.asp?Qid=…&DivisionId=…`. |
| `Files/` | PDF output directory (`Q<Qid>.pdf`) and any attached sales documents (via `TableFiles`). |

---

## 2. URL map

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/Quotes/` (or `Default.asp`) | Filter + list |
| `…/Quotes/Add.asp` | New quote — choose division (currently forced to 1) |
| `…/Quotes/Add2.asp?DivisionId=<n>` | New quote form |
| `…/Quotes/Edit.asp?Qid=<n>` | Edit quote |
| `…/Quotes/View.asp?Qid=<n>[&Print=True][&Email=true]` | Read-only view |
| `…/Quotes/UpdateStatus.asp?Qid=<n>` | Change status |
| `…/Quotes/Approve.asp?Qid=<n>` | Approve (approver-only) |
| `…/Quotes/Decline.asp?Qid=<n>` | Decline (approver-only) |
| `…/Quotes/Email.asp?Qid=<n>` | Compose email |
| `…/Quotes/GenerateQuote.asp` (POST) | Generates PDF via .NET and emails/prints |
| `…/Quotes/Copy_Proc.asp?Id=<n>` | Deep-copy a quote |
| `…/Quotes/Del_Proc.asp?Id=<n>` | Delete |
| `…/Quotes/ViewHistory.asp?Qid=<n>` | Audit trail + comments |
| `…/Quotes/Report.asp` (POST) | Printable report |
| `…/Quotes/Transporter.asp?Qid=<n>` | → Job Order |
| `…/Quotes/Transporter_QuoteToInvoice.asp?Qid=<n>&DivisionId=<n>` | → Invoice |
| `…/Quotes/Transporter_QuoteToPO.asp?Qid=<n>&DivisionId=<n>&Parent=<bool>` | → Purchase Order |

---

## 3. Access control

Every page in this module starts with:

```asp
If Not Request.Cookies("DivisionIdsAccess")("Quotes") <> "0" Then
    Response.Redirect("../Portal/AccessDenied.asp")
```

i.e. the user must have at least one quote-enabled division in their CSV access list (this flag is set at login, see `02-Authentication-Portal.md §3`).

Additional per-action gates:

| Action | Gate |
|---|---|
| **See another user's quotes** | `Request.Cookies("UserSettings")("Manager") = true` — otherwise `strCode` is forced to the user's own `Code`. |
| **Approve** (show Approve/Decline buttons in `NavBar.asp`) | `(QuoteStatusId ∈ {1,9}) AND (GetQuoteLineApprover_Check(Qid, Code) OR CheckForLine(ownerCode, Code, Qid, True, False))`. |
| **Immediate approval to status 10** (`Approve.asp`) | `IsDirector(Code)` OR `GetQuoteLastLineApprover(Qid) = userName` OR `UserRoles.QuoteApprovalLimit >= Quotes.NettPriceTotal`. |
| **Delete** (`Del_Proc.asp`) | `UserTypeId ∈ {5, 6}` OR `QuoteStatusId = 1` (Draft). Issued/Won quotes cannot be deleted by standard users. |
| **Manager filter override** | `Manager = true` can set `Filter_Code` to another user. Non-managers always see only their own. |
| **Print button** | Only visible once `QuoteStatusId ∈ {2,3,4,7,10}` OR the quote's last approver has already approved. Special case: user `TL0084` (Hannah G) sees a Print button even on earlier statuses. |
| **Status dropdown filtering** (`UpdateStatus.asp`) | If the quote has reached 2/4/7/10 OR user is `UserTypeId=6`, show every status; otherwise hide statuses 2/3/4/5/7/10/11 so non-approvers can't promote into approval-only states. |

---

## 4. Data model

### 4.1 `Quotes` (header)

| Column | Type | Notes |
|---|---|---|
| `Qid` | AutoNumber PK | |
| `QuoteDate` | DateTime | Set to `ServerToEST(Now())` on create and on every Save. |
| `Code` | varchar(FK Users) | Quote **owner** (not necessarily the creator). |
| `SenderCode` | varchar(FK Users) | Who actually sends the email (`Edit_Proc.asp`). Defaults to `Code` if blank. |
| `ContactId` | long (FK Contacts) | Customer contact. |
| `DivisionId` | int | |
| `QuoteNumber` | varchar | Free-text customer reference number. |
| `QuoteStatusId` | int (FK QuoteStatus) | See §5. |
| `Attention` | varchar | "Attention: <name>" in email and PDF. |
| `Reference` | varchar(50) | Project name (the "Project" column in the list). |
| `Terms` | memo | Payment/freight terms — default `"F.I.S. via general road freight"`. |
| `Delivery` | varchar(50) | |
| `Validity` | long | Days — default 30. |
| `InternalNotes` | memo(1500) | Visible in `View.asp` only (not on PDF/email). Hidden if `ClientSettings("HasInternalNotes") != "true"`. |
| `CustomerNotes` | memo(1500) | Visible to customer (PDF + email). |
| `PPriceTotal` | decimal | "P" price (legacy pricing tier) — carried by Copy_Proc but not editable on Add2. |
| `UnitCostTotal` | decimal | Sum of line `UnitCostSubTotal`. |
| `NettPriceTotal` | decimal | Sum of line `ExtNettPrice`. |
| `Margin` | decimal | `(NettPriceTotal - UnitCostTotal) / NettPriceTotal × 100`. |
| `QuoteCOSId` | int (FK QuoteCOS) | Conditions-of-sale template (enabled by `ClientSettings("HasQuoteCOS") == "true"`). |
| `IncludeInReporting` | boolean | Shown on some reports; copied by `Copy_Proc`. |
| `POid` | long | Cross-reference when this quote was originated from a PO. |
| `RealQid` / `Revision` | long / long | Revision-chain pointers (written by `Copy_Proc` as 0/0; populated in revision workflows). |

### 4.2 `QuoteContents` (line items — own products)

| Column | Notes |
|---|---|
| `QuoteItemId` | PK |
| `Qid` | FK Quotes |
| `ProductId` | FK Products (0 on Edit_Proc — it clears ProductId because lines are wiped and re-inserted without the FK) |
| `Quantity`, `Units`, `Days`, `Type` | Quantity OR (Units × Days) — `Days > 0 AND Units > 0` triggers the `Units × Days × rate` calc path |
| `ProductCode`, `Description` | Denormalised product data |
| `UnitCost`, `MinNettPrice`, `NettPrice`, `PPrice` | per-unit pricing |
| `UnitCostSubTotal`, `PPriceSubTotal`, `ExtNettPrice` | line totals recalculated server-side in `Edit_Proc` |
| `MinExtNettPrice` | Minimum allowed extended Nett price (drives the `boolNotApproved` flag) |

**Minimum price rule**: if `NettPrice < MinNettPrice` on any line, `boolNotApproved = true` is raised but **not blocking** — the quote is still saved; the approval chain handles it (see §6).

### 4.3 `QuoteThirdPartyContents` (line items — bought-in supply)

| Column | Notes |
|---|---|
| `QuoteThirdPartyId` | PK |
| `QuoteId` | FK (column-name differs from `Qid` used elsewhere — retained as-is) |
| `Description`, `Supplier`, `QuoteNumber`, `QuoteDate`, `ExpiryDate`, `SupplierPartNumber`, `OurPartNumber` | Supplier-quote reference data |
| `Quantity`, `Type`, `UnitCost`, `NettPrice`, `Margin`, `ExtNettPrice`, `TotalCost` | Pricing |

### 4.4 `QuoteAudit`

One row per state change:

```
Qid | Code (who) | Action (varchar) | DateEntered
```

Actions written by this module: `Created`, `Auto-Approved`, `Updated`, `Approved`, `Status changed to <name>`, `Declined`, `Printed`, `Issued by email to <addr>`.

### 4.5 `QuoteApproval`

One row per approval/decline event (used by `CheckForLine` / `GetQuoteLineApprover_Check`):

```
Qid | Code (approver)
```

Populated by `Approve.asp` and `Decline.asp`. Wiped by `Edit_Proc.asp` (so that re-editing a pending-approval quote re-starts the chain).

### 4.6 `QuoteStatus` (lookup)

Observed IDs (derived from the code paths):

| ID | Status | Notes |
|---|---|---|
| 1 | Draft | Initial state on insert |
| 2 | Issued | Set when printed/emailed (`View.asp`), or auto-approved by owner |
| 3 | (approved state) | Hidden from non-approver status dropdown |
| 4 | Accepted / Won | `UpdateStatus_Proc` triggers "Invoice now?" modal; counted as `Won` in Dashboard KPIs |
| 5 | Lost / Dropped | Hidden from non-approver status dropdown |
| 7 | (intermediate) | Unlocks Print/Invoice/PO buttons |
| 8 | (approved) | Printed/emailed still flips to Issued |
| 9 | Pending Approval | Approve/Decline flow |
| 10 | Fully Approved | Counted as `Won` alongside 4 in Dashboard |
| 11 | Declined | Set by `Decline.asp` |

Cross-check with the actual `QuoteStatus` rows in `Techlight2.mdb` for the display names.

---

## 5. Status lifecycle

```
                 ┌───────────[Edit_Proc with QuoteStatusId=9]──────┐
                 ▼                                                  │
  (create)   Draft (1) ──[Add_Proc auto-approve if within limit]─► Issued (2)
                 │                                                  ▲
    [Submit for │                                                   │ [Print / Email]
     approval]  ▼                                                   │
        Pending Approval (9) ──[Approve.asp]─► Fully Approved (10) ─┘
                 │                 │
                 │                 └─[if not final approver]─► stays at 9, emails next approver
                 │
                 └──[Decline.asp]──► Declined (11)   [emails owner]

Any "active" state (2,3,4,7,10) → [UpdateStatus Accept] → Accepted (4) ─► prompt "Invoice now?"
Any state ── [UpdateStatus] ──► arbitrary target allowed by gate (see §3)
```

### Auto-approval

`Add_Proc.asp` immediately promotes status 1 or 9 → 2 if **either**:
- `GetQuoteLineApprover_Check(Qid, Code)` (user is a defined approver for this quote's margin), OR
- `CheckForLine(ownerCode, userCode, Qid, True, False)` (user is in the approver line for the quote owner).

An `Auto-Approved` audit row is written.

### Emails

- `Edit_Proc.asp` (status=9 path): if current user is the final approver → email owner "approval process completed"; else email next approver "waiting for your approval".
- `UpdateStatus_Proc.asp` (status=9): email next approver.
- `Decline.asp`: email owner.
- `Approve.asp` in the current source has both `SendMail` calls commented out (see lines 44, 47). The audit trail is still written. **Flag**: approvers today get no notification; was intentional or regression — document as current state.

All emails use `Request.Cookies("UserSettings")("Email")` as the `From:` and build the body from `QuoteDetails_ForEmail(Qid)` (helper in `ssi_Functions_Quote.asp`).

---

## 6. Approval chain helpers

Defined in `ssi_Functions_Quote.asp` (and referenced by the Quote and PO modules):

| Function | Purpose |
|---|---|
| `GetQuoteNextLineApprover(Qid)` | Returns the **name** of the next approver required. |
| `GetQuoteNextLineApprover_Email(Qid)` | Their email. |
| `GetQuoteLastLineApprover(Qid)` | The final approver — returns `"Already approved"` if all rows in `QuoteApproval` cover the chain. |
| `GetQuoteLineApprover_Check(Qid, Code)` | Boolean — is `Code` the next-in-line approver. |
| `CheckForLine(ownerCode, userCode, Qid, boolUp, boolDown)` | Walks the `Users.LineManagerCode` chain (up or down) to see if `userCode` is in `ownerCode`'s approval line. |
| `QuoteDetails_ForEmail(Qid)` | Multi-line plaintext summary (reference, customer, totals) appended to alert emails. |

The chain is driven by:
- `Users.LineManagerCode` hierarchy,
- `UserRoles.QuoteApprovalLimit` (Directors have `IsDirector(code) = true` bypass),
- `QuoteApproval` table (who has already approved this quote).

---

## 7. List page — `Default.asp` + `IFrame.asp`

### Filter form (Default.asp)

| Field | Default | Bound to |
|---|---|---|
| **Date From** | today − 3 months | `DateFrom` |
| **Date To** | today + 1 day | `DateTo` |
| **User** | logged-in user's Code (Managers: "All") | `Code` via `GetAccessCodesList(Code, UserTypeID)` (helper filters to users the manager can see) |
| **Customer** | "All companies" + "Not an account" (CompanyId=142) | `CompanyId` — options loaded from `Companies ∩ Contacts` where division is in user's access or contact `.Code` matches the user |
| **Customer Search** | blank | `CustomerSearch` — `CompanyName LIKE '%<text>%'` |
| **Division** | user's cookie `DivisionId` | `DivisionId` — options from `Divisions WHERE Quotes = True AND DivisionId IN (<access list>)` |
| **Status** | "All (Active)" = 555 | `QuoteStatusId`. 555 → `NOT IN (4,5)`; 0 → all including Won/Lost; otherwise exact match. |
| **Keyword** | blank | `Keyword` — searches `Reference`, `Terms`, `InternalNotes`, `CustomerNotes`, `Qid`, `Users.Name`. |

Two submit buttons:
- **Filter** → targets `IFrame.asp` in the child iframe (refreshes grid).
- **Generate Report** → targets `Report.asp` in the child iframe (printable report). Guard: must pick a division (`DivisionId != 555`).

### Grid (IFrame.asp)

Underlying SQL (simplified):

```sql
SELECT DISTINCT Quotes.Qid, Contacts_WithCustomersAndSuppliers_V2.CompanyName, Quotes.Reference,
                QuoteStatus.QuoteStatus, Quotes.UnitCostTotal, Quotes.NettPriceTotal, Quotes.Margin,
                Quotes.QuoteDate, Users.Name AS Originator
FROM Contacts_WithCustomersAndSuppliers_V2
  INNER JOIN (QuoteStatus INNER JOIN (Divisions INNER JOIN (Users INNER JOIN Quotes
      ON Users.Code = Quotes.Code) ON Divisions.DivisionId = Quotes.DivisionId)
    ON QuoteStatus.QuoteStatusId = Quotes.QuoteStatusId)
    ON Contacts_WithCustomersAndSuppliers_V2.ContactId = Quotes.ContactId
WHERE <filters>
ORDER BY Qid DESC
```

**Division filter fallback**: if no `DivisionId` was chosen AND `Cookies("DivisionId") != 2`, the query scopes to the user's default division.

**Status filter**: `555` → `QuoteStatusId NOT IN (4,5)`; `0` → no filter; else exact.

**Columns rendered** (HTML table, not ActiveWidgets):

| Header | Value |
|---|---|
| Quote # | `<strong>Qid</strong>` |
| Company | `CompanyName` |
| Project | `Reference` |
| Status | `tl-badge` pill (`tl-badge-success` for Won/Approved, `-warning` for Pending/Submitted, `-danger` for Lost/Cancelled, `-info` otherwise) |
| Cost Ex GST | `$NNN.NN` |
| Price Ex GST | `$NNN.NN` |
| Margin | `NN.NN%` |
| Date | `DD-MM-YYYY` (via `FormatDateU2`) |
| Actions | **View** + **Edit** pill buttons |

**Empty state**: if `rs.BOF And rs.EOF` → `MyRedirect("/Clients/SalesEngineTL/NoRecords.asp")` shows the "No matching records" fragment.

There's also an internal `activewidgets_html(Id, s, FieldName)` helper defined in the page (lines 118–243) that formats `DESCRIPTION`, `ACTION`, `HISTORY`, `FILES`, `QUNEXTAPPROVER`, `QULASTAPPROVER` magic tokens — this is a legacy code path retained for an ActiveWidgets variant but **not invoked** by the current HTML-table rendering.

---

## 8. Add flow (`Add2.asp` + `Add_Proc.asp`)

### 8.1 Form layout

The form is laid out as a classic 4-column table (width 760) and uses a mix of legacy `.Req`/`.TDAReq` markers and modern `.tl-input`/`.tl-select`/`.tl-btn` classes.

Header block:

| Field | Type | Required | Default |
|---|---|---|---|
| Contact | `select` (Contacts visible to user — special-cased for `TL0039` who sees only their own) | Yes (JS `checkForm`) | none |
| Quote Date | read-only label "Today" | — | `Now()` server-side |
| Status | read-only label "Draft" | — | 1 |
| Project | `input`(50) | No | blank |
| Terms | `textarea`(500) | No | `"F.I.S. via general road freight"` |
| Delivery | `input`(50) | No | blank |
| Conditions of Sale | `select` (only if `ClientSettings("HasQuoteCOS") = "true"`) | No | 0 |
| Validity | `input`(3) days | **Yes** (`checkForm` — must be numeric) | `30` |
| Customer Notes | `textarea`(1500) | No | blank |
| Internal Notes | `textarea`(1500) (only if `HasInternalNotes = "true"`) | No | blank |

### 8.2 Line-item grid

Dynamic: "Insert Item Line" button calls `Items_InsertLine(boolDivisionManager)` (defined in `System/Quotes.js`) which appends rows with:

| Field | Behaviour |
|---|---|
| Quantity | One numeric input. Use `Units × Days` alternative for rental-style lines. |
| Type | e.g. "Supply", "Hire" |
| Description | Bound to a product picker; "Create New Contact"-style pop-up pattern may apply. |
| UnitCost, MinNettPrice, NettPrice | Per-unit prices. `MinNettPrice` is the floor. |
| UnitCostSubTotal, ExtNettPrice | Auto-calculated per row. |
| Margin | Visible only to division managers (`boolDivisionManager = SearchArray(Cookies("DivisionIdsAccess")("ArrDivisionIdsManager"), DivisionId)`). |

Totals panel shows: `UnitCostTotal`, `NettPriceTotal`, `NettPriceTotalInc` (GST added client-side), `RealMargin`. All read-only; recalculated client-side by `Quotes.js` on every input change.

A **Third Party Supply** table (hidden by default via `style="display:none;"`) supports bought-in lines — see §4.3.

### 8.3 Validation (client-side)

```js
- Contact must be selected
- Validity must be non-empty and numeric
  (commented-out: margin < 100%; margin = 0 confirm)
```

### 8.4 Server-side insert (`Add_Proc.asp`)

```
1. INSERT INTO Quotes (QuoteDate, Code, ContactId, DivisionId, QuoteNumber, QuoteStatusId=1,
                       Reference, Terms, Delivery, Validity, InternalNotes, CustomerNotes,
                       UnitCostTotal, NettPriceTotal, Margin, QuoteCOSId)
2. lngQid = @@IDENTITY
3. Loop i=2..ItemLinesVal:
     If Quantity>0 OR (Units>0 AND Days>0):
         If NettPrice < MinNettPrice: boolNotApproved = true
         INSERT INTO QuoteContents (...)
4. Loop i=2..ThirdPartyLinesVal:
     If TP_Quantity>0: INSERT INTO QuoteThirdPartyContents (...)
5. INSERT INTO QuoteAudit ('Created')
6. Re-read Quote; if status=1/9 AND (GetQuoteLineApprover_Check OR CheckForLine):
     UPDATE Quotes SET QuoteStatusId=2
     INSERT INTO QuoteAudit ('Auto-Approved')
7. MyRedirect Default.asp?DivisionId=<n>&Msg=Quote+added
```

**Notes/risks**:
- All inputs are interpolated into SQL with only single-quote doubling (`Replace(x, "'", "''")`) — not parameterised.
- `intDays`, `intUnits` blank-to-zero normalisation is done manually.
- The `boolNotApproved` flag is raised but never read in this file — the approval is delegated to the line-approver helpers. Document as baseline behaviour.

---

## 9. Edit flow (`Edit.asp` + `Edit_Proc.asp`)

### 9.1 Shape

Edit.asp is a ~22 KB variant of Add2.asp that pre-populates every field from the `Quotes` row and renders existing line items with delete/re-order affordances. Same validation rules as Add. The submit button POSTs to `Edit_Proc.asp`.

### 9.2 Server-side update

```
1. DELETE FROM QuoteContents WHERE Qid = <n>
2. UPDATE Quotes SET <all editable fields> WHERE Qid = <n>
3. DELETE FROM QuoteThirdPartyContents WHERE QuoteId = <n>
4. DELETE FROM QuoteContents WHERE Qid = <n>      -- (second time — belt-and-braces)
5. DELETE FROM QuoteApproval WHERE Qid = <n>      -- reset approval chain
6. Loop ItemLinesVal → INSERT QuoteContents
     (server-side recalculates UnitCostSubTotal and ExtNettPrice)
7. Loop ThirdPartyLinesVal → INSERT QuoteThirdPartyContents
8. Recalculate totals from DB:
     SELECT SUM(UnitCostSubTotal), SUM(ExtNettPrice) FROM QuoteContents + QuoteThirdPartyContents
     → UPDATE Quotes SET UnitCostTotal, NettPriceTotal, Margin
9. INSERT INTO QuoteAudit ('Updated')
10. If new status = 9 (Pending Approval):
      If GetQuoteLastLineApprover() = current user OR "Already approved":
         UPDATE Quotes SET QuoteStatusId = 10 (fully approved)
         INSERT INTO QuoteAudit ('Approved')
         SendMail(owner, "Approved by <name>. You may now issue.")
      Else:
         SendMail(next approver, "Waiting for your approval.")
11. MyRedirect Default.asp?DivisionId=<n>&Msg=Quote+updated
```

**Key correctness behaviour**: server-side totals recompute in step 8 is intentional and documented in the code comment block `// BUG FIX: Server-side recalculation of quote totals`. Client-side totals can't be trusted.

---

## 10. View / Print / PDF (`View.asp` + `NavBar.asp` + `GenerateQuote.asp`)

### 10.1 View.asp modes

| Query param | Effect |
|---|---|
| `?Qid=<n>` | Normal in-browser view (NavBar + full quote layout). |
| `?Qid=<n>&Print=True` | Print-friendly rendering. Writes `Printed` audit; if current status is Draft/Pending/Approved/Issued (1/3/8/9) **forces status to 2 (Issued)**. |
| `?Qid=<n>&Email=true` | Same promotion rule; used when the .NET PDF generator scrapes the HTML for the PDF. Sets `dblGSTPercentage=10`, `dblCurrencyRate=1`, `strCurrencyPrefix="$"` to ensure a clean email-ready render. |

The status promotion is done in the page body, lines 50–73:

```
If boolPrint Or boolEmail:
  Select Case QuoteStatusId
    Case 9, 1, 8, 3: UPDATE Quotes SET QuoteStatusId = 2
    Case Else: (no-op)
```

### 10.2 NavBar.asp — button matrix

| Button | Shown when |
|---|---|
| Close [x] | Always |
| View Quote | Always |
| View History | Always |
| Update Status | Always |
| **Decline** (red) | `QuoteStatusId ∈ {1,9} AND (GetQuoteLineApprover_Check OR CheckForLine)` |
| **Approve** (red) | Same gate as Decline |
| Invoice Quote | `QuoteStatusId ∈ {2,3,4,7,10}` OR `GetQuoteLastLineApprover = "Already approved"` |
| Generate Purchase Order | Same gate as "Invoice Quote" |
| Email | Same gate |
| Print (conditional) | `Print=True` → renders immediate `print()` button. Else a "confirm then navigate with `?Print=True`" button. |
| Print (special) | If user is `TL0084` (Hannah G), show even on statuses that otherwise wouldn't allow it. |

### 10.3 PDF generation (`GenerateQuote.asp` → `/MyDeskASPNet/GenerateQuote.aspx`)

```
GenerateQuote.asp
  ↓ If QuoteStatusId = 1 → UPDATE to 2 (Issued)
  ↓
  Response.Redirect("/MyDeskASPNet/GenerateQuote.aspx
        ?Mode=<1|2|3>&Qid=<n>&Attention=<>&ToEmail=<>&FromFax=<>&ToFax=<>
        &Notes=<>&WorkingDir=/Clients/SalesEngineTL")

/MyDeskASPNet/GenerateQuote.aspx (.NET 4.8)
  ↓ ABCpdf: AddImageUrl("<host>/Clients/SalesEngineTL/Quotes/View.asp?Qid=<n>&Email=true")
  ↓ Saves to Server.MapPath("/Clients/SalesEngineTL/Quotes/Files/Q<n>.pdf")
  ↓ Redirects back to Quotes/Email_Proc.asp (legacy) OR sends email directly
```

Full .NET-side details in `50-ASPNet-Interop.md`. Key constants:
- PDF path: `/Clients/SalesEngineTL/Quotes/Files/Q<Qid>.pdf`
- Render URL: `View.asp?Qid=<n>&Email=true`
- `Mode=1` is Email, `Mode=2` is Print, `Mode=3` is Fax (observed — confirm against the .NET handler).

---

## 11. Email flow (`Email.asp`, `Email_Proc.asp`)

### 11.1 `Email.asp` — compose

| Field | Default |
|---|---|
| Attention | `Quotes.Attention` (red label, required client-side) |
| Notes | `Quotes.CustomerNotes` (shown in email body) |
| Contact picker | Rendered from `Contacts_WithCustomersAndSuppliers_V2 WHERE Code = <userCode>` — `onchange` populates `ToEmail`. |
| Email Address | `Contacts.Email` (via `rsCon`). "Set to my email" button replaces it with `Cookies("UserSettings")("Email")`. |

Submits to `GenerateQuote.asp?WorkingDir=…` with `Mode=1` → triggers PDF + email.

### 11.2 `Email_Proc.asp`

Legacy direct-CDO email sender (pre-.NET). Features a 5-second `Sleep` hack (lines 12–18) — presumably to let the PDF finish writing before attaching. Superseded by the `/MyDeskASPNet/` pipeline but retained for fallback.

---

## 12. Status change (`UpdateStatus.asp` + `_Proc`)

### Form

```
Current Quote Status:  <label>
New Quote Status:      <select from QuoteStatus filtered by §3 gate>
                       [Submit]
```

### Handler

```
UPDATE Quotes SET QuoteStatusId = <new>
INSERT INTO QuoteAudit ('Status changed to <name>')

If new status = 9:
  SendMail(next approver, "Waiting for your approval")

Rendered response (JS in the response HTML):
  If new status = 4 (Accepted):
    confirm("Do you want to invoice this quote?")
       → RedirectPage_Global('Transporter_QuoteToInvoice.asp?Qid=<n>')
       → RefreshWindowClose()
  Else:
    alert("Quote Status updated successfully.")
    document.location.href = 'View.asp?Qid=<n>'
```

---

## 13. Approve / Decline

### `Approve.asp?Qid=<n>`

```
UPDATE Quotes SET QuoteStatusId = 9
INSERT INTO QuoteApproval (Qid, Code = current user)
Lookup UserRoles.QuoteApprovalLimit for current user
If IsDirector(userCode) OR GetQuoteLastLineApprover = userName OR QuoteApprovalLimit >= NettPriceTotal:
    UPDATE Quotes SET QuoteStatusId = 10
    -- Email to owner: approval chain complete (currently COMMENTED OUT — see §5)
Else:
    -- Email to next approver: waiting for your approval (currently COMMENTED OUT)
INSERT INTO QuoteAudit ('Approved')
MyRedirect View.asp?Qid=<n>&Msg=Quote+approved
```

### `Decline.asp?Qid=<n>`

```
INSERT INTO QuoteApproval (Qid, Code)
UPDATE Quotes SET QuoteStatusId = 11
INSERT INTO QuoteAudit ('Declined')
SendMail(owner, "Quote declined by <name>")
MyRedirect View.asp?Qid=<n>&Msg=Quote+declined
```

---

## 14. Copy

`Copy_Proc.asp?Id=<Qid>` — invoked from the grid via `copyRecord(id)` (`window.open('Copy_Proc.asp?Id=…')`).

```
1. SELECT * FROM Quotes WHERE Qid = <src>
2. INSERT INTO Quotes (...) VALUES (
     RealQid=0, Revision=0, Code=<src.Code>, ContactId, DivisionId,
     QuoteDate=Now, QuoteStatusId=<src>, Attention, Reference, Terms, Delivery,
     Validity, InternalNotes, CustomerNotes, PPriceTotal, UnitCostTotal,
     NettPriceTotal, Margin, QuoteCOSId, QuoteNumber, IncludeInReporting, POid)
3. newQid = SELECT TOP 1 Qid FROM Quotes ORDER BY Qid DESC
4. Loop QuoteContents of <src> → INSERT into QuoteContents for <newQid>
   (Third-party items are NOT copied — copy block is commented out in source)
5. alert('Quote Copied'); RefreshIFrame_Global_Opener(); window.close();
```

The copy carries the **original status** (not reset to Draft) — operational point worth flagging.

---

## 15. Delete

`Del_Proc.asp?Id=<Qid>` — invoked from the grid via `deleteRecord(id)`.

### Gate

```
boolCanDelete = (UserTypeId ∈ {5, 6}) OR (QuoteStatusId = 1)
```

### Cascade

```
DELETE FROM Comments WHERE ItemId=<n> AND TableId=6
DELETE FROM QuoteThirdPartyContents WHERE QuoteId=<n>
DELETE FROM QuoteContents WHERE Qid=<n>
DELETE FROM QuoteAudit WHERE Qid=<n>
DELETE FROM Quotes WHERE Qid=<n>
```

If `boolCanDelete = false` → `alert('Cannot delete this Quote, as it has been issued.')`.
If FK conflict (`GetErrorCode = 1`) → `alert('Record cannot be deleten, as there are historical records that depend on it.')` (typo preserved in source).

---

## 16. History (`ViewHistory.asp`)

Renders:
1. `QuoteAudit` — `Name | Action | DateEntered` (sorted desc).
2. `Comments` (joined with `Users`) for `TableId = 6` — `Name | Comment | DateEntered`.

Used for after-the-fact audit review. Accessible from `NavBar.asp` as "View History".

---

## 17. Report (`Report.asp`)

Print-oriented aggregate view. Uses the same filters as `Default.asp` but grouped/totalled. Renders:
- Filter echo (user, division, date range, customer, status)
- Per-quote expandable rows with inline `Comments` (colour-coded by follow-up status)
- Totals row: running `UnitCostTotal`, `NettPriceTotal`, and margin %, with `boolDivisionManager` gating cost + margin columns
- `Print` button (shown if `strCode = currentUserCode` OR Manager)

---

## 18. Module-specific cookies / session flags

| Cookie/Session | Effect in this module |
|---|---|
| `ClientSettings("HasQuoteCOS") = "true"` | Shows "Conditions of Sale" dropdown on Add2/Edit |
| `ClientSettings("HasInternalNotes") = "true"` | Shows the Internal Notes textarea |
| `ClientSettings("PortalCompany")` | Prefix in alert email subject ("MyDesk <Company> Alert : Quote #…") |
| `DivisionIdsAccess("Quotes")` | CSV of division IDs the user can see quotes for |
| `DivisionIdsAccess("ArrDivisionIdsManager")` | Used by `boolDivisionManager = SearchArray(...)` — shows Margin column in Add2 if user manages this division |
| `UserSettings("Manager")` | Unlocks user-filter dropdown on `Default.asp` |
| `UserSettings("UserTypeId")` | 5 or 6 → always-allowed delete; 6 also unlocks full status dropdown on `UpdateStatus.asp` |
| `UserSettings("Code")` | Owner filter when not Manager; audit trail author |
| `UserSettings("Email")` | From-address for alert emails |

---

## 19. Known baseline issues (documentation only — not recommendations)

- **Add.asp division-selection is short-circuited** to `DivisionId=1` (`Response.Redirect("Add2.asp?DivisionId=1")` on line 15). The select UI further down is dead code.
- **`Approve.asp` does not send emails** — both `SendMail` calls are commented out in source. Audit entry still written.
- **`Email_Proc.asp` has a 5-second `Sleep` hack** to work around PDF-generation race. The modern path uses `/MyDeskASPNet/GenerateQuote.aspx` instead.
- **SQL is built by string concatenation** with only `Replace("'","''")` escaping; no parameterised queries.
- **`Copy_Proc.asp` preserves source status** — copied quote does not revert to Draft.
- **Grid SQL uses `DISTINCT`** over a 5-way join to the contacts view; performance on large tables is acceptable because the Access DB is typically small, but flag for future migration.

This is recorded as the current-state baseline only.
