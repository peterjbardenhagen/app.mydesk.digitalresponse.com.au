-- Add marketing tables for EmailCampaigns and MarketingStrategy
-- Created: 2026-04-30

-- EmailCampaigns table
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'EmailCampaigns')
BEGIN
    CREATE TABLE EmailCampaigns (
        CampaignId INT IDENTITY(1,1) PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        Subject NVARCHAR(500),
        Audience NVARCHAR(100),
        Status NVARCHAR(50) DEFAULT 'Draft', -- Draft, Scheduled, Sent, Cancelled
        SentCount INT DEFAULT 0,
        RecipientCount INT DEFAULT 0,
        OpenRate DECIMAL(5,2) DEFAULT 0,
        ClickRate DECIMAL(5,2) DEFAULT 0,
        SentAt DATETIME NULL,
        ScheduledAt DATETIME NULL,
        CreatedAt DATETIME DEFAULT GETDATE(),
        UpdatedAt DATETIME DEFAULT GETDATE(),
        CreatedBy NVARCHAR(100),
        HtmlContent NVARCHAR(MAX),
        TextContent NVARCHAR(MAX)
    );
    CREATE INDEX IX_EmailCampaigns_Status ON EmailCampaigns(Status);
    CREATE INDEX IX_EmailCampaigns_SentAt ON EmailCampaigns(SentAt);
    PRINT 'EmailCampaigns table created.';
END
ELSE
    PRINT 'EmailCampaigns table already exists.';

-- MarketingStrategy table (single row configuration)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'MarketingStrategy')
BEGIN
    CREATE TABLE MarketingStrategy (
        StrategyId INT IDENTITY(1,1) PRIMARY KEY,
        IcpIndustries NVARCHAR(500),
        IcpCompanySize NVARCHAR(200),
        IcpPainPoints NVARCHAR(500),
        IcpBuyingTriggers NVARCHAR(500),
        ValueProposition NVARCHAR(500),
        Differentiators NVARCHAR(500),
        PositioningStatement NVARCHAR(1000),
        Q1Initiatives NVARCHAR(1000),
        Q2Initiatives NVARCHAR(1000),
        Q3Initiatives NVARCHAR(1000),
        Q4Initiatives NVARCHAR(1000),
        KpiLeadTarget INT DEFAULT 0,
        KpiConversionRate DECIMAL(5,2) DEFAULT 0,
        KpiCacTarget DECIMAL(10,2) DEFAULT 0,
        KpiNpsTarget INT DEFAULT 0,
        UpdatedAt DATETIME DEFAULT GETDATE(),
        UpdatedBy NVARCHAR(100)
    );
    PRINT 'MarketingStrategy table created.';
END
ELSE
    PRINT 'MarketingStrategy table already exists.';

-- CalendarEvents table (for user-created events)
IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'CalendarEvents')
BEGIN
    CREATE TABLE CalendarEvents (
        EventId INT IDENTITY(1,1) PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        Description NVARCHAR(1000),
        EventDate DATE NOT NULL,
        EventType NVARCHAR(50), -- Invoice, Quote, PO, Follow-up, Custom
        CreatedAt DATETIME DEFAULT GETDATE(),
        CreatedBy NVARCHAR(100),
        Link NVARCHAR(500), -- Link to related entity
        IsActive BIT DEFAULT 1
    );
    CREATE INDEX IX_CalendarEvents_EventDate ON CalendarEvents(EventDate);
    CREATE INDEX IX_CalendarEvents_EventType ON CalendarEvents(EventType);
    PRINT 'CalendarEvents table created.';
END
ELSE
    PRINT 'CalendarEvents table already exists.';

PRINT 'Marketing tables migration completed.';
