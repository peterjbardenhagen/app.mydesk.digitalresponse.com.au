using Microsoft.Extensions.Logging;

namespace MyDesk.Shared.Services;

/// <summary>
/// SQL-based predictive analytics for quotes, invoices, and pipeline health.
/// Uses historical win/loss data to score open opportunities.
/// No external ML library required — scoring is done with statistical SQL queries.
/// </summary>
public class PredictiveAnalyticsService
{
    private readonly DatabaseService _db;
    private readonly ILogger<PredictiveAnalyticsService> _logger;

    public PredictiveAnalyticsService(DatabaseService db, ILogger<PredictiveAnalyticsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Calculates win probability for each open quote based on:
    ///   - Historical win rate for that customer
    ///   - Historical win rate at that value tier
    ///   - Days in pipeline vs typical win cycle
    ///   - Originator's personal win rate
    /// Returns a score 0–100 per QuoteId.
    /// </summary>
    public async Task<List<QuoteWinScore>> ScoreOpenQuotesAsync(string? originatorCode = null)
    {
        try
        {
            // Compute per-customer win rates (over last 2 years)
            var historicSql = @"
                SELECT
                    CompanyId,
                    COUNT(*) AS Total,
                    SUM(CASE WHEN QuoteStatusId = 4 THEN 1 ELSE 0 END) AS Won
                FROM Quotes
                WHERE QuoteDate >= DATEADD(YEAR, -2, GETDATE())
                  AND QuoteStatusId IN (4, 5)   -- 4=Won, 5=Lost (closed outcomes only)
                GROUP BY CompanyId";

            var historicDt = await _db.QueryAsync(historicSql, new());
            var custWinRate = historicDt.Map(r => (
                CompanyId: Convert.ToInt32(r["CompanyId"]),
                Rate: Convert.ToInt32(r["Total"]) == 0 ? 0.5
                    : (double)Convert.ToInt32(r["Won"]) / Convert.ToInt32(r["Total"])
            )).ToDictionary(x => x.CompanyId, x => x.Rate);

            // Overall win rate for baseline
            var overallWon   = historicDt.Map(r => Convert.ToInt32(r["Won"])).Sum();
            var overallTotal = historicDt.Map(r => Convert.ToInt32(r["Total"])).Sum();
            var baselineRate = overallTotal == 0 ? 0.5 : (double)overallWon / overallTotal;

            // Originator win rates
            var origSql = @"
                SELECT
                    OriginatorCode,
                    COUNT(*) AS Total,
                    SUM(CASE WHEN QuoteStatusId = 4 THEN 1 ELSE 0 END) AS Won
                FROM Quotes
                WHERE QuoteDate >= DATEADD(YEAR, -2, GETDATE())
                  AND QuoteStatusId IN (4, 5)
                GROUP BY OriginatorCode";
            var origDt = await _db.QueryAsync(origSql, new());
            var origWinRate = origDt.Map(r => (
                Code: r["OriginatorCode"]?.ToString() ?? "",
                Rate: Convert.ToInt32(r["Total"]) == 0 ? 0.5
                    : (double)Convert.ToInt32(r["Won"]) / Convert.ToInt32(r["Total"])
            )).ToDictionary(x => x.Code, x => x.Rate);

            // Median days to win (for cycle time scoring)
            var cycleDt = await _db.QueryAsync(@"
                SELECT AVG(CAST(DATEDIFF(DAY, QuoteDate, GETDATE()) AS FLOAT)) AS AvgDays
                FROM Quotes
                WHERE QuoteStatusId = 4 AND QuoteDate >= DATEADD(YEAR, -2, GETDATE())", new());
            var avgWinDays = cycleDt.Rows.Count > 0 && cycleDt.Rows[0][0] != DBNull.Value
                ? Convert.ToDouble(cycleDt.Rows[0][0])
                : 30.0;

            // Open quotes
            var openSql = @"
                SELECT q.QuoteId, q.CompanyId, q.OriginatorCode,
                       ISNULL(q.NettPriceTotal, 0) AS Value,
                       DATEDIFF(DAY, q.QuoteDate, GETDATE()) AS AgeDays,
                       c.CompanyName
                FROM Quotes q
                LEFT JOIN Companies c ON c.CompanyId = q.CompanyId
                WHERE q.QuoteStatusId NOT IN (4, 5)";

            if (!string.IsNullOrWhiteSpace(originatorCode))
                openSql += " AND q.OriginatorCode = @Orig";

            openSql += " ORDER BY q.QuoteDate DESC";

            var openDt = await _db.QueryAsync(openSql,
                new Dictionary<string, object?> { ["Orig"] = originatorCode! });

            return openDt.Map(r =>
            {
                var companyId = Convert.ToInt32(r["CompanyId"]);
                var origCode  = r["OriginatorCode"]?.ToString() ?? "";
                var ageDays   = Convert.ToInt32(r["AgeDays"]);
                var value     = Convert.ToDecimal(r["Value"]);

                // Customer affinity (40%)
                var custRate = custWinRate.TryGetValue(companyId, out var cr) ? cr : baselineRate;

                // Originator skill (30%)
                var origRate = origWinRate.TryGetValue(origCode, out var or) ? or : baselineRate;

                // Cycle time factor (20%) — penalise quotes well past average win cycle
                var cycleFactor = ageDays <= avgWinDays
                    ? 1.0
                    : Math.Max(0.1, 1.0 - ((ageDays - avgWinDays) / (avgWinDays * 2)));

                // Value tier (10%) — lower value quotes historically win more often in AU SME
                var valueFactor = value switch
                {
                    < 5000        => 0.8,
                    < 25000       => 0.7,
                    < 100000      => 0.6,
                    < 500000      => 0.5,
                    _             => 0.4,
                };

                var rawScore = (custRate * 0.4) + (origRate * 0.3) + (cycleFactor * 0.2) + (valueFactor * 0.1);
                var score    = (int)Math.Round(rawScore * 100);

                return new QuoteWinScore
                {
                    QuoteId       = Convert.ToInt32(r["QuoteId"]),
                    CompanyName   = r["CompanyName"]?.ToString() ?? "",
                    Value         = value,
                    AgeDays       = ageDays,
                    WinProbability = score,
                    Signals       = BuildSignals(custRate, origRate, cycleFactor, ageDays, avgWinDays),
                };
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not score open quotes");
            return new();
        }
    }

    /// <summary>
    /// Scores open invoices for payment risk:
    ///   - Customer historical payment delay
    ///   - Days overdue
    ///   - Invoice value tier
    /// Returns a risk score 0–100 (100 = highest risk).
    /// </summary>
    public async Task<List<InvoicePaymentRisk>> ScoreInvoiceRiskAsync()
    {
        try
        {
            // Historical average payment delay per customer (days between InvoiceDate and paid status change)
            // We approximate using invoices that are now in status 3 (paid) or similar
            var historicDt = await _db.QueryAsync(@"
                SELECT
                    CompanyId,
                    AVG(CAST(DATEDIFF(DAY, InvoiceDate, GETDATE()) AS FLOAT)) AS AvgAgeDays
                FROM Invoices
                WHERE InvoiceStatusId = 3 AND InvoiceDate >= DATEADD(YEAR, -2, GETDATE())
                GROUP BY CompanyId", new());

            var custPayDelay = historicDt.Map(r => (
                CompanyId: Convert.ToInt32(r["CompanyId"]),
                AvgDays: r["AvgAgeDays"] == DBNull.Value ? 30.0 : Convert.ToDouble(r["AvgAgeDays"])
            )).ToDictionary(x => x.CompanyId, x => x.AvgDays);

            // Open/unpaid invoices
            var openDt = await _db.QueryAsync(@"
                SELECT i.InvoiceId, i.CompanyId,
                       ISNULL(i.NettPriceTotal, 0) AS Value,
                       DATEDIFF(DAY, i.InvoiceDate, GETDATE()) AS AgeDays,
                       c.CompanyName,
                       ist.InvoiceStatus
                FROM Invoices i
                LEFT JOIN Companies c ON c.CompanyId = i.CompanyId
                LEFT JOIN InvoiceStatus ist ON ist.InvoiceStatusId = i.InvoiceStatusId
                WHERE i.InvoiceStatusId NOT IN (3, 6)   -- 3=paid, 6=cancelled
                ORDER BY AgeDays DESC", new());

            return openDt.Map(r =>
            {
                var companyId = Convert.ToInt32(r["CompanyId"]);
                var ageDays   = Convert.ToInt32(r["AgeDays"]);
                var value     = Convert.ToDecimal(r["Value"]);

                var avgDelay = custPayDelay.TryGetValue(companyId, out var d) ? d : 30.0;

                // Overdue factor (50%): how many times past typical
                var overdueFactor = ageDays <= avgDelay
                    ? 0.1
                    : Math.Min(1.0, (ageDays - avgDelay) / (avgDelay + 1));

                // Age raw (30%)
                var ageFactor = ageDays switch
                {
                    < 30  => 0.1,
                    < 60  => 0.3,
                    < 90  => 0.5,
                    < 180 => 0.7,
                    _     => 0.9,
                };

                // Value (20%) — larger invoices carry more risk exposure
                var valueFactor = value switch
                {
                    < 1000   => 0.1,
                    < 10000  => 0.3,
                    < 50000  => 0.5,
                    < 200000 => 0.7,
                    _        => 0.9,
                };

                var rawRisk = (overdueFactor * 0.5) + (ageFactor * 0.3) + (valueFactor * 0.2);
                return new InvoicePaymentRisk
                {
                    InvoiceId   = Convert.ToInt32(r["InvoiceId"]),
                    CompanyName = r["CompanyName"]?.ToString() ?? "",
                    Status      = r["InvoiceStatus"]?.ToString() ?? "",
                    Value       = value,
                    AgeDays     = ageDays,
                    RiskScore   = (int)Math.Round(rawRisk * 100),
                };
            }).OrderByDescending(x => x.RiskScore).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not score invoice risk");
            return new();
        }
    }

    /// <summary>
    /// Pipeline health summary: open opportunities, expected value,
    /// weighted by win probability, plus monthly revenue forecast.
    /// </summary>
    public async Task<PipelineHealthSummary> GetPipelineHealthAsync()
    {
        try
        {
            var scores  = await ScoreOpenQuotesAsync();
            var riskInv = await ScoreInvoiceRiskAsync();

            var totalOpen      = scores.Count;
            var totalValue     = scores.Sum(s => s.Value);
            var weightedValue  = scores.Sum(s => s.Value * s.WinProbability / 100m);
            var highConfidence = scores.Count(s => s.WinProbability >= 70);
            var atRisk         = scores.Count(s => s.WinProbability < 30 && s.AgeDays > 30);

            // 90-day revenue forecast based on current weighted pipeline
            var forecastRevenue = weightedValue;

            // Invoice exposure
            var highRiskInvoices = riskInv.Where(i => i.RiskScore >= 60).ToList();
            var invoiceExposure  = highRiskInvoices.Sum(i => i.Value);

            // Health score: weighted by conversion likelihood, penalised by stale + at-risk
            var healthScore = totalOpen == 0 ? 50 :
                (int)Math.Round(((double)highConfidence / totalOpen * 60)
                                + (atRisk == 0 ? 20 : Math.Max(0, 20 - atRisk * 3))
                                + (invoiceExposure > 0 ? 10 : 20));

            return new PipelineHealthSummary
            {
                OpenQuotes         = totalOpen,
                TotalPipelineValue = totalValue,
                WeightedValue      = weightedValue,
                HighConfidenceCount = highConfidence,
                AtRiskCount        = atRisk,
                ForecastRevenue90d = forecastRevenue,
                HighRiskInvoiceCount = highRiskInvoices.Count,
                HighRiskInvoiceExposure = invoiceExposure,
                HealthScore        = Math.Clamp(healthScore, 0, 100),
                TopOpportunities   = scores.OrderByDescending(s => s.Value * s.WinProbability / 100).Take(5).ToList(),
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not compute pipeline health");
            return new PipelineHealthSummary();
        }
    }

    private static List<string> BuildSignals(double custRate, double origRate, double cycleFactor, int ageDays, double avgWinDays)
    {
        var signals = new List<string>();
        if (custRate >= 0.6)  signals.Add("Strong customer history");
        if (custRate < 0.3)   signals.Add("Customer rarely buys");
        if (origRate >= 0.6)  signals.Add("High-performing rep");
        if (origRate < 0.3)   signals.Add("Rep below average win rate");
        if (cycleFactor < 0.5 && ageDays > avgWinDays * 1.5) signals.Add("Overdue — consider follow-up");
        if (ageDays <= 7)     signals.Add("Fresh opportunity");
        return signals;
    }
}

public record QuoteWinScore
{
    public int     QuoteId        { get; init; }
    public string  CompanyName    { get; init; } = "";
    public decimal Value          { get; init; }
    public int     AgeDays        { get; init; }
    public int     WinProbability { get; init; }
    public List<string> Signals   { get; init; } = new();
}

public record InvoicePaymentRisk
{
    public int     InvoiceId   { get; init; }
    public string  CompanyName { get; init; } = "";
    public string  Status      { get; init; } = "";
    public decimal Value       { get; init; }
    public int     AgeDays     { get; init; }
    public int     RiskScore   { get; init; }
}

public record PipelineHealthSummary
{
    public int     OpenQuotes              { get; init; }
    public decimal TotalPipelineValue      { get; init; }
    public decimal WeightedValue           { get; init; }
    public int     HighConfidenceCount     { get; init; }
    public int     AtRiskCount             { get; init; }
    public decimal ForecastRevenue90d      { get; init; }
    public int     HighRiskInvoiceCount    { get; init; }
    public decimal HighRiskInvoiceExposure { get; init; }
    public int     HealthScore             { get; init; }
    public List<QuoteWinScore> TopOpportunities { get; init; } = new();
}
