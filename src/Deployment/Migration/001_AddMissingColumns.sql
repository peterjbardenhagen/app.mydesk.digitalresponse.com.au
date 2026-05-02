-- Migration 001: Add missing columns to tables to support MyDesk V3.0 features.
-- Run this script against the Techlight_MyDesk database.

-- 1. Favourites: add CreatedAt column (if not exists)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Favourites') AND name = 'CreatedAt')
BEGIN
    ALTER TABLE Favourites ADD CreatedAt datetime NOT NULL DEFAULT GETDATE();
END
GO

-- 2. QuoteContents: add Deleted bit column (if not exists)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('QuoteContents') AND name = 'Deleted')
BEGIN
    ALTER TABLE QuoteContents ADD Deleted bit NOT NULL DEFAULT 0;
END
GO

-- 3. Companies: add missing columns (ABN, IsCustomer, IsSupplier, Notes, Invoice Address, Delivery Address)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Companies') AND name = 'ABN')
BEGIN
    ALTER TABLE Companies ADD ABN nvarchar(50) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Companies') AND name = 'IsCustomer')
BEGIN
    ALTER TABLE Companies ADD IsCustomer bit NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Companies') AND name = 'IsSupplier')
BEGIN
    ALTER TABLE Companies ADD IsSupplier bit NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Companies') AND name = 'Notes')
BEGIN
    ALTER TABLE Companies ADD Notes nvarchar(MAX) NULL;
END
GO

-- Invoice Address columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Companies') AND name = 'InvAddress1')
BEGIN
    ALTER TABLE Companies ADD InvAddress1 nvarchar(200) NULL;
END
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Companies') AND name = 'InvAddress2')
BEGIN
    ALTER TABLE Companies ADD InvAddress2 nvarchar(200) NULL;
END
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Companies') AND name = 'InvSuburb')
BEGIN
    ALTER TABLE Companies ADD InvSuburb nvarchar(100) NULL;
END
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Companies') AND name = 'InvState')
BEGIN
    ALTER TABLE Companies ADD InvState nvarchar(50) NULL;
END
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Companies') AND name = 'InvPostCode')
BEGIN
    ALTER TABLE Companies ADD InvPostCode nvarchar(20) NULL;
END
GO

-- Delivery Address columns
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Companies') AND name = 'DelAddress1')
BEGIN
    ALTER TABLE Companies ADD DelAddress1 nvarchar(200) NULL;
END
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Companies') AND name = 'DelAddress2')
BEGIN
    ALTER TABLE Companies ADD DelAddress2 nvarchar(200) NULL;
END
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Companies') AND name = 'DelSuburb')
BEGIN
    ALTER TABLE Companies ADD DelSuburb nvarchar(100) NULL;
END
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Companies') AND name = 'DelState')
BEGIN
    ALTER TABLE Companies ADD DelState nvarchar(50) NULL;
END
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Companies') AND name = 'DelPostCode')
BEGIN
    ALTER TABLE Companies ADD DelPostCode nvarchar(20) NULL;
END
GO

-- 4. Invoices: add InvoiceNumber column (if not exists)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Invoices') AND name = 'InvoiceNumber')
BEGIN
    ALTER TABLE Invoices ADD InvoiceNumber nvarchar(100) NULL;
END
GO

-- 5. Invoices: add DelAddress columns if missing (used by Despatch PDF)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Invoices') AND name = 'DelAddress1')
BEGIN
    ALTER TABLE Invoices ADD DelAddress1 nvarchar(200) NULL;
END
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Invoices') AND name = 'DelAddress2')
BEGIN
    ALTER TABLE Invoices ADD DelAddress2 nvarchar(200) NULL;
END
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Invoices') AND name = 'DelSuburb')
BEGIN
    ALTER TABLE Invoices ADD DelSuburb nvarchar(100) NULL;
END
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Invoices') AND name = 'DelState')
BEGIN
    ALTER TABLE Invoices ADD DelState nvarchar(50) NULL;
END
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Invoices') AND name = 'DelPostCode')
BEGIN
    ALTER TABLE Invoices ADD DelPostCode nvarchar(20) NULL;
END
GO
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Invoices') AND name = 'DelCountry')
BEGIN
    ALTER TABLE Invoices ADD DelCountry nvarchar(50) NULL;
END
GO

-- 6. Ensure Users table has required columns for "Compare to Users" feature
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Users') AND name = 'IsActive')
BEGIN
    ALTER TABLE Users ADD IsActive bit NOT NULL DEFAULT 1;
END
GO

PRINT 'Migration 001 completed successfully.';
