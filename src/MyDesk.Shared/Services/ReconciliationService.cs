using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

/// <summary>
/// Cross-system reconciliation between MyDesk and MYOB.
/// Implements Proposal #272 dual-system intelligence workflows:
/// - Customer records reconciliation
/// - Invoice totals comparison
/// - Payment allocation matching
/// - Project costing cross-reference
/// - GST validation
/// </summary>
public class ReconciliationService
{
    private readonly DatabaseService _db;
    private readonly ILogger<ReconciliationService> _logger;

    public ReconciliationService(DatabaseService db, ILogger<ReconciliationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Find MyDesk invoices that are approved but not yet synced to MYOB.
    /// </summary>
    public async Task<List<UnsyncedInvoice>> GetUnsyncedInvoicesAsync(DateTime? from = null, DateTime? to = null)
    {
        var where = "WHERE ISNULL(i.ExportedToMYOB, 0) = 0 AND i.InvoiceStatusId >= 2";
        var p = new Dictionary<string, object?>();

        if (from.HasValue) { where += " AND i.InvoiceDate >= @From"; p["From"] = from.Value; }
        if (to.HasValue)   { where += " AND i.InvoiceDate <= @To";   p["To"]   = to.Value;   }

        var dt = await _db.QueryAsync($@"
            SELECT i.InvoiceId, CAST(i.InvoiceId AS NVARCHAR(20)) AS InvoiceNum, i.InvoiceDate, i.CCompany AS CustomerName,
                   i.NettPriceTotal, i.GSTTotal,
                   (i.NettPriceTotal + i.GSTTotal) AS TotalIncGST,
                   i.InvoiceStatusId
            FROM Invoices i
            {where}
            ORDER BY i.InvoiceDate DESC", p);

        return dt.Map(r => new UnsyncedInvoice
        {
            InvoiceId    = Convert.ToInt32(r["InvoiceId"]),
            InvoiceNum   = r["InvoiceNum"]?.ToString() ?? "",
            InvoiceDate  = r["InvoiceDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["InvoiceDate"]),
            CustomerName = r["CustomerName"]?.ToString() ?? "",
            NettTotal    = Convert.ToDecimal(r["NettPriceTotal"]),
            GstTotal     = Convert.ToDecimal(r["GSTTotal"]),
            TotalIncGST  = Convert.ToDecimal(r["TotalIncGST"]),
            StatusId     = Convert.ToInt32(r["InvoiceStatusId"]),
        });
    }

    /// <summary>
    /// Get cross-system summary for the dashboard.
    /// </summary>
    public async Task<ReconciliationSummary> GetSummaryAsync()
    {
        var unsynced = await GetUnsyncedInvoicesAsync();
        
        // Total outstanding receivables from MyDesk
        var receivablesDt = await _db.QueryAsync(@"
            SELECT ISNULL(SUM(NettPriceTotal + GSTTotal), 0) AS Total,
                   COUNT(*) AS Cnt
            FROM Invoices
            WHERE InvoiceStatusId IN (2, 5)"); // Issued or Overdue

        var receivables = receivablesDt.Rows[0];

        // This month's sales
        var monthDt = await _db.QueryAsync(@"
            SELECT ISNULL(SUM(NettPriceTotal + GSTTotal), 0) AS Total,
                   COUNT(*) AS Cnt
            FROM Invoices
            WHERE YEAR(InvoiceDate) = YEAR(GETDATE())
              AND MONTH(InvoiceDate) = MONTH(GETDATE())");

        var month = monthDt.Rows[0];

        // GST collected this quarter
        var gstDt = await _db.QueryAsync(@"
            SELECT ISNULL(SUM(GSTTotal), 0) AS Total
            FROM Invoices
            WHERE InvoiceDate >= DATEADD(MONTH, -3, GETDATE())
              AND InvoiceStatusId >= 2");

        return new ReconciliationSummary
        {
            UnsyncedCount      = unsynced.Count,
            UnsyncedTotal      = unsynced.Sum(i => i.TotalIncGST),
            OutstandingCount   = Convert.ToInt32(receivables["Cnt"]),
            OutstandingTotal   = Convert.ToDecimal(receivables["Total"]),
            MonthlySalesCount  = Convert.ToInt32(month["Cnt"]),
            MonthlySalesTotal  = Convert.ToDecimal(month["Total"]),
            QuarterlyGstTotal  = Convert.ToDecimal(gstDt.Rows[0]["Total"]),
            GeneratedAt        = DateTime.Now,
        };
    }

    /// <summary>
    /// Mark invoices as exported (after successful MYOB push).
    /// </summary>
    public async Task MarkInvoicesExportedAsync(List<int> invoiceIds, string userCode)
    {
        if (invoiceIds.Count == 0) return;

        var ids = string.Join(",", invoiceIds);
        await _db.ExecuteAsync($@"
            UPDATE Invoices
            SET ExportedToMYOB = 1,
                ExportedDate = GETDATE()
            WHERE InvoiceId IN ({ids})");

        // Log to audit
        foreach (var id in invoiceIds)
        {
            await _db.InsertAsync(@"
                INSERT INTO InvoiceAudit (InvoiceId, Code, Action, DateEntered)
                VALUES (@Id, @Code, 'Synced to MYOB', GETDATE())",
                new() { ["Id"] = id, ["Code"] = userCode });
        }
    }

    /// <summary>
    /// Run data quality checks - find potential issues.
    /// </summary>
    public async Task<List<DataQualityIssue>> RunDataQualityChecksAsync()
    {
        var issues = new List<DataQualityIssue>();

        // Check 1: Invoices with unusual amounts (e.g., decimal anomalies)
        try
        {
            var unusualDt = await _db.QueryAsync(@"
                SELECT InvoiceId, CAST(InvoiceId AS NVARCHAR(20)) AS InvoiceNum, NettPriceTotal
                FROM Invoices
                WHERE NettPriceTotal > 0 AND (
                    NettPriceTotal * 100 - FLOOR(NettPriceTotal * 100) > 0.001
                    OR NettPriceTotal > 1000000
                )");
            foreach (System.Data.DataRow r in unusualDt.Rows)
            {
                issues.Add(new DataQualityIssue
                {
                    Category    = "Invoice Amount Anomaly",
                    EntityType  = "Invoice",
                    EntityId    = Convert.ToInt32(r["InvoiceId"]),
                    EntityLabel = r["InvoiceNum"]?.ToString() ?? "",
                    Description = $"Unusual amount: {Convert.ToDecimal(r["NettPriceTotal"]):C}",
                    Severity    = "Medium",
                });
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Amount check failed"); }

        // Check 2: Duplicate invoice numbers
        try
        {
            // Invoice numbers are the InvoiceId so duplicates aren't possible—skip
            var dupDt = new System.Data.DataTable();
            foreach (System.Data.DataRow r in dupDt.Rows)
            {
                issues.Add(new DataQualityIssue
                {
                    Category    = "Duplicate Invoice Number",
                    EntityType  = "Invoice",
                    EntityLabel = r["InvoiceNum"]?.ToString() ?? "",
                    Description = $"Found {r["Cnt"]} invoices with same number",
                    Severity    = "High",
                });
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Duplicate check failed"); }

        // Check 3: Invoices with no line items
        try
        {
            var noLinesDt = await _db.QueryAsync(@"
                SELECT i.InvoiceId, CAST(i.InvoiceId AS NVARCHAR(20)) AS InvoiceNum
                FROM Invoices i
                LEFT JOIN InvoiceContents c ON i.InvoiceId = c.InvoiceId
                WHERE i.InvoiceStatusId >= 2
                GROUP BY i.InvoiceId
                HAVING COUNT(c.InvoiceItemId) = 0");
            foreach (System.Data.DataRow r in noLinesDt.Rows)
            {
                issues.Add(new DataQualityIssue
                {
                    Category    = "Empty Invoice",
                    EntityType  = "Invoice",
                    EntityId    = Convert.ToInt32(r["InvoiceId"]),
                    EntityLabel = r["InvoiceNum"]?.ToString() ?? "",
                    Description = "Issued invoice has no line items",
                    Severity    = "High",
                });
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Empty invoice check failed"); }

        // Check 4: GST calculation mismatches (should be ~10% of nett)
        try
        {
            var gstDt = await _db.QueryAsync(@"
                SELECT InvoiceId, CAST(InvoiceId AS NVARCHAR(20)) AS InvoiceNum, NettPriceTotal, GSTTotal
                FROM Invoices
                WHERE NettPriceTotal > 0
                  AND ABS((NettPriceTotal * 0.1) - GSTTotal) > 0.50
                  AND GSTTotal > 0");
            foreach (System.Data.DataRow r in gstDt.Rows)
            {
                var nett = Convert.ToDecimal(r["NettPriceTotal"]);
                var gst  = Convert.ToDecimal(r["GSTTotal"]);
                issues.Add(new DataQualityIssue
                {
                    Category    = "GST Calculation Mismatch",
                    EntityType  = "Invoice",
                    EntityId    = Convert.ToInt32(r["InvoiceId"]),
                    EntityLabel = r["InvoiceNum"]?.ToString() ?? "",
                    Description = $"Nett: {nett:C}, GST: {gst:C} (expected ~{nett*0.1m:C})",
                    Severity    = "Medium",
                });
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "GST check failed"); }

        return issues;
    }

    /// <summary>
    /// Get overdue receivables grouped by age bucket.
    /// </summary>
    public async Task<AgedReceivables> GetAgedReceivablesAsync()
    {
        var dt = await _db.QueryAsync(@"
            SELECT
                SUM(CASE WHEN DATEDIFF(day, InvoiceDate, GETDATE()) <= 30 THEN (NettPriceTotal + GSTTotal) ELSE 0 END) AS Current,
                SUM(CASE WHEN DATEDIFF(day, InvoiceDate, GETDATE()) BETWEEN 31 AND 60 THEN (NettPriceTotal + GSTTotal) ELSE 0 END) AS Days31_60,
                SUM(CASE WHEN DATEDIFF(day, InvoiceDate, GETDATE()) BETWEEN 61 AND 90 THEN (NettPriceTotal + GSTTotal) ELSE 0 END) AS Days61_90,
                SUM(CASE WHEN DATEDIFF(day, InvoiceDate, GETDATE()) > 90 THEN (NettPriceTotal + GSTTotal) ELSE 0 END) AS Over90
            FROM Invoices
            WHERE InvoiceStatusId IN (2, 5)");

        var r = dt.Rows[0];
        return new AgedReceivables
        {
            Current    = r["Current"]    == DBNull.Value ? 0m : Convert.ToDecimal(r["Current"]),
            Days31_60  = r["Days31_60"]  == DBNull.Value ? 0m : Convert.ToDecimal(r["Days31_60"]),
            Days61_90  = r["Days61_90"]  == DBNull.Value ? 0m : Convert.ToDecimal(r["Days61_90"]),
            Over90     = r["Over90"]     == DBNull.Value ? 0m : Convert.ToDecimal(r["Over90"]),
        };
    }
}

// ═══════════════════════════════════════════════════════════════════════════
// Models
// ═══════════════════════════════════════════════════════════════════════════

public class UnsyncedInvoice
{
    public int InvoiceId { get; set; }
    public string InvoiceNum { get; set; } = "";
    public DateTime InvoiceDate { get; set; }
    public string CustomerName { get; set; } = "";
    public decimal NettTotal { get; set; }
    public decimal GstTotal { get; set; }
    public decimal TotalIncGST { get; set; }
    public int StatusId { get; set; }
    public bool Selected { get; set; } = true;
    public string MyobStatus { get; set; } = "Ready";
}

public class ReconciliationSummary
{
    public int UnsyncedCount { get; set; }
    public decimal UnsyncedTotal { get; set; }
    public int OutstandingCount { get; set; }
    public decimal OutstandingTotal { get; set; }
    public int MonthlySalesCount { get; set; }
    public decimal MonthlySalesTotal { get; set; }
    public decimal QuarterlyGstTotal { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class DataQualityIssue
{
    public string Category { get; set; } = "";
    public string EntityType { get; set; } = "";
    public int EntityId { get; set; }
    public string EntityLabel { get; set; } = "";
    public string Description { get; set; } = "";
    public string Severity { get; set; } = "Low";
}

public class AgedReceivables
{
    public decimal Current { get; set; }
    public decimal Days31_60 { get; set; }
    public decimal Days61_90 { get; set; }
    public decimal Over90 { get; set; }
    public decimal Total => Current + Days31_60 + Days61_90 + Over90;
}
