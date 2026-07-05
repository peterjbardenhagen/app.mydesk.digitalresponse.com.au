-- Migration 020: Expense Receipt Photos with AI Extraction
-- Purpose: Link receipt photos to expenses and store AI-extracted data
-- Features: Photo capture, AI-powered extraction, user-correctable fields, audit trail

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ExpenseReceipts')
BEGIN
    CREATE TABLE dbo.ExpenseReceipts (
        ReceiptId INT PRIMARY KEY IDENTITY(1,1),
        ExpenseId INT NOT NULL,
        TenantId INT NOT NULL,

        -- Receipt photo metadata
        FileName NVARCHAR(255) NOT NULL,
        ContentType VARCHAR(50) NOT NULL,  -- image/jpeg, image/png
        FilePath NVARCHAR(500) NOT NULL,   -- /tenant/{id}/receipts/{expenseId}/{guid}.jpg
        FileSizeBytes BIGINT NOT NULL,
        UploadedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        -- AI Extraction results
        ExtractionStrategy VARCHAR(50),     -- PdfPig, AzureDocIntel, GptVision, Manual
        ExtractionConfidence DECIMAL(3,2),  -- 0.00 to 1.00
        ExtractionAuditPassed BIT DEFAULT 0,

        -- Extracted & user-corrected data
        ExtractedSupplierName NVARCHAR(255),
        ExtractedDate DATETIME2,
        ExtractedAmount DECIMAL(19,4),
        ExtractedGst DECIMAL(19,4),
        ExtractedDescription NVARCHAR(MAX),
        ExtractedRawText NVARCHAR(MAX),     -- Full OCR text for audit

        -- User corrections (overrides extraction)
        CorrectedSupplierName NVARCHAR(255),
        CorrectedDate DATETIME2,
        CorrectedAmount DECIMAL(19,4),
        CorrectedDescription NVARCHAR(MAX),
        UserCorrectedFields NVARCHAR(500),  -- Comma-separated: SupplierName,Date,Amount

        -- Status tracking
        Status VARCHAR(20) NOT NULL DEFAULT 'Pending',  -- Pending, Reviewed, Rejected, Archived
        ExtractionStatus VARCHAR(20) NOT NULL DEFAULT 'Pending',  -- Pending, Completed, Failed
        RequiresManualReview BIT DEFAULT 0,
        ReviewNotes NVARCHAR(MAX),

        -- Audit
        CreatedBy INT NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ReviewedBy INT,
        ReviewedAt DATETIME2,
        ModifiedBy INT,
        ModifiedAt DATETIME2,

        CONSTRAINT FK_ExpenseReceipts_Expense FOREIGN KEY (ExpenseId) REFERENCES dbo.Expenses(ExpenseId),
        CONSTRAINT FK_ExpenseReceipts_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        CONSTRAINT FK_ExpenseReceipts_CreatedBy FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(UserId),
        INDEX IX_ExpenseReceipts_ExpenseId (ExpenseId),
        INDEX IX_ExpenseReceipts_TenantId (TenantId),
        INDEX IX_ExpenseReceipts_Status (Status),
        INDEX IX_ExpenseReceipts_ExtractionStatus (ExtractionStatus),
        INDEX IX_ExpenseReceipts_CreatedAt (CreatedAt)
    );
    PRINT 'Created table: ExpenseReceipts';
END

-- Extraction details table for multi-line receipts
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ExpenseReceiptLineItems')
BEGIN
    CREATE TABLE dbo.ExpenseReceiptLineItems (
        LineItemId INT PRIMARY KEY IDENTITY(1,1),
        ReceiptId INT NOT NULL,
        ExpenseId INT NOT NULL,

        -- Extracted line item
        ExtractedDescription NVARCHAR(500),
        ExtractedQuantity DECIMAL(18,4),
        ExtractedUnitPrice DECIMAL(19,4),
        ExtractedLineTotal DECIMAL(19,4),

        -- User corrections
        CorrectedDescription NVARCHAR(500),
        CorrectedQuantity DECIMAL(18,4),
        CorrectedUnitPrice DECIMAL(19,4),
        CorrectedLineTotal DECIMAL(19,4),
        UserCorrected BIT DEFAULT 0,

        -- Status
        Status VARCHAR(20) NOT NULL DEFAULT 'Pending',  -- Pending, Verified, Rejected
        Notes NVARCHAR(MAX),

        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedAt DATETIME2,

        CONSTRAINT FK_ReceiptLineItems_Receipt FOREIGN KEY (ReceiptId) REFERENCES dbo.ExpenseReceipts(ReceiptId),
        CONSTRAINT FK_ReceiptLineItems_Expense FOREIGN KEY (ExpenseId) REFERENCES dbo.Expenses(ExpenseId),
        INDEX IX_ReceiptLineItems_ReceiptId (ReceiptId),
        INDEX IX_ReceiptLineItems_ExpenseId (ExpenseId)
    );
    PRINT 'Created table: ExpenseReceiptLineItems';
END

-- Audit trail for receipt extraction and corrections
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ExpenseReceiptAudit')
BEGIN
    CREATE TABLE dbo.ExpenseReceiptAudit (
        AuditId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        ExpenseId INT NOT NULL,
        ReceiptId INT,

        Action VARCHAR(50) NOT NULL,  -- Uploaded, Extracted, Corrected, Approved, Rejected
        ActionDetails NVARCHAR(MAX),
        ExtractedFields NVARCHAR(MAX),  -- JSON of what was extracted
        CorrectedFields NVARCHAR(MAX),  -- JSON of corrections made
        ConfidenceScore DECIMAL(3,2),
        ExtractionStrategy VARCHAR(50),

        AuditedBy INT NOT NULL,
        AuditedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        IpAddress VARCHAR(45),
        UserAgent NVARCHAR(500),

        CONSTRAINT FK_ReceiptAudit_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        CONSTRAINT FK_ReceiptAudit_Expense FOREIGN KEY (ExpenseId) REFERENCES dbo.Expenses(ExpenseId),
        CONSTRAINT FK_ReceiptAudit_Receipt FOREIGN KEY (ReceiptId) REFERENCES dbo.ExpenseReceipts(ReceiptId),
        CONSTRAINT FK_ReceiptAudit_User FOREIGN KEY (AuditedBy) REFERENCES dbo.Users(UserId),
        INDEX IX_ReceiptAudit_ExpenseId (ExpenseId),
        INDEX IX_ReceiptAudit_TenantId (TenantId),
        INDEX IX_ReceiptAudit_AuditedAt (AuditedAt),
        INDEX IX_ReceiptAudit_Action (Action)
    );
    PRINT 'Created table: ExpenseReceiptAudit';
END

-- Tenant receipt extraction settings
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TenantReceiptSettings')
BEGIN
    CREATE TABLE dbo.TenantReceiptSettings (
        SettingId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL UNIQUE,

        -- Receipt constraints
        MaxReceiptSizeBytes BIGINT NOT NULL DEFAULT 10485760,  -- 10 MB
        AllowedContentTypes VARCHAR(200) NOT NULL DEFAULT 'image/jpeg,image/png',

        -- AI extraction settings
        EnableAutoExtraction BIT NOT NULL DEFAULT 1,
        RequireManualReviewBelow DECIMAL(3,2) NOT NULL DEFAULT 0.80,  -- Require review if confidence < 80%
        MaxExtractionRetries INT NOT NULL DEFAULT 3,

        -- Processing settings
        CompressImages BIT NOT NULL DEFAULT 1,
        CompressionQuality INT NOT NULL DEFAULT 85,

        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedAt DATETIME2,

        CONSTRAINT FK_TenantReceiptSettings_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId)
    );
    PRINT 'Created table: TenantReceiptSettings';
END
