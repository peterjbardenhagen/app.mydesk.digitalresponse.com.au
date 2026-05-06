using System.Text.Json;
using MyDesk.Shared.Services;

namespace MyDesk.Web.AI.Tools;

/// <summary>
/// Open pipeline (quotes still in play): count, weighted value, and ageing buckets.
/// Useful for "what's in the pipeline?" / "what's about to expire?" questions.
/// </summary>
public class PipelineSummaryTool : IAiTool
{
    private readonly DatabaseService _db;

    public PipelineSummaryTool(DatabaseService db) { _db = db; }

    public string Name => "get_pipeline_summary";

    public string Description =>
        "Returns the current tenant's open quote pipeline: total count, total value, " +
        "and ageing breakdown (0-7, 8-14, 15-30, 30+ days since QuoteDate). Includes a " +
        "donut chart spec for the ageing buckets.";

    public JsonElement ParametersSchema => JsonDocument.Parse("""
    {
        "type": "object",
        "properties": {
            "chartType": { "type": "string", "enum": ["none","donut","pie","bar"], "description": "Default 'donut'." }
        }
    }
    """).RootElement;

    public async Task<AiToolResult> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var chartType = args.TryGetProperty("chartType", out var c) ? c.GetString() ?? "donut" : "donut";

        // Status 1,2,3,6,7,8 are "open"; 4=Accepted, 5/9/10=Declined/Rejected are closed.
        var dt = await _db.QueryAsync(@"
SELECT
    SUM(CASE WHEN DATEDIFF(DAY, QuoteDate, GETDATE()) BETWEEN 0  AND 7  THEN 1 ELSE 0 END) AS B07,
    SUM(CASE WHEN DATEDIFF(DAY, QuoteDate, GETDATE()) BETWEEN 8  AND 14 THEN 1 ELSE 0 END) AS B814,
    SUM(CASE WHEN DATEDIFF(DAY, QuoteDate, GETDATE()) BETWEEN 15 AND 30 THEN 1 ELSE 0 END) AS B1530,
    SUM(CASE WHEN DATEDIFF(DAY, QuoteDate, GETDATE())          > 30     THEN 1 ELSE 0 END) AS B30P,
    COUNT(*)                                  AS Cnt,
    ISNULL(SUM(NettPriceTotal), 0)            AS Total
FROM Quotes
WHERE QuoteStatusId IN (1, 2, 3, 6, 7, 8);");

        var row = dt.Rows.Count > 0 ? dt.Rows[0] : null;
        int b07   = row != null && row["B07"]   != DBNull.Value ? Convert.ToInt32(row["B07"])   : 0;
        int b814  = row != null && row["B814"]  != DBNull.Value ? Convert.ToInt32(row["B814"])  : 0;
        int b1530 = row != null && row["B1530"] != DBNull.Value ? Convert.ToInt32(row["B1530"]) : 0;
        int b30p  = row != null && row["B30P"]  != DBNull.Value ? Convert.ToInt32(row["B30P"])  : 0;
        int cnt   = row != null ? Convert.ToInt32(row["Cnt"])   : 0;
        decimal tot = row != null ? Convert.ToDecimal(row["Total"]) : 0m;

        var summary = new
        {
            open_quote_count = cnt,
            open_quote_value = tot,
            ageing_buckets = new
            {
                days_0_7   = b07,
                days_8_14  = b814,
                days_15_30 = b1530,
                days_30_plus = b30p,
            }
        };

        AiRenderable? chart = chartType is "donut" or "pie" or "bar"
            ? new AiChartSpec(
                Title: "Open pipeline — ageing",
                ChartType: chartType,
                Labels: new[] { "0–7 days", "8–14 days", "15–30 days", "30+ days" },
                Series: new[] { new AiChartSeries("Open quotes", new double[] { b07, b814, b1530, b30p }) })
            : null;

        return new AiToolResult(JsonSerializer.Serialize(summary), chart);
    }
}
