using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

/// <summary>
/// High-value business intelligence: Targets, Customer Intelligence, Team Leaderboard.
/// Director/Admin-only insights for executive decision making.
/// </summary>
public class IntelligenceService
{
    private readonly DatabaseService _db;
    private readonly ITargetsProvider _targets;
    private readonly ILogger<IntelligenceService> _logger;

    public IntelligenceService(
        DatabaseService db,
        ITargetsProvider targets,
        ILogger<IntelligenceService> logger)
    {
        _db = db;
        _targets = targets;
        _logger = logger;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  PERFORMANCE TARGETS  (Company / Team / Individual)
    // ══════════════════════════════════════════════════════════════════════════
    public async Task<PerformanceTargets> GetPerformanceTargetsAsync()
    {
        var t = new PerformanceTargets();
        try
        {
            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var quarterStart = new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1);
            var yearStart = new DateTime(now.Year, 1, 1);

            // Period metadata
            t.DaysIntoMonth = now.Day;
            t.DaysInMonth = DateTime.DaysInMonth(now.Year, now.Month);
            t.DaysIntoQuarter = (now - quarterStart).Days + 1;
            t.DaysInQuarter = (quarterStart.AddMonths(3) - quarterStart).Days;
            t.DaysIntoYear = now.DayOfYear;
            t.DaysInYear = DateTime.IsLeapYear(now.Year) ? 366 : 365;

            // Config-driven targets
            t.CompanyMonthlyTarget = _targets.CompanyMonthlyTarget;
            t.CompanyQuarterlyTarget = _targets.CompanyQuarterlyTarget;
            t.CompanyYearlyTarget = _targets.CompanyYearlyTarget;

            // Team target = sum of user monthly targets (for active sales users)
            var activeUserCount = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Users WHERE Active = 1 AND Code IS NOT NULL");
            t.TeamMonthlyTarget = activeUserCount * _targets.DefaultUserMonthlyTarget;
            t.TeamQuarterlyTarget = activeUserCount * _targets.DefaultUserQuarterlyTarget;
            t.TeamYearlyTarget = activeUserCount * _targets.DefaultUserYearlyTarget;

            // Actuals from invoices (revenue)
            t.CompanyMonthlyActual = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(NettPriceTotal),0) FROM Invoices WHERE InvoiceDate >= @S",
                new() { ["S"] = monthStart });

            t.CompanyQuarterlyActual = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(NettPriceTotal),0) FROM Invoices WHERE InvoiceDate >= @S",
                new() { ["S"] = quarterStart });

            t.CompanyYearlyActual = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(NettPriceTotal),0) FROM Invoices WHERE InvoiceDate >= @S",
                new() { ["S"] = yearStart });

            // For the team actual, use the same values (all revenue attributed to sales)
            t.TeamMonthlyActual = t.CompanyMonthlyActual;
            t.TeamQuarterlyActual = t.CompanyQuarterlyActual;
            t.TeamYearlyActual = t.CompanyYearlyActual;

            // Forecasts (linear projection based on run rate)
            t.MonthlyForecast = t.DaysIntoMonth > 0
                ? t.CompanyMonthlyActual * t.DaysInMonth / t.DaysIntoMonth
                : 0;
            t.QuarterlyForecast = t.DaysIntoQuarter > 0
                ? t.CompanyQuarterlyActual * t.DaysInQuarter / t.DaysIntoQuarter
                : 0;
            t.YearlyForecast = t.DaysIntoYear > 0
                ? t.CompanyYearlyActual * t.DaysInYear / t.DaysIntoYear
                : 0;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating performance targets");
        }
        return t;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  TEAM LEADERBOARD
    // ══════════════════════════════════════════════════════════════════════════
    public async Task<TeamLeaderboard> GetTeamLeaderboardAsync()
    {
        var board = new TeamLeaderboard();
        try
        {
            var now = DateTime.Now;
            var monthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = monthStart.AddMonths(-1);
            var quarterStart = new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1);
            var yearStart = new DateTime(now.Year, 1, 1);

            var users = await _db.QueryAsync(
                "SELECT Code, Name FROM Users WHERE Active = 1 AND Code IS NOT NULL ORDER BY Name");

            foreach (System.Data.DataRow row in users.Rows)
            {
                var code = row["Code"].ToString() ?? "";
                var name = row["Name"].ToString() ?? "";

                var kpi = new UserKPI
                {
                    UserCode = code,
                    UserName = name,
                    MonthlyTarget = _targets.GetUserMonthlyTarget(code),
                    QuarterlyTarget = _targets.GetUserQuarterlyTarget(code),
                    YearlyTarget = _targets.GetUserYearlyTarget(code),

                    QuotesRaisedThisMonth = await _db.ScalarAsync<int>(
                        "SELECT COUNT(*) FROM Quotes WHERE Code=@C AND QuoteDate>=@S",
                        new() { ["C"] = code, ["S"] = monthStart }),
                    QuotesWonThisMonth = await _db.ScalarAsync<int>(
                        "SELECT COUNT(*) FROM Quotes WHERE Code=@C AND QuoteDate>=@S AND QuoteStatusId = 4",
                        new() { ["C"] = code, ["S"] = monthStart }),
                    QuoteValueThisMonth = await _db.ScalarAsync<decimal>(
                        "SELECT ISNULL(SUM(NettPriceTotal),0) FROM Quotes WHERE Code=@C AND QuoteDate>=@S",
                        new() { ["C"] = code, ["S"] = monthStart }),
                    InvoicesClosedThisMonth = await _db.ScalarAsync<int>(
                        "SELECT COUNT(*) FROM Invoices WHERE Code=@C AND InvoiceDate>=@S",
                        new() { ["C"] = code, ["S"] = monthStart }),
                    InvoiceValueThisMonth = await _db.ScalarAsync<decimal>(
                        "SELECT ISNULL(SUM(NettPriceTotal),0) FROM Invoices WHERE Code=@C AND InvoiceDate>=@S",
                        new() { ["C"] = code, ["S"] = monthStart }),
                    QuarterlyRevenue = await _db.ScalarAsync<decimal>(
                        "SELECT ISNULL(SUM(NettPriceTotal),0) FROM Invoices WHERE Code=@C AND InvoiceDate>=@S",
                        new() { ["C"] = code, ["S"] = quarterStart }),
                    YtdRevenue = await _db.ScalarAsync<decimal>(
                        "SELECT ISNULL(SUM(NettPriceTotal),0) FROM Invoices WHERE Code=@C AND InvoiceDate>=@S",
                        new() { ["C"] = code, ["S"] = yearStart }),
                    PendingQuotes = await _db.ScalarAsync<int>(
                        "SELECT COUNT(*) FROM Quotes WHERE Code=@C AND QuoteStatusId IN (1,2,3,6,7,8)",
                        new() { ["C"] = code }),
                    OverdueQuotes = await _db.ScalarAsync<int>(
                        "SELECT COUNT(*) FROM Quotes WHERE Code=@C AND QuoteStatusId IN (1,2,3,6,7,8) AND QuoteDate < DATEADD(day,-30,GETDATE())",
                        new() { ["C"] = code })
                };

                if (kpi.QuotesRaisedThisMonth > 0)
                {
                    kpi.AverageQuoteValue = kpi.QuoteValueThisMonth / kpi.QuotesRaisedThisMonth;
                    kpi.WinRate = ((decimal)kpi.QuotesWonThisMonth / kpi.QuotesRaisedThisMonth) * 100;
                }
                if (kpi.QuarterlyTarget > 0)
                    kpi.QuarterlyProgress = (kpi.QuarterlyRevenue / kpi.QuarterlyTarget) * 100;

                // Performance trend (compare to last month)
                var lastMonthRevenue = await _db.ScalarAsync<decimal>(
                    "SELECT ISNULL(SUM(NettPriceTotal),0) FROM Invoices WHERE Code=@C AND InvoiceDate>=@S AND InvoiceDate<@E",
                    new() { ["C"] = code, ["S"] = lastMonthStart, ["E"] = monthStart });
                if (lastMonthRevenue > 0)
                {
                    var diff = ((kpi.InvoiceValueThisMonth - lastMonthRevenue) / lastMonthRevenue) * 100;
                    kpi.PerformanceTrend = diff > 5 ? "up" : diff < -5 ? "down" : "stable";
                }
                else if (kpi.InvoiceValueThisMonth > 0)
                {
                    kpi.PerformanceTrend = "up";
                }

                // Only include users who've had at least *some* activity (quotes/invoices)
                if (kpi.QuotesRaisedThisMonth + kpi.InvoicesClosedThisMonth + kpi.PendingQuotes > 0
                    || kpi.YtdRevenue > 0)
                {
                    board.Members.Add(kpi);
                }
            }

            // Rank by this-month invoice value (revenue attributed to them)
            var ranked = board.Members.OrderByDescending(k => k.InvoiceValueThisMonth).ToList();
            for (int i = 0; i < ranked.Count; i++) ranked[i].Rank = i + 1;
            board.Members = ranked;

            board.TopPerformer = ranked.FirstOrDefault();
            board.MostImproved = ranked
                .Where(k => k.PerformanceTrend == "up")
                .OrderByDescending(k => k.InvoiceValueThisMonth)
                .FirstOrDefault();

            board.TotalMembers = ranked.Count;
            board.TeamTotalRevenue = ranked.Sum(k => k.InvoiceValueThisMonth);
            board.TeamAverageRevenue = ranked.Count > 0 ? board.TeamTotalRevenue / ranked.Count : 0;
            board.TeamAverageWinRate = ranked.Count > 0 ? ranked.Average(k => k.WinRate) : 0;
            board.MembersMeetingTarget = ranked.Count(k => k.MonthlyProgress >= 90);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error building team leaderboard");
        }
        return board;
    }

    public async Task<UserKPI?> GetUserKPIAsync(string userCode)
    {
        var board = await GetTeamLeaderboardAsync();
        return board.Members.FirstOrDefault(m =>
            string.Equals(m.UserCode, userCode, StringComparison.OrdinalIgnoreCase));
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  CUSTOMER INTELLIGENCE  (Best / Worst / At-Risk / Growth)
    // ══════════════════════════════════════════════════════════════════════════
    public async Task<CustomerIntelligence> GetCustomerIntelligenceAsync(int topN = 10)
    {
        var ci = new CustomerIntelligence();
        try
        {
            var now = DateTime.Now;
            var yearStart = new DateTime(now.Year, 1, 1);
            var lastYearStart = new DateTime(now.Year - 1, 1, 1);
            var lastYearSameDate = now.AddYears(-1);
            var ninetyDaysAgo = now.AddDays(-90);
            var oneEightyDaysAgo = now.AddDays(-180);

            // Aggregate all customer metrics in one pass (more efficient than per-customer loop)
            var sql = @"
                WITH CustomerStats AS (
                    SELECT
                        c.CompanyId,
                        c.Company,
                        ISNULL(SUM(CASE WHEN i.InvoiceDate >= @Y THEN i.NettPriceTotal ELSE 0 END), 0) AS YtdRevenue,
                        ISNULL(SUM(CASE WHEN i.InvoiceDate >= @LY AND i.InvoiceDate <= @LYE THEN i.NettPriceTotal ELSE 0 END), 0) AS LastYearRevenue,
                        ISNULL(SUM(i.NettPriceTotal), 0) AS LifetimeRevenue,
                        COUNT(DISTINCT i.InvoiceId) AS InvoiceCount,
                        MAX(i.InvoiceDate) AS LastInvoice
                    FROM Companies c
                    LEFT JOIN Contacts ct ON ct.CompanyId = c.CompanyId
                    LEFT JOIN Invoices i  ON i.ContactId  = ct.ContactId
                    WHERE c.Company IS NOT NULL AND c.Company <> ''
                    GROUP BY c.CompanyId, c.Company
                ),
                CustomerQuotes AS (
                    SELECT
                        c.CompanyId,
                        COUNT(DISTINCT q.Qid) AS QuoteCount,
                        ISNULL(SUM(q.NettPriceTotal), 0) AS QuoteValue,
                        SUM(CASE WHEN q.QuoteStatusId = 4 THEN 1 ELSE 0 END) AS WonQuotes
                    FROM Companies c
                    LEFT JOIN Contacts ct ON ct.CompanyId = c.CompanyId
                    LEFT JOIN Quotes   q  ON q.ContactId  = ct.ContactId
                    WHERE c.Company IS NOT NULL AND c.Company <> ''
                    GROUP BY c.CompanyId
                )
                SELECT
                    s.CompanyId, s.Company, s.YtdRevenue, s.LastYearRevenue, s.LifetimeRevenue,
                    s.InvoiceCount, s.LastInvoice,
                    ISNULL(q.QuoteCount,0) AS QuoteCount,
                    ISNULL(q.QuoteValue,0) AS QuoteValue,
                    ISNULL(q.WonQuotes,0)  AS WonQuotes
                FROM CustomerStats s
                LEFT JOIN CustomerQuotes q ON q.CompanyId = s.CompanyId
                WHERE s.LifetimeRevenue > 0 OR ISNULL(q.QuoteValue,0) > 0";

            var dt = await _db.QueryAsync(sql, new()
            {
                ["Y"] = yearStart,
                ["LY"] = lastYearStart,
                ["LYE"] = lastYearSameDate
            });

            var allCustomers = new List<CustomerPerformance>();
            foreach (System.Data.DataRow r in dt.Rows)
            {
                var cp = new CustomerPerformance
                {
                    CompanyId = Convert.ToInt32(r["CompanyId"]),
                    CompanyName = r["Company"].ToString() ?? "",
                    YtdRevenue = Convert.ToDecimal(r["YtdRevenue"]),
                    LastYearRevenue = Convert.ToDecimal(r["LastYearRevenue"]),
                    LifetimeRevenue = Convert.ToDecimal(r["LifetimeRevenue"]),
                    InvoiceCount = Convert.ToInt32(r["InvoiceCount"]),
                    QuoteCount = Convert.ToInt32(r["QuoteCount"]),
                    QuoteValue = Convert.ToDecimal(r["QuoteValue"])
                };

                if (r["LastInvoice"] != DBNull.Value)
                {
                    cp.LastActivity = Convert.ToDateTime(r["LastInvoice"]);
                    cp.DaysSinceLastActivity = (now - cp.LastActivity.Value).Days;
                }
                else
                {
                    cp.DaysSinceLastActivity = 999;
                }

                if (cp.InvoiceCount > 0)
                    cp.AverageInvoiceValue = cp.LifetimeRevenue / cp.InvoiceCount;

                var wonQuotes = Convert.ToInt32(r["WonQuotes"]);
                if (cp.QuoteCount > 0)
                    cp.WinRate = ((decimal)wonQuotes / cp.QuoteCount) * 100;

                if (cp.LastYearRevenue > 0)
                    cp.GrowthPercent = ((cp.YtdRevenue - cp.LastYearRevenue) / cp.LastYearRevenue) * 100;
                else if (cp.YtdRevenue > 0)
                    cp.GrowthPercent = 100;

                // Effort-to-value: quote value vs actual revenue delivered (how much "wasted" quoting)
                if (cp.YtdRevenue > 0 && cp.QuoteValue > 0)
                    cp.EffortToValueRatio = cp.QuoteValue / cp.YtdRevenue;
                else if (cp.QuoteValue > 0)
                    cp.EffortToValueRatio = 999;   // pure effort, no revenue

                cp.Rating = ClassifyCustomer(cp);
                allCustomers.Add(cp);
            }

            ci.TotalCustomers = allCustomers.Count;
            ci.ActiveCustomers = allCustomers.Count(c =>
                c.LastActivity.HasValue && c.LastActivity >= ninetyDaysAgo);
            ci.DormantCustomers = allCustomers.Count(c =>
                !c.LastActivity.HasValue || c.LastActivity < oneEightyDaysAgo);

            var totalYtd = allCustomers.Sum(c => c.YtdRevenue);
            ci.AverageCustomerValue = allCustomers.Count > 0 && totalYtd > 0
                ? totalYtd / allCustomers.Count(c => c.YtdRevenue > 0)
                : 0;

            // Best: top by YTD revenue
            ci.BestCustomers = allCustomers
                .Where(c => c.YtdRevenue > 0)
                .OrderByDescending(c => c.YtdRevenue)
                .Take(topN)
                .ToList();
            for (int i = 0; i < ci.BestCustomers.Count; i++) ci.BestCustomers[i].Rank = i + 1;

            // Top 10 concentration
            if (totalYtd > 0)
            {
                var top10Sum = allCustomers
                    .Where(c => c.YtdRevenue > 0)
                    .OrderByDescending(c => c.YtdRevenue)
                    .Take(10)
                    .Sum(c => c.YtdRevenue);
                ci.Top10CustomerConcentration = (top10Sum / totalYtd) * 100;
            }

            // Worst: high effort, low/no revenue (wasting time)
            ci.WorstCustomers = allCustomers
                .Where(c => c.QuoteCount >= 3 && c.EffortToValueRatio >= 5)
                .OrderByDescending(c => c.EffortToValueRatio)
                .ThenByDescending(c => c.QuoteValue)
                .Take(topN)
                .ToList();
            for (int i = 0; i < ci.WorstCustomers.Count; i++) ci.WorstCustomers[i].Rank = i + 1;

            // At-risk: was a significant customer, now declining
            ci.AtRiskCustomers = allCustomers
                .Where(c => c.LastYearRevenue > 10000 && c.GrowthPercent < -25)
                .OrderBy(c => c.GrowthPercent)
                .Take(topN)
                .ToList();
            for (int i = 0; i < ci.AtRiskCustomers.Count; i++) ci.AtRiskCustomers[i].Rank = i + 1;

            // Growth: fast-growing customers
            ci.GrowthCustomers = allCustomers
                .Where(c => c.YtdRevenue >= 5000 && c.GrowthPercent >= 25)
                .OrderByDescending(c => c.GrowthPercent)
                .Take(topN)
                .ToList();
            for (int i = 0; i < ci.GrowthCustomers.Count; i++) ci.GrowthCustomers[i].Rank = i + 1;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error calculating customer intelligence");
        }
        return ci;
    }

    private static string ClassifyCustomer(CustomerPerformance c)
    {
        if (c.YtdRevenue >= 500_000) return "Diamond";
        if (c.YtdRevenue >= 100_000) return "Gold";
        if (c.YtdRevenue >= 25_000)  return "Silver";
        if (c.YtdRevenue >= 5_000)   return "Bronze";
        if (c.QuoteValue > 0 && c.YtdRevenue == 0) return "Watch";
        return "New";
    }
}
