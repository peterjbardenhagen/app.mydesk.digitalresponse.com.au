-- Migration 006: Add Portal Access columns to Contacts table
-- Run this script against the Techlight_MyDesk database.

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Contacts') AND name = 'PortalUsername')
BEGIN
    ALTER TABLE Contacts ADD PortalUsername NVARCHAR(100) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Contacts') AND name = 'PortalPasswordHash')
BEGIN
    ALTER TABLE Contacts ADD PortalPasswordHash NVARCHAR(MAX) NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Contacts') AND name = 'IsPortalEnabled')
BEGIN
    ALTER TABLE Contacts ADD IsPortalEnabled BIT NOT NULL DEFAULT 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Contacts') AND name = 'PortalAccessExpires')
BEGIN
    ALTER TABLE Contacts ADD PortalAccessExpires DATETIME NULL;
END
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Contacts') AND name = 'PortalLastLogin')
BEGIN
    ALTER TABLE Contacts ADD PortalLastLogin DATETIME NULL;
END
GO

PRINT 'Migration 006 completed successfully.';
