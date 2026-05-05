using System.Collections.Concurrent;

namespace MyDesk.Web.Services;

/// <summary>
/// Short-lived cross-page hand-off bag for parsed supplier-quote data so the
/// "Copy Supplier Quote" page can ship parsed lines and header info to the
/// new-quote page without round-tripping the entire payload through the URL.
/// </summary>
public static class SupplierQuoteSession
{
    public class Bag
    {
        public List<SupplierQuoteLine> Lines { get; set; } = new();
        public SupplierQuoteHeader Header { get; set; } = new();
    }

    private static readonly ConcurrentDictionary<string, Bag> _cache = new();

    public static string Store(List<SupplierQuoteLine> lines, SupplierQuoteHeader? header = null)
    {
        var key = Guid.NewGuid().ToString("N");
        _cache[key] = new Bag { Lines = lines, Header = header ?? new SupplierQuoteHeader() };
        return key;
    }

    /// <summary>Returns the bag and removes it from the cache (single-use hand-off).</summary>
    public static Bag? Take(string key)
    {
        _cache.TryRemove(key, out var bag);
        return bag;
    }

    /// <summary>Backwards-compatible accessor that returns just the lines.</summary>
    public static List<SupplierQuoteLine>? Get(string key) => Take(key)?.Lines;
}
