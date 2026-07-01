using System.Text.Json;
using MyDesk.Shared.Services;

namespace MyDesk.Web.AI.Tools;

/// <summary>
/// Projects a 30 / 60 / 90-day cash position using outstanding invoices
/// (money coming in), purchase orders (money going out), and won-quote pipeline.
/// </summary>
public class CashFlowForecastTool : IAiTool
{
    private readonly DatabaseService _db;
    private readonly ICurrentTenantAccessor _tenant;

    public CashFlowForecastTool(DatabaseService db, ICurrentTenantAccessor tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    public string Name => "get_cash_flow_forecast";

    public string Description =>
        "Projects the tenant's 30, 60, and 90-day cash position by combining outstanding " +
        "receivables (unpaid invoices due soon), payables (approved POs not yet billed), " +
        "and won-quote pipeline. Returns a bar chart of projected cash in/out per period. " +
        "Use for questions like 'what's our cash flow looking like?', 'how much are we owed?', " +
        "'what do we owe suppliers this quarter?'.";

    public JsonElement ParametersSchema => JsonDocument.Parse("""
    {
        "type": "object",
        "properties": {
            "chartType": { "type": "string", "enum": ["bar", "line", "none"], "description": "Chart type to display. Default 'bar'." }
        }
    }
    """).RootElement;

    public async Task<AiToolResult> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        var chartType = args.TryGetProperty("chartType", out var cEl) ? cEl.GetString() ?? "bar" : "bar";
        var today = DateTime.Today;

        // ── Receivables: unpaid invoices ─────────────────────────────────────
        var recDt = await _db.QueryAsync(@"
SELECT
    CASE
        WHEN ISNULL(i.DueDate, DATEADD(day,30,i.InvoiceDate)) <= DATEADD(day,30,@Today) THEN '0-30'
        WHEN ISNULL(i.DueDate, DATEADD(day,30,i.InvoiceDate)) <= DATEADD(day,60,@Today) THEN '31-60'
        ELSE '61-90'
    END AS Band,
    COUNT(*)        AS InvoiceCount,
    ISNULL(SUM(i.NettPriceTotal + ISNULL(i.GSTTotal,0)), 0) AS TotalOwed
FROM Invoices i
WHERE i.InvoiceStatusId NOT IN (3, 6)   -- exclude Paid / Cancelled
  AND ISNULL(i.DueDate, DATEADD(day,30,i.InvoiceDate)) <= DATEADD(day,90,@Today)
  AND ISNULL(i.DueDate, DATEADD(day,30,i.InvoiceDate)) >= @Today
GROUP BY
    CASE
        WHEN ISNULL(i.DueDate, DATEADD(day,30,i.InvoiceDate)) <= DATEADD(day,30,@Today) THEN '0-30'
        WHEN ISNULL(i.DueDate, DATEADD(day,30,i.InvoiceDate)) <= DATEADD(day,60,@Today) THEN '31-60'
        ELSE '61-90'
    END",
            new() { ["Today"] = today });

        // ── Payables: open POs not yet invoiced ──────────────────────────────
        var payDt = await _db.QueryAsync(@"
SELECT
    CASE
        WHEN ISNULL(po.DeliveryDate, DATEADD(day,30,po.PODate)) <= DATEADD(day,30,@Today) THEN '0-30'
        WHEN ISNULL(po.DeliveryDate, DATEADD(day,30,po.PODate)) <= DATEADD(day,60,@Today) THEN '31-60'
        ELSE '61-90'
    END AS Band,
    COUNT(*) AS POCount,
    ISNULL(SUM(po.PriceIncTotal), 0) AS TotalOwed
FROM PurchaseOrders po
WHERE po.POStatusId NOT IN (3, 5)   -- exclude Received / Cancelled
  AND ISNULL(po.DeliveryDate, DATEADD(day,30,po.PODate)) <= DATEADD(day,90,@Today)
GROUP BY
    CASE
        WHEN ISNULL(po.DeliveryDate, DATEADD(day,30,po.PODate)) <= DATEADD(day,30,@Today) THEN '0-30'
        WHEN ISNULL(po.DeliveryDate, DATEADD(day,30,po.PODate)) <= DATEADD(day,60,@Today) THEN '31-60'
        ELSE '61-90'
    END",
            new() { ["Today"] = today });

        // ── Pipeline: won / approved quotes not yet invoiced ─────────────────
        var pipeDt = await _db.QueryAsync(@"
SELECT ISNULL(SUM(q.NettPriceTotal), 0) AS PipelineValue,
       COUNT(*) AS QuoteCount
FROM Quotes q
WHERE q.QuoteStatusId IN (4, 8)   -- Client Accepted, Manager Approved
  AND q.QuoteDate >= DATEADD(day,-180,@Today)",
            new() { ["Today"] = today });

        // ── Outstanding overdue ───────────────────────────────────────────────
        var overdueDt = await _db.QueryAsync(@"
SELECT COUNT(*) AS OverdueCount,
       ISNULL(SUM(i.NettPriceTotal + ISNULL(i.GSTTotal,0)), 0) AS OverdueAmount
FROM Invoices i
WHERE i.InvoiceStatusId NOT IN (3, 6)
  AND ISNULL(i.DueDate, DATEADD(day,30,i.InvoiceDate)) < @Today",
            new() { ["Today"] = today });

        // ── Build period buckets ──────────────────────────────────────────────
        var bands = new[] { "0-30 days", "31-60 days", "61-90 days" };
        var recIn   = new double[3];
        var payOut  = new double[3];

        int BandIndex(string b) => b switch { "0-30" => 0, "31-60" => 1, _ => 2 };

        foreach (System.Data.DataRow r in recDt.Rows)
        {
            var i = BandIndex(r["Band"].ToString()!);
            recIn[i]  = (double)Convert.ToDecimal(r["TotalOwed"]);
        }
        foreach (System.Data.DataRow r in payDt.Rows)
        {
            var i = BandIndex(r["Band"].ToString()!);
            payOut[i] = (double)Convert.ToDecimal(r["TotalOwed"]);
        }

        var netFlow = recIn.Zip(payOut, (r, p) => r - p).ToArray();

        var pipelineValue = pipeDt.Rows.Count > 0 ? Convert.ToDecimal(pipeDt.Rows[0]["PipelineValue"]) : 0m;
        var pipelineCount = pipeDt.Rows.Count > 0 ? Convert.ToInt32(pipeDt.Rows[0]["QuoteCount"])      : 0;
        var overdueAmount = overdueDt.Rows.Count > 0 ? Convert.ToDecimal(overdueDt.Rows[0]["OverdueAmount"]) : 0m;
        var overdueCount  = overdueDt.Rows.Count > 0 ? Convert.ToInt32(overdueDt.Rows[0]["OverdueCount"])    : 0;

        var summary = new
        {
            tenantId = _tenant.TenantId,
            asOf = today.ToString("yyyy-MM-dd"),
            receivables = new
            {
                day30 = recIn[0], day60 = recIn[1], day90 = recIn[2],
                total = recIn.Sum()
            },
            payables = new
            {
                day30 = payOut[0], day60 = payOut[1], day90 = payOut[2],
                total = payOut.Sum()
            },
            net_flow = new
            {
                day30 = netFlow[0], day60 = netFlow[1], day90 = netFlow[2],
                total = netFlow.Sum()
            },
            overdue = new { count = overdueCount, amount = (double)overdueAmount },
            pipeline = new { count = pipelineCount, value = (double)pipelineValue },
            insight = BuildInsight(recIn, payOut, overdueAmount, pipelineValue)
        };

        AiRenderable? chart = chartType is "bar" or "line"
            ? new AiChartSpec(
                Title: "Cash Flow Forecast — Next 90 Days",
                ChartType: chartType,
                Labels: bands,
                Series: new[]
                {
                    new AiChartSeries("Receivables (in $)", recIn),
                    new AiChartSeries("Payables (out $)", payOut),
                    new AiChartSeries("Net Position $", netFlow),
                },
                XAxisLabel: "Period",
                YAxisLabel: "Amount (AUD)")
            : null;

        return new AiToolResult(JsonSerializer.Serialize(summary), chart);
    }

    private static string BuildInsight(double[] recIn, double[] payOut, decimal overdue, decimal pipeline)
    {
        var parts = new List<string>();

        var totalRec = recIn.Sum();
        var totalPay = payOut.Sum();

        if (overdue > 0)
            parts.Add($"${overdue:N0} overdue — chase these first.");

        if (totalRec > 0)
            parts.Add($"${totalRec:N0} expected in from outstanding invoices over 90 days.");

        if (totalPay > 0)
            parts.Add($"${totalPay:N0} owed to suppliers.");

        if (pipeline > 0)
            parts.Add($"${pipeline:N0} in won quotes not yet invoiced — raise invoices to improve cash position.");

        var net = totalRec - totalPay;
        parts.Add(net >= 0
            ? $"Net 90-day position: +${net:N0} (positive)."
            : $"Net 90-day position: -${Math.Abs(net):N0} (watch cash reserves).");

        return string.Join(" ", parts);
    }
}
