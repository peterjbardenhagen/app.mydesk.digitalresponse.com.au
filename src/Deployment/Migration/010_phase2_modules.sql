-- Migration 010: Create Phase 2 module tables
-- Cash Flow, Goals/KPIs, Projects, Contacts

-- ─── Cash Flow Forecast ──────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'CashFlowForecasts')
BEGIN
    CREATE TABLE CashFlowForecasts (
        ForecastId INT PRIMARY KEY IDENTITY(1,1),
        TenantId UNIQUEIDENTIFIER NOT NULL,
        ForecastDate DATE NOT NULL,
        WeekNumber INT,
        ProjectedIncoming DECIMAL(18,2) NOT NULL DEFAULT 0,
        ProjectedOutgoing DECIMAL(18,2) NOT NULL DEFAULT 0,
        CashPosition DECIMAL(18,2),
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2,
        CONSTRAINT FK_CashFlowForecasts_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId) ON DELETE CASCADE
    );
    CREATE INDEX IX_CashFlowForecasts_TenantId ON CashFlowForecasts(TenantId);
    CREATE INDEX IX_CashFlowForecasts_ForecastDate ON CashFlowForecasts(ForecastDate);
    PRINT 'Created CashFlowForecasts table';
END

-- ─── Business Goals & KPIs ──────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'BusinessGoals')
BEGIN
    CREATE TABLE BusinessGoals (
        GoalId INT PRIMARY KEY IDENTITY(1,1),
        Reference NVARCHAR(50) NOT NULL,
        Title NVARCHAR(500) NOT NULL,
        [Description] NVARCHAR(MAX),
        TargetValue DECIMAL(18,2),
        CurrentValue DECIMAL(18,2) DEFAULT 0,
        UnitOfMeasure NVARCHAR(50),
        [Period] NVARCHAR(50),  -- Quarterly, Annual
        [Status] NVARCHAR(50) DEFAULT 'Active',
        TenantId UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2,
        CONSTRAINT FK_BusinessGoals_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId) ON DELETE CASCADE
    );
    CREATE INDEX IX_BusinessGoals_TenantId ON BusinessGoals(TenantId);
    CREATE INDEX IX_BusinessGoals_Period ON BusinessGoals([Period]);
    PRINT 'Created BusinessGoals table';
END

-- ─── Projects Extended ──────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Object_ID = Object_ID('Projects') AND Name = 'Milestone')
BEGIN
    ALTER TABLE Projects ADD
        Milestone NVARCHAR(MAX),
        TeamMembers NVARCHAR(MAX),
        Health NVARCHAR(50),
        [Percent] INT DEFAULT 0;
    PRINT 'Extended Projects table with milestone and health fields';
END
ELSE
    PRINT 'Projects table already has extended fields';

-- ─── Contacts ────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'Contacts')
BEGIN
    CREATE TABLE Contacts (
        ContactId INT PRIMARY KEY IDENTITY(1,1),
        Reference NVARCHAR(50) NOT NULL,
        FirstName NVARCHAR(200),
        LastName NVARCHAR(200),
        Email NVARCHAR(200),
        Phone NVARCHAR(20),
        Mobile NVARCHAR(20),
        [Address] NVARCHAR(500),
        [Role] NVARCHAR(200),
        CompanyId INT,
        TenantId UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2,
        CONSTRAINT FK_Contacts_Companies FOREIGN KEY (CompanyId) REFERENCES Companies(CompanyId),
        CONSTRAINT FK_Contacts_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId) ON DELETE CASCADE
    );
    CREATE INDEX IX_Contacts_TenantId ON Contacts(TenantId);
    CREATE INDEX IX_Contacts_CompanyId ON Contacts(CompanyId);
    CREATE INDEX IX_Contacts_Email ON Contacts(Email);
    PRINT 'Created Contacts table with indexes';
END
ELSE
    PRINT 'Contacts table already exists';
