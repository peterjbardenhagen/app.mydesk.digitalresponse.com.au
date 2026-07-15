-- Migration 024: Phase 5 Notifications & Alerts
-- Purpose: Add tables for budget alerts, notification settings, and approval notifications
-- Features: Real-time budget threshold alerts, notification preferences, approval workflow notifications

-- ─────────────────────────────────────────────────────────────────────────────
-- BudgetAlerts: Track budget threshold alerts
-- ─────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BudgetAlerts')
BEGIN
    CREATE TABLE dbo.BudgetAlerts (
        AlertId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        DepartmentId INT NOT NULL,
        BudgetId INT NOT NULL,
        UsagePercentage INT NOT NULL,
        SpentAmount DECIMAL(12,2) NOT NULL,
        AllocatedAmount DECIMAL(12,2) NOT NULL,
        AlertType NVARCHAR(50) NOT NULL,  -- 'Threshold', 'Full', 'Overspend'
        AlertLevel NVARCHAR(50) NOT NULL,  -- 'Warning', 'Critical'
        IsAcknowledged BIT NOT NULL DEFAULT 0,
        AcknowledgedAt DATETIME2,
        AcknowledgedBy INT,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_BudgetAlerts_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        CONSTRAINT FK_BudgetAlerts_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments(DepartmentId),
        CONSTRAINT FK_BudgetAlerts_Budget FOREIGN KEY (BudgetId) REFERENCES dbo.DepartmentBudgets(BudgetId),
        CONSTRAINT FK_BudgetAlerts_AcknowledgedBy FOREIGN KEY (AcknowledgedBy) REFERENCES dbo.Users(UserId),
        INDEX IX_BudgetAlerts_TenantId (TenantId),
        INDEX IX_BudgetAlerts_DepartmentId (DepartmentId),
        INDEX IX_BudgetAlerts_BudgetId (BudgetId),
        INDEX IX_BudgetAlerts_Unacknowledged (IsAcknowledged, CreatedAt),
        INDEX IX_BudgetAlerts_CreatedAt (CreatedAt DESC)
    );
    PRINT 'Created table: BudgetAlerts';
END

-- ─────────────────────────────────────────────────────────────────────────────
-- NotificationSettings: User notification preferences
-- ─────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NotificationSettings')
BEGIN
    CREATE TABLE dbo.NotificationSettings (
        SettingId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        UserId INT NOT NULL,
        EnableEmailNotifications BIT NOT NULL DEFAULT 1,
        EnableInAppNotifications BIT NOT NULL DEFAULT 1,
        EnableSmsNotifications BIT NOT NULL DEFAULT 0,
        BudgetAlertFrequency NVARCHAR(50) NOT NULL DEFAULT 'Immediate',  -- 'Immediate', 'Daily', 'Weekly'
        ApprovalAlertFrequency NVARCHAR(50) NOT NULL DEFAULT 'Immediate',
        DigestEnabled BIT NOT NULL DEFAULT 0,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_NotificationSettings_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        CONSTRAINT FK_NotificationSettings_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT UQ_NotificationSettings_User UNIQUE (TenantId, UserId),
        INDEX IX_NotificationSettings_TenantId (TenantId),
        INDEX IX_NotificationSettings_UserId (UserId)
    );
    PRINT 'Created table: NotificationSettings';
END

-- ─────────────────────────────────────────────────────────────────────────────
-- ApprovalNotifications: Track approval workflow notifications
-- ─────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ApprovalNotifications')
BEGIN
    CREATE TABLE dbo.ApprovalNotifications (
        NotificationId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        ApprovalId INT NOT NULL,
        EventType NVARCHAR(50) NOT NULL,  -- 'Delegated', 'Escalated', 'Approved', 'Rejected', 'Reminder'
        RecipientUserId INT NOT NULL,
        TriggeredByUserId INT,
        SentAt DATETIME2,
        DeliveredAt DATETIME2,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',  -- 'Pending', 'Sent', 'Delivered', 'Failed'
        ErrorMessage NVARCHAR(MAX),
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_ApprovalNotif_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        CONSTRAINT FK_ApprovalNotif_Recipient FOREIGN KEY (RecipientUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_ApprovalNotif_TriggeredBy FOREIGN KEY (TriggeredByUserId) REFERENCES dbo.Users(UserId),
        INDEX IX_ApprovalNotif_TenantId (TenantId),
        INDEX IX_ApprovalNotif_RecipientId (RecipientUserId),
        INDEX IX_ApprovalNotif_EventType (EventType),
        INDEX IX_ApprovalNotif_Status (Status),
        INDEX IX_ApprovalNotif_CreatedAt (CreatedAt DESC)
    );
    PRINT 'Created table: ApprovalNotifications';
END

-- ─────────────────────────────────────────────────────────────────────────────
-- NotificationDigestLog: Track compiled digests for users
-- ─────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NotificationDigestLog')
BEGIN
    CREATE TABLE dbo.NotificationDigestLog (
        DigestId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        UserId INT NOT NULL,
        DigestDate DATE NOT NULL,
        BudgetAlertCount INT NOT NULL DEFAULT 0,
        ApprovalNotificationCount INT NOT NULL DEFAULT 0,
        SentAt DATETIME2,
        Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',  -- 'Pending', 'Sent', 'Failed'
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_DigestLog_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        CONSTRAINT FK_DigestLog_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT UQ_DigestLog_Daily UNIQUE (TenantId, UserId, DigestDate),
        INDEX IX_DigestLog_Status (Status),
        INDEX IX_DigestLog_DigestDate (DigestDate)
    );
    PRINT 'Created table: NotificationDigestLog';
END

-- ─────────────────────────────────────────────────────────────────────────────
-- Seed default notification settings for existing users
-- ─────────────────────────────────────────────────────────────────────────────
INSERT INTO dbo.NotificationSettings (TenantId, UserId, EnableEmailNotifications, EnableInAppNotifications, BudgetAlertFrequency, ApprovalAlertFrequency)
SELECT DISTINCT TenantId, UserId, 1, 1, 'Immediate', 'Immediate'
FROM dbo.Users
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.NotificationSettings ns
    WHERE ns.TenantId = dbo.Users.TenantId AND ns.UserId = dbo.Users.UserId
);

PRINT 'Seeded default notification settings for existing users';

-- ─────────────────────────────────────────────────────────────────────────────
-- Verify tables created
-- ─────────────────────────────────────────────────────────────────────────────
SELECT 'Phase 5 Notifications Tables Created Successfully' AS [Status];
SELECT COUNT(*) AS [BudgetAlertCount] FROM dbo.BudgetAlerts;
SELECT COUNT(*) AS [NotificationSettingsCount] FROM dbo.NotificationSettings;
SELECT COUNT(*) AS [ApprovalNotificationCount] FROM dbo.ApprovalNotifications;
