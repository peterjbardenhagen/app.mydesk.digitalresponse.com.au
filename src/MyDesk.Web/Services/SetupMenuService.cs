using System.Text.Json;
using MudBlazor;
using static MudBlazor.Icons.Material.Rounded;

namespace MyDesk.Web.Services;

/// <summary>
/// Manages visibility of Setup page tiles, persisted to setupmenu.json.
/// Similar to NavMenuService but for the /admin/setup tile grid.
/// </summary>
public class SetupMenuService
{
    private readonly string _filePath;
    private readonly Dictionary<string, bool> _visibility = new();
    private List<SetupTile> _tiles = new();

    public SetupMenuService(IWebHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "Config", "setupmenu.json");
        LoadSettings();
    }

    public record SetupTile(
        string Key,
        string Label,
        string Description,
        string Route,
        string Icon,
        string Group,
        bool Visible);

    private void LoadSettings()
    {
        if (!File.Exists(_filePath))
        {
            _tiles = GetDefaultTiles();
            SaveSettings();
            return;
        }

        try
        {
            var json = File.ReadAllText(_filePath);
            var doc = JsonDocument.Parse(json);
            _tiles = doc.RootElement.GetProperty("tiles")
                .EnumerateArray()
                .Select(e => new SetupTile(
                    e.GetProperty("key").GetString()!,
                    e.GetProperty("label").GetString()!,
                    e.GetProperty("description").GetString()!,
                    e.GetProperty("route").GetString()!,
                    e.GetProperty("icon").GetString()!,
                    e.GetProperty("group").GetString()!,
                    e.GetProperty("visible").GetBoolean()))
                .ToList();

            foreach (var tile in _tiles)
                _visibility[tile.Key] = tile.Visible;
        }
        catch
        {
            _tiles = GetDefaultTiles();
        }
    }

    public void SaveSettings()
    {
        var json = JsonSerializer.Serialize(new { tiles = _tiles.Select(t => new
        {
            t.Key, t.Label, t.Description, t.Route, t.Icon, t.Group, t.Visible
        }) }, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_filePath, json);
    }

    public bool IsVisible(string key) => _visibility.GetValueOrDefault(key, true);

    public void SetVisible(string key, bool visible)
    {
        _visibility[key] = visible;
        var idx = _tiles.FindIndex(t => t.Key == key);
        if (idx >= 0)
            _tiles[idx] = _tiles[idx] with { Visible = visible };
    }

    public List<SetupTile> GetVisibleTiles() => _tiles.Where(t => t.Visible).ToList();

    public List<SetupTile> GetAllTiles() => _tiles;

    public List<string> GetGroups() => _tiles.Select(t => t.Group).Distinct().ToList();

    public List<SetupTile> GetTilesByGroup(string group) => 
        _tiles.Where(t => t.Group == group && t.Visible).ToList();

    private static List<SetupTile> GetDefaultTiles() => new()
    {
        new("users", "Users", "Manage user accounts, roles and passwords", "/admin/users", "People", "User administration", true),
        new("userRoles", "User Roles", "Define permission levels", "/admin/user-roles", "VerifiedUser", "User administration", true),
        new("divisions", "Divisions", "Business units", "/admin/divisions", "AccountTree", "Organisation", true),
        new("locations", "Locations", "Office locations", "/admin/locations", "LocationOn", "Organisation", true),
        new("quoteStatus", "Quote Status", "Pipeline stages", "/admin/quote-status", "RequestQuote", "Workflow status", true),
        new("invoiceStatus", "Invoice Status", "Lifecycle states", "/admin/invoice-status", "ReceiptLong", "Workflow status", true),
        new("poStatus", "PO Status", "Purchase order states", "/admin/po-status", "Inventory2", "Workflow status", true),
        new("jobOrderStatus", "Job Order Status", "Job order states", "/admin/job-order-status", "Work", "Workflow status", true),
        new("parameters", "Parameters", "Global settings", "/admin/parameters", "Tune", "System", true),
        new("navMenu", "Navigation Menu", "Side menu visibility", "/admin/nav-menu", "Menu", "System", true),
    };

    public static string IconToMudIcon(string icon) => icon switch
    {
        "People" => Icons.Material.Rounded.People,
        "VerifiedUser" => Icons.Material.Rounded.VerifiedUser,
        "AccountTree" => Icons.Material.Rounded.AccountTree,
        "LocationOn" => Icons.Material.Rounded.LocationOn,
        "Category" => Icons.Material.Rounded.Category,
        "QrCode" => Icons.Material.Rounded.QrCode,
        "CurrencyExchange" => Icons.Material.Rounded.CurrencyExchange,
        "CalendarMonth" => Icons.Material.Rounded.CalendarMonth,
        "Bolt" => Icons.Material.Rounded.Bolt,
        "RequestQuote" => Icons.Material.Rounded.RequestQuote,
        "ReceiptLong" => Icons.Material.Rounded.ReceiptLong,
        "Inventory2" => Icons.Material.Rounded.Inventory2,
        "Work" => Icons.Material.Rounded.Work,
        "Tune" => Icons.Material.Rounded.Tune,
        "Menu" => Icons.Material.Rounded.Menu,
        "Settings" => Icons.Material.Rounded.Settings,
        _ => Icons.Material.Rounded.Circle
    };

    public static string GroupToColor(string group) => group switch
    {
        "User administration" => "primary",
        "Organisation" => "info",
        "Reference data" => "accent",
        "Workflow status" => "warning",
        "System" => "success",
        _ => "info"
    };
}
