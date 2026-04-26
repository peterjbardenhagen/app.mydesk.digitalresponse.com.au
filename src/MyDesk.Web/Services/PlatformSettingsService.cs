using MyDesk.Shared.Models;

namespace MyDesk.Web.Services;

/// <summary>
/// Service to manage platform-wide and tenant-specific settings
/// </summary>
public class PlatformSettingsService
{
    private readonly IConfiguration _configuration;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private PlatformSettings? _cachedSettings;
    private TenantSettings? _cachedTenantSettings;

    public PlatformSettings Current => GetSettings();
    public TenantSettings? CurrentTenant => GetTenantSettings();

    public PlatformSettingsService(IConfiguration configuration, IHttpContextAccessor httpContextAccessor)
    {
        _configuration = configuration;
        _httpContextAccessor = httpContextAccessor;
    }

    public PlatformSettings GetSettings()
    {
        if (_cachedSettings != null) return _cachedSettings;

        var settings = new PlatformSettings();
        
        // Load from configuration
        _configuration.GetSection("PlatformSettings").Bind(settings);
        
        // Apply tenant-specific overrides if available
        var tenantSettings = GetTenantSettings();
        if (tenantSettings != null)
        {
            ApplyTenantOverrides(settings, tenantSettings);
        }
        
        // Apply founding customer (Techlight) specific settings if on that domain
        ApplyDomainSpecificSettings(settings);

        _cachedSettings = settings;
        return settings;
    }

    public TenantSettings? GetTenantSettings()
    {
        if (_cachedTenantSettings != null) return _cachedTenantSettings;

        // In a multi-tenant setup, this would look up based on:
        // 1. Subdomain (tenant1.mydesk.digitalresponse.com.au)
        // 2. Custom domain mapping
        // 3. Header or query parameter
        // 4. JWT token claim
        
        var host = _httpContextAccessor.HttpContext?.Request.Host.Host;
        
        // Special case: Techlight (founding customer)
        if (host?.Contains("techlight") == true)
        {
            _cachedTenantSettings = new TenantSettings
            {
                TenantId = 1,
                TenantName = "Techlight",
                TenantSubdomain = "techlight",
                CustomCompanyName = "Techlight",
                EnableAIAssistant = true,
                EnableAskAI = true,
                EnableMYOBIntegration = true
            };
            return _cachedTenantSettings;
        }

        // For now, return null (use platform defaults)
        // In production, this would query the database
        _cachedTenantSettings = null;
        return _cachedTenantSettings;
    }

    public bool IsTechlightTenant()
    {
        var host = _httpContextAccessor.HttpContext?.Request.Host.Host;
        return host?.Contains("techlight") == true;
    }

    public string GetBrandingName()
    {
        var tenant = GetTenantSettings();
        if (!string.IsNullOrEmpty(tenant?.CustomCompanyName))
        {
            return tenant.CustomCompanyName;
        }
        return Current.PlatformName;
    }

    private void ApplyTenantOverrides(PlatformSettings settings, TenantSettings tenant)
    {
        if (!string.IsNullOrEmpty(tenant.CustomLogoUrl))
            settings.LogoUrl = tenant.CustomLogoUrl;
        
        if (!string.IsNullOrEmpty(tenant.CustomCompanyName))
        {
            settings.CompanyName = tenant.CustomCompanyName;
            settings.CopyrightText = $"Copyright {DateTime.Now.Year} {tenant.CustomCompanyName}. All rights reserved.";
        }

        if (tenant.EnableAIAssistant.HasValue)
            settings.EnableAIAssistant = tenant.EnableAIAssistant.Value;
        
        if (tenant.EnableAskAI.HasValue)
            settings.EnableAskAI = tenant.EnableAskAI.Value;
        
        if (tenant.EnableMYOBIntegration.HasValue)
            settings.EnableMYOBIntegration = tenant.EnableMYOBIntegration.Value;
    }

    private void ApplyDomainSpecificSettings(PlatformSettings settings)
    {
        var host = _httpContextAccessor.HttpContext?.Request.Host.Host ?? "";
        
        // Techlight specific branding
        if (host.Contains("techlight"))
        {
            settings.CompanyName = "Techlight";
            settings.LogoUrl = "/images/techlight-logo.svg";
            settings.CopyrightText = $"Copyright {DateTime.Now.Year} Techlight. Powered by MyDesk.";
        }
    }

    public void InvalidateCache()
    {
        _cachedSettings = null;
        _cachedTenantSettings = null;
    }

    public Task SaveAsync()
    {
        // In a real application, this would save the settings to the database
        // For now, we rely on the in-memory changes applied to the Current object.
        return Task.CompletedTask;
    }
}
