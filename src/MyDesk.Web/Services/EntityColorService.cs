using Microsoft.Extensions.Logging;
using MudBlazor;
using static MudBlazor.Icons.Material.Rounded;

namespace MyDesk.Web.Services;

public class EntityColorService
{
    private readonly Dictionary<string, EntityStyle> _styles = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<EntityColorService> _logger;
    private readonly IWebHostEnvironment _env;

    public EntityColorService(IWebHostEnvironment env, ILogger<EntityColorService> logger)
    {
        _env = env;
        _logger = logger;

        var path = Path.Combine(env.ContentRootPath, "Config", "entitycolors.json");
        if (!File.Exists(path)) { SeedDefaults(); return; }

        try
        {
            var json = File.ReadAllText(path);
            var doc = JsonDocument.Parse(json);
            foreach (var e in doc.RootElement.GetProperty("entities").EnumerateObject())
            {
                _styles[e.Name] = new EntityStyle(
                    Color:   e.Value.GetProperty("color").GetString()!,
                    BgColor: e.Value.GetProperty("bgColor").GetString()!,
                    Icon:    e.Value.GetProperty("icon").GetString()!,
                    Label:   e.Value.GetProperty("label").GetString()!);
            }
        }
        catch { SeedDefaults(); }
    }

    private void SeedDefaults()
    {
        _styles["Quote"]         = new("#00a8b0", "#e0f7f8", "RequestQuote",  "Quote");
        _styles["Invoice"]       = new("#22c55e", "#e8f7ee", "ReceiptLong",   "Invoice");
        _styles["PurchaseOrder"] = new("#f59e0b", "#fef3dd", "Inventory2",    "Purchase Order");
        _styles["Contact"]       = new("#6941c6", "#efeafc", "Contacts",      "Contact");
        _styles["Company"]       = new("#0369a1", "#e0f2fe", "Business",      "Company");
        _styles["Despatch"]      = new("#2e90fa", "#e4f1ff", "LocalShipping", "Delivery Note");
        _styles["JobOrder"]      = new("#cca05a", "#fbf3e4", "Work",          "Job Order");
        _styles["Product"]       = new("#ea580c", "#ffeee2", "Category",      "Product");
    }

    public EntityStyle Get(string entityType) =>
        _styles.TryGetValue(entityType, out var s)
            ? s
            : new EntityStyle("#6b7280", "#f3f4f6", "Circle", entityType);

    public IReadOnlyDictionary<string, EntityStyle> All => _styles;

    public string IconToMudIcon(string icon) => icon switch
    {
        "RequestQuote"  => Icons.Material.Rounded.RequestQuote,
        "ReceiptLong"   => Icons.Material.Rounded.ReceiptLong,
        "Inventory2"    => Icons.Material.Rounded.Inventory2,
        "Contacts"      => Icons.Material.Rounded.Contacts,
        "Business"      => Icons.Material.Rounded.Business,
        "LocalShipping" => Icons.Material.Rounded.LocalShipping,
        "Work"          => Icons.Material.Rounded.Work,
        "Category"      => Icons.Material.Rounded.Category,
        _               => Icons.Material.Rounded.Circle
    };
}

public record EntityStyle(string Color, string BgColor, string Icon, string Label);
