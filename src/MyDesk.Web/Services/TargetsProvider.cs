using System.Text.Json;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Services;

/// <summary>
/// Loads performance targets from Config/targets.json (hot-reload each request in dev).
/// </summary>
public class TargetsProvider : ITargetsProvider
{
    private readonly string _path;
    private TargetsFile _cache = new();
    private DateTime _loadedAt = DateTime.MinValue;
    private readonly object _lock = new();

    public TargetsProvider(IWebHostEnvironment env)
    {
        _path = Path.Combine(env.ContentRootPath, "Config", "targets.json");
        Reload();
    }

    public decimal CompanyMonthlyTarget    { get { EnsureFresh(); return _cache.CompanyMonthlyTarget; } }
    public decimal CompanyQuarterlyTarget  { get { EnsureFresh(); return _cache.CompanyQuarterlyTarget; } }
    public decimal CompanyYearlyTarget     { get { EnsureFresh(); return _cache.CompanyYearlyTarget; } }

    public decimal DefaultUserMonthlyTarget   { get { EnsureFresh(); return _cache.DefaultUserMonthlyTarget; } }
    public decimal DefaultUserQuarterlyTarget { get { EnsureFresh(); return _cache.DefaultUserQuarterlyTarget; } }
    public decimal DefaultUserYearlyTarget    { get { EnsureFresh(); return _cache.DefaultUserYearlyTarget; } }

    public decimal GetUserMonthlyTarget(string userCode)
    {
        EnsureFresh();
        return _cache.UserTargets.TryGetValue(userCode, out var t) && t.Monthly > 0
            ? t.Monthly : _cache.DefaultUserMonthlyTarget;
    }

    public decimal GetUserQuarterlyTarget(string userCode)
    {
        EnsureFresh();
        return _cache.UserTargets.TryGetValue(userCode, out var t) && t.Quarterly > 0
            ? t.Quarterly : _cache.DefaultUserQuarterlyTarget;
    }

    public decimal GetUserYearlyTarget(string userCode)
    {
        EnsureFresh();
        return _cache.UserTargets.TryGetValue(userCode, out var t) && t.Yearly > 0
            ? t.Yearly : _cache.DefaultUserYearlyTarget;
    }

    private void EnsureFresh()
    {
        if ((DateTime.Now - _loadedAt).TotalSeconds > 30) Reload();
    }

    private void Reload()
    {
        lock (_lock)
        {
            try
            {
                if (File.Exists(_path))
                {
                    var json = File.ReadAllText(_path);
                    var parsed = JsonSerializer.Deserialize<TargetsFile>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (parsed != null) _cache = parsed;
                }
            }
            catch { /* use existing cache if parse fails */ }
            _loadedAt = DateTime.Now;
        }
    }

    private class TargetsFile
    {
        public decimal CompanyMonthlyTarget   { get; set; } = 500_000;
        public decimal CompanyQuarterlyTarget { get; set; } = 1_500_000;
        public decimal CompanyYearlyTarget    { get; set; } = 6_000_000;
        public decimal DefaultUserMonthlyTarget   { get; set; } = 50_000;
        public decimal DefaultUserQuarterlyTarget { get; set; } = 150_000;
        public decimal DefaultUserYearlyTarget    { get; set; } = 600_000;
        public Dictionary<string, UserTargetEntry> UserTargets { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }

    private class UserTargetEntry
    {
        public decimal Monthly   { get; set; }
        public decimal Quarterly { get; set; }
        public decimal Yearly    { get; set; }
    }
}
