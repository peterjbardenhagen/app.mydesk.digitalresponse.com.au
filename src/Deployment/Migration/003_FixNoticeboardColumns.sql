-- Migration 003: Fix Noticeboard Table
-- This script ensures the Noticeboard table has the correct schema.

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Noticeboard')
BEGIN
    CREATE TABLE Noticeboard (
        NoticeboardId INT IDENTITY(1,1) PRIMARY KEY,
        Title NVARCHAR(500) NOT NULL,
        Notice NVARCHAR(MAX),
        DatePosted DATETIME DEFAULT GETDATE(),
        ExpiryDate DATETIME NULL,
        PostedBy NVARCHAR(100)
    );
    PRINT 'Noticeboard table created.';
END
ELSE
BEGIN
    -- Ensure columns exist
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Noticeboard') AND name = 'Title')
        ALTER TABLE Noticeboard ADD Title NVARCHAR(500) NOT NULL DEFAULT '';
        
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Noticeboard') AND name = 'Notice')
        ALTER TABLE Noticeboard ADD Notice NVARCHAR(MAX) NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Noticeboard') AND name = 'DatePosted')
        ALTER TABLE Noticeboard ADD DatePosted DATETIME DEFAULT GETDATE();

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Noticeboard') AND name = 'ExpiryDate')
        ALTER TABLE Noticeboard ADD ExpiryDate DATETIME NULL;

    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Noticeboard') AND name = 'PostedBy')
        ALTER TABLE Noticeboard ADD PostedBy NVARCHAR(100) NULL;

    PRINT 'Noticeboard table columns verified.';
END
GO
