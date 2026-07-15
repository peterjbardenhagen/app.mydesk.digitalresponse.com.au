-- Migration 026: Phase 6 - Report Distribution Lists
-- Purpose: Allow users to create and manage distribution lists for automated report delivery
-- Features: Distribution list CRUD, recipient management, email targeting

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ReportDistributionLists')
BEGIN
    CREATE TABLE dbo.ReportDistributionLists (
        ListId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        CreatedBy INT NOT NULL,
        Name NVARCHAR(255) NOT NULL,
        Description NVARCHAR(500),
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_ReportDistributionLists_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        CONSTRAINT FK_ReportDistributionLists_Creator FOREIGN KEY (CreatedBy) REFERENCES dbo.Users(UserId),
        INDEX IX_ReportDistributionLists_TenantId (TenantId),
        INDEX IX_ReportDistributionLists_Active (TenantId, IsActive)
    );
    PRINT 'Created table: ReportDistributionLists';
END

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ReportDistributionRecipients')
BEGIN
    CREATE TABLE dbo.ReportDistributionRecipients (
        RecipientId INT PRIMARY KEY IDENTITY(1,1),
        ListId INT NOT NULL,
        Email NVARCHAR(255) NOT NULL,
        Name NVARCHAR(255),
        IsActive BIT NOT NULL DEFAULT 1,

        CONSTRAINT FK_ReportDistributionRecipients_List FOREIGN KEY (ListId) REFERENCES dbo.ReportDistributionLists(ListId),
        INDEX IX_ReportDistributionRecipients_ListId (ListId),
        INDEX IX_ReportDistributionRecipients_Active (ListId, IsActive)
    );
    PRINT 'Created table: ReportDistributionRecipients';
END
