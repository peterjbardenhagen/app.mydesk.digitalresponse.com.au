using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using MyDesk.Shared.Services;
using MyDesk.Shared.Models;

namespace MyDesk.Web.Services;

public class PlatformSettingsService
{
    private readonly PlatformSettings _settings;
    private readonly IConfiguration _config;
    
    public PlatformSettingsService(IConfiguration config, IHttpContextAccessor http, DatabaseService db)
    {
        _config = config;
        _settings = config.GetSection("PlatformSettings").Get<PlatformSettings>() ?? new PlatformSettings();
    }
    
    public PlatformSettings Current => _settings;
    
    public string GetBrandingName() => _settings?.BrandName ?? "MyDesk";
    
    public string GetSupportEmail() => _settings?.SupportEmail ?? "support@techlight.com.au";
    
    public string GetSalesEmail() => _settings?.SalesEmail ?? "info@techlight.com.au";
    
    public async Task SaveAsync(PlatformSettings? settings = null)
    {
        var settingsToSave = settings ?? _settings;
        _config.GetSection("PlatformSettings").Bind(settingsToSave);
        var json = System.Text.Json.JsonSerializer.Serialize(settingsToSave, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync("Config/platformsettings.json", json);
    }
    
    public void InvalidateCache()
    {
    }
}