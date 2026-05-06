using System.Data;
using System.Text.Json;
using MyDesk.Shared.Services;

namespace MyDesk.Web.AI.Tools;

/// <summary>
/// Returns the best customers by various metrics — highest revenue, most profitable,
/// or least effort (fewest support interactions). Supports sorting by revenue, profit,
/// or effort and returns an optional bar chart for visual comparison.
/// </summary>
public class BestCustomersTool : IAiTool
{
    private readonly DatabaseService _db;
    private readonly ICurrentTenantAccessor _tenant;

    public BestCustomersTool(DatabaseService db, ICurrentTenantAccessor tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public string Name => "get_best_customers";

    public string Description =>
        "Returns the top customers ranked by a specific metric. " +
        "sortBy='revenue' = highest invoice revenue; " +
        "sortBy='profit' = highest gross profit (sell price minus cost from quote line items); " +
        "sortBy='effort' = customers requiring least effort (fewest call reports / follow-ups, fewest open quotes). " +
        "Returns top N customers with their metric value and optional bar chart.";

    public JsonElement ParametersSchema => JsonDocument.Parse("""
    {
        "type": "object",
        "properties": {
            "sortBy":    { "type": "string", "enum": ["revenue","profit","effort"], "description": "Ranking metric. Default 'revenue'." },
            "period":    { "type": "string", "enum": ["this-week","this-month","this-quarter","this-year","ytd","last-12-months"], "description": "Date range. Default 'this-year'." },
            "topN":      { "type": "integer", "minimum": 1, "maximum": 50, "description": "Number of top customers to return. Default 10." },
            "chartType": { "type": "string", "enum": ["none","bar","horizontal-bar"], "description": "Chart type. Default 'horizontal-bar'." }
        }
    }
    """).RootElement;

    public async Task<AiToolResult> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var sortBy = args.TryGetProperty("sortBy", out var sEl) ? sEl.GetString() ?? "revenue" : "revenue";
        var period = args.TryGetProperty("period", out var pEl) ? pEl.GetString() ?? "this-year" : "this-year";
        var topN = args.TryGetProperty("topN", out var tEl) ? tEl.GetInt32() : 10;
        var chartType = args.TryGetProperty("chartType", out var cEl) ? cEl.GetString() ?? "horizontal-bar" : "horizontal-bar";

        var (from, to) = ResolveDateRange(period);
        var fromStr = from.ToString("yyyy-MM-dd");
        var toStr = to.AddDays(-1).ToString("yyyy-MM-dd");

        try
        {
            var result = sortBy.ToLower() switch
            {
                "revenue" => await GetBestByRevenueAsync(from, to, topN),
                "profit"  => await GetBestByProfitAsync(from, to, topN),
                "effort"  => await GetBestByEffortAsync(from, to, topN),
                _         => await GetBestByRevenueAsync(from, to, topN)
            };

            AiRenderable? chart = chartType is "bar" or "horizontal-bar"
                ? new AiChartSpec(
                    Title: $"Best customers by {sortBy} ({FriendlyPeriod(period)})",
                    ChartType: chartType,
                    Labels: result.Labels,
                    Series: new[] { new AiChartSeries(FriendlyMetric(sortBy), result.Values) },
                    XAxisLabel: "Customer",
                    YAxisLabel: sortBy == "effort" ? "Effort Score (lower = better)" : "$ AUD")
                : null;

            var summary = new
            {
                metric = sortBy,
                period,
                from = fromStr,
                to = toStr,
                topN,
                customers = result.Customers,
                totalMetricValue = result.Values.Sum()
            };

            return new AiToolResult(JsonSerializer.Serialize(summary), chart);
        }
        catch (Exception ex)
        {
            return new AiToolResult(JsonSerializer.Serialize(new { error = ex.Message }));
        }
    }

    private async Task<CustomerResult> GetBestByRevenueAsync(DateTime from, DateTime to, int topN)
    {
        var dt = await _db.QueryAsync(@"
SELECT TOP (@n) c.Company,
       ISNULL(SUM(i.NettPriceTotal), 0) AS TotalRevenue,
       COUNT(i.InvoiceId) AS InvoiceCount
FROM Companies c
INNER JOIN Invoices i ON c.CompanyId = i.CompanyId
LEFT JOIN InvoiceStatus s ON i.InvoiceStatusId = s.InvoiceStatusId
WHERE i.InvoiceDate >= @From AND i.InvoiceDate < @To
  AND ISNULL(s.InvoiceStatus, '') NOT IN ('Cancelled', 'Draft')
GROUP BY c.Company
ORDER BY TotalRevenue DESC",
            new() { ["n"] = topN, ["From"] = from, ["To"] = to });

        return BuildResult(dt, "TotalRevenue", "Company");
    }

    private async Task<CustomerResult> GetBestByProfitAsync(DateTime from, DateTime to, int topN)
    {
        var dt = await _db.QueryAsync(@"
SELECT TOP (@n) c.Company,
       ISNULL(SUM(ql.NetSellPrice - ql.NetCostPrice), 0) AS TotalProfit,
       COUNT(DISTINCT q.Qid) AS QuoteCount,
       ISNULL(SUM(ql.NetSellPrice), 0) AS TotalSell,
       ISNULL(SUM(ql.NetCostPrice), 0) AS TotalCost,
       CASE WHEN ISNULL(SUM(ql.NetSellPrice), 0) > 0
            THEN ROUND((SUM(ql.NetSellPrice) - SUM(ql.NetCostPrice)) / SUM(ql.NetSellPrice) * 100, 1)
            ELSE 0 END AS MarginPercent
FROM Companies c
INNER JOIN Quotes q ON c.CompanyId = q.CompanyId
INNER JOIN QuoteLineItems ql ON q.Qid = ql.Qid
WHERE q.QuoteDate >= @From AND q.QuoteDate < @To
  AND q.QuoteStatusId IN (4)
GROUP BY c.Company
ORDER BY TotalProfit DESC",
            new() { ["n"] = topN, ["From"] = from, ["To"] = to });

        return BuildResult(dt, "TotalProfit", "Company");
    }

    private async Task<CustomerResult> GetBestByEffortAsync(DateTime from, DateTime to, int topN)
    {
        var dt = await _db.QueryAsync(@"
SELECT TOP (@n) c.Company,
       (ISNULL(cr.CallCount, 0) * 2 + ISNULL(q.OpenQuoteCount, 0) + ISNULL(i.OpenInvoiceCount, 0)) AS EffortScore,
       ISNULL(cr.CallCount, 0) AS FollowUps,
       ISNULL(q.OpenQuoteCount, 0) AS OpenQuotes,
       ISNULL(i.OpenInvoiceCount, 0) AS OpenInvoices
FROM Companies c
LEFT JOIN (
    SELECT CompanyId, COUNT(*) AS CallCount
    FROM CallReports
    WHERE FollowUpDate >= @From AND FollowUpDate < @To
    GROUP BY CompanyId
) cr ON c.CompanyId = cr.CompanyId
LEFT JOIN (
    SELECT CompanyId, COUNT(*) AS OpenQuoteCount
    FROM Quotes
    WHERE QuoteDate >= @From AND QuoteDate < @To
      AND QuoteStatusId NOT IN (4, 5, 9, 10)
    GROUP BY CompanyId
) q ON c.CompanyId = q.CompanyId
LEFT JOIN (
    SELECT CompanyId, COUNT(*) AS OpenInvoiceCount
    FROM Invoices i
    LEFT JOIN InvoiceStatus s ON i.InvoiceStatusId = s.InvoiceStatusId
    WHERE i.InvoiceDate >= @From AND i.InvoiceDate < @To
      AND ISNULL(s.InvoiceStatus, '') NOT IN ('Paid', 'Cancelled')
    GROUP BY CompanyId
) i ON c.CompanyId = i.CompanyId
WHERE cr.CallCount IS NOT NULL OR q.OpenQuoteCount IS NOT NULL OR i.OpenInvoiceCount IS NOT NULL
ORDER BY EffortScore ASC",
            new() { ["n"] = topN, ["From"] = from, ["To"] = to });

        return BuildResult(dt, "EffortScore", "Company");
    }

    private static CustomerResult BuildResult(DataTable dt, string valueColumn, string nameColumn)
    {
        var labels = new List<string>();
        var values = new List<double>();
        var customers = new List<object>();

        foreach (DataRow row in dt.Rows)
        {
            var name = row[nameColumn]?.ToString() ?? "";
            var val = row[valueColumn] != DBNull.Value ? Convert.ToDouble(row[valueColumn]) : 0;
            labels.Add(name);
            values.Add(val);
            var cust = new Dictionary<string, object?> { { "name", name }, { "value", val } };
            foreach (DataColumn col in dt.Columns)
            {
                if (col.ColumnName != nameColumn && col.ColumnName != valueColumn)
                    cust[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
            }
            customers.Add(cust);
        }

        return new CustomerResult { Labels = labels.ToArray(), Values = values.ToArray(), Customers = customers };
    }

    private static (DateTime From, DateTime To) ResolveDateRange(string period)
    {
        var today = DateTime.Today;
        return period switch
        {
            "this-week" => (today.AddDays(-(int)today.DayOfWeek), today.AddDays(1)),
            "this-month" => (new DateTime(today.Year, today.Month, 1), today.AddDays(1)),
            "this-quarter" => StartOfQuarter(today),
            "this-year" => (new DateTime(today.Year, 1, 1), new DateTime(today.Year + 1, 1, 1)),
            "ytd" => (new DateTime(today.Year, 1, 1), today.AddDays(1)),
            "last-12-months" => (today.AddMonths(-12), today.AddDays(1)),
            _ => (new DateTime(today.Year, 1, 1), new DateTime(today.Year + 1, 1, 1))
        };
    }

    private static (DateTime, DateTime) StartOfQuarter(DateTime t)
    {
        var q = (t.Month - 1) / 3;
        var start = new DateTime(t.Year, q * 3 + 1, 1);
        return (start, start.AddMonths(3));
    }

    private static string FriendlyPeriod(string p) => p switch
    {
        "this-week" => "this week",
        "this-month" => "this month",
        "this-quarter" => $"Q{((DateTime.Today.Month - 1) / 3) + 1} {DateTime.Today.Year}",
        "this-year" => DateTime.Today.Year.ToString(),
        "ytd" => $"YTD {DateTime.Today.Year}",
        "last-12-months" => "last 12 months",
        _ => p
    };

    private static string FriendlyMetric(string sortBy) => sortBy switch
    {
        "revenue" => "Revenue ($)",
        "profit" => "Gross Profit ($)",
        "effort" => "Effort Score",
        _ => sortBy
    };

    private class CustomerResult
    {
        public string[] Labels { get; set; } = Array.Empty<string>();
        public double[] Values { get; set; } = Array.Empty<double>();
        public List<object> Customers { get; set; } = new();
    }
}
