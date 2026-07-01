using Microsoft.AspNetCore.Http;
using MyDesk.Shared.Services;
using MyDesk.Shared.Services.Integrations;
using MyDesk.Shared.Models;
using System.Text.Json;

namespace MyDesk.Web.Services;

/// <summary>
/// Tenant-scoped PlatformSettings.
///
/// Source of truth is now the <c>PlatformSettingsEntities</c> table (one row per tenant).
/// The legacy <c>PlatformSettings</c> section in appsettings.json is no longer used as a
/// fallback — code defaults (from <see cref="PlatformSettings"/>) apply if no row is found.
///
/// Resolution order on construction:
///   1) If the request is authenticated → load by tenant claim (ICurrentTenantAccessor).
///   2) Else if the request host matches a row in TenantHostnames → load by that tenant.
///   3) Else → bare PlatformSettings defaults.
///
/// Use <see cref="LoadForHostAsync"/> from anonymous pages (Login) to refresh the
/// branding once the host is known.
/// </summary>
public class PlatformSettingsService : IAccountingSettingsService
{
    private readonly DatabaseService _db;
    private readonly TenantService _tenantSvc;
    private readonly IHttpContextAccessor _http;
    private readonly ICurrentTenantAccessor _tenantAccessor;

    private PlatformSettings _settings;
    private Guid? _resolvedTenantId;

    public PlatformSettingsService(
        IHttpContextAccessor http,
        DatabaseService db,
        TenantService tenantSvc,
        ICurrentTenantAccessor tenantAccessor)
    {
        _http = http;
        _db = db;
        _tenantSvc = tenantSvc;
        _tenantAccessor = tenantAccessor;

        _settings = ResolveInitial();
    }

    public PlatformSettings Current => _settings;
    public Guid? ResolvedTenantId => _resolvedTenantId;

    public string GetBrandingName() => _settings?.BrandName ?? "MyDesk";
    public string GetSupportEmail() => _settings?.SupportEmail ?? "support@techlight.com.au";
    public string GetSalesEmail() => _settings?.SalesEmail ?? "info@techlight.com.au";

    /// <summary>
    /// Save the current (or supplied) settings to the resolved tenant's row.
    /// </summary>
    public async Task SaveAsync(PlatformSettings? settings = null)
    {
        var settingsToSave = settings ?? _settings;
        var tenantId = _tenantAccessor.TenantId ?? _resolvedTenantId;
        if (tenantId is null) return;

        var json = JsonSerializer.Serialize(settingsToSave, new JsonSerializerOptions { WriteIndented = true });
        await _db.ExecuteNonQueryAsync(@"
IF EXISTS (SELECT 1 FROM PlatformSettingsEntities WHERE TenantId = @TenantId)
    UPDATE PlatformSettingsEntities SET SettingsJson = @SettingsJson, UpdatedAt = GETDATE(), UpdatedBy = @UpdatedBy WHERE TenantId = @TenantId;
ELSE
    INSERT INTO PlatformSettingsEntities (TenantId, SettingsJson, UpdatedAt, UpdatedBy) VALUES (@TenantId, @SettingsJson, GETDATE(), @UpdatedBy);",
            new()
            {
                ["TenantId"] = tenantId,
                ["SettingsJson"] = json,
                ["UpdatedBy"] = _tenantAccessor.UserCode ?? "system"
            });

        _settings = settingsToSave;
    }

    public void InvalidateCache() { /* no-op — settings are loaded per scope */ }

    /// <summary>
    /// Re-resolve settings using the supplied host (typically <c>HttpContext.Request.Host</c>).
    /// Called by the Login page so the correct tenant branding is shown before sign-in.
    /// </summary>
    public async Task LoadForHostAsync(string? host)
    {
        if (string.IsNullOrWhiteSpace(host)) return;
        var tenant = await _tenantSvc.GetTenantByHostAsync(host);
        if (tenant is null) return;

        _resolvedTenantId = tenant.TenantId;
        var json = await _tenantSvc.GetPlatformSettingsJsonAsync(tenant.TenantId);
        if (!string.IsNullOrWhiteSpace(json))
        {
            try { _settings = JsonSerializer.Deserialize<PlatformSettings>(json) ?? _settings; }
            catch { /* keep defaults */ }
        }
    }

    /// <summary>
    /// Get login page color scheme from Tenants table columns
    /// </summary>
    public async Task<LoginColorScheme> GetLoginColorSchemeAsync(Guid? tenantId = null)
    {
        var tid = tenantId ?? _tenantAccessor.TenantId ?? _resolvedTenantId;
        if (tid is null) return new LoginColorScheme();
        
        var dt = await _db.QueryAsync(@"
            SELECT LoginPrimaryColor, LoginAccentColor, LoginBackgroundColor, 
                   LoginHeading, LoginSubheading, LoginCopyrightText, LoginSupportEmail, LoginSupportPhone
            FROM Tenants WHERE TenantId = @Tid",
            new() { ["Tid"] = tid });
            
        if (dt.Rows.Count == 0) return new LoginColorScheme();
        var row = dt.Rows[0];
        return new LoginColorScheme
        {
            PrimaryColor = row["LoginPrimaryColor"]?.ToString() ?? "#1e40af",
            AccentColor = row["LoginAccentColor"]?.ToString() ?? "#0ea5e9",
            BackgroundColor = row["LoginBackgroundColor"]?.ToString() ?? "#0a0a0a",
            Heading = row["LoginHeading"]?.ToString() ?? "Welcome to MyDesk",
            Subheading = row["LoginSubheading"]?.ToString() ?? "Sign in to access your dashboard",
            CopyrightText = row["LoginCopyrightText"]?.ToString() ?? "Digital Response. All rights reserved.",
            SupportEmail = row["LoginSupportEmail"]?.ToString() ?? "support@digitalresponse.com.au",
            SupportPhone = row["LoginSupportPhone"]?.ToString() ?? ""
        };
    }

    public class LoginColorScheme
    {
        public string PrimaryColor { get; set; } = "#1e40af";
        public string AccentColor { get; set; } = "#0ea5e9";
        public string BackgroundColor { get; set; } = "#0a0a0a";
        public string Heading { get; set; } = "Welcome to MyDesk";
        public string Subheading { get; set; } = "Sign in to access your dashboard";
        public string CopyrightText { get; set; } = "Digital Response. All rights reserved.";
        public string SupportEmail { get; set; } = "support@digitalresponse.com.au";
        public string SupportPhone { get; set; } = "";
    }

    private PlatformSettings ResolveInitial()
    {
        // 1) Authenticated user's tenant claim.
        if (_tenantAccessor.TenantId is { } authTenantId)
        {
            _resolvedTenantId = authTenantId;
            return LoadFromDb(authTenantId) ?? new PlatformSettings();
        }

        // 2) Host-based lookup (synchronous best-effort).
        var host = _http.HttpContext?.Request?.Host.Host;
        if (!string.IsNullOrWhiteSpace(host))
        {
            try
            {
                var dt = _db.Query(
                    @"SELECT TOP 1 t.TenantId
                      FROM Tenants t
                      INNER JOIN TenantHostnames h ON h.TenantId = t.TenantId
                      WHERE LOWER(h.Hostname) = @Host AND t.IsActive = 1",
                    new() { ["Host"] = host.ToLowerInvariant() });
                if (dt.Rows.Count > 0 && Guid.TryParse(dt.Rows[0][0]?.ToString(), out var g))
                {
                    _resolvedTenantId = g;
                    return LoadFromDb(g) ?? new PlatformSettings();
                }
            }
            catch { /* table may not exist yet on first run */ }
        }

        return new PlatformSettings();
    }

    private PlatformSettings? LoadFromDb(Guid tenantId)
    {
        try
        {
            var dt = _db.Query(
                "SELECT TOP 1 SettingsJson FROM PlatformSettingsEntities WHERE TenantId = @TenantId",
                new() { ["TenantId"] = tenantId });
            if (dt.Rows.Count == 0) return null;
            var json = dt.Rows[0]["SettingsJson"]?.ToString();
            if (string.IsNullOrWhiteSpace(json)) return null;
            return JsonSerializer.Deserialize<PlatformSettings>(json);
        }
        catch
        {
            return null;
        }
    }
}
