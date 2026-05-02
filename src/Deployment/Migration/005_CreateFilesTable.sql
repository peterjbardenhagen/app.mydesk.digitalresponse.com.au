-- Migration 005: Create Files Table
-- Run this script against the Techlight_MyDesk database.

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Files')
BEGIN
    CREATE TABLE Files (
        FileId INT IDENTITY(1,1) PRIMARY KEY,
        FileName NVARCHAR(500) NOT NULL,
        FilePath NVARCHAR(MAX) NOT NULL,
        FileSize BIGINT DEFAULT 0,
        ContentType NVARCHAR(100),
        UploadedAt DATETIME DEFAULT GETDATE(),
        UploadedBy NVARCHAR(100),
        IsPublic BIT NOT NULL DEFAULT 1,
        Category NVARCHAR(100),
        EntityId INT NULL,
        EntityType NVARCHAR(50) NULL
    );
    PRINT 'Files table created successfully.';
END
ELSE
BEGIN
    -- Ensure columns exist if table already exists
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Files') AND name = 'IsPublic')
        ALTER TABLE Files ADD IsPublic BIT NOT NULL DEFAULT 1;

    PRINT 'Files table columns verified.';
END
GO
