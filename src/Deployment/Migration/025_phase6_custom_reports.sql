-- Migration 025: Phase 6 - Custom Report Templates
-- Purpose: Allow users to create and save custom report templates with their preferred metrics and settings
-- Features: Template CRUD, user-specific templates, default template selection

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'CustomReportTemplates')
BEGIN
    CREATE TABLE dbo.CustomReportTemplates (
        TemplateId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        UserId INT NOT NULL,
        Name NVARCHAR(255) NOT NULL,
        DashboardType NVARCHAR(50) NOT NULL,  -- executive, manager, employee

        -- Report content settings
        IncludeSummary BIT NOT NULL DEFAULT 1,
        IncludeCharts BIT NOT NULL DEFAULT 1,
        IncludeDetailed BIT NOT NULL DEFAULT 1,
        IncludeAnalysis BIT NOT NULL DEFAULT 0,

        -- Template management
        IsDefault BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_CustomReportTemplates_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        CONSTRAINT FK_CustomReportTemplates_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        INDEX IX_CustomReportTemplates_TenantId (TenantId),
        INDEX IX_CustomReportTemplates_UserId (UserId),
        INDEX IX_CustomReportTemplates_Default (TenantId, UserId, IsDefault)
    );
    PRINT 'Created table: CustomReportTemplates';
END
