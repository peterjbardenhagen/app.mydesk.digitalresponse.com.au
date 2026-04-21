using System.Text.Json;

namespace MyDesk.Web.Services;

/// <summary>
/// Per-user preferences (timezone, theme) persisted to userprefs.json keyed by user code.
/// </summary>
public class UserPreferencesService
{
    private readonly string _filePath;
    private Dictionary<string, UserPrefs> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public UserPreferencesService(IWebHostEnvironment env)
    {
        _filePath = Path.Combine(env.ContentRootPath, "userprefs.json");
        Load();
    }

    private void Load()
    {
        lock (_lock)
        {
            if (!File.Exists(_filePath)) { _cache = new(); return; }
            try
            {
                var json = File.ReadAllText(_filePath);
                _cache = JsonSerializer.Deserialize<Dictionary<string, UserPrefs>>(json)
                         ?? new(StringComparer.OrdinalIgnoreCase);
            }
            catch { _cache = new(); }
        }
    }

    private void Save()
    {
        lock (_lock)
        {
            var json = JsonSerializer.Serialize(_cache, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }

    public UserPrefs Get(string userCode)
    {
        if (string.IsNullOrEmpty(userCode)) return new UserPrefs();
        return _cache.TryGetValue(userCode, out var p)
            ? p
            : new UserPrefs();
    }

    public void Set(string userCode, UserPrefs prefs)
    {
        if (string.IsNullOrEmpty(userCode)) return;
        lock (_lock) { _cache[userCode] = prefs; }
        Save();
    }

    /// <summary>Common Australian / world timezones for the picker.</summary>
    public static readonly (string Id, string Label)[] CommonTimezones = new[]
    {
        ("AUS Eastern Standard Time",     "Brisbane / Sydney / Melbourne (AEST)"),
        ("E. Australia Standard Time",    "Brisbane (no DST)"),
        ("AUS Central Standard Time",     "Adelaide / Darwin (ACST)"),
        ("W. Australia Standard Time",    "Perth (AWST)"),
        ("Tasmania Standard Time",        "Hobart"),
        ("New Zealand Standard Time",     "Auckland (NZST)"),
        ("Singapore Standard Time",       "Singapore"),
        ("China Standard Time",           "Beijing / Shanghai"),
        ("Tokyo Standard Time",           "Tokyo"),
        ("GMT Standard Time",             "London (GMT/BST)"),
        ("Eastern Standard Time",         "New York (EST/EDT)"),
        ("Pacific Standard Time",         "Los Angeles (PST/PDT)"),
        ("UTC",                            "UTC"),
    };
}

public class UserPrefs
{
    public string Timezone { get; set; } = "E. Australia Standard Time"; // Brisbane default
    public string Theme    { get; set; } = "Light";
    public string Location { get; set; } = "Brisbane";
    public string DateFormat { get; set; } = "dd/MM/yyyy";
}
