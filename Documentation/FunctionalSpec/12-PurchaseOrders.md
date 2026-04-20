# 12 — Purchase Orders

Status: **IN REVIEW** — verified against source in `Clients/SalesEngineTL/PurchaseOrders/`.

Procurement workflow for supplier purchases, including multi-level approval chains, capital expenditure tracking, and integration with Quotes (for job-related purchases) and RFQ (Request for Quote) data. The module distinguishes between standard Purchase Orders and Purchase Requests (for divisions with `PurchaseRequests = True`).

---

## 1. Files

| File | Role |
|---|---|
| `Default.asp` | Filter + list page (modern UI, hardened with `On Error Resume Next` blocks). |
| `IFrame.asp` | Data grid showing PO list with action buttons (View, Update Status, Edit, Delete). |
| `Add.asp` | Division selector for new PO (intermediate step before Add2). |
| `Add2.asp` | Main PO creation form (24 KB). Handles pre-population from RFQ or Quote. |
| `Add_Proc.asp` | Insert handler: creates `PurchaseOrders` header + `PurchaseOrderContents` lines + audit trail. Handles auto-approval if within user's limit. |
| `Edit.asp` | Edit PO form (23 KB). Mirrors Add2 but loads existing data. Status dropdown limited to statuses < 3 (Draft/Pending) for editing. |
| `Edit_Proc.asp` | Update handler: deletes existing lines and re-inserts (full replacement). Re-evaluates approval chain on save. |
| `View.asp` | Printable purchase order view. On `?Print=True` or `?Email=True`, promotes status 1/3 → 4 (Issued) and writes audit. Redirects to `ViewRequest.asp` if `Divisions.PurchaseRequests = True`. |
| `ViewRequest.asp` | Alternative view for Purchase Requests (displays "This is not a purchase order" watermark, uses `NavBar_Requests.asp`). |
| `NavBar.asp` | Standard action bar for View.asp (Close, View PO, Edit, History, Approve/Decline buttons, Enter Invoice Details, Email, Print). |
| `NavBar_Requests.asp` | Simplified action bar for ViewRequest.asp. |
| `Approve.asp` | Approval handler: inserts into `PurchaseOrderApproval`, checks approval limits, promotes status 2 → 3 (Approved) if limit sufficient or user is Director. Sends email notifications to next approver. |
| `Decline.asp` | Decline handler: inserts approval record, sets status 5 (Declined), emails originator. |
| `UpdateStatus.asp` | Status change form with notes field. Only Director (UserTypeId = 6) can access full status list; regular users limited to Cancel (6) and Received (7) for issued POs. |
| `UpdateStatus_Proc.asp` | Applies status change, writes audit, triggers approval workflow email if status = 2 (Pending Approval). |
| `Email.asp` | Email composition form (Attention, Notes, ToEmail selector). Posts to `GeneratePO.asp`. |
| `Email_Proc.asp` | Legacy CDO email sender (SendGrid SMTP with hardcoded API key). Attaches PDF from `/PurchaseOrders/Files/PurchaseOrder.pdf`. |
| `GeneratePO.asp` | Shim: updates status to 4 (Issued) if < 4, redirects to `/MyDeskASPNet/GeneratePurchaseOrder.aspx`. |
| `GeneratePO.aspx` + `.aspx.vb` | In-place VB.NET PDF generator (ABCpdf) - fallback when `/MyDeskASPNet/` unavailable. |
| `GenerateFromRFQ.asp` | Transporter: takes `?RFQid=<n>`, builds query string from RFQ data, redirects to `Add2.asp` with pre-filled values. |
| `EnterInvoiceDetails.asp` / `_Proc.asp` | Form to record supplier invoice numbers/amounts against a PO. Supports up to 5 invoices per PO. Deletes and re-inserts `PurchaseOrderInvoices` rows. |
| `Del_Proc.asp` | Delete handler with permission gates: Admins (UserTypeId 5/6) can delete any PO; regular users can only delete Draft (1), Pending (2), or Approved (3) POs. Cascades: Comments (TableId=8), Audit, Approvals, Contents, then header. |
| `ViewHistory.asp` | Audit trail + comments viewer. Shows RFQ origin if `RFQId > 0`. Uses `NavBar_Requests.asp` for request-type POs. |
| `Report.asp` | Printable aggregate report with running totals, inline comments expansion, filter echo. Landscape orientation. |
| `Files/` | PDF output directory: `PurchaseOrder.pdf` (single file, overwritten per generation). |

---

## 2. URL Map

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/PurchaseOrders/` | Filter + list |
| `…/PurchaseOrders/Add.asp` | Select division for new PO |
| `…/PurchaseOrders/Add2.asp[?RFQid=<n>][?Qid=<n>]` | New PO form (pre-filled from RFQ or Quote) |
| `…/PurchaseOrders/Edit.asp?POid=<n>` | Edit existing PO |
| `…/PurchaseOrders/View.asp?POid=<n>[&Print=True][&Email=True]` | View/Print/Issue PO |
| `…/PurchaseOrders/ViewRequest.asp?POid=<n>` | View as Purchase Request |
| `…/PurchaseOrders/Approve.asp?POid=<n>&HasCapEx=<bool>` | Approve action |
| `…/PurchaseOrders/Decline.asp?POid=<n>&HasCapEx=<bool>` | Decline action |
| `…/PurchaseOrders/UpdateStatus.asp?POid=<n>` | Manual status change |
| `…/PurchaseOrders/Email.asp?POid=<n>` | Email compose |
| `…/PurchaseOrders/GeneratePO.asp` (POST) | PDF generation + email trigger |
| `…/PurchaseOrders/EnterInvoiceDetails.asp?POid=<n>` | Record supplier invoices |
| `…/PurchaseOrders/ViewHistory.asp?POid=<n>[&Requests=True]` | History/audit |
| `…/PurchaseOrders/Report.asp` (POST) | Printable report |
| `…/PurchaseOrders/Del_Proc.asp?Id=<n>` | Delete |

---

## 3. Access Control

### Primary Gate

All pages gate on:
```
Request.Cookies("DivisionIdsAccess")("PurchaseOrders") <> "0"
```

### Create/Edit Permissions
- Any user with PurchaseOrders division access can create POs
- Edit restrictions (commented out in current source but logic preserved):
  - Cannot edit if POStatusId = 4 (Issued) - commented block at Edit.asp:36-44
  - Cannot edit if POStatusId = 6 (Cancelled) or 7 (Received)
  - Cannot edit if POStatusId = 2/3 and user is not the next approver (approval chain logic)

### Delete Permissions (Del_Proc.asp)
```
boolCanDelete = (UserTypeId ∈ {5,6}) OR (POStatusId ∈ {1,2,3})
```

Issued (4), Declined (5), Received (7) POs cannot be deleted by regular users.

### Approval Permissions
- **Approve/Decline buttons** visible in NavBar if:
  - POStatusId ∈ {1,2} (Draft/Pending)
  - AND user is next line approver (`GetPOLineApprover_Check`) OR user is originator's line manager (`CheckForLine`)
  
- **Director Override**: `IsDirector(Code)` returns True for UserTypeId ∈ {5,6} - Directors can approve any amount.

### Status Update Permissions (UpdateStatus.asp)
```
If UserTypeId = 6 (Director) → Full status list
Else → Limited to Cancel (6) and Received (7) only
```

---

## 4. Data Model

### 4.1 `PurchaseOrders` (Header)

| Column | Notes |
|---|---|
| `POid` | AutoNumber PK |
| `Code` | FK Users - PO originator |
| `Project` | Free-text project/job/replacement reference (required) |
| `ContactId` | FK Contacts - Supplier |
| `DivisionId` | FK Divisions |
| `PODate` | Creation date (set to `ServerToEST(Now())` on insert) |
| `POStatusId` | 1=Draft, 2=Pending Approval, 3=Approved, 4=Issued, 5=Declined, 6=Cancelled, 7=Received |
| `GST` | Boolean - GST applicable (default True) |
| `POPaymentTypeId` | FK PurchaseOrderPaymentTypes (Credit Card, Account, etc.) |
| `Terms` | Free-text terms (500 char limit) |
| `DateRequired` | Date field (required) |
| `DeliverToLocationId` | FK Locations - Depot delivery address |
| `DeliverToLocation` | Free-text delivery address (500 char limit) - manual entry alternative to LocationId |
| `IntroText` | Notes visible to supplier (1500 char limit) |
| `InternalNotes` | Internal only - Reason for purchase (1500 char limit) |
| `PriceExTotal`, `PriceGSTTotal`, `PriceIncTotal` | Financial totals (client-calculated) |
| `RFQid` | FK RFQ (0 if not from RFQ) |
| `Qid` | FK Quotes (0 if not related to quote) |
| `HasCapEx` | Boolean - set True if any line item has `POProductTypeId` with `CapEx = True` |
| `LocationId` | User's location (from cookie) |

### 4.2 `PurchaseOrderContents` (Line Items)

| Column | Notes |
|---|---|
| `POItemId` | AutoNumber PK |
| `POid` | FK |
| `PartCodeId` | FK PartCodes (division-specific product codes) |
| `Quantity` | Integer |
| `Description` | Item description (with Type prefix if applicable) |
| `PriceEx` | Unit price |
| `PriceExSubTotal` | Extended price |
| `POProductTypeId` | FK PurchaseOrderProductTypes (determines CapEx flag) |

### 4.3 `PurchaseOrderStatus` Lookup

| ID | Status | Description |
|---|---|---|
| 1 | Draft | Initial state |
| 2 | Pending Approval | Awaiting line manager approval |
| 3 | Approved | Ready to issue |
| 4 | Issued | Sent to supplier |
| 5 | Declined | Rejected by approver |
| 6 | Cancelled | Voided |
| 7 | Received | Goods received |

### 4.4 `PurchaseOrderApproval` (Approval Chain Log)

| Column | Notes |
|---|---|
| `POApprovalId` | AutoNumber PK |
| `POid` | FK |
| `Code` | User who approved |
| `DateEntered` | Timestamp (implicit via Access) |

**Logic**: Each approval inserts a row. `GetPONextLineApprover` walks the user hierarchy until finding someone with sufficient `POApprovalLimit`.

### 4.5 `PurchaseOrderInvoices` (Supplier Invoice Tracking)

| Column | Notes |
|---|---|
| `POInvoiceId` | AutoNumber PK |
| `POid` | FK |
| `InvoiceDate` | Date |
| `InvoiceNumber` | Text (12 char) |
| `InvoiceAmount` | Currency |

Max 5 invoices per PO (hard limit in UI). `EnterInvoiceDetails_Proc.asp` deletes all existing rows then re-inserts submitted ones.

### 4.6 `PurchaseOrderAudit`

```
POid | Code | Action | DateEntered
```

Actions: `Created`, `Updated`, `Approved`, `Declined`, `Status updated: <name>`, `Printed`, `Reprinted`, `Issued by email to <addr>`.

### 4.7 `PurchaseOrderProductTypes`

| Column | Notes |
|---|---|
| `POProductTypeId` | PK |
| `POProductType` | Name (e.g., "Stock", "CapEx") |
| `CapEx` | Boolean - determines if items count as capital expenditure |
| `InOrder` | Sort order |

### 4.8 `PurchaseOrderPaymentTypes`

| ID | Type |
|---|---|
| 1 | Credit Card |
| 2 | Account |
| 3 | Credit Application |
| etc. | Configurable |

---

## 5. List Page — `Default.asp` + `IFrame.asp`

### Filter Form (Default.asp)

| Field | Default | Bound to |
|---|---|---|
| Date From | today − 3 months | `DateFrom` |
| Date To | today + 1 day | `DateTo` |
| User | "All users" for Managers; own Code for others | `Code` |
| Supplier | "All companies" + "Not an account" (142) + supplier companies | `CompanyId` |
| Division | user's cookie `DivisionId` | `DivisionId` (options from `Divisions WHERE PurchaseOrders = True`) |
| Status | "All (Active & Complete)" = 0 | `POStatusId`. 555 = Active only (excludes completed/cancelled) |

**Buttons**:
- **Filter** → IFrame.asp
- **Generate Report** → Report.asp (requires DivisionId ≠ 555)
- **Pending Approval** → Quick filter to status 2

### Grid Columns (IFrame.asp)

- PO # | Originator | Supplier | Action | Status | PO Date | PriceExTotal

### Action Buttons per Row

- **View** → `View.asp?POid=<n>`
- **Update Status** → `UpdateStatus.asp?POid=<n>`
- **Edit** → `Edit.asp?POid=<n>`
- **Delete** → `deleteRecord(id)` → `Del_Proc.asp?Id=<n>` (only for Division Managers via `SearchArray` check)

---

## 6. Add Flow (`Add.asp` → `Add2.asp` → `Add_Proc.asp`)

### Step 1: Add.asp
Division selector only. Posts to `Add2.asp` with `DivisionId`.

### Step 2: Add2.asp (Main Form)

**Pre-population Logic**:
- If `?RFQid=<n>`: Loads RFQ data (ContactId, LocationId, Terms, DateRequired, etc.) + RFQContents as line items
- If `?Qid=<n>`: Loads Quote data + QuoteContents + QuoteThirdPartyContents as line items
- Otherwise: Blank form with user's LocationId as default

**Form Fields**:

| Field | Required | Notes |
|---|---|---|
| Contact (Supplier) | **Yes** | Select from user's contacts or TL0039's contacts |
| Delivery Address (Depot) | Yes* | Select from Locations table |
| Or Delivery Address (Manual) | Yes* | Textarea (500 char), mutually exclusive with Depot |
| GST applicable | No | Radio Yes/No (default Yes) |
| Status | Read-only | Shows "Draft" (hidden field POStatusId = 1) |
| Payment Type | **Yes** | POPaymentTypeId dropdown |
| Terms | No | 500 char textarea |
| Date Required | **Yes** | Calendar picker |
| Notes (IntroText) | No | Visible to supplier, 1500 char |
| Project / Job / Replacement | **Yes** | 50 char text |
| Reason For Purchase (InternalNotes) | **Yes** | Internal only, 1500 char |
| Line Items | Min 1 | Dynamic table with Qty, Product Type, Part Code, Description, Unit Price, Subtotal |

**Dynamic Line Item JavaScript** (`PurchaseOrders.js`):
- `Items_InsertLine()` - adds blank row
- `Items_InsertLine_WithData()` - adds row with data (used when pre-filling from RFQ/Quote)
- `Items_CalcTotal()` - recalculates running totals and GST
- `Switch_GST(bool)` - shows/hides GST rows

### Step 3: Add_Proc.asp

```
1. INSERT INTO PurchaseOrders (all header fields, POStatusId = 1)
2. lngPOid = @@IDENTITY
3. Loop through line items (i = 2 to ItemLinesVal):
   - Check ProductType.CapEx → set boolCapEx if any found
   - INSERT INTO PurchaseOrderContents
4. If boolCapEx → UPDATE PurchaseOrders SET HasCapEx = true
5. IF RFQid > 0 → Audit 'Generated by RFQ #<n>'
6. Audit 'Created'
7. IF GetPOLastLineApprover = "Already approved" (within limit):
   - INSERT PurchaseOrderApproval (self-approved)
   - UPDATE POStatusId = 3 (Approved)
   - Audit 'Approved'
   ELSE IF POStatusId = 2 (Pending):
   - SendMail to next approver (GetPONextLineApprover_Email)
8. MyRedirect to Default.asp
```

---

## 7. Edit Flow

### Edit.asp Restrictions
- Status dropdown only shows statuses < 3 (Draft, Pending) if current status ≤ 2
- If status > 2, status is read-only with hidden field preserving current value

### Edit_Proc.asp Logic

```
1. Delete all PurchaseOrderApproval rows for this PO (resets approval chain)
2. DELETE * FROM PurchaseOrderContents WHERE POid = <n>
3. Re-insert all line items from form (same logic as Add_Proc)
4. UPDATE PurchaseOrders header
5. Audit 'Updated'
6. Re-evaluate approval:
   - IF within limit → Auto-approve (status 3)
   - ELSE IF status = 2 → Email next approver
```

**Note**: Edit_Proc does NOT update Qid/RFQid - those are read-only in edit mode.

---

## 8. Approval Workflow

### Approval Chain Algorithm (`ssi_Functions_PO.asp`)

```
GetPONextLineApprover(lngPOid, boolCapEx):
  1. Get originator's POApprovalLimit from UserRoles
  2. IF PriceExTotal <= limit → "Already approved"
  3. ELSE walk LineManagerCode hierarchy:
     - For each manager, check their POApprovalLimit
     - Return first manager where PriceExTotal <= limit
     - Max 10 iterations (prevents infinite loops)
  4. If no one found → returns empty (shouldn't happen with Director at top)
```

### Approval Actions

**Approve.asp**:
```
1. INSERT PurchaseOrderApproval (POid, current user's Code)
2. Get user's approval limit
3. IF IsDirector OR limit >= PriceIncTotal OR user = final approver:
   - UPDATE POStatusId = 3 (Approved)
   - Email originator: "Approved, process complete"
   ELSE:
   - Email next approver: "Waiting for your approval"
4. Audit 'Approved'
```

**Decline.asp**:
```
1. INSERT PurchaseOrderApproval (record of decline)
2. UPDATE POStatusId = 5 (Declined)
3. Email originator
4. Audit 'Declined'
```

---

## 9. Issue/Print/Email Flow

### View.asp Print/Email Logic

```
If boolPrint Or boolEmail Then:
  Select Case POStatusId:
    Case 1, 2, 3:  // Draft, Pending, Approved
      UPDATE POStatusId = 4 (Issued)
      Audit 'Printed' (or 'Reprinted' if already 4)
    Case 4:
      strPOStatus = "REPRINTED"
      Audit 'Reprinted'
```

**Note**: When `Divisions.PurchaseRequests = True` for the PO's division, `View.asp` immediately redirects to `ViewRequest.asp`.

### Email Flow

```
Email.asp (compose)
  ↓ POST to GeneratePO.asp
    ↓ UPDATE POStatusId = 4 (if < 4)
    ↓ Redirect to /MyDeskASPNet/GeneratePurchaseOrder.aspx
      ↓ ABCpdf renders View.asp?Email=True
      ↓ Save to .../PurchaseOrders/Files/PurchaseOrder.pdf
      ↓ Email via .NET SmtpClient OR redirect back to Email_Proc.asp
```

Legacy `Email_Proc.asp` path (used as fallback):
- SendGrid SMTP (smtp.sendgrid.net:587)
- Hardcoded API key
- Always BCCs bertb@techlight.com.au
- Attaches `.../PurchaseOrders/Files/PurchaseOrder.pdf`

---

## 10. Invoice Entry Flow

`EnterDespatchDetails.asp` in Invoices module ≠ `EnterInvoiceDetails.asp` in PO module.

PO Invoice Entry records supplier's invoice details against the PO:

```
EnterInvoiceDetails.asp:
  - Shows up to 5 invoice slots
  - Each: Date (calendar), Number (12 char), Amount (currency)
EnterInvoiceDetails_Proc.asp:
  1. DELETE FROM PurchaseOrderInvoices WHERE POid = <n>
  2. Loop i=1 to 5:
     IF InvoiceDate <> "Date not available" AND Amount is numeric:
       INSERT INTO PurchaseOrderInvoices
```

**Display**: NavBar.asp shows running total of all recorded invoices below the action buttons.

---

## 11. Integration Points

### Inbound

| Source | Entry Point | Data Flow |
|---|---|---|
| RFQ | `GenerateFromRFQ.asp?RFQid=<n>` | Redirects to `Add2.asp` with pre-filled RFQ data |
| Quotes | `Add2.asp?Qid=<n>` | Pre-fills from Quote + QuoteContents + ThirdParty |
| Quotes → Job | JobOrders/Add.asp via transporter | Links PO back to Quote via Qid field |

### Outbound

| Target | Trigger | Action |
|---|---|---|
| `/MyDeskASPNet/GeneratePurchaseOrder.aspx` | GeneratePO.asp | PDF generation |
| Email (SendGrid) | Approve.asp / Add_Proc.asp / UpdateStatus_Proc.asp | Approval notifications |
| JobOrders | Transporter (manual) | `Transporter.asp?POid=<n>` → `JobOrders/Add.asp` |

---

## 12. Known Baseline Issues

1. **SendGrid API Key Exposure**: `Email_Proc.asp` and `EmailDeliveryNote_Proc.asp` contain hardcoded `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`

2. **Edit.asp Status Logic Commented**: Lines 36-44 in Edit.asp contain commented-out restrictions that would prevent editing approved/issued/cancelled POs. Currently users can edit anything (though status dropdown is limited for >2).

3. **Line Item Loop Start**: Both Add_Proc and Edit_Proc start looping at `i = 2` (skipping index 1), which suggests either a legacy offset or intentional skip of a template row.

4. **RFQ Folder Missing**: `Purchasing/Default.asp` links to `…/RFQ/` folder which doesn't exist in the codebase. RFQ functionality exists as database tables and transporters but no standalone UI.

5. **Export Detection in View.asp**: `boolExport` is determined by checking `rsCon("Country")` against variations of "Australia". NZ exports get special GST treatment (12.5% for DivisionId 6).

6. **Price Display Bug**: `ViewRequest.asp` shows unit prices prefixed with "A" (e.g., "A$100.00") - appears to be debug code or currency indicator that should have been removed.

7. **Edit_Proc Delete Syntax**: Uses `Delete * From` (Access SQL syntax) rather than standard `DELETE FROM`.

---

## 13. Status State Machine

```
                    ┌─────────────┐
                    │   Draft (1) │
                    └──────┬──────┘
                           │ Save with status 2
                           ▼
              ┌────────────────────────┐
              │   Pending Approval (2) │◄────────┐
              └───────────┬────────────┘         │
                          │ Approve               │
              ┌───────────┴───────────┐          │
              ▼                       ▼          │
   ┌──────────────┐         ┌──────────────┐     │
   │ Approved (3) │         │ Declined (5) │     │
   └──────┬───────┘         └──────────────┘     │
          │ Print/Email                          │
          ▼                                      │
   ┌──────────────┐                              │
   │  Issued (4)  │─────────────────────────────┘
   └──────┬───────┘    (Update Status to Pending)
          │
    ┌─────┴─────┐
    ▼           ▼
┌────────┐  ┌─────────┐
│Cancelled│  │Received │
│  (6)   │  │  (7)    │
└────────┘  └─────────┘
```

---

## 14. Related Modules

- **10-Quotes.md** — POs can be created from Quotes (job-related purchases)
- **13-RFQ.md** — POs can be generated from RFQ records (planned module)
- **15-JobOrders.md** — POs link to Jobs via Qid
- **50-ASPNet-Interop.md** — PDF generation via `/MyDeskASPNet/GeneratePurchaseOrder.aspx`
