-- Migration: Add TenantId column to Divisions table and apply RLS
-- Date: 2026-05-07
-- This migration adds tenant isolation support to the Divisions table.
-- The TenantIsolationService will automatically create RLS policies on next startup.

-- Add TenantId column if it doesn't exist
IF OBJECT_ID('Divisions') IS NOT NULL
BEGIN
    IF COL_LENGTH('Divisions','TenantId') IS NULL
    BEGIN
        ALTER TABLE Divisions ADD TenantId UNIQUEIDENTIFIER NULL;
        PRINT 'Added TenantId column to Divisions';
    END
    ELSE
    BEGIN
        PRINT 'TenantId column already exists in Divisions';
    END
END

GO

-- Backfill TenantId with Techlight tenant GUID for existing rows
-- Using a separate batch to avoid "column not found in same batch" SQL Server parsing issue
IF OBJECT_ID('Divisions') IS NOT NULL
BEGIN
    UPDATE Divisions
    SET TenantId = '11111111-1111-1111-1111-111111111111'
    WHERE TenantId IS NULL;
    PRINT 'Backfilled Divisions.TenantId with Techlight tenant';
END

PRINT 'Migration 022 completed.';
