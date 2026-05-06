using System.Data;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

/// <summary>
/// Aggregates Quote/Invoice data for the Sales Reports dashboard at /reports/sales.
/// Kept as a separate service so the existing ReportService remains untouched.
/// All queries read live data via DatabaseService and aggregate in memory.
/// </summary>
public class SalesReportsService
{
    private readonly DatabaseService _db;
    private readonly ILogger<SalesReportsService> _logger;

    public SalesReportsService(DatabaseService db, ILogger<SalesReportsService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<MonthlySalesDto[]> GetSalesByMonthAsync(int monthsBack = 12)
    {
        var end       = DateTime.Today;
        var start     = new DateTime(end.AddMonths(-(monthsBack - 1)).Year,
                                     end.AddMonths(-(monthsBack - 1)).Month, 1);

        var sqlQuotes = @"
            SELECT YEAR(QuoteDate) AS Y, MONTH(QuoteDate) AS M,
                   ISNULL(SUM(NettPriceTotal),0) AS Total
            FROM Quotes
            WHERE QuoteDate >= @S AND QuoteStatusId IN (4, 10)
            GROUP BY YEAR(QuoteDate), MONTH(QuoteDate)";

        var sqlInv = @"
            SELECT YEAR(InvoiceDate) AS Y, MONTH(InvoiceDate) AS M,
                   ISNULL(SUM(NettPriceTotal),0) AS Total
            FROM Invoices
            WHERE InvoiceDate >= @S
            GROUP BY YEAR(InvoiceDate), MONTH(InvoiceDate)";

        var qDt = await SafeQueryAsync(sqlQuotes, new() { ["S"] = start });
        var iDt = await SafeQueryAsync(sqlInv,    new() { ["S"] = start });

        var buckets = new MonthlySalesDto[monthsBack];
        for (int i = 0; i < monthsBack; i++)
        {
            var d = start.AddMonths(i);
            buckets[i] = new MonthlySalesDto { Year = d.Year, Month = d.Month };
        }

        foreach (DataRow r in qDt.Rows)
        {
            var y = Convert.ToInt32(r["Y"]);
            var m = Convert.ToInt32(r["M"]);
            var b = buckets.FirstOrDefault(x => x.Year == y && x.Month == m);
            if (b != null) b.QuoteTotal = Convert.ToDecimal(r["Total"]);
        }
        foreach (DataRow r in iDt.Rows)
        {
            var y = Convert.ToInt32(r["Y"]);
            var m = Convert.ToInt32(r["M"]);
            var b = buckets.FirstOrDefault(x => x.Year == y && x.Month == m);
            if (b != null) b.InvoiceTotal = Convert.ToDecimal(r["Total"]);
        }
        return buckets;
    }

    public async Task<RepSalesDto[]> GetSalesByRepAsync(DateTime from, DateTime to)
    {
        var sql = @"
            SELECT q.Code AS OwnerUserCode, ISNULL(u.Name,'') AS OwnerName,
                   ISNULL(SUM(q.NettPriceTotal),0) AS Total,
                   COUNT(*) AS Cnt
            FROM Quotes q
            LEFT JOIN Users u ON u.Code = q.Code
            WHERE q.QuoteDate BETWEEN @F AND @T
              AND q.QuoteStatusId IN (4, 10)
            GROUP BY q.Code, u.Name
            ORDER BY Total DESC";
        var dt = await SafeQueryAsync(sql, new() { ["F"] = from, ["T"] = to });
        return dt.Map(r => new RepSalesDto
        {
            OwnerUserCode = r["OwnerUserCode"]?.ToString() ?? "",
            OwnerName     = r["OwnerName"]?.ToString() ?? "",
            Total         = Convert.ToDecimal(r["Total"]),
            Count         = Convert.ToInt32(r["Cnt"]),
        }).ToArray();
    }

    public async Task<DivisionSalesDto[]> GetSalesByDivisionAsync(DateTime from, DateTime to)
    {
        var sql = @"
            SELECT q.DivisionId, ISNULL(d.Division,'(unset)') AS DivisionName,
                   ISNULL(SUM(q.NettPriceTotal),0) AS Total,
                   COUNT(*) AS Cnt
            FROM Quotes q
            LEFT JOIN Divisions d ON d.DivisionId = q.DivisionId
            WHERE q.QuoteDate BETWEEN @F AND @T
              AND q.QuoteStatusId IN (4, 10)
            GROUP BY q.DivisionId, d.Division
            ORDER BY Total DESC";
        var dt = await SafeQueryAsync(sql, new() { ["F"] = from, ["T"] = to });
        return dt.Map(r => new DivisionSalesDto
        {
            DivisionId   = r["DivisionId"] == DBNull.Value ? 0 : Convert.ToInt32(r["DivisionId"]),
            DivisionName = r["DivisionName"]?.ToString() ?? "(unset)",
            Total        = Convert.ToDecimal(r["Total"]),
            Count        = Convert.ToInt32(r["Cnt"]),
        }).ToArray();
    }

    public async Task<YearOnYearDto> GetYearOnYearAsync()
    {
        var now    = DateTime.Today;
        var thisY  = now.Year;
        var prevY  = now.Year - 1;

        var sql = @"
            SELECT YEAR(InvoiceDate) AS Y, MONTH(InvoiceDate) AS M,
                   ISNULL(SUM(NettPriceTotal),0) AS Total
            FROM Invoices
            WHERE InvoiceDate >= @S
            GROUP BY YEAR(InvoiceDate), MONTH(InvoiceDate)";
        var start = new DateTime(prevY, 1, 1);
        var dt = await SafeQueryAsync(sql, new() { ["S"] = start });

        var dto = new YearOnYearDto { CurrentYear = thisY, PreviousYear = prevY };
        foreach (DataRow r in dt.Rows)
        {
            var y = Convert.ToInt32(r["Y"]);
            var m = Convert.ToInt32(r["M"]) - 1;
            if (m < 0 || m > 11) continue;
            var total = Convert.ToDecimal(r["Total"]);
            if (y == thisY)      dto.CurrentYearMonthly[m]  = total;
            else if (y == prevY) dto.PreviousYearMonthly[m] = total;
        }
        return dto;
    }

    public async Task<QuoteStatusSummaryDto> GetPendingVsWonAsync(DateTime from, DateTime to)
    {
        var sql = @"
            SELECT YEAR(QuoteDate) AS Y, MONTH(QuoteDate) AS M,
                   SUM(CASE WHEN QuoteStatusId IN (1,2,3,6,7,8)  THEN NettPriceTotal ELSE 0 END) AS Pending,
                   SUM(CASE WHEN QuoteStatusId = 4   THEN NettPriceTotal ELSE 0 END) AS Won
            FROM Quotes
            WHERE QuoteDate BETWEEN @F AND @T
            GROUP BY YEAR(QuoteDate), MONTH(QuoteDate)
            ORDER BY Y, M";
        var dt = await SafeQueryAsync(sql, new() { ["F"] = from, ["T"] = to });

        var totalMonths = ((to.Year - from.Year) * 12) + to.Month - from.Month + 1;
        if (totalMonths < 1) totalMonths = 1;

        var dto = new QuoteStatusSummaryDto
        {
            Labels  = new string[totalMonths],
            Pending = new decimal[totalMonths],
            Won     = new decimal[totalMonths],
        };
        var cursor = new DateTime(from.Year, from.Month, 1);
        for (int i = 0; i < totalMonths; i++)
        {
            dto.Labels[i] = cursor.AddMonths(i).ToString("MMM yy");
        }
        foreach (DataRow r in dt.Rows)
        {
            var y = Convert.ToInt32(r["Y"]);
            var m = Convert.ToInt32(r["M"]);
            var idx = ((y - from.Year) * 12) + m - from.Month;
            if (idx < 0 || idx >= totalMonths) continue;
            dto.Pending[idx] = Convert.ToDecimal(r["Pending"]);
            dto.Won[idx]     = Convert.ToDecimal(r["Won"]);
        }
        return dto;
    }

    private async Task<DataTable> SafeQueryAsync(string sql, Dictionary<string, object?>? p)
    {
        try
        {
            return await _db.QueryAsync(sql, p ?? new());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SalesReportsService query failed; returning empty table");
            return new DataTable();
        }
    }
}
