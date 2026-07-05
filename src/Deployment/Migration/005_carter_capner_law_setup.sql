-- Migration 005: Setup Carter Capner Law tenant with sample data
-- Creates Carter Capner Law tenant, user account for Peter Carter, and sample data
-- Runs automatically at next startup via MigrationRunnerService; deletes itself on success.

-- ══════════════════════════════════════════════════════════════════════════════
-- PART 1: Update Carter Capner Law Tenant
-- ══════════════════════════════════════════════════════════════════════════════

DECLARE @CarterCapnerTenantId UNIQUEIDENTIFIER = '44444444-4444-4444-4444-444444444444';

-- Ensure Carter Capner Law tenant exists and is properly configured
IF NOT EXISTS (SELECT 1 FROM Tenants WHERE TenantId = @CarterCapnerTenantId)
BEGIN
    INSERT INTO Tenants (
        TenantId, TenantName, [Slug], Subdomain, ContactEmail, ContactPhone,
        AddressLine1, Suburb, [State], PostCode, Country, ABN, SubscriptionPlan,
        MaxUsers, StorageLimitMB, IsTrial, TrialExpiresAt, IsActive, IsSuspended,
        CreatedAt, UpdatedAt
    ) VALUES (
        @CarterCapnerTenantId,
        'Carter Capner Law',
        'carter-capner-law',
        'carter-capner-law',
        'admin@cartercapnerlaw.com.au',
        '03 9000 0000',
        '400 Legal Avenue',
        'Melbourne',
        'VIC',
        '3000',
        'Australia',
        '11111111111',
        'Professional',
        100,
        20480,
        1,
        DATEADD(DAY, 30, GETDATE()),
        1,
        0,
        GETDATE(),
        GETDATE()
    );
    PRINT 'Created Carter Capner Law tenant';
END
ELSE
BEGIN
    -- Update existing tenant with proper configuration
    UPDATE Tenants
    SET
        TenantName = 'Carter Capner Law',
        ContactEmail = COALESCE(NULLIF(ContactEmail, ''), 'admin@cartercapnerlaw.com.au'),
        ContactPhone = COALESCE(NULLIF(ContactPhone, ''), '03 9000 0000'),
        IsActive = 1,
        UpdatedAt = GETDATE()
    WHERE TenantId = @CarterCapnerTenantId;
    PRINT 'Updated Carter Capner Law tenant';
END

-- ── Add Hostnames for Carter Capner Law ──────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM TenantHostnames WHERE TenantId = @CarterCapnerTenantId)
BEGIN
    INSERT INTO TenantHostnames (TenantId, Hostname, IsPrimary, CreatedAt)
    VALUES (@CarterCapnerTenantId, 'carter-capner-law.digitalresponse.com.au', 1, GETDATE());
    PRINT 'Added hostname for Carter Capner Law';
END

-- ══════════════════════════════════════════════════════════════════════════════
-- PART 2: Create Peter Carter User Account (peter.carter / demo123)
-- ══════════════════════════════════════════════════════════════════════════════

-- BCrypt hash of 'demo123' (workFactor 12)
DECLARE @PeterCarterlHash NVARCHAR(200) = '$2a$12$N9qo8uLOickgx2ZMRZoMyeIjZAgcg7b3XeKeUxWdeS86E36gZvQOo';

IF NOT EXISTS (SELECT 1 FROM Users WHERE UPPER(Code) = 'PETERC')
BEGIN
    INSERT INTO Users (Code, Name, Email, PW, UserTypeId, UserRoleId, Active, Deleted, DateCreated)
    VALUES (
        'PETERC',
        'Peter Carter',
        'peter.carter@cartercapnerlaw.com.au',
        @PeterCarterlHash,
        1,   -- UserTypeId 1 = Admin/Director type
        2,   -- UserRoleId 2 = Administrator
        1,
        0,
        GETDATE()
    );
    PRINT 'Created user PETERC (Peter Carter)';
END

-- ── Add Peter Carter to Carter Capner Law tenant ─────────────────────────────
DECLARE @PeterCarterId INT = (SELECT TOP 1 UserId FROM Users WHERE UPPER(Code) = 'PETERC');

IF @PeterCarterId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM UserTenants WHERE UserId = @PeterCarterId AND TenantId = @CarterCapnerTenantId)
BEGIN
    INSERT INTO UserTenants (UserId, TenantId, Role, IsDefault, IsActive, AcceptedAt, CreatedAt)
    VALUES (@PeterCarterId, @CarterCapnerTenantId, 'Admin', 1, 1, GETDATE(), GETDATE());
    PRINT 'Added Peter Carter to Carter Capner Law tenant';
END

-- ══════════════════════════════════════════════════════════════════════════════
-- PART 3: Setup Platform Settings (Branding for Law Firm)
-- ══════════════════════════════════════════════════════════════════════════════

DECLARE @LawFirmBrandingJson NVARCHAR(MAX) = '{
  "BrandingName": "Carter Capner Law",
  "PrimaryColor": "#1C3A47",
  "SecondaryColor": "#D4AF37",
  "AccentColor": "#B8860B",
  "TextPrimary": "#1A1A1A",
  "TextSecondary": "#4A4A4A",
  "TextTertiary": "#777777",
  "BackgroundPrimary": "#FFFFFF",
  "BackgroundSecondary": "#F9F7F4",
  "BorderColor": "#D4D4D4",
  "SuccessColor": "#4CAF50",
  "WarningColor": "#FFC107",
  "ErrorColor": "#F44336",
  "LoginLogoUrl": "/images/carter-capner-law-logo.svg",
  "DashboardLogoUrl": "/images/carter-capner-law-logo.svg",
  "FaviconUrl": "/favicon-ccl.ico",
  "FontFamily": "Georgia, Garamond, serif"
}';

IF NOT EXISTS (SELECT 1 FROM PlatformSettingsEntities WHERE TenantId = @CarterCapnerTenantId)
BEGIN
    INSERT INTO PlatformSettingsEntities (TenantId, SettingsJson, UpdatedAt, UpdatedBy)
    VALUES (@CarterCapnerTenantId, @LawFirmBrandingJson, GETDATE(), 'System');
    PRINT 'Created platform settings for Carter Capner Law with branding';
END
ELSE
BEGIN
    UPDATE PlatformSettingsEntities
    SET SettingsJson = @LawFirmBrandingJson, UpdatedAt = GETDATE(), UpdatedBy = 'System'
    WHERE TenantId = @CarterCapnerTenantId;
    PRINT 'Updated platform settings for Carter Capner Law';
END

-- ══════════════════════════════════════════════════════════════════════════════
-- PART 4: Create Sample Data - Law Firm Companies & Matters
-- ══════════════════════════════════════════════════════════════════════════════

-- Sample Clients (Companies)
DECLARE @ClientId1 INT;
DECLARE @ClientId2 INT;

IF NOT EXISTS (SELECT 1 FROM Companies WHERE UPPER(CompanyName) LIKE '%ABC CONSTRUCTION%')
BEGIN
    INSERT INTO Companies (CompanyName, ShortName, ABN, Email, Phone, Address, TenantId, DateCreated)
    OUTPUT INSERTED.CompanyId INTO @ClientId1
    VALUES ('ABC Construction Pty Ltd', 'ABC Const', '12345678901', 'legal@abcconst.com.au', '03 8000 1234', '500 Building Road, Melbourne VIC', @CarterCapnerTenantId, GETDATE());
    PRINT 'Created client: ABC Construction';
END
ELSE
    SELECT @ClientId1 = CompanyId FROM Companies WHERE UPPER(CompanyName) LIKE '%ABC CONSTRUCTION%' AND TenantId = @CarterCapnerTenantId;

IF NOT EXISTS (SELECT 1 FROM Companies WHERE UPPER(CompanyName) LIKE '%TECH INNOVATIONS%')
BEGIN
    INSERT INTO Companies (CompanyName, ShortName, ABN, Email, Phone, Address, TenantId, DateCreated)
    OUTPUT INSERTED.CompanyId INTO @ClientId2
    VALUES ('Tech Innovations Pty Ltd', 'Tech Innov', '98765432101', 'admin@techinnovate.com.au', '03 7000 5678', '600 Tech Street, Sydney NSW', @CarterCapnerTenantId, GETDATE());
    PRINT 'Created client: Tech Innovations';
END
ELSE
    SELECT @ClientId2 = CompanyId FROM Companies WHERE UPPER(CompanyName) LIKE '%TECH INNOVATIONS%' AND TenantId = @CarterCapnerTenantId;

-- Sample Matters (using Quotes as Matters)
IF @ClientId1 IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Quotes WHERE UPPER(Reference) LIKE '%CCL-MATTER-001%')
    BEGIN
        INSERT INTO Quotes (
            Reference, CompanyName, QuoteStatus, UnitCostTotal, NettPriceTotal, Margin,
            QuoteDate, Originator, ContactId, CompanyId, DivisionId, Code,
            QuoteStatusId, Validity, CustomerNotes, Terms, TenantId, DateCreated
        ) VALUES (
            'CCL-MATTER-001',
            'ABC Construction Pty Ltd',
            'In Progress',
            15000.00,
            15000.00,
            0.0,
            DATEADD(DAY, -20, GETDATE()),
            'Peter Carter',
            1,
            @ClientId1,
            1,
            'M001',
            2,
            30,
            'Commercial Property Dispute - Contract Review',
            'Standard Legal Terms',
            @CarterCapnerTenantId,
            GETDATE()
        );
        PRINT 'Created matter CCL-MATTER-001';
    END

    IF NOT EXISTS (SELECT 1 FROM Quotes WHERE UPPER(Reference) LIKE '%CCL-MATTER-002%')
    BEGIN
        INSERT INTO Quotes (
            Reference, CompanyName, QuoteStatus, UnitCostTotal, NettPriceTotal, Margin,
            QuoteDate, Originator, ContactId, CompanyId, DivisionId, Code,
            QuoteStatusId, Validity, CustomerNotes, Terms, TenantId, DateCreated
        ) VALUES (
            'CCL-MATTER-002',
            'ABC Construction Pty Ltd',
            'Completed',
            12000.00,
            12000.00,
            0.0,
            DATEADD(DAY, -60, GETDATE()),
            'Peter Carter',
            1,
            @ClientId1,
            1,
            'M002',
            3,
            30,
            'Employment Law Consultation - Completed Successfully',
            'Standard Legal Terms',
            @CarterCapnerTenantId,
            GETDATE()
        );
        PRINT 'Created matter CCL-MATTER-002';
    END
END

IF @ClientId2 IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Quotes WHERE UPPER(Reference) LIKE '%CCL-MATTER-003%')
    BEGIN
        INSERT INTO Quotes (
            Reference, CompanyName, QuoteStatus, UnitCostTotal, NettPriceTotal, Margin,
            QuoteDate, Originator, ContactId, CompanyId, DivisionId, Code,
            QuoteStatusId, Validity, CustomerNotes, Terms, TenantId, DateCreated
        ) VALUES (
            'CCL-MATTER-003',
            'Tech Innovations Pty Ltd',
            'In Progress',
            18000.00,
            18000.00,
            0.0,
            DATEADD(DAY, -10, GETDATE()),
            'Peter Carter',
            1,
            @ClientId2,
            1,
            'M003',
            2,
            30,
            'Intellectual Property - Patent Application Support',
            'Standard Legal Terms',
            @CarterCapnerTenantId,
            GETDATE()
        );
        PRINT 'Created matter CCL-MATTER-003';
    END
END

PRINT '✓ Carter Capner Law setup complete';
PRINT '  - Tenant: Carter Capner Law';
PRINT '  - User: peter.carter (PETERC) / demo123';
PRINT '  - Sample clients and legal matters created';
PRINT '  - Law firm branding configured';
