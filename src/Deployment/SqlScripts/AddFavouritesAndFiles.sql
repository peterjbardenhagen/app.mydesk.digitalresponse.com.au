-- Favourites table for storing bookmarked records
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Favourites')
BEGIN
    CREATE TABLE Favourites (
        FavouriteId INT IDENTITY(1,1) PRIMARY KEY,
        UserCode NVARCHAR(50) NOT NULL,
        EntityType NVARCHAR(50) NOT NULL, -- 'Quote', 'Invoice', 'PurchaseOrder', 'Contact', 'Company', 'Product'
        EntityId INT NOT NULL,
        EntityName NVARCHAR(255) NULL, -- Display name for quick reference
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_Favourites_Users FOREIGN KEY (UserCode) REFERENCES Users(Code)
    );
    
    CREATE INDEX IX_Favourites_UserCode ON Favourites(UserCode);
    CREATE INDEX IX_Favourites_Entity ON Favourites(EntityType, EntityId);
END
GO

-- Files Library table
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FileLibrary')
BEGIN
    CREATE TABLE FileLibrary (
        FileId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        ParentFolderId UNIQUEIDENTIFIER NULL, -- NULL for root level
        CompanyId INT NULL, -- NULL for general files, set for company-specific folders
        Name NVARCHAR(255) NOT NULL,
        IsFolder BIT NOT NULL DEFAULT 0, -- 1 = folder, 0 = file
        FilePath NVARCHAR(500) NULL, -- physical path for files
        FileSize BIGINT NULL, -- in bytes
        ContentType NVARCHAR(100) NULL,
        SharedWithCompanies NVARCHAR(MAX) NULL, -- JSON array of CompanyId
        SharedWithContacts NVARCHAR(MAX) NULL, -- JSON array of ContactId
        IsPublic BIT NOT NULL DEFAULT 0, -- visible to all
        CreatedBy NVARCHAR(50) NOT NULL,
        CreatedAt DATETIME2 DEFAULT GETDATE(),
        ModifiedAt DATETIME2 DEFAULT GETDATE(),
        CONSTRAINT FK_FileLibrary_Parent FOREIGN KEY (ParentFolderId) REFERENCES FileLibrary(FileId),
        CONSTRAINT FK_FileLibrary_Companies FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId),
        CONSTRAINT FK_FileLibrary_Users FOREIGN KEY (CreatedBy) REFERENCES Users(Code)
    );
    
    CREATE INDEX IX_FileLibrary_Parent ON FileLibrary(ParentFolderId);
    CREATE INDEX IX_FileLibrary_Company ON FileLibrary(CompanyId);
    CREATE INDEX IX_FileLibrary_IsFolder ON FileLibrary(IsFolder);
END
GO

-- Insert Techlight company folder
IF NOT EXISTS (SELECT * FROM FileLibrary WHERE Name = 'Techlight' AND IsFolder = 1)
BEGIN
    DECLARE @TechlightCompanyId INT = (SELECT TOP 1 CompanyId FROM Companies WHERE Company LIKE '%Techlight%');
    INSERT INTO FileLibrary (Name, IsFolder, CompanyId, CreatedBy, IsPublic)
    VALUES ('Techlight', 1, @TechlightCompanyId, 'System', 0);
END
GO
