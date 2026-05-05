using System.Text.Json;
using MyDesk.Shared.Services;

namespace MyDesk.Web.AI.Tools;

/// <summary>
/// Aggregates invoiced revenue by month / period for the current tenant. Returns
/// an optional line/bar chart spec so questions like "show our monthly revenue
/// trend this year" can be answered visually.
/// </summary>
public class InvoicesSummaryTool : IAiTool
{
    private readonly DatabaseService _db;

    public InvoicesSummaryTool(DatabaseService db) { _db = db; }

    public string Name => "get_invoices_summary";

    public string Description =>
        "Aggregates the current tenant's invoices by month within a date range. Returns " +
        "totals (nett, GST, gross), invoice count, and an optional line/bar chart of monthly " +
        "revenue. Use for revenue / cashflow questions.";

    public JsonElement ParametersSchema => JsonDocument.Parse("""
    {
        "type": "object",
        "properties": {
            "months":    { "type": "integer", "minimum": 1, "maximum": 36, "description": "Trailing months to include (default 12)." },
            "chartType": { "type": "string",  "enum": ["none","bar","line"], "description": "UI chart type. Default 'line'." }
        }
    }
    """).RootElement;

    public async Task<AiToolResult> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var months = args.TryGetProperty("months", out var m) ? m.GetInt32() : 12;
        var chartType = args.TryGetProperty("chartType", out var c) ? c.GetString() ?? "line" : "line";
        var to = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(1);
        var from = to.AddMonths(-months);

        var dt = await _db.QueryAsync(@"
SELECT YEAR(InvoiceDate) AS Y, MONTH(InvoiceDate) AS M,
       COUNT(*) AS Cnt,
       ISNULL(SUM(NettPriceTotal), 0) AS Nett,
       ISNULL(SUM(GSTTotal), 0)        AS GST
FROM Invoices
WHERE InvoiceDate >= @From AND InvoiceDate < @To
GROUP BY YEAR(InvoiceDate), MONTH(InvoiceDate)
ORDER BY Y, M",
            new() { ["From"] = from, ["To"] = to });

        // Pad missing months so the chart has a continuous x axis.
        var labels = new List<string>();
        var counts = new List<double>();
        var nettVals = new List<double>();
        var grossVals = new List<double>();
        for (int i = 0; i < months; i++)
        {
            var dtm = from.AddMonths(i);
            labels.Add(dtm.ToString("MMM yyyy"));
            var row = dt.Rows.Cast<System.Data.DataRow>()
                .FirstOrDefault(r => Convert.ToInt32(r["Y"]) == dtm.Year && Convert.ToInt32(r["M"]) == dtm.Month);
            if (row != null)
            {
                counts.Add(Convert.ToDouble(row["Cnt"]));
                var nett = Convert.ToDouble(row["Nett"]);
                var gst  = Convert.ToDouble(row["GST"]);
                nettVals.Add(nett);
                grossVals.Add(nett + gst);
            }
            else
            {
                counts.Add(0); nettVals.Add(0); grossVals.Add(0);
            }
        }

        var summary = new
        {
            from = from.ToString("yyyy-MM-dd"),
            to   = to.AddDays(-1).ToString("yyyy-MM-dd"),
            months,
            total_invoices = (int)counts.Sum(),
            total_nett     = nettVals.Sum(),
            total_gross    = grossVals.Sum(),
            monthly = labels.Zip(nettVals, (l, v) => new { month = l, nett = v }).ToArray()
        };

        AiRenderable? chart = chartType is "bar" or "line"
            ? new AiChartSpec(
                Title: $"Invoiced revenue — last {months} months",
                ChartType: chartType,
                Labels: labels.ToArray(),
                Series: new[]
                {
                    new AiChartSeries("Nett", nettVals.ToArray()),
                    new AiChartSeries("Gross", grossVals.ToArray()),
                },
                XAxisLabel: "Month",
                YAxisLabel: "$ AUD")
            : null;

        return new AiToolResult(JsonSerializer.Serialize(summary), chart);
    }
}
