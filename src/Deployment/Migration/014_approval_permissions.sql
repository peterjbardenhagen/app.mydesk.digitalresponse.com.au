-- Migration 014: Approval Permissions System
-- Implements fine-grained approval authority with threshold-based routing
-- Phase 1 Week 2: Critical foundation for compliance-ready approval workflows

-- Create ApprovalPermissions table for role-based approval authority
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ApprovalPermissions')
BEGIN
    CREATE TABLE dbo.ApprovalPermissions (
        Id INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        RoleId NVARCHAR(50),                     -- Role (Manager, Director, Administrator) or null for specific user
        UserId INT,                              -- Specific user if RoleId is null
        ModuleType NVARCHAR(50) NOT NULL,        -- Module: 'Expense', 'Timesheet', 'PurchaseOrder', 'All'
        ApprovalLevel INT NOT NULL,              -- 1, 2, 3 etc. (which level this permission applies to)
        MinThreshold DECIMAL(18,2),              -- Minimum amount required for this approval level (null = all amounts)
        MaxThreshold DECIMAL(18,2),              -- Maximum amount for this approval level (null = unlimited)
        CanDelegate BIT NOT NULL DEFAULT 1,      -- Whether this approver can delegate authority
        CanReject BIT NOT NULL DEFAULT 1,        -- Whether this approver can reject requests
        CanComment BIT NOT NULL DEFAULT 1,       -- Whether this approver can add comments
        IsActive BIT NOT NULL DEFAULT 1,         -- Soft delete support
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CreatedBy INT NOT NULL,
        UpdatedAt DATETIME2,
        UpdatedBy INT,

        CONSTRAINT FK_ApprovalPermissions_TenantId FOREIGN KEY (TenantId)
            REFERENCES dbo.Tenants(TenantId) ON DELETE CASCADE,
        CONSTRAINT FK_ApprovalPermissions_UserId FOREIGN KEY (UserId)
            REFERENCES dbo.Users(UserId) ON DELETE CASCADE,
        CONSTRAINT CK_ApprovalPermissions_RoleOrUser
            CHECK ((RoleId IS NOT NULL AND UserId IS NULL) OR (RoleId IS NULL AND UserId IS NOT NULL))
    );

    -- Create indexes for performance
    CREATE INDEX IDX_ApprovalPermissions_TenantId ON dbo.ApprovalPermissions(TenantId, IsActive);
    CREATE INDEX IDX_ApprovalPermissions_RoleId ON dbo.ApprovalPermissions(TenantId, RoleId, ModuleType, ApprovalLevel)
        WHERE IsActive = 1 AND RoleId IS NOT NULL;
    CREATE INDEX IDX_ApprovalPermissions_UserId ON dbo.ApprovalPermissions(UserId, TenantId, ModuleType)
        WHERE IsActive = 1 AND UserId IS NOT NULL;
    CREATE INDEX IDX_ApprovalPermissions_Threshold ON dbo.ApprovalPermissions(TenantId, ModuleType, MinThreshold, MaxThreshold)
        WHERE IsActive = 1;

    PRINT 'Created ApprovalPermissions table with indexes';
END;

-- Create ApprovalPermissionAudit table for compliance tracking
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ApprovalPermissionAudit')
BEGIN
    CREATE TABLE dbo.ApprovalPermissionAudit (
        Id INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        PermissionId INT,                        -- Reference to ApprovalPermissions (may be null if permission was deleted)
        UserId INT,                              -- User whose permission changed
        ChangeType NVARCHAR(50) NOT NULL,        -- 'CREATE', 'UPDATE', 'DELETE', 'REVOKE'
        OldValues NVARCHAR(MAX),                 -- JSON of old values before change
        NewValues NVARCHAR(MAX),                 -- JSON of new values after change
        Reason NVARCHAR(500),                    -- Why the change was made
        ChangedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ChangedBy INT NOT NULL,                  -- Admin user who made the change
        IpAddress NVARCHAR(45),
        IsActive BIT NOT NULL DEFAULT 1,

        CONSTRAINT FK_ApprovalPermissionAudit_TenantId FOREIGN KEY (TenantId)
            REFERENCES dbo.Tenants(TenantId) ON DELETE CASCADE
    );

    CREATE INDEX IDX_ApprovalPermissionAudit_TenantId ON dbo.ApprovalPermissionAudit(TenantId, ChangedAt DESC);
    CREATE INDEX IDX_ApprovalPermissionAudit_UserId ON dbo.ApprovalPermissionAudit(UserId, TenantId);
    CREATE INDEX IDX_ApprovalPermissionAudit_ChangeType ON dbo.ApprovalPermissionAudit(ChangeType, ChangedAt DESC);

    PRINT 'Created ApprovalPermissionAudit table with indexes';
END;

-- Seed default permissions for demo tenants
-- Creates standard 2-level approval workflow for each tenant
IF NOT EXISTS (SELECT 1 FROM dbo.ApprovalPermissions WHERE TenantId = (SELECT TOP 1 TenantId FROM dbo.Tenants))
BEGIN
    DECLARE @AdminUserId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE IsAdmin = 1);
    IF @AdminUserId IS NULL SET @AdminUserId = 1;

    -- For each tenant, create default approval permissions
    INSERT INTO dbo.ApprovalPermissions (TenantId, RoleId, ModuleType, ApprovalLevel, MinThreshold, MaxThreshold, CanDelegate, CanReject, CanComment, CreatedBy, IsActive)
    SELECT
        t.TenantId,
        'Manager' AS RoleId,
        'Expense' AS ModuleType,
        1 AS ApprovalLevel,
        0.00 AS MinThreshold,
        5000.00 AS MaxThreshold,
        1, 1, 1,
        @AdminUserId,
        1
    FROM dbo.Tenants t
    WHERE t.IsActive = 1
    AND NOT EXISTS (
        SELECT 1 FROM dbo.ApprovalPermissions ap
        WHERE ap.TenantId = t.TenantId AND ap.ModuleType = 'Expense' AND ap.ApprovalLevel = 1
    );

    INSERT INTO dbo.ApprovalPermissions (TenantId, RoleId, ModuleType, ApprovalLevel, MinThreshold, MaxThreshold, CanDelegate, CanReject, CanComment, CreatedBy, IsActive)
    SELECT
        t.TenantId,
        'Director' AS RoleId,
        'Expense' AS ModuleType,
        2 AS ApprovalLevel,
        5000.00 AS MinThreshold,
        NULL AS MaxThreshold,
        1, 1, 1,
        @AdminUserId,
        1
    FROM dbo.Tenants t
    WHERE t.IsActive = 1
    AND NOT EXISTS (
        SELECT 1 FROM dbo.ApprovalPermissions ap
        WHERE ap.TenantId = t.TenantId AND ap.ModuleType = 'Expense' AND ap.ApprovalLevel = 2
    );

    -- Similar for Timesheet approvals (no threshold, just level)
    INSERT INTO dbo.ApprovalPermissions (TenantId, RoleId, ModuleType, ApprovalLevel, CanDelegate, CanReject, CanComment, CreatedBy, IsActive)
    SELECT
        t.TenantId,
        'Manager' AS RoleId,
        'Timesheet' AS ModuleType,
        1 AS ApprovalLevel,
        1, 1, 1,
        @AdminUserId,
        1
    FROM dbo.Tenants t
    WHERE t.IsActive = 1
    AND NOT EXISTS (
        SELECT 1 FROM dbo.ApprovalPermissions ap
        WHERE ap.TenantId = t.TenantId AND ap.ModuleType = 'Timesheet' AND ap.ApprovalLevel = 1
    );

    PRINT 'Seeded default approval permissions for existing tenants';
END;

-- Compliance note: This migration implements fine-grained approval authority required for:
-- - SOC 2 Type II: Segregation of duties and approval controls
-- - Sarbanes-Oxley: Approval workflows with audit trail
-- - ISO 27001: Access control and authorization
-- - Australian Privacy Act: Data protection through proper authorization

PRINT 'Migration 014: Approval Permissions System - Complete';
