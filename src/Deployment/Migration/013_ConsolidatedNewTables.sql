-- Migration 013: Consolidated New Tables
-- Date: 2026-05-02
-- Purpose: Create all remaining tables referenced in code but not covered by migrations 001-012
-- Covers: PurchaseOrderInvoices, SavedReports, ApplicationLogs, AiInteractionAudit, EntityAudit, Marketing, Calendar, EmailCampaigns, MarketingStrategy

-- ============================================================================
-- PurchaseOrderInvoices - Links POs to their invoice details
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PurchaseOrderInvoices')
BEGIN
    CREATE TABLE PurchaseOrderInvoices (
        POInvoiceId INT IDENTITY(1,1) PRIMARY KEY,
        PurchaseOrderId INT NOT NULL,
        InvoiceNumber NVARCHAR(50) NULL,
        InvoiceDate DATE NULL,
        InvoiceAmount DECIMAL(18,2) NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
        Notes NVARCHAR(500) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_PurchaseOrderInvoices_POId ON PurchaseOrderInvoices(PurchaseOrderId);
    PRINT 'PurchaseOrderInvoices table created';
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrderInvoices') AND name = 'Notes')
        ALTER TABLE PurchaseOrderInvoices ADD Notes NVARCHAR(500) NULL;
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('PurchaseOrderInvoices') AND name = 'UpdatedAt')
        ALTER TABLE PurchaseOrderInvoices ADD UpdatedAt DATETIME NOT NULL DEFAULT GETDATE();
    
    PRINT 'PurchaseOrderInvoices table verified';
END

-- ============================================================================
-- SavedReports - User-saved report configurations
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SavedReports')
BEGIN
    CREATE TABLE SavedReports (
        ReportId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(500) NULL,
        ReportType NVARCHAR(50) NOT NULL,
        Configuration NVARCHAR(MAX) NULL,
        CreatedBy INT NULL,
        CreatedByName NVARCHAR(100) NULL,
        IsPublic BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_SavedReports_CreatedBy ON SavedReports(CreatedBy);
    CREATE INDEX IX_SavedReports_IsPublic ON SavedReports(IsPublic);
    PRINT 'SavedReports table created';
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SavedReports') AND name = 'Description')
        ALTER TABLE SavedReports ADD Description NVARCHAR(500) NULL;
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('SavedReports') AND name = 'UpdatedBy')
        ALTER TABLE SavedReports ADD UpdatedAt DATETIME NOT NULL DEFAULT GETDATE();
    
    PRINT 'SavedReports table verified';
END

-- ============================================================================
-- ApplicationLogs - Database-stored application logs (alternative to file logs)
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ApplicationLogs')
BEGIN
    CREATE TABLE ApplicationLogs (
        LogId INT IDENTITY(1,1) PRIMARY KEY,
        LogDate DATETIME NOT NULL DEFAULT GETDATE(),
        Level NVARCHAR(20) NOT NULL,
        Message NVARCHAR(MAX) NOT NULL,
        SourceContext NVARCHAR(255) NULL,
        Exception NVARCHAR(MAX) NULL,
        MachineName NVARCHAR(100) NULL,
        ThreadId INT NULL,
        RequestPath NVARCHAR(500) NULL,
        RequestMethod NVARCHAR(10) NULL,
        StatusCode INT NULL,
        ElapsedMs DECIMAL(10,2) NULL,
        UserName NVARCHAR(100) NULL,
        RemoteIP NVARCHAR(45) NULL,
        UserAgent NVARCHAR(500) NULL
    );
    CREATE INDEX IX_ApplicationLogs_LogDate ON ApplicationLogs(LogDate DESC);
    CREATE INDEX IX_ApplicationLogs_Level ON ApplicationLogs(Level);
    PRINT 'ApplicationLogs table created';
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ApplicationLogs') AND name = 'RequestPath')
        ALTER TABLE ApplicationLogs ADD RequestPath NVARCHAR(500) NULL;
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ApplicationLogs') AND name = 'RequestMethod')
        ALTER TABLE ApplicationLogs ADD RequestMethod NVARCHAR(10) NULL;
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ApplicationLogs') AND name = 'StatusCode')
        ALTER TABLE ApplicationLogs ADD StatusCode INT NULL;
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ApplicationLogs') AND name = 'ElapsedMs')
        ALTER TABLE ApplicationLogs ADD ElapsedMs DECIMAL(10,2) NULL;
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ApplicationLogs') AND name = 'UserName')
        ALTER TABLE ApplicationLogs ADD UserName NVARCHAR(100) NULL;
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ApplicationLogs') AND name = 'RemoteIP')
        ALTER TABLE ApplicationLogs ADD RemoteIP NVARCHAR(45) NULL;
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('ApplicationLogs') AND name = 'UserAgent')
        ALTER TABLE ApplicationLogs ADD UserAgent NVARCHAR(500) NULL;
    
    PRINT 'ApplicationLogs table verified';
END

-- ============================================================================
-- AiInteractionAudit - Audit trail for AI interactions
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AiInteractionAudit')
BEGIN
    CREATE TABLE AiInteractionAudit (
        AuditId INT IDENTITY(1,1) PRIMARY KEY,
        InteractionDate DATETIME NOT NULL DEFAULT GETDATE(),
        UserId INT NULL,
        UserName NVARCHAR(100) NULL,
        Feature NVARCHAR(100) NOT NULL,
        InputSummary NVARCHAR(MAX) NULL,
        OutputSummary NVARCHAR(MAX) NULL,
        TokensUsed INT NULL,
        Cost DECIMAL(10,4) NULL,
        DurationMs INT NULL,
        Model NVARCHAR(100) NULL,
        Success BIT NOT NULL DEFAULT 1,
        ErrorMessage NVARCHAR(500) NULL
    );
    CREATE INDEX IX_AiInteractionAudit_InteractionDate ON AiInteractionAudit(InteractionDate DESC);
    CREATE INDEX IX_AiInteractionAudit_UserId ON AiInteractionAudit(UserId);
    CREATE INDEX IX_AiInteractionAudit_Feature ON AiInteractionAudit(Feature);
    PRINT 'AiInteractionAudit table created';
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AiInteractionAudit') AND name = 'Cost')
        ALTER TABLE AiInteractionAudit ADD Cost DECIMAL(10,4) NULL;
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AiInteractionAudit') AND name = 'DurationMs')
        ALTER TABLE AiInteractionAudit ADD DurationMs INT NULL;
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('AiInteractionAudit') AND name = 'Model')
        ALTER TABLE AiInteractionAudit ADD Model NVARCHAR(100) NULL;
    
    PRINT 'AiInteractionAudit table verified';
END

-- ============================================================================
-- EntityAudit - General entity change tracking
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EntityAudit')
BEGIN
    CREATE TABLE EntityAudit (
        AuditId INT IDENTITY(1,1) PRIMARY KEY,
        EntityName NVARCHAR(100) NOT NULL,
        EntityId INT NOT NULL,
        Action NVARCHAR(50) NOT NULL, -- Created, Updated, Deleted
        OldValues NVARCHAR(MAX) NULL,
        NewValues NVARCHAR(MAX) NULL,
        ChangedBy INT NULL,
        ChangedByName NVARCHAR(100) NULL,
        ChangedAt DATETIME NOT NULL DEFAULT GETDATE(),
        IpAddress NVARCHAR(45) NULL
    );
    CREATE INDEX IX_EntityAudit_EntityNameId ON EntityAudit(EntityName, EntityId);
    CREATE INDEX IX_EntityAudit_ChangedAt ON EntityAudit(ChangedAt DESC);
    CREATE INDEX IX_EntityAudit_ChangedBy ON EntityAudit(ChangedBy);
    PRINT 'EntityAudit table created';
END
ELSE
BEGIN
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('EntityAudit') AND name = 'ChangedByName')
        ALTER TABLE EntityAudit ADD ChangedByName NVARCHAR(100) NULL;
    
    IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('EntityAudit') AND name = 'IpAddress')
        ALTER TABLE EntityAudit ADD IpAddress NVARCHAR(45) NULL;
    
    PRINT 'EntityAudit table verified';
END

-- ============================================================================
-- EmailCampaigns - Marketing email campaign tracking
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'EmailCampaigns')
BEGIN
    CREATE TABLE EmailCampaigns (
        CampaignId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Subject NVARCHAR(300) NULL,
        Body NVARCHAR(MAX) NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Draft', -- Draft, Scheduled, Sending, Sent, Failed
        SentDate DATETIME NULL,
        TotalSent INT NOT NULL DEFAULT 0,
        TotalOpened INT NOT NULL DEFAULT 0,
        TotalClicked INT NOT NULL DEFAULT 0,
        TotalBounced INT NOT NULL DEFAULT 0,
        CreatedBy INT NULL,
        CreatedByName NVARCHAR(100) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_EmailCampaigns_Status ON EmailCampaigns(Status);
    CREATE INDEX IX_EmailCampaigns_CreatedAt ON EmailCampaigns(CreatedAt DESC);
    PRINT 'EmailCampaigns table created';
END

-- ============================================================================
-- MarketingStrategy - Marketing strategy and planning
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'MarketingStrategy')
BEGIN
    CREATE TABLE MarketingStrategy (
        StrategyId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000) NULL,
        Objectives NVARCHAR(MAX) NULL,
        TargetAudience NVARCHAR(500) NULL,
        Budget DECIMAL(18,2) NULL,
        StartDate DATE NULL,
        EndDate DATE NULL,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Draft',
        CreatedBy INT NULL,
        CreatedByName NVARCHAR(100) NULL,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_MarketingStrategy_Status ON MarketingStrategy(Status);
    PRINT 'MarketingStrategy table created';
END

-- ============================================================================
-- CalendarEvents - Calendar events for invoices, quotes, etc.
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CalendarEvents')
BEGIN
    CREATE TABLE CalendarEvents (
        EventId INT IDENTITY(1,1) PRIMARY KEY,
        Title NVARCHAR(300) NOT NULL,
        Description NVARCHAR(1000) NULL,
        EventType NVARCHAR(50) NOT NULL, -- Invoice, Quote, PO, Meeting, Reminder
        EventDate DATE NOT NULL,
        DueDate DATE NULL,
        RelatedEntityId INT NULL,
        RelatedEntityType NVARCHAR(50) NULL,
        UserId INT NULL,
        UserName NVARCHAR(100) NULL,
        IsCompleted BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_CalendarEvents_EventDate ON CalendarEvents(EventDate);
    CREATE INDEX IX_CalendarEvents_UserId ON CalendarEvents(UserId);
    CREATE INDEX IX_CalendarEvents_EventType ON CalendarEvents(EventType);
    PRINT 'CalendarEvents table created';
END

PRINT 'Migration 013 complete - All consolidated new tables created/verified';
