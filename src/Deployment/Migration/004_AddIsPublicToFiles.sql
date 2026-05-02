-- Migration 004: Add IsPublic column to Files table
-- Run this script against the Techlight_MyDesk database.

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Files') AND name = 'IsPublic')
BEGIN
    ALTER TABLE Files ADD IsPublic bit NOT NULL DEFAULT 1;
    PRINT 'Added IsPublic column to Files table.';
END
ELSE
BEGIN
    PRINT 'IsPublic column already exists in Files table.';
END
GO
