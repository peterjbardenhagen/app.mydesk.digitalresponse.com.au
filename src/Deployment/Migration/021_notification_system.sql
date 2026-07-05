-- Migration 021: Notification System
-- Purpose: Email, SMS, and in-app notifications for approvals and workflow events
-- Features: Templated notifications, user preferences, delivery queue, audit trail

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NotificationTemplates')
BEGIN
    CREATE TABLE dbo.NotificationTemplates (
        TemplateId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,

        -- Template identification
        Name NVARCHAR(100) NOT NULL,  -- ExpenseSubmittedForApproval, ApprovalApproved, etc.
        EventType VARCHAR(50) NOT NULL,  -- ExpenseSubmitted, ApprovalApproved, ApprovalRejected, UserInvited
        NotificationType VARCHAR(20) NOT NULL,  -- Email, SMS, InApp

        -- Email template content
        Subject NVARCHAR(255),
        Body NVARCHAR(MAX),  -- Can contain {{placeholders}}
        BodyHtml NVARCHAR(MAX),  -- HTML version

        -- SMS template (short)
        SmsTemplate NVARCHAR(160),

        -- In-app notification
        InAppTitle NVARCHAR(255),
        InAppBody NVARCHAR(500),
        InAppIcon VARCHAR(50),  -- icon name from Material Design

        -- Configuration
        IsActive BIT NOT NULL DEFAULT 1,
        IsEditable BIT NOT NULL DEFAULT 1,  -- False for system templates
        SendToInitiator BIT NOT NULL DEFAULT 1,  -- Send to person who submitted
        SendToApprovers BIT NOT NULL DEFAULT 1,  -- Send to approval chain
        SendToManagers BIT NOT NULL DEFAULT 0,  -- Send to user's manager

        -- Defaults
        DefaultEnabled BIT NOT NULL DEFAULT 1,
        DefaultDeliveryChannel VARCHAR(50) NOT NULL DEFAULT 'Email',  -- Email, SMS, InApp

        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedAt DATETIME2,
        CreatedBy INT,

        CONSTRAINT FK_Templates_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        CONSTRAINT UQ_NotificationTemplate_Name UNIQUE (TenantId, EventType, NotificationType),
        INDEX IX_NotificationTemplates_EventType (EventType),
        INDEX IX_NotificationTemplates_Active (IsActive)
    );
    PRINT 'Created table: NotificationTemplates';
END

-- User notification preferences
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NotificationSettings')
BEGIN
    CREATE TABLE dbo.NotificationSettings (
        SettingId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        UserId INT NOT NULL,

        -- Email notifications
        EnableEmailNotifications BIT NOT NULL DEFAULT 1,
        EmailOnExpenseSubmitted BIT NOT NULL DEFAULT 1,
        EmailOnApprovalRequired BIT NOT NULL DEFAULT 1,
        EmailOnApprovalApproved BIT NOT NULL DEFAULT 1,
        EmailOnApprovalRejected BIT NOT NULL DEFAULT 1,
        EmailDigestFrequency VARCHAR(20) NOT NULL DEFAULT 'Immediate',  -- Immediate, Daily, Weekly, Never

        -- SMS notifications (optional)
        EnableSmsNotifications BIT NOT NULL DEFAULT 0,
        PhoneNumber NVARCHAR(20),
        SmsOnUrgentApprovals BIT NOT NULL DEFAULT 1,  -- Only SMS for urgent (e.g., > $10k)

        -- In-app notifications
        EnableInAppNotifications BIT NOT NULL DEFAULT 1,
        InAppOnExpenseSubmitted BIT NOT NULL DEFAULT 1,
        InAppOnApprovalRequired BIT NOT NULL DEFAULT 1,
        InAppOnApprovalApproved BIT NOT NULL DEFAULT 0,
        InAppOnApprovalRejected BIT NOT NULL DEFAULT 1,

        -- Quiet hours (don't send notifications)
        QuietHoursEnabled BIT NOT NULL DEFAULT 0,
        QuietHoursStart TIME,  -- 22:00
        QuietHoursEnd TIME,    -- 06:00

        -- Preferences
        PreferCurrencyInNotifications VARCHAR(3) DEFAULT 'AUD',
        UnsubscribeToken NVARCHAR(100),
        UnsubscribedAt DATETIME2,

        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedAt DATETIME2,

        CONSTRAINT FK_NotificationSettings_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_NotificationSettings_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        CONSTRAINT UQ_NotificationSettings_User UNIQUE (TenantId, UserId),
        INDEX IX_NotificationSettings_UserId (UserId),
        INDEX IX_NotificationSettings_TenantId (TenantId)
    );
    PRINT 'Created table: NotificationSettings';
END

-- Notification log for audit trail
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NotificationLog')
BEGIN
    CREATE TABLE dbo.NotificationLog (
        NotificationId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,

        -- What triggered the notification
        EventType VARCHAR(50) NOT NULL,  -- ExpenseSubmitted, ApprovalApproved, etc.
        EventEntityType VARCHAR(50),  -- Expense, Timesheet, Approval
        EventEntityId INT,
        TriggeredByUserId INT,

        -- Who received it
        RecipientUserId INT NOT NULL,
        RecipientEmail NVARCHAR(255) NOT NULL,
        RecipientPhone NVARCHAR(20),

        -- What was sent
        TemplateId INT,
        NotificationType VARCHAR(20) NOT NULL,  -- Email, SMS, InApp
        Channel VARCHAR(50),  -- EmailQueue, SmsGateway, InApp

        -- Content
        Subject NVARCHAR(255),
        BodyPreview NVARCHAR(500),  -- First 500 chars for log
        FullContent NVARCHAR(MAX),

        -- Delivery status
        Status VARCHAR(20) NOT NULL DEFAULT 'Pending',  -- Pending, Sent, Delivered, Failed, Bounced, Unsubscribed
        SentAt DATETIME2,
        DeliveredAt DATETIME2,
        FailureReason NVARCHAR(500),
        RetryCount INT DEFAULT 0,
        LastRetryAt DATETIME2,

        -- Engagement
        OpenedAt DATETIME2,
        ClickedAt DATETIME2,
        ClickedUrl NVARCHAR(500),

        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_NotificationLog_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        CONSTRAINT FK_NotificationLog_Recipient FOREIGN KEY (RecipientUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_NotificationLog_Template FOREIGN KEY (TemplateId) REFERENCES dbo.NotificationTemplates(TemplateId),
        INDEX IX_NotificationLog_UserId (RecipientUserId),
        INDEX IX_NotificationLog_EventType (EventType),
        INDEX IX_NotificationLog_Status (Status),
        INDEX IX_NotificationLog_CreatedAt (CreatedAt),
        INDEX IX_NotificationLog_TenantId (TenantId),
        INDEX IX_NotificationLog_EventEntity (EventEntityType, EventEntityId)
    );
    PRINT 'Created table: NotificationLog';
END

-- Email queue for async processing
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EmailQueue')
BEGIN
    CREATE TABLE dbo.EmailQueue (
        QueueId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,

        -- Email details
        ToEmail NVARCHAR(255) NOT NULL,
        ToName NVARCHAR(255),
        FromEmail NVARCHAR(255) NOT NULL,
        FromName NVARCHAR(255),
        Subject NVARCHAR(255) NOT NULL,
        BodyText NVARCHAR(MAX),
        BodyHtml NVARCHAR(MAX),

        -- Attachments
        AttachmentJson NVARCHAR(MAX),  -- JSON array of { name, contentType, base64Content }

        -- Linked to notification log
        NotificationLogId INT,

        -- Processing
        Status VARCHAR(20) NOT NULL DEFAULT 'Pending',  -- Pending, Processing, Sent, Failed, Bounced
        Priority INT NOT NULL DEFAULT 5,  -- 1=highest, 10=lowest
        MaxRetries INT NOT NULL DEFAULT 3,
        RetryCount INT DEFAULT 0,
        LastAttemptAt DATETIME2,
        LastError NVARCHAR(500),
        SentAt DATETIME2,

        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ProcessedAt DATETIME2,

        CONSTRAINT FK_EmailQueue_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        CONSTRAINT FK_EmailQueue_Notification FOREIGN KEY (NotificationLogId) REFERENCES dbo.NotificationLog(NotificationId),
        INDEX IX_EmailQueue_Status (Status),
        INDEX IX_EmailQueue_Priority (Priority),
        INDEX IX_EmailQueue_CreatedAt (CreatedAt),
        INDEX IX_EmailQueue_TenantId (TenantId),
        INDEX IX_EmailQueue_RetryCount (RetryCount)
    );
    PRINT 'Created table: EmailQueue';
END

-- In-app notification storage
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'InAppNotifications')
BEGIN
    CREATE TABLE dbo.InAppNotifications (
        NotificationId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        UserId INT NOT NULL,

        -- Notification content
        Title NVARCHAR(255) NOT NULL,
        Message NVARCHAR(1000) NOT NULL,
        Icon VARCHAR(50),  -- Material Design icon name
        ActionUrl NVARCHAR(500),  -- Link to related entity
        ActionText NVARCHAR(100),  -- "View Approval", "View Expense"

        -- Notification type
        Type VARCHAR(50) NOT NULL,  -- Info, Success, Warning, Error, Action
        Category VARCHAR(50) NOT NULL,  -- Approval, Expense, Timesheet, Admin, System

        -- Related entity
        EntityType VARCHAR(50),  -- Expense, Approval, Timesheet
        EntityId INT,

        -- Status
        IsRead BIT NOT NULL DEFAULT 0,
        IsDismissed BIT NOT NULL DEFAULT 0,
        ReadAt DATETIME2,
        DismissedAt DATETIME2,

        -- Lifecycle
        ExpiresAt DATETIME2,  -- Auto-dismiss after N days
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_InAppNotifications_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_InAppNotifications_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        INDEX IX_InAppNotifications_UserId (UserId),
        INDEX IX_InAppNotifications_IsRead (IsRead),
        INDEX IX_InAppNotifications_CreatedAt (CreatedAt),
        INDEX IX_InAppNotifications_TenantId (TenantId)
    );
    PRINT 'Created table: InAppNotifications';
END

-- Notification preferences by event type
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NotificationEventPreferences')
BEGIN
    CREATE TABLE dbo.NotificationEventPreferences (
        PreferenceId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        UserId INT NOT NULL,

        -- Event configuration
        EventType VARCHAR(50) NOT NULL,  -- ExpenseSubmitted, ApprovalApproved, etc.
        DeliveryChannels VARCHAR(200) NOT NULL,  -- Comma-separated: Email,SMS,InApp
        IsEnabled BIT NOT NULL DEFAULT 1,

        -- Optional: override template for this user
        CustomTemplateId INT,

        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedAt DATETIME2,

        CONSTRAINT FK_EventPref_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_EventPref_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        CONSTRAINT FK_EventPref_Template FOREIGN KEY (CustomTemplateId) REFERENCES dbo.NotificationTemplates(TemplateId),
        CONSTRAINT UQ_NotificationEventPref UNIQUE (TenantId, UserId, EventType),
        INDEX IX_NotificationEventPref_UserId (UserId),
        INDEX IX_NotificationEventPref_EventType (EventType)
    );
    PRINT 'Created table: NotificationEventPreferences';
END

-- Store notification read/unread state for UI
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'NotificationState')
BEGIN
    CREATE TABLE dbo.NotificationState (
        StateId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        UserId INT NOT NULL,

        -- Unread counts by category
        UnreadApprovals INT DEFAULT 0,
        UnreadExpenses INT DEFAULT 0,
        UnreadAdmin INT DEFAULT 0,
        UnreadTotal INT DEFAULT 0,

        -- Last checked
        LastCheckedAt DATETIME2,
        LastNotificationReadAt DATETIME2,

        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_NotificationState_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_NotificationState_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        CONSTRAINT UQ_NotificationState_User UNIQUE (TenantId, UserId),
        INDEX IX_NotificationState_UserId (UserId)
    );
    PRINT 'Created table: NotificationState';
END

-- Create default system notification templates (pre-populated for new tenants)
IF NOT EXISTS (SELECT 1 FROM dbo.NotificationTemplates WHERE Name = 'ExpenseSubmittedForApproval')
BEGIN
    INSERT INTO dbo.NotificationTemplates
    (TenantId, Name, EventType, NotificationType, Subject, BodyHtml, InAppTitle, InAppBody, InAppIcon, SendToApprovers, DefaultEnabled)
    VALUES
    (1, 'ExpenseSubmittedForApproval', 'ExpenseSubmitted', 'Email',
     '{{SubmitterName}} submitted {{CurrencySymbol}}{{Amount}} expense for approval',
     '<p>Hi {{ApproverName}},</p><p>{{SubmitterName}} has submitted an expense claim of {{CurrencySymbol}}{{Amount}} for {{Description}}.</p><p><a href="{{ApprovalLink}}">Review Expense</a></p>',
     'Expense Pending Review', '{{SubmitterName}} submitted {{CurrencySymbol}}{{Amount}} for approval', 'receipt_long', 1, 1),

    (1, 'ApprovalApproved', 'ApprovalApproved', 'Email',
     'Your {{CurrencySymbol}}{{Amount}} expense has been approved',
     '<p>Hi {{SubmitterName}},</p><p>Your expense claim of {{CurrencySymbol}}{{Amount}} has been approved by {{ApproverName}}.</p><p>{{ApprovalComment}}</p>',
     'Expense Approved', '{{ApproverName}} approved your {{CurrencySymbol}}{{Amount}} expense', 'check_circle', 1, 1),

    (1, 'ApprovalRejected', 'ApprovalRejected', 'Email',
     'Your {{CurrencySymbol}}{{Amount}} expense requires revision',
     '<p>Hi {{SubmitterName}},</p><p>Your expense claim of {{CurrencySymbol}}{{Amount}} was returned for revision.</p><p>Reason: {{RejectionReason}}</p><p><a href="{{EditLink}}">Edit Expense</a></p>',
     'Expense Needs Revision', '{{ApproverName}} returned your {{CurrencySymbol}}{{Amount}} expense', 'edit', 1, 1);

    PRINT 'Created default notification templates';
END
