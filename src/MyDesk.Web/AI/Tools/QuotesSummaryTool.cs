using System.Text.Json;
using MyDesk.Shared.Services;

namespace MyDesk.Web.AI.Tools;

/// <summary>
/// Aggregates the current tenant's quotes by status / period and (optionally)
/// returns a chart spec the UI can render.
///
/// Status ids (from QuoteStatus lookup):
///   1 = Draft, 2 = Sent, 10 = Approved (Won), 11 = Declined (Lost)
/// </summary>
public class QuotesSummaryTool : IAiTool
{
    private readonly DatabaseService _db;
    private readonly ICurrentTenantAccessor _tenant;

    public QuotesSummaryTool(DatabaseService db, ICurrentTenantAccessor tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public string Name => "get_quotes_summary";

    public string Description =>
        "Aggregates the current tenant's quotes for a date range — counts and total value " +
        "broken down by status (draft, sent, won, lost). Optionally returns a chart spec " +
        "(bar or pie) ready to render. Use for questions like 'how many quotes did we win " +
        "this month?', 'show me quotes won vs lost this quarter', etc.";

    public JsonElement ParametersSchema => JsonDocument.Parse("""
    {
        "type": "object",
        "properties": {
            "period":     { "type": "string", "enum": ["this-week","this-month","last-month","this-quarter","this-year","ytd","custom"], "description": "The date range to summarise. Defaults to this-month." },
            "fromDate":   { "type": "string", "format": "date", "description": "Required if period == 'custom'." },
            "toDate":     { "type": "string", "format": "date", "description": "Required if period == 'custom'." },
            "chartType":  { "type": "string", "enum": ["none","bar","pie","donut"], "description": "Whether the UI should render a chart, and which kind. Default 'bar'." }
        }
    }
    """).RootElement;

    public async Task<AiToolResult> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var period = args.TryGetProperty("period", out var pEl) ? pEl.GetString() ?? "this-month" : "this-month";
        var chartType = args.TryGetProperty("chartType", out var cEl) ? cEl.GetString() ?? "bar" : "bar";
        var (from, to) = ResolveDateRange(period, args);

        // Single grouped query — total per status with count + nett price total.
        var dt = await _db.QueryAsync(@"
SELECT QuoteStatusId,
       COUNT(*) AS Cnt,
       ISNULL(SUM(NettPriceTotal), 0) AS Total
FROM Quotes
WHERE QuoteDate >= @From AND QuoteDate < @To
GROUP BY QuoteStatusId",
            new() { ["From"] = from, ["To"] = to });

        var byStatus = new Dictionary<int, (int Count, decimal Total)>();
        foreach (System.Data.DataRow r in dt.Rows)
        {
            var sid = r["QuoteStatusId"] == DBNull.Value ? 0 : Convert.ToInt32(r["QuoteStatusId"]);
            byStatus[sid] = (Convert.ToInt32(r["Cnt"]), Convert.ToDecimal(r["Total"]));
        }

        int CountFor(int id)   => byStatus.TryGetValue(id, out var v) ? v.Count : 0;
        decimal TotalFor(int id) => byStatus.TryGetValue(id, out var v) ? v.Total : 0m;

        // Map to friendly labels — these match the existing QuoteStatus FK values.
        var labels  = new[] { "Draft", "Sent", "Won", "Lost" };
        var counts  = new double[] { CountFor(1), CountFor(2), CountFor(10), CountFor(11) };
        var totals  = new double[] { (double)TotalFor(1), (double)TotalFor(2), (double)TotalFor(10), (double)TotalFor(11) };

        var summary = new
        {
            tenantId = _tenant.TenantId,
            period,
            from = from.ToString("yyyy-MM-dd"),
            to   = to.AddDays(-0).ToString("yyyy-MM-dd"),
            won_count   = (int)counts[2], won_value   = totals[2],
            lost_count  = (int)counts[3], lost_value  = totals[3],
            sent_count  = (int)counts[1], sent_value  = totals[1],
            draft_count = (int)counts[0], draft_value = totals[0],
            total_count = counts.Sum(),
            total_value = totals.Sum(),
            win_rate    = (counts[2] + counts[3]) > 0 ? Math.Round(counts[2] / (counts[2] + counts[3]) * 100, 1) : 0,
        };

        AiRenderable? chart = chartType is "bar" or "pie" or "donut"
            ? new AiChartSpec(
                Title: $"Quotes — {FriendlyPeriod(period, from, to)}",
                ChartType: chartType,
                Labels: labels,
                Series: new[]
                {
                    new AiChartSeries("Count", counts),
                    new AiChartSeries("Total $", totals),
                },
                XAxisLabel: "Status",
                YAxisLabel: "Count / $")
            : null;

        return new AiToolResult(
            JsonSerializer.Serialize(summary),
            chart);
    }

    private static (DateTime From, DateTime To) ResolveDateRange(string period, JsonElement args)
    {
        var today = DateTime.Today;
        return period switch
        {
            "this-week"     => (today.AddDays(-(int)today.DayOfWeek), today.AddDays(1)),
            "last-month"    => (new DateTime(today.AddMonths(-1).Year, today.AddMonths(-1).Month, 1),
                                new DateTime(today.Year, today.Month, 1)),
            "this-quarter"  => StartOfQuarter(today),
            "this-year"     => (new DateTime(today.Year, 1, 1), new DateTime(today.Year + 1, 1, 1)),
            "ytd"           => (new DateTime(today.Year, 1, 1), today.AddDays(1)),
            "custom"        => (
                args.TryGetProperty("fromDate", out var f) && DateTime.TryParse(f.GetString(), out var fd) ? fd : today.AddDays(-30),
                args.TryGetProperty("toDate",   out var t) && DateTime.TryParse(t.GetString(), out var td) ? td.AddDays(1) : today.AddDays(1)),
            _               => (new DateTime(today.Year, today.Month, 1), today.AddDays(1)),
        };
    }

    private static (DateTime, DateTime) StartOfQuarter(DateTime t)
    {
        var q = (t.Month - 1) / 3;
        var start = new DateTime(t.Year, q * 3 + 1, 1);
        return (start, start.AddMonths(3));
    }

    private static string FriendlyPeriod(string p, DateTime from, DateTime to) => p switch
    {
        "this-week"    => "this week",
        "this-month"   => from.ToString("MMMM yyyy"),
        "last-month"   => from.ToString("MMMM yyyy"),
        "this-quarter" => $"Q{((from.Month - 1) / 3) + 1} {from.Year}",
        "this-year"    => from.Year.ToString(),
        "ytd"          => $"YTD {from.Year}",
        _              => $"{from:yyyy-MM-dd} – {to.AddDays(-1):yyyy-MM-dd}"
    };
}
