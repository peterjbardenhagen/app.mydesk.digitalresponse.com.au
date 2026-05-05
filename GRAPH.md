# MyDesk Database Schema & Relationships

> **Complete database entity-relationship documentation**
> Generated: 2026-05-03
> Database: Microsoft SQL Server

---

## Table of Contents

1. [Core Business Tables](#1-core-business-tables)
2. [CRM Tables](#2-crm-tables)
3. [Sales Tables](#3-sales-tables)
4. [Purchasing Tables](#4-purchasing-tables)
5. [Operations Tables](#5-operations-tables)
6. [DRM Tables](#6-drm-tables)
7. [Accounting Integration Tables](#7-accounting-integration-tables)
8. [Marketing Tables](#8-marketing-tables)
9. [System & Admin Tables](#9-system--admin-tables)
10. [Entity Relationship Diagrams](#10-entity-relationship-diagrams)

---

## 1. Core Business Tables

### 1.1 Companies
Primary table for customer and supplier organizations.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| CompanyId | INT | PK, Identity | Unique identifier |
| Company | NVARCHAR(100) | Required | Company name |
| Contact | NVARCHAR(100) | Nullable | Primary contact name |
| ABN | NVARCHAR(20) | Nullable | Australian Business Number |
| Phone | NVARCHAR(50) | Nullable | Primary phone |
| Fax | NVARCHAR(50) | Nullable | Fax number |
| Email | NVARCHAR(100) | Nullable | Email address |
| Website | NVARCHAR(200) | Nullable | Company website |
| Address1 | NVARCHAR(100) | Nullable | Street address |
| Address2 | NVARCHAR(100) | Nullable | Address line 2 |
| Suburb | NVARCHAR(50) | Nullable | City/Suburb |
| State | NVARCHAR(50) | Nullable | State/Province |
| PostCode | NVARCHAR(20) | Nullable | Postal code |
| Country | NVARCHAR(50) | Nullable | Country |
| Industry | NVARCHAR(100) | Nullable | Industry classification |
| LeadSource | NVARCHAR(100) | Nullable | How acquired |
| Status | NVARCHAR(50) | Default 'Active' | Active/Inactive |
| IsSupplier | BIT | Default 0 | Supplier flag |
| IsCustomer | BIT | Default 1 | Customer flag |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |
| UpdatedAt | DATETIME | Nullable | Last update |
| AccountingCustomerCode | NVARCHAR(50) | Nullable | MYOB/Xero customer code |
| AccountingSupplierCode | NVARCHAR(50) | Nullable | MYOB/Xero supplier code |
| LastSyncedToAccounting | DATETIME | Nullable | Last sync timestamp |

**Indexes:**
- IX_Companies_Company (Company)
- IX_Companies_Status (Status)
- IX_Companies_IsSupplier (IsSupplier)

---

### 1.2 Contacts
Individual people associated with companies.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| ContactId | INT | PK, Identity | Unique identifier |
| FirstName | NVARCHAR(100) | Required | First name |
| Surname | NVARCHAR(100) | Required | Last name |
| Position | NVARCHAR(100) | Nullable | Job title |
| Email | NVARCHAR(100) | Nullable | Email address |
| Phone | NVARCHAR(50) | Nullable | Phone number |
| Mobile | NVARCHAR(50) | Nullable | Mobile number |
| Fax | NVARCHAR(50) | Nullable | Fax number |
| Address1 | NVARCHAR(100) | Nullable | Street address |
| Address2 | NVARCHAR(100) | Nullable | Address line 2 |
| Suburb | NVARCHAR(50) | Nullable | City/Suburb |
| PostCode | NVARCHAR(20) | Nullable | Postal code |
| CompanyId | INT | FK → Companies | Associated company |
| IsActive | BIT | Default 1 | Active flag |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |

**Indexes:**
- IX_Contacts_CompanyId (CompanyId)
- IX_Contacts_Email (Email)

**Relationships:**
- Contacts.CompanyId → Companies.CompanyId (Many-to-One)

---

### 1.3 Users
System users with authentication.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| UserId | INT | PK, Identity | Unique identifier |
| Code | NVARCHAR(20) | Required, Unique | User code (login) |
| Name | NVARCHAR(100) | Required | Full name |
| Password | NVARCHAR(255) | Required | Hashed password |
| Email | NVARCHAR(100) | Nullable | Email address |
| UserTypeId | INT | FK → UserTypes | Role type |
| DivisionId | INT | FK → Divisions | Assigned division |
| IsActive | BIT | Default 1 | Active flag |
| LastLogin | DATETIME | Nullable | Last login timestamp |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |
| UpdatedAt | DATETIME | Nullable | Last update |

**Relationships:**
- Users.UserTypeId → UserTypes.UserTypeId
- Users.DivisionId → Divisions.DivisionId

---

### 1.4 UserTypes
Role definitions and permissions.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| UserTypeId | INT | PK, Identity | Unique identifier |
| TypeName | NVARCHAR(50) | Required | Role name (Admin, Director, etc.) |
| Description | NVARCHAR(200) | Nullable | Role description |
| IsAdmin | BIT | Default 0 | Full admin access |
| IsDirector | BIT | Default 0 | Director level access |
| IsAccounts | BIT | Default 0 | Accounts team access |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |

---

### 1.5 Divisions
Business divisions/departments.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| DivisionId | INT | PK, Identity | Unique identifier |
| DivisionName | NVARCHAR(100) | Required | Division name |
| QuotePrefix | NVARCHAR(10) | Nullable | Quote number prefix |
| InvoicePrefix | NVARCHAR(10) | Nullable | Invoice number prefix |
| IsActive | BIT | Default 1 | Active flag |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |

---

## 2. CRM Tables

### 2.1 ContactNotes
Notes and interactions with contacts.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| NoteId | INT | PK, Identity | Unique identifier |
| ContactId | INT | FK → Contacts | Associated contact |
| Date | DATETIME | Required | Note date |
| NoteType | NVARCHAR(50) | Required | Type (Call, Email, Meeting, etc.) |
| NoteText | NVARCHAR(MAX) | Required | Note content |
| CreatedBy | NVARCHAR(100) | Nullable | User who created |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |

**Relationships:**
- ContactNotes.ContactId → Contacts.ContactId (Many-to-One)

---

### 2.2 Favourites
User's favourite items for quick access.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| FavouriteId | INT | PK, Identity | Unique identifier |
| UserId | INT | Required | User who favourited |
| EntityType | NVARCHAR(50) | Required | Type (Quote, Invoice, Contact, etc.) |
| EntityId | INT | Required | ID of favourited item |
| DisplayName | NVARCHAR(200) | Required | Display text |
| Url | NVARCHAR(500) | Required | Navigation URL |
| Icon | NVARCHAR(50) | Nullable | Material icon name |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |

---

### 2.3 Files
File storage and metadata.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| FileId | UNIQUEIDENTIFIER | PK, Default NEWID() | Unique identifier |
| FileName | NVARCHAR(255) | Required | Original filename |
| ContentType | NVARCHAR(100) | Required | MIME type |
| FileSize | BIGINT | Required | Size in bytes |
| FilePath | NVARCHAR(500) | Required | Storage path |
| FolderId | UNIQUEIDENTIFIER | FK → FileFolders | Parent folder |
| UploadedBy | INT | Required | Uploader user ID |
| UploadedAt | DATETIME | Default GETDATE() | Upload timestamp |
| IsPublic | BIT | Default 0 | Public access flag |
| Description | NVARCHAR(500) | Nullable | File description |

---

## 3. Sales Tables

### 3.1 Quotes
Sales quotations.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| Qid | INT | PK, Identity | Quote ID (Quote number) |
| QuoteDate | DATETIME | Required | Quote date |
| ContactId | INT | FK → Contacts | Associated contact |
| Reference | NVARCHAR(100) | Nullable | Customer reference |
| CustomerNotes | NVARCHAR(MAX) | Nullable | Notes for customer |
| InternalNotes | NVARCHAR(MAX) | Nullable | Internal notes |
| QuoteStatusId | INT | FK → QuoteStatus | Status |
| DivisionId | INT | FK → Divisions | Division |
| Code | NVARCHAR(20) | FK → Users | Originator code |
| TotalExGST | DECIMAL(18,2) | Default 0 | Total excl GST |
| GST | DECIMAL(18,2) | Default 0 | GST amount |
| Total | DECIMAL(18,2) | Default 0 | Total incl GST |
| ExpiryDate | DATETIME | Nullable | Quote expiry |
| ApprovedBy | NVARCHAR(100) | Nullable | Approver name |
| ApprovedDate | DATETIME | Nullable | Approval date |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |
| UpdatedAt | DATETIME | Nullable | Last update |

**Relationships:**
- Quotes.ContactId → Contacts.ContactId
- Quotes.QuoteStatusId → QuoteStatus.QuoteStatusId
- Quotes.DivisionId → Divisions.DivisionId
- Quotes.Code → Users.Code

---

### 3.2 QuoteContents
Line items for quotes.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| QuoteContentsId | INT | PK, Identity | Unique identifier |
| Qid | INT | FK → Quotes | Parent quote |
| ProductCode | NVARCHAR(50) | Nullable | Product code |
| Description | NVARCHAR(500) | Required | Item description |
| Quantity | DECIMAL(18,2) | Default 1 | Quantity |
| Price | DECIMAL(18,2) | Default 0 | Unit price |
| Discount | DECIMAL(18,2) | Default 0 | Discount % |
| LineTotal | DECIMAL(18,2) | Default 0 | Line total |
| SortOrder | INT | Default 0 | Display order |

**Relationships:**
- QuoteContents.Qid → Quotes.Qid (Many-to-One, Cascade Delete)

---

### 3.3 Invoices
Customer invoices.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| InvoiceId | INT | PK, Identity | Unique identifier |
| InvoiceNumber | NVARCHAR(50) | Required | Invoice number |
| InvoiceDate | DATETIME | Required | Invoice date |
| CompanyId | INT | FK → Companies | Billed company |
| ContactId | INT | FK → Contacts | Contact person |
| InvCompany | NVARCHAR(200) | Nullable | Invoice company name (denormalized) |
| InvAddress | NVARCHAR(500) | Nullable | Invoice address |
| DelCompany | NVARCHAR(200) | Nullable | Delivery company |
| DelAddress | NVARCHAR(500) | Nullable | Delivery address |
| CustomerPO | NVARCHAR(100) | Nullable | Customer PO number |
| Attention | NVARCHAR(100) | Nullable | Attention to |
| Terms | NVARCHAR(50) | Nullable | Payment terms |
| CustomerNotes | NVARCHAR(MAX) | Nullable | Notes for customer |
| InternalNotes | NVARCHAR(MAX) | Nullable | Internal notes |
| InvoiceStatusId | INT | FK → InvoiceStatus | Status |
| DivisionId | INT | FK → Divisions | Division |
| InvoicedBy | NVARCHAR(50) | Nullable | Invoice creator |
| TotalExGST | DECIMAL(18,2) | Default 0 | Total excl GST |
| GST | DECIMAL(18,2) | Default 0 | GST amount |
| Total | DECIMAL(18,2) | Default 0 | Total incl GST |
| Qid | INT | FK → Quotes | Source quote |
| ExportedToAccounting | BIT | Default 0 | Export flag |
| ExportedDate | DATETIME | Nullable | Export timestamp |
| ExternalReference | NVARCHAR(100) | Nullable | MYOB/Xero reference |
| ExportStatus | NVARCHAR(50) | Default 'NotExported' | Export status |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |
| UpdatedAt | DATETIME | Nullable | Last update |

**Relationships:**
- Invoices.CompanyId → Companies.CompanyId
- Invoices.ContactId → Contacts.ContactId
- Invoices.InvoiceStatusId → InvoiceStatus.InvoiceStatusId
- Invoices.Qid → Quotes.Qid

---

### 3.4 InvoiceContents
Line items for invoices.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| InvoiceContentsId | INT | PK, Identity | Unique identifier |
| InvoiceId | INT | FK → Invoices | Parent invoice |
| ProductCode | NVARCHAR(50) | Nullable | Product code |
| Description | NVARCHAR(500) | Required | Item description |
| Quantity | DECIMAL(18,2) | Default 1 | Quantity |
| Price | DECIMAL(18,2) | Default 0 | Unit price |
| LineTotal | DECIMAL(18,2) | Default 0 | Line total |
| SortOrder | INT | Default 0 | Display order |

**Relationships:**
- InvoiceContents.InvoiceId → Invoices.InvoiceId (Many-to-One)

---

### 3.5 QuoteStatus
Quote status lookup.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| QuoteStatusId | INT | PK, Identity | Unique identifier |
| QuoteStatus | NVARCHAR(50) | Required | Status name |
| DisplayOrder | INT | Default 0 | Sort order |
| IsActive | BIT | Default 1 | Active flag |

---

### 3.6 InvoiceStatus
Invoice status lookup.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| InvoiceStatusId | INT | PK, Identity | Unique identifier |
| InvoiceStatus | NVARCHAR(50) | Required | Status name |
| DisplayOrder | INT | Default 0 | Sort order |
| IsActive | BIT | Default 1 | Active flag |

---

## 4. Purchasing Tables

### 4.1 PurchaseOrders
Supplier purchase orders.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| PurchaseOrderId | INT | PK, Identity | Unique identifier |
| PONumber | NVARCHAR(50) | Required | PO number |
| PODate | DATETIME | Required | PO date |
| SupplierId | INT | FK → Companies | Supplier company |
| SupplierName | NVARCHAR(200) | Nullable | Supplier name (denormalized) |
| SupplierAddress | NVARCHAR(500) | Nullable | Supplier address |
| ContactName | NVARCHAR(100) | Nullable | Contact person |
| Reference | NVARCHAR(100) | Nullable | Internal reference |
| POStatusId | INT | FK → POStatus | Status |
| DivisionId | INT | FK → Divisions | Division |
| CreatedBy | NVARCHAR(50) | Nullable | Creator |
| TotalExGST | DECIMAL(18,2) | Default 0 | Total excl GST |
| GST | DECIMAL(18,2) | Default 0 | GST amount |
| Total | DECIMAL(18,2) | Default 0 | Total incl GST |
| DeliveryDate | DATETIME | Nullable | Expected delivery |
| Notes | NVARCHAR(MAX) | Nullable | Notes |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |
| UpdatedAt | DATETIME | Nullable | Last update |

---

### 4.2 PurchaseOrderLines
PO line items.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| POLineId | INT | PK, Identity | Unique identifier |
| PurchaseOrderId | INT | FK → PurchaseOrders | Parent PO |
| LineNumber | INT | Required | Line number |
| ProductCode | NVARCHAR(50) | Nullable | Product code |
| Description | NVARCHAR(500) | Required | Item description |
| Quantity | DECIMAL(18,2) | Default 1 | Quantity |
| UnitPrice | DECIMAL(18,2) | Default 0 | Unit price |
| LineTotal | DECIMAL(18,2) | Default 0 | Line total |

---

### 4.3 PurchaseOrderInvoices
Linked invoices for POs.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| POInvoiceId | INT | PK, Identity | Unique identifier |
| PurchaseOrderId | INT | FK → PurchaseOrders | Parent PO |
| InvoiceNumber | NVARCHAR(50) | Nullable | Invoice number |
| InvoiceDate | DATE | Nullable | Invoice date |
| InvoiceAmount | DECIMAL(18,2) | Nullable | Invoice amount |
| Status | NVARCHAR(50) | Default 'Pending' | Status |
| Notes | NVARCHAR(500) | Nullable | Notes |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |
| UpdatedAt | DATETIME | Default GETDATE() | Last update |

---

### 4.4 Products
Product catalog.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| ProductId | INT | PK, Identity | Unique identifier |
| ProductCode | NVARCHAR(50) | Required, Unique | Product code |
| ProductName | NVARCHAR(200) | Required | Product name |
| Description | NVARCHAR(500) | Nullable | Description |
| Category | NVARCHAR(100) | Nullable | Category |
| UnitPrice | DECIMAL(18,2) | Default 0 | Standard price |
| CostPrice | DECIMAL(18,2) | Default 0 | Cost price |
| GSTCode | NVARCHAR(20) | Default 'GST' | GST code |
| IsActive | BIT | Default 1 | Active flag |
| IncomeAccountCode | NVARCHAR(50) | Default '4-1000' | MYOB income account |
| ExpenseAccountCode | NVARCHAR(50) | Default '5-1000' | MYOB expense account |
| TaxCode | NVARCHAR(20) | Default 'GST' | Tax code |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |
| UpdatedAt | DATETIME | Nullable | Last update |

---

### 4.5 Expenses
Expense tracking.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| ExpenseId | INT | PK, Identity | Unique identifier |
| ExpenseDate | DATE | Required | Expense date |
| Description | NVARCHAR(500) | Required | Description |
| Category | NVARCHAR(100) | Default 'General' | Category |
| Amount | DECIMAL(18,2) | Default 0 | Amount |
| GST | DECIMAL(18,2) | Default 0 | GST portion |
| EmployeeId | INT | FK → Users | Employee |
| IsReimbursed | BIT | Default 0 | Reimbursed flag |
| ReceiptFile | NVARCHAR(255) | Nullable | Receipt filename |
| Notes | NVARCHAR(500) | Nullable | Notes |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |

---

## 5. Operations Tables

### 5.1 JobOrders
Job/Work orders.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| JobOrderId | INT | PK, Identity | Unique identifier |
| JobNumber | NVARCHAR(50) | Required | Job number |
| JobDescription | NVARCHAR(500) | Required | Description |
| CompanyId | INT | FK → Companies | Customer company |
| ContactId | INT | FK → Contacts | Contact person |
| QuoteId | INT | FK → Quotes | Source quote |
| JobOrderStatusId | INT | FK → JobOrderStatus | Status |
| DivisionId | INT | FK → Divisions | Division |
| ScheduledDate | DATETIME | Nullable | Scheduled date |
| CompletedDate | DATETIME | Nullable | Completion date |
| AssignedTo | NVARCHAR(100) | Nullable | Assigned staff |
| Priority | NVARCHAR(20) | Default 'Normal' | Priority |
| Notes | NVARCHAR(MAX) | Nullable | Notes |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |
| UpdatedAt | DATETIME | Nullable | Last update |

---

### 5.2 TimesheetEntries
Staff timesheets.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| TimesheetId | INT | PK, Identity | Unique identifier |
| EntryDate | DATE | Required | Date of work |
| UserId | INT | FK → Users | Staff member |
| JobOrderId | INT | FK → JobOrders | Related job |
| Qid | INT | FK → Quotes | Related quote |
| Description | NVARCHAR(500) | Required | Work description |
| Hours | DECIMAL(5,2) | Default 0 | Hours worked |
| IsBillable | BIT | Default 1 | Billable flag |
| HourlyRate | DECIMAL(18,2) | Default 0 | Hourly rate |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |

---

## 6. DRM Tables

### 6.1 Subscriptions
Recurring subscriptions.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| SubscriptionId | INT | PK, Identity | Unique identifier |
| ClientName | NVARCHAR(200) | Required | Client name |
| Description | NVARCHAR(500) | Required | Description |
| Category | NVARCHAR(100) | Default 'Hosting' | Category |
| Schedule | NVARCHAR(50) | Default 'Monthly' | Billing schedule |
| AmountInclGST | DECIMAL(18,2) | Default 0 | Amount incl GST |
| AmountExGST | DECIMAL(18,2) | Default 0 | Amount excl GST |
| StartDate | DATE | Required | Start date |
| NextInvoiceDate | DATE | Nullable | Next invoice date |
| Status | NVARCHAR(50) | Default 'Active' | Status |
| Notes | NVARCHAR(1000) | Nullable | Notes |
| ApproxCost | DECIMAL(18,2) | Nullable | Approximate cost |
| LoginDetails | NVARCHAR(500) | Nullable | Login info |
| InvoiceLink | NVARCHAR(500) | Nullable | Invoice URL |
| CreatedBy | INT | Nullable | Creator |
| CreatedByName | NVARCHAR(100) | Nullable | Creator name |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |
| UpdatedAt | DATETIME | Default GETDATE() | Last update |

---

### 6.2 DRMProjects
Project tracking.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| ProjectId | INT | PK, Identity | Unique identifier |
| QuoteRef | NVARCHAR(50) | Nullable | Quote reference |
| ClientName | NVARCHAR(200) | Required | Client name |
| ProjectName | NVARCHAR(300) | Required | Project name |
| ProjectType | NVARCHAR(50) | Default 'Fixed' | Type (Fixed, T&M, Subscription) |
| QuoteAmount | DECIMAL(18,2) | Default 0 | Quoted amount |
| HourlyRateDefault | DECIMAL(18,2) | Nullable | Default hourly rate |
| StartDate | DATE | Nullable | Start date |
| EndDate | DATE | Nullable | End date |
| Status | NVARCHAR(50) | Default 'Active' | Status |
| PONumber | NVARCHAR(100) | Nullable | PO number |
| BudgetHours | DECIMAL(8,2) | Nullable | Budgeted hours |
| ActualHours | DECIMAL(8,2) | Default 0 | Actual hours |
| UnbilledHours | DECIMAL(8,2) | Default 0 | Unbilled hours |
| AlreadyInvoiced | DECIMAL(18,2) | Default 0 | Previously invoiced |
| BilledAmount | DECIMAL(18,2) | Default 0 | Total billed |
| UnbilledAmount | DECIMAL(18,2) | Default 0 | Unbilled amount |
| CostAmount | DECIMAL(18,2) | Default 0 | Total costs |
| Profit | DECIMAL(18,2) | Default 0 | Profit |
| Notes | NVARCHAR(1000) | Nullable | Notes |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |
| UpdatedAt | DATETIME | Default GETDATE() | Last update |

---

### 6.3 DRM_TimesheetEntries
DRM-specific timesheets.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| EntryId | INT | PK, Identity | Unique identifier |
| EntryDate | DATE | Required | Date |
| ConsultantId | INT | Required | Consultant user ID |
| ConsultantName | NVARCHAR(100) | Required | Consultant name |
| ClientName | NVARCHAR(200) | Required | Client name |
| ProjectName | NVARCHAR(300) | Required | Project name |
| Task | NVARCHAR(200) | Nullable | Task description |
| Description | NVARCHAR(1000) | Required | Work description |
| Hours | DECIMAL(5,2) | Default 0 | Hours |
| IsBillable | BIT | Default 1 | Billable flag |
| IsInvoiced | BIT | Default 0 | Invoiced flag |
| HourlyRate | DECIMAL(18,2) | Default 0 | Rate |
| Amount | DECIMAL(18,2) | Default 0 | Amount |
| CostRate | DECIMAL(18,2) | Default 0 | Cost rate |
| CostAmount | DECIMAL(18,2) | Default 0 | Cost amount |
| DRMProjectId | INT | FK → DRMProjects | Parent project |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |
| UpdatedAt | DATETIME | Default GETDATE() | Last update |

---

### 6.4 DRMCharges
Non-time charges.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| ChargeId | INT | PK, Identity | Unique identifier |
| ChargeDate | DATE | Required | Date |
| ClientName | NVARCHAR(200) | Required | Client name |
| ProjectName | NVARCHAR(300) | Required | Project name |
| Category | NVARCHAR(100) | Default 'General' | Category |
| Description | NVARCHAR(500) | Required | Description |
| Amount | DECIMAL(18,2) | Default 0 | Amount |
| IsInvoiced | BIT | Default 0 | Invoiced flag |
| Cost | DECIMAL(18,2) | Default 0 | Cost |
| Notes | NVARCHAR(500) | Nullable | Notes |
| DRMProjectId | INT | FK → DRMProjects | Parent project |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |
| UpdatedAt | DATETIME | Default GETDATE() | Last update |

---

### 6.5 ExpenseReports
Monthly expense reports.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| ReportId | INT | PK, Identity | Unique identifier |
| ReportPeriod | DATE | Required | Report month (1st day) |
| Status | NVARCHAR(50) | Default 'Draft' | Status |
| SubmittedBy | INT | Nullable | Submitter |
| SubmittedByName | NVARCHAR(100) | Nullable | Submitter name |
| SubmittedDate | DATETIME | Nullable | Submission date |
| ApprovedBy | INT | Nullable | Approver |
| ApprovedByName | NVARCHAR(100) | Nullable | Approver name |
| ApprovedDate | DATETIME | Nullable | Approval date |
| ReimbursedDate | DATETIME | Nullable | Reimbursement date |
| ReimbursementAmount | DECIMAL(18,2) | Nullable | Amount reimbursed |
| ReimbursementNotes | NVARCHAR(500) | Nullable | Notes |
| TotalExGST | DECIMAL(18,2) | Default 0 | Total excl GST |
| TotalGST | DECIMAL(18,2) | Default 0 | GST amount |
| TotalInclGST | DECIMAL(18,2) | Default 0 | Total incl GST |
| OwnerType | NVARCHAR(10) | Default 'DR' | Owner (DR/PB) |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |
| UpdatedAt | DATETIME | Default GETDATE() | Last update |

---

### 6.6 ExpenseReportLines
Expense report line items.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| LineId | INT | PK, Identity | Unique identifier |
| ReportId | INT | FK → ExpenseReports | Parent report |
| ExpenseDate | DATE | Required | Expense date |
| Description | NVARCHAR(500) | Required | Description |
| Category | NVARCHAR(100) | Default 'General' | Category |
| AmountExGST | DECIMAL(18,2) | Default 0 | Amount excl GST |
| GSTAmount | DECIMAL(18,2) | Default 0 | GST amount |
| AmountInclGST | DECIMAL(18,2) | Default 0 | Amount incl GST |
| OwnerType | NVARCHAR(10) | Default 'DR' | Owner |
| Classification | NVARCHAR(100) | Nullable | Classification |
| HasReceipt | BIT | Default 0 | Receipt flag |
| ReceiptFileName | NVARCHAR(255) | Nullable | Receipt filename |
| ReceiptFilePath | NVARCHAR(500) | Nullable | Receipt path |
| Notes | NVARCHAR(500) | Nullable | Notes |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |

---

### 6.7 O365Subscriptions
Office 365 service tracking.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| O365SubId | INT | PK, Identity | Unique identifier |
| ServiceName | NVARCHAR(200) | Required | Service name |
| CustomerName | NVARCHAR(200) | Required | Customer name |
| UserName | NVARCHAR(100) | Nullable | User name |
| BillingCycle | NVARCHAR(50) | Default 'Monthly' | Billing cycle |
| DateCommenced | DATE | Nullable | Start date |
| CostPrice | DECIMAL(18,2) | Default 0 | Cost price |
| SellPrice | DECIMAL(18,2) | Default 0 | Sell price |
| IsActive | BIT | Default 1 | Active flag |
| Notes | NVARCHAR(500) | Nullable | Notes |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |
| UpdatedAt | DATETIME | Default GETDATE() | Last update |

---

### 6.8 SystemCredentials
Password/credential storage.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| CredentialId | INT | PK, Identity | Unique identifier |
| SiteName | NVARCHAR(200) | Required | Site name |
| Description | NVARCHAR(500) | Nullable | Description |
| Website | NVARCHAR(500) | Nullable | Website URL |
| Username | NVARCHAR(200) | Nullable | Username |
| EncryptedPassword | NVARCHAR(500) | Nullable | Encrypted password |
| Category | NVARCHAR(100) | Nullable | Category |
| IsActive | BIT | Default 1 | Active flag |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |
| UpdatedAt | DATETIME | Default GETDATE() | Last update |

---

## 7. Accounting Integration Tables

### 7.1 InvoiceExportTracking
MYOB/Xero export tracking.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| ExportId | INT | PK, Identity | Unique identifier |
| InvoiceId | INT | Required | Invoice ID |
| InvoiceNumber | NVARCHAR(50) | Required | Invoice number |
| CustomerName | NVARCHAR(200) | Required | Customer name |
| ExportType | NVARCHAR(50) | Default 'MYOB' | Export type |
| ExportFormat | NVARCHAR(50) | Default 'CSV' | Format |
| ExportStatus | NVARCHAR(50) | Default 'Pending' | Status |
| ExportedBy | INT | Nullable | Exporter |
| ExportedByName | NVARCHAR(100) | Nullable | Exporter name |
| ExportedDate | DATETIME | Nullable | Export date |
| FilePath | NVARCHAR(500) | Nullable | File path |
| FileName | NVARCHAR(255) | Nullable | File name |
| ErrorMessage | NVARCHAR(1000) | Nullable | Error message |
| ReconciledDate | DATETIME | Nullable | Reconciliation date |
| ReconciledBy | INT | Nullable | Reconciler |
| ExternalReference | NVARCHAR(100) | Nullable | External system ID |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |
| UpdatedAt | DATETIME | Default GETDATE() | Last update |

---

### 7.2 InvoiceExportLines
Export line item details.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| ExportLineId | INT | PK, Identity | Unique identifier |
| ExportId | INT | FK → InvoiceExportTracking | Parent export |
| InvoiceId | INT | Required | Invoice ID |
| LineNumber | INT | Required | Line number |
| ProductCode | NVARCHAR(50) | Nullable | Product code |
| Description | NVARCHAR(500) | Required | Description |
| Quantity | DECIMAL(10,2) | Default 1 | Quantity |
| UnitPrice | DECIMAL(18,2) | Default 0 | Unit price |
| TaxAmount | DECIMAL(18,2) | Default 0 | Tax amount |
| LineTotal | DECIMAL(18,2) | Default 0 | Line total |
| AccountCode | NVARCHAR(50) | Nullable | GL account code |
| TaxCode | NVARCHAR(20) | Nullable | Tax code |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |

---

### 7.3 AccountingIntegrations
Integration configuration.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| IntegrationId | INT | PK, Identity | Unique identifier |
| IntegrationName | NVARCHAR(100) | Required | Name |
| Provider | NVARCHAR(100) | Required | Provider (MYOB, Xero, etc.) |
| IsEnabled | BIT | Default 0 | Enabled flag |
| IsConnected | BIT | Default 0 | Connected flag |
| ApiEndpoint | NVARCHAR(500) | Nullable | API URL |
| ClientId | NVARCHAR(255) | Nullable | OAuth client ID |
| ClientSecret | NVARCHAR(255) | Nullable | OAuth client secret |
| AccessToken | NVARCHAR(MAX) | Nullable | OAuth access token |
| RefreshToken | NVARCHAR(MAX) | Nullable | OAuth refresh token |
| TokenExpiry | DATETIME | Nullable | Token expiry |
| LastSyncDate | DATETIME | Nullable | Last sync |
| DefaultAccountCode | NVARCHAR(50) | Nullable | Default account |
| DefaultTaxCode | NVARCHAR(20) | Default 'GST' | Default tax code |
| ExportPath | NVARCHAR(500) | Nullable | CSV export path |
| AutoExport | BIT | Default 0 | Auto export flag |
| AutoExportFrequency | NVARCHAR(50) | Nullable | Frequency |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |
| UpdatedAt | DATETIME | Default GETDATE() | Last update |

---

## 8. Marketing Tables

### 8.1 EmailCampaigns
Marketing campaigns.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| CampaignId | INT | PK, Identity | Unique identifier |
| Name | NVARCHAR(200) | Required | Campaign name |
| Subject | NVARCHAR(300) | Nullable | Email subject |
| TemplateId | INT | Nullable | Template ID |
| Status | NVARCHAR(50) | Default 'Draft' | Status |
| ScheduledDate | DATETIME | Nullable | Send date |
| SentDate | DATETIME | Nullable | Actual send date |
| Recipients | INT | Default 0 | Recipient count |
| Opens | INT | Default 0 | Open count |
| Clicks | INT | Default 0 | Click count |
| CreatedBy | INT | Nullable | Creator |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |

---

### 8.2 MarketingAssets
Brand/marketing assets.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| AssetId | INT | PK, Identity | Unique identifier |
| AssetName | NVARCHAR(200) | Required | Asset name |
| AssetType | NVARCHAR(50) | Required | Type (Logo, Image, etc.) |
| FilePath | NVARCHAR(500) | Required | File path |
| ThumbnailPath | NVARCHAR(500) | Nullable | Thumbnail path |
| Description | NVARCHAR(500) | Nullable | Description |
| Tags | NVARCHAR(500) | Nullable | Tags |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |

---

## 9. System & Admin Tables

### 9.1 ErrorLogs
Application error tracking.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| ErrorLogId | INT | PK, Identity | Unique identifier |
| ErrorDate | DATETIME | Required | Error timestamp |
| Severity | NVARCHAR(20) | Required | Critical, Error, Warning, Info |
| Message | NVARCHAR(MAX) | Required | Error message |
| Source | NVARCHAR(255) | Nullable | Source context |
| StackTrace | NVARCHAR(MAX) | Nullable | Stack trace |
| UserName | NVARCHAR(100) | Nullable | User |
| IPAddress | NVARCHAR(45) | Nullable | IP address |
| IsResolved | BIT | Default 0 | Resolved flag |
| ResolvedBy | NVARCHAR(100) | Nullable | Resolver |
| ResolvedDate | DATETIME | Nullable | Resolution date |

---

### 9.2 ApplicationLogs
Application event logging.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| LogId | INT | PK, Identity | Unique identifier |
| LogDate | DATETIME | Default GETDATE() | Timestamp |
| Level | NVARCHAR(20) | Required | Log level |
| Message | NVARCHAR(MAX) | Required | Message |
| SourceContext | NVARCHAR(255) | Nullable | Source |
| Exception | NVARCHAR(MAX) | Nullable | Exception details |
| MachineName | NVARCHAR(100) | Nullable | Server |
| ThreadId | INT | Nullable | Thread ID |
| RequestPath | NVARCHAR(500) | Nullable | URL path |
| RequestMethod | NVARCHAR(10) | Nullable | HTTP method |
| StatusCode | INT | Nullable | Response code |
| ElapsedMs | DECIMAL(10,2) | Nullable | Duration |
| UserName | NVARCHAR(100) | Nullable | User |
| RemoteIP | NVARCHAR(45) | Nullable | Client IP |
| UserAgent | NVARCHAR(500) | Nullable | Browser |

---

### 9.3 EntityAudit
Entity change tracking.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| AuditId | INT | PK, Identity | Unique identifier |
| EntityName | NVARCHAR(100) | Required | Table/Entity name |
| EntityId | INT | Required | Entity ID |
| Action | NVARCHAR(50) | Required | Created, Updated, Deleted |
| OldValues | NVARCHAR(MAX) | Nullable | Previous values (JSON) |
| NewValues | NVARCHAR(MAX) | Nullable | New values (JSON) |
| ChangedBy | INT | Nullable | User ID |
| ChangedByName | NVARCHAR(100) | Nullable | User name |
| ChangedAt | DATETIME | Default GETDATE() | Timestamp |
| IpAddress | NVARCHAR(45) | Nullable | Client IP |

---

### 9.4 AiInteractionAudit
AI feature usage audit.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| AuditId | INT | PK, Identity | Unique identifier |
| InteractionDate | DATETIME | Default GETDATE() | Timestamp |
| UserId | INT | Nullable | User |
| UserName | NVARCHAR(100) | Nullable | User name |
| Feature | NVARCHAR(100) | Required | AI feature used |
| InputSummary | NVARCHAR(MAX) | Nullable | Input summary |
| OutputSummary | NVARCHAR(MAX) | Nullable | Output summary |
| TokensUsed | INT | Nullable | Token count |
| Cost | DECIMAL(10,4) | Nullable | Cost estimate |
| DurationMs | INT | Nullable | Response time |
| Model | NVARCHAR(100) | Nullable | AI model |
| Success | BIT | Default 1 | Success flag |
| ErrorMessage | NVARCHAR(500) | Nullable | Error |

---

### 9.5 SavedReports
User-saved report configurations.

| Column | Type | Constraints | Description |
|--------|------|-------------|-------------|
| ReportId | INT | PK, Identity | Unique identifier |
| Name | NVARCHAR(200) | Required | Report name |
| Description | NVARCHAR(500) | Nullable | Description |
| ReportType | NVARCHAR(50) | Required | Type |
| Configuration | NVARCHAR(MAX) | Nullable | JSON config |
| CreatedBy | INT | Nullable | Creator |
| CreatedByName | NVARCHAR(100) | Nullable | Creator name |
| IsPublic | BIT | Default 0 | Public flag |
| CreatedAt | DATETIME | Default GETDATE() | Creation timestamp |
| UpdatedAt | DATETIME | Default GETDATE() | Last update |

---

## 10. Entity Relationship Diagrams

### 10.1 Core CRM Relationships

```
┌─────────────────┐       ┌─────────────────┐       ┌─────────────────┐
│    Companies    │       │    Contacts     │       │  ContactNotes   │
├─────────────────┤       ├─────────────────┤       ├─────────────────┤
│ PK CompanyId    │◄──────┤ FK CompanyId    │       │ PK NoteId       │
│    Company      │       │ PK ContactId    │◄──────┤ FK ContactId    │
│    ABN          │       │    FirstName    │       │    NoteText     │
│    ...          │       │    Surname      │       │    CreatedBy    │
└─────────────────┘       │    Email        │       └─────────────────┘
                          │    Phone        │
                          └─────────────────┘
                                    │
                                    │
                                    ▼
                          ┌─────────────────┐
                          │     Quotes      │
                          ├─────────────────┤
                          │ PK Qid          │
                          │ FK ContactId    │
                          │    Total        │
                          │    QuoteDate    │
                          └─────────────────┘
```

### 10.2 Sales Document Relationships

```
┌─────────────────┐       ┌─────────────────┐       ┌─────────────────┐
│     Quotes      │       │    Invoices     │       │   Despatch      │
├─────────────────┤       ├─────────────────┤       ├─────────────────┤
│ PK Qid          │◄──────┤ FK Qid          │       │ PK DespatchId   │
│ FK ContactId    │       │ PK InvoiceId    │◄──────┤ FK InvoiceId    │
│    Total        │       │ FK CompanyId    │       │    DespatchDate │
│    QuoteStatus  │       │    InvCompany   │       │    Status       │
└─────────────────┘       │    Total        │       └─────────────────┘
         │                │    ExportStatus │
         │                └─────────────────┘
         │                         │
         ▼                         ▼
┌─────────────────┐       ┌─────────────────┐
│  QuoteContents  │       │ InvoiceContents │
├─────────────────┤       ├─────────────────┤
│ PK QContentsId  │       │ PK IContentsId  │
│ FK Qid          │       │ FK InvoiceId    │
│    Description  │       │    Description  │
│    LineTotal    │       │    LineTotal    │
└─────────────────┘       └─────────────────┘
```

### 10.3 Purchasing Relationships

```
┌─────────────────┐       ┌─────────────────┐       ┌─────────────────┐
│   Companies     │       │ PurchaseOrders  │       │     Products    │
│   (Supplier)    │       ├─────────────────┤       ├─────────────────┤
├─────────────────┤◄──────┤ FK SupplierId   │       │ PK ProductId    │
│ PK CompanyId    │       │ PK PurchaseOrder│       │    ProductCode  │
│    Company      │       │    PONumber     │       │    ProductName  │
└─────────────────┘       │    Total        │       │    UnitPrice    │
                          │    POStatusId   │       └─────────────────┘
                          └─────────────────┘
                                    │
                                    ▼
                          ┌─────────────────┐
                          │ PurchaseOrder   │
                          │    Lines        │
                          ├─────────────────┤
                          │ PK POLineId     │
                          │ FK PurchaseOrder│
                          │    ProductCode  │
                          │    LineTotal    │
                          └─────────────────┘
```

### 10.4 DRM Project Relationships

```
┌─────────────────┐       ┌─────────────────┐       ┌─────────────────┐
│   DRMProjects   │       │ DRM_Timesheet   │       │   DRMCharges    │
├─────────────────┤◄──────┤    Entries      │       ├─────────────────┤
│ PK ProjectId    │       ├─────────────────┤       │ PK ChargeId     │
│    ClientName   │       │ PK EntryId      │       │ FK DRMProjectId │
│    ProjectName  │       │ FK DRMProjectId │       │    Description  │
│    QuoteAmount  │       │    Hours        │       │    Amount       │
│    Profit       │       │    IsBillable   │       │    IsInvoiced   │
└─────────────────┘       │    Amount       │       └─────────────────┘
                          └─────────────────┘
                                    │
                                    ▼
                          ┌─────────────────┐
                          │ ExpenseReports  │
                          ├─────────────────┤
                          │ PK ReportId     │
                          │    ReportPeriod │
                          │    Status       │
                          │    TotalInclGST │
                          └─────────────────┘
                                    │
                                    ▼
                          ┌─────────────────┐
                          │ ExpenseReport   │
                          │    Lines        │
                          ├─────────────────┤
                          │ PK LineId       │
                          │ FK ReportId     │
                          │    AmountInclGST│
                          │    HasReceipt   │
                          └─────────────────┘
```

### 10.5 Accounting Integration Relationships

```
┌─────────────────┐       ┌─────────────────┐       ┌─────────────────┐
│    Invoices     │       │ InvoiceExport   │       │  Accounting     │
│                 │       │    Tracking     │       │  Integrations   │
├─────────────────┤       ├─────────────────┤       ├─────────────────┤
│ PK InvoiceId    │◄──────┤ FK InvoiceId    │       │ PK IntegrationId│
│    InvoiceNumber│       │ PK ExportId     │       │    Provider     │
│    ExportedTo   │       │    ExportType   │       │    IsConnected  │
│    Accounting   │       │    ExportStatus │       │    ApiEndpoint  │
└─────────────────┘       │    FileName     │       └─────────────────┘
                          └─────────────────┘
                                    │
                                    ▼
                          ┌─────────────────┐
                          │ InvoiceExport   │
                          │    Lines        │
                          ├─────────────────┤
                          │ PK ExportLineId │
                          │ FK ExportId     │
                          │    AccountCode  │
                          │    TaxCode      │
                          └─────────────────┘
```

### 10.6 User & Security Relationships

```
┌─────────────────┐       ┌─────────────────┐       ┌─────────────────┐
│    UserTypes    │       │     Users       │       │   Divisions     │
├─────────────────┤       ├─────────────────┤       ├─────────────────┤
│ PK UserTypeId   │◄──────┤ FK UserTypeId   │       │ PK DivisionId   │
│    TypeName     │       │ PK UserId       │◄──────┤    DivisionName │
│    IsAdmin      │       │    Name         │       │    QuotePrefix  │
│    IsDirector   │       │    Code         │       └─────────────────┘
└─────────────────┘       │    Email        │
                          └─────────────────┘
                                    │
                    ┌───────────────┼───────────────┐
                    ▼               ▼               ▼
            ┌──────────────┐ ┌──────────────┐ ┌──────────────┐
            │  Timesheet   │ │  Expenses    │ │    DRM_      │
            │   Entries    │ │              │ │  Timesheet   │
            └──────────────┘ └──────────────┘ └──────────────┘
```

---

## Index Summary

### Critical Indexes (Performance)
- IX_Companies_Company - Company name lookups
- IX_Contacts_CompanyId - Contact-to-company joins
- IX_Quotes_ContactId - Quote lookups by contact
- IX_Quotes_QuoteDate - Date range queries
- IX_Invoices_CompanyId - Invoice lookups by company
- IX_Invoices_InvoiceDate - Date range queries
- IX_PurchaseOrders_SupplierId - PO lookups by supplier
- IX_DRM_TimesheetEntries_ConsultantId - User timesheets
- IX_InvoiceExportTracking_ExportStatus - Export queue

### Foreign Key Indexes
All foreign keys have automatic supporting indexes for JOIN operations.

---

## Data Retention Policies

| Table | Retention | Notes |
|-------|-----------|-------|
| ErrorLogs | 90 days | Purge old resolved errors |
| ApplicationLogs | 30 days | Rotate regularly |
| AiInteractionAudit | 1 year | Keep for billing analysis |
| EntityAudit | 2 years | Compliance requirement |
| InvoiceExportTracking | Indefinite | Audit trail |

---

*Last updated: 2026-05-03*
