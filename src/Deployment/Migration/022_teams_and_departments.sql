-- Migration 022: Teams and Department Management System
-- Purpose: Enable multi-level department hierarchies, team-based approval workflows, and budget tracking
-- Features: Departments, Teams, TeamMembers, ApprovalDelegation, DepartmentBudgets

-- ─────────────────────────────────────────────────────────────────────────────
-- DEPARTMENTS TABLE: Multi-level organizational structure
-- ─────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Departments')
BEGIN
    CREATE TABLE dbo.Departments (
        DepartmentId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        ParentDepartmentId INT,  -- NULL for top-level departments
        [Name] NVARCHAR(255) NOT NULL,
        [Description] NVARCHAR(500),

        -- Organizational info
        ManagerUserId INT,
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Active',  -- Active, Inactive, Archived
        CostCenter NVARCHAR(50),

        -- Timestamps
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_Departments_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        CONSTRAINT FK_Departments_Parent FOREIGN KEY (ParentDepartmentId) REFERENCES dbo.Departments(DepartmentId),
        CONSTRAINT FK_Departments_Manager FOREIGN KEY (ManagerUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT UQ_Departments_Name UNIQUE (TenantId, [Name]),
        INDEX IX_Departments_TenantId (TenantId),
        INDEX IX_Departments_ParentId (ParentDepartmentId),
        INDEX IX_Departments_Status ([Status])
    );
    PRINT 'Created table: Departments';
END

-- ─────────────────────────────────────────────────────────────────────────────
-- TEAMS TABLE: Teams within departments
-- ─────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Teams')
BEGIN
    CREATE TABLE dbo.Teams (
        TeamId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        DepartmentId INT NOT NULL,
        [Name] NVARCHAR(255) NOT NULL,
        [Description] NVARCHAR(500),

        -- Team leadership
        TeamLeadUserId INT,

        -- Status and settings
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Active',  -- Active, Inactive, Archived
        IsApprovalTeam BIT NOT NULL DEFAULT 0,  -- Is this an approval team?

        -- Timestamps
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_Teams_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        CONSTRAINT FK_Teams_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments(DepartmentId),
        CONSTRAINT FK_Teams_Lead FOREIGN KEY (TeamLeadUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT UQ_Teams_Name UNIQUE (DepartmentId, [Name]),
        INDEX IX_Teams_TenantId (TenantId),
        INDEX IX_Teams_DepartmentId (DepartmentId),
        INDEX IX_Teams_Status ([Status])
    );
    PRINT 'Created table: Teams';
END

-- ─────────────────────────────────────────────────────────────────────────────
-- TEAM_MEMBERS TABLE: User membership in teams with roles
-- ─────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TeamMembers')
BEGIN
    CREATE TABLE dbo.TeamMembers (
        TeamMemberId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        TeamId INT NOT NULL,
        UserId INT NOT NULL,

        -- Role within team
        [Role] NVARCHAR(100) NOT NULL DEFAULT 'Member',  -- Member, Lead, Manager

        -- Membership status
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Active',  -- Active, Inactive, Pending
        JoinedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_TeamMembers_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        CONSTRAINT FK_TeamMembers_Team FOREIGN KEY (TeamId) REFERENCES dbo.Teams(TeamId),
        CONSTRAINT FK_TeamMembers_User FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT UQ_TeamMembers_User UNIQUE (TeamId, UserId),
        INDEX IX_TeamMembers_TenantId (TenantId),
        INDEX IX_TeamMembers_TeamId (TeamId),
        INDEX IX_TeamMembers_UserId (UserId),
        INDEX IX_TeamMembers_Role ([Role])
    );
    PRINT 'Created table: TeamMembers';
END

-- ─────────────────────────────────────────────────────────────────────────────
-- APPROVAL_DELEGATION TABLE: Delegate approval authority to team members
-- ─────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ApprovalDelegation')
BEGIN
    CREATE TABLE dbo.ApprovalDelegation (
        DelegationId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        TeamId INT,  -- NULL = tenant-wide delegation
        FromUserId INT NOT NULL,
        ToUserId INT NOT NULL,

        -- Delegation scope
        ModuleType NVARCHAR(50),  -- NULL = all modules, 'Expense', 'Timesheet', etc.
        MinThreshold DECIMAL(12,2),  -- NULL = no minimum
        MaxThreshold DECIMAL(12,2),  -- NULL = no maximum

        -- Time-based delegation
        StartDate DATE NOT NULL DEFAULT CAST(GETUTCDATE() AS DATE),
        EndDate DATE,  -- NULL = indefinite

        -- Delegation rules
        CanApprove BIT NOT NULL DEFAULT 1,
        CanReject BIT NOT NULL DEFAULT 1,
        CanDelegate BIT NOT NULL DEFAULT 0,
        CanComment BIT NOT NULL DEFAULT 1,

        -- Status
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_ApprovalDelegation_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        CONSTRAINT FK_ApprovalDelegation_Team FOREIGN KEY (TeamId) REFERENCES dbo.Teams(TeamId),
        CONSTRAINT FK_ApprovalDelegation_From FOREIGN KEY (FromUserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_ApprovalDelegation_To FOREIGN KEY (ToUserId) REFERENCES dbo.Users(UserId),
        INDEX IX_ApprovalDelegation_TenantId (TenantId),
        INDEX IX_ApprovalDelegation_TeamId (TeamId),
        INDEX IX_ApprovalDelegation_FromUserId (FromUserId),
        INDEX IX_ApprovalDelegation_ToUserId (ToUserId),
        INDEX IX_ApprovalDelegation_Active (IsActive)
    );
    PRINT 'Created table: ApprovalDelegation';
END

-- ─────────────────────────────────────────────────────────────────────────────
-- DEPARTMENT_BUDGETS TABLE: Track allocated vs spent budgets
-- ─────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'DepartmentBudgets')
BEGIN
    CREATE TABLE dbo.DepartmentBudgets (
        BudgetId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        DepartmentId INT NOT NULL,

        -- Budget allocation
        FiscalYear INT NOT NULL,  -- e.g., 2026
        AllocatedAmount DECIMAL(12,2) NOT NULL,

        -- Budget tracking
        SpentAmount DECIMAL(12,2) NOT NULL DEFAULT 0,
        EncumberedAmount DECIMAL(12,2) NOT NULL DEFAULT 0,  -- Approved but not yet spent

        -- Budget enforcement
        AllowOverspend BIT NOT NULL DEFAULT 0,
        ThresholdAlertPercentage INT NOT NULL DEFAULT 80,  -- Alert at 80%

        -- Category breakdown
        CatExpense DECIMAL(12,2) NOT NULL DEFAULT 0,
        CatTravel DECIMAL(12,2) NOT NULL DEFAULT 0,
        CatMeals DECIMAL(12,2) NOT NULL DEFAULT 0,
        CatOther DECIMAL(12,2) NOT NULL DEFAULT 0,

        -- Status
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'Active',
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_DepartmentBudgets_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        CONSTRAINT FK_DepartmentBudgets_Department FOREIGN KEY (DepartmentId) REFERENCES dbo.Departments(DepartmentId),
        CONSTRAINT UQ_DepartmentBudgets_Year UNIQUE (DepartmentId, FiscalYear),
        INDEX IX_DepartmentBudgets_TenantId (TenantId),
        INDEX IX_DepartmentBudgets_DepartmentId (DepartmentId),
        INDEX IX_DepartmentBudgets_FiscalYear (FiscalYear)
    );
    PRINT 'Created table: DepartmentBudgets';
END

-- ─────────────────────────────────────────────────────────────────────────────
-- BULK_USER_IMPORT_LOG TABLE: Track bulk user imports for audit
-- ─────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'BulkUserImportLog')
BEGIN
    CREATE TABLE dbo.BulkUserImportLog (
        ImportId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        ImportedById INT NOT NULL,

        -- Import metadata
        Filename NVARCHAR(255),
        TotalRows INT NOT NULL,
        SuccessfulRows INT NOT NULL,
        FailedRows INT NOT NULL,
        [Status] NVARCHAR(50) NOT NULL,  -- Success, PartialSuccess, Failed

        -- Error details
        ErrorMessage NVARCHAR(MAX),

        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_BulkUserImportLog_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        CONSTRAINT FK_BulkUserImportLog_User FOREIGN KEY (ImportedById) REFERENCES dbo.Users(UserId),
        INDEX IX_BulkUserImportLog_TenantId (TenantId),
        INDEX IX_BulkUserImportLog_CreatedAt (CreatedAt)
    );
    PRINT 'Created table: BulkUserImportLog';
END

-- ─────────────────────────────────────────────────────────────────────────────
-- Extend Users table with department/team reference
-- ─────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME='Users' AND COLUMN_NAME='PrimaryDepartmentId'
)
BEGIN
    ALTER TABLE dbo.Users ADD PrimaryDepartmentId INT;
    ALTER TABLE dbo.Users ADD PrimaryTeamId INT;
    ALTER TABLE dbo.Users ADD CONSTRAINT FK_Users_Department
        FOREIGN KEY (PrimaryDepartmentId) REFERENCES dbo.Departments(DepartmentId);
    ALTER TABLE dbo.Users ADD CONSTRAINT FK_Users_Team
        FOREIGN KEY (PrimaryTeamId) REFERENCES dbo.Teams(TeamId);
    PRINT 'Extended Users table with department/team references';
END

-- ─────────────────────────────────────────────────────────────────────────────
-- Pre-configured data
-- ─────────────────────────────────────────────────────────────────────────────

-- Verify tables are created
SELECT 'Phase 4 Teams and Departments schema created successfully' AS [Migration Status];
