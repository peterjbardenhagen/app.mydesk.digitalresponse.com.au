using Microsoft.Extensions.Logging;
using Techlight.MyDesk.Shared.Models;

namespace Techlight.MyDesk.Shared.Services;

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
}
