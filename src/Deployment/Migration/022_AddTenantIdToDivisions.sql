-- Migration 022: Add TenantId to Divisions table
-- Purpose: Enable tenant isolation for Division data
-- Date: May 2026

-- Add TenantId column if missing (idempotent)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Divisions' AND COLUMN_NAME='TenantId')
BEGIN
    ALTER TABLE Divisions ADD TenantId UNIQUEIDENTIFIER NULL;
    PRINT 'Added TenantId column to Divisions table';
END
ELSE
BEGIN
    PRINT 'TenantId column already exists on Divisions table';
END

-- Backfill NULL TenantId values to Techlight tenant (legacy data)
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Divisions' AND COLUMN_NAME='TenantId')
BEGIN
    UPDATE Divisions 
    SET TenantId = '11111111-1111-1111-1111-111111111111'
    WHERE TenantId IS NULL;
    PRINT 'Backfilled NULL TenantId values to Techlight tenant';
END

-- Set TenantId to NOT NULL with default
ALTER TABLE Divisions 
ALTER COLUMN TenantId UNIQUEIDENTIFIER NOT NULL;

-- Add FK constraint to Tenants (if not already present)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS WHERE TABLE_NAME='Divisions' AND CONSTRAINT_TYPE='FOREIGN KEY' AND CONSTRAINT_NAME LIKE '%Divisions%Tenants%')
BEGIN
    ALTER TABLE Divisions 
    ADD CONSTRAINT FK_Divisions_Tenants 
    FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId);
    PRINT 'Added FK constraint: Divisions.TenantId → Tenants.TenantId';
END

-- Add RLS security policy for Divisions (if TenantIsolationService hasn't already)
-- This will be applied by TenantIsolationService.EnforceAsync() at startup,
-- but we include the SQL here for reference
/*
CREATE SECURITY POLICY sp_TenantIsolation_Divisions
    ADD FILTER PREDICATE dbo.fn_TenantPredicate(TenantId) ON [dbo].[Divisions]
    ADD BLOCK PREDICATE dbo.fn_TenantPredicate(TenantId) ON [dbo].[Divisions] AFTER INSERT
    ADD BLOCK PREDICATE dbo.fn_TenantPredicate(TenantId) ON [dbo].[Divisions] AFTER UPDATE
    ADD BLOCK PREDICATE dbo.fn_TenantPredicate(TenantId) ON [dbo].[Divisions] AFTER DELETE
    ADD BLOCK PREDICATE dbo.fn_TenantPredicate(TenantId) ON [dbo].[Divisions] BEFORE DELETE
    ADD BLOCK PREDICATE dbo.fn_TenantPredicate(TenantId) ON [dbo].[Divisions] BEFORE UPDATE
WITH (STATE = ON);
*/

PRINT 'Migration 022 completed.';
