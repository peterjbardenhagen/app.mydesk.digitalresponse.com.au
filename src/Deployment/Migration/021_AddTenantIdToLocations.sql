-- Migration 021: Add TenantId to Locations table
-- Purpose: Enable tenant isolation for Location data
-- Date: May 2026
-- NOTE: Split into separate batches to avoid "Msg 207: Invalid column name" during ALTER+UPDATE in same batch

-- Step 1: Add TenantId column if missing (idempotent)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Locations' AND COLUMN_NAME='TenantId')
BEGIN
    ALTER TABLE Locations ADD TenantId UNIQUEIDENTIFIER NULL;
    PRINT 'Added TenantId column to Locations table';
END
ELSE
BEGIN
    PRINT 'TenantId column already exists on Locations table';
END
GO

-- Step 2: Backfill NULL TenantId values to Techlight tenant (legacy data) - separate batch
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Locations' AND COLUMN_NAME='TenantId')
BEGIN
    UPDATE Locations 
    SET TenantId = '11111111-1111-1111-1111-111111111111'
    WHERE TenantId IS NULL;
    PRINT 'Backfilled NULL TenantId values to Techlight tenant';
END
GO

-- Step 3: Set TenantId to NOT NULL with default - separate batch
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Locations' AND COLUMN_NAME='TenantId')
BEGIN
    ALTER TABLE Locations 
    ALTER COLUMN TenantId UNIQUEIDENTIFIER NOT NULL;
    PRINT 'Set TenantId to NOT NULL';
END
GO

-- Step 4: Add FK constraint to Tenants (if not already present)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE TABLE_NAME='Locations' AND CONSTRAINT_TYPE='FOREIGN KEY' AND CONSTRAINT_NAME LIKE '%Locations%Tenants%')
BEGIN
    ALTER TABLE Locations 
    ADD CONSTRAINT FK_Locations_Tenants 
    FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId);
    PRINT 'Added FK constraint: Locations.TenantId → Tenants.TenantId';
END
GO

PRINT 'Migration 021 completed.';
