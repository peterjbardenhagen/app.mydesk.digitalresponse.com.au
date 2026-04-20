# 15 — Job Orders (Job Monitoring)

Status: **IN REVIEW** — verified against source in `Clients/SalesEngineTL/JobOrders/`.

Job Orders represent the operational fulfillment stage of the sales process. Created from accepted Quotes, Job Orders track the warehouse picking, delivery scheduling, and fulfillment status of line items. The module integrates tightly with Quotes (source) and Invoices (billing).

---

## 1. Files

| File | Role |
|---|---|
| `Default.asp` | Filter + list page for job monitoring. Uses legacy styling. |
| `IFrame.asp` | Data grid showing job line items with status tracking (ActiveWidgets grid). |
| `Add.asp` | Create Job Order from Quote. Pre-fills all Quote data including line items and third-party items. |
| `Add_Proc.asp` | Insert handler: creates `JobOrders` header + `JobOrderContents` + `JobOrderThirdPartyContents`. Alerts Purchasing Manager on create. |
| `Edit.asp` | Edit individual job line item status and scheduling (not header edit). Shows related jobs sidebar. |
| `Edit_Proc.asp` | Update line item status, schedule date, and insert comment. Handles both regular and third-party items. |
| `EditJobOrder.asp` | Edit Job Order header details (customer PO, addresses, project). |
| `EditJobOrder_Proc.asp` | Header update processor. |
| `View.asp` | Printable "Picking Slip" for warehouse use. |
| `NavBar.asp` | Minimal action bar for View.asp (just Close). |
| `UpdateStatus.asp` / `_Proc.asp` | **Misnamed** — actually updates Quote status (not Job Order status). See Code Issues. |
| `GenerateQuote.asp` / `.aspx` / `.aspx.vb` | In-place VB.NET PDF generator for quotes (legacy/copy). |
| `Email.asp` / `Email_Proc.asp` | Email job order details. |
| `Del_Proc.asp` | Delete Job Order with cascade. |
| `Transporter.asp` | One-liner redirect to `Invoices/Add.asp?JobOrderId=<n>`. |
| `ViewHistory.asp` | Job audit trail and comments. |
| `Report.asp` | Printable aggregate job report. |
| `Files/` | PDF output directory. |

---

## 2. URL Map

| URL | Purpose |
|---|---|
| `/Clients/SalesEngineTL/JobOrders/` | Filter + list |
| `…/JobOrders/Add.asp?Qid=<n>` | Create Job from Quote (required param) |
| `…/JobOrders/EditJobOrder.asp?JobOrderId=<n>` | Edit header |
| `…/JobOrders/Edit.asp?JobOrderId=<n>&JobOrderContentId=<n>[&TP=<bool>]` | Edit line item status |
| `…/JobOrders/View.asp?JobOrderId=<n>` | Picking Slip view |
| `…/JobOrders/Transporter.asp?JobOrderId=<n>` | → Invoice creation |
| `…/JobOrders/ViewHistory.asp?JobOrderId=<n>` | Audit trail |
| `…/JobOrders/Del_Proc.asp?JobOrderId=<n>` | Delete |
| `…/JobOrders/Report.asp` | Printable report |

---

## 3. Access Control

All JobOrders pages gate on:
```
Request.Cookies("DivisionIdsAccess")("Quotes") <> "0"
```

Jobs inherit Quotes access permissions — if you can access Quotes, you can access Job Orders for your division.

### List View Permissions
- Managers see all users via `GetAccessCodesList`
- Regular users see only their own jobs

### Edit Permissions
- Any user with Quotes access can edit job line items they own
- Line item status updates require comment entry (enforced client-side)

---

## 4. Data Model

### 4.1 `JobOrders` (Header)

| Column | Notes |
|---|---|
| `JobOrderId` | AutoNumber PK |
| `DivisionId` | FK Divisions |
| `Qid` | FK Quotes (source quote) |
| `Code` | FK Users - originator |
| `CompanyId` | FK Companies (customer) |
| `Company` | Denormalized company name (for non-account) |
| `CustomerPO` | Customer purchase order reference |
| `DelCompany`, `DelAddress1/2`, `DelSuburb`, `DelState`, `DelStateId`, `DelPostCode`, `DelCountry` | Structured delivery address |
| `InvCompany`, `InvAddress1/2`, `InvSuburb`, `InvState`, `InvStateId`, `InvPostCode`, `InvCountry` | Structured invoice address |
| `Project` | Project/job reference text |
| `DateAccepted` | Job creation timestamp |

### 4.2 `JobOrderContents` (Line Items)

| Column | Notes |
|---|---|
| `JobOrderContentId` | AutoNumber PK |
| `JobOrderId` | FK |
| `JobOrderStatusCode` | FK JobOrderStatus (default 10 = New) |
| `ProductId` | FK Products |
| `Quantity` | |
| `Type` | Item type/category |
| `Days`, `Units` | Duration/quantity modifiers (from Quote) |
| `ProductCode` | |
| `Description` | |
| `UnitCost` | Cost price |
| `NettPrice` | Sell price |
| `UnitCostSubTotal` | Extended cost |
| `ExtNettPrice` | Extended sell |
| `DateDeliveryRequested` | Requested delivery date (1/1/1900 = not set) |
| `DateDeliveryScheduled` | Scheduled delivery date (1/1/1900 = not set) |
| `Comment` | Line item comment |

### 4.3 `JobOrderThirdPartyContents` (3rd Party Items)

Same structure as JobOrderContents but for items sourced from external suppliers:

| Column | Notes |
|---|---|
| `JobOrderThirdPartyId` | AutoNumber PK |
| `Supplier` | Supplier name |
| `QuoteNumber` | Supplier's quote reference |
| `QuoteDate`, `ExpiryDate` | |
| `SupplierPartNumber`, `OurPartNumber` | Cross-reference |

### 4.4 `JobOrderStatus` Lookup

| Code | Status |
|---|---|
| 10 | New |
| 20 | Picking |
| 30 | Picked |
| 40 | Packed |
| 50 | Dispatched |
| 60 | Delivered |
| 70 | Invoiced |
| 80 | Cancelled |

### 4.5 `JobOrderComments` / `JobOrderThirdPartyComments`

Audit trail per line item:

| Column | Notes |
|---|---|
| `JobOrderCommentId` / `JobOrderThirdPartyCommentId` | PK |
| `JobOrderStatusCode` | Status at time of comment |
| `JobOrderContentId` / `JobOrderThirdPartyId` | FK |
| `Code` | User who commented |
| `Comment` | Text |
| `DateEntered` | Timestamp |

---

## 5. Create Flow (Quote → Job)

### Step 1: Transporter_QuoteToJob.asp (in Quotes folder)
When a Quote is accepted, the system offers to create a Job Order:
```
Quotes/Transporter_QuoteToJob.asp?Qid=<n>&DivisionId=<n>
  ↓ MyRedirect to JobOrders/Add.asp?Qid=<n>
```

### Step 2: Add.asp
- Loads Quote header and all line items
- Loads all ThirdParty items
- JavaScript pre-fills form with `Items_InsertLine_WithData()` and `ThirdParty_InsertLineWithData()`
- User can adjust quantities, prices, delivery addresses before saving
- Form fields: Customer (CompanyId/CCompany), CustomerPO, Project, Delivery/Invoice addresses, line items, third-party items

### Step 3: Add_Proc.asp

```
1. INSERT INTO JobOrders (header fields, DateAccepted = Now)
2. lngJobOrderId = @@IDENTITY
3. Loop i=2 to ItemLinesVal:
   - INSERT INTO JobOrderContents (JobOrderStatusCode = 10)
   - Get @@IDENTITY as JobOrderContentId
   - INSERT INTO JobOrderComments (status 10, empty comment)
4. Loop i=2 to ThirdPartyLinesVal:
   - INSERT INTO JobOrderThirdPartyContents
   - INSERT INTO JobOrderThirdPartyComments
5. AlertPurchasingManager(intDivisionId, "New Job Order awaiting attention")
6. Redirect to Default.asp
```

---

## 6. Edit Flows

### Edit Line Item (Edit.asp + Edit_Proc.asp)
Used for updating status and schedule of individual items:

**Form Fields**:
- Status (dropdown from JobOrderStatus)
- Comment (required, 500 char)
- Date Scheduled (calendar picker)

**Side Panel**: Shows related items from same Job Order (both regular and third-party) with links to edit each.

**Process**:
```
1. UPDATE JobOrderContents SET DateDeliveryScheduled, JobOrderStatusCode
2. INSERT INTO JobOrderComments (new status, comment, timestamp)
```

### Edit Header (EditJobOrder.asp + EditJobOrder_Proc.asp)
Updates customer-facing information:
- CustomerPO
- Delivery/Invoice addresses
- Project reference

---

## 7. Picking Slip (View.asp)

Printable warehouse document showing:
- JobOrderId as "Slip #"
- Customer name and PO reference
- Project name
- Line items with Product Code, Description, Unit Cost, Nett Price
- Delivery and Invoice addresses

**Navigation**: `NavBar.asp` (minimal - just Close button)

---

## 8. List + Grid (Default.asp + IFrame.asp)

### Filter Form

| Field | Default |
|---|---|
| Date Range | Last 3 months |
| User | All (Managers) / Own (Regular) |
| Keyword | Text search |
| Customer | Company dropdown |
| Status | All (Active) / All (Active & Complete) |
| Division | User's DivisionId |

### Action Buttons (per row in grid)
- **View** → `View.asp`
- **Invoice Job** → `Invoices/Add.asp?JobOrderId=<n>` (creates invoice from job)
- **Edit** → `Edit.asp` (line item edit)
- **Edit Details** → `EditJobOrder.asp` (header edit)

### Grid Columns
- Job # | Qty | Slip # | Job | Status | Quote # | $Cost | $Sell | Margin | Requested | Scheduled | Delivery Address | Invoice Address | Description

---

## 9. Integration Points

### Inbound

| Source | Path |
|---|---|
| Quotes | `Transporter_QuoteToJob.asp` → `Add.asp?Qid=<n>` |
| Invoices | `Transporter.asp?JobOrderId=<n>` → `Invoices/Add.asp` |

### Outbound

| Target | Trigger |
|---|---|
| Invoices | "Invoice Job" button in grid → `Invoices/Add.asp?JobOrderId=<n>` |
| Email | `Email.asp` sends job details to customer |
| Purchasing Manager | `AlertPurchasingManager()` on job creation |

---

## 10. Known Baseline Issues

1. **UpdateStatus.asp Misnomer**: The file `JobOrders/UpdateStatus.asp` actually updates **Quote** status (queries `Quotes` table, updates `QuoteStatusId`), not Job Order status. This appears to be a copy-paste error or legacy naming issue.

2. **Missing Header Edit Gate**: `EditJobOrder.asp` doesn't check if the job is already invoiced before allowing edits.

3. **Address Display Bug**: `IFrame.asp` uses inline SQL to fetch addresses for each row (inefficient for large grids).

4. **Date Sentinel Values**: Empty dates stored as `1/1/1900` instead of NULL, requiring special display handling.

5. **Line Item Loop Start**: Like other modules, loops start at `i = 2`, skipping index 1 (template row pattern).

6. **Third Party Query Duplication**: `Edit.asp` runs the same Third Party query twice (lines 186-220).

7. **Edit_Proc.asp Update Bug**: Third-party update uses wrong table name in SQL (appears to update `JobOrderThirdPartyComments` instead of `JobOrderThirdPartyContents` - see line 37).

---

## 11. Status Workflow

```
New (10) → Picking (20) → Picked (30) → Packed (40) → Dispatched (50) → Delivered (60) → Invoiced (70)
                                    ↓
                                 Cancelled (80)
```

Each status change:
- Updates `JobOrderContents.JobOrderStatusCode`
- Inserts row into `JobOrderComments` with comment and timestamp

---

## 12. Related Modules

- **10-Quotes.md** — Jobs are created from accepted Quotes
- **11-Invoices.md** — Jobs convert to Invoices for billing
- **16-Projects.md** — Jobs reference Projects for categorization
