-- Create Favourites table for MyDesk
-- Created: 2026-04-30

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Favourites')
BEGIN
    CREATE TABLE Favourites (
        FavouriteId INT IDENTITY(1,1) PRIMARY KEY,
        UserCode NVARCHAR(100) NOT NULL DEFAULT '',
        EntityType NVARCHAR(50) NOT NULL DEFAULT '', -- Invoice, Quote, PO, Contact, Company
        EntityId INT NOT NULL DEFAULT 0,
        EntityName NVARCHAR(200) NOT NULL DEFAULT '',
        DateAdded DATETIME DEFAULT GETDATE(),
        Notes NVARCHAR(500) DEFAULT ''
    );
    
    CREATE INDEX IX_Favourites_UserCode ON Favourites(UserCode);
    CREATE INDEX IX_Favourites_Entity ON Favourites(EntityType, EntityId);
    
    PRINT 'Favourites table created.';
END
ELSE
    PRINT 'Favourites table already exists.';
GO

-- Create FileLibrary table for MyDesk
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'FileLibrary')
BEGIN
    CREATE TABLE FileLibrary (
        FileId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        ParentFolderId UNIQUEIDENTIFIER NULL,
        CompanyId INT NULL,
        Name NVARCHAR(500) NOT NULL DEFAULT '',
        IsFolder BIT NOT NULL DEFAULT 0,
        FileSize BIGINT NULL,
        ContentType NVARCHAR(200) NULL,
        FilePath NVARCHAR(1000) NULL,
        IsShared BIT NOT NULL DEFAULT 0,
        SharedWithCompanies NVARCHAR(MAX) NULL, -- JSON array of company IDs
        SharedWithContacts NVARCHAR(MAX) NULL, -- JSON array of contact IDs
        CreatedBy NVARCHAR(100) NULL,
        CreatedAt DATETIME DEFAULT GETDATE(),
        UpdatedAt DATETIME DEFAULT GETDATE()
    );
    
    CREATE INDEX IX_FileLibrary_Parent ON FileLibrary(ParentFolderId);
    CREATE INDEX IX_FileLibrary_Company ON FileLibrary(CompanyId);
    
    -- Insert Techlight company folder
    INSERT INTO FileLibrary (Name, IsFolder, CreatedBy)
    VALUES ('Techlight', 1, 'System');
    
    PRINT 'FileLibrary table created with Techlight folder.';
END
ELSE
    PRINT 'FileLibrary table already exists.';
GO

-- Create QuoteThirdPartyItems table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'QuoteThirdPartyItems')
BEGIN
    CREATE TABLE QuoteThirdPartyItems (
        QuoteThirdPartyId INT IDENTITY(1,1) PRIMARY KEY,
        Qid INT NOT NULL,
        Description NVARCHAR(500) NOT NULL DEFAULT '',
        Quantity DECIMAL(18,2) NOT NULL DEFAULT 0,
        UnitPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
        GSTTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
        ExtPrice DECIMAL(18,2) NOT NULL DEFAULT 0,
        SortOrder INT NOT NULL DEFAULT 0,
        CreatedAt DATETIME DEFAULT GETDATE(),
        UpdatedAt DATETIME DEFAULT GETDATE()
    );
    
    CREATE INDEX IX_QuoteThirdPartyItems_Qid ON QuoteThirdPartyItems(Qid);
    
    -- Add FK constraint if Quotes table exists
    IF EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'Quotes')
    BEGIN
        IF NOT EXISTS (SELECT * FROM sys.foreign_keys WHERE name = 'FK_QuoteThirdPartyItems_Quotes')
        BEGIN
            ALTER TABLE QuoteThirdPartyItems
            ADD CONSTRAINT FK_QuoteThirdPartyItems_Quotes FOREIGN KEY (Qid) REFERENCES Quotes(Qid) ON DELETE CASCADE;
        END
    END
    
    PRINT 'QuoteThirdPartyItems table created.';
END
ELSE
    PRINT 'QuoteThirdPartyItems table already exists.';
GO

-- Ensure Companies table has InvoiceAddress and DeliveryAddress columns
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Companies' AND COLUMN_NAME = 'InvoiceAddress')
BEGIN
    ALTER TABLE Companies ADD InvoiceAddress NVARCHAR(500) DEFAULT '';
    PRINT 'Added InvoiceAddress column to Companies.';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Companies' AND COLUMN_NAME = 'DeliveryAddress')
BEGIN
    ALTER TABLE Companies ADD DeliveryAddress NVARCHAR(500) DEFAULT '';
    PRINT 'Added DeliveryAddress column to Companies.';
END

-- Ensure Companies table has ABN column
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Companies' AND COLUMN_NAME = 'ABN')
BEGIN
    ALTER TABLE Companies ADD ABN NVARCHAR(20) DEFAULT '';
    PRINT 'Added ABN column to Companies.';
END

-- Ensure Companies table has Phone, Fax, Email, Website columns
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Companies' AND COLUMN_NAME = 'Phone')
BEGIN
    ALTER TABLE Companies ADD Phone NVARCHAR(50) DEFAULT '';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Companies' AND COLUMN_NAME = 'Fax')
BEGIN
    ALTER TABLE Companies ADD Fax NVARCHAR(50) DEFAULT '';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Companies' AND COLUMN_NAME = 'Email')
BEGIN
    ALTER TABLE Companies ADD Email NVARCHAR(200) DEFAULT '';
END

IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Companies' AND COLUMN_NAME = 'Website')
BEGIN
    ALTER TABLE Companies ADD Website NVARCHAR(200) DEFAULT '';
END

PRINT 'Database schema update completed.';
