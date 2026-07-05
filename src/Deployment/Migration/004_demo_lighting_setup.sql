-- Migration 004: Setup Demo Lighting tenant with sample data
-- Creates Demo Lighting tenant, user accounts, and sample data for all core modules
-- Runs automatically at next startup via MigrationRunnerService; deletes itself on success.

-- ══════════════════════════════════════════════════════════════════════════════
-- PART 1: Create Demo Lighting Tenant
-- ══════════════════════════════════════════════════════════════════════════════

DECLARE @DemoLightingTenantId UNIQUEIDENTIFIER = '55555555-5555-5555-5555-555555555555';

IF NOT EXISTS (SELECT 1 FROM Tenants WHERE TenantId = @DemoLightingTenantId)
BEGIN
    INSERT INTO Tenants (
        TenantId, TenantName, [Slug], Subdomain, ContactEmail, ContactPhone,
        AddressLine1, Suburb, [State], PostCode, Country, ABN, SubscriptionPlan,
        MaxUsers, StorageLimitMB, IsTrial, TrialExpiresAt, IsActive, IsSuspended,
        CreatedAt, UpdatedAt
    ) VALUES (
        @DemoLightingTenantId,
        'Demo Lighting',
        'demo-lighting',
        'demo-lighting',
        'admin@demolighting.com.au',
        '1300 000 000',
        '123 Demo Street',
        'Melbourne',
        'VIC',
        '3000',
        'Australia',
        '12345678901',
        'Professional',
        50,
        10240,
        1,
        DATEADD(DAY, 30, GETDATE()),
        1,
        0,
        GETDATE(),
        GETDATE()
    );
    PRINT 'Created Demo Lighting tenant';
END

-- ── Add Hostnames for Demo Lighting ──────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM TenantHostnames WHERE TenantId = @DemoLightingTenantId AND Hostname = 'dev.digitalresponse.com.au')
BEGIN
    INSERT INTO TenantHostnames (TenantId, Hostname, IsPrimary, CreatedAt)
    VALUES (@DemoLightingTenantId, 'dev.digitalresponse.com.au', 1, GETDATE());
    PRINT 'Added dev.digitalresponse.com.au hostname';
END

IF NOT EXISTS (SELECT 1 FROM TenantHostnames WHERE TenantId = @DemoLightingTenantId AND Hostname = 'app.mydesk.digitalresponse.com.au')
BEGIN
    INSERT INTO TenantHostnames (TenantId, Hostname, IsPrimary, CreatedAt)
    VALUES (@DemoLightingTenantId, 'app.mydesk.digitalresponse.com.au', 0, GETDATE());
    PRINT 'Added app.mydesk.digitalresponse.com.au hostname';
END

-- ══════════════════════════════════════════════════════════════════════════════
-- PART 2: Create Demo User Account (demo / demo123)
-- ══════════════════════════════════════════════════════════════════════════════

-- BCrypt hash of 'demo123' (workFactor 12)
DECLARE @DemoUserHash NVARCHAR(200) = '$2a$12$N9qo8uLOickgx2ZMRZoMyeIjZAgcg7b3XeKeUxWdeS86E36gZvQOo';

IF NOT EXISTS (SELECT 1 FROM Users WHERE UPPER(Code) = 'DEMO')
BEGIN
    INSERT INTO Users (Code, Name, Email, PW, UserTypeId, UserRoleId, Active, Deleted, DateCreated)
    VALUES (
        'DEMO',
        'Demo User',
        'demo@demolighting.com.au',
        @DemoUserHash,
        1,   -- UserTypeId 1 = Admin/Director type
        2,   -- UserRoleId 2 = Administrator
        1,
        0,
        GETDATE()
    );
    PRINT 'Created user DEMO for Demo Lighting';
END

-- ── Add demo user to Demo Lighting tenant ────────────────────────────────────
DECLARE @DemoUserId INT = (SELECT TOP 1 UserId FROM Users WHERE UPPER(Code) = 'DEMO');

IF @DemoUserId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM UserTenants WHERE UserId = @DemoUserId AND TenantId = @DemoLightingTenantId)
BEGIN
    INSERT INTO UserTenants (UserId, TenantId, Role, IsDefault, IsActive, AcceptedAt, CreatedAt)
    VALUES (@DemoUserId, @DemoLightingTenantId, 'Admin', 1, 1, GETDATE(), GETDATE());
    PRINT 'Added demo user to Demo Lighting tenant';
END

-- ══════════════════════════════════════════════════════════════════════════════
-- PART 3: Setup Platform Settings (Branding)
-- ══════════════════════════════════════════════════════════════════════════════

DECLARE @BrandingJson NVARCHAR(MAX) = '{
  "BrandingName": "Demo Lighting",
  "PrimaryColor": "#FF6B35",
  "SecondaryColor": "#004E89",
  "AccentColor": "#F7B801",
  "TextPrimary": "#1A1A1A",
  "TextSecondary": "#666666",
  "TextTertiary": "#999999",
  "BackgroundPrimary": "#FFFFFF",
  "BackgroundSecondary": "#F5F5F5",
  "BorderColor": "#E0E0E0",
  "SuccessColor": "#4CAF50",
  "WarningColor": "#FFC107",
  "ErrorColor": "#F44336",
  "LoginLogoUrl": "/images/demo-lighting-logo.svg",
  "DashboardLogoUrl": "/images/demo-lighting-logo.svg",
  "FaviconUrl": "/favicon-demo-lighting.ico",
  "FontFamily": "Inter, -apple-system, BlinkMacSystemFont, Segoe UI, sans-serif"
}';

IF NOT EXISTS (SELECT 1 FROM PlatformSettingsEntities WHERE TenantId = @DemoLightingTenantId)
BEGIN
    INSERT INTO PlatformSettingsEntities (TenantId, SettingsJson, UpdatedAt, UpdatedBy)
    VALUES (@DemoLightingTenantId, @BrandingJson, GETDATE(), 'System');
    PRINT 'Created platform settings for Demo Lighting with branding';
END

-- ══════════════════════════════════════════════════════════════════════════════
-- PART 4: Create Sample Data - Companies & Divisions
-- ══════════════════════════════════════════════════════════════════════════════

-- Sample Companies
DECLARE @CompanyId1 INT;
DECLARE @CompanyId2 INT;
DECLARE @CompanyId3 INT;

IF NOT EXISTS (SELECT 1 FROM Companies WHERE UPPER(CompanyName) = 'SOLAR SYSTEMS PTY LTD')
BEGIN
    INSERT INTO Companies (CompanyName, ShortName, ABN, Email, Phone, Address, TenantId, DateCreated)
    OUTPUT INSERTED.CompanyId INTO @CompanyId1
    VALUES ('Solar Systems Pty Ltd', 'Solar Systems', '12345678901', 'contact@solarsystems.com.au', '03 9000 1234', '100 Solar Road, Melbourne VIC', @DemoLightingTenantId, GETDATE());
    PRINT 'Created company: Solar Systems';
END
ELSE
    SELECT @CompanyId1 = CompanyId FROM Companies WHERE UPPER(CompanyName) = 'SOLAR SYSTEMS PTY LTD' AND TenantId = @DemoLightingTenantId;

IF NOT EXISTS (SELECT 1 FROM Companies WHERE UPPER(CompanyName) = 'BRIGHT LED SOLUTIONS')
BEGIN
    INSERT INTO Companies (CompanyName, ShortName, ABN, Email, Phone, Address, TenantId, DateCreated)
    OUTPUT INSERTED.CompanyId INTO @CompanyId2
    VALUES ('Bright LED Solutions', 'Bright LED', '98765432101', 'info@brightled.com.au', '03 9000 5678', '200 LED Lane, Sydney NSW', @DemoLightingTenantId, GETDATE());
    PRINT 'Created company: Bright LED Solutions';
END
ELSE
    SELECT @CompanyId2 = CompanyId FROM Companies WHERE UPPER(CompanyName) = 'BRIGHT LED SOLUTIONS' AND TenantId = @DemoLightingTenantId;

IF NOT EXISTS (SELECT 1 FROM Companies WHERE UPPER(CompanyName) = 'SMART LIGHTING CO')
BEGIN
    INSERT INTO Companies (CompanyName, ShortName, ABN, Email, Phone, Address, TenantId, DateCreated)
    OUTPUT INSERTED.CompanyId INTO @CompanyId3
    VALUES ('Smart Lighting Co', 'Smart Lighting', '55555555555', 'sales@smartlighting.com.au', '07 3000 9999', '300 Tech Street, Brisbane QLD', @DemoLightingTenantId, GETDATE());
    PRINT 'Created company: Smart Lighting Co';
END
ELSE
    SELECT @CompanyId3 = CompanyId FROM Companies WHERE UPPER(CompanyName) = 'SMART LIGHTING CO' AND TenantId = @DemoLightingTenantId;

-- Sample Divisions
DECLARE @DivisionId1 INT;
DECLARE @DivisionId2 INT;

IF NOT EXISTS (SELECT 1 FROM Divisions WHERE UPPER(DivisionName) = 'SALES & COMMERCIAL')
BEGIN
    INSERT INTO Divisions (DivisionName, DivisionCode, TenantId, DateCreated)
    OUTPUT INSERTED.DivisionId INTO @DivisionId1
    VALUES ('Sales & Commercial', 'SC', @DemoLightingTenantId, GETDATE());
    PRINT 'Created division: Sales & Commercial';
END
ELSE
    SELECT @DivisionId1 = DivisionId FROM Divisions WHERE UPPER(DivisionName) = 'SALES & COMMERCIAL' AND TenantId = @DemoLightingTenantId;

IF NOT EXISTS (SELECT 1 FROM Divisions WHERE UPPER(DivisionName) = 'PROJECTS & INSTALLATION')
BEGIN
    INSERT INTO Divisions (DivisionName, DivisionCode, TenantId, DateCreated)
    OUTPUT INSERTED.DivisionId INTO @DivisionId2
    VALUES ('Projects & Installation', 'PI', @DemoLightingTenantId, GETDATE());
    PRINT 'Created division: Projects & Installation';
END
ELSE
    SELECT @DivisionId2 = DivisionId FROM Divisions WHERE UPPER(DivisionName) = 'PROJECTS & INSTALLATION' AND TenantId = @DemoLightingTenantId;

-- ══════════════════════════════════════════════════════════════════════════════
-- PART 5: Create Sample Quotes
-- ══════════════════════════════════════════════════════════════════════════════

IF @CompanyId1 IS NOT NULL AND @DivisionId1 IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Quotes WHERE UPPER(Reference) LIKE '%DL-QUOTE-001%' AND CompanyId = @CompanyId1)
    BEGIN
        DECLARE @QuoteId1 INT;
        INSERT INTO Quotes (
            Reference, CompanyName, QuoteStatus, UnitCostTotal, NettPriceTotal, Margin,
            QuoteDate, Originator, ContactId, CompanyId, DivisionId, Code,
            QuoteStatusId, Validity, CustomerNotes, Terms, TenantId, DateCreated
        ) OUTPUT INSERTED.Qid INTO @QuoteId1
        VALUES (
            'DL-QUOTE-001',
            'Solar Systems Pty Ltd',
            'Approved',
            15000.00,
            18750.00,
            25.0,
            DATEADD(DAY, -5, GETDATE()),
            'Demo User',
            1,
            @CompanyId1,
            @DivisionId1,
            'Q001',
            1,
            30,
            'Customer approved. Ready for project.',
            'Net 30 days',
            @DemoLightingTenantId,
            GETDATE()
        );

        -- Add quote line items
        INSERT INTO QuoteContents (Qid, Description, Quantity, UnitCost, NettPrice, ExtNettPrice, DateCreated, TenantId)
        VALUES
            (@QuoteId1, 'Solar Panel Installation - 5kW System', 5, 2500.00, 2500.00, 12500.00, GETDATE(), @DemoLightingTenantId),
            (@QuoteId1, 'Inverter & Battery Storage - 10kWh', 1, 3000.00, 3000.00, 3000.00, GETDATE(), @DemoLightingTenantId),
            (@QuoteId1, 'Installation Labour & Commissioning', 1, 2750.00, 2750.00, 2750.00, GETDATE(), @DemoLightingTenantId);

        PRINT 'Created sample quote DL-QUOTE-001';
    END
END

-- ══════════════════════════════════════════════════════════════════════════════
-- PART 6: Create Sample Invoices
-- ══════════════════════════════════════════════════════════════════════════════

IF @CompanyId2 IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM Invoices WHERE UPPER(Reference) LIKE '%DL-INV-001%' AND CompanyId = @CompanyId2)
    BEGIN
        DECLARE @InvoiceId1 INT;
        INSERT INTO Invoices (
            InvoiceNumber, Reference, CompanyName, InvoiceDate, DueDate, Amount, TaxAmount, TotalAmount,
            Status, CompanyId, TenantId, DateCreated, [Code]
        ) OUTPUT INSERTED.InvoiceId INTO @InvoiceId1
        VALUES (
            'INV-2026-001',
            'DL-INV-001',
            'Bright LED Solutions',
            DATEADD(DAY, -10, GETDATE()),
            DATEADD(DAY, 20, GETDATE()),
            45000.00,
            4500.00,
            49500.00,
            'Issued',
            @CompanyId2,
            @DemoLightingTenantId,
            GETDATE(),
            'INV001'
        );
        PRINT 'Created sample invoice DL-INV-001';
    END

    IF NOT EXISTS (SELECT 1 FROM Invoices WHERE UPPER(Reference) LIKE '%DL-INV-002%' AND CompanyId = @CompanyId2)
    BEGIN
        DECLARE @InvoiceId2 INT;
        INSERT INTO Invoices (
            InvoiceNumber, Reference, CompanyName, InvoiceDate, DueDate, Amount, TaxAmount, TotalAmount,
            Status, CompanyId, TenantId, DateCreated, [Code]
        ) OUTPUT INSERTED.InvoiceId INTO @InvoiceId2
        VALUES (
            'INV-2026-002',
            'DL-INV-002',
            'Bright LED Solutions',
            DATEADD(DAY, -5, GETDATE()),
            DATEADD(DAY, 25, GETDATE()),
            32500.00,
            3250.00,
            35750.00,
            'Issued',
            @CompanyId2,
            @DemoLightingTenantId,
            GETDATE(),
            'INV002'
        );
        PRINT 'Created sample invoice DL-INV-002';
    END
END

-- ══════════════════════════════════════════════════════════════════════════════
-- PART 7: Create Sample Purchase Orders
-- ══════════════════════════════════════════════════════════════════════════════

IF @CompanyId3 IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM PurchaseOrders WHERE UPPER(Reference) LIKE '%DL-PO-001%' AND SupplierName LIKE '%Smart Lighting%')
    BEGIN
        DECLARE @POId1 INT;
        INSERT INTO PurchaseOrders (
            PoNumber, Reference, SupplierName, PODate, RequiredBy, Amount, TaxAmount, TotalAmount,
            Status, CompanyId, TenantId, DateCreated
        ) OUTPUT INSERTED.PoId INTO @POId1
        VALUES (
            'PO-2026-001',
            'DL-PO-001',
            'Smart Lighting Co',
            GETDATE(),
            DATEADD(DAY, 14, GETDATE()),
            22500.00,
            2250.00,
            24750.00,
            'Pending',
            @CompanyId3,
            @DemoLightingTenantId,
            GETDATE()
        );

        -- Add PO line items
        INSERT INTO PurchaseOrderContents (PoId, Description, Quantity, UnitPrice, TotalPrice, DateCreated, TenantId)
        VALUES
            (@POId1, 'LED Downlights - 10W', 50, 35.00, 1750.00, GETDATE(), @DemoLightingTenantId),
            (@POId1, 'Smart Dimmer Switches', 25, 85.00, 2125.00, GETDATE(), @DemoLightingTenantId),
            (@POId1, 'Installation Wiring & Connectors', 1, 18625.00, 18625.00, GETDATE(), @DemoLightingTenantId);

        PRINT 'Created sample purchase order DL-PO-001';
    END
END

PRINT '✓ Demo Lighting tenant setup complete';
PRINT '  - Tenant: Demo Lighting (demo-lighting)';
PRINT '  - User: demo / demo123';
PRINT '  - Sample companies, divisions, quotes, invoices, and purchase orders created';
