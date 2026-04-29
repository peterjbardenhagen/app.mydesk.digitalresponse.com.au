-- =============================================================================
-- Techlight MyDesk — Add Missing Company Columns
-- This script adds columns that are used by the application but missing from DB
-- =============================================================================

USE [Techlight_MyDesk];
GO

-- Add ABN column
IF COL_LENGTH('Companies', 'ABN') IS NULL
BEGIN
    ALTER TABLE Companies ADD ABN NVARCHAR(20) NULL;
END
GO

-- Add IsCustomer column
IF COL_LENGTH('Companies', 'IsCustomer') IS NULL
BEGIN
    ALTER TABLE Companies ADD IsCustomer BIT NOT NULL DEFAULT 0;
END
GO

-- Add IsSupplier column
IF COL_LENGTH('Companies', 'IsSupplier') IS NULL
BEGIN
    ALTER TABLE Companies ADD IsSupplier BIT NOT NULL DEFAULT 0;
END
GO

-- Add Notes column
IF COL_LENGTH('Companies', 'Notes') IS NULL
BEGIN
    ALTER TABLE Companies ADD Notes NVARCHAR(MAX) NULL;
END
GO

-- Invoice Address fields
IF COL_LENGTH('Companies', 'InvAddress1') IS NULL
BEGIN
    ALTER TABLE Companies ADD InvAddress1 NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH('Companies', 'InvAddress2') IS NULL
BEGIN
    ALTER TABLE Companies ADD InvAddress2 NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH('Companies', 'InvSuburb') IS NULL
BEGIN
    ALTER TABLE Companies ADD InvSuburb NVARCHAR(50) NULL;
END
GO

IF COL_LENGTH('Companies', 'InvState') IS NULL
BEGIN
    ALTER TABLE Companies ADD InvState NVARCHAR(20) NULL;
END
GO

IF COL_LENGTH('Companies', 'InvPostCode') IS NULL
BEGIN
    ALTER TABLE Companies ADD InvPostCode NVARCHAR(10) NULL;
END
GO

-- Delivery Address fields
IF COL_LENGTH('Companies', 'DelAddress1') IS NULL
BEGIN
    ALTER TABLE Companies ADD DelAddress1 NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH('Companies', 'DelAddress2') IS NULL
BEGIN
    ALTER TABLE Companies ADD DelAddress2 NVARCHAR(100) NULL;
END
GO

IF COL_LENGTH('Companies', 'DelSuburb') IS NULL
BEGIN
    ALTER TABLE Companies ADD DelSuburb NVARCHAR(50) NULL;
END
GO

IF COL_LENGTH('Companies', 'DelState') IS NULL
BEGIN
    ALTER TABLE Companies ADD DelState NVARCHAR(20) NULL;
END
GO

IF COL_LENGTH('Companies', 'DelPostCode') IS NULL
BEGIN
    ALTER TABLE Companies ADD DelPostCode NVARCHAR(10) NULL;
END
GO

PRINT 'Missing Company columns added successfully.';
GO
