-- Migration 013: Domain-Based Tenant Routing
-- Adds support for domain-based multi-tenancy routing
-- Allows users to login with their email domain to automatically route to correct tenant
-- Phase 1 Week 1: Critical foundation for compliance

-- Create TenantDomains table for domain-to-tenant mapping
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'TenantDomains')
BEGIN
    CREATE TABLE dbo.TenantDomains (
        Id INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        Domain NVARCHAR(255) NOT NULL UNIQUE,  -- e.g., digitalresponse.com.au, techlight.com.au
        IsVerified BIT NOT NULL DEFAULT 0,     -- Domain ownership verified
        VerificationToken NVARCHAR(500),        -- Random token for DNS/email verification
        VerificationTokenExpiry DATETIME2,      -- When verification token expires
        VerifiedAt DATETIME2,                   -- When domain was verified
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CreatedBy INT NOT NULL,                 -- Admin user who added domain
        UpdatedAt DATETIME2,
        UpdatedBy INT,
        IsActive BIT NOT NULL DEFAULT 1,        -- Soft delete support

        CONSTRAINT FK_TenantDomains_TenantId FOREIGN KEY (TenantId)
            REFERENCES dbo.Tenants(TenantId) ON DELETE CASCADE
    );

    -- Create indexes for performance
    CREATE INDEX IDX_TenantDomains_Domain ON dbo.TenantDomains(Domain) WHERE IsActive = 1;
    CREATE INDEX IDX_TenantDomains_TenantId ON dbo.TenantDomains(TenantId) WHERE IsActive = 1;
    CREATE INDEX IDX_TenantDomains_IsVerified ON dbo.TenantDomains(IsVerified, IsActive);

    PRINT 'Created TenantDomains table with indexes';
END;

-- Create DomainVerifications table for audit trail of verification attempts
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'DomainVerifications')
BEGIN
    CREATE TABLE dbo.DomainVerifications (
        Id INT PRIMARY KEY IDENTITY(1,1),
        TenantDomainId INT NOT NULL,
        VerificationType NVARCHAR(50) NOT NULL,  -- 'DNS', 'EMAIL', 'FILE'
        VerificationMethod NVARCHAR(500),        -- Details of verification method used
        IsSuccessful BIT NOT NULL,
        AttemptedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        AttemptedBy INT,                         -- User who initiated verification
        IpAddress NVARCHAR(45),                  -- IPv4 or IPv6
        ErrorMessage NVARCHAR(MAX),              -- Error if verification failed

        CONSTRAINT FK_DomainVerifications_TenantDomainId FOREIGN KEY (TenantDomainId)
            REFERENCES dbo.TenantDomains(Id) ON DELETE CASCADE
    );

    CREATE INDEX IDX_DomainVerifications_TenantDomainId ON dbo.DomainVerifications(TenantDomainId);
    CREATE INDEX IDX_DomainVerifications_AttemptedAt ON dbo.DomainVerifications(AttemptedAt DESC);

    PRINT 'Created DomainVerifications table with indexes';
END;

-- Seed default domains for existing tenants (for demo/testing)
-- Only insert if no domains exist yet
IF NOT EXISTS (SELECT 1 FROM dbo.TenantDomains)
BEGIN
    -- Get the first admin user for CreatedBy field
    DECLARE @AdminUserId INT = (SELECT TOP 1 UserId FROM dbo.Users WHERE IsAdmin = 1);

    IF @AdminUserId IS NULL
        SET @AdminUserId = 1;  -- Fallback if no admin exists

    -- Add domains for demo tenants if they exist
    INSERT INTO dbo.TenantDomains (TenantId, Domain, IsVerified, VerifiedAt, CreatedBy, IsActive)
    SELECT
        t.TenantId,
        LOWER(REPLACE(t.Name, ' ', '')) + '.local',  -- e.g., 'digitalresponse.local'
        1,  -- Mark as verified for demo
        GETUTCDATE(),
        @AdminUserId,
        1
    FROM dbo.Tenants t
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.TenantDomains td WHERE td.TenantId = t.TenantId
    )
    AND t.IsActive = 1;

    PRINT 'Seeded default domains for existing tenants';
END;

-- Compliance note: This migration supports Australian Privacy Act compliance by enabling
-- proper tenant isolation based on user email domain. Users logging in with email address
-- are automatically routed to their organization's tenant, preventing cross-tenant access.
-- All domain verification is logged for audit purposes.

PRINT 'Migration 013: Domain-Based Tenant Routing - Complete';
