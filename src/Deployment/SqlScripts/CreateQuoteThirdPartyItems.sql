-- Create QuoteThirdPartyItems table for third-party items on quotes
-- Created: 2026-04-30

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
