using Microsoft.Extensions.Logging;
using Techlight.MyDesk.Shared.Models;

namespace Techlight.MyDesk.Shared.Services;

public enum ChartPeriod { ThisYear, FyToDate, LastYear, SinceInception }

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
        var m = new DashboardMetrics();
        try
        {
            var now            = DateTime.Now;
            var thisMonth      = new DateTime(now.Year, now.Month, 1);
            var lastMonth      = thisMonth.AddMonths(-1);
            var ytdStart       = new DateTime(now.Year, 1, 1);
            var lastYtdStart   = new DateTime(now.Year - 1, 1, 1);
            var lastYtdEnd     = new DateTime(now.Year - 1, now.Month, 1).AddMonths(1).AddDays(-1);
            var thirtyDaysAgo  = now.AddDays(-30);

            // ── Quotes this month ────────────────────────────────────────────
            m.ThisMonthQuotes = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Quotes WHERE QuoteDate >= @S", p(thisMonth));

            m.ThisMonthQuotesWon = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Quotes WHERE QuoteDate >= @S AND QuoteStatusId IN (4,10)", p(thisMonth));

            m.ThisMonthQuotesValue = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(NettPriceTotal),0) FROM Quotes WHERE QuoteDate >= @S", p(thisMonth));

            m.LastMonthQuotesWon = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Quotes WHERE QuoteDate >= @S AND QuoteDate < @E AND QuoteStatusId IN (4,10)",
                p2(lastMonth, thisMonth));

            // ── Invoices this month ──────────────────────────────────────────
            m.ThisMonthInvoices = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Invoices WHERE InvoiceDate >= @S", p(thisMonth));

            m.ThisMonthInvoiceValue = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(Amount),0) FROM Invoices WHERE InvoiceDate >= @S", p(thisMonth));

            m.LastMonthInvoices = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Invoices WHERE InvoiceDate >= @S AND InvoiceDate < @E",
                p2(lastMonth, thisMonth));

            // ── Purchase Orders this month ───────────────────────────────────
            m.ThisMonthPOs = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM PurchaseOrders WHERE PODate >= @S", p(thisMonth));

            m.ThisMonthPOValue = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(AmountExGST),0) FROM PurchaseOrders WHERE PODate >= @S", p(thisMonth));

            m.LastMonthPOs = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM PurchaseOrders WHERE PODate >= @S AND PODate < @E",
                p2(lastMonth, thisMonth));

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
                "SELECT ISNULL(SUM(Amount),0) FROM Invoices WHERE InvoiceDate >= @S", p(ytdStart));

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
            m.MonthlyQuotesThisYear  = await GetMonthlyTotals("Quotes", "QuoteDate",
                "CASE WHEN QuoteStatusId IN (4,10) THEN NettPriceTotal ELSE 0 END", now.Year);
            m.MonthlyQuotesLastYear  = await GetMonthlyTotals("Quotes", "QuoteDate",
                "CASE WHEN QuoteStatusId IN (4,10) THEN NettPriceTotal ELSE 0 END", now.Year - 1);
            m.MonthlyInvoicesThisYear = await GetMonthlyTotals("Invoices", "InvoiceDate", "Amount", now.Year);
            m.MonthlyPOsThisYear      = await GetMonthlyTotals("PurchaseOrders", "PODate", "AmountExGST", now.Year);

            // ── Activity feed ────────────────────────────────────────────────
            m.RecentActivity = await _activity.GetRecentAsync(30);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard metrics");
        }
        return m;
    }

    private static Dictionary<string, object?> p(DateTime start) =>
        new() { ["S"] = start };

    private static Dictionary<string, object?> p2(DateTime start, DateTime end) =>
        new() { ["S"] = start, ["E"] = end };

    private async Task<decimal[]> GetMonthlyTotals(string table, string dateCol, string valueExpr, int year)
    {
        var sql = $@"
            SELECT MONTH({dateCol}) AS Mnth, ISNULL(SUM({valueExpr}), 0) AS Total
            FROM {table}
            WHERE YEAR({dateCol}) = @Year
            GROUP BY MONTH({dateCol})";
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
                ChartPeriod.ThisYear       => await GetCalendarYearChartAsync(now.Year),
                ChartPeriod.FyToDate       => await GetFyToDateChartAsync(now),
                ChartPeriod.LastYear       => await GetCalendarYearChartAsync(now.Year - 1),
                ChartPeriod.SinceInception => await GetSinceInceptionChartAsync(),
                _                          => await GetCalendarYearChartAsync(now.Year),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading chart data for {Period}", period);
            return new(new decimal[12], new decimal[12], new decimal[12],
                new[]{"Jan","Feb","Mar","Apr","May","Jun","Jul","Aug","Sep","Oct","Nov","Dec"});
        }
    }

    private async Task<DashboardChartData> GetCalendarYearChartAsync(int year)
    {
        var quotes   = await GetMonthlyTotals("Quotes",         "QuoteDate",
            "CASE WHEN QuoteStatusId IN (4,10) THEN NettPriceTotal ELSE 0 END", year);
        var invoices = await GetMonthlyTotals("Invoices",        "InvoiceDate", "Amount",      year);
        var pos      = await GetMonthlyTotals("PurchaseOrders",  "PODate",      "AmountExGST", year);
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
            "CASE WHEN QuoteStatusId IN (4,10) THEN NettPriceTotal ELSE 0 END", fyStart, fyEnd);
        var iMap  = await GetMonthlyTotalsInRange("Invoices",       "InvoiceDate", "Amount",      fyStart, fyEnd);
        var pMap  = await GetMonthlyTotalsInRange("PurchaseOrders", "PODate",      "AmountExGST", fyStart, fyEnd);

        return new(
            months.Select(m => qMap.GetValueOrDefault(m)).ToArray(),
            months.Select(m => iMap.GetValueOrDefault(m)).ToArray(),
            months.Select(m => pMap.GetValueOrDefault(m)).ToArray(),
            months.Select(Label).ToArray());
    }

    private async Task<DashboardChartData> GetSinceInceptionChartAsync()
    {
        var quotes   = await GetAnnualTotals("Quotes",        "QuoteDate",
            "CASE WHEN QuoteStatusId IN (4,10) THEN NettPriceTotal ELSE 0 END");
        var invoices = await GetAnnualTotals("Invoices",       "InvoiceDate", "Amount");
        var pos      = await GetAnnualTotals("PurchaseOrders", "PODate",      "AmountExGST");

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
        string table, string dateCol, string valueExpr, DateTime start, DateTime end)
    {
        var sql = $@"
            SELECT YEAR({dateCol}) AS Yr, MONTH({dateCol}) AS Mo, ISNULL(SUM({valueExpr}), 0) AS Total
            FROM {table}
            WHERE {dateCol} >= @S AND {dateCol} < @E
            GROUP BY YEAR({dateCol}), MONTH({dateCol})";
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
        string table, string dateCol, string valueExpr)
    {
        var sql = $@"
            SELECT YEAR({dateCol}) AS Yr, ISNULL(SUM({valueExpr}), 0) AS Total
            FROM {table}
            WHERE {dateCol} IS NOT NULL
            GROUP BY YEAR({dateCol})";
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
