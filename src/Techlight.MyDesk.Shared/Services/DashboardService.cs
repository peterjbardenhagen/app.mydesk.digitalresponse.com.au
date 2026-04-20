using Microsoft.Extensions.Logging;
using Techlight.MyDesk.Shared.Models;

namespace Techlight.MyDesk.Shared.Services;

public class DashboardService
{
    private readonly DatabaseService _db;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(DatabaseService db, ILogger<DashboardService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<DashboardMetrics> GetMetricsAsync(string? originatorCode = null)
    {
        var metrics = new DashboardMetrics();
        try
        {
            var now = DateTime.Now;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var ytdStart = new DateTime(now.Year, 1, 1);
            var lastYearYtdStart = new DateTime(now.Year - 1, 1, 1);
            var lastYearYtdEnd = new DateTime(now.Year - 1, now.Month, 1).AddMonths(1).AddDays(-1);

            // This month
            metrics.ThisMonthQuotes = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Quotes WHERE QuoteDate >= @Start", new() { ["Start"] = thisMonthStart });

            metrics.ThisMonthQuotesWon = await _db.ScalarAsync<int>(
                @"SELECT COUNT(*) FROM Quotes q 
                  LEFT JOIN QuoteStatus qs ON q.QuoteStatusId = qs.QuoteStatusId
                  WHERE q.QuoteDate >= @Start AND qs.QuoteStatus LIKE '%Won%'",
                new() { ["Start"] = thisMonthStart });

            metrics.ThisMonthQuotesValue = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(NettPriceTotal), 0) FROM Quotes WHERE QuoteDate >= @Start",
                new() { ["Start"] = thisMonthStart });

            metrics.ThisMonthInvoices = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Invoices WHERE InvoiceDate >= @Start", new() { ["Start"] = thisMonthStart });

            metrics.ThisMonthInvoiceValue = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(Amount), 0) FROM Invoices WHERE InvoiceDate >= @Start",
                new() { ["Start"] = thisMonthStart });

            // Last month
            metrics.LastMonthQuotesWon = await _db.ScalarAsync<int>(
                @"SELECT COUNT(*) FROM Quotes q
                  LEFT JOIN QuoteStatus qs ON q.QuoteStatusId = qs.QuoteStatusId
                  WHERE q.QuoteDate >= @Start AND q.QuoteDate < @End AND qs.QuoteStatus LIKE '%Won%'",
                new() { ["Start"] = lastMonthStart, ["End"] = thisMonthStart });

            metrics.LastMonthInvoices = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Invoices WHERE InvoiceDate >= @Start AND InvoiceDate < @End",
                new() { ["Start"] = lastMonthStart, ["End"] = thisMonthStart });

            // YTD
            metrics.YtdQuotesValue = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(NettPriceTotal), 0) FROM Quotes WHERE QuoteDate >= @Start",
                new() { ["Start"] = ytdStart });

            metrics.YtdQuotesWon = await _db.ScalarAsync<int>(
                @"SELECT COUNT(*) FROM Quotes q
                  LEFT JOIN QuoteStatus qs ON q.QuoteStatusId = qs.QuoteStatusId
                  WHERE q.QuoteDate >= @Start AND qs.QuoteStatus LIKE '%Won%'",
                new() { ["Start"] = ytdStart });

            metrics.YtdInvoices = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Invoices WHERE InvoiceDate >= @Start",
                new() { ["Start"] = ytdStart });

            metrics.YtdInvoiceValue = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(Amount), 0) FROM Invoices WHERE InvoiceDate >= @Start",
                new() { ["Start"] = ytdStart });

            metrics.LastYearYtdQuotesValue = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(NettPriceTotal), 0) FROM Quotes WHERE QuoteDate >= @Start AND QuoteDate <= @End",
                new() { ["Start"] = lastYearYtdStart, ["End"] = lastYearYtdEnd });

            // Alerts
            var thirtyDaysAgo = now.AddDays(-30);
            metrics.PendingQuotesOver30Days = await _db.ScalarAsync<int>(
                @"SELECT COUNT(*) FROM Quotes q
                  LEFT JOIN QuoteStatus qs ON q.QuoteStatusId = qs.QuoteStatusId
                  WHERE q.QuoteDate < @Thirty AND qs.QuoteStatus LIKE '%Pending%'",
                new() { ["Thirty"] = thirtyDaysAgo });

            metrics.InvoicesOverdue = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Invoices WHERE DueDate < @Now AND ISNULL(Amount, 0) > 0",
                new() { ["Now"] = now });

            // Monthly breakdowns
            metrics.MonthlyQuotesThisYear = await GetMonthlyTotals("Quotes", "QuoteDate", "NettPriceTotal", now.Year);
            metrics.MonthlyQuotesLastYear = await GetMonthlyTotals("Quotes", "QuoteDate", "NettPriceTotal", now.Year - 1);
            metrics.MonthlyInvoicesThisYear = await GetMonthlyTotals("Invoices", "InvoiceDate", "Amount", now.Year);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading dashboard metrics");
        }
        return metrics;
    }

    private async Task<decimal[]> GetMonthlyTotals(string table, string dateCol, string valueCol, int year)
    {
        var sql = $@"
            SELECT MONTH({dateCol}) AS Mnth, ISNULL(SUM({valueCol}), 0) AS Total
            FROM {table}
            WHERE YEAR({dateCol}) = @Year
            GROUP BY MONTH({dateCol})";
        var dt = await _db.QueryAsync(sql, new() { ["Year"] = year });
        var result = new decimal[12];
        foreach (System.Data.DataRow r in dt.Rows)
        {
            var m = Convert.ToInt32(r["Mnth"]);
            if (m is >= 1 and <= 12) result[m - 1] = Convert.ToDecimal(r["Total"]);
        }
        return result;
    }
}
