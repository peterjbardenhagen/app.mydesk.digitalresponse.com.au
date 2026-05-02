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

    public DashboardService(DatabaseService db, ActivityService activity, ILogger<DashboardService> logger)
    {
        _db       = db;
        _activity = activity;
        _logger   = logger;
    }

    public async Task<DashboardMetrics> GetMetricsAsync(string? originatorCode = null)
    {
        return await GetMetricsAsync(ChartPeriod.ThisYear, originatorCode);
    }

    public async Task<DashboardMetrics> GetMetricsAsync(ChartPeriod period, string? originatorCode = null)
    {
        var m = new DashboardMetrics();
        try
        {
            var now            = DateTime.Now;
            var thisMonth      = new DateTime(now.Year, now.Month, 1);
            var lastMonth      = thisMonth.AddMonths(-1);
            
            // Calculate period date range based on selected period
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
            
            // Use period dates for "this month" queries - this makes all KPI cards respond to period change
            var queryStart = periodStart;
            var queryEnd = periodEnd;

            // ── Quotes for selected period ──────────────────────────────────
            m.ThisMonthQuotes = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Quotes WHERE QuoteDate >= @S AND QuoteDate <= @E", p2(queryStart, queryEnd));

            m.ThisMonthQuotesWon = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Quotes WHERE QuoteDate >= @S AND QuoteDate <= @E AND QuoteStatusId IN (4,10)", 
                p2(queryStart, queryEnd));

            m.ThisMonthQuotesValue = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(NettPriceTotal),0) FROM Quotes WHERE QuoteDate >= @S AND QuoteDate <= @E", 
                p2(queryStart, queryEnd));

            // Last period comparison (for trend)
            var lastPeriodStart = periodStart.AddTicks(-1).AddDays(1 - periodStart.Day); // Previous period start
            if (period == ChartPeriod.ThisMonth) lastPeriodStart = lastMonth;
            else if (period == ChartPeriod.ThisYear) lastPeriodStart = new DateTime(now.Year - 1, 1, 1);
            else if (period == ChartPeriod.FyToDate) lastPeriodStart = new DateTime(now.Month >= 7 ? now.Year - 1 : now.Year - 2, 7, 1);
            else if (period == ChartPeriod.LastYear) lastPeriodStart = new DateTime(now.Year - 2, 1, 1);
            
            m.LastMonthQuotesWon = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Quotes WHERE QuoteDate >= @S AND QuoteDate <= @E AND QuoteStatusId IN (4,10)",
                p2(lastPeriodStart, periodStart.AddTicks(-1)));

            // ── Invoices for selected period ─────────────────────────────────
            m.ThisMonthInvoices = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Invoices WHERE InvoiceDate >= @S AND InvoiceDate <= @E", p2(queryStart, queryEnd));

            m.ThisMonthInvoiceValue = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(NettPriceTotal),0) FROM Invoices WHERE InvoiceDate >= @S AND InvoiceDate <= @E", 
                p2(queryStart, queryEnd));

            m.LastMonthInvoices = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Invoices WHERE InvoiceDate >= @S AND InvoiceDate <= @E",
                p2(lastPeriodStart, periodStart.AddTicks(-1)));

            // ── Purchase Orders for selected period ─────────────────────────
            m.ThisMonthPOs = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM PurchaseOrders WHERE PODate >= @S AND PODate <= @E", p2(queryStart, queryEnd));

            m.ThisMonthPOValue = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(PriceExTotal),0) FROM PurchaseOrders WHERE PODate >= @S AND PODate <= @E", p2(queryStart, queryEnd));

            m.LastMonthPOs = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM PurchaseOrders WHERE PODate >= @S AND PODate <= @E",
                p2(lastPeriodStart, periodStart.AddTicks(-1)));

            // ── Despatch this month ──────────────────────────────────────────
            try
            {
                m.ThisMonthDespatch = await _db.ScalarAsync<int>(
                    "SELECT COUNT(*) FROM Despatch WHERE DespatchDate >= @S", p(thisMonth));
            }
            catch { }

            // ── YTD ─────────────────────────────────────────────────────────
            m.YtdQuotesWon = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Quotes WHERE QuoteDate >= @S AND QuoteStatusId IN (4,10)", p(ytdStart));

            m.YtdQuotesValue = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(NettPriceTotal),0) FROM Quotes WHERE QuoteDate >= @S AND QuoteStatusId IN (4,10)",
                p(ytdStart));

            m.YtdInvoices = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Invoices WHERE InvoiceDate >= @S", p(ytdStart));

            m.YtdInvoiceValue = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(NettPriceTotal),0) FROM Invoices WHERE InvoiceDate >= @S", p(ytdStart));

            m.LastYearYtdQuotesValue = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(NettPriceTotal),0) FROM Quotes WHERE QuoteDate >= @S AND QuoteDate <= @E AND QuoteStatusId IN (4,10)",
                p2(lastYtdStart, lastYtdEnd));

            // ── Alerts ───────────────────────────────────────────────────────
            m.PendingQuotesOver30Days = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Quotes WHERE QuoteStatusId IN (1,2) AND QuoteDate < @S",
                p(thirtyDaysAgo));

            m.InvoicesOverdue = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Invoices WHERE InvoiceStatusId = 2 AND InvoiceDate < @S",
                p(thirtyDaysAgo));

            m.PendingApprovalPOs = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM PurchaseOrders WHERE POStatusId = 2");

            // ── Monthly breakdowns ───────────────────────────────────────────
            m.MonthlyQuotesThisYear   = await GetMonthlyTotals("Quotes", "QuoteDate",
                "t.NettPriceTotal", now.Year,
                join: "LEFT JOIN QuoteStatus qs ON qs.QuoteStatusId = t.QuoteStatusId",
                extraWhere: "(qs.QuoteStatus IS NULL OR qs.QuoteStatus NOT LIKE '%lost%')");
            m.MonthlyQuotesLastYear   = await GetMonthlyTotals("Quotes", "QuoteDate",
                "t.NettPriceTotal", now.Year - 1,
                join: "LEFT JOIN QuoteStatus qs ON qs.QuoteStatusId = t.QuoteStatusId",
                extraWhere: "(qs.QuoteStatus IS NULL OR qs.QuoteStatus NOT LIKE '%lost%')");
            m.MonthlyInvoicesThisYear = await GetMonthlyTotals("Invoices",       "InvoiceDate", "t.NettPriceTotal",      now.Year);
            m.MonthlyPOsThisYear      = await GetMonthlyTotals("PurchaseOrders", "PODate",      "t.PriceExTotal", now.Year);

            // ── Activity feed ────────────────────────────────────────────────
            m.RecentActivity = await _activity.GetRecentAsync(30);

            // ── Advanced Business KPIs ─────────────────────────────────────────
            await CalculateAdvancedKPIsAsync(m, now, queryStart, queryEnd, lastPeriodStart, ytdStart, lastYtdStart, lastYtdEnd);
            
            // ── Team Performance ───────────────────────────────────────────────
            m.TeamMemberKPIs = await GetTeamMemberKPIsAsync(periodStart, periodEnd, ytdStart);
            m.DivisionPerformance = await GetDivisionPerformanceAsync(periodStart, periodEnd, ytdStart);
            
            // ── Health Indicators ────────────────────────────────────────────────
            m.Warnings = await GenerateWarningsAsync(m);
            m.Recommendations = await GenerateRecommendationsAsync(m);
            m.OverallHealthScore = CalculateHealthScore(m);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard metrics");
        }
        return m;
    }

    private async Task CalculateAdvancedKPIsAsync(DashboardMetrics m, DateTime now, DateTime periodStart, DateTime periodEnd, DateTime lastPeriodStart, DateTime ytdStart, DateTime lastYtdStart, DateTime lastYtdEnd)
    {
        try
        {
            // Average Quote Value (for the selected period)
            m.AverageQuoteValue = m.ThisMonthQuotes > 0 
                ? m.ThisMonthQuotesValue / m.ThisMonthQuotes 
                : 0;

            // Average Invoice Value (for the selected period)
            m.AverageInvoiceValue = m.ThisMonthInvoices > 0 
                ? m.ThisMonthInvoiceValue / m.ThisMonthInvoices 
                : 0;

            // Open Quotes Count (current snapshot, not period-filtered)
            m.OpenQuotesCount = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Quotes WHERE QuoteStatusId IN (1, 2, 9)");

            // Pipeline Value (open quotes)
            m.PipelineValue = await _db.ScalarAsync<decimal>(
                @"SELECT ISNULL(SUM(NettPriceTotal), 0) FROM Quotes q 
                  LEFT JOIN QuoteStatus qs ON qs.QuoteStatusId = q.QuoteStatusId
                  WHERE q.QuoteStatusId IN (1, 2, 9) 
                  AND (qs.QuoteStatus IS NULL OR qs.QuoteStatus NOT LIKE '%lost%')");

            // Gross Profit Margin (based on YTD for comparison stability)
            var quotesWonValue = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(NettPriceTotal), 0) FROM Quotes WHERE QuoteDate >= @S AND QuoteStatusId IN (4,10)",
                p(ytdStart));
            var quotesWonCost = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(UnitCostTotal), 0) FROM Quotes WHERE QuoteDate >= @S AND QuoteStatusId IN (4,10)",
                p(ytdStart));
            m.GrossProfitMargin = quotesWonValue > 0 
                ? ((quotesWonValue - quotesWonCost) / quotesWonValue) * 100 
                : 0;

            // Quote to Invoice Conversion Rate (YTD based)
            var totalQuotesYTD = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Quotes WHERE QuoteDate >= @S", p(ytdStart));
            var convertedQuotesYTD = await _db.ScalarAsync<int>(
                @"SELECT COUNT(DISTINCT q.Qid) FROM Quotes q 
                  INNER JOIN Invoices i ON i.Qid = q.Qid 
                  WHERE q.QuoteDate >= @S", p(ytdStart));
            m.QuoteToInvoiceConversionRate = totalQuotesYTD > 0 
                ? ((decimal)convertedQuotesYTD / totalQuotesYTD) * 100 
                : 0;

            // Growth Calculations (comparing selected period to previous period)
            var lastPeriodEnd = lastPeriodStart.AddTicks(periodEnd.Subtract(periodStart).Ticks);
            var lastPeriodValue = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(NettPriceTotal), 0) FROM Invoices WHERE InvoiceDate >= @S AND InvoiceDate <= @E",
                p2(lastPeriodStart, lastPeriodEnd));
            
            // Same period last year (for YoY comparison)
            var lastYearPeriodStart = periodStart.AddYears(-1);
            var lastYearPeriodEnd = periodStart.AddYears(-1).AddTicks(periodEnd.Subtract(periodStart).Ticks);
            var lastYearThisPeriodValue = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(NettPriceTotal), 0) FROM Invoices WHERE InvoiceDate >= @S AND InvoiceDate <= @E",
                p2(lastYearPeriodStart, lastYearPeriodEnd));

            m.ThisMonthVsLastMonthPercent = lastPeriodValue > 0 
                ? ((m.ThisMonthInvoiceValue - lastPeriodValue) / lastPeriodValue) * 100 
                : 0;
            m.ThisMonthVsLastYearPercent = lastYearThisPeriodValue > 0 
                ? ((m.ThisMonthInvoiceValue - lastYearThisPeriodValue) / lastYearThisPeriodValue) * 100 
                : 0;

            // YTD Growth
            var lastYearYtdInvoices = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(NettPriceTotal), 0) FROM Invoices WHERE InvoiceDate >= @S AND InvoiceDate <= @E",
                p2(lastYtdStart, lastYtdEnd));
            m.YtdRevenueGrowth = lastYearYtdInvoices > 0 
                ? ((m.YtdInvoiceValue - lastYearYtdInvoices) / lastYearYtdInvoices) * 100 
                : 0;

            // Monthly growth tracking
            m.MonthOverMonthGrowth = m.ThisMonthVsLastMonthPercent;
            m.YearOverYearGrowth = m.ThisMonthVsLastYearPercent;

            // Projected Monthly Revenue (based on current run rate for This Month period)
            if (periodStart.Month == now.Month && periodStart.Year == now.Year)
            {
                var daysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
                var dayOfMonth = now.Day;
                m.ProjectedMonthlyRevenue = dayOfMonth > 0 
                    ? m.ThisMonthInvoiceValue / dayOfMonth * daysInMonth 
                    : m.ThisMonthInvoiceValue;
            }
            else
            {
                m.ProjectedMonthlyRevenue = m.ThisMonthInvoiceValue;
            }

            // Quarterly calculations
            var quarterStart = new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1);
            m.QuarterlyTarget = 500000; // Example: $500k quarterly target
            m.QuarterlyProgress = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(NettPriceTotal), 0) FROM Invoices WHERE InvoiceDate >= @S",
                p(quarterStart));

            // Comparative data
            m.LastYearMonthlyRevenue = await GetMonthlyTotals("Invoices", "InvoiceDate", "t.NettPriceTotal", now.Year - 1);
            
            // Target revenue (example: 10% growth over last year)
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
            // Get active users (simplified for legacy database)
            var users = await _db.QueryAsync(@"
                SELECT DISTINCT Code, Name, IsDirector = 0
                FROM Users
                WHERE Active = 1 AND Code IS NOT NULL");

            foreach (System.Data.DataRow user in users.Rows)
            {
                var code = user["Code"].ToString() ?? "";
                var name = user["Name"].ToString() ?? "";
                var isDirector = Convert.ToInt32(user["IsDirector"]) == 1;

                var kpi = new UserKPI
                {
                    UserCode = code,
                    UserName = name,
                    IsDirector = isDirector,
                    QuotesRaisedThisMonth = await _db.ScalarAsync<int>(
                        "SELECT COUNT(*) FROM Quotes WHERE Code = @C AND QuoteDate >= @S AND QuoteDate <= @E",
                        new() { ["C"] = code, ["S"] = periodStart, ["E"] = periodEnd }),
                    QuotesWonThisMonth = await _db.ScalarAsync<int>(
                        "SELECT COUNT(*) FROM Quotes WHERE Code = @C AND QuoteDate >= @S AND QuoteDate <= @E AND QuoteStatusId IN (4,10)",
                        new() { ["C"] = code, ["S"] = periodStart, ["E"] = periodEnd }),
                    QuoteValueThisMonth = await _db.ScalarAsync<decimal>(
                        "SELECT ISNULL(SUM(NettPriceTotal), 0) FROM Quotes WHERE Code = @C AND QuoteDate >= @S AND QuoteDate <= @E",
                        new() { ["C"] = code, ["S"] = periodStart, ["E"] = periodEnd }),
                    InvoicesClosedThisMonth = await _db.ScalarAsync<int>(
                        "SELECT COUNT(*) FROM Invoices WHERE Code = @C AND InvoiceDate >= @S AND InvoiceDate <= @E",
                        new() { ["C"] = code, ["S"] = periodStart, ["E"] = periodEnd }),
                    InvoiceValueThisMonth = await _db.ScalarAsync<decimal>(
                        "SELECT ISNULL(SUM(NettPriceTotal), 0) FROM Invoices WHERE Code = @C AND InvoiceDate >= @S AND InvoiceDate <= @E",
                        new() { ["C"] = code, ["S"] = periodStart, ["E"] = periodEnd }),
                    YtdRevenue = await _db.ScalarAsync<decimal>(
                        "SELECT ISNULL(SUM(NettPriceTotal), 0) FROM Invoices WHERE Code = @C AND InvoiceDate >= @S",
                        new() { ["C"] = code, ["S"] = ytdStart }),
                    PendingQuotes = await _db.ScalarAsync<int>(
                        "SELECT COUNT(*) FROM Quotes WHERE Code = @C AND QuoteStatusId IN (1, 2)",
                        new() { ["C"] = code }),
                    OverdueQuotes = await _db.ScalarAsync<int>(
                        "SELECT COUNT(*) FROM Quotes WHERE Code = @C AND QuoteStatusId IN (1, 2) AND QuoteDate < DATEADD(day, -30, GETDATE())",
                        new() { ["C"] = code })
                };

                kpi.AverageQuoteValue = kpi.QuotesRaisedThisMonth > 0 
                    ? kpi.QuoteValueThisMonth / kpi.QuotesRaisedThisMonth 
                    : 0;
                kpi.WinRate = kpi.QuotesRaisedThisMonth > 0 
                    ? ((decimal)kpi.QuotesWonThisMonth / kpi.QuotesRaisedThisMonth) * 100 
                    : 0;

                // Calculate trend (compare to previous period)
                var lastPeriodValue = await _db.ScalarAsync<decimal>(
                    "SELECT ISNULL(SUM(NettPriceTotal), 0) FROM Invoices WHERE Code = @C AND InvoiceDate >= DATEADD(month, -1, @S) AND InvoiceDate < @S",
                    new() { ["C"] = code, ["S"] = periodStart });
                kpi.PerformanceTrend = kpi.InvoiceValueThisMonth > lastPeriodValue ? "up" : 
                                       kpi.InvoiceValueThisMonth < lastPeriodValue ? "down" : "stable";

                kpis.Add(kpi);
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
            var dt = await _db.QueryAsync("SELECT DivisionId, Division as DivisionName FROM Divisions WHERE Visible = 1 ORDER BY Division");

            foreach (System.Data.DataRow r in dt.Rows)
            {
                var divisionId = Convert.ToInt32(r["DivisionId"]);
                var divisionName = r["DivisionName"].ToString() ?? "";

                var thisMonthRevenue = await _db.ScalarAsync<decimal>(
                    @"SELECT ISNULL(SUM(NettPriceTotal), 0) FROM Invoices 
                      WHERE DivisionId = @D AND InvoiceDate >= @S AND InvoiceDate <= @E",
                    new() { ["D"] = divisionId, ["S"] = periodStart, ["E"] = periodEnd });

                var ytdRevenue = await _db.ScalarAsync<decimal>(
                    @"SELECT ISNULL(SUM(NettPriceTotal), 0) FROM Invoices 
                      WHERE DivisionId = @D AND InvoiceDate >= @Y",
                    new() { ["D"] = divisionId, ["Y"] = ytdStart });

                var quotesCount = await _db.ScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Quotes 
                      WHERE DivisionId = @D AND QuoteDate >= @S AND QuoteDate <= @E",
                    new() { ["D"] = divisionId, ["S"] = periodStart, ["E"] = periodEnd });

                var invoicesCount = await _db.ScalarAsync<int>(
                    @"SELECT COUNT(*) FROM Invoices 
                      WHERE DivisionId = @D AND InvoiceDate >= @S AND InvoiceDate <= @E",
                    new() { ["D"] = divisionId, ["S"] = periodStart, ["E"] = periodEnd });

                divisions.Add(new DivisionPerformance
                {
                    DivisionId = divisionId,
                    DivisionName = divisionName,
                    ThisMonthRevenue = thisMonthRevenue,
                    YtdRevenue = ytdRevenue,
                    QuotesCount = quotesCount,
                    InvoicesCount = invoicesCount,
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
        {
            warnings.Add(new BusinessWarning
            {
                Id = "overdue-invoices",
                Severity = m.InvoicesOverdue > 10 ? "critical" : "warning",
                Category = "Cash Flow",
                Title = $"{m.InvoicesOverdue} Overdue Invoices",
                Description = "Outstanding invoices over 30 days old may impact cash flow",
                Metric = "Invoices > 30 days",
                CurrentValue = m.InvoicesOverdue,
                Threshold = 5,
                ActionLink = "/invoices?filter=overdue"
            });
        }

        if (m.PendingQuotesOver30Days > 10)
        {
            warnings.Add(new BusinessWarning
            {
                Id = "stale-quotes",
                Severity = "warning",
                Category = "Sales",
                Title = $"{m.PendingQuotesOver30Days} Quotes Open > 30 Days",
                Description = "Stale quotes may need follow-up or closure",
                Metric = "Open quotes > 30 days",
                CurrentValue = m.PendingQuotesOver30Days,
                Threshold = 10,
                ActionLink = "/quotes?status=overdue"
            });
        }

        if (m.GrossProfitMargin < 20)
        {
            warnings.Add(new BusinessWarning
            {
                Id = "low-margin",
                Severity = "critical",
                Category = "Profitability",
                Title = "Low Gross Profit Margin",
                Description = $"Current margin {m.GrossProfitMargin:N1}% is below healthy threshold of 20%",
                Metric = "Gross Profit Margin",
                CurrentValue = m.GrossProfitMargin,
                Threshold = 20,
                ActionLink = "/reports/margins"
            });
        }

        if (m.QuoteToInvoiceConversionRate < 30)
        {
            warnings.Add(new BusinessWarning
            {
                Id = "low-conversion",
                Severity = "warning",
                Category = "Sales",
                Title = "Low Quote Conversion Rate",
                Description = $"Only {m.QuoteToInvoiceConversionRate:N1}% of quotes are converting to invoices",
                Metric = "Conversion Rate",
                CurrentValue = m.QuoteToInvoiceConversionRate,
                Threshold = 30,
                ActionLink = "/quotes?filter=analysis"
            });
        }

        return warnings;
    }

    private async Task<List<BusinessRecommendation>> GenerateRecommendationsAsync(DashboardMetrics m)
    {
        var recs = new List<BusinessRecommendation>();

        if (m.PendingApprovalPOs > 0)
        {
            recs.Add(new BusinessRecommendation
            {
                Id = "approve-pos",
                Priority = "high",
                Category = "Operations",
                Title = $"Approve {m.PendingApprovalPOs} Pending POs",
                Description = "Purchase orders awaiting approval may delay project delivery",
                ExpectedImpact = "Faster project delivery",
                ActionLink = "/purchase-orders?status=pending"
            });
        }

        if (m.OpenQuotesCount > 20)
        {
            recs.Add(new BusinessRecommendation
            {
                Id = "follow-up-quotes",
                Priority = "medium",
                Category = "Sales",
                Title = "Follow Up on Open Quotes",
                Description = $"{m.OpenQuotesCount} open quotes worth {m.PipelineValue:C0} need follow-up",
                ExpectedImpact = $"Potential {(m.PipelineValue * 0.3m):C0} revenue",
                ActionLink = "/quotes?status=open"
            });
        }

        if (m.ThisMonthVsLastMonthPercent < 0)
        {
            recs.Add(new BusinessRecommendation
            {
                Id = "boost-sales",
                Priority = "high",
                Category = "Revenue",
                Title = "Revenue Declining",
                Description = $"This month is {Math.Abs(m.ThisMonthVsLastMonthPercent):N1}% below last month",
                ExpectedImpact = "Identify opportunities to close more business",
                ActionLink = "/quotes?filter=opportunities"
            });
        }

        if (m.AverageQuoteValue < 5000)
        {
            recs.Add(new BusinessRecommendation
            {
                Id = "increase-avg-value",
                Priority = "medium",
                Category = "Sales",
                Title = "Increase Average Quote Value",
                Description = $"Current average is {m.AverageQuoteValue:C0}. Consider upselling or bundling",
                ExpectedImpact = "Higher revenue per transaction",
                ActionLink = "/reports/quote-analysis"
            });
        }

        return recs;
    }

    private int CalculateHealthScore(DashboardMetrics m)
    {
        int score = 70; // Base score

        // Positive factors
        if (m.GrossProfitMargin > 30) score += 10;
        if (m.QuoteToInvoiceConversionRate > 40) score += 5;
        if (m.ThisMonthVsLastMonthPercent > 0) score += 5;
        if (m.YtdRevenueGrowth > 10) score += 5;
        if (m.InvoicesOverdue == 0) score += 5;

        // Negative factors
        if (m.InvoicesOverdue > 10) score -= 10;
        if (m.InvoicesOverdue > 5) score -= 5;
        if (m.GrossProfitMargin < 20) score -= 10;
        if (m.GrossProfitMargin < 15) score -= 10;
        if (m.QuoteToInvoiceConversionRate < 20) score -= 10;
        if (m.ThisMonthVsLastMonthPercent < -20) score -= 10;
        if (m.PendingQuotesOver30Days > 20) score -= 5;

        return Math.Clamp(score, 0, 100);
    }

    private static Dictionary<string, object?> p(DateTime start) =>
        new() { ["S"] = start };

    private static Dictionary<string, object?> p2(DateTime start, DateTime end) =>
        new() { ["S"] = start, ["E"] = end };

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

    // ── Chart period queries ─────────────────────────────────────────────────

    public async Task<DashboardChartData> GetChartDataAsync(ChartPeriod period)
    {
        var now = DateTime.Now;
        try
        {
            return period switch
            {
                ChartPeriod.ThisMonth      => await GetThisMonthChartAsync(now),
                ChartPeriod.ThisYear       => await GetCalendarYearChartAsync(now.Year),
                ChartPeriod.FyToDate       => await GetFyToDateChartAsync(now),
                ChartPeriod.LastYear       => await GetCalendarYearChartAsync(now.Year - 1),
                ChartPeriod.SinceInception => await GetSinceInceptionChartAsync(),
                _                          => await GetThisMonthChartAsync(now),
            };
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
        var quotes   = await GetMonthlyTotals("Quotes",         "QuoteDate",
            "t.NettPriceTotal", year,
            join: "LEFT JOIN QuoteStatus qs ON qs.QuoteStatusId = t.QuoteStatusId",
            extraWhere: "(qs.QuoteStatus IS NULL OR qs.QuoteStatus NOT LIKE '%lost%')");
        var invoices = await GetMonthlyTotals("Invoices",        "InvoiceDate", "t.NettPriceTotal",      year);
        var pos      = await GetMonthlyTotals("PurchaseOrders",  "PODate",      "t.PriceExTotal", year);
        return new(quotes, invoices, pos,
            new[]{"Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec"});
    }

    private async Task<DashboardChartData> GetFyToDateChartAsync(DateTime now)
    {
        // Australian FY starts 1 July.
        // If current month >= July the FY started in the current calendar year; otherwise the previous one.
        var fyYear  = now.Month >= 7 ? now.Year : now.Year - 1;
        var fyStart = new DateTime(fyYear, 7, 1);
        var fyEnd   = now.Date.AddDays(1);      // exclusive upper bound (includes today)

        // Build ordered list of (year, month) pairs from FY start to now
        var months = new List<(int Year, int Month)>();
        for (var d = fyStart; d <= new DateTime(now.Year, now.Month, 1); d = d.AddMonths(1))
            months.Add((d.Year, d.Month));

        if (months.Count == 0)
            return await GetCalendarYearChartAsync(fyYear);

        var spansYears = months.Select(m => m.Year).Distinct().Count() > 1;
        string Label((int Year, int Month) m) =>
            spansYears
                ? $"{new DateTime(m.Year, m.Month, 1):MMM}'{m.Year % 100:D2}"
                : new DateTime(m.Year, m.Month, 1).ToString("MMM");

        var qMap  = await GetMonthlyTotalsInRange("Quotes",        "QuoteDate",
            "t.NettPriceTotal", fyStart, fyEnd,
            join: "LEFT JOIN QuoteStatus qs ON qs.QuoteStatusId = t.QuoteStatusId",
            extraWhere: "(qs.QuoteStatus IS NULL OR qs.QuoteStatus NOT LIKE '%lost%')");
        var iMap  = await GetMonthlyTotalsInRange("Invoices",       "InvoiceDate", "t.NettPriceTotal",      fyStart, fyEnd);
        var pMap  = await GetMonthlyTotalsInRange("PurchaseOrders", "PODate",      "t.PriceExTotal", fyStart, fyEnd);

        return new(
            months.Select(m => qMap.GetValueOrDefault(m)).ToArray(),
            months.Select(m => iMap.GetValueOrDefault(m)).ToArray(),
            months.Select(m => pMap.GetValueOrDefault(m)).ToArray(),
            months.Select(Label).ToArray());
    }

    private async Task<DashboardChartData> GetSinceInceptionChartAsync()
    {
        var quotes   = await GetAnnualTotals("Quotes",        "QuoteDate",
            "t.NettPriceTotal",
            join: "LEFT JOIN QuoteStatus qs ON qs.QuoteStatusId = t.QuoteStatusId",
            extraWhere: "(qs.QuoteStatus IS NULL OR qs.QuoteStatus NOT LIKE '%lost%')");
        var invoices = await GetAnnualTotals("Invoices",       "InvoiceDate", "t.NettPriceTotal");
        var pos      = await GetAnnualTotals("PurchaseOrders", "PODate",      "t.PriceExTotal");

        var allYears = quotes.Keys.Union(invoices.Keys).Union(pos.Keys).OrderBy(y => y).ToArray();

        // If there's only one year of data fall back to monthly view for that year
        if (allYears.Length <= 1)
            return await GetCalendarYearChartAsync(allYears.Length == 1 ? allYears[0] : DateTime.Now.Year);

        return new(
            allYears.Select(y => quotes.GetValueOrDefault(y)).ToArray(),
            allYears.Select(y => invoices.GetValueOrDefault(y)).ToArray(),
            allYears.Select(y => pos.GetValueOrDefault(y)).ToArray(),
            allYears.Select(y => y.ToString()).ToArray());
    }

    private async Task<Dictionary<(int Year, int Month), decimal>> GetMonthlyTotalsInRange(
        string table, string dateCol, string valueExpr, DateTime start, DateTime end,
        string? join = null, string? extraWhere = null)
    {
        var sql = $@"
            SELECT YEAR(t.{dateCol}) AS Yr, MONTH(t.{dateCol}) AS Mo, ISNULL(SUM({valueExpr}), 0) AS Total
            FROM {table} t
            {join ?? string.Empty}
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

    private async Task<Dictionary<int, decimal>> GetAnnualTotals(
        string table, string dateCol, string valueExpr,
        string? join = null, string? extraWhere = null)
    {
        var sql = $@"
            SELECT YEAR(t.{dateCol}) AS Yr, ISNULL(SUM({valueExpr}), 0) AS Total
            FROM {table} t
            {join ?? string.Empty}
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
