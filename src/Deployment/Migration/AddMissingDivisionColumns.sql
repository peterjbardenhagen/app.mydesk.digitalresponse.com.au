-- =============================================================================
-- Techlight MyDesk — Add Missing Division Columns
-- =============================================================================

USE [Techlight_MyDesk];
GO

-- Add GSTRate column if missing
IF COL_LENGTH('Divisions', 'GSTRate') IS NULL
BEGIN
    ALTER TABLE Divisions ADD GSTRate DECIMAL(5,2) DEFAULT 10.00;
END
GO

-- Add InvoicePrefix column if missing
IF COL_LENGTH('Divisions', 'InvoicePrefix') IS NULL
BEGIN
    ALTER TABLE Divisions ADD InvoicePrefix NVARCHAR(10) DEFAULT 'INV-';
END
GO

-- Add QuotePrefix column if missing
IF COL_LENGTH('Divisions', 'QuotePrefix') IS NULL
BEGIN
    ALTER TABLE Divisions ADD QuotePrefix NVARCHAR(10) DEFAULT 'QT-';
END
GO

-- Add POPrefix column if missing
IF COL_LENGTH('Divisions', 'POPrefix') IS NULL
BEGIN
    ALTER TABLE Divisions ADD POPrefix NVARCHAR(10) DEFAULT 'PO-';
END
GO

-- Add Address column if missing
IF COL_LENGTH('Divisions', 'Address') IS NULL
BEGIN
    ALTER TABLE Divisions ADD Address NVARCHAR(255) NULL;
END
GO

-- Add Email column if missing
IF COL_LENGTH('Divisions', 'Email') IS NULL
BEGIN
    ALTER TABLE Divisions ADD Email NVARCHAR(100) NULL;
END
GO

-- Add Phone column if missing
IF COL_LENGTH('Divisions', 'Phone') IS NULL
BEGIN
    ALTER TABLE Divisions ADD Phone NVARCHAR(20) NULL;
END
GO

PRINT 'Missing Division columns added successfully.';
GO
