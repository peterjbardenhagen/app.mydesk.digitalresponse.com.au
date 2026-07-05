-- Migration 016: Encryption Keys & Rate Limiting Configuration
-- Implements field-level encryption support and rate limiting rules
-- Phase 1 Week 4: Final critical security foundation

-- Create EncryptionKeys table for managing encryption keys with rotation
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'EncryptionKeys')
BEGIN
    CREATE TABLE dbo.EncryptionKeys (
        Id INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        KeyName NVARCHAR(100) NOT NULL,             -- e.g., 'ExpenseData', 'UserPII', 'BankDetails'
        EncryptionAlgorithm NVARCHAR(50) NOT NULL,  -- 'AES256', 'AES256-GCM'
        KeyVersion INT NOT NULL DEFAULT 1,           -- For key rotation support
        IsActive BIT NOT NULL DEFAULT 1,             -- Current key (only one should be active per KeyName)
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ExpiresAt DATETIME2,                         -- When key should be rotated
        RotatedAt DATETIME2,                         -- When key was rotated
        CreatedBy INT NOT NULL,
        RotationNotes NVARCHAR(500),                 -- Notes about rotation reason

        CONSTRAINT FK_EncryptionKeys_TenantId FOREIGN KEY (TenantId)
            REFERENCES dbo.Tenants(TenantId) ON DELETE CASCADE,
        CONSTRAINT CK_EncryptionKeys_OneActivePerName
            CHECK (IsActive = 0 OR (SELECT COUNT(*) FROM dbo.EncryptionKeys ek2
                WHERE ek2.TenantId = dbo.EncryptionKeys.TenantId AND ek2.KeyName = dbo.EncryptionKeys.KeyName AND ek2.IsActive = 1) = 1)
    );

    CREATE INDEX IDX_EncryptionKeys_TenantId ON dbo.EncryptionKeys(TenantId, IsActive);
    CREATE INDEX IDX_EncryptionKeys_KeyName ON dbo.EncryptionKeys(TenantId, KeyName, IsActive);
    CREATE INDEX IDX_EncryptionKeys_ExpiresAt ON dbo.EncryptionKeys(ExpiresAt) WHERE IsActive = 1;

    PRINT 'Created EncryptionKeys table with indexes';
END;

-- Create FieldEncryption table to track which fields are encrypted
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'FieldEncryption')
BEGIN
    CREATE TABLE dbo.FieldEncryption (
        Id INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        TableName NVARCHAR(100) NOT NULL,           -- e.g., 'Expenses', 'Users', 'BankAccounts'
        ColumnName NVARCHAR(100) NOT NULL,          -- e.g., 'Amount', 'Email', 'AccountNumber'
        DataClassification NVARCHAR(50) NOT NULL,   -- 'PII', 'SENSITIVE', 'CONFIDENTIAL', 'PUBLIC'
        EncryptionKeyName NVARCHAR(100) NOT NULL,   -- Reference to key name in EncryptionKeys
        IsEnabled BIT NOT NULL DEFAULT 1,           -- Whether encryption is active for this field
        EncryptedColumnName NVARCHAR(100),          -- Name of encrypted column (null = inline encryption)
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CreatedBy INT NOT NULL,

        CONSTRAINT FK_FieldEncryption_TenantId FOREIGN KEY (TenantId)
            REFERENCES dbo.Tenants(TenantId) ON DELETE CASCADE
    );

    CREATE INDEX IDX_FieldEncryption_TenantId ON dbo.FieldEncryption(TenantId, IsEnabled);
    CREATE INDEX IDX_FieldEncryption_Table ON dbo.FieldEncryption(TableName, ColumnName);
    CREATE INDEX IDX_FieldEncryption_Classification ON dbo.FieldEncryption(DataClassification, IsEnabled);

    PRINT 'Created FieldEncryption table with indexes';
END;

-- Create RateLimitingRules table for configurable rate limits
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RateLimitingRules')
BEGIN
    CREATE TABLE dbo.RateLimitingRules (
        Id INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT,                              -- Null = applies to all tenants (platform-level)
        EndpointPattern NVARCHAR(255) NOT NULL,    -- e.g., '/api/auth/login', '/api/expenses/*'
        RateLimitType NVARCHAR(50) NOT NULL,       -- 'IP', 'USER', 'GLOBAL'
        RequestsPerWindow INT NOT NULL,            -- e.g., 5 requests
        WindowSizeSeconds INT NOT NULL,            -- e.g., 300 seconds = 5 min window
        BackoffStrategyType NVARCHAR(50),          -- 'EXPONENTIAL', 'LINEAR', 'FIXED'
        BackoffMultiplier DECIMAL(5,2),            -- For exponential backoff
        MaxBackoffSeconds INT,                     -- Maximum backoff time
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CreatedBy INT NOT NULL,

        CONSTRAINT FK_RateLimitingRules_TenantId FOREIGN KEY (TenantId)
            REFERENCES dbo.Tenants(TenantId) ON DELETE CASCADE
    );

    CREATE INDEX IDX_RateLimitingRules_EndpointPattern ON dbo.RateLimitingRules(EndpointPattern, IsActive);
    CREATE INDEX IDX_RateLimitingRules_TenantId ON dbo.RateLimitingRules(TenantId, IsActive);

    PRINT 'Created RateLimitingRules table with indexes';
END;

-- Create RateLimitingViolations table for tracking breaches
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'RateLimitingViolations')
BEGIN
    CREATE TABLE dbo.RateLimitingViolations (
        Id INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT,
        RuleId INT NOT NULL,
        Identifier NVARCHAR(255) NOT NULL,         -- IP address or UserId
        IdentifierType NVARCHAR(50) NOT NULL,      -- 'IP', 'USER'
        EndpointPattern NVARCHAR(255),
        ViolationAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        RequestCount INT,
        SuspicionLevel NVARCHAR(50) NOT NULL,      -- 'LOW', 'MEDIUM', 'HIGH', 'CRITICAL'
        IsAutoBlocked BIT NOT NULL DEFAULT 0,
        BlockedUntil DATETIME2,
        InvestigatedAt DATETIME2,

        CONSTRAINT FK_RateLimitingViolations_RuleId FOREIGN KEY (RuleId)
            REFERENCES dbo.RateLimitingRules(Id) ON DELETE CASCADE
    );

    CREATE INDEX IDX_RateLimitingViolations_Identifier ON dbo.RateLimitingViolations(Identifier, IdentifierType, ViolationAt DESC);
    CREATE INDEX IDX_RateLimitingViolations_SuspicionLevel ON dbo.RateLimitingViolations(SuspicionLevel, ViolationAt DESC);
    CREATE INDEX IDX_RateLimitingViolations_IsAutoBlocked ON dbo.RateLimitingViolations(IsAutoBlocked, BlockedUntil);

    PRINT 'Created RateLimitingViolations table with indexes';
END;

-- Seed default rate limiting rules for critical endpoints
IF NOT EXISTS (SELECT 1 FROM dbo.RateLimitingRules)
BEGIN
    DECLARE @AdminUserId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE IsAdmin = 1);
    IF @AdminUserId IS NULL SET @AdminUserId = 1;

    INSERT INTO dbo.RateLimitingRules (EndpointPattern, RateLimitType, RequestsPerWindow, WindowSizeSeconds, BackoffStrategyType, BackoffMultiplier, MaxBackoffSeconds, IsActive, CreatedBy)
    VALUES
        -- Authentication endpoints (strict limits)
        ('/api/auth/login', 'IP', 5, 300, 'EXPONENTIAL', 2.0, 900, 1, @AdminUserId),         -- 5 requests per 5 minutes per IP
        ('/api/auth/forgot-password', 'IP', 3, 600, 'EXPONENTIAL', 2.0, 1800, 1, @AdminUserId), -- 3 requests per 10 minutes per IP

        -- API endpoints (moderate limits)
        ('/api/expenses/*', 'USER', 100, 3600, 'LINEAR', 1.0, 300, 1, @AdminUserId),          -- 100 requests per hour per user
        ('/api/timesheets/*', 'USER', 100, 3600, 'LINEAR', 1.0, 300, 1, @AdminUserId),        -- 100 requests per hour per user
        ('/api/approval/*', 'USER', 50, 3600, 'LINEAR', 1.0, 300, 1, @AdminUserId),           -- 50 requests per hour per user

        -- Data export endpoints (very strict)
        ('/api/compliance/audit-log', 'USER', 10, 3600, 'FIXED', 1.0, 600, 1, @AdminUserId),  -- 10 requests per hour per user

    PRINT 'Seeded default rate limiting rules';
END;

-- Compliance note: This migration implements:
--
-- Encryption Requirements (ISO 27001, SOC 2, Australian Privacy Act):
--   - Field-level encryption for PII (Personal Identifiable Information)
--   - Support for key rotation to meet compliance requirements
--   - Per-tenant encryption keys for data isolation
--   - Classification tracking for data sensitivity
--
-- Rate Limiting Requirements (OWASP Top 10, DDoS Protection):
--   - Configurable per-endpoint rate limits
--   - IP-based and user-based limiting
--   - Exponential backoff for brute-force attacks
--   - Violation tracking for security investigation
--
-- Australian Privacy Act Compliance:
--   - Encryption of personal information in transit and at rest
--   - Access control via rate limiting prevents data harvesting
--   - Violation logging for breach investigation

PRINT 'Migration 016: Encryption Keys & Rate Limiting Configuration - Complete';
