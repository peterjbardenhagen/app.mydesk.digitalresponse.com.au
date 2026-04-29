using System.Data;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class ReconciliationService
{
    private readonly DatabaseService _db;
    private readonly ILogger<ReconciliationService> _logger;

    public ReconciliationService(DatabaseService db, ILogger<ReconciliationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<AgedReceivable>> GetAgedReceivablesAsync()
    {
        var sql = @"
            SELECT c.Company AS CustomerName,
                   c.CustomerCode,
                   SUM(CASE WHEN DATEDIFF(day, i.InvoiceDate, GETDATE()) <= 30 THEN (ISNULL(i.NettPriceTotal, 0) + ISNULL(i.GSTTotal, 0)) ELSE 0 END) AS CurrentAmount,
                   SUM(CASE WHEN DATEDIFF(day, i.InvoiceDate, GETDATE()) BETWEEN 31 AND 60 THEN (ISNULL(i.NettPriceTotal, 0) + ISNULL(i.GSTTotal, 0)) ELSE 0 END) AS Days30Amount,
                   SUM(CASE WHEN DATEDIFF(day, i.InvoiceDate, GETDATE()) BETWEEN 61 AND 90 THEN (ISNULL(i.NettPriceTotal, 0) + ISNULL(i.GSTTotal, 0)) ELSE 0 END) AS Days60Amount,
                   SUM(CASE WHEN DATEDIFF(day, i.InvoiceDate, GETDATE()) > 90 THEN (ISNULL(i.NettPriceTotal, 0) + ISNULL(i.GSTTotal, 0)) ELSE 0 END) AS Days90PlusAmount,
                   SUM(ISNULL(i.NettPriceTotal, 0) + ISNULL(i.GSTTotal, 0)) AS TotalOutstanding
            FROM Invoices i
            LEFT JOIN Companies c ON c.CompanyId = i.CompanyId
            WHERE i.InvoiceStatusId IN (2, 6)
              AND (ISNULL(i.NettPriceTotal, 0) + ISNULL(i.GSTTotal, 0)) > 0
            GROUP BY c.Company, c.CustomerCode
            ORDER BY TotalOutstanding DESC";

        var dt = await _db.QueryAsync(sql);
        return dt.Map(MapAgedReceivable);
    }

    public async Task<AgedReceivables> GetAggAgedReceivablesAsync()
    {
        var receivables = await GetAgedReceivablesAsync();
        return new AgedReceivables
        {
            Current = receivables.Sum(r => r.CurrentAmount),
            Days31_60 = receivables.Sum(r => r.Days30Amount),
            Days61_90 = receivables.Sum(r => r.Days60Amount),
            Over90 = receivables.Sum(r => r.Days90PlusAmount),
            Total = receivables.Sum(r => r.TotalOutstanding)
        };
    }

    public async Task<List<AgedPayable>> GetAgedPayablesAsync()
    {
        var sql = @"
            SELECT c.Company AS SupplierName,
                   c.SupplierCode,
                   SUM(CASE WHEN DATEDIFF(day, po.PODate, GETDATE()) <= 30 THEN (ISNULL(po.PriceExTotal, 0) + ISNULL(po.GstTotal, 0)) ELSE 0 END) AS CurrentAmount,
                   SUM(CASE WHEN DATEDIFF(day, po.PODate, GETDATE()) BETWEEN 31 AND 60 THEN (ISNULL(po.PriceExTotal, 0) + ISNULL(po.GstTotal, 0)) ELSE 0 END) AS Days30Amount,
                   SUM(CASE WHEN DATEDIFF(day, po.PODate, GETDATE()) BETWEEN 61 AND 90 THEN (ISNULL(po.PriceExTotal, 0) + ISNULL(po.GstTotal, 0)) ELSE 0 END) AS Days60Amount,
                   SUM(CASE WHEN DATEDIFF(day, po.PODate, GETDATE()) > 90 THEN (ISNULL(po.PriceExTotal, 0) + ISNULL(po.GstTotal, 0)) ELSE 0 END) AS Days90PlusAmount,
                   SUM(ISNULL(po.PriceExTotal, 0) + ISNULL(po.GstTotal, 0)) AS TotalOutstanding
            FROM PurchaseOrders po
            LEFT JOIN Companies c ON c.CompanyId = po.SupplierId
            WHERE po.POStatusId IN (2, 3)
              AND (ISNULL(po.PriceExTotal, 0) + ISNULL(po.GstTotal, 0)) > 0
            GROUP BY c.Company, c.SupplierCode
            ORDER BY TotalOutstanding DESC";

        var dt = await _db.QueryAsync(sql);
        return dt.Map(MapAgedPayable);
    }

    private static AgedReceivable MapAgedReceivable(DataRow r) => new()
    {
        CustomerName = r["CustomerName"]?.ToString() ?? "",
        CustomerCode = r["CustomerCode"]?.ToString() ?? "",
        CurrentAmount = r["CurrentAmount"] != DBNull.Value ? Convert.ToDecimal(r["CurrentAmount"]) : 0,
        Days30Amount = r["Days30Amount"] != DBNull.Value ? Convert.ToDecimal(r["Days30Amount"]) : 0,
        Days60Amount = r["Days60Amount"] != DBNull.Value ? Convert.ToDecimal(r["Days60Amount"]) : 0,
        Days90PlusAmount = r["Days90PlusAmount"] != DBNull.Value ? Convert.ToDecimal(r["Days90PlusAmount"]) : 0,
        TotalOutstanding = r["TotalOutstanding"] != DBNull.Value ? Convert.ToDecimal(r["TotalOutstanding"]) : 0,
    };

    private static AgedPayable MapAgedPayable(DataRow r) => new()
    {
        SupplierName = r["SupplierName"]?.ToString() ?? "",
        SupplierCode = r["SupplierCode"]?.ToString() ?? "",
        CurrentAmount = r["CurrentAmount"] != DBNull.Value ? Convert.ToDecimal(r["CurrentAmount"]) : 0,
        Days30Amount = r["Days30Amount"] != DBNull.Value ? Convert.ToDecimal(r["Days30Amount"]) : 0,
        Days60Amount = r["Days60Amount"] != DBNull.Value ? Convert.ToDecimal(r["Days60Amount"]) : 0,
        Days90PlusAmount = r["Days90PlusAmount"] != DBNull.Value ? Convert.ToDecimal(r["Days90PlusAmount"]) : 0,
        TotalOutstanding = r["TotalOutstanding"] != DBNull.Value ? Convert.ToDecimal(r["TotalOutstanding"]) : 0,
    };

    public async Task<ReconciliationSummary> GetSummaryAsync()
    {
        var receivables = await GetAgedReceivablesAsync();
        var agedRec = new AgedReceivables
        {
            Current = receivables.Sum(r => r.CurrentAmount),
            Days31_60 = receivables.Sum(r => r.Days30Amount),
            Days61_90 = receivables.Sum(r => r.Days60Amount),
            Over90 = receivables.Sum(r => r.Days90PlusAmount),
            Total = receivables.Sum(r => r.TotalOutstanding)
        };
        
        var summary = new ReconciliationSummary
        {
            TotalReceivables = agedRec.Total,
            CurrentReceivables = agedRec.Current,
            OverdueReceivables = agedRec.Days31_60 + agedRec.Days61_90 + agedRec.Over90,
            ReceivablesCount = receivables.Count,
            OutstandingTotal = agedRec.Total,
            OutstandingCount = receivables.Count,
            LastUpdated = DateTime.Now
        };

        var recentSql = @"
            SELECT COUNT(*) AS InvoiceCount,
                   SUM(ISNULL(NettPriceTotal, 0)) AS MonthlyTotal
            FROM Invoices
            WHERE InvoiceDate >= DATEADD(month, -1, GETDATE())
              AND InvoiceStatusId IN (2, 3)";
        var recentDt = await _db.QueryAsync(recentSql);
        if (recentDt.Rows.Count > 0)
        {
            summary.MonthlySalesCount = recentDt.Rows[0]["InvoiceCount"] != DBNull.Value 
                ? Convert.ToInt32(recentDt.Rows[0]["InvoiceCount"]) : 0;
            summary.MonthlySalesTotal = recentDt.Rows[0]["MonthlyTotal"] != DBNull.Value 
                ? Convert.ToDecimal(recentDt.Rows[0]["MonthlyTotal"]) : 0;
        }

        var unsyncedSql = @"
            SELECT COUNT(*) AS UnsyncedCount,
                   SUM(ISNULL(NettPriceTotal, 0) + ISNULL(GSTTotal, 0)) AS UnsyncedTotal
            FROM Invoices
            WHERE ExportedDate IS NULL
              AND InvoiceStatusId IN (2, 3)";
        var unsyncedDt = await _db.QueryAsync(unsyncedSql);
        if (unsyncedDt.Rows.Count > 0)
        {
            summary.UnsyncedCount = unsyncedDt.Rows[0]["UnsyncedCount"] != DBNull.Value 
                ? Convert.ToInt32(unsyncedDt.Rows[0]["UnsyncedCount"]) : 0;
            summary.UnsyncedTotal = unsyncedDt.Rows[0]["UnsyncedTotal"] != DBNull.Value 
                ? Convert.ToDecimal(unsyncedDt.Rows[0]["UnsyncedTotal"]) : 0;
        }

        var gstSql = @"
            SELECT SUM(ISNULL(GSTTotal, 0)) AS QuarterlyGst
            FROM Invoices
            WHERE InvoiceDate >= DATEADD(quarter, -1, GETDATE())
              AND InvoiceStatusId IN (2, 3)";
        var gstDt = await _db.QueryAsync(gstSql);
        if (gstDt.Rows.Count > 0)
        {
            summary.QuarterlyGstTotal = gstDt.Rows[0]["QuarterlyGst"] != DBNull.Value 
                ? Convert.ToDecimal(gstDt.Rows[0]["QuarterlyGst"]) : 0;
        }

        return summary;
    }

    public async Task<List<DataQualityIssue>> RunDataQualityChecksAsync()
    {
        var issues = new List<DataQualityIssue>();

        var sql = @"
            SELECT InvoiceId, InvoiceNum, CompanyId, (ISNULL(NettPriceTotal, 0) + ISNULL(GSTTotal, 0)) AS Total
            FROM Invoices
            WHERE (CompanyId IS NULL OR CompanyId = 0)
              AND InvoiceStatusId IN (2, 3, 4)
              AND (ISNULL(NettPriceTotal, 0) + ISNULL(GSTTotal, 0)) > 0";
        var dt = await _db.QueryAsync(sql);
        foreach (DataRow r in dt.Rows)
        {
            issues.Add(new DataQualityIssue
            {
                Severity = "High",
                Category = "Missing Data",
                EntityType = "Invoice",
                EntityLabel = r["InvoiceNum"]?.ToString() ?? "",
                Description = "Invoice has no customer assigned"
            });
        }

        var sql2 = @"
            SELECT InvoiceId, InvoiceNum
            FROM Invoices
            WHERE ISNULL(NettPriceTotal, 0) <= 0 AND InvoiceStatusId IN (2, 3)";
        var dt2 = await _db.QueryAsync(sql2);
        foreach (DataRow r in dt2.Rows)
        {
            issues.Add(new DataQualityIssue
            {
                Severity = "Medium",
                Category = "Invalid Data",
                EntityType = "Invoice",
                EntityLabel = r["InvoiceNum"]?.ToString() ?? "",
                Description = "Invoice has zero or negative amount"
            });
        }

        var sql3 = @"
            SELECT c.Company, c.CompanyId, COUNT(*) AS InvoiceCount
            FROM Invoices i
            JOIN Companies c ON c.CompanyId = i.CompanyId
            WHERE i.InvoiceStatusId = 2
              AND i.InvoiceDate < DATEADD(day, -90, GETDATE())
            GROUP BY c.Company, c.CompanyId
            HAVING SUM(ISNULL(i.NettPriceTotal, 0) + ISNULL(i.GSTTotal, 0)) > 0";
        var dt3 = await _db.QueryAsync(sql3);
        foreach (DataRow r in dt3.Rows)
        {
            issues.Add(new DataQualityIssue
            {
                Severity = "Low",
                Category = "Overdue",
                EntityType = "Customer",
                EntityLabel = r["Company"]?.ToString() ?? "",
                Description = $"Customer has overdue invoices over 90 days"
            });
        }

        return issues;
    }

    public async Task<List<UnsyncedInvoice>> GetUnsyncedInvoicesAsync(DateTime? fromDate, DateTime? toDate)
    {
        var sql = @"
            SELECT i.InvoiceId, i.InvoiceNum, i.InvoiceDate, c.Company AS CustomerName,
                   i.TotalExGst, i.GstTotal, i.TotalIncGST,
                   CASE WHEN i.MyobExportDate IS NOT NULL THEN 'Synced' ELSE 'Pending' END AS MyobStatus
            FROM Invoices i
            LEFT JOIN Companies c ON c.CompanyId = i.CompanyId
            WHERE i.MyobExportDate IS NULL
              AND i.StatusName IN ('Issued', 'Approved')";
        
        if (fromDate.HasValue)
            sql += $" AND i.InvoiceDate >= '{fromDate.Value:yyyyMMdd}'";
        if (toDate.HasValue)
            sql += $" AND i.InvoiceDate <= '{toDate.Value:yyyyMMdd}'";
        
        sql += " ORDER BY i.InvoiceDate DESC";

        var dt = await _db.QueryAsync(sql);
        var result = new List<UnsyncedInvoice>();
        foreach (DataRow r in dt.Rows)
        {
            result.Add(new UnsyncedInvoice
            {
                InvoiceId = Convert.ToInt32(r["InvoiceId"]),
                InvoiceNum = r["InvoiceNum"]?.ToString() ?? "",
                InvoiceDate = r["InvoiceDate"] != DBNull.Value ? Convert.ToDateTime(r["InvoiceDate"]) : DateTime.MinValue,
                CustomerName = r["CustomerName"]?.ToString() ?? "",
                NettTotal = r["TotalExGst"] != DBNull.Value ? Convert.ToDecimal(r["TotalExGst"]) : 0,
                GstTotal = r["GstTotal"] != DBNull.Value ? Convert.ToDecimal(r["GstTotal"]) : 0,
                TotalIncGST = r["TotalIncGST"] != DBNull.Value ? Convert.ToDecimal(r["TotalIncGST"]) : 0,
                MyobStatus = r["MyobStatus"]?.ToString() ?? ""
            });
        }
        return result;
    }

    public async Task MarkInvoicesExportedAsync(List<int> invoiceIds, string userCode)
    {
        if (invoiceIds == null || invoiceIds.Count == 0) return;
        
        var ids = string.Join(",", invoiceIds);
        var sql = $@"
            UPDATE Invoices 
            SET MyobExportDate = GETDATE(),
                MyobExportUser = '{userCode}'
            WHERE InvoiceId IN ({ids})";
        
        await _db.ExecuteAsync(sql);
        _logger.LogInformation("Marked {Count} invoices as exported by {User}", invoiceIds.Count, userCode);
    }
}

public class AgedReceivable
{
    public string CustomerName { get; set; } = "";
    public string CustomerCode { get; set; } = "";
    public decimal CurrentAmount { get; set; }
    public decimal Days30Amount { get; set; }
    public decimal Days60Amount { get; set; }
    public decimal Days90PlusAmount { get; set; }
    public decimal TotalOutstanding { get; set; }
}

public class AgedPayable
{
    public string SupplierName { get; set; } = "";
    public string SupplierCode { get; set; } = "";
    public decimal CurrentAmount { get; set; }
    public decimal Days30Amount { get; set; }
    public decimal Days60Amount { get; set; }
    public decimal Days90PlusAmount { get; set; }
    public decimal TotalOutstanding { get; set; }
}

public class ReconciliationSummary
{
    public decimal TotalReceivables { get; set; }
    public decimal TotalPayables { get; set; }
    public decimal NetPosition { get; set; }
    public decimal CurrentReceivables { get; set; }
    public decimal OverdueReceivables { get; set; }
    public decimal CurrentPayables { get; set; }
    public decimal OverduePayables { get; set; }
    public int ReceivablesCount { get; set; }
    public int PayablesCount { get; set; }
    public DateTime LastUpdated { get; set; }
    public int UnsyncedCount { get; set; }
    public decimal UnsyncedTotal { get; set; }
    public decimal OutstandingTotal { get; set; }
    public int OutstandingCount { get; set; }
    public decimal MonthlySalesTotal { get; set; }
    public int MonthlySalesCount { get; set; }
    public decimal QuarterlyGstTotal { get; set; }
}

public class AgedReceivables
{
    public decimal Current { get; set; }
    public decimal Days31_60 { get; set; }
    public decimal Days61_90 { get; set; }
    public decimal Over90 { get; set; }
    public decimal Total { get; set; }
}

public class DataQualityIssue
{
    public string Issue { get; set; } = "";
    public string Description { get; set; } = "";
    public string Severity { get; set; } = "";
    public string Category { get; set; } = "";
    public string EntityType { get; set; } = "";
    public string EntityLabel { get; set; } = "";
}

public class UnsyncedInvoice
{
    public int InvoiceId { get; set; }
    public string InvoiceNum { get; set; } = "";
    public DateTime InvoiceDate { get; set; }
    public string CustomerName { get; set; } = "";
    public decimal NettTotal { get; set; }
    public decimal GstTotal { get; set; }
    public decimal TotalIncGST { get; set; }
    public string MyobStatus { get; set; } = "";
    public bool Selected { get; set; }
}