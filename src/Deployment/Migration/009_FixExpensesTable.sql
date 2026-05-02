-- Fix Expenses table schema to match the Expense model
-- Created: 2026-05-02

-- Add missing columns if they don't exist
IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Expenses')
BEGIN
    -- Rename ExpenseDate to Date if ExpenseDate exists but Date doesn't
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Expenses' AND COLUMN_NAME = 'ExpenseDate')
       AND NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Expenses' AND COLUMN_NAME = 'Date')
    BEGIN
        EXEC sp_rename 'Expenses.ExpenseDate', 'Date', 'COLUMN';
        PRINT 'Renamed ExpenseDate to Date.';
    END

    -- Add SupplierName column if missing
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Expenses' AND COLUMN_NAME = 'SupplierName')
    BEGIN
        ALTER TABLE Expenses ADD SupplierName NVARCHAR(200) NULL;
        PRINT 'Added SupplierName column.';
    END

    -- Add TaxAmount column if missing
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Expenses' AND COLUMN_NAME = 'TaxAmount')
    BEGIN
        ALTER TABLE Expenses ADD TaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0;
        PRINT 'Added TaxAmount column.';
    END

    -- Add FilePath column if missing
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Expenses' AND COLUMN_NAME = 'FilePath')
    BEGIN
        ALTER TABLE Expenses ADD FilePath NVARCHAR(MAX) NULL;
        PRINT 'Added FilePath column.';
    END

    -- Add AIProcessingResult column if missing
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Expenses' AND COLUMN_NAME = 'AIProcessingResult')
    BEGIN
        ALTER TABLE Expenses ADD AIProcessingResult NVARCHAR(MAX) NULL;
        PRINT 'Added AIProcessingResult column.';
    END

    -- Add CreatedBy column if missing
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Expenses' AND COLUMN_NAME = 'CreatedBy')
    BEGIN
        ALTER TABLE Expenses ADD CreatedBy NVARCHAR(100) NULL;
        PRINT 'Added CreatedBy column.';
    END

    -- Add CreatedAt column if missing
    IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Expenses' AND COLUMN_NAME = 'CreatedAt')
    BEGIN
        ALTER TABLE Expenses ADD CreatedAt DATETIME NOT NULL DEFAULT GETDATE();
        PRINT 'Added CreatedAt column.';
    END

    -- Ensure Category has a default
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Expenses' AND COLUMN_NAME = 'Category' AND IS_NULLABLE = 'YES')
    BEGIN
        ALTER TABLE Expenses ALTER COLUMN Category NVARCHAR(100) NOT NULL;
        PRINT 'Made Category NOT NULL.';
    END
END
ELSE
BEGIN
    -- Create the table from scratch if it doesn't exist
    CREATE TABLE Expenses (
        ExpenseId INT IDENTITY(1,1) PRIMARY KEY,
        Date DATETIME NOT NULL DEFAULT GETDATE(),
        Description NVARCHAR(500) NOT NULL DEFAULT '',
        Amount DECIMAL(18,2) NOT NULL DEFAULT 0,
        TaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        GST DECIMAL(18,2) NOT NULL DEFAULT 0,
        Total DECIMAL(18,2) NOT NULL DEFAULT 0,
        SupplierId INT NULL,
        SupplierName NVARCHAR(200) NULL,
        Category NVARCHAR(100) NOT NULL DEFAULT 'General',
        Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        FileName NVARCHAR(255) NULL,
        FilePath NVARCHAR(MAX) NULL,
        AIProcessingResult NVARCHAR(MAX) NULL,
        CreatedBy NVARCHAR(100) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_Expenses_Date ON Expenses(Date DESC);
    PRINT 'Created Expenses table.';
END

PRINT 'Expenses table fix completed.';
