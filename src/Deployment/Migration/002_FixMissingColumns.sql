-- Migration 002: Add missing columns identified during UAT
-- Run this script against the Techlight_MyDesk database.

-- 1. Expenses: add Filename column if missing
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'Filename')
BEGIN
    ALTER TABLE Expenses ADD Filename nvarchar(255) NULL;
END
GO

-- 2. Invoices: add ContactId column if missing (for PDF generation)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Invoices') AND name = 'ContactId')
BEGIN
    ALTER TABLE Invoices ADD ContactId int NULL;
END
GO

-- 3. Quotes: Ensure CompanyId exists (It should exist, but check)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Quotes') AND name = 'CompanyId')
BEGIN
    ALTER TABLE Quotes ADD CompanyId int NULL;
END
GO

PRINT 'Migration 002 completed successfully.';
