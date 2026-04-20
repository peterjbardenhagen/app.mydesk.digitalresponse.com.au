# 11 — Invoices

Status: **IN REVIEW** — verified against source in `Clients/SalesEngineTL/Invoices/`.

Customer invoices — typically originated from an accepted quote (`Quotes → Transporter_QuoteToInvoice.asp → Invoices/Add.asp`), but can also be raised standalone. Includes Delivery Note and Despatch Note variants, MYOB export, and PDF generation via ASP.NET.

---

## 1. Files

| File | Role |
|---|---|
| `Default.asp` | Filter + list (hardened against missing cookies — extensive `On Error Resume Next` blocks). |
| `IFrame.asp` | Grid (HTML table, legacy style). |
| `Add.asp` | New invoice form (27 KB). When called with `?Qid=<n>` pre-populates from the quote. |
| `Add_Proc.asp` | Insert: `Invoices` header + `InvoiceContents` lines + `InvoiceAudit` row. |
| `Edit.asp` / `Edit_Proc.asp` | Edit invoice header (line items aren't re-saved by Edit_Proc). |
| `View.asp` | Printable invoice (25 KB). On `?Print=True` promotes status 1 → 2. |
| `ViewDeliveryNote.asp` | Printable delivery note (same layout, no pricing). |
| `ViewDespatchNote.asp` | Printable despatch note with carrier/package details. |
| `NavBar.asp`, `NavBarDeliveryNote.asp`, `NavBarDespatchNote.asp` | Top action-bar variants injected inside each view page. |
| `Email.asp` | Compose invoice email (recipient/attention/notes). Posts to `GenerateInvoice.asp`. |
| `Email_Proc.asp` | Legacy direct-CDO email sender (pre-.NET). |
| `EmailDeliveryNote.asp` / `EmailDeliveryNote_Proc.asp` | Same pair for delivery notes (uses SendGrid SMTP). |
| `GenerateInvoice.asp` | Shim → `/MyDeskASPNet/GenerateInvoice.aspx`. Forces status 1 → 2 before redirecting. |
| `GenerateInvoice.aspx` + `.aspx.vb` + `.aspx.resx` | **In-place** VB.NET handler (copied from `MyDeskASPNet` for compatibility). |
| `GenerateDeliveryNote.asp` | Shim → `/MyDeskASPNet/GenerateDeliveryNote.aspx`. |
| `GenerateDeliveryNote.aspx` + `.aspx.vb` | In-place VB.NET delivery-note handler. |
| `EnterDespatchDetails.asp` / `_Proc.asp` | Form to capture carrier/ref/packages → INSERT into `Despatch`. |
| `ExportToMYOB.asp` | Date-range picker. |
| `ExportToMYOB_Proc.asp` | Streams CSV (`Content-Type: text/csv`, downloadable filename `Techlight_Invoices_<date>.csv`), marks exported rows, logs to `InvoiceExportLog`. |
| `Del_Proc.asp` | Delete invoice + cascade (Comments TableId=10, InvoiceContents, InvoiceAudit, Invoices). |
| `Transporter.asp` | One-liner → `JobOrders/Add.asp?InvoiceId=<n>`. |
| `Transporter_EditInvoice.asp` | One-liner: navigate parent to `Edit.asp?InvoiceId=<n>&DivisionId=<n>`. |
| `ViewHistory.asp` | Audit trail + comments (TableId = 10). |
| `Report.asp` | Printable aggregate report. |
| `Files/` | Output directory: `I<InvoiceId>.pdf`, `DN<InvoiceId>.pdf`. |

---

## 2. URL map

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/Invoices/` | Filter + list |
| `…/Invoices/Add.asp[?Qid=<n>]` | New invoice (optionally pre-filled from Quote) |
| `…/Invoices/Edit.asp?InvoiceId=<n>` | Edit header |
| `…/Invoices/View.asp?InvoiceId=<n>[&Print=True]` | Read-only / print |
| `…/Invoices/ViewDeliveryNote.asp?InvoiceId=<n>` | Delivery note view |
| `…/Invoices/ViewDespatchNote.asp?InvoiceId=<n>` | Despatch note view |
| `…/Invoices/Email.asp?InvoiceId=<n>` | Email compose |
| `…/Invoices/EmailDeliveryNote.asp?InvoiceId=<n>` | DN email compose |
| `…/Invoices/GenerateInvoice.asp` (POST) | PDF generation + email (via .NET) |
| `…/Invoices/GenerateDeliveryNote.asp` (POST) | DN PDF |
| `…/Invoices/EnterDespatchDetails.asp?InvoiceId=<n>` | Despatch form |
| `…/Invoices/ExportToMYOB.asp` | Date range picker |
| `…/Invoices/ExportToMYOB_Proc.asp` (POST) | Downloads CSV |
| `…/Invoices/Del_Proc.asp?Id=<n>` | Delete |
| `…/Invoices/Report.asp` (POST) | Printable report |
| `…/Invoices/ViewHistory.asp?InvoiceId=<n>` | Audit trail |

---

## 3. Access control

Most pages gate on `DivisionIdsAccess("Quotes")` (sic — intentionally uses the Quotes CSV, not a separate Invoices one). Exceptions:

- `Default.asp` reads `DivisionIdsAccess("Invoices")` but the redirect on failure is **commented out** — list access is effectively open to any logged-in user (still gated by login via `ssi_Security.inc`).
- `Edit_Proc.asp`, `ExportToMYOB.asp`, `ExportToMYOB_Proc.asp` properly gate on `DivisionIdsAccess("Invoices")`.
- `Del_Proc.asp` gates on `DivisionIdsAccess("QUOTES")` (uppercase — still works since cookie keys are case-insensitive in ASP).

### Delete gate

```
boolCanDelete = (UserTypeId ∈ {5, 6}) OR (InvoiceStatusId = 1)
```

Issued (status 2) or finalised invoices can only be deleted by admins.

### Status dropdown — no per-user filtering (unlike Quotes). Any user with edit access can promote arbitrarily.

---

## 4. Data model

### 4.1 `Invoices` (header)

| Column | Notes |
|---|---|
| `InvoiceId` | AutoNumber PK |
| `InvoiceNum` | Free-text customer-facing invoice number (used in MYOB export). |
| `InvoiceDate` / `Date` | Set to `ServerToEST(Now())` on create. Both columns exist; MYOB export reads `Date`, rest of UI reads `InvoiceDate`. |
| `Code` | FK Users — "Invoiced By". |
| `InvoiceStatusId` | 1=Draft, 2=Issued (set on Print/Email). |
| `DivisionId` | |
| `Qid` | FK Quotes (0 if standalone). |
| `CompanyId` | FK Companies. 142 = "Not an account" sentinel. |
| `ContactId` | FK Contacts. |
| `CCompany` | Denormalised company name (used when `CompanyId = 142`). |
| `InvCompany`, `DelCompany` | Invoice-to / Deliver-to company names (can differ). |
| `InvAddress`, `DelAddress` | **Single-text-area** addresses (as-of current Add.asp/Edit.asp). |
| `InvAddress1/2`, `InvSuburb`, `InvState`, `InvStateId`, `InvPostCode`, `InvCountry` | Structured invoice-to address columns (older schema — still populated by Edit_Proc, written empty by Add_Proc). |
| `DelAddress1/2`, `DelSuburb`, `DelState`, `DelStateId`, `DelPostCode`, `DelCountry` | Structured delivery address. |
| `CustomerPO` | Purchase order reference from the customer. |
| `Attention` | "Attention: <name>" on PDF/email. |
| `Account` | Customer account reference. |
| `Terms`, `CustomerNotes`, `InternalNotes` | Free-text. |
| `NettPriceTotal`, `GSTTotal` | Totals (client-entered on Add, not recalculated server-side). |
| `ExportedToMYOB`, `ExportedDate` | Updated by `ExportToMYOB_Proc.asp` after CSV generation. |
| `PriceExGST` | Read by MYOB export (likely = `NettPriceTotal`). |

### 4.2 `InvoiceContents` (line items)

| Column | Notes |
|---|---|
| `InvoiceContentId` | PK |
| `InvoiceId` | FK |
| `Quantity`, `Units`, `Days`, `BackOrder`, `Ordered` | `Ordered = Quantity` on insert; `BackOrder = 0`; `Units = Quantity` (bug-for-bug compat with the Quote line shape). |
| `ProductCode`, `Description` | Copied from quote (with `"Type: <x> "` prepended if `Type` was set). |
| `NettPrice`, `ExtNettPrice` | Unit + extended totals. |

**Side-effect on insert**: `Add_Proc.asp` **updates the source quote's line items** by adding the invoiced quantity to `QuoteContents.Days`:

```sql
UPDATE QuoteContents SET Days = Days + <quantity> WHERE QuoteItemId = <n>
```

This is a quirk — `Days` is being repurposed as a "quantity already invoiced" counter. Flag as baseline behaviour.

### 4.3 `Despatch`

| Column | Notes |
|---|---|
| `DespatchId` | PK |
| `InvoiceId` | FK |
| `Code` | User who entered (⚠ `strCode` is never set in `_Proc` — inserted as empty string) |
| `DespatchDate` | |
| `Carrier`, `CarrierRef` | Up to 50 chars |
| `PackageDetails` | Memo (500 chars, live-count) |
| `InternalNotes` | Memo (500 chars) |

### 4.4 `InvoiceAudit`

```
InvoiceId | Code | Action | DateEntered
```

Actions emitted: `Invoice created`, `Invoice updated`, `Despatch details entered`, `Delivery Note issued by email to <addr>`, `Invoice issued by email to <addr>` (from `/MyDeskASPNet/`), `Status changed to <name>` (if UpdateStatus used), and print/email events written by `View.asp`/`ViewDeliveryNote.asp`.

### 4.5 `InvoiceStatus`

| ID | Status |
|---|---|
| 1 | Draft |
| 2 | Issued (aka Unpaid — used by Dashboard "overdue" calc) |
| Other IDs | Paid / Cancelled / etc. (see the `InvoiceStatus` table rows) |

### 4.6 `InvoiceExportLog`

```
ExportedBy (Code) | DateFrom | DateTo | InvoiceCount | Status
```

Written by `ExportToMYOB_Proc.asp`.

---

## 5. List page — `Default.asp` + `IFrame.asp`

### Filter form

| Field | Default | Bound to |
|---|---|---|
| Date From | today − 3 months | `DateFrom` |
| Date To | today + 1 day | `DateTo` |
| User | user's Code (Managers = "All") | `Code` |
| Customer | "All companies" + "Not an account" (142) + companies from user's access | `CompanyId` |
| Entity | user's cookie `DivisionId` | `DivisionId` (options from `Divisions WHERE Quotes=True`) |
| Status | "All (Active)" = 555 | `InvoicestatusId`. 555 → `NOT IN (2)`; 0 → all; else exact. |

**Two buttons**: `Filter` (→ IFrame.asp) and `Generate Report` (→ Report.asp, needs a division). Same pattern as Quotes.

### Action buttons in grid

The grid doesn't expose Edit from its row; instead:

- **View** → `View.asp?InvoiceId=<n>`
- **Delivery Note** → `ViewDeliveryNote.asp?InvoiceId=<n>`
- **Delete** → calls `deleteRecord(id)` which calls `Del_Proc.asp?Id=<n>`

Columns: Invoice # | Company | Action | Invoice Status | Invoice Date | Nett Price Total | Invoiced By.

---

## 6. Add flow (`Add.asp` + `Add_Proc.asp`)

### 6.1 Initialization from quote

If called with `?Qid=<n>`, Add.asp loads the source quote (joined to `Contacts_WithCustomersAndSuppliers_V2`) and pre-fills:
- `DivisionId`, `Code` (owner), `CompanyId`, `NettPriceTotal`.
- Delivery and invoice addresses from `Contacts.OAddress1/OAddress2/OSuburb/OPostCode/OStateId/OState/OCountry`.
- Contact's `CompanyName` (or `CCompany` if CompanyId=142).

The form also includes a hidden `<input name="Qid" value="<n>">` so Add_Proc can write the cross-reference back.

Line items are pulled from `QuoteContents` so the user sees a grid of invoiceable lines (with pre-filled `Quantity`, `Description`, `NettPrice`) and can edit/adjust before saving.

### 6.2 Form fields (header)

| Field | Required | Notes |
|---|---|---|
| Invoiced By | **Yes** | `select` of users visible via `GetAccessCodesList` |
| Company (CCompany) | **Yes** | Text input; `CompanyId` is hidden and defaults to 142 unless loaded from quote |
| Entity (DivisionId) | **Yes** | Hidden input from quote |
| CustomerPO | No | |
| Delivery Address (text area) | **Yes** | Single-text-area `DelAddress` |
| Invoice Address (text area) | **Yes** | Single-text-area `InvAddress` |
| DelCompany / InvCompany | No | |
| Attention | No | |
| Account | No | |
| Terms | No | |
| Customer Notes | No | |
| Internal Notes | No | |
| NettPriceTotal | **Yes** | Numeric |
| GSTTotal | **Yes** | Numeric |
| NettPriceTotalInc | (calculated) | |

Per-line fields: `Quantity<i>`, `OriginalQuantity<i>`, `Days<i>`, `Units<i>`, `Type<i>`, `ProductCode<i>`, `Description<i>`, `NettPrice<i>`, `SubTotal<i>`, `QuoteItemId<i>`, plus hidden `X` = row count.

### 6.3 Insert (`Add_Proc.asp`)

```
1. INSERT INTO Invoices (Code, InvoiceStatusId=1, InvoiceDate=Now,
       DivisionId, Qid, CompanyId, CCompany, CustomerPO,
       DelCompany, DelAddress (+ empty structured cols),
       InvCompany, InvAddress (+ empty structured cols),
       Attention, Account, Terms, CustomerNotes, InternalNotes,
       NettPriceTotal, GSTTotal)
2. lngInvoiceId = @@IDENTITY
3. Loop i=0..X:
     If Quantity<i> > 0:
       If Type<i> not empty: prepend "Type: <x> " to Description<i>
       UPDATE QuoteContents SET Days = Days + <qty> WHERE QuoteItemId = <n>
       INSERT INTO InvoiceContents (InvoiceId, Quantity, BackOrder=0, Ordered=Quantity,
           Units=Quantity, Days, ProductCode, Description, NettPrice, ExtNettPrice)
4. INSERT INTO InvoiceAudit ('Invoice created')
5. MyRedirect Default.asp?DivisionId=<n>&Msg=Invoice+added
```

**Note**: `strCode` in the Despatch insert (and the Invoice address block) is interpolated without being `Replace`'d — minor SQL injection exposure for same-session values. Baseline behaviour; not a recommendation.

---

## 7. Edit flow

`Edit.asp` lets the user change header fields only. `Edit_Proc.asp` writes:

```
UPDATE Invoices SET DelAddress, InvAddress, Code, InvoiceStatusId=1, DivisionId,
       Qid, CompanyId, CCompany, CustomerPO, <all structured Del/Inv address cols>,
       Attention, Account, Terms, CustomerNotes, InternalNotes
  WHERE InvoiceId = <n>
INSERT INTO InvoiceAudit ('Invoice updated')
MyRedirect Default.asp?DivisionId=<n>&Msg=Invoice+updated
```

**Gotchas**:
- `Edit_Proc` forces `InvoiceStatusId = 1` (Draft) on every save — editing an Issued invoice demotes it back to Draft. Flag as baseline.
- Line items are **not** re-written by Edit_Proc — to change amounts, users go via Delete + Add, or edit directly in the DB.
- `InvCompany = strCCompany` (bug-for-bug: even though the form has a separate `InvCompany` field, it's overwritten by `strCCompany`).

---

## 8. View / Print / PDF

### `View.asp?InvoiceId=<n>`

- Renders a printable invoice (techlight logo, division address, invoice number, customer block, line items, GST summary, terms/notes).
- Embeds `NavBar.asp` (Close, View Invoice, Edit Invoice, View History, Email, Print).
- On `?Print=True` → confirms "Invoice status will be set to issued" then reloads with the flag, and `View.asp` promotes status 1 → 2.
- Supports `CurrencySelector.asp` include (user can switch display currency for the rendered invoice).

### `ViewDeliveryNote.asp`

- Same layout without pricing columns (quantity + description only).
- Embeds `NavBarDeliveryNote.asp`.
- Used as the render target by the .NET PDF generator.

### `ViewDespatchNote.asp`

- Includes `NavBarDespatchNote.asp`.
- Joined with `Despatch` for carrier/ref/packages.

### PDF generation

```
GenerateInvoice.asp
  ↓ If InvoiceStatusId = 1 → UPDATE to 2
  ↓ Redirect to /MyDeskASPNet/GenerateInvoice.aspx
      ?Mode=1&InvoiceId=<n>&Attention=<>&ToEmail=<>&FromFax=<>&ToFax=<>
       &Notes=<>&WorkingDir=/Clients/SalesEngineTL

GenerateInvoice.aspx(.vb)
  ↓ ABCpdf: AddImageUrl("<host>/Clients/SalesEngineTL/Invoices/View.asp?InvoiceId=<n>&Email=true")
  ↓ Save to Server.MapPath("/Clients/SalesEngineTL/Invoices/Files/I<n>.pdf")
  ↓ Redirect back to Invoices/Email_Proc.asp (legacy) OR send email directly via .NET SmtpClient

GenerateDeliveryNote.asp / GenerateDeliveryNote.aspx
  ↓ Same shape, output path: /Clients/SalesEngineTL/Invoices/Files/DN<n>.pdf
```

There are **two parallel copies** of the generator handlers:
- `/MyDeskASPNet/GenerateInvoice.aspx(.cs)` — C# master version.
- `/Clients/SalesEngineTL/Invoices/GenerateInvoice.aspx(.vb)` — VB.NET copy (also present for `GenerateDeliveryNote`). These appear to be used as a fallback when the top-level `/MyDeskASPNet/` app pool is unreachable. Check `50-ASPNet-Interop.md` for details.

---

## 9. Email flow

### `Email.asp` (invoice) / `EmailDeliveryNote.asp`

Form fields:
- **Attention** (required): pre-filled from `Invoices.Attention`.
- **Notes**: pre-filled from `CustomerNotes` — shown in email body.
- **Select contact to get email address**: dropdown of this user's contacts; `onchange` populates `ToEmail`.
- **Email Address** (required): pre-filled from the source quote's contact email (if `Qid > 0`) — otherwise blank.
- "Set to my email" button: shortcut to self-send.

Submits to `GenerateInvoice.asp` (or `GenerateDeliveryNote.asp`) with hidden `Mode=1`.

### `Email_Proc.asp` (legacy invoice sender)

Not typically used — modern path goes via `/MyDeskASPNet/`. Included for fallback.

### `EmailDeliveryNote_Proc.asp`

Still used. SMTP config:
- Server: `smtp.sendgrid.net:587` (TLS)
- User: `apikey`
- Password: hard-coded SendGrid API key in source (flag as baseline security concern)
- Always BCCs `bertb@techlight.com.au; admin@techlight.com.au`
- Subject: `<Division> (Delivery Note # <InvoiceId>)`
- Attaches `Server.MapPath(WorkingDir & "/DeliveryNotes") & "\Files\DeliveryNote.pdf"` — note the path `/DeliveryNotes/Files/` (not `/Invoices/Files/`), indicating the .NET handler writes the DN PDF to a sibling folder. Cross-check in `50-ASPNet-Interop.md`.
- Writes `InvoiceAudit ('Delivery Note issued by email to <addr>')`.

---

## 10. Despatch flow

User flow:
1. From `ViewDeliveryNote.asp` → click "Enter Despatch Details" in NavBar.
2. `EnterDespatchDetails.asp?InvoiceId=<n>` — form captures DespatchDate (calendar picker), Carrier, CarrierRef, PackageDetails (500 char text area with live counter), InternalNotes (500 chars).
3. `EnterDespatchDetails_Proc.asp`:
   - INSERT INTO Despatch (Code='', DespatchDate, InvoiceId, Carrier, CarrierRef, PackageDetails, InternalNotes)
   - INSERT INTO InvoiceAudit ('Despatch details entered')
   - Redirect → `ViewDeliveryNote.asp?InvoiceId=<n>`.
4. From there user can jump to `ViewDespatchNote.asp` (which includes the carrier details).

All four fields are required (client-side); no server-side check.

---

## 11. MYOB export

### `ExportToMYOB.asp`

Date-range picker (DateFrom/DateTo — default last 30 days). Posts to `_Proc.asp`.

### `ExportToMYOB_Proc.asp`

```asp
Response.ContentType = "text/csv"
Response.AddHeader "Content-Disposition", "attachment; filename=Techlight_Invoices_<date>.csv"
```

CSV header: `Co./Last Name, First Name, Invoice No, Date, Description, Amount, Status`.

Query:

```sql
SELECT Invoices.*, Companies.Company AS CompanyName,
       Contacts.FirstName, Contacts.Surname AS LastName
FROM (Invoices INNER JOIN Companies ON Invoices.CompanyId = Companies.CompanyId)
  LEFT JOIN Contacts ON Invoices.ContactId = Contacts.ContactId
WHERE Invoices.Date >= #<from>#
  AND Invoices.Date <= #<to>#
  AND Invoices.InvoiceStatusId = 2
ORDER BY Invoices.Date
```

Each row → 7 CSV fields (double-quote escaped). Description is hard-coded as `"Invoice from Techlight"`. Amount = `PriceExGST`.

Post-export side-effects (only if `invoiceCount > 0`):

```sql
UPDATE Invoices SET ExportedToMYOB = -1, ExportedDate = Now()
  WHERE <same date range> AND InvoiceStatusId = 2

INSERT INTO InvoiceExportLog (ExportedBy, DateFrom, DateTo, InvoiceCount, Status='Exported')
```

The UPDATE re-marks every Issued invoice in the range on every export — there's no "skip already exported" filter. Flag as baseline.

---

## 12. Delete

`Del_Proc.asp?Id=<InvoiceId>`:

### Gate

```
boolCanDelete = (UserTypeId ∈ {5,6}) OR (InvoiceStatusId = 1)
```

### Cascade

```sql
DELETE FROM Comments WHERE ItemId=<n> AND TableId=10
DELETE FROM InvoiceContents WHERE InvoiceId=<n>
DELETE FROM InvoiceAudit WHERE InvoiceId=<n>
DELETE FROM Invoices WHERE InvoiceId=<n>
```

Despatch rows are **not** cascaded — can leave orphans. Flag as baseline.

---

## 13. History (`ViewHistory.asp`)

Renders:
1. `InvoiceAudit` rows (inner joined `Users`) sorted desc.
2. `Comments` for `TableId = 10` with author name + follow-up status.

Available via "View History" button in `NavBar.asp`.

---

## 14. Report (`Report.asp`)

Printable aggregate view. Mirrors `Default.asp` filters; emits:
- Filter echo header
- Table of invoices with inline comments (per-row expansion)
- Running totals at the bottom
- Margin/cost columns gated by `boolDivisionManager = SearchArray(Cookies("DivisionIdsAccess")("ArrDivisionIdsManager"), DivisionId)`.

---

## 15. Integration points

| Outbound | Target | When |
|---|---|---|
| `QuoteContents.Days += qty` | `Quotes` table | On every Add_Proc (tracks invoiced qty per quote line) |
| JobOrders | `Transporter.asp?InvoiceId=<n>` → `JobOrders/Add.asp` | Manual from View.asp |
| `/MyDeskASPNet/GenerateInvoice.aspx` | .NET PDF generator | On Email / Print-via-.NET |
| SendGrid SMTP | `smtp.sendgrid.net:587` | `EmailDeliveryNote_Proc.asp` direct path |
| CSV download | User's browser | `ExportToMYOB_Proc.asp` |

| Inbound | Source |
|---|---|
| Quote → Invoice | `Quotes/Transporter_QuoteToInvoice.asp?Qid=<n>&DivisionId=<n>` |
| UpdateStatus on Quote = 4 (Accepted) | Modal prompt → `Transporter_QuoteToInvoice.asp` |

---

## 16. Known baseline issues (documentation only)

- **`Edit_Proc.asp` forces status back to 1** on every save. No explicit "save without demote" path.
- **`Add_Proc.asp` debug writes** to the page before the `Response.Redirect`: `Response.Write "intDelStateId…"`. Breaks the redirect header semantics (Response is flushed before redirect header). Observe in test.
- **`Del_Proc` doesn't cascade `Despatch`**.
- **MYOB export re-marks already-exported rows**.
- **SendGrid API key hard-coded** in `EmailDeliveryNote_Proc.asp` and the .NET generators.
- **`Default.asp` access redirect commented out** — the module trusts `DivisionIdsAccess("Quotes")` rather than a separate Invoices flag.
- **Add.asp's address flow** writes only the single-text-area `DelAddress` / `InvAddress` on insert but Edit_Proc writes the structured columns — inconsistent schema shape.
- **`QuoteContents.Days` dual-use** — it's the "rental days" column on quotes but the invoice flow overloads it as "qty invoiced". Reports reading `Days` get wrong results after invoicing.

Baseline only — not recommendations.
