-- Migration 018: Client Onboarding System
-- Tracks client onboarding process through 6-step wizard
-- Phase 2 Weeks 7-8: Client onboarding wizard implementation

-- Create ClientOnboardingSession table to track wizard progress
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ClientOnboardingSession')
BEGIN
    CREATE TABLE dbo.ClientOnboardingSession (
        Id INT PRIMARY KEY IDENTITY(1,1),
        SessionToken NVARCHAR(100) NOT NULL UNIQUE,  -- Unique session identifier
        TenantId INT,                                -- Null until tenant is created
        AdminUserId INT NOT NULL,                    -- Super Admin creating the client
        CurrentStep INT NOT NULL DEFAULT 1,          -- 1-6: Wizard step progress
        AdminName NVARCHAR(255) NOT NULL,
        AdminEmail NVARCHAR(255) NOT NULL,
        AdminPassword NVARCHAR(255),                 -- Hashed password for initial user

        -- Step 1: Basic Info
        TenantName NVARCHAR(255),
        TenantCode NVARCHAR(50),

        -- Step 2: Domain Configuration
        Domain NVARCHAR(255),
        DomainVerified BIT DEFAULT 0,

        -- Step 3: Approval Workflow Setup
        ApprovalWorkflowTemplate NVARCHAR(50),       -- 'STANDARD_2LEVEL', 'DIRECTOR_APPROVAL', etc.

        -- Step 4: Billing Model
        BillingModel NVARCHAR(50),
        BillingContactEmail NVARCHAR(255),

        -- Step 5: Users & Seats
        InitialUserSeats INT,

        -- Step 6: Confirmation
        IsConfirmed BIT DEFAULT 0,
        ConfirmedAt DATETIME2,

        -- Status tracking
        [Status] NVARCHAR(50) NOT NULL DEFAULT 'IN_PROGRESS',  -- IN_PROGRESS, COMPLETED, ABANDONED
        StartedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CompletedAt DATETIME2,
        AbandonedAt DATETIME2,
        LastActivityAt DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE INDEX IDX_ClientOnboardingSession_TenantId ON dbo.ClientOnboardingSession(TenantId);
    CREATE INDEX IDX_ClientOnboardingSession_Status ON dbo.ClientOnboardingSession(Status, LastActivityAt DESC);
    CREATE INDEX IDX_ClientOnboardingSession_Token ON dbo.ClientOnboardingSession(SessionToken);

    PRINT 'Created ClientOnboardingSession table with indexes';
END;

-- Create ClientOnboardingStepAudit table for step-by-step tracking
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ClientOnboardingStepAudit')
BEGIN
    CREATE TABLE dbo.ClientOnboardingStepAudit (
        Id INT PRIMARY KEY IDENTITY(1,1),
        SessionId INT NOT NULL,
        StepNumber INT NOT NULL,
        StepName NVARCHAR(100),
        DataSubmitted NVARCHAR(MAX),              -- JSON of form data
        ValidationStatus NVARCHAR(50),            -- PENDING, VALID, INVALID
        ValidationErrors NVARCHAR(MAX),           -- JSON array of errors
        CompletedAt DATETIME2,

        CONSTRAINT FK_OnboardingStepAudit_SessionId FOREIGN KEY (SessionId)
            REFERENCES dbo.ClientOnboardingSession(Id) ON DELETE CASCADE
    );

    CREATE INDEX IDX_OnboardingStepAudit_SessionId ON dbo.ClientOnboardingStepAudit(SessionId, StepNumber);

    PRINT 'Created ClientOnboardingStepAudit table with indexes';
END;

-- Create OnboardingWorkflowTemplates table for predefined approval workflows
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'OnboardingWorkflowTemplates')
BEGIN
    CREATE TABLE dbo.OnboardingWorkflowTemplates (
        Id INT PRIMARY KEY IDENTITY(1,1),
        TemplateName NVARCHAR(100) NOT NULL UNIQUE,  -- e.g., 'STANDARD_2LEVEL'
        TemplateCode NVARCHAR(50) NOT NULL,          -- Used in onboarding
        Description NVARCHAR(MAX),
        ApprovalLevels INT DEFAULT 2,
        IsActive BIT DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        -- JSON configuration: roles, thresholds, etc.
        TemplateConfig NVARCHAR(MAX)                 -- JSON of approval configuration
    );

    CREATE INDEX IDX_OnboardingWorkflowTemplates_Code ON dbo.OnboardingWorkflowTemplates(TemplateCode);

    PRINT 'Created OnboardingWorkflowTemplates table with indexes';
END;

-- Seed onboarding workflow templates
IF NOT EXISTS (SELECT 1 FROM dbo.OnboardingWorkflowTemplates)
BEGIN
    INSERT INTO dbo.OnboardingWorkflowTemplates (TemplateName, TemplateCode, Description, ApprovalLevels, TemplateConfig)
    VALUES
        (
            'Standard 2-Level Approval',
            'STANDARD_2LEVEL',
            'Manager approval up to $5000, Director for larger amounts',
            2,
            '{"level1":{"role":"Manager","maxThreshold":5000},"level2":{"role":"Director","maxThreshold":null}}'
        ),
        (
            'Director Approval Only',
            'DIRECTOR_APPROVAL',
            'Single approval level by Director (no manager approval)',
            1,
            '{"level1":{"role":"Director","maxThreshold":null}}'
        ),
        (
            'Threshold-Based (Manager/Finance/CEO)',
            'THRESHOLD_3LEVEL',
            'Manager <$2000, Finance <$10000, CEO for larger',
            3,
            '{"level1":{"role":"Manager","maxThreshold":2000},"level2":{"role":"Finance","maxThreshold":10000},"level3":{"role":"Director"}}'
        );

    PRINT 'Seeded onboarding workflow templates';
END;

-- Create ClientOnboardingTemplate table for storing completed wizard results
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ClientOnboardingTemplate')
BEGIN
    CREATE TABLE dbo.ClientOnboardingTemplate (
        Id INT PRIMARY KEY IDENTITY(1,1),
        SessionId INT NOT NULL,
        TenantId INT NOT NULL,

        -- Complete snapshot of all wizard inputs
        WizardData NVARCHAR(MAX) NOT NULL,          -- JSON of all 6 steps

        -- Final configuration
        InitialDomain NVARCHAR(255),
        InitialBillingModel NVARCHAR(50),
        InitialApprovalWorkflow NVARCHAR(50),
        InitialUserCount INT,

        -- Completion tracking
        OnboardingCompletedBy INT NOT NULL,         -- Super Admin who completed
        OnboardingCompletedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        -- Follow-up
        IsFollowUpScheduled BIT DEFAULT 0,
        FollowUpDate DATETIME2,
        FollowUpNotes NVARCHAR(MAX),

        CONSTRAINT FK_OnboardingTemplate_SessionId FOREIGN KEY (SessionId)
            REFERENCES dbo.ClientOnboardingSession(Id) ON DELETE CASCADE,
        CONSTRAINT FK_OnboardingTemplate_TenantId FOREIGN KEY (TenantId)
            REFERENCES dbo.Tenants(TenantId) ON DELETE CASCADE
    );

    CREATE INDEX IDX_OnboardingTemplate_TenantId ON dbo.ClientOnboardingTemplate(TenantId);
    CREATE INDEX IDX_OnboardingTemplate_CompletedAt ON dbo.ClientOnboardingTemplate(OnboardingCompletedAt DESC);

    PRINT 'Created ClientOnboardingTemplate table with indexes';
END;

-- Compliance note: This migration implements:
--
-- Client Onboarding Workflow:
--   - Step 1: Basic tenant information (name, code)
--   - Step 2: Domain configuration with verification
--   - Step 3: Approval workflow template selection
--   - Step 4: Billing model and contact info
--   - Step 5: Initial user seats
--   - Step 6: Review and confirmation
--
-- Session Management:
--   - Session tokens for secure wizard access
--   - Progress tracking through all 6 steps
--   - Validation at each step
--   - Abandonment tracking
--   - Session timeout support (via LastActivityAt)
--
-- Audit Trail:
--   - Step-by-step completion audit
--   - Form data capture for troubleshooting
--   - Validation error logging
--   - Complete wizard snapshot stored

PRINT 'Migration 018: Client Onboarding System - Complete';
