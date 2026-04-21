-- MyDesk Database Updates - April 2026
-- Run this script in Microsoft Access to add required columns for new features
-- 
-- Instructions:
-- 1. Open Techlight2.mdb in Microsoft Access
-- 2. Go to Database Tools tab > Visual Basic (or press Alt+F11)
-- 3. In the Immediate Window (Ctrl+G), run each ALTER TABLE statement
--    OR use the Access GUI to add the fields manually
--
-- Note: MS Access SQL syntax is slightly different from standard SQL
-- These statements are provided as guidance - may need GUI adjustment

-- ============================================================================
-- ITEM 2: QUOTE SENDER DROPDOWN
-- Add SenderCode column to Quotes table
-- ============================================================================

-- Add SenderCode to Quotes table (stores the user who is sending the quote)
ALTER TABLE Quotes ADD COLUMN SenderCode TEXT(10);

-- Set default value for existing quotes (copy from Code - the quote owner)
UPDATE Quotes SET SenderCode = Code WHERE SenderCode IS NULL;

-- ============================================================================
-- ITEM 4: MYOB EXPORT INTEGRATION
-- Add export tracking columns to Invoices table
-- ============================================================================

-- Add ExportedToMYOB flag to Invoices table
ALTER TABLE Invoices ADD COLUMN ExportedToMYOB YESNO DEFAULT 0;

-- Add ExportedDate to track when invoice was exported
ALTER TABLE Invoices ADD COLUMN ExportedDate DATETIME;

-- ============================================================================
-- ITEM 4: MYOB EXPORT LOG TABLE
-- Create table to track export history
-- ============================================================================

-- Create InvoiceExportLog table
CREATE TABLE InvoiceExportLog (
    ExportId COUNTER PRIMARY KEY,
    ExportDate DATETIME DEFAULT NOW(),
    ExportedBy TEXT(10),
    DateFrom DATETIME,
    DateTo DATETIME,
    InvoiceCount INTEGER,
    TotalAmount CURRENCY,
    Status TEXT(50)
);

-- ============================================================================
-- VERIFICATION QUERIES
-- Run these to verify the changes were applied correctly
-- ============================================================================

-- Check Quotes table has SenderCode
SELECT TOP 1 SenderCode FROM Quotes;

-- Check Invoices table has export columns
SELECT TOP 1 ExportedToMYOB, ExportedDate FROM Invoices;

-- Check InvoiceExportLog table exists
SELECT TOP 1 * FROM InvoiceExportLog;

-- ============================================================================
-- MANUAL GUI INSTRUCTIONS (if SQL fails)
-- ============================================================================

-- For Quotes.SenderCode:
-- 1. Open Techlight2.mdb
-- 2. Right-click Quotes table > Design View
-- 3. Add new field: SenderCode, Data Type: Text, Field Size: 10
-- 4. Save table
-- 5. Run update: UPDATE Quotes SET SenderCode = Code WHERE SenderCode IS NULL;

-- For Invoices.ExportedToMYBOB and ExportedDate:
-- 1. Right-click Invoices table > Design View
-- 2. Add field: ExportedToMYOB, Data Type: Yes/No, Default Value: 0
-- 3. Add field: ExportedDate, Data Type: Date/Time
-- 4. Save table

-- For InvoiceExportLog table:
-- 1. Create > Table Design
-- 2. Add fields as defined above
-- 3. Set ExportId as Primary Key (AutoNumber)
-- 4. Save table as "InvoiceExportLog"

-- HERE IS THE COMPLETE SQL TO RUN THROUGH MYDESK ADMIN

ALTER TABLE Quotes ADD COLUMN SenderCode TEXT(10);

UPDATE Quotes SET SenderCode = Code WHERE SenderCode IS NULL;

ALTER TABLE Invoices ADD COLUMN ExportedToMYOB YESNO;

UPDATE Invoices SET ExportedToMYOB = 0 WHERE ExportedToMYOB IS NULL;

ALTER TABLE Invoices ADD COLUMN ExportedDate DATETIME;

CREATE TABLE InvoiceExportLog (
    ExportId COUNTER PRIMARY KEY,
    ExportDate DATETIME,
    ExportedBy TEXT(10),
    DateFrom DATETIME,
    DateTo DATETIME,
    InvoiceCount INTEGER,
    TotalAmount CURRENCY,
    Status TEXT(50)
);

SELECT TOP 1 SenderCode FROM Quotes;

SELECT TOP 1 ExportedToMYOB, ExportedDate FROM Invoices;

SELECT TOP 1 * FROM InvoiceExportLog;