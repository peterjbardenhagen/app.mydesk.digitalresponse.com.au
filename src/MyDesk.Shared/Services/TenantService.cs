using System.Data;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class TenantService
{
    private readonly DatabaseService _db;
    private readonly ILogger<TenantService> _logger;

    public TenantService(DatabaseService db, ILogger<TenantService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EnsureTablesAsync()
    {
        await MigrateTenantIdTypeAsync();
        await AddMissingColumnsAsync();
        await EnsureTenantHostnamesTableAsync();

        var sql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tenants')
            BEGIN
                CREATE TABLE Tenants (
                    TenantId       UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY,
                    Name           NVARCHAR(200) NOT NULL,
                    Slug           NVARCHAR(100) NOT NULL UNIQUE,
                    Subdomain      NVARCHAR(100) NULL,
                    ContactEmail   NVARCHAR(200) NULL,
                    ContactPhone   NVARCHAR(50) NULL,
                    AddressLine1   NVARCHAR(200) NULL,
                    Suburb         NVARCHAR(100) NULL,
                    State          NVARCHAR(50) NULL,
                    PostCode       NVARCHAR(20) NULL,
                    Country        NVARCHAR(100) NOT NULL DEFAULT 'Australia',
                    ABN            NVARCHAR(20) NULL,
                    SubscriptionPlan NVARCHAR(50) NOT NULL DEFAULT 'Foundation',
                    MaxUsers       INT NOT NULL DEFAULT 10,
                    StorageLimitMB INT NOT NULL DEFAULT 1024,
                    IsTrial        BIT NOT NULL DEFAULT 0,
                    TrialExpiresAt DATETIME NULL,
                    IsActive       BIT NOT NULL DEFAULT 1,
                    IsSuspended    BIT NOT NULL DEFAULT 0,
                    SuspendedAt    DATETIME NULL,
                    SuspendedReason NVARCHAR(500) NULL,
                    CreatedAt      DATETIME NOT NULL DEFAULT GETDATE(),
                    UpdatedAt      DATETIME NOT NULL DEFAULT GETDATE()
                );
                CREATE UNIQUE INDEX IX_Tenants_Slug ON Tenants(Slug);
            END

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserTenants')
            BEGIN
                CREATE TABLE UserTenants (
                    UserTenantId INT IDENTITY(1,1) PRIMARY KEY,
                    UserId       INT NOT NULL,
                    TenantId     UNIQUEIDENTIFIER NOT NULL,
                    Role         NVARCHAR(50) NOT NULL DEFAULT 'User',
                    IsDefault    BIT NOT NULL DEFAULT 0,
                    IsActive     BIT NOT NULL DEFAULT 1,
                    InvitedBy    NVARCHAR(100) NULL,
                    InvitedAt    DATETIME NULL,
                    AcceptedAt   DATETIME NULL,
                    CreatedAt    DATETIME NOT NULL DEFAULT GETDATE()
                );
                CREATE INDEX IX_UserTenants_UserId ON UserTenants(UserId);
            END

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'PlatformSettingsEntities')
            BEGIN
                CREATE TABLE PlatformSettingsEntities (
                    TenantId     UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
                    SettingsJson NVARCHAR(MAX) NOT NULL DEFAULT '{}',
                    UpdatedAt    DATETIME NOT NULL DEFAULT GETDATE(),
                    UpdatedBy    NVARCHAR(100) NULL
                );
            END

            IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PlatformSettingsEntities_TenantId')
            BEGIN TRY
                ALTER TABLE PlatformSettingsEntities ADD CONSTRAINT FK_PlatformSettingsEntities_TenantId FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId);
            END TRY
            BEGIN CATCH
            END CATCH";
        await _db.ExecuteNonQueryAsync(sql);

        await SeedTenantsAsync();
        await SeedPlatformSettingsAsync();
        await EnsureUserTenantAssignmentsAsync();
    }

    private async Task MigrateTenantIdTypeAsync()
    {
        try
        {
            var colType = await _db.ScalarAsync<string?>(
                @"SELECT DATA_TYPE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'TenantId'");
            if (colType?.ToLowerInvariant() == "int")
            {
                _logger.LogInformation("Migrating Tenants.TenantId from INT to UNIQUEIDENTIFIER...");

                await _db.ExecuteNonQueryAsync(@"
                    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_PlatformSettingsEntities_TenantId')
                        ALTER TABLE PlatformSettingsEntities DROP CONSTRAINT FK_PlatformSettingsEntities_TenantId;

                    IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'UserTenants' AND COLUMN_NAME = 'TenantId')
                        ALTER TABLE UserTenants DROP COLUMN TenantId;

                    IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Tenants_Slug')
                        DROP INDEX IX_Tenants_Slug ON Tenants;

                    DECLARE @pkName NVARCHAR(200);
                    SELECT @pkName = name FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID('Tenants') AND type = 'PK';
                    IF @pkName IS NOT NULL EXEC('ALTER TABLE Tenants DROP CONSTRAINT ' + @pkName);

                    ALTER TABLE Tenants DROP COLUMN TenantId;
                    ALTER TABLE Tenants ADD TenantId UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID() PRIMARY KEY;
                    CREATE UNIQUE INDEX IX_Tenants_Slug ON Tenants(Slug);

                    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'UserTenants')
                    BEGIN
                        ALTER TABLE UserTenants ADD TenantId UNIQUEIDENTIFIER NOT NULL DEFAULT '11111111-1111-1111-1111-111111111111';
                        CREATE INDEX IX_UserTenants_UserId ON UserTenants(UserId);
                    END

                    IF EXISTS (SELECT * FROM sys.tables WHERE name = 'PlatformSettingsEntities')
                    BEGIN
                        DECLARE @psPk NVARCHAR(200);
                        SELECT @psPk = name FROM sys.key_constraints WHERE parent_object_id = OBJECT_ID('PlatformSettingsEntities') AND type = 'PK';
                        IF @psPk IS NOT NULL EXEC('ALTER TABLE PlatformSettingsEntities DROP CONSTRAINT ' + @psPk);
                        ALTER TABLE PlatformSettingsEntities DROP COLUMN TenantId;
                        ALTER TABLE PlatformSettingsEntities ADD TenantId UNIQUEIDENTIFIER NOT NULL DEFAULT '11111111-1111-1111-1111-111111111111' PRIMARY KEY;
                    END
                ");

                _logger.LogInformation("Tenants.TenantId migration completed.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "TenantId type migration skipped or failed - will attempt normal table creation");
        }
    }

    private async Task AddMissingColumnsAsync()
    {
        var alterSql = @"
            IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Tenants')
            BEGIN
                -- Core text columns (re-check in case legacy schema is missing them)
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'Name')
                    ALTER TABLE Tenants ADD Name NVARCHAR(200) NOT NULL DEFAULT '';
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'Slug')
                    ALTER TABLE Tenants ADD Slug NVARCHAR(100) NOT NULL DEFAULT '';
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'Subdomain')
                    ALTER TABLE Tenants ADD Subdomain NVARCHAR(100) NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'ContactEmail')
                    ALTER TABLE Tenants ADD ContactEmail NVARCHAR(200) NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'ContactPhone')
                    ALTER TABLE Tenants ADD ContactPhone NVARCHAR(50) NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'AddressLine1')
                    ALTER TABLE Tenants ADD AddressLine1 NVARCHAR(200) NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'Suburb')
                    ALTER TABLE Tenants ADD Suburb NVARCHAR(100) NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'State')
                    ALTER TABLE Tenants ADD State NVARCHAR(50) NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'PostCode')
                    ALTER TABLE Tenants ADD PostCode NVARCHAR(20) NULL;

                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'Country')
                    ALTER TABLE Tenants ADD Country NVARCHAR(100) NOT NULL DEFAULT 'Australia';
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'MaxUsers')
                    ALTER TABLE Tenants ADD MaxUsers INT NOT NULL DEFAULT 10;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'StorageLimitMB')
                    ALTER TABLE Tenants ADD StorageLimitMB INT NOT NULL DEFAULT 1024;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'SubscriptionPlan')
                    ALTER TABLE Tenants ADD SubscriptionPlan NVARCHAR(50) NOT NULL DEFAULT 'Foundation';
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'ABN')
                    ALTER TABLE Tenants ADD ABN NVARCHAR(20) NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'IsTrial')
                    ALTER TABLE Tenants ADD IsTrial BIT NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'TrialExpiresAt')
                    ALTER TABLE Tenants ADD TrialExpiresAt DATETIME NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'IsActive')
                    ALTER TABLE Tenants ADD IsActive BIT NOT NULL DEFAULT 1;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'IsSuspended')
                    ALTER TABLE Tenants ADD IsSuspended BIT NOT NULL DEFAULT 0;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'SuspendedAt')
                    ALTER TABLE Tenants ADD SuspendedAt DATETIME NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'SuspendedReason')
                    ALTER TABLE Tenants ADD SuspendedReason NVARCHAR(500) NULL;
                IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Tenants' AND COLUMN_NAME = 'UpdatedAt')
                    ALTER TABLE Tenants ADD UpdatedAt DATETIME NOT NULL DEFAULT GETDATE();
            END";
        await _db.ExecuteNonQueryAsync(alterSql);
    }

    private async Task EnsureTenantHostnamesTableAsync()
    {
        var sql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TenantHostnames')
            BEGIN
                CREATE TABLE TenantHostnames (
                    TenantHostnameId INT IDENTITY(1,1) PRIMARY KEY,
                    TenantId         UNIQUEIDENTIFIER NOT NULL,
                    Hostname         NVARCHAR(255) NOT NULL,
                    IsPrimary        BIT NOT NULL DEFAULT 0,
                    CreatedAt        DATETIME NOT NULL DEFAULT GETDATE(),
                    CONSTRAINT FK_TenantHostnames_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId) ON DELETE CASCADE
                );
                CREATE UNIQUE INDEX UX_TenantHostnames_Hostname ON TenantHostnames(Hostname);
            END";
        await _db.ExecuteNonQueryAsync(sql);
    }

    private async Task SeedTenantsAsync()
    {
        await _db.ExecuteNonQueryAsync(@"
IF NOT EXISTS (SELECT 1 FROM Tenants WHERE TenantId = @TechlightTenantId)
BEGIN
    INSERT INTO Tenants (TenantId, TenantName, Name, Slug, ContactEmail, ContactPhone, SubscriptionPlan, MaxUsers, IsActive, Country)
    VALUES (@TechlightTenantId, 'Techlight', 'Techlight', 'techlight', 'support@techlight.com.au', '0418 736 454', 'Enterprise', 999, 1, 'Australia');
END
ELSE
BEGIN
    UPDATE Tenants
    SET TenantName = COALESCE(NULLIF(TenantName, ''), 'Techlight'),
        Name = COALESCE(NULLIF(Name, ''), 'Techlight'),
        Slug = COALESCE(NULLIF(Slug, ''), 'techlight'),
        ContactEmail = COALESCE(ContactEmail, 'support@techlight.com.au'),
        ContactPhone = COALESCE(ContactPhone, '0418 736 454')
    WHERE TenantId = @TechlightTenantId;
END

IF NOT EXISTS (SELECT 1 FROM Tenants WHERE TenantId = @DigitalResponseTenantId)
BEGIN
    INSERT INTO Tenants (TenantId, TenantName, Name, Slug, ContactEmail, AddressLine1, Suburb, State, PostCode, Country, ABN, SubscriptionPlan, MaxUsers, IsActive)
    VALUES (@DigitalResponseTenantId, 'Digital Response', 'Digital Response', 'digital-response', 'info@digitalresponse.com.au', '477 Boundary Street', 'Spring Hill', 'QLD', '4000', 'Australia', '91 071 383 401', 'Enterprise', 999, 1);
END
ELSE
BEGIN
    UPDATE Tenants
    SET TenantName = COALESCE(NULLIF(TenantName, ''), 'Digital Response'),
        Name = COALESCE(NULLIF(Name, ''), 'Digital Response'),
        Slug = COALESCE(NULLIF(Slug, ''), 'digital-response'),
        ContactEmail = COALESCE(NULLIF(ContactEmail, ''), 'info@digitalresponse.com.au'),
        AddressLine1 = COALESCE(NULLIF(AddressLine1, ''), '477 Boundary Street'),
        Suburb = COALESCE(NULLIF(Suburb, ''), 'Spring Hill'),
        State = COALESCE(NULLIF(State, ''), 'QLD'),
        PostCode = COALESCE(NULLIF(PostCode, ''), '4000'),
        ABN = COALESCE(NULLIF(ABN, ''), '91 071 383 401')
    WHERE TenantId = @DigitalResponseTenantId;
END

-- Demo MyDesk: isolated demo / Playwright-test tenant.
IF NOT EXISTS (SELECT 1 FROM Tenants WHERE TenantId = @DemoTenantId)
BEGIN
    INSERT INTO Tenants (TenantId, TenantName, Name, Slug, ContactEmail, ContactPhone, Country, SubscriptionPlan, MaxUsers, IsActive)
    VALUES (@DemoTenantId, 'Demo MyDesk', 'Demo MyDesk', 'demo', 'demo@bardenhagen.xyz', '0400 000 000', 'Australia', 'Enterprise', 999, 1);
END
ELSE
BEGIN
    UPDATE Tenants
    SET TenantName = COALESCE(NULLIF(TenantName, ''), 'Demo MyDesk'),
        Name = COALESCE(NULLIF(Name, ''), 'Demo MyDesk'),
        Slug = COALESCE(NULLIF(Slug, ''), 'demo'),
        ContactEmail = COALESCE(NULLIF(ContactEmail, ''), 'demo@bardenhagen.xyz')
    WHERE TenantId = @DemoTenantId;
END

-- Carter Capner Law: Brisbane law firm client.
IF NOT EXISTS (SELECT 1 FROM Tenants WHERE TenantId = @CarterCapnerTenantId)
BEGIN
    INSERT INTO Tenants (TenantId, TenantName, Name, Slug, ContactEmail, AddressLine1, Suburb, State, PostCode, Country, SubscriptionPlan, MaxUsers, IsActive)
    VALUES (@CarterCapnerTenantId, 'Carter Capner Law', 'Carter Capner Law', 'carter-capner-law', 'admin@cartercapner.com.au', 'Level 5, 231 George Street', 'Brisbane', 'QLD', '4000', 'Australia', 'Enterprise', 999, 1);
END
ELSE
BEGIN
    UPDATE Tenants
    SET TenantName = COALESCE(NULLIF(TenantName, ''), 'Carter Capner Law'),
        Name = COALESCE(NULLIF(Name, ''), 'Carter Capner Law'),
        Slug = COALESCE(NULLIF(Slug, ''), 'carter-capner-law'),
        ContactEmail = COALESCE(NULLIF(ContactEmail, ''), 'admin@cartercapner.com.au')
    WHERE TenantId = @CarterCapnerTenantId;
END",
            new()
            {
                ["TechlightTenantId"]    = TenantConstants.TechlightTenantId,
                ["DigitalResponseTenantId"] = TenantConstants.DigitalResponseTenantId,
                ["DemoTenantId"]         = TenantConstants.DemoTenantId,
                ["CarterCapnerTenantId"] = TenantConstants.CarterCapnerTenantId
            });

        await SeedTenantHostnamesAsync();
    }

    private async Task SeedTenantHostnamesAsync()
    {
        // Techlight:          techlight.digitalresponse.com.au + localhost (dev default)
        // Digital Response:   portal.digitalresponse.com.au + app.dr.mydesk.digitalresponse.com.au
        // Demo MyDesk:        demo.localhost + demo.mydesk.local + demo.digitalresponse.com.au
        // Carter Capner Law:  app.ccl.mydesk.digitalresponse.com.au
        var seedSql = @"
IF NOT EXISTS (SELECT 1 FROM TenantHostnames WHERE Hostname = @H1)
    INSERT INTO TenantHostnames (TenantId, Hostname, IsPrimary) VALUES (@TechlightTenantId, @H1, 1);

IF NOT EXISTS (SELECT 1 FROM TenantHostnames WHERE Hostname = @H2)
    INSERT INTO TenantHostnames (TenantId, Hostname, IsPrimary) VALUES (@TechlightTenantId, @H2, 0);

IF NOT EXISTS (SELECT 1 FROM TenantHostnames WHERE Hostname = @H3)
    INSERT INTO TenantHostnames (TenantId, Hostname, IsPrimary) VALUES (@DigitalResponseTenantId, @H3, 1);

IF NOT EXISTS (SELECT 1 FROM TenantHostnames WHERE Hostname = @H4)
    INSERT INTO TenantHostnames (TenantId, Hostname, IsPrimary) VALUES (@DemoTenantId, @H4, 1);

IF NOT EXISTS (SELECT 1 FROM TenantHostnames WHERE Hostname = @H5)
    INSERT INTO TenantHostnames (TenantId, Hostname, IsPrimary) VALUES (@DemoTenantId, @H5, 0);

IF NOT EXISTS (SELECT 1 FROM TenantHostnames WHERE Hostname = @H6)
    INSERT INTO TenantHostnames (TenantId, Hostname, IsPrimary) VALUES (@DemoTenantId, @H6, 0);

IF NOT EXISTS (SELECT 1 FROM TenantHostnames WHERE Hostname = @H7)
    INSERT INTO TenantHostnames (TenantId, Hostname, IsPrimary) VALUES (@DigitalResponseTenantId, @H7, 0);

IF NOT EXISTS (SELECT 1 FROM TenantHostnames WHERE Hostname = @H8)
    INSERT INTO TenantHostnames (TenantId, Hostname, IsPrimary) VALUES (@CarterCapnerTenantId, @H8, 1);";

        await _db.ExecuteNonQueryAsync(seedSql, new()
        {
            ["TechlightTenantId"]        = TenantConstants.TechlightTenantId,
            ["DigitalResponseTenantId"]  = TenantConstants.DigitalResponseTenantId,
            ["DemoTenantId"]             = TenantConstants.DemoTenantId,
            ["CarterCapnerTenantId"]     = TenantConstants.CarterCapnerTenantId,
            ["H1"] = "techlight.digitalresponse.com.au",
            ["H2"] = "localhost",
            ["H3"] = "portal.digitalresponse.com.au",
            ["H4"] = "demo.localhost",
            ["H5"] = "demo.mydesk.local",
            ["H6"] = "demo.digitalresponse.com.au",
            ["H7"] = "app.dr.mydesk.digitalresponse.com.au",
            ["H8"] = "app.ccl.mydesk.digitalresponse.com.au"
        });
    }

    /// <summary>
    /// Resolve a tenant by request hostname (case-insensitive, port stripped).
    /// Used by the Login page to show the correct branding before any user is authenticated.
    /// </summary>
    public async Task<Tenant?> GetTenantByHostAsync(string? host)
    {
        if (string.IsNullOrWhiteSpace(host)) return null;
        var clean = host.Split(':')[0].Trim().ToLowerInvariant();

        try
        {
            var sql = @"
                SELECT TOP 1 t.*
                FROM Tenants t
                INNER JOIN TenantHostnames h ON h.TenantId = t.TenantId
                WHERE LOWER(h.Hostname) = @Host AND t.IsActive = 1";
            var dt = await _db.QueryAsync(sql, new() { ["Host"] = clean });
            return dt.Map(MapTenant).FirstOrDefault();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Returns the tenant-scoped PlatformSettings JSON (or null) for a given tenant.
    /// </summary>
    public async Task<string?> GetPlatformSettingsJsonAsync(Guid tenantId)
    {
        try
        {
            return await _db.ScalarAsync<string?>(
                "SELECT TOP 1 SettingsJson FROM PlatformSettingsEntities WHERE TenantId = @TenantId",
                new() { ["TenantId"] = tenantId });
        }
        catch
        {
            return null;
        }
    }

    private async Task SeedPlatformSettingsAsync()
    {
        var techlightJson = "{" +
            "\"PlatformName\":\"MyDesk\"," +
            "\"BrandName\":\"Techlight\"," +
            "\"CompanyName\":\"Techlight\"," +
            "\"CompanyLegalName\":\"Techlight Pty Ltd\"," +
            "\"CompanyWebsite\":\"https://techlight.com.au\"," +
            "\"SupportEmail\":\"support@techlight.com.au\"," +
            "\"SalesEmail\":\"info@techlight.com.au\"," +
            "\"LoginLogoUrl\":\"/images/mydesk-logo-white-text.svg\"," +
            "\"LoginMarkUrl\":\"/images/techlight-mark.svg\"," +
            "\"LoginHeading\":\"Sign in to MyDesk\"," +
            "\"LoginPrimaryColor\":\"#00C8C8\"," +
            "\"LoginAccentColor\":\"#cca05a\"," +
            "\"LoginBackgroundColor\":\"#08121a\"," +
            "\"IsMultiTenant\":true" +
            "}";

        // Digital Response — operating name of iFusion Pty Ltd. ABN 91 071 383 401.
        // Brisbane-based digital agency, est 2008. 477 Boundary Street, Spring Hill QLD.
        // Source: https://www.digitalresponse.com.au
        var digitalResponseJson = "{" +
            "\"PlatformName\":\"MyDesk\"," +
            "\"BrandName\":\"Digital Response\"," +
            "\"CompanyName\":\"Digital Response\"," +
            "\"CompanyLegalName\":\"iFusion Pty Ltd\"," +
            "\"CompanyWebsite\":\"https://www.digitalresponse.com.au\"," +
            "\"PlatformTagline\":\"Practical digital solutions that actually work\"," +
            "\"SupportEmail\":\"info@digitalresponse.com.au\"," +
            "\"SalesEmail\":\"info@digitalresponse.com.au\"," +
            "\"ContactEmail\":\"info@digitalresponse.com.au\"," +
            "\"CompanyAddress\":\"477 Boundary Street, Spring Hill QLD, Australia\"," +
            "\"PdfAddress1\":\"477 Boundary Street\"," +
            "\"PdfSuburb\":\"Spring Hill\"," +
            "\"PdfState\":\"QLD\"," +
            "\"PrivacyPolicyUrl\":\"https://www.digitalresponse.com.au/privacy-policy\"," +
            "\"TermsAndConditionsUrl\":\"https://www.digitalresponse.com.au/terms-and-conditions\"," +
            "\"CopyrightText\":\"\\u00A9 2026 Digital Response. iFusion Pty Ltd ABN 91 071 383 401.\"," +
            "\"LogoUrl\":\"/images/dr-logo-white.png\"," +
            "\"LoginLogoUrl\":\"/images/dr-logo-white.png\"," +
            "\"LoginMarkUrl\":\"/images/dr-mark.png\"," +
            "\"LoginHeading\":\"Sign in to MyDesk\"," +
            "\"LoginSubheading\":\"Digital Response client portal\"," +
            "\"LoginPrimaryColor\":\"#12261d\"," +
            "\"LoginAccentColor\":\"#3d7a32\"," +
            "\"LoginBackgroundColor\":\"#0a1510\"," +
            "\"IsMultiTenant\":true" +
            "}";

        // Demo MyDesk — bright, distinct branding so testers/demo viewers know they're not in production.
        var demoJson = "{" +
            "\"PlatformName\":\"MyDesk\"," +
            "\"BrandName\":\"Demo MyDesk\"," +
            "\"CompanyName\":\"Demo MyDesk\"," +
            "\"CompanyLegalName\":\"Demo Sandbox\"," +
            "\"CompanyWebsite\":\"https://demo.digitalresponse.com.au\"," +
            "\"PlatformTagline\":\"Demo / Test Sandbox \\u2014 emails redirect to peter@bardenhagen.xyz\"," +
            "\"SupportEmail\":\"peter@bardenhagen.xyz\"," +
            "\"SalesEmail\":\"peter@bardenhagen.xyz\"," +
            "\"ContactEmail\":\"peter@bardenhagen.xyz\"," +
            "\"LoginLogoUrl\":\"/images/mydesk-logo-white-text.svg\"," +
            "\"LoginMarkUrl\":\"/images/techlight-mark.svg\"," +
            "\"LoginHeading\":\"Sign in to Demo MyDesk\"," +
            "\"LoginSubheading\":\"Demo / sandbox tenant \\u2014 safe to test anything here.\"," +
            "\"LoginPrimaryColor\":\"#a855f7\"," +
            "\"LoginAccentColor\":\"#facc15\"," +
            "\"LoginBackgroundColor\":\"#1e1b4b\"," +
            "\"CopyrightText\":\"\\u00A9 2026 Demo MyDesk \\u2014 Not for production use.\"," +
            "\"IsMultiTenant\":true," +
            "\"DisableAllEmails\":false" +
            "}";

        // Carter Capner Law — Brisbane law firm. Brand: yellow #FFED00, blue #1C7BC4, black #1A1A1A.
        // Hero uses dark→blue gradient so white text remains readable; yellow shows in mark and logo accent.
        var carterCapnerJson = "{" +
            "\"PlatformName\":\"MyDesk\"," +
            "\"BrandName\":\"Carter Capner Law\"," +
            "\"CompanyName\":\"Carter Capner Law\"," +
            "\"CompanyLegalName\":\"Carter Capner Law\"," +
            "\"CompanyWebsite\":\"https://www.cartercapner.com.au\"," +
            "\"SupportEmail\":\"admin@cartercapner.com.au\"," +
            "\"SalesEmail\":\"admin@cartercapner.com.au\"," +
            "\"ContactEmail\":\"admin@cartercapner.com.au\"," +
            "\"CompanyAddress\":\"Level 5, 231 George Street, Brisbane QLD 4000\"," +
            "\"PdfAddress1\":\"Level 5, 231 George Street\"," +
            "\"PdfSuburb\":\"Brisbane\"," +
            "\"PdfState\":\"QLD\"," +
            "\"LoginLogoUrl\":\"/images/ccl-logo-white.svg\"," +
            "\"LoginMarkUrl\":\"/images/ccl-mark.svg\"," +
            "\"LoginHeading\":\"Carter Capner Law\"," +
            "\"LoginSubheading\":\"Staff & client portal\"," +
            "\"LoginPrimaryColor\":\"#1a1a1a\"," +
            "\"LoginAccentColor\":\"#1C7BC4\"," +
            "\"LoginBackgroundColor\":\"#0c1620\"," +
            "\"CopyrightText\":\"\\u00A9 2026 Carter Capner Law.\"," +
            "\"IsMultiTenant\":true" +
            "}";

        // Insert if missing; refresh if still flagged as 'system' (so we don't trample user edits via admin UI)
        await _db.ExecuteNonQueryAsync(@"
IF NOT EXISTS (SELECT 1 FROM PlatformSettingsEntities WHERE TenantId = @TechlightTenantId)
    INSERT INTO PlatformSettingsEntities (TenantId, SettingsJson, UpdatedAt, UpdatedBy) VALUES (@TechlightTenantId, @TechlightJson, GETDATE(), 'system');
ELSE IF EXISTS (SELECT 1 FROM PlatformSettingsEntities WHERE TenantId = @TechlightTenantId AND (UpdatedBy IS NULL OR UpdatedBy = 'system'))
    UPDATE PlatformSettingsEntities SET SettingsJson = @TechlightJson, UpdatedAt = GETDATE(), UpdatedBy = 'system' WHERE TenantId = @TechlightTenantId;

IF NOT EXISTS (SELECT 1 FROM PlatformSettingsEntities WHERE TenantId = @DigitalResponseTenantId)
    INSERT INTO PlatformSettingsEntities (TenantId, SettingsJson, UpdatedAt, UpdatedBy) VALUES (@DigitalResponseTenantId, @DigitalResponseJson, GETDATE(), 'system');
ELSE IF EXISTS (SELECT 1 FROM PlatformSettingsEntities WHERE TenantId = @DigitalResponseTenantId AND (UpdatedBy IS NULL OR UpdatedBy = 'system'))
    UPDATE PlatformSettingsEntities SET SettingsJson = @DigitalResponseJson, UpdatedAt = GETDATE(), UpdatedBy = 'system' WHERE TenantId = @DigitalResponseTenantId;

IF NOT EXISTS (SELECT 1 FROM PlatformSettingsEntities WHERE TenantId = @DemoTenantId)
    INSERT INTO PlatformSettingsEntities (TenantId, SettingsJson, UpdatedAt, UpdatedBy) VALUES (@DemoTenantId, @DemoJson, GETDATE(), 'system');
ELSE IF EXISTS (SELECT 1 FROM PlatformSettingsEntities WHERE TenantId = @DemoTenantId AND (UpdatedBy IS NULL OR UpdatedBy = 'system'))
    UPDATE PlatformSettingsEntities SET SettingsJson = @DemoJson, UpdatedAt = GETDATE(), UpdatedBy = 'system' WHERE TenantId = @DemoTenantId;

IF NOT EXISTS (SELECT 1 FROM PlatformSettingsEntities WHERE TenantId = @CarterCapnerTenantId)
    INSERT INTO PlatformSettingsEntities (TenantId, SettingsJson, UpdatedAt, UpdatedBy) VALUES (@CarterCapnerTenantId, @CarterCapnerJson, GETDATE(), 'system');
ELSE IF EXISTS (SELECT 1 FROM PlatformSettingsEntities WHERE TenantId = @CarterCapnerTenantId AND (UpdatedBy IS NULL OR UpdatedBy = 'system'))
    UPDATE PlatformSettingsEntities SET SettingsJson = @CarterCapnerJson, UpdatedAt = GETDATE(), UpdatedBy = 'system' WHERE TenantId = @CarterCapnerTenantId;",
            new()
            {
                ["TechlightTenantId"]        = TenantConstants.TechlightTenantId,
                ["DigitalResponseTenantId"]  = TenantConstants.DigitalResponseTenantId,
                ["DemoTenantId"]             = TenantConstants.DemoTenantId,
                ["CarterCapnerTenantId"]     = TenantConstants.CarterCapnerTenantId,
                ["TechlightJson"]            = techlightJson,
                ["DigitalResponseJson"]      = digitalResponseJson,
                ["DemoJson"]                 = demoJson,
                ["CarterCapnerJson"]         = carterCapnerJson
            });
    }

    public async Task EnsureUserTenantAssignmentsAsync()
    {
        const string sql = @"
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Users')
BEGIN
    INSERT INTO UserTenants (UserId, TenantId, Role, IsDefault, IsActive, AcceptedAt)
    SELECT u.UserId, @TechlightTenantId,
           CASE WHEN ISNULL(u.UserTypeId, 0) IN (1,2) THEN 'Admin' ELSE 'User' END,
           1, 1, GETDATE()
    FROM Users u
    WHERE ISNULL(u.Deleted,0) = 0
      AND NOT EXISTS (SELECT 1 FROM UserTenants ut WHERE ut.UserId = u.UserId AND ut.TenantId = @TechlightTenantId);

    -- Digital Response: any user whose email is @digitalresponse.com.au, any admin/director,
    -- and explicitly Peter Bardenhagen (Code TL0025) regardless of UserTypeId.
    INSERT INTO UserTenants (UserId, TenantId, Role, IsDefault, IsActive, AcceptedAt)
    SELECT u.UserId, @DigitalResponseTenantId,
           CASE
               WHEN ISNULL(u.UserTypeId, 0) IN (1,2) THEN 'Admin'
               WHEN UPPER(ISNULL(u.Code,'')) = 'TL0025' THEN 'Admin'
               ELSE 'User'
           END,
           0, 1, GETDATE()
    FROM Users u
    WHERE ISNULL(u.Deleted,0) = 0
      AND (LOWER(ISNULL(u.Email, '')) LIKE '%@digitalresponse.com.au'
           OR ISNULL(u.UserTypeId, 0) IN (1,2)
           OR UPPER(ISNULL(u.Code,'')) = 'TL0025')
      AND NOT EXISTS (SELECT 1 FROM UserTenants ut WHERE ut.UserId = u.UserId AND ut.TenantId = @DigitalResponseTenantId);

    -- Demo MyDesk: explicit access for Peter Bardenhagen (TL0025) + any director/admin so
    -- Playwright tests and demos can target an isolated tenant.
    INSERT INTO UserTenants (UserId, TenantId, Role, IsDefault, IsActive, AcceptedAt)
    SELECT u.UserId, @DemoTenantId,
           CASE
               WHEN ISNULL(u.UserTypeId, 0) IN (1,2) THEN 'Admin'
               WHEN UPPER(ISNULL(u.Code,'')) = 'TL0025' THEN 'Admin'
               ELSE 'User'
           END,
           0, 1, GETDATE()
    FROM Users u
    WHERE ISNULL(u.Deleted,0) = 0
      AND (UPPER(ISNULL(u.Code,'')) = 'TL0025' OR ISNULL(u.UserTypeId, 0) IN (1,2))
      AND NOT EXISTS (SELECT 1 FROM UserTenants ut WHERE ut.UserId = u.UserId AND ut.TenantId = @DemoTenantId);

    -- Carter Capner Law: admins and TL0025 get admin access; CCL staff are added manually via admin UI.
    INSERT INTO UserTenants (UserId, TenantId, Role, IsDefault, IsActive, AcceptedAt)
    SELECT u.UserId, @CarterCapnerTenantId,
           'Admin',
           0, 1, GETDATE()
    FROM Users u
    WHERE ISNULL(u.Deleted,0) = 0
      AND (UPPER(ISNULL(u.Code,'')) = 'TL0025' OR ISNULL(u.UserTypeId, 0) IN (1,2))
      AND NOT EXISTS (SELECT 1 FROM UserTenants ut WHERE ut.UserId = u.UserId AND ut.TenantId = @CarterCapnerTenantId);
END";

        await _db.ExecuteNonQueryAsync(sql, new()
        {
            ["TechlightTenantId"]        = TenantConstants.TechlightTenantId,
            ["DigitalResponseTenantId"]  = TenantConstants.DigitalResponseTenantId,
            ["DemoTenantId"]             = TenantConstants.DemoTenantId,
            ["CarterCapnerTenantId"]     = TenantConstants.CarterCapnerTenantId
        });
    }

    public async Task<Tenant?> GetTenantAsync(Guid tenantId)
    {
        var sql = "SELECT * FROM Tenants WHERE TenantId = @Id";
        var dt = await _db.QueryAsync(sql, new() { ["Id"] = tenantId });
        return dt.Map(MapTenant).FirstOrDefault();
    }

    public async Task<List<Tenant>> GetAllTenantsAsync()
    {
        var sql = "SELECT * FROM Tenants WHERE IsActive = 1 ORDER BY Name";
        var dt = await _db.QueryAsync(sql);
        return dt.Map(MapTenant);
    }

    public async Task<List<TenantMembership>> GetUserTenantsAsync(int userId)
    {
        var sql = @"
            SELECT ut.UserTenantId, ut.UserId, ut.TenantId, ut.IsDefault, ut.IsActive,
                   t.Name AS TenantName, t.Slug AS TenantSlug
            FROM UserTenants ut
            JOIN Tenants t ON ut.TenantId = t.TenantId
            WHERE ut.UserId = @UserId AND ut.IsActive = 1
            ORDER BY ut.IsDefault DESC, t.Name";
        var dt = await _db.QueryAsync(sql, new() { ["UserId"] = userId });
        return dt.Map(r => new TenantMembership
        {
            UserTenantId = Convert.ToInt32(r["UserTenantId"]),
            UserId = Convert.ToInt32(r["UserId"]),
            TenantId = Guid.Parse(r["TenantId"].ToString()!),
            TenantName = r["TenantName"]?.ToString() ?? "",
            TenantSlug = r["TenantSlug"]?.ToString() ?? "",
            IsDefault = r["IsDefault"] != DBNull.Value && Convert.ToBoolean(r["IsDefault"]),
            IsActive = r["IsActive"] != DBNull.Value && Convert.ToBoolean(r["IsActive"]),
        });
    }

    public async Task<Guid> CreateTenantAsync(Tenant tenant)
    {
        if (tenant.TenantId == Guid.Empty) tenant.TenantId = Guid.NewGuid();
        var sql = @"
            INSERT INTO Tenants (TenantId, Name, Slug, Subdomain, ContactEmail, SubscriptionPlan, MaxUsers, ABN)
            VALUES (@TenantId, @Name, @Slug, @Subdomain, @ContactEmail, @SubscriptionPlan, @MaxUsers, @ABN);";
        await _db.ExecuteNonQueryAsync(sql, new Dictionary<string, object?>
        {
            ["TenantId"] = tenant.TenantId,
            ["Name"] = tenant.Name,
            ["Slug"] = tenant.Slug,
            ["Subdomain"] = (object?)tenant.Subdomain ?? DBNull.Value,
            ["ContactEmail"] = (object?)tenant.ContactEmail ?? DBNull.Value,
            ["SubscriptionPlan"] = tenant.SubscriptionPlan,
            ["MaxUsers"] = tenant.MaxUsers,
            ["ABN"] = (object?)tenant.ABN ?? DBNull.Value,
        });
        return tenant.TenantId;
    }

    public async Task UpdateTenantAsync(Tenant tenant)
    {
        var sql = @"
            UPDATE Tenants SET
                Name = @Name, Slug = @Slug, ContactEmail = @ContactEmail,
                SubscriptionPlan = @SubscriptionPlan, MaxUsers = @MaxUsers,
                IsActive = @IsActive, UpdatedAt = GETDATE()
            WHERE TenantId = @TenantId";
        await _db.ExecuteNonQueryAsync(sql, new Dictionary<string, object?>
        {
            ["TenantId"] = tenant.TenantId,
            ["Name"] = tenant.Name,
            ["Slug"] = tenant.Slug,
            ["ContactEmail"] = (object?)tenant.ContactEmail ?? DBNull.Value,
            ["SubscriptionPlan"] = tenant.SubscriptionPlan,
            ["MaxUsers"] = tenant.MaxUsers,
            ["IsActive"] = tenant.IsActive,
        });
    }

    public async Task AssignUserToTenantAsync(int userId, Guid tenantId, string role = "User", bool isDefault = false)
    {
        var sql = @"
            IF NOT EXISTS (SELECT 1 FROM UserTenants WHERE UserId = @UserId AND TenantId = @TenantId)
            BEGIN
                INSERT INTO UserTenants (UserId, TenantId, Role, IsDefault, AcceptedAt)
                VALUES (@UserId, @TenantId, @Role, @IsDefault, GETDATE());
            END";
        await _db.ExecuteNonQueryAsync(sql, new Dictionary<string, object?>
        {
            ["UserId"] = userId,
            ["TenantId"] = tenantId,
            ["Role"] = role,
            ["IsDefault"] = isDefault,
        });
    }

    public async Task SetDefaultTenantAsync(int userId, Guid tenantId)
    {
        await _db.ExecuteNonQueryAsync(@"
            UPDATE UserTenants SET IsDefault = 0 WHERE UserId = @UserId;
            UPDATE UserTenants SET IsDefault = 1 WHERE UserId = @UserId AND TenantId = @TenantId;",
            new Dictionary<string, object?> { ["UserId"] = userId, ["TenantId"] = tenantId });
    }

    private static Tenant MapTenant(DataRow r) => new()
    {
        TenantId = Guid.Parse(r["TenantId"].ToString()!),
        Name = r["Name"]?.ToString() ?? "",
        Slug = r["Slug"]?.ToString() ?? "",
        Subdomain = r["Subdomain"]?.ToString(),
        ContactEmail = r["ContactEmail"]?.ToString(),
        SubscriptionPlan = r["SubscriptionPlan"]?.ToString() ?? "Foundation",
        MaxUsers = r["MaxUsers"] != DBNull.Value ? Convert.ToInt32(r["MaxUsers"]) : 10,
        IsActive = r["IsActive"] != DBNull.Value && Convert.ToBoolean(r["IsActive"]),
        IsSuspended = r["IsSuspended"] != DBNull.Value && Convert.ToBoolean(r["IsSuspended"]),
        CreatedAt = r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]) : DateTime.UtcNow,
    };
}
