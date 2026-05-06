-- Migration: Remove Address fields from Contacts table
-- Addresses now come from Company (InvAddress/DelAddress fields)
-- This enforces single source of truth for addresses

BEGIN TRY
    -- Check if columns exist before dropping
    IF COL_LENGTH('Contacts', 'Address1') IS NOT NULL
    BEGIN
        ALTER TABLE Contacts DROP COLUMN Address1;
        PRINT 'Dropped Contacts.Address1';
    END

    IF COL_LENGTH('Contacts', 'Address2') IS NOT NULL
    BEGIN
        ALTER TABLE Contacts DROP COLUMN Address2;
        PRINT 'Dropped Contacts.Address2';
    END

    IF COL_LENGTH('Contacts', 'Suburb') IS NOT NULL
    BEGIN
        ALTER TABLE Contacts DROP COLUMN Suburb;
        PRINT 'Dropped Contacts.Suburb';
    END

    IF COL_LENGTH('Contacts', 'State') IS NOT NULL
    BEGIN
        ALTER TABLE Contacts DROP COLUMN State;
        PRINT 'Dropped Contacts.State';
    END

    IF COL_LENGTH('Contacts', 'PostCode') IS NOT NULL
    BEGIN
        ALTER TABLE Contacts DROP COLUMN PostCode;
        PRINT 'Dropped Contacts.PostCode';
    END

    PRINT 'Migration 020_RemoveContactAddresses completed successfully.';
END TRY
BEGIN CATCH
    PRINT 'Error in Migration 020_RemoveContactAddresses: ' + ERROR_MESSAGE();
    THROW;
END CATCH
