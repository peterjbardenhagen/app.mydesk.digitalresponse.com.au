using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public enum ChartPeriod { ThisMonth, ThisYear, FyToDate, LastYear, SinceInception }

public record DashboardChartData(
    decimal[] QuotesWon,
    decimal[] Invoices,
    decimal[] POs,
    string[]  Labels);

public class DashboardService
{
    private readonly DatabaseService _db;
    private readonly ActivityService _activity;
    private readonly ILogger<DashboardService> _logger;
    private readonly IMemoryCache _cache;
    private readonly ICurrentTenantAccessor _currentTenantAccessor;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public DashboardService(DatabaseService db, ActivityService activity, ILogger<DashboardService> logger, IMemoryCache cache, ICurrentTenantAccessor currentTenantAccessor)
    {
        _db       = db;
        _activity = activity;
        _logger   = logger;
        _cache    = cache;
        _currentTenantAccessor = currentTenantAccessor;
    }

    public async Task<DashboardMetrics> GetMetricsAsync(string? originatorCode = null)
    {
        return await GetMetricsAsync(ChartPeriod.ThisYear, originatorCode);
    }

    public async Task<DashboardMetrics> GetMetricsAsync(ChartPeriod period, string? originatorCode = null)
    {
        var tenantId = _currentTenantAccessor?.TenantId ?? Guid.Empty;
        var cacheKey = $"dashboard_metrics_{period}_{originatorCode ?? "all"}_{tenantId}";
        if (_cache.TryGetValue(cacheKey, out DashboardMetrics? cached) && cached != null)
            return cached;

        var m = new DashboardMetrics();
        try
        {
            var now            = DateTime.Now;
            var thisMonth      = new DateTime(now.Year, now.Month, 1);
            var lastMonth      = thisMonth.AddMonths(-1);
            
            var (periodStart, periodEnd, comparisonStart, comparisonEnd) = period switch
            {
                ChartPeriod.ThisMonth      => (thisMonth, now, lastMonth, new DateTime(lastMonth.Year, lastMonth.Month, DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month))),
                ChartPeriod.ThisYear       => (new DateTime(now.Year, 1, 1), now, new DateTime(now.Year - 1, 1, 1), new DateTime(now.Year - 1, 12, 31)),
                ChartPeriod.FyToDate       => (new DateTime(now.Month >= 7 ? now.Year : now.Year - 1, 7, 1), now, new DateTime(now.Month >= 7 ? now.Year - 1 : now.Year - 2, 7, 1), new DateTime(now.Year - 1, 6, 30)),
                ChartPeriod.LastYear       => (new DateTime(now.Year - 1, 1, 1), new DateTime(now.Year - 1, 12, 31), new DateTime(now.Year - 2, 1, 1), new DateTime(now.Year - 2, 12, 31)),
                ChartPeriod.SinceInception => (new DateTime(2000, 1, 1), now, new DateTime(2000, 1, 1), now.AddYears(-1)),
                _                          => (thisMonth, now, lastMonth, new DateTime(lastMonth.Year, lastMonth.Month, DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month)))
            };
            
            var ytdStart       = periodStart;
            var lastYtdStart   = comparisonStart;
            var lastYtdEnd     = comparisonEnd;
            var thirtyDaysAgo  = now.AddDays(-30);
            var queryStart     = periodStart;
            var queryEnd       = periodEnd;

            // ── BATCH 1: Core KPIs via individual queries ─────────────
            var periodParams = new Dictionary<string, object?>
            {
                ["PS"] = queryStart, ["PE"] = queryEnd,
                ["YS"] = ytdStart, ["LYS"] = lastYtdStart, ["LYE"] = lastYtdEnd,
                ["TDA"] = thirtyDaysAgo, ["TM"] = thisMonth
            };

            // Row 1: Period quotes
            var row1 = await _db.QueryAsync(@"
                SELECT COUNT(*) AS QuoteCount,
                    SUM(CASE WHEN QuoteStatusId = 4 THEN 1 ELSE 0 END) AS QuotesWon,
                    ISNULL(SUM(NettPriceTotal),0) AS QuotesValue,
                    ISNULL(SUM(UnitCostTotal),0) AS QuotesCost
                FROM Quotes WHERE QuoteDate >= @PS AND QuoteDate <= @PE", periodParams);
            if (row1.Rows.Count > 0)
            {
                var r = row1.Rows[0];
                m.ThisMonthQuotes = Convert.ToInt32(r["QuoteCount"]);
                m.ThisMonthQuotesWon = Convert.ToInt32(r["QuotesWon"]);
                m.ThisMonthQuotesValue = Convert.ToDecimal(r["QuotesValue"]);
            }

            var lastPeriodStartVal = lastPeriodStart(queryStart);
            var lastPeriodEndVal = queryStart.AddTicks(-1);

            // Row 2: Last period quotes won
            var row2 = await _db.QueryAsync(
                "SELECT COUNT(*) FROM Quotes WHERE QuoteDate >= @LPS AND QuoteDate <= @LPE AND QuoteStatusId = 4",
                new() { ["LPS"] = lastPeriodStartVal, ["LPE"] = lastPeriodEndVal });
            if (row2.Rows.Count > 0)
                m.LastMonthQuotesWon = Convert.ToInt32(row2.Rows[0][0]);

            // Row 3: Period invoices
            var row3 = await _db.QueryAsync(@"
                SELECT COUNT(*) AS InvoiceCount, ISNULL(SUM(NettPriceTotal),0) AS InvoiceValue
                FROM Invoices WHERE InvoiceDate >= @PS AND InvoiceDate <= @PE", periodParams);
            if (row3.Rows.Count > 0)
            {
                var r = row3.Rows[0];
                m.ThisMonthInvoices = Convert.ToInt32(r["InvoiceCount"]);
                m.ThisMonthInvoiceValue = Convert.ToDecimal(r["InvoiceValue"]);
            }

            // Row 4: Last period invoices
            var row4 = await _db.QueryAsync(
                "SELECT COUNT(*) FROM Invoices WHERE InvoiceDate >= @LPS AND InvoiceDate <= @LPE",
                new() { ["LPS"] = lastPeriodStartVal, ["LPE"] = lastPeriodEndVal });
            if (row4.Rows.Count > 0)
                m.LastMonthInvoices = Convert.ToInt32(row4.Rows[0][0]);

            // Row 5: Period POs
            var row5 = await _db.QueryAsync(@"
                SELECT COUNT(*) AS POCount, ISNULL(SUM(PriceExTotal),0) AS POValue
                FROM PurchaseOrders WHERE PODate >= @PS AND PODate <= @PE", periodParams);
            if (row5.Rows.Count > 0)
            {
                var r = row5.Rows[0];
                m.ThisMonthPOs = Convert.ToInt32(r["POCount"]);
                m.ThisMonthPOValue = Convert.ToDecimal(r["POValue"]);
            }

            // Row 6: Last period POs
            var row6 = await _db.QueryAsync(
                "SELECT COUNT(*) FROM PurchaseOrders WHERE PODate >= @LPS AND PODate <= @LPE",
                new() { ["LPS"] = lastPeriodStartVal, ["LPE"] = lastPeriodEndVal });
            if (row6.Rows.Count > 0)
                m.LastMonthPOs = Convert.ToInt32(row6.Rows[0][0]);

            // Row 7: YTD quotes won + value
            var row7 = await _db.QueryAsync(@"
                SELECT COUNT(*) AS YtdQuotesWon, ISNULL(SUM(NettPriceTotal),0) AS YtdQuotesValue, ISNULL(SUM(UnitCostTotal),0) AS YtdQuotesCost
                FROM Quotes WHERE QuoteDate >= @YS AND QuoteStatusId = 4",
                new() { ["YS"] = ytdStart });
            if (row7.Rows.Count > 0)
            {
                var r = row7.Rows[0];
                m.YtdQuotesWon = Convert.ToInt32(r["YtdQuotesWon"]);
                m.YtdQuotesValue = Convert.ToDecimal(r["YtdQuotesValue"]);
            }

            // Row 8: YTD invoices
            var row8 = await _db.QueryAsync(
                "SELECT COUNT(*) AS YtdInvoiceCount, ISNULL(SUM(NettPriceTotal),0) AS YtdInvoiceValue FROM Invoices WHERE InvoiceDate >= @YS",
                new() { ["YS"] = ytdStart });
            if (row8.Rows.Count > 0)
            {
                var r = row8.Rows[0];
                m.YtdInvoices = Convert.ToInt32(r["YtdInvoiceCount"]);
                m.YtdInvoiceValue = Convert.ToDecimal(r["YtdInvoiceValue"]);
            }

            // Row 9: Last YTD comparison
            var row9 = await _db.QueryAsync(
                "SELECT ISNULL(SUM(NettPriceTotal),0) FROM Quotes WHERE QuoteDate >= @LYS AND QuoteDate <= @LYE AND QuoteStatusId = 4",
                new() { ["LYS"] = lastYtdStart, ["LYE"] = lastYtdEnd });
            if (row9.Rows.Count > 0)
                m.LastYearYtdQuotesValue = Convert.ToDecimal(row9.Rows[0][0]);

            // Row 10: Alerts
            var row10 = await _db.QueryAsync(@"
                SELECT
                    (SELECT COUNT(*) FROM Quotes WHERE QuoteStatusId IN (1,2) AND QuoteDate < @TDA) AS PendingQuotesOver30,
                    (SELECT COUNT(*) FROM Invoices WHERE InvoiceStatusId = 2 AND InvoiceDate < @TDA) AS InvoicesOverdue,
                    (SELECT COUNT(*) FROM PurchaseOrders WHERE POStatusId = 2) AS PendingApprovalPOs,
                    (SELECT COUNT(*) FROM Quotes WHERE QuoteStatusId IN (1, 2, 3, 6, 7, 8)) AS OpenQuotes,
                    ISNULL((SELECT SUM(NettPriceTotal) FROM Quotes q LEFT JOIN QuoteStatus qs ON qs.QuoteStatusId = q.QuoteStatusId WHERE q.QuoteStatusId IN (1, 2, 3, 6, 7, 8) AND (qs.QuoteStatus IS NULL OR qs.QuoteStatus NOT LIKE '%declined%' AND qs.QuoteStatus NOT LIKE '%reject%')), 0) AS PipelineValue",
                new() { ["TDA"] = thirtyDaysAgo });
            if (row10.Rows.Count > 0)
            {
                var r = row10.Rows[0];
                m.PendingQuotesOver30Days = Convert.ToInt32(r["PendingQuotesOver30"]);
                m.InvoicesOverdue = Convert.ToInt32(r["InvoicesOverdue"]);
                m.PendingApprovalPOs = Convert.ToInt32(r["PendingApprovalPOs"]);
                m.OpenQuotesCount = Convert.ToInt32(r["OpenQuotes"]);
                m.PipelineValue = Convert.ToDecimal(r["PipelineValue"]);
            }

            // Row 11: Despatch
            var row11 = await _db.QueryAsync(
                "SELECT COUNT(*) FROM Despatch WHERE DespatchDate >= @TM",
                new() { ["TM"] = thisMonth });
            if (row11.Rows.Count > 0)
                m.ThisMonthDespatch = Convert.ToInt32(row11.Rows[0][0]);

            // ── BATCH 2: Monthly breakdowns (4 queries, already efficient) ────
            m.MonthlyQuotesThisYear   = await GetMonthlyTotals("Quotes", "QuoteDate",
                "t.NettPriceTotal", now.Year,
                join: "LEFT JOIN QuoteStatus qs ON qs.QuoteStatusId = t.QuoteStatusId",
                extraWhere: "(qs.QuoteStatus IS NULL OR qs.QuoteStatus NOT LIKE '%declined%' AND qs.QuoteStatus NOT LIKE '%reject%')");
            m.MonthlyQuotesLastYear   = await GetMonthlyTotals("Quotes", "QuoteDate",
                "t.NettPriceTotal", now.Year - 1,
                join: "LEFT JOIN QuoteStatus qs ON qs.QuoteStatusId = t.QuoteStatusId",
                extraWhere: "(qs.QuoteStatus IS NULL OR qs.QuoteStatus NOT LIKE '%declined%' AND qs.QuoteStatus NOT LIKE '%reject%')");
            m.MonthlyInvoicesThisYear = await GetMonthlyTotals("Invoices", "InvoiceDate", "t.NettPriceTotal", now.Year);
            m.MonthlyPOsThisYear      = await GetMonthlyTotals("PurchaseOrders", "PODate", "t.PriceExTotal", now.Year);

            // ── Activity feed ────────────────────────────────────────────────
            m.RecentActivity = await _activity.GetRecentAsync(30);

            // ── Advanced KPIs (now uses cached values, 3 extra queries max) ──
            await CalculateAdvancedKPIsAsync(m, now, queryStart, queryEnd, ytdStart, lastYtdStart, lastYtdEnd);
            
            // ── Team Performance (single batched query) ──────────────────────
            m.TeamMemberKPIs = await GetTeamMemberKPIsAsync(queryStart, queryEnd, ytdStart);
            m.DivisionPerformance = await GetDivisionPerformanceAsync(queryStart, queryEnd, ytdStart);
            
            // ── Health Indicators ────────────────────────────────────────────
            m.Warnings = await GenerateWarningsAsync(m);
            m.Recommendations = await GenerateRecommendationsAsync(m);
            m.OverallHealthScore = CalculateHealthScore(m);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard metrics");
        }

        _cache.Set(cacheKey, m, CacheDuration);
        return m;
    }

    private static DateTime lastPeriodStart(DateTime periodStart)
    {
        var prev = periodStart.AddMonths(-1);
        return new DateTime(prev.Year, prev.Month, 1);
    }

    private async Task CalculateAdvancedKPIsAsync(DashboardMetrics m, DateTime now, DateTime periodStart, DateTime periodEnd, DateTime ytdStart, DateTime lastYtdStart, DateTime lastYtdEnd)
    {
        try
        {
            m.AverageQuoteValue = m.ThisMonthQuotes > 0 ? m.ThisMonthQuotesValue / m.ThisMonthQuotes : 0;
            m.AverageInvoiceValue = m.ThisMonthInvoices > 0 ? m.ThisMonthInvoiceValue / m.ThisMonthInvoices : 0;

            // Gross Profit Margin - fetch YTD cost via query
            var ytdValue = m.YtdQuotesValue;
            var ytdCostDt = await _db.QueryAsync(
                "SELECT ISNULL(SUM(UnitCostTotal),0) AS YtdCost FROM Quotes WHERE QuoteDate >= @YS AND QuoteStatusId = 4",
                new() { ["YS"] = ytdStart });
            var ytdCost = ytdCostDt.Rows.Count > 0 ? Convert.ToDecimal(ytdCostDt.Rows[0]["YtdCost"]) : 0m;
            m.GrossProfitMargin = ytdValue > 0 ? ((ytdValue - ytdCost) / ytdValue) * 100 : 0;

            // Quote to Invoice Conversion Rate (single JOIN query)
            var conversionSql = @"
                SELECT 
                    COUNT(DISTINCT q.Qid) AS Converted,
                    (SELECT COUNT(*) FROM Quotes WHERE QuoteDate >= @S) AS Total
                FROM Quotes q 
                INNER JOIN Invoices i ON i.Qid = q.Qid 
                WHERE q.QuoteDate >= @S";
            var convDt = await _db.QueryAsync(conversionSql, p(ytdStart));
            if (convDt.Rows.Count > 0)
            {
                var total = Convert.ToInt32(convDt.Rows[0]["Total"]);
                var converted = Convert.ToInt32(convDt.Rows[0]["Converted"]);
                m.QuoteToInvoiceConversionRate = total > 0 ? ((decimal)converted / total) * 100 : 0;
            }

            // Growth calculations
            var lps = lastPeriodStart(periodStart);
            var lpe = lps.AddTicks(periodEnd.Subtract(periodStart).Ticks);
            var growthSql = @"
                SELECT 
                    ISNULL(SUM(CASE WHEN InvoiceDate >= @LPS AND InvoiceDate <= @LPE THEN NettPriceTotal ELSE 0 END), 0) AS LastPeriod,
                    ISNULL(SUM(CASE WHEN InvoiceDate >= @LYPS AND InvoiceDate <= @LYPE THEN NettPriceTotal ELSE 0 END), 0) AS LastYearPeriod
                FROM Invoices 
                WHERE (InvoiceDate >= @LPS AND InvoiceDate <= @LPE) OR (InvoiceDate >= @LYPS AND InvoiceDate <= @LYPE)";
            var growthDt = await _db.QueryAsync(growthSql, new()
            {
                ["LPS"] = lps, ["LPE"] = lpe,
                ["LYPS"] = periodStart.AddYears(-1), ["LYPE"] = periodStart.AddYears(-1).AddTicks(periodEnd.Subtract(periodStart).Ticks)
            });
            if (growthDt.Rows.Count > 0)
            {
                var lastPeriodValue = Convert.ToDecimal(growthDt.Rows[0]["LastPeriod"]);
                var lastYearValue = Convert.ToDecimal(growthDt.Rows[0]["LastYearPeriod"]);
                m.ThisMonthVsLastMonthPercent = lastPeriodValue > 0 ? ((m.ThisMonthInvoiceValue - lastPeriodValue) / lastPeriodValue) * 100 : 0;
                m.ThisMonthVsLastYearPercent = lastYearValue > 0 ? ((m.ThisMonthInvoiceValue - lastYearValue) / lastYearValue) * 100 : 0;
            }

            // YTD Growth
            var ytdGrowthDt = await _db.QueryAsync(
                "SELECT ISNULL(SUM(NettPriceTotal), 0) AS LastYtd FROM Invoices WHERE InvoiceDate >= @S AND InvoiceDate <= @E",
                p2(lastYtdStart, lastYtdEnd));
            if (ytdGrowthDt.Rows.Count > 0)
            {
                var lastYtdInvoices = Convert.ToDecimal(ytdGrowthDt.Rows[0]["LastYtd"]);
                m.YtdRevenueGrowth = lastYtdInvoices > 0 ? ((m.YtdInvoiceValue - lastYtdInvoices) / lastYtdInvoices) * 100 : 0;
            }

            m.MonthOverMonthGrowth = m.ThisMonthVsLastMonthPercent;
            m.YearOverYearGrowth = m.ThisMonthVsLastYearPercent;

            // Projected Monthly Revenue
            if (periodStart.Month == now.Month && periodStart.Year == now.Year)
            {
                var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
                var dayOfMonth = now.Day;
                m.ProjectedMonthlyRevenue = dayOfMonth > 0 ? m.ThisMonthInvoiceValue / dayOfMonth * daysInMonth : m.ThisMonthInvoiceValue;
            }
            else
            {
                m.ProjectedMonthlyRevenue = m.ThisMonthInvoiceValue;
            }

            // Quarterly progress
            var quarterStart = new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1);
            m.QuarterlyTarget = 500000;
            var qDt = await _db.QueryAsync("SELECT ISNULL(SUM(NettPriceTotal), 0) AS QProgress FROM Invoices WHERE InvoiceDate >= @S", p(quarterStart));
            if (qDt.Rows.Count > 0)
                m.QuarterlyProgress = Convert.ToDecimal(qDt.Rows[0]["QProgress"]);

            // Target revenue
            m.LastYearMonthlyRevenue = await GetMonthlyTotals("Invoices", "InvoiceDate", "t.NettPriceTotal", now.Year - 1);
            var lastYearTotal = m.LastYearMonthlyRevenue.Sum();
            var monthlyTarget = lastYearTotal * 1.1m / 12;
            m.TargetMonthlyRevenue = Enumerable.Repeat(monthlyTarget, 12).ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating advanced KPIs");
        }
    }

    private async Task<List<UserKPI>> GetTeamMemberKPIsAsync(DateTime periodStart, DateTime periodEnd, DateTime ytdStart)
    {
        var kpis = new List<UserKPI>();
        try
        {
            var lps = lastPeriodStart(periodStart);

            // Get active users
            var users = await _db.QueryAsync(
                "SELECT DISTINCT Code, Name, IsDirector = 0 FROM Users WHERE Active = 1 AND Code IS NOT NULL");

            // Get all user quote stats in period
            var quotes = await _db.QueryAsync(@"
                SELECT Code, 
                    COUNT(*) AS QuotesRaised,
                    SUM(CASE WHEN QuoteStatusId = 4 THEN 1 ELSE 0 END) AS QuotesWon,
                    ISNULL(SUM(NettPriceTotal),0) AS QuoteValue,
                    SUM(CASE WHEN QuoteStatusId IN (1,2,3,6,7,8) THEN 1 ELSE 0 END) AS PendingQuotes,
                    SUM(CASE WHEN QuoteStatusId IN (1,2,3,6,7,8) AND QuoteDate < DATEADD(day,-30,GETDATE()) THEN 1 ELSE 0 END) AS OverdueQuotes
                FROM Quotes WHERE QuoteDate >= @PS AND QuoteDate <= @PE
                GROUP BY Code", new() { ["PS"] = periodStart, ["PE"] = periodEnd });

            // Get all user invoice stats in period
            var invoices = await _db.QueryAsync(@"
                SELECT Code, COUNT(*) AS InvoicesClosed, ISNULL(SUM(NettPriceTotal),0) AS InvoiceValue
                FROM Invoices WHERE InvoiceDate >= @PS AND InvoiceDate <= @PE
                GROUP BY Code", new() { ["PS"] = periodStart, ["PE"] = periodEnd });

            // Get YTD revenue per user
            var ytd = await _db.QueryAsync(
                "SELECT Code, ISNULL(SUM(NettPriceTotal),0) AS YtdRevenue FROM Invoices WHERE InvoiceDate >= @YS GROUP BY Code",
                new() { ["YS"] = ytdStart });

            // Get last period invoice value per user
            var trend = await _db.QueryAsync(
                "SELECT Code, ISNULL(SUM(NettPriceTotal),0) AS LastPeriodValue FROM Invoices WHERE InvoiceDate >= @LPS AND InvoiceDate < @PS GROUP BY Code",
                new() { ["LPS"] = lps, ["PS"] = periodStart });

            // Build lookup dictionaries
            var quoteDict = new Dictionary<string, System.Data.DataRow>();
            var invoiceDict = new Dictionary<string, System.Data.DataRow>();
            var ytdDict = new Dictionary<string, System.Data.DataRow>();
            var trendDict = new Dictionary<string, System.Data.DataRow>();

            foreach (System.Data.DataRow r in quotes.Rows)
            {
                var code = r["Code"].ToString() ?? "";
                if (!string.IsNullOrEmpty(code)) quoteDict[code] = r;
            }

            foreach (System.Data.DataRow r in invoices.Rows)
            {
                var code = r["Code"].ToString() ?? "";
                if (!string.IsNullOrEmpty(code)) invoiceDict[code] = r;
            }

            foreach (System.Data.DataRow r in ytd.Rows)
            {
                var code = r["Code"].ToString() ?? "";
                if (!string.IsNullOrEmpty(code)) ytdDict[code] = r;
            }

            foreach (System.Data.DataRow r in trend.Rows)
            {
                var code = r["Code"].ToString() ?? "";
                if (!string.IsNullOrEmpty(code)) trendDict[code] = r;
            }

            // Build KPIs from user list + lookups
            foreach (System.Data.DataRow user in users.Rows)
            {
                var code = user["Code"].ToString() ?? "";
                if (string.IsNullOrEmpty(code)) continue;
                var name = user["Name"].ToString() ?? "";
                var isDirector = Convert.ToInt32(user["IsDirector"]) == 1;

                var qRow = quoteDict.GetValueOrDefault(code);
                var iRow = invoiceDict.GetValueOrDefault(code);
                var yRow = ytdDict.GetValueOrDefault(code);
                var tRow = trendDict.GetValueOrDefault(code);

                var quotesRaised = qRow != null ? Convert.ToInt32(qRow["QuotesRaised"]) : 0;
                var quotesWon = qRow != null ? Convert.ToInt32(qRow["QuotesWon"]) : 0;
                var quoteValue = qRow != null ? Convert.ToDecimal(qRow["QuoteValue"]) : 0;
                var invoicesClosed = iRow != null ? Convert.ToInt32(iRow["InvoicesClosed"]) : 0;
                var invoiceValue = iRow != null ? Convert.ToDecimal(iRow["InvoiceValue"]) : 0;
                var ytdRevenue = yRow != null ? Convert.ToDecimal(yRow["YtdRevenue"]) : 0;
                var pendingQuotes = qRow != null ? Convert.ToInt32(qRow["PendingQuotes"]) : 0;
                var overdueQuotes = qRow != null ? Convert.ToInt32(qRow["OverdueQuotes"]) : 0;
                var lastPeriodValue = tRow != null ? Convert.ToDecimal(tRow["LastPeriodValue"]) : 0;

                kpis.Add(new UserKPI
                {
                    UserCode = code,
                    UserName = name,
                    IsDirector = isDirector,
                    QuotesRaisedThisMonth = quotesRaised,
                    QuotesWonThisMonth = quotesWon,
                    QuoteValueThisMonth = quoteValue,
                    InvoicesClosedThisMonth = invoicesClosed,
                    InvoiceValueThisMonth = invoiceValue,
                    YtdRevenue = ytdRevenue,
                    PendingQuotes = pendingQuotes,
                    OverdueQuotes = overdueQuotes,
                    AverageQuoteValue = quotesRaised > 0 ? quoteValue / quotesRaised : 0,
                    WinRate = quotesRaised > 0 ? ((decimal)quotesWon / quotesRaised) * 100 : 0,
                    PerformanceTrend = invoiceValue > lastPeriodValue ? "up" : invoiceValue < lastPeriodValue ? "down" : "stable"
                });
            }

            // Rank by YTD revenue
            var ranked = kpis.OrderByDescending(k => k.YtdRevenue).ToList();
            for (int i = 0; i < ranked.Count; i++)
                ranked[i].Rank = i + 1;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading team member KPIs");
        }
        return kpis;
    }

    private async Task<List<DivisionPerformance>> GetDivisionPerformanceAsync(DateTime periodStart, DateTime periodEnd, DateTime ytdStart)
    {
        var divisions = new List<DivisionPerformance>();
        try
        {
            // Get division list
            var divList = await _db.QueryAsync(
                "SELECT DivisionId, Division as DivisionName FROM Divisions WHERE Visible = 1 ORDER BY Division");

            // Get division invoice revenue in period
            var revenue = await _db.QueryAsync(@"
                SELECT DivisionId, ISNULL(SUM(NettPriceTotal),0) AS PeriodRevenue, COUNT(*) AS InvoiceCount
                FROM Invoices WHERE DivisionId IS NOT NULL AND InvoiceDate >= @PS AND InvoiceDate <= @PE
                GROUP BY DivisionId", new() { ["PS"] = periodStart, ["PE"] = periodEnd });

            // Get division YTD revenue
            var ytd = await _db.QueryAsync(
                "SELECT DivisionId, ISNULL(SUM(NettPriceTotal),0) AS YtdRevenue FROM Invoices WHERE DivisionId IS NOT NULL AND InvoiceDate >= @YS GROUP BY DivisionId",
                new() { ["YS"] = ytdStart });

            // Get division quotes in period
            var quotes = await _db.QueryAsync(
                "SELECT DivisionId, COUNT(*) AS QuoteCount FROM Quotes WHERE DivisionId IS NOT NULL AND QuoteDate >= @PS AND QuoteDate <= @PE GROUP BY DivisionId",
                new() { ["PS"] = periodStart, ["PE"] = periodEnd });

            var revenueDict = new Dictionary<int, System.Data.DataRow>();
            var ytdDict = new Dictionary<int, System.Data.DataRow>();
            var quoteDict = new Dictionary<int, System.Data.DataRow>();

            foreach (System.Data.DataRow r in revenue.Rows)
                revenueDict[Convert.ToInt32(r["DivisionId"])] = r;

            foreach (System.Data.DataRow r in ytd.Rows)
                ytdDict[Convert.ToInt32(r["DivisionId"])] = r;

            foreach (System.Data.DataRow r in quotes.Rows)
                quoteDict[Convert.ToInt32(r["DivisionId"])] = r;

            foreach (System.Data.DataRow r in divList.Rows)
            {
                var divisionId = Convert.ToInt32(r["DivisionId"]);
                var divisionName = r["DivisionName"].ToString() ?? "";

                var revRow = revenueDict.GetValueOrDefault(divisionId);
                var ytdRow = ytdDict.GetValueOrDefault(divisionId);
                var qRow = quoteDict.GetValueOrDefault(divisionId);

                divisions.Add(new DivisionPerformance
                {
                    DivisionId = divisionId,
                    DivisionName = divisionName,
                    ThisMonthRevenue = revRow != null ? Convert.ToDecimal(revRow["PeriodRevenue"]) : 0,
                    YtdRevenue = ytdRow != null ? Convert.ToDecimal(ytdRow["YtdRevenue"]) : 0,
                    QuotesCount = qRow != null ? Convert.ToInt32(qRow["QuoteCount"]) : 0,
                    InvoicesCount = revRow != null ? Convert.ToInt32(revRow["InvoiceCount"]) : 0,
                    GrowthPercent = 0,
                    HealthScore = 75
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error loading division performance");
        }
        return divisions;
    }

    private async Task<List<BusinessWarning>> GenerateWarningsAsync(DashboardMetrics m)
    {
        var warnings = new List<BusinessWarning>();
        
        if (m.InvoicesOverdue > 5)
            warnings.Add(new BusinessWarning { Id = "overdue-invoices", Severity = m.InvoicesOverdue > 10 ? "critical" : "warning", Category = "Cash Flow", Title = $"{m.InvoicesOverdue} Overdue Invoices", Description = "Outstanding invoices over 30 days old may impact cash flow", Metric = "Invoices > 30 days", CurrentValue = m.InvoicesOverdue, Threshold = 5, ActionLink = "/invoices?filter=overdue" });

        if (m.PendingQuotesOver30Days > 10)
            warnings.Add(new BusinessWarning { Id = "stale-quotes", Severity = "warning", Category = "Sales", Title = $"{m.PendingQuotesOver30Days} Quotes Open > 30 Days", Description = "Stale quotes may need follow-up or closure", Metric = "Open quotes > 30 days", CurrentValue = m.PendingQuotesOver30Days, Threshold = 10, ActionLink = "/quotes?status=overdue" });

        if (m.GrossProfitMargin < 20)
            warnings.Add(new BusinessWarning { Id = "low-margin", Severity = "critical", Category = "Profitability", Title = "Low Gross Profit Margin", Description = $"Current margin {m.GrossProfitMargin:N1}% is below healthy threshold of 20%", Metric = "Gross Profit Margin", CurrentValue = m.GrossProfitMargin, Threshold = 20, ActionLink = "/reports/margins" });

        if (m.QuoteToInvoiceConversionRate < 30)
            warnings.Add(new BusinessWarning { Id = "low-conversion", Severity = "warning", Category = "Sales", Title = "Low Quote Conversion Rate", Description = $"Only {m.QuoteToInvoiceConversionRate:N1}% of quotes are converting to invoices", Metric = "Conversion Rate", CurrentValue = m.QuoteToInvoiceConversionRate, Threshold = 30, ActionLink = "/quotes?filter=analysis" });

        return warnings;
    }

    private async Task<List<BusinessRecommendation>> GenerateRecommendationsAsync(DashboardMetrics m)
    {
        var recs = new List<BusinessRecommendation>();

        if (m.PendingApprovalPOs > 0)
            recs.Add(new BusinessRecommendation { Id = "approve-pos", Priority = "high", Category = "Operations", Title = $"Approve {m.PendingApprovalPOs} Pending POs", Description = "Purchase orders awaiting approval may delay project delivery", ExpectedImpact = "Faster project delivery", ActionLink = "/purchase-orders?status=pending" });

        if (m.OpenQuotesCount > 20)
            recs.Add(new BusinessRecommendation { Id = "follow-up-quotes", Priority = "medium", Category = "Sales", Title = "Follow Up on Open Quotes", Description = $"{m.OpenQuotesCount} open quotes worth {m.PipelineValue:C0} need follow-up", ExpectedImpact = $"Potential {(m.PipelineValue * 0.3m):C0} revenue", ActionLink = "/quotes?status=open" });

        if (m.ThisMonthVsLastMonthPercent < 0)
            recs.Add(new BusinessRecommendation { Id = "boost-sales", Priority = "high", Category = "Revenue", Title = "Revenue Declining", Description = $"This month is {Math.Abs(m.ThisMonthVsLastMonthPercent):N1}% below last month", ExpectedImpact = "Identify opportunities to close more business", ActionLink = "/quotes?filter=opportunities" });

        if (m.AverageQuoteValue < 5000)
            recs.Add(new BusinessRecommendation { Id = "increase-avg-value", Priority = "medium", Category = "Sales", Title = "Increase Average Quote Value", Description = $"Current average is {m.AverageQuoteValue:C0}. Consider upselling or bundling", ExpectedImpact = "Higher revenue per transaction", ActionLink = "/reports/quote-analysis" });

        return recs;
    }

    private int CalculateHealthScore(DashboardMetrics m)
    {
        int score = 70;
        if (m.GrossProfitMargin > 30) score += 10;
        if (m.QuoteToInvoiceConversionRate > 40) score += 5;
        if (m.ThisMonthVsLastMonthPercent > 0) score += 5;
        if (m.YtdRevenueGrowth > 10) score += 5;
        if (m.InvoicesOverdue == 0) score += 5;
        if (m.InvoicesOverdue > 10) score -= 10;
        if (m.InvoicesOverdue > 5) score -= 5;
        if (m.GrossProfitMargin < 20) score -= 10;
        if (m.GrossProfitMargin < 15) score -= 10;
        if (m.QuoteToInvoiceConversionRate < 20) score -= 10;
        if (m.ThisMonthVsLastMonthPercent < -20) score -= 10;
        if (m.PendingQuotesOver30Days > 20) score -= 5;
        return Math.Clamp(score, 0, 100);
    }

    private static Dictionary<string, object?> p(DateTime start) => new() { ["S"] = start };
    private static Dictionary<string, object?> p2(DateTime start, DateTime end) => new() { ["S"] = start, ["E"] = end };

    private async Task<decimal[]> GetMonthlyTotals(string table, string dateCol, string valueExpr, int year, string? join = null, string? extraWhere = null)
    {
        var sql = $@"
            SELECT MONTH(t.{dateCol}) AS Mnth, ISNULL(SUM({valueExpr}), 0) AS Total
            FROM {table} t
            {join ?? string.Empty}
            WHERE YEAR(t.{dateCol}) = @Year {(extraWhere != null ? " AND " + extraWhere : "")}
            GROUP BY MONTH(t.{dateCol})";
        try
        {
            var dt = await _db.QueryAsync(sql, new() { ["Year"] = year });
            var result = new decimal[12];
            foreach (System.Data.DataRow r in dt.Rows)
            {
                var mo = Convert.ToInt32(r["Mnth"]);
                if (mo is >= 1 and <= 12) result[mo - 1] = Convert.ToDecimal(r["Total"]);
            }
            return result;
        }
        catch { return new decimal[12]; }
    }

    public async Task<DashboardChartData> GetChartDataAsync(ChartPeriod period)
    {
        var tenantId = _currentTenantAccessor?.TenantId ?? Guid.Empty;
        var cacheKey = $"dashboard_chart_{period}_{tenantId}";
        if (_cache.TryGetValue(cacheKey, out DashboardChartData? cached) && cached != null)
            return cached;

        var now = DateTime.Now;
        try
        {
            var result = period switch
            {
                ChartPeriod.ThisMonth      => await GetThisMonthChartAsync(now),
                ChartPeriod.ThisYear       => await GetCalendarYearChartAsync(now.Year),
                ChartPeriod.FyToDate       => await GetFyToDateChartAsync(now),
                ChartPeriod.LastYear       => await GetCalendarYearChartAsync(now.Year - 1),
                ChartPeriod.SinceInception => await GetSinceInceptionChartAsync(),
                _                          => await GetThisMonthChartAsync(now),
            };
            _cache.Set(cacheKey, result, CacheDuration);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading chart data for {Period}", period);
            return new(new decimal[12], new decimal[12], new decimal[12],
                new[]{"Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec"});
        }
    }

    private async Task<DashboardChartData> GetThisMonthChartAsync(DateTime now)
    {
        var start = new DateTime(now.Year, now.Month, 1);
        var end = start.AddMonths(1);
        var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
        var labels = Enumerable.Range(1, daysInMonth).Select(d => d.ToString()).ToArray();
        
        var qMap = await GetDailyTotalsInRange("Quotes", "QuoteDate", "t.NettPriceTotal", start, end);
        var iMap = await GetDailyTotalsInRange("Invoices", "InvoiceDate", "t.NettPriceTotal", start, end);
        var pMap = await GetDailyTotalsInRange("PurchaseOrders", "PODate", "t.PriceExTotal", start, end);
        
        return new(
            Enumerable.Range(1, daysInMonth).Select(d => qMap.GetValueOrDefault(d)).ToArray(),
            Enumerable.Range(1, daysInMonth).Select(d => iMap.GetValueOrDefault(d)).ToArray(),
            Enumerable.Range(1, daysInMonth).Select(d => pMap.GetValueOrDefault(d)).ToArray(),
            labels);
    }

    private async Task<Dictionary<int, decimal>> GetDailyTotalsInRange(string table, string dateCol, string valueExpr, DateTime start, DateTime end)
    {
        var sql = $@"
            SELECT DAY(t.{dateCol}) AS Dy, ISNULL(SUM({valueExpr}), 0) AS Total
            FROM {table} t
            WHERE t.{dateCol} >= @S AND t.{dateCol} < @E
            GROUP BY DAY(t.{dateCol})";
        var dict = new Dictionary<int, decimal>();
        try
        {
            var dt = await _db.QueryAsync(sql, new() { ["S"] = start, ["E"] = end });
            foreach (System.Data.DataRow r in dt.Rows)
                dict[Convert.ToInt32(r["Dy"])] = Convert.ToDecimal(r["Total"]);
        }
        catch { }
        return dict;
    }

    private async Task<DashboardChartData> GetCalendarYearChartAsync(int year)
    {
        var quotes   = await GetMonthlyTotals("Quotes", "QuoteDate", "t.NettPriceTotal", year,
            join: "LEFT JOIN QuoteStatus qs ON qs.QuoteStatusId = t.QuoteStatusId",
            extraWhere: "(qs.QuoteStatus IS NULL OR qs.QuoteStatus NOT LIKE '%declined%' AND qs.QuoteStatus NOT LIKE '%reject%')");
        var invoices = await GetMonthlyTotals("Invoices", "InvoiceDate", "t.NettPriceTotal", year);
        var pos      = await GetMonthlyTotals("PurchaseOrders", "PODate", "t.PriceExTotal", year);
        return new(quotes, invoices, pos, new[]{"Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec"});
    }

    private async Task<DashboardChartData> GetFyToDateChartAsync(DateTime now)
    {
        var fyYear  = now.Month >= 7 ? now.Year : now.Year - 1;
        var fyStart = new DateTime(fyYear, 7, 1);
        var fyEnd   = now.Date.AddDays(1);
        var months = new List<(int Year, int Month)>();
        for (var d = fyStart; d <= new DateTime(now.Year, now.Month, 1); d = d.AddMonths(1))
            months.Add((d.Year, d.Month));
        if (months.Count == 0) return await GetCalendarYearChartAsync(fyYear);
        var spansYears = months.Select(m => m.Year).Distinct().Count() > 1;
        string Label((int Year, int Month) m) => spansYears ? $"{new DateTime(m.Year, m.Month, 1):MMM}'{m.Year % 100:D2}" : new DateTime(m.Year, m.Month, 1).ToString("MMM");
        var qMap = await GetMonthlyTotalsInRange("Quotes", "QuoteDate", "t.NettPriceTotal", fyStart, fyEnd,
            join: "LEFT JOIN QuoteStatus qs ON qs.QuoteStatusId = t.QuoteStatusId", extraWhere: "(qs.QuoteStatus IS NULL OR qs.QuoteStatus NOT LIKE '%declined%' AND qs.QuoteStatus NOT LIKE '%reject%')");
        var iMap = await GetMonthlyTotalsInRange("Invoices", "InvoiceDate", "t.NettPriceTotal", fyStart, fyEnd);
        var pMap = await GetMonthlyTotalsInRange("PurchaseOrders", "PODate", "t.PriceExTotal", fyStart, fyEnd);
        return new(months.Select(m => qMap.GetValueOrDefault(m)).ToArray(), months.Select(m => iMap.GetValueOrDefault(m)).ToArray(), months.Select(m => pMap.GetValueOrDefault(m)).ToArray(), months.Select(Label).ToArray());
    }

    private async Task<DashboardChartData> GetSinceInceptionChartAsync()
    {
        var quotes = await GetAnnualTotals("Quotes", "QuoteDate", "t.NettPriceTotal",
            join: "LEFT JOIN QuoteStatus qs ON qs.QuoteStatusId = t.QuoteStatusId",
            extraWhere: "(qs.QuoteStatus IS NULL OR qs.QuoteStatus NOT LIKE '%declined%' AND qs.QuoteStatus NOT LIKE '%reject%')");
        var invoices = await GetAnnualTotals("Invoices", "InvoiceDate", "t.NettPriceTotal");
        var pos = await GetAnnualTotals("PurchaseOrders", "PODate", "t.PriceExTotal");
        var allYears = quotes.Keys.Union(invoices.Keys).Union(pos.Keys).OrderBy(y => y).ToArray();
        if (allYears.Length <= 1) return await GetCalendarYearChartAsync(allYears.Length == 1 ? allYears[0] : DateTime.Now.Year);
        return new(allYears.Select(y => quotes.GetValueOrDefault(y)).ToArray(), allYears.Select(y => invoices.GetValueOrDefault(y)).ToArray(), allYears.Select(y => pos.GetValueOrDefault(y)).ToArray(), allYears.Select(y => y.ToString()).ToArray());
    }

    private async Task<Dictionary<(int Year, int Month), decimal>> GetMonthlyTotalsInRange(string table, string dateCol, string valueExpr, DateTime start, DateTime end, string? join = null, string? extraWhere = null)
    {
        var sql = $@"
            SELECT YEAR(t.{dateCol}) AS Yr, MONTH(t.{dateCol}) AS Mo, ISNULL(SUM({valueExpr}), 0) AS Total
            FROM {table} t {join ?? string.Empty}
            WHERE t.{dateCol} >= @S AND t.{dateCol} < @E {(extraWhere != null ? " AND " + extraWhere : "")}
            GROUP BY YEAR(t.{dateCol}), MONTH(t.{dateCol})";
        var dict = new Dictionary<(int, int), decimal>();
        try
        {
            var dt = await _db.QueryAsync(sql, new() { ["S"] = start, ["E"] = end });
            foreach (System.Data.DataRow r in dt.Rows)
                dict[(Convert.ToInt32(r["Yr"]), Convert.ToInt32(r["Mo"]))] = Convert.ToDecimal(r["Total"]);
        }
        catch { }
        return dict;
    }

    private async Task<Dictionary<int, decimal>> GetAnnualTotals(string table, string dateCol, string valueExpr, string? join = null, string? extraWhere = null)
    {
        var sql = $@"
            SELECT YEAR(t.{dateCol}) AS Yr, ISNULL(SUM({valueExpr}), 0) AS Total
            FROM {table} t {join ?? string.Empty}
            WHERE t.{dateCol} IS NOT NULL {(extraWhere != null ? " AND " + extraWhere : "")}
            GROUP BY YEAR(t.{dateCol})";
        var dict = new Dictionary<int, decimal>();
        try
        {
            var dt = await _db.QueryAsync(sql, new Dictionary<string, object?>());
            foreach (System.Data.DataRow r in dt.Rows)
                dict[Convert.ToInt32(r["Yr"])] = Convert.ToDecimal(r["Total"]);
        }
        catch { }
        return dict;
    }
}
