# Code Changes Summary - April 15, 2026

## Overview
This document details all code changes made to the Techlight MyDesk system on April 15, 2026, organized by functionality.

---

## 1. Bug Fix - Quote Recalculation (CRITICAL)

### Files Modified:
- `Clients/SalesEngineTL/Quotes/Edit_Proc.asp`

### Summary:
Fixed critical bug where quote totals were being calculated incorrectly after editing line items. The system was not recalculating server-side totals after line item updates, relying only on client-side JavaScript which could become inconsistent.

### Changes:
- Added server-side recalculation of quote totals after line items are saved
- Calculates totals from `QuoteContents` and `QuoteThirdPartyContents` tables
- Updates `Quotes` table with correct `UnitCostTotal`, `NettPriceTotal`, and `Margin`
- Added validation to ensure `LineTotal = Qty * NetPrice` for each line item
- Integrated `SenderCode` capture into the quote update process

---

## 2. New Feature - Quote Sender Dropdown

### Files Modified:
- `Clients/SalesEngineTL/Quotes/Edit.asp`
- `Clients/SalesEngineTL/Quotes/Edit_Proc.asp`
- `Clients/SalesEngineTL/Quotes/View.asp`
- `Clients/SalesEngineTL/Quotes/Email_Proc.asp`
- `Clients/SalesEngineTL/Quotes/Default.asp`

### Summary:
Added "Quote From" dropdown allowing Isaac to prepare quotes that appear to come from Bert (or vice versa), with flexibility for future team expansion.

### Changes:

**Edit.asp:**
- Added "Quote From" dropdown UI element (lines 161-205)
- Dropdown lists all active users from `Users` table
- Shows current user as default selection
- Only shows other users if manager or specific permissions

**Edit_Proc.asp:**
- Captures `SenderCode` from form POST data (lines 19-21)
- Saves selected sender to database (lines 63-65)
- Falls back to current user code if no sender selected

**View.asp:**
- Displays sender information in quote view
- Uses `SenderCode` to lookup sender details from `Users` table
- Shows sender name, email, and phone in quote header

**Email_Proc.asp:**
- Uses `SenderCode` to determine email "From" field
- Retrieves sender's email address from `Users` table
- Updates email template to show correct sender name

**Default.asp:**
- Added sender column to quote list grid
- Displays who the quote is "From" in the listing

---

## 3. New Feature - Search Quotes by Customer

### Files Modified:
- `Clients/SalesEngineTL/Quotes/Default.asp`
- `Clients/SalesEngineTL/Quotes/IFrame.asp`

### Summary:
Added customer name search capability to the quotes list, allowing users to find quotes by company name rather than only by date range.

### Changes:

**Default.asp:**
- Added "Customer Search" text input field to filter form (lines 40-43, 178-181)
- Added form parameter handling for `CustomerSearch`
- Maintains search term in form after submission

**IFrame.asp:**
- Added customer search filter to SQL WHERE clause (lines 50-54, 258-259, 271-276)
- SQL: `AND Companies.CompanyName LIKE '%searchterm%'`
- Supports partial matching (e.g., "SDF" finds "SDF ELECTRICAL")
- Works alongside existing filters (date range, user, division, status)
- Added customer name column to grid display

---

## 4. New Feature - MYOB CSV Export (Invoice Integration)

### Files Created:
- `Clients/SalesEngineTL/Invoices/ExportToMYOB.asp`
- `Clients/SalesEngineTL/Invoices/ExportToMYOB_Proc.asp`

### Summary:
Created invoice export functionality that generates MYOB-compatible CSV files for streamlined month-end invoice entry into MYOB AccountRight for aged receivables tracking.

### Changes:

**ExportToMYOB.asp (164 lines):**
- Export form with date range selection
- Security check for invoice access permissions
- Checkbox option to export only unexported invoices
- Display of export history from `InvoiceExportLog` table
- Link/button to generate CSV
- Instructions for MYOB import process

**ExportToMYOB_Proc.asp (153 lines):**
- CSV generation with MYOB-compatible format
- Queries invoices by date range and export status
- CSV Header: `Co./Last Name, First Name, Invoice No, Date, Description, Amount`
- Escapes quotes in company names for CSV safety
- Updates `Invoices` table with `ExportedToMYOB = -1` and `ExportedDate = Now()`
- Inserts record into `InvoiceExportLog` table
- Sets HTTP headers for CSV download attachment

### Database Integration:
- Uses new `ExportedToMYOB` column to track export status
- Uses new `ExportedDate` column to record when exported
- Creates audit trail in `InvoiceExportLog` table
- Prevents duplicate exports by filtering on `ExportedToMYOB`

---

## 5. Infrastructure - Setup.ps1 PowerShell 7 Compatibility

### Files Modified:
- `Setup.ps1`

### Summary:
Fixed Setup.ps1 script to work with PowerShell 7 and added ASP handler verification for IIS configuration.

### Changes:
- Replaced `WebAdministration` module with `appcmd.exe` calls (module incompatible with PS7)
- Removed `IIS:` drive dependency (doesn't exist in PS7)
- Fixed string interpolation with colons in binding strings
- Added `Test-ASPPHandler` function to verify ASP handler configuration
- Added automatic detection and repair of missing `asp.dll`
- Added automatic handler mapping for `.asp` files if missing
- Updated troubleshooting section with 404.3 error guidance

---

## 6. Project Cleanup (Maintenance)

### Files Deleted:
- `System/Thumbs.db`
- `images/Thumbs.db`
- `Clients/SalesEngineTL/Images/Thumbs.db`
- `System/TTL2.new.mdb`

### Folders Deleted:
- `Quotes/` (empty)
- `data/` (empty)
- `bin/` (empty)
- `App_Code/` (empty)

### Code Cleanup:

**System/Var.asp:**
- Removed VA (Vantage) and TT (TTL) client conditions
- Kept only TL (Techlight) client configuration
- Simplified to single-tenant setup

**System/ssi_dbConn_open.inc:**
- Removed unused database connection logic for:
  - CL (Liosatos.mdb)
  - PL (Pierlite_NSW.mdb)
  - TT (TTL2.mdb)
  - TG (TGA2.mdb)
  - TGA (TGA2.mdb)
  - VA (Vantage.mdb)
- Kept only TL (Techlight2.mdb) connection

---

## Summary Table

| Feature | Type | Files | New/Modified |
|---------|------|-------|--------------|
| Quote Recalculation Bug | Bug Fix | Edit_Proc.asp | Modified |
| Quote Sender Dropdown | New Feature | Edit.asp, Edit_Proc.asp, View.asp, Email_Proc.asp, Default.asp | Modified |
| Customer Search | New Feature | Default.asp, IFrame.asp | Modified |
| MYOB CSV Export | New Feature | ExportToMYOB.asp, ExportToMYOB_Proc.asp | Created |
| Setup.ps1 Fix | Infrastructure | Setup.ps1 | Modified |
| Project Cleanup | Maintenance | Var.asp, ssi_dbConn_open.inc | Modified |

---

## Database Changes Required

These code changes require the following database schema updates:

1. **Quotes table:** Add `SenderCode TEXT(10)` column
2. **Invoices table:** Add `ExportedToMYOB YESNO` column
3. **Invoices table:** Add `ExportedDate DATETIME` column
4. **New table:** `InvoiceExportLog` (tracking table)

See `MyDesk_April2026_Updates.sql` for exact SQL commands.

---

## Testing Checklist

- [ ] Quote totals recalculate correctly after editing line items
- [ ] Sender dropdown shows all active users
- [ ] Emails show correct sender based on dropdown selection
- [ ] Customer search finds partial matches
- [ ] MYOB export generates valid CSV format
- [ ] Exported invoices marked as exported in database
- [ ] Export history tracked in InvoiceExportLog
