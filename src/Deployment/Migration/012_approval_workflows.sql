-- Migration 012: Create Approval Workflows tables
-- Supports manager approval workflows for Expenses and Timesheets

-- ─── Approval Workflows ──────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'ApprovalWorkflows')
BEGIN
    CREATE TABLE ApprovalWorkflows (
        WorkflowId INT PRIMARY KEY IDENTITY(1,1),
        TenantId UNIQUEIDENTIFIER NOT NULL,
        ModuleType NVARCHAR(50) NOT NULL,  -- 'Expense', 'Timesheet'
        [Name] NVARCHAR(200) NOT NULL,
        [Description] NVARCHAR(MAX),
        IsDefault BIT DEFAULT 1,
        ApprovalLevels INT DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2,
        CONSTRAINT FK_ApprovalWorkflows_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId) ON DELETE CASCADE
    );
    CREATE INDEX IX_ApprovalWorkflows_TenantId ON ApprovalWorkflows(TenantId);
    CREATE INDEX IX_ApprovalWorkflows_ModuleType ON ApprovalWorkflows(ModuleType);
    PRINT 'Created ApprovalWorkflows table';
END
ELSE
    PRINT 'ApprovalWorkflows table already exists';

-- ─── Approval Rules ───────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'ApprovalRules')
BEGIN
    CREATE TABLE ApprovalRules (
        RuleId INT PRIMARY KEY IDENTITY(1,1),
        WorkflowId INT NOT NULL,
        [Level] INT NOT NULL,
        ApproverUserId INT,
        ApproverRole NVARCHAR(100),
        ThresholdAmount DECIMAL(18,2),
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_ApprovalRules_Workflows FOREIGN KEY (WorkflowId) REFERENCES ApprovalWorkflows(WorkflowId) ON DELETE CASCADE,
        CONSTRAINT FK_ApprovalRules_Approver FOREIGN KEY (ApproverUserId) REFERENCES Users(UserId)
    );
    CREATE INDEX IX_ApprovalRules_WorkflowId ON ApprovalRules(WorkflowId);
    CREATE INDEX IX_ApprovalRules_Level ON ApprovalRules([Level]);
    PRINT 'Created ApprovalRules table';
END
ELSE
    PRINT 'ApprovalRules table already exists';

-- ─── Approval Requests ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'ApprovalRequests')
BEGIN
    CREATE TABLE ApprovalRequests (
        RequestId INT PRIMARY KEY IDENTITY(1,1),
        TenantId UNIQUEIDENTIFIER NOT NULL,
        WorkflowId INT NOT NULL,
        ModuleType NVARCHAR(50) NOT NULL,  -- 'Expense', 'Timesheet'
        ModuleId INT NOT NULL,
        CurrentLevel INT DEFAULT 1,
        [Status] NVARCHAR(50) DEFAULT 'Pending',  -- Pending, Approved, Rejected, Withdrawn
        SubmittedById INT NOT NULL,
        SubmittedAt DATETIME2 NOT NULL,
        CompletedAt DATETIME2,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2,
        CONSTRAINT FK_ApprovalRequests_Workflows FOREIGN KEY (WorkflowId) REFERENCES ApprovalWorkflows(WorkflowId),
        CONSTRAINT FK_ApprovalRequests_Submitter FOREIGN KEY (SubmittedById) REFERENCES Users(UserId),
        CONSTRAINT FK_ApprovalRequests_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId) ON DELETE CASCADE
    );
    CREATE INDEX IX_ApprovalRequests_TenantId ON ApprovalRequests(TenantId);
    CREATE INDEX IX_ApprovalRequests_Status ON ApprovalRequests([Status]);
    CREATE INDEX IX_ApprovalRequests_ModuleType ON ApprovalRequests(ModuleType);
    CREATE INDEX IX_ApprovalRequests_SubmittedById ON ApprovalRequests(SubmittedById);
    PRINT 'Created ApprovalRequests table';
END
ELSE
    PRINT 'ApprovalRequests table already exists';

-- ─── Approval Actions ───────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'ApprovalActions')
BEGIN
    CREATE TABLE ApprovalActions (
        ActionId INT PRIMARY KEY IDENTITY(1,1),
        RequestId INT NOT NULL,
        ApprovalLevel INT NOT NULL,
        ApprovedById INT,
        [Action] NVARCHAR(50) NOT NULL,  -- 'Approved', 'Rejected', 'Delegated'
        [Comments] NVARCHAR(MAX),
        DelegatedToUserId INT,
        ActionAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_ApprovalActions_Requests FOREIGN KEY (RequestId) REFERENCES ApprovalRequests(RequestId) ON DELETE CASCADE,
        CONSTRAINT FK_ApprovalActions_ApprovedBy FOREIGN KEY (ApprovedById) REFERENCES Users(UserId),
        CONSTRAINT FK_ApprovalActions_DelegatedTo FOREIGN KEY (DelegatedToUserId) REFERENCES Users(UserId)
    );
    CREATE INDEX IX_ApprovalActions_RequestId ON ApprovalActions(RequestId);
    CREATE INDEX IX_ApprovalActions_ActionAt ON ApprovalActions(ActionAt);
    PRINT 'Created ApprovalActions table';
END
ELSE
    PRINT 'ApprovalActions table already exists';

-- ─── Approval Delegations ───────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE [name] = 'ApprovalDelegations')
BEGIN
    CREATE TABLE ApprovalDelegations (
        DelegationId INT PRIMARY KEY IDENTITY(1,1),
        TenantId UNIQUEIDENTIFIER NOT NULL,
        ApproverUserId INT NOT NULL,
        DelegateUserId INT NOT NULL,
        StartDate DATE NOT NULL,
        EndDate DATE NOT NULL,
        ModuleType NVARCHAR(50),  -- NULL = all modules
        IsActive BIT DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT FK_ApprovalDelegations_Approver FOREIGN KEY (ApproverUserId) REFERENCES Users(UserId),
        CONSTRAINT FK_ApprovalDelegations_Delegate FOREIGN KEY (DelegateUserId) REFERENCES Users(UserId),
        CONSTRAINT FK_ApprovalDelegations_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId) ON DELETE CASCADE
    );
    CREATE INDEX IX_ApprovalDelegations_TenantId ON ApprovalDelegations(TenantId);
    CREATE INDEX IX_ApprovalDelegations_ApproverUserId ON ApprovalDelegations(ApproverUserId);
    CREATE INDEX IX_ApprovalDelegations_DelegateUserId ON ApprovalDelegations(DelegateUserId);
    CREATE INDEX IX_ApprovalDelegations_Dates ON ApprovalDelegations(StartDate, EndDate);
    PRINT 'Created ApprovalDelegations table';
END
ELSE
    PRINT 'ApprovalDelegations table already exists';

-- ─── Sample Default Workflows ─────────────────────────────────────────────────
-- Create default single-level approval workflows for demo tenants
DECLARE @DemoLightingId UNIQUEIDENTIFIER = (SELECT TenantId FROM Tenants WHERE [Name] = 'Demo Lighting' LIMIT 1);
DECLARE @TechlightId UNIQUEIDENTIFIER = (SELECT TenantId FROM Tenants WHERE [Name] = 'Techlight' LIMIT 1);
DECLARE @DigitalResponseId UNIQUEIDENTIFIER = (SELECT TenantId FROM Tenants WHERE [Name] = 'Digital Response' LIMIT 1);

IF @DemoLightingId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ApprovalWorkflows WHERE TenantId = @DemoLightingId AND ModuleType = 'Expense')
BEGIN
    INSERT INTO ApprovalWorkflows (TenantId, ModuleType, [Name], [Description], IsDefault, ApprovalLevels)
    VALUES (@DemoLightingId, 'Expense', 'Default Expense Approval', 'Manager approval required', 1, 1);
    INSERT INTO ApprovalWorkflows (TenantId, ModuleType, [Name], [Description], IsDefault, ApprovalLevels)
    VALUES (@DemoLightingId, 'Timesheet', 'Default Timesheet Approval', 'Manager approval required', 1, 1);
    PRINT 'Created default workflows for Demo Lighting';
END

IF @TechlightId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ApprovalWorkflows WHERE TenantId = @TechlightId AND ModuleType = 'Expense')
BEGIN
    INSERT INTO ApprovalWorkflows (TenantId, ModuleType, [Name], [Description], IsDefault, ApprovalLevels)
    VALUES (@TechlightId, 'Expense', 'Default Expense Approval', 'Manager approval required', 1, 1);
    INSERT INTO ApprovalWorkflows (TenantId, ModuleType, [Name], [Description], IsDefault, ApprovalLevels)
    VALUES (@TechlightId, 'Timesheet', 'Default Timesheet Approval', 'Manager approval required', 1, 1);
    PRINT 'Created default workflows for Techlight';
END

IF @DigitalResponseId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM ApprovalWorkflows WHERE TenantId = @DigitalResponseId AND ModuleType = 'Expense')
BEGIN
    INSERT INTO ApprovalWorkflows (TenantId, ModuleType, [Name], [Description], IsDefault, ApprovalLevels)
    VALUES (@DigitalResponseId, 'Expense', 'Default Expense Approval', 'Manager approval required', 1, 1);
    INSERT INTO ApprovalWorkflows (TenantId, ModuleType, [Name], [Description], IsDefault, ApprovalLevels)
    VALUES (@DigitalResponseId, 'Timesheet', 'Default Timesheet Approval', 'Manager approval required', 1, 1);
    PRINT 'Created default workflows for Digital Response';
END
