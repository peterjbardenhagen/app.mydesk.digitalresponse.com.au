-- Migration 021: Add TenantId to Locations table
-- Purpose: Enable tenant isolation for Location data
-- Date: May 2026

-- Add TenantId column if missing (idempotent)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Locations' AND COLUMN_NAME='TenantId')
BEGIN
    ALTER TABLE Locations ADD TenantId UNIQUEIDENTIFIER NULL;
    PRINT 'Added TenantId column to Locations table';
END
ELSE
BEGIN
    PRINT 'TenantId column already exists on Locations table';
END

-- Backfill NULL TenantId values to Techlight tenant (legacy data)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Locations' AND COLUMN_NAME='TenantId')
BEGIN
    UPDATE Locations 
    SET TenantId = '11111111-1111-1111-1111-111111111111'
    WHERE TenantId IS NULL;
    PRINT 'Backfilled NULL TenantId values to Techlight tenant';
END

-- Set TenantId to NOT NULL with default
ALTER TABLE Locations 
ALTER COLUMN TenantId UNIQUEIDENTIFIER NOT NULL;

-- Add FK constraint to Tenants (if not already present)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE TABLE_NAME='Locations' AND CONSTRAINT_TYPE='FOREIGN KEY' AND CONSTRAINT_NAME LIKE '%Locations%Tenants%')
BEGIN
    ALTER TABLE Locations 
    ADD CONSTRAINT FK_Locations_Tenants 
    FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId);
    PRINT 'Added FK constraint: Locations.TenantId → Tenants.TenantId';
END

-- Add RLS security policy for Locations (if TenantIsolationService hasn't already)
-- This will be applied by TenantIsolationService.EnforceAsync() at startup,
-- but we include the SQL here for reference
/*
CREATE SECURITY POLICY sp_TenantIsolation_Locations
    ADD FILTER PREDICATE dbo.fn_TenantPredicate(TenantId) ON [dbo].[Locations]
    ADD BLOCK PREDICATE dbo.fn_TenantPredicate(TenantId) ON [dbo].[Locations] AFTER INSERT
    ADD BLOCK PREDICATE dbo.fn_TenantPredicate(TenantId) ON [dbo].[Locations] AFTER UPDATE
    ADD BLOCK PREDICATE dbo.fn_TenantPredicate(TenantId) ON [dbo].[Locations] AFTER DELETE
    ADD BLOCK PREDICATE dbo.fn_TenantPredicate(TenantId) ON [dbo].[Locations] BEFORE DELETE
    ADD BLOCK PREDICATE dbo.fn_TenantPredicate(TenantId) ON [dbo].[Locations] BEFORE UPDATE
WITH (STATE = ON);
*/

PRINT 'Migration 021 completed.';
