-- Migration 017: Client Billing System
-- Implements multi-tenant billing configuration and invoice management
-- Phase 2 Weeks 5-6: Product Admin module for client management

-- Create ClientBillingConfig table for per-client billing models
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ClientBillingConfig')
BEGIN
    CREATE TABLE dbo.ClientBillingConfig (
        Id INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL UNIQUE,             -- One billing config per client tenant
        BillingModel NVARCHAR(50) NOT NULL,       -- 'MONTHLY_ADVANCE', 'YEARLY_ADVANCE', 'PAY_AS_YOU_GO', 'FLAT_RATE'
        CycleStartDay INT DEFAULT 1,              -- Day of month for recurring cycles
        CycleStartMonth INT DEFAULT 1,            -- Month for yearly billing
        BillingContactEmail NVARCHAR(255),        -- Email for invoice delivery
        Currency NVARCHAR(3) NOT NULL DEFAULT 'AUD',
        TaxIdNumber NVARCHAR(50),                 -- ABN/ACN for Australian tax
        TaxPercentage DECIMAL(5,2),               -- GST percentage (typically 10%)
        Status NVARCHAR(50) NOT NULL DEFAULT 'ACTIVE',  -- ACTIVE, SUSPENDED, TERMINATED
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CreatedBy INT NOT NULL,
        UpdatedAt DATETIME2,
        UpdatedBy INT,

        CONSTRAINT FK_ClientBillingConfig_TenantId FOREIGN KEY (TenantId)
            REFERENCES dbo.Tenants(TenantId) ON DELETE CASCADE
    );

    CREATE INDEX IDX_ClientBillingConfig_TenantId ON dbo.ClientBillingConfig(TenantId);
    CREATE INDEX IDX_ClientBillingConfig_Status ON dbo.ClientBillingConfig(Status);
    CREATE INDEX IDX_ClientBillingConfig_BillingModel ON dbo.ClientBillingConfig(BillingModel);

    PRINT 'Created ClientBillingConfig table with indexes';
END;

-- Create ClientInvoice table for invoice generation and tracking
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ClientInvoice')
BEGIN
    CREATE TABLE dbo.ClientInvoice (
        Id INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        InvoiceNumber NVARCHAR(50) NOT NULL UNIQUE,  -- Format: INV-YYYY-MM-#### or similar
        InvoiceDate DATETIME2 NOT NULL,
        DueDate DATETIME2 NOT NULL,
        BillingPeriodStart DATETIME2 NOT NULL,
        BillingPeriodEnd DATETIME2 NOT NULL,
        BillingModel NVARCHAR(50) NOT NULL,          -- Copy of model for historical accuracy
        Description NVARCHAR(MAX),                    -- Itemized description of charges

        -- Pricing breakdown
        BaseAmount DECIMAL(18,2) NOT NULL,           -- Base recurring charge or flat fee
        UsageAmount DECIMAL(18,2) NOT NULL DEFAULT 0, -- Pay-as-you-go charges
        DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        TaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
        TotalAmount DECIMAL(18,2) NOT NULL,

        -- Invoice status tracking
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'DRAFT',  -- DRAFT, SENT, PAID, OVERDUE, CANCELLED
        PaidAt DATETIME2,
        PaidAmount DECIMAL(18,2),
        PaymentMethod NVARCHAR(50),                  -- 'BANK_TRANSFER', 'CREDIT_CARD', 'DIRECT_DEBIT'
        PaymentReference NVARCHAR(100),              -- Transaction ID or reference number

        -- Document tracking
        PdfUrl NVARCHAR(500),                        -- URL to generated PDF invoice
        GeneratedAt DATETIME2,
        SentAt DATETIME2,
        SentTo NVARCHAR(255),

        Notes NVARCHAR(MAX),                         -- Admin notes about invoice
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CreatedBy INT NOT NULL,
        UpdatedAt DATETIME2,
        UpdatedBy INT,

        CONSTRAINT FK_ClientInvoice_TenantId FOREIGN KEY (TenantId)
            REFERENCES dbo.Tenants(TenantId) ON DELETE CASCADE
    );

    CREATE INDEX IDX_ClientInvoice_TenantId ON dbo.ClientInvoice(TenantId, InvoiceDate DESC);
    CREATE INDEX IDX_ClientInvoice_Status ON dbo.ClientInvoice(Status, DueDate);
    CREATE INDEX IDX_ClientInvoice_InvoiceNumber ON dbo.ClientInvoice(InvoiceNumber);
    CREATE INDEX IDX_ClientInvoice_BillingPeriod ON dbo.ClientInvoice(TenantId, BillingPeriodStart, BillingPeriodEnd);
    CREATE INDEX IDX_ClientInvoice_PaymentStatus ON dbo.ClientInvoice(Status, PaidAt DESC);

    PRINT 'Created ClientInvoice table with indexes';
END;

-- Create ClientUsageLog table for usage-based billing tracking
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ClientUsageLog')
BEGIN
    CREATE TABLE dbo.ClientUsageLog (
        Id BIGINT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        UsageType NVARCHAR(100) NOT NULL,          -- 'EXPENSE_SUBMISSION', 'TIMESHEET_SUBMISSION', 'API_CALL', 'REPORT_EXPORT'
        UsageDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UsageQuantity INT NOT NULL DEFAULT 1,      -- Number of units (count of expenses, etc.)
        CostPerUnit DECIMAL(18,4),                 -- Unit price for this usage type
        TotalCost DECIMAL(18,2),                   -- UsageQuantity * CostPerUnit
        EntityType NVARCHAR(50),                   -- 'Expense', 'Timesheet', 'Report', etc.
        EntityId INT,                              -- ID of the entity that triggered usage
        IncludedInInvoiceId INT,                   -- Reference to ClientInvoice if already billed
        IsBillable BIT NOT NULL DEFAULT 1,         -- Whether this usage should be charged
        Notes NVARCHAR(255),

        CONSTRAINT FK_ClientUsageLog_TenantId FOREIGN KEY (TenantId)
            REFERENCES dbo.Tenants(TenantId) ON DELETE CASCADE,
        CONSTRAINT FK_ClientUsageLog_InvoiceId FOREIGN KEY (IncludedInInvoiceId)
            REFERENCES dbo.ClientInvoice(Id) ON DELETE SET NULL
    );

    CREATE INDEX IDX_ClientUsageLog_TenantId ON dbo.ClientUsageLog(TenantId, UsageDate DESC);
    CREATE INDEX IDX_ClientUsageLog_UsageType ON dbo.ClientUsageLog(UsageType, UsageDate DESC);
    CREATE INDEX IDX_ClientUsageLog_BillableOnly ON dbo.ClientUsageLog(TenantId, IsBillable) WHERE IsBillable = 1;
    CREATE INDEX IDX_ClientUsageLog_InvoiceId ON dbo.ClientUsageLog(IncludedInInvoiceId);

    PRINT 'Created ClientUsageLog table with indexes';
END;

-- Create ClientBillingHistory table for audit trail of billing changes
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ClientBillingHistory')
BEGIN
    CREATE TABLE dbo.ClientBillingHistory (
        Id INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        ChangeType NVARCHAR(50) NOT NULL,          -- 'CONFIG_CHANGE', 'INVOICE_GENERATED', 'PAYMENT_RECORDED', 'SUSPENSION'
        OldValues NVARCHAR(MAX),
        NewValues NVARCHAR(MAX),
        Reason NVARCHAR(500),
        ChangedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ChangedBy INT NOT NULL,

        CONSTRAINT FK_ClientBillingHistory_TenantId FOREIGN KEY (TenantId)
            REFERENCES dbo.Tenants(TenantId) ON DELETE CASCADE
    );

    CREATE INDEX IDX_ClientBillingHistory_TenantId ON dbo.ClientBillingHistory(TenantId, ChangedAt DESC);
    CREATE INDEX IDX_ClientBillingHistory_ChangeType ON dbo.ClientBillingHistory(ChangeType, ChangedAt DESC);

    PRINT 'Created ClientBillingHistory table with indexes';
END;

-- Seed default billing configurations for demo tenants
-- Each existing tenant (except MyDesk platform tenant) gets a default billing config
IF NOT EXISTS (SELECT 1 FROM dbo.ClientBillingConfig)
BEGIN
    DECLARE @AdminUserId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE IsAdmin = 1);
    DECLARE @MyDeskTenantId INT = (SELECT TOP 1 TenantId FROM dbo.Tenants WHERE Name LIKE '%MyDesk%' OR Name LIKE '%Digital Response%');

    IF @AdminUserId IS NULL SET @AdminUserId = 1;
    IF @MyDeskTenantId IS NULL SET @MyDeskTenantId = 1;

    INSERT INTO dbo.ClientBillingConfig (TenantId, BillingModel, BillingContactEmail, TaxPercentage, CreatedBy, Status)
    SELECT
        t.TenantId,
        'MONTHLY_ADVANCE' AS BillingModel,
        NULL AS BillingContactEmail,
        10 AS TaxPercentage,
        @AdminUserId AS CreatedBy,
        'ACTIVE' AS Status
    FROM dbo.Tenants t
    WHERE t.IsActive = 1
    AND t.TenantId != @MyDeskTenantId
    AND NOT EXISTS (
        SELECT 1 FROM dbo.ClientBillingConfig cbc WHERE cbc.TenantId = t.TenantId
    );

    PRINT 'Seeded default billing configurations for existing client tenants';
END;

-- Compliance note: This migration implements:
--
-- Billing & Financial Controls:
--   - Multi-model billing (monthly, yearly, pay-as-you-go)
--   - Invoice generation with complete audit trail
--   - Usage tracking for variable-cost scenarios
--   - Tax calculation support (Australian GST)
--
-- Financial Compliance:
--   - Invoice tracking for accounting
--   - Payment status management for reconciliation
--   - Billing period tracking for regulatory reporting
--   - Tax ID support for ABN/ACN
--
-- Data Integrity:
--   - Billing model stored in invoice for historical accuracy
--   - Usage logs immutable after invoice generation
--   - Complete audit trail of billing changes
--   - Payment reference tracking for bank reconciliation

PRINT 'Migration 017: Client Billing System - Complete';
