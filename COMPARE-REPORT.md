# MyDesk V3.0 Enhancement Report
## Bridging User Functionality from V2.0 with Modern Improvements

---

## Executive Summary

MyDesk V3.0 is built on a modern tech stack (.NET 10 + Blazor + MudBlazor) that enables significant improvements over V2.0. This report identifies user-facing functionality gaps and enhancements needed to ensure V3.0 provides a complete, modern user experience that exceeds V2.0 capabilities.

**Key Improvements in V3.0:**
- Modern responsive UI with dark mode support
- Real-time data access
- Better search and filtering
- Improved Companies & Contacts with separate invoice/delivery addresses
- VCF export for contacts
- Import customers from existing invoice data

---

# CRITICAL USER FUNCTIONALITY

## 1. INVOICES

### Current State in V3.0
- View, Add, Edit, Delete invoices
- PDF generation
- Email invoice
- Delivery note generation
- Despatch tracking
- Status changes
- MYOB export

### Required Enhancements

#### High Priority
| # | Feature | Description | V2.0 Behavior | V3.0 Improvement |
|---|---------|-------------|---------------|------------------|
| 1.1 | **Invoice from Quote** | Convert a Quote to Invoice with one click | Manual process - copy data | Should auto-create invoice from quote, copy line items, populate addresses from company |
| 1.2 | **Contact Selection** | Link invoice to a Contact | Free-text attention field | Dropdown to select Contact from Company, auto-populate Attention |
| 1.3 | **Attention Field** | ATTN: line on invoice | Present | Keep - shows Contact name |
| 1.4 | **Account Field** | Customer account number | Present | Add field for MYOB customer ID |
| 1.5 | **Payment Terms** | COD, 7 Days, 30 Days, etc. | Dropdown selection | Add dropdown tied to Company default |
| 1.6 | **Print Handling** | Print should not auto-change status | Changed status to "Issued" on print | Only Draft→Issued; log to audit trail "Invoice Printed" |
| 1.7 | **Email Handling** | Email should not auto-change status | Changed status on email | Only Draft→Issued; log to audit "Invoice Emailed" |
| 1.8 | **Audit Trail** | Track all print/email/status changes | Basic | Full audit log with timestamp, user, action |
| 1.9 | **Company Address Auto-fill** | From linked Company | Manual entry | Auto-populate Invoice/Delivery addresses from Company |
| 1.10 | **Division-Specific Numbering** | INV-TL-001, INV-TT-001 | Manual | Auto-number with division prefix |

#### Medium Priority
| # | Feature | Description |
|---|---------|-------------|
| 1.11 | **Copy Invoice** | Duplicate existing invoice |
| 1.12 | **Credit Note** | Create credit from invoice |
| 1.13 | **Partial Payments** | Record payments against invoice |
| 1.14 | **Overdue Status** | Auto-calculate and display |

---

## 2. QUOTES

### Current State in V3.0
- View, Add, Edit, Delete quotes
- PDF generation
- Email quote
- Status management
- Line items with pricing

### Required Enhancements

#### High Priority
| # | Feature | Description | V2.0 Behavior | V3.0 Improvement |
|---|---------|-------------|---------------|------------------|
| 2.1 | **Copy Quote** | Duplicate existing quote | Present | Add copy function |
| 2.2 | **Quote to Invoice** | Convert Quote to Invoice | Manual | One-click "Create Invoice" button |
| 2.3 | **Quote to Purchase Order** | Create PO from Quote items | Manual | One-click "Create PO" for subcontracted items |
| 2.4 | **Quote Approval Workflow** | Submit for approval, approve/decline | Present | Add approval buttons and status |
| 2.5 | **Quote Expiry Warning** | Notify before quote expires | None | Show warning on expired/expiring quotes |
| 2.6 | **Company/Contact Selection** | Link to Company and Contact | Free-text | Dropdowns with auto-complete |
| 2.7 | **Product Pricing** | Pull pricing from Products | Manual | Auto-fill unit cost from Product |
| 2.8 | **Print/Email Audit** | Track when quoted sent | Present | Full audit trail |
| 2.9 | **Quote Validity** | 30, 60, 90 days | Present | Enforce and show "Valid Until" |

#### Medium Priority
| # | Feature | Description |
|---|---------|-------------|
| 2.10 | **Margin Display** | Show profit $ and % |
| 2.11 | **Sales Project Linking** | Associate with Sales Project |
| 2.12 | **Follow-up Reminders** | Set reminder date |

---

## 3. PURCHASE ORDERS

### Current State in V3.0
- View, Add, Edit, Delete POs
- PDF generation
- Email PO
- Invoice details entry
- Status management

### Required Enhancements

#### High Priority
| # | Feature | Description | V2.0 Behavior | V3.0 Improvement |
|---|---------|-------------|---------------|------------------|
| 3.1 | **Supplier Contact** | Link to Contact at supplier | Free-text | Dropdown from Company contacts |
| 3.2 | **RFQ to PO** | Generate PO from RFQ | Present | Add "Create PO from RFQ" |
| 3.3 | **Quote Line Items** | Pull lines from Quote | Manual | Import from linked Quote |
| 3.4 | **Payment Terms** | COD, 30 Days, etc. | Present | Add dropdown |
| 3.5 | **Location Selection** | Delivery location | Free-text | Dropdown from Locations table |
| 3.6 | **Print/Email Audit** | Track when sent | Present | Full audit trail |
| 3.7 | **Approval Workflow** | Approve/Decline | Present | Add buttons and status |

#### Medium Priority
| # | Feature | Description |
|---|---------|-------------|
| 3.8 | **Copy PO** | Duplicate PO |
| 3.9 | **Part Code Selection** | Select from Parts list |

---

## 4. DESPATCH / DELIVERY

### Current State in V3.0
- Enter despatch details
- Generate delivery note PDF
- View despatch history

### Required Enhancements

#### High Priority
| # | Feature | Description |
|---|---------|-------------|
| 4.1 | **Carrier Selection** | Dropdown of carriers |
| 4.2 | **Tracking Number** | Record consignment number |
| 4.3 | **Package Details** | Pallet count, weight |
| 4.4 | **Create from Invoice** | Button on invoice to create despatch |

---

## 5. COMPANIES (Significantly Enhanced in V3.0)

### V3.0 Improvements Over V2.0

| Feature | V2.0 | V3.0 |
|---------|------|------|
| **Import from Invoices** | ❌ Not available | ✅ NEW - Bulk import customers from invoice data |
| **Invoice Address** | Single address | ✅ Separate Invoice Address fields |
| **Delivery Address** | Single address | ✅ Separate Delivery Address fields |
| **ABN** | Text only | ✅ Text field (validation future) |
| **Customer/Supplier Type** | ❌ Not available | ✅ Flags for Customer, Supplier |
| **Customer Code** | ❌ Not available | ✅ MYOB Customer ID field |
| **Supplier Code** | ❌ Not available | ✅ Supplier Code field |

### Required Enhancements
| # | Feature | Description |
|---|---------|-------------|
| 5.1 | **ABN Validation** | Validate Australian Business Numbers |
| 5.2 | **Default Payment Terms** | Set default terms per company |
| 5.3 | **Credit Limit** | Set customer credit limit |
| 5.4 | **Tax Zone** | GST applicable / Export / Exempt |
| 5.5 | **Primary Contact** | Designate main contact person |
| 5.6 | **Notes** | Company-specific notes |

---

## 6. CONTACTS (Significantly Enhanced in V3.0)

### V3.0 Improvements Over V2.0

| Feature | V2.0 | V3.0 |
|---------|------|------|
| **Company Selector** | Free-text | ✅ Dropdown from Companies |
| **Create Company** | Not while adding contact | ✅ "Add New Company" option |
| **Invoice Address** | ❌ Not available | ✅ Can store per-contact address |
| **Delivery Address** | ❌ Not available | ✅ Can store per-contact address |
| **VCF Export** | ❌ Not available | ✅ **NEW** - Export to Outlook/Thunderbird |

### Required Enhancements
| # | Feature | Description |
|---|---------|-------------|
| 6.1 | **Contact Photo** | Upload/display photo |
| 6.2 | **Department** | Department at company |
| 6.3 | **Contact Type** | Decision maker, influencer, etc. |
| 6.4 | **Last Contacted** | Activity tracking |
| 6.5 | **Contact Hierarchy** | Reports to whom |

---

## 7. DIVISIONS (Enhanced)

### Current Issues
- Division data may be incomplete
- No GST handling per division
- No logo/branding per division

### Required Enhancements
| # | Feature | Description |
|---|---------|-------------|
| 7.1 | **GST Rate** | Default GST % (10% for Australia) |
| 7.2 | **Logo** | Division logo for PDFs |
| 7.3 | **Invoice Prefix** | e.g., "INV-TL-", "INV-TT-" |
| 7.4 | **Quote Prefix** | e.g., "QT-TL-", "QT-TT-" |
| 7.5 | **PO Prefix** | e.g., "PO-TL-", "PO-TT-" |
| 7.6 | **Contact Details** | Phone, email for division |
| 7.7 | **Address** | Division office address |

---

## 8. GST HANDLING (New in V3.0)

### Australian Business GST Logic

| Scenario | GST Applied | Notes |
|----------|-------------|-------|
| Australian Company (has ABN) | ✅ Yes (10%) | Default |
| International Company (no ABN) | ❌ No | Export sales |
| Company marked "GST Exempt" | ❌ No | Special categories |
| Cash Basis < $75k | ❌ No | Optional |

### Required Implementation

1. **Company GST Flag**
   - Has ABN = GST applies (default)
   - No ABN = No GST (export)
   - Override option: GST Exempt

2. **Division GST Rate**
   - Default: 10% (Australian standard)
   - Can be overridden per division

3. **Invoice GST Calculation**
   - Auto-calculate GST based on Company GST status
   - Show GST breakdown on invoice

4. **International Handling**
   - No ABN = treat as export (0% GST)
   - Country field on Company
   - Currency field on Division

---

## 9. PRINT/EMAIL AUDIT LOG

### Current Issue
V2.0 changed status on print/email automatically, which was confusing.

### V3.0 Behavior

| Action | Status Change | Audit Log Entry |
|--------|---------------|-----------------|
| Print (Draft) | Draft → Issued | "Invoice Printed - Status changed to Issued" |
| Print (Issued/Paid) | No change | "Invoice Reprinted" |
| Email (Draft) | Draft → Issued | "Invoice Emailed - Status changed to Issued" |
| Email (Issued/Paid) | No change | "Invoice Emailed" |
| PDF Download | No change | "Invoice PDF Downloaded" |

### Required Fields in Audit Table
- `EntityType` (Quote, Invoice, PO)
- `EntityId`
- `UserCode`
- `Action` (Printed, Emailed, PDF Downloaded, Status Changed)
- `Details` (new status if changed)
- `Timestamp`

---

## 10. QUOTE TO INVOICE WORKFLOW

### V3.0 Implementation

```
Quote (Won) → "Create Invoice" button → Pre-filled Invoice Form → User confirms → Invoice Created
```

**Auto-populate from Quote:**
- Company Name → Invoice To
- Company Address → Invoice Address
- Contact → Attention field
- Line Items → Invoice Line Items
- Pricing (ex-GST, GST, total)
- Customer Notes
- Division
- Quote Reference

**Post-Create:**
- Link Invoice to Quote (Qid field)
- Show "Invoiced" status on Quote
- Audit log entry

---

# SUMMARY: PRIORITY IMPLEMENTATION LIST

## Phase 1: Core Business (Must Have)
1. **Invoice from Quote** - Critical workflow
2. **Company Dropdown** in Contacts - Data integrity
3. **VCF Export** in Contacts - User requirement
4. **Import from Invoices** in Companies - Data migration
5. **Print/Email Audit + Status Logic** - Key functionality
6. **GST Handling** - Australian compliance

## Phase 2: Essential Features
7. **Copy Quote** / Copy PO
8. **Quote to PO** conversion
9. **Quote Approval** workflow
10. **Supplier Contact** in PO
11. **Division GST & Prefixes**
12. **Company Address Auto-fill** in Invoices

## Phase 3: Polish
13. Credit Notes
14. Partial Payments
15. Contact Activity Tracking
16. Quote Expiry Warnings

---

# TECHNICAL RECOMMENDATIONS

## Database Schema Additions

### Companies Table
```sql
ALTER TABLE Companies ADD
    InvAddress1 NVARCHAR(100) NULL,
    InvAddress2 NVARCHAR(100) NULL,
    InvSuburb NVARCHAR(50) NULL,
    InvState NVARCHAR(50) NULL,
    InvPostCode NVARCHAR(10) NULL,
    DelAddress1 NVARCHAR(100) NULL,
    DelAddress2 NVARCHAR(100) NULL,
    DelSuburb NVARCHAR(50) NULL,
    DelState NVARCHAR(50) NULL,
    DelPostCode NVARCHAR(10) NULL,
    IsCustomer BIT DEFAULT 1,
    IsSupplier BIT DEFAULT 0,
    CustomerCode NVARCHAR(50) NULL,
    SupplierCode NVARCHAR(50) NULL,
    HasGST BIT DEFAULT 1,  -- Based on ABN presence
    CreditLimit DECIMAL(18,2) NULL,
    DefaultTerms NVARCHAR(50) NULL,
    Notes NVARCHAR(MAX) NULL;
```

### Divisions Table
```sql
ALTER TABLE Divisions ADD
    GSTRate DECIMAL(5,2) DEFAULT 10.00,
    InvoicePrefix NVARCHAR(10) DEFAULT 'INV-',
    QuotePrefix NVARCHAR(10) DEFAULT 'QT-',
    POPrefix NVARCHAR(10) DEFAULT 'PO-',
    Logo NVARCHAR(255) NULL;
```

### Audit Trail
```sql
CREATE TABLE EntityAudit (
    AuditId INT IDENTITY PRIMARY KEY,
    EntityType NVARCHAR(20),  -- Quote, Invoice, PO
    EntityId INT,
    Code NVARCHAR(50),  -- User
    Action NVARCHAR(100),
    Details NVARCHAR(500) NULL,
    Timestamp DATETIME DEFAULT GETDATE()
);
```

---

# CONCLUSION

MyDesk V3.0 provides a significantly better foundation than V2.0:

✅ **Modern Tech Stack** - Faster, more reliable, responsive
✅ **Better Companies/Contacts** - Separate addresses, VCF export, import feature
✅ **Improved UI/UX** - Dark mode, better navigation, modern design

The enhancements identified in this report will bring V3.0 to full feature parity with V2.0 while leveraging the modern platform capabilities for a superior user experience.

---

*Report generated: April 2026*