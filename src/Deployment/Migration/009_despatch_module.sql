-- Migration 009: Create Despatch and DespatchItems tables
-- Supports delivery tracking with proof of delivery

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'Despatch')
BEGIN
    CREATE TABLE Despatch (
        DespatchId INT PRIMARY KEY IDENTITY(1,1),
        Reference NVARCHAR(50) NOT NULL,
        OrderReference NVARCHAR(50),
        DeliveryDate DATE NOT NULL,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Pending',  -- Pending, InTransit, Delivered, Failed
        RecipientName NVARCHAR(200),
        RecipientAddress NVARCHAR(500),
        RecipientPhone NVARCHAR(20),
        DeliveredDate DATETIME2,
        DeliveredBy INT,
        SignatureUrl NVARCHAR(500),
        Notes NVARCHAR(MAX),
        TenantId UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2,
        CONSTRAINT FK_Despatch_DeliveredBy FOREIGN KEY (DeliveredBy) REFERENCES Users(UserId),
        CONSTRAINT FK_Despatch_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId) ON DELETE CASCADE
    );

    CREATE INDEX IX_Despatch_TenantId ON Despatch(TenantId);
    CREATE INDEX IX_Despatch_Status ON Despatch([Status]);
    CREATE INDEX IX_Despatch_DeliveryDate ON Despatch(DeliveryDate);

    PRINT 'Created Despatch table with indexes';
END
ELSE
    PRINT 'Despatch table already exists';

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'DespatchItems')
BEGIN
    CREATE TABLE DespatchItems (
        ItemId INT PRIMARY KEY IDENTITY(1,1),
        DespatchId INT NOT NULL,
        LineNumber INT,
        Description NVARCHAR(500) NOT NULL,
        Quantity INT NOT NULL,
        Unit NVARCHAR(50),
        Notes NVARCHAR(MAX),
        TenantId UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_DespatchItems_Despatch FOREIGN KEY (DespatchId) REFERENCES Despatch(DespatchId) ON DELETE CASCADE,
        CONSTRAINT FK_DespatchItems_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId) ON DELETE CASCADE
    );

    CREATE INDEX IX_DespatchItems_DespatchId ON DespatchItems(DespatchId);

    PRINT 'Created DespatchItems table with indexes';
END
ELSE
    PRINT 'DespatchItems table already exists';
