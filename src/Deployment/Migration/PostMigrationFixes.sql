-- =============================================================================
-- Techlight MyDesk — Post Migration Fixes
-- Normalizes denormalized columns from the raw Access migration.
-- =============================================================================

USE [Techlight_MyDesk];
GO

-- 0. Ensure CompanyId mapping is the Primary Key on Companies Table
IF NOT EXISTS (SELECT * FROM sys.key_constraints WHERE type = 'PK' AND parent_object_id = OBJECT_ID('Companies'))
BEGIN
    -- Depending on migration script, CompanyId might not be PK on Companies
    ALTER TABLE Companies ADD CONSTRAINT PK_Companies PRIMARY KEY CLUSTERED (CompanyId);
END
GO

-- 1. Contacts Table Customizations
-- Drop the flat CCompany column since we now rely strictly on CompanyId FK
IF COL_LENGTH('Contacts', 'CCompany') IS NOT NULL
BEGIN
    ALTER TABLE Contacts DROP COLUMN CCompany;
END
GO

-- Combine FirstName and Surname into a clean FullName, or leave them separated but drop display logic if needed.
-- Contact strictly relies on CompanyId.
-- Add Foreign key from Contacts -> Companies if not present
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Contacts_Companies')
BEGIN
    ALTER TABLE Contacts WITH NOCHECK
    ADD CONSTRAINT FK_Contacts_Companies FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId);
END
GO

-- 2. Quotes Table Customizations
-- Add Foreign key from Quotes -> Contacts
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Quotes_Contacts')
BEGIN
    ALTER TABLE Quotes WITH NOCHECK
    ADD CONSTRAINT FK_Quotes_Contacts FOREIGN KEY (ContactId) REFERENCES Contacts(ContactId);
END
GO

-- Add Foreign key from Quotes -> QuoteStatus
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Quotes_QuoteStatus')
BEGIN
    ALTER TABLE Quotes WITH NOCHECK
    ADD CONSTRAINT FK_Quotes_QuoteStatus FOREIGN KEY (QuoteStatusId) REFERENCES QuoteStatus(QuoteStatusId);
END
GO

-- 3. Invoices Table Customizations
-- Invoices historically have denormalized names. Let's ensure they have proper constraints.
IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Invoices_Companies')
BEGIN
    ALTER TABLE Invoices WITH NOCHECK
    ADD CONSTRAINT FK_Invoices_Companies FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId);
END
GO

IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_Invoices_InvoiceStatus')
BEGIN
    ALTER TABLE Invoices WITH NOCHECK
    ADD CONSTRAINT FK_Invoices_InvoiceStatus FOREIGN KEY (InvoiceStatusId) REFERENCES InvoiceStatus(InvoiceStatusId);
END
GO

-- Drop redundant CCompany from Invoices (if it's there from Access)
IF COL_LENGTH('Invoices', 'CCompany') IS NOT NULL
BEGIN
    ALTER TABLE Invoices DROP COLUMN CCompany;
END
GO

PRINT 'Post Migration Database Fixes Completed.';
GO
