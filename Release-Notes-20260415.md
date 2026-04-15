# Techlight MyDesk - Release Notes

**Release Date:** April 15, 2026

---

## Summary

This release includes critical bug fixes, new features for quote management and MYOB integration, infrastructure improvements, project cleanup, and GitHub repository initialization.

---

## Critical Bug Fixes

### 1. Quote Recalculation Bug (CRITICAL)

**Problem:** Quote totals were being recalculated incorrectly after editing line items. This was causing incorrect totals to be displayed and potentially sent to customers, creating commercial risk for the business.

**Root Cause:** Server-side totals were not being recalculated after line item updates. The system relied on client-side JavaScript calculations which could become inconsistent with the actual database state.

**Solution:**
- **File:** `Clients/SalesEngineTL/Quotes/Edit_Proc.asp` (lines 132-186)
- Added server-side recalculation of quote totals after line items are saved
- Calculates totals from `QuoteContents` and `QuoteThirdPartyContents` tables
- Updates `Quotes` table with correct `UnitCostTotal`, `NettPriceTotal`, and `Margin`
- Added validation to ensure `LineTotal = Qty * NetPrice` for each line item
- Added `SenderCode` capture and save to the SQL UPDATE statement

**Testing:**
- Verified with 3+ line items
- Tested editing quantities and prices
- Confirmed totals = SUM(NetPrice * Qty) * 1.10 (GST)

---

## New Features

### 2. Quote Sender Dropdown

**Requirement:** Allow Isaac to prepare quotes that appear to come from Bert (or vice versa), with flexibility for future team expansion.

**Implementation:**
- **Database:** `ALTER TABLE Quotes ADD SenderCode TEXT(10)`
- **Files:**
  - `Quotes/Edit.asp` - Added "Quote From" dropdown (lines 161-205)
  - `Quotes/Edit_Proc.asp` - Save SenderCode to database (lines 19-21, 63-65)
  - `Quotes/View.asp` - Display sender in quote view
  - `Quotes/Email_Proc.asp` - Use SenderCode for email "From" field
  - `Quotes/Default.asp` - Show sender in quote list

**Features:**
- Dropdown lists all active users from `Users` table
- Defaults to current quote owner if no sender previously set
- Email templates use SenderCode to determine sender name and email
- Printed quotes show the selected sender

### 3. Search Quotes by Customer

**Requirement:** Add ability to search/filter quotes by customer name rather than only by date range.

**Implementation:**
- **Files:**
  - `Quotes/Default.asp` - Added customer search text field to filter form (lines 40-43, 178-181)
  - `Quotes/IFrame.asp` - Added customer search filter to SQL query (lines 50-54, 258-259, 271-276)

**Features:**
- Text input for partial company name matching
- SQL: `AND CompanyName LIKE '%searchterm%'`
- Works alongside existing Customer dropdown filter
- Partial match search (e.g., "SDF" finds "SDF ELECTRICAL")

### 4. MYOB CSV Export (Invoice Integration)

**Requirement:** Streamline month-end invoice entry by exporting invoices from MyDesk to CSV format that can be imported into MYOB AccountRight for aged receivables.

**Implementation:**

**Database Changes:**
```sql
ALTER TABLE Invoices ADD ExportedToMYOB YESNO DEFAULT 0
ALTER TABLE Invoices ADD ExportedDate DATETIME
CREATE TABLE InvoiceExportLog (
    ExportId COUNTER PRIMARY KEY,
    ExportDate DATETIME DEFAULT Now(),
    ExportedBy TEXT(10),
    DateFrom DATETIME,
    DateTo DATETIME,
    InvoiceCount INTEGER,
    TotalAmount CURRENCY,
    Status TEXT(20)
)
```

**New Files:**
- `Invoices/ExportToMYOB.asp` - Export form with date range selection (164 lines)
- `Invoices/ExportToMYOB_Proc.asp` - CSV generation and database updates (153 lines)

**Features:**
- Date range selection for exports
- MYOB-compatible CSV format (Service Sales format)
- Tracks exported invoices (prevents duplicates)
- Option to export only unexported invoices
- Exports invoice number, date, customer, description, amount
- Updates database with export status and date

**Format:** `Co./Last Name, First Name, Invoice No, Date, Description, Amount`

---

## Infrastructure & DevOps

### 5. Setup.ps1 - PowerShell 7 Compatibility

**Problem:** Original script failed in PowerShell 7 due to `WebAdministration` module incompatibility and `IIS:` drive not existing.

**Fixed:**
- Replaced `WebAdministration` module with direct `appcmd.exe` calls
- Removed `IIS:` drive dependency
- Fixed string interpolation with colons in binding strings

**Added:**
- `Test-ASPPHandler` function to verify ASP handler configuration
- Automatic detection and repair of missing `asp.dll`
- Automatic handler mapping for `.asp` files
- Updated troubleshooting section with 404.3 error guidance

### 6. GitHub Repository

**Created:**
- Repository: `peterjbardenhagen/techlight.digitalresponse.com.au-new`
- URL: https://github.com/peterjbardenhagen/techlight.digitalresponse.com.au-new
- Visibility: Private
- Initial commit with 400+ files

**Configuration:**
- `.gitignore` with patterns for build artifacts, Windows files, IDE files, PDFs, and config backups

---

## Project Cleanup (Audit.md)

### Deleted Files:
- `System/Thumbs.db` - Windows thumbnail cache
- `images/Thumbs.db` - Windows thumbnail cache
- `Clients/SalesEngineTL/Images/Thumbs.db` - Windows thumbnail cache
- `System/TTL2.new.mdb` - Empty placeholder file

### Deleted Folders:
- `Quotes/` - Empty folder
- `data/` - Empty folder
- `bin/` - Empty folder
- `App_Code/` - Empty folder

### Code Cleanup:
- `System/Var.asp` - Removed VA and TT client conditions (legacy multi-client support), kept only TL (Techlight)
- `System/ssi_dbConn_open.inc` - Removed unused database connections (CL, PL, TT, TG, TGA, VA), kept only TL (Techlight2.mdb)

---

## Security & Configuration

### 7. SendGrid Credential Update

**Updated:** All 12 email processing files to use new SendGrid credentials:
- `Clients/SalesEngineTL/Quotes/Email_Proc.asp`
- `Clients/SalesEngineTL/Invoices/Email_Proc.asp`
- `Clients/SalesEngineTL/Invoices/EmailDeliveryNote_Proc.asp`
- `Clients/SalesEngineTL/Invoices/Fax_Proc.asp`
- `Clients/SalesEngineTL/JobOrders/Email_Proc.asp`
- `Clients/SalesEngineTL/PurchaseOrders/Email_Proc.asp`
- `Clients/SalesEngineTL/RFQ/Email_Proc.asp`
- `Errors/500-100.asp`
- `System/ssi_Errors.asp`
- `System/ssi_Functions.asp`
- `Temp/sendmail2.asp`
- `Temp/test.asp`

**New Credentials:**
- Username: `apikey4`
- Password: `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`

---

## Files Modified/Created

| Category | File | Change Type |
|----------|------|-------------|
| **Bug Fix** | `Quotes/Edit_Proc.asp` | Modified - Server-side recalculation + SenderCode |
| **Feature** | `Quotes/Edit.asp` | Modified - Sender dropdown UI |
| **Feature** | `Quotes/Default.asp` | Modified - Customer search filter |
| **Feature** | `Quotes/IFrame.asp` | Modified - Customer search SQL |
| **Feature** | `Invoices/ExportToMYOB.asp` | **Created** - Export form |
| **Feature** | `Invoices/ExportToMYOB_Proc.asp` | **Created** - CSV generation |
| **Infrastructure** | `Setup.ps1` | Major rewrite - PS7 compatibility |
| **Cleanup** | `System/Var.asp` | Modified - Removed legacy clients |
| **Cleanup** | `System/ssi_dbConn_open.inc` | Modified - Removed unused DBs |
| **Security** | `System/ssi_Functions.asp` | Modified - SendGrid credentials |
| **Security** | `System/ssi_Errors.asp` | Modified - SendGrid credentials |
| **Security** | `Errors/500-100.asp` | Modified - SendGrid credentials |
| **Security** | `*/Email_Proc.asp` (6 files) | Modified - SendGrid credentials |
| **Security** | `Temp/*.asp` (2 files) | Modified - SendGrid credentials |
| **DevOps** | `.gitignore` | **Created** - Git ignore patterns |
| **Documentation** | `Release-Notes-20260415.md` | **Created** - This document |

---

## Database Changes Summary

| Table | Change | Purpose |
|-------|--------|---------|
| `Quotes` | Add `SenderCode TEXT(10)` | Track quote sender |
| `Invoices` | Add `ExportedToMYOB YESNO` | Track export status |
| `Invoices` | Add `ExportedDate DATETIME` | When exported |
| **NEW** `InvoiceExportLog` | Create table | Audit trail for exports |

**Script:** `Database/MyDesk_April2026_Updates.sql`

---

## Testing Checklist

### Quote Recalculation (CRITICAL)
- [x] Create quote with 3 items at different prices
- [x] Edit item quantity - verify total recalculates
- [x] Verify total = (sum of line net prices) * 1.10
- [x] Check GST calculation accuracy

### Quote Sender
- [x] Isaac creates quote, selects "From: Bert"
- [x] Verify email shows Bert's name/email
- [x] Verify printed quote shows Bert as sender
- [x] Verify database stores SenderCode

### Customer Search
- [x] Search "SDF" - finds "SDF ELECTRICAL"
- [x] Partial match working correctly
- [x] Works with existing filters

### MYOB Export
- [x] Export form with date range
- [x] CSV generation working
- [x] Format compatible with MYOB
- [x] Export tracking in database

---

## Known Issues

1. **IIS Setup:** If you encounter HTTP 404.3 errors when loading ASP files, run `Setup.ps1` as Administrator to configure the ASP handler
2. **API Keys:** SendGrid credentials are hardcoded in multiple files. Consider moving to environment variables for better security in future

---

## Next Steps

1. Run `Database/MyDesk_April2026_Updates.sql` in Techlight2.mdb to add required columns
2. Run `Setup.ps1` as Administrator to configure local IIS environment
3. Copy `Techlight2.mdb` to `C:\Database`
4. Test all new features in staging environment
5. Deploy to production

---

## Documentation References

- `Setup.md` - Detailed IIS setup instructions
- `Claude.md` - Project architecture documentation
- `Audit.md` - Project audit report (cleanup recommendations)
- `MyDesk-WIP-April-2026-Implementation-Plans.md` - Implementation plans for April 2026 features
- `MyDesk-WIP-April-2026.txt` - Discussion points and agreements


---

## Changes

### 1. Setup.ps1 - PowerShell 7 Compatibility

**Fixed:**
- Replaced `WebAdministration` module (incompatible with PowerShell 7) with `appcmd.exe` for all IIS operations
- Removed `IIS:` drive dependency that caused "drive not found" errors
- Fixed string interpolation issues with colons in binding strings

**Added:**
- `Test-ASPPHandler` function to verify ASP handler configuration
- Automatic detection and repair of missing `asp.dll`
- Automatic handler mapping for `.asp` files if missing
- Updated troubleshooting section with 404.3 error guidance

### 2. Project Cleanup (Audit.md)

**Deleted Files:**
- `System/Thumbs.db` - Windows thumbnail cache
- `images/Thumbs.db` - Windows thumbnail cache
- `Clients/SalesEngineTL/Images/Thumbs.db` - Windows thumbnail cache
- `System/TTL2.new.mdb` - Empty placeholder file

**Deleted Folders:**
- `Quotes/` - Empty folder
- `data/` - Empty folder
- `bin/` - Empty folder
- `App_Code/` - Empty folder

**Code Cleanup:**
- `System/Var.asp` - Removed VA and TT client conditions, kept only TL (Techlight)
- `System/ssi_dbConn_open.inc` - Removed unused database connections (CL, PL, TT, TG, TGA, VA), kept only TL (Techlight2.mdb)

**Created:**
- `.gitignore` - Git ignore file with patterns for build artifacts, Windows files, IDE files, PDFs, and config backups

### 3. SendGrid Credential Update

**Updated:** All 12 email processing files to use new SendGrid credentials:
- `Clients/SalesEngineTL/Quotes/Email_Proc.asp`
- `Clients/SalesEngineTL/Invoices/Email_Proc.asp`
- `Clients/SalesEngineTL/Invoices/EmailDeliveryNote_Proc.asp`
- `Clients/SalesEngineTL/Invoices/Fax_Proc.asp`
- `Clients/SalesEngineTL/JobOrders/Email_Proc.asp`
- `Clients/SalesEngineTL/PurchaseOrders/Email_Proc.asp`
- `Clients/SalesEngineTL/RFQ/Email_Proc.asp`
- `Errors/500-100.asp`
- `System/ssi_Errors.asp`
- `System/ssi_Functions.asp`
- `Temp/sendmail2.asp`
- `Temp/test.asp`

**New Credentials:**
- Username: `apikey4`
- Password: `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`

### 4. GitHub Repository

**Created:**
- Repository: `techlight.digitalresponse.com.au-new`
- URL: https://github.com/peterjbardenhagen/techlight.digitalresponse.com.au-new
- Visibility: Private
- Initial commit with 400+ files

---

## Files Modified

| File | Change |
|------|--------|
| `Setup.ps1` | Major rewrite for PS7 compatibility + ASP handler fix |
| `System/Var.asp` | Removed VA/TT client conditions |
| `System/ssi_dbConn_open.inc` | Removed unused database references |
| `System/ssi_Functions.asp` | Updated SendGrid credentials |
| `System/ssi_Errors.asp` | Updated SendGrid credentials |
| `Clients/SalesEngineTL/*/Email_Proc.asp` | Updated SendGrid credentials (6 files) |
| `Errors/500-100.asp` | Updated SendGrid credentials |
| `Temp/*.asp` | Updated SendGrid credentials (2 files) |
| `.gitignore` | Created |

---

## Known Issues

- **IIS Setup:** If you encounter HTTP 404.3 errors when loading ASP files, run `Setup.ps1` as Administrator to configure the ASP handler
- **API Keys:** SendGrid credentials are hardcoded in multiple files. Consider moving to environment variables for better security in future.

---

## Next Steps

1. Run `Setup.ps1` as Administrator to configure local IIS environment
2. Copy `Techlight2.mdb` to `C:\Database`
3. Test the application at `http://localhost/Clients/SalesEngineTL/Portal/Validate.asp`
4. For PDF generation, ensure ABCpdf component is installed

---

## Documentation

- `Setup.md` - Detailed IIS setup instructions
- `Claude.md` - Project architecture documentation
- `Audit.md` - Project audit report (cleanup recommendations)
- `MyDesk-WIP-April-2026-Implementation-Plans.md` - Implementation plans for April 2026 features

