using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public enum ReportType
{
    Contacts,
    DeliveryNotes,
    Invoices,
    Quotes,
    PurchaseOrders
}

public enum DateFilter
{
    AllTime,
    ThisMonth,
    LastMonth,
    ThisQuarter,
    ThisFY,
    LastFY,
    Custom
}

public class ReportDefinition
{
    public int ReportId { get; set; }
    public string Name { get; set; } = "";
    public ReportType Type { get; set; }
    public DateFilter DateFilter { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ReportService
{
    private readonly DatabaseService _db;
    private readonly ILogger<ReportService> _logger;

    public ReportService(DatabaseService db, ILogger<ReportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EnsureTableAsync()
    {
        var sql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SavedReports')
            BEGIN
                CREATE TABLE SavedReports (
                    ReportId INT IDENTITY(1,1) PRIMARY KEY,
                    Name NVARCHAR(200) NOT NULL,
                    ReportType INT NOT NULL,
                    DateFilter INT NOT NULL,
                    StartDate DATETIME NULL,
                    EndDate DATETIME NULL,
                    CreatedBy NVARCHAR(100) NULL,
                    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                );
            END";
        await _db.ExecuteAsync(sql);
    }

    public async Task<ReportResult> GenerateReportAsync(ReportType type, DateFilter dateFilter, DateTime? startDate = null, DateTime? endDate = null)
    {
        var (start, end) = GetDateRange(dateFilter, startDate, endDate);
        return type switch
        {
            ReportType.Contacts => await GenerateContactsReportAsync(start, end),
            ReportType.DeliveryNotes => await GenerateDeliveryNotesReportAsync(start, end),
            ReportType.Invoices => await GenerateInvoicesReportAsync(start, end),
            ReportType.Quotes => await GenerateQuotesReportAsync(start, end),
            ReportType.PurchaseOrders => await GeneratePurchaseOrdersReportAsync(start, end),
            _ => throw new ArgumentException($"Unknown report type: {type}")
        };
    }

    private (DateTime? Start, DateTime? End) GetDateRange(DateFilter filter, DateTime? customStart, DateTime? customEnd)
    {
        var now = DateTime.Now;
        switch (filter)
        {
            case DateFilter.AllTime:
                return (null, null);
            case DateFilter.ThisMonth:
                return (new DateTime(now.Year, now.Month, 1), new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month)));
            case DateFilter.LastMonth:
                var last = now.AddMonths(-1);
                return (new DateTime(last.Year, last.Month, 1), new DateTime(last.Year, last.Month, DateTime.DaysInMonth(last.Year, last.Month)));
            case DateFilter.ThisQuarter:
                var q = (now.Month - 1) / 3;
                var start = new DateTime(now.Year, q * 3 + 1, 1);
                var end = start.AddMonths(3).AddDays(-1);
                return (start, end);
            case DateFilter.ThisFY:
                var fyStart = now.Month >= 7 ? new DateTime(now.Year, 7, 1) : new DateTime(now.Year - 1, 7, 1);
                var fyEnd = fyStart.AddYears(1).AddDays(-1);
                return (fyStart, fyEnd);
            case DateFilter.LastFY:
                var fyStart2 = now.Month >= 7 ? new DateTime(now.Year - 1, 7, 1) : new DateTime(now.Year - 2, 7, 1);
                var fyEnd2 = fyStart2.AddYears(1).AddDays(-1);
                return (fyStart2, fyEnd2);
            case DateFilter.Custom:
                return (customStart, customEnd);
            default:
                return (null, null);
        }
    }

    private async Task<ReportResult> GenerateContactsReportAsync(DateTime? start, DateTime? end)
    {
        var sql = @"
            SELECT c.ContactId, c.FirstName + ' ' + c.Surname AS Name, 
                   co.Company AS Company, c.Email, c.Phone, c.Mobile
            FROM Contacts c
            LEFT JOIN Companies co ON c.CompanyId = co.CompanyId
            WHERE ISNULL(c.Deleted,0) = 0";
        
        if (start.HasValue && end.HasValue)
        {
            sql += " AND c.CreatedDate BETWEEN @Start AND @End";
        }
        sql += " ORDER BY co.Company, c.Surname, c.FirstName";

        var dt = await _db.QueryAsync(sql, new() { ["Start"] = start, ["End"] = end });
        
        return new ReportResult
        {
            Title = "Contacts Report",
            Headers = new() { "ID", "Name", "Company", "Email", "Phone", "Mobile" },
            Rows = dt.Map(r => new List<string>
            {
                r["ContactId"]?.ToString() ?? "",
                r["Name"]?.ToString() ?? "",
                r["Company"]?.ToString() ?? "",
                r["Email"]?.ToString() ?? "",
                r["Phone"]?.ToString() ?? "",
                r["Mobile"]?.ToString() ?? ""
            }).ToList(),
            GeneratedAt = DateTime.Now
        };
    }

    private async Task<ReportResult> GenerateDeliveryNotesReportAsync(DateTime? start, DateTime? end)
    {
        var sql = @"
            SELECT d.DespatchId, d.DespatchNumber, d.DespatchDate,
                   co.Company AS Customer, d.DeliveryAddress,
                   ISNULL(d.NettPriceTotal,0) AS Amount
            FROM Despatch d
            LEFT JOIN Contacts c ON c.ContactId = d.ContactId
            LEFT JOIN Companies co ON c.CompanyId = co.CompanyId
            WHERE ISNULL(d.Deleted,0) = 0";
        
        if (start.HasValue && end.HasValue)
        {
            sql += " AND d.DespatchDate BETWEEN @Start AND @End";
        }
        sql += " ORDER BY d.DespatchDate DESC";

        var dt = await _db.QueryAsync(sql, new() { ["Start"] = start, ["End"] = end });
        
        return new ReportResult
        {
            Title = "Delivery Notes Report",
            Headers = new() { "ID", "Number", "Date", "Customer", "Delivery Address", "Amount" },
            Rows = dt.Map(r => new List<string>
            {
                r["DespatchId"]?.ToString() ?? "",
                r["DespatchNumber"]?.ToString() ?? "",
                r["DespatchDate"] == DBNull.Value ? "" : Convert.ToDateTime(r["DespatchDate"]).ToString("dd/MM/yyyy"),
                r["Customer"]?.ToString() ?? "",
                r["DeliveryAddress"]?.ToString() ?? "",
                r["Amount"] == DBNull.Value ? "$0.00" : Convert.ToDecimal(r["Amount"]).ToString("C")
            }).ToList(),
            TotalAmount = dt.Rows.Count > 0 ? dt.Map(r => Convert.ToDecimal(r["Amount"])).Sum() : 0,
            GeneratedAt = DateTime.Now
        };
    }

    private async Task<ReportResult> GenerateInvoicesReportAsync(DateTime? start, DateTime? end)
    {
        var sql = @"
            SELECT i.InvoiceId, i.InvoiceNumber, i.InvoiceDate,
                   co.Company AS Customer, ISNULL(i.NettPriceTotal,0) AS Amount,
                   ISNULL(s.InvoiceStatus,'') AS Status
            FROM Invoices i
            LEFT JOIN Contacts c ON c.ContactId = i.ContactId
            LEFT JOIN Companies co ON c.CompanyId = co.CompanyId
            LEFT JOIN InvoiceStatus s ON s.InvoiceStatusId = i.InvoiceStatusId
            WHERE ISNULL(i.Deleted,0) = 0";
        
        if (start.HasValue && end.HasValue)
        {
            sql += " AND i.InvoiceDate BETWEEN @Start AND @End";
        }
        sql += " ORDER BY i.InvoiceDate DESC";

        var dt = await _db.QueryAsync(sql, new() { ["Start"] = start, ["End"] = end });
        
        return new ReportResult
        {
            Title = "Invoices Report",
            Headers = new() { "ID", "Number", "Date", "Customer", "Amount", "Status" },
            Rows = dt.Map(r => new List<string>
            {
                r["InvoiceId"]?.ToString() ?? "",
                r["InvoiceNumber"]?.ToString() ?? "",
                r["InvoiceDate"] == DBNull.Value ? "" : Convert.ToDateTime(r["InvoiceDate"]).ToString("dd/MM/yyyy"),
                r["Customer"]?.ToString() ?? "",
                r["Amount"] == DBNull.Value ? "$0.00" : Convert.ToDecimal(r["Amount"]).ToString("C"),
                r["Status"]?.ToString() ?? ""
            }).ToList(),
            TotalAmount = dt.Rows.Count > 0 ? dt.Map(r => Convert.ToDecimal(r["Amount"])).Sum() : 0,
            GeneratedAt = DateTime.Now
        };
    }

    private async Task<ReportResult> GenerateQuotesReportAsync(DateTime? start, DateTime? end)
    {
        var sql = @"
            SELECT q.Qid, q.Reference, q.QuoteDate,
                   co.Company AS Customer, ISNULL(q.NettPriceTotal,0) AS Amount,
                   ISNULL(s.QuoteStatus,'') AS Status
            FROM Quotes q
            LEFT JOIN Contacts c ON c.ContactId = q.ContactId
            LEFT JOIN Companies co ON c.CompanyId = co.CompanyId
            LEFT JOIN QuoteStatus s ON s.QuoteStatusId = q.QuoteStatusId
            WHERE ISNULL(q.Deleted,0) = 0";
        
        if (start.HasValue && end.HasValue)
        {
            sql += " AND q.QuoteDate BETWEEN @Start AND @End";
        }
        sql += " ORDER BY q.QuoteDate DESC";

        var dt = await _db.QueryAsync(sql, new() { ["Start"] = start, ["End"] = end });
        
        return new ReportResult
        {
            Title = "Quotes Report",
            Headers = new() { "ID", "Reference", "Date", "Customer", "Amount", "Status" },
            Rows = dt.Map(r => new List<string>
            {
                r["Qid"]?.ToString() ?? "",
                r["Reference"]?.ToString() ?? "",
                r["QuoteDate"] == DBNull.Value ? "" : Convert.ToDateTime(r["QuoteDate"]).ToString("dd/MM/yyyy"),
                r["Customer"]?.ToString() ?? "",
                r["Amount"] == DBNull.Value ? "$0.00" : Convert.ToDecimal(r["Amount"]).ToString("C"),
                r["Status"]?.ToString() ?? ""
            }).ToList(),
            TotalAmount = dt.Rows.Count > 0 ? dt.Map(r => Convert.ToDecimal(r["Amount"])).Sum() : 0,
            GeneratedAt = DateTime.Now
        };
    }

    private async Task<ReportResult> GeneratePurchaseOrdersReportAsync(DateTime? start, DateTime? end)
    {
        var sql = @"
            SELECT po.PurchaseOrderId, po.PONumber, po.PODate,
                   co.Company AS Supplier, ISNULL(po.PriceExTotal,0) AS Amount,
                   ISNULL(s.POStatus,'') AS Status
            FROM PurchaseOrders po
            LEFT JOIN Contacts c ON c.ContactId = po.ContactId
            LEFT JOIN Companies co ON c.CompanyId = co.CompanyId
            LEFT JOIN POStatus s ON s.POStatusId = po.POStatusId
            WHERE ISNULL(po.Deleted,0) = 0";
        
        if (start.HasValue && end.HasValue)
        {
            sql += " AND po.PODate BETWEEN @Start AND @End";
        }
        sql += " ORDER BY po.PODate DESC";

        var dt = await _db.QueryAsync(sql, new() { ["Start"] = start, ["End"] = end });
        
        return new ReportResult
        {
            Title = "Purchase Orders Report",
            Headers = new() { "ID", "Number", "Date", "Supplier", "Amount", "Status" },
            Rows = dt.Map(r => new List<string>
            {
                r["PurchaseOrderId"]?.ToString() ?? "",
                r["PONumber"]?.ToString() ?? "",
                r["PODate"] == DBNull.Value ? "" : Convert.ToDateTime(r["PODate"]).ToString("dd/MM/yyyy"),
                r["Supplier"]?.ToString() ?? "",
                r["Amount"] == DBNull.Value ? "$0.00" : Convert.ToDecimal(r["Amount"]).ToString("C"),
                r["Status"]?.ToString() ?? ""
            }).ToList(),
            TotalAmount = dt.Rows.Count > 0 ? dt.Map(r => Convert.ToDecimal(r["Amount"])).Sum() : 0,
            GeneratedAt = DateTime.Now
        };
    }

    public async Task<List<ReportDefinition>> GetSavedReportsAsync()
    {
        var dt = await _db.QueryAsync("SELECT * FROM SavedReports ORDER BY CreatedAt DESC");
        return dt.Map(r => new ReportDefinition
        {
            ReportId = Convert.ToInt32(r["ReportId"]),
            Name = r["Name"]?.ToString() ?? "",
            Type = (ReportType)Convert.ToInt32(r["ReportType"]),
            DateFilter = (DateFilter)Convert.ToInt32(r["DateFilter"]),
            StartDate = r["StartDate"] != DBNull.Value ? Convert.ToDateTime(r["StartDate"]) : null,
            EndDate = r["EndDate"] != DBNull.Value ? Convert.ToDateTime(r["EndDate"]) : null,
            CreatedBy = r["CreatedBy"]?.ToString(),
            CreatedAt = Convert.ToDateTime(r["CreatedAt"])
        }).ToList();
    }

    public async Task<int> SaveReportAsync(ReportDefinition report)
    {
        var sql = @"
            INSERT INTO SavedReports (Name, ReportType, DateFilter, StartDate, EndDate, CreatedBy, CreatedAt)
            VALUES (@Name, @ReportType, @DateFilter, @StartDate, @EndDate, @CreatedBy, GETDATE());
            SELECT CAST(SCOPE_IDENTITY() AS int);";
        return await _db.ScalarAsync<int>(sql, new()
        {
            ["Name"] = report.Name,
            ["ReportType"] = (int)report.Type,
            ["DateFilter"] = (int)report.DateFilter,
            ["StartDate"] = (object?)report.StartDate ?? DBNull.Value,
            ["EndDate"] = (object?)report.EndDate ?? DBNull.Value,
            ["CreatedBy"] = report.CreatedBy
        });
    }

    public async Task DeleteReportAsync(int reportId)
    {
        await _db.ExecuteAsync("DELETE FROM SavedReports WHERE ReportId = @Id", new() { ["Id"] = reportId });
    }

    // ============================================================================
    // Custom Report Builder Logic
    // ============================================================================

    public async Task<List<string>> GetTablesAsync()
    {
        var sql = "SELECT name FROM sys.tables WHERE is_ms_shipped = 0 ORDER BY name";
        var dt = await _db.QueryAsync(sql);
        return dt.Map(r => r["name"]?.ToString() ?? "").ToList();
    }

    public async Task<List<string>> GetColumnsAsync(string tableName)
    {
        var sql = "SELECT name FROM sys.columns WHERE object_id = OBJECT_ID(@Table) ORDER BY column_id";
        var dt = await _db.QueryAsync(sql, new() { ["Table"] = tableName });
        return dt.Map(r => r["name"]?.ToString() ?? "").ToList();
    }

    public async Task<ReportResult> GenerateCustomReportAsync(string tableName, List<string> columns)
    {
        if (string.IsNullOrEmpty(tableName) || columns == null || !columns.Any())
            throw new ArgumentException("Table and columns must be specified");

        var colList = string.Join(", ", columns.Select(c => $"[{c}]"));
        var sql = $"SELECT TOP 1000 {colList} FROM [{tableName}]";

        var dt = await _db.QueryAsync(sql);

        return new ReportResult
        {
            Title = $"Custom Report: {tableName}",
            Headers = columns,
            Rows = dt.Map(r => columns.Select(c => r[c]?.ToString() ?? "").ToList()).ToList(),
            GeneratedAt = DateTime.Now
        };
    }
}

