using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace MyDesk.Web.Services;

/// <summary>
/// Singleton service that controls which side-nav items are visible.
/// Settings are persisted to navmenu.json in the content root.
/// Defaults all items to visible if the file does not exist.
/// </summary>
public class NavMenuService
{
    public record NavMenuItem(string Key, string Label, string Href, string Group);

    public static readonly IReadOnlyList<NavMenuItem> AllItems = new List<NavMenuItem>
    {
        new("dashboard",       "Dashboard",       "/",                "Overview"),
        new("ask-ai",          "Ask AI",           "/ask-ai",          "Overview"),
        new("quotes",          "Quotes",           "/quotes",          "Sales"),
        new("invoices",        "Invoices",         "/invoices",        "Sales"),
        new("despatch",        "Despatch",         "/despatch",        "Sales"),
        new("purchase-orders", "Purchase Orders",  "/purchase-orders", "Purchasing"),
        new("rfq",             "RFQ",              "/rfq",             "Purchasing"),
        new("contacts",        "Contacts",         "/contacts",        "CRM"),
        new("companies",       "Companies",        "/companies",       "CRM"),
        new("call-reports",    "Call Reports",     "/call-reports",    "CRM"),
        new("sales-projects",  "Sales Projects",   "/sales-projects",  "CRM"),
        new("job-orders",      "Job Orders",       "/job-orders",      "Operations"),
        new("products",        "Products",         "/products",        "Operations"),
        new("reports",         "Reports",          "/reports",         "Insights"),
        new("noticeboard",     "Noticeboard",      "/noticeboard",     "Insights"),
        new("help",            "Help",             "/help",            "Support"),
        new("release-notes",   "Release Notes",    "/release-notes",   "Support"),
    };

    private readonly string _filePath;
    private Dictionary<string, bool> _visibility;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public NavMenuService(IWebHostEnvironment env)
    {
        _filePath   = Path.Combine(env.ContentRootPath, "Config", "navmenu.json");
        _visibility = Load();
    }

    public bool IsVisible(string key) =>
        _visibility.TryGetValue(key, out var v) ? v : true;

    public Dictionary<string, bool> GetAll()
    {
        var result = new Dictionary<string, bool>();
        foreach (var item in AllItems)
            result[item.Key] = IsVisible(item.Key);
        return result;
    }

    public async Task SaveAsync(Dictionary<string, bool> updated)
    {
        await _lock.WaitAsync();
        try
        {
            _visibility = new Dictionary<string, bool>(updated);
            var json = JsonSerializer.Serialize(_visibility,
                new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_filePath, json);
        }
        finally { _lock.Release(); }
    }

    private Dictionary<string, bool> Load()
    {
        try
        {
            if (File.Exists(_filePath))
            {
                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<Dictionary<string, bool>>(json)
                       ?? new Dictionary<string, bool>();
            }
        }
        catch { }
        return new Dictionary<string, bool>();
    }
}
