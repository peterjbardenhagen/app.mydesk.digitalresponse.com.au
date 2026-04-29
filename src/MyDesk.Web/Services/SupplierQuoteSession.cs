using System.Collections.Concurrent;

namespace MyDesk.Web.Services;

public static class SupplierQuoteSession
{
    private static readonly ConcurrentDictionary<string, List<SupplierQuoteLine>> _cache = new();

    public static string Store(List<SupplierQuoteLine> lines)
    {
        var key = Guid.NewGuid().ToString("N");
        _cache[key] = lines;
        return key;
    }

    public static List<SupplierQuoteLine>? Get(string key)
    {
        _cache.TryRemove(key, out var lines);
        return lines;
    }
}