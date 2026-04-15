using System.Data;
using Techlight.MyDesk.MCP.Models;

namespace Techlight.MyDesk.MCP.Services;

public class InvoiceService
{
    private readonly DatabaseService _db;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(DatabaseService db, ILogger<InvoiceService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Invoice?> GetInvoiceByIdAsync(int invoiceId, McpContext context)
    {
        var sql = @"
            SELECT i.InvoiceId, i.InvoiceNumber, c.CompanyName, 
                   i.AmountIncGST as Amount, i.AmountExGST, i.GST,
                   COALESCE(i.Status, 'Pending') as Status, i.InvoiceDate, 
                   i.DueDate, u.Name as Originator, i.Reference, 
                   i.QuoteId, i.PurchaseOrderId, i.ContactId
            FROM Invoices i
            INNER JOIN Contacts c ON i.ContactId = c.ContactId
            INNER JOIN Users u ON i.Code = u.Code
            WHERE i.InvoiceId = @InvoiceId
            AND i.DivisionId IN (SELECT value FROM STRING_SPLIT(@DivisionIds, ','))";

        var dt = await _db.ExecuteQueryAsync(sql, new Dictionary<string, object>
        {
            ["InvoiceId"] = invoiceId,
            ["DivisionIds"] = string.Join(",", context.AccessibleDivisions)
        });

        if (dt.Rows.Count == 0) return null;

        return MapInvoiceFromDataRow(dt.Rows[0]);
    }

    public async Task<List<Invoice>> GetInvoicesAsync(DateTime? dateFrom = null, DateTime? dateTo = null,
        string? customerName = null, string? originatorCode = null, string? status = null,
        int? quoteId = null, int? limit = 50, McpContext? context = null)
    {
        var sql = @"
            SELECT TOP (@Limit) i.InvoiceId, i.InvoiceNumber, c.CompanyName, 
                   i.AmountIncGST as Amount, i.AmountExGST, i.GST,
                   COALESCE(i.Status, 'Pending') as Status, i.InvoiceDate, 
                   i.DueDate, u.Name as Originator, i.Reference, 
                   i.QuoteId, i.PurchaseOrderId, i.ContactId
            FROM Invoices i
            INNER JOIN Contacts c ON i.ContactId = c.ContactId
            INNER JOIN Users u ON i.Code = u.Code
            WHERE 1=1";

        var parameters = new Dictionary<string, object>
        {
            ["Limit"] = limit ?? 50
        };

        if (dateFrom.HasValue)
        {
            sql += " AND i.InvoiceDate >= @DateFrom";
            parameters["DateFrom"] = dateFrom.Value;
        }

        if (dateTo.HasValue)
        {
            sql += " AND i.InvoiceDate <= @DateTo";
            parameters["DateTo"] = dateTo.Value;
        }

        if (!string.IsNullOrEmpty(customerName))
        {
            sql += " AND c.CompanyName LIKE @CustomerName";
            parameters["CustomerName"] = $"%{customerName}%";
        }

        if (!string.IsNullOrEmpty(originatorCode))
        {
            sql += " AND i.Code = @OriginatorCode";
            parameters["OriginatorCode"] = originatorCode;
        }

        if (!string.IsNullOrEmpty(status))
        {
            sql += " AND COALESCE(i.Status, 'Pending') = @Status";
            parameters["Status"] = status;
        }

        if (quoteId.HasValue)
        {
            sql += " AND i.QuoteId = @QuoteId";
            parameters["QuoteId"] = quoteId.Value;
        }

        if (context?.AccessibleDivisions?.Any() == true)
        {
            sql += " AND i.DivisionId IN (SELECT value FROM STRING_SPLIT(@DivisionIds, ','))";
            parameters["DivisionIds"] = string.Join(",", context.AccessibleDivisions);
        }

        sql += " ORDER BY i.InvoiceDate DESC";

        var dt = await _db.ExecuteQueryAsync(sql, parameters);
        return dt.AsEnumerable().Select(MapInvoiceFromDataRow).ToList();
    }

    public async Task<ReportResult> GenerateInvoiceReportAsync(InvoiceReportRequest request, McpContext context)
    {
        var sql = @"
            SELECT i.InvoiceId, i.InvoiceNumber, c.CompanyName, i.AmountIncGST, 
                   i.AmountExGST, i.GST, i.InvoiceDate, i.DueDate, 
                   u.Name as Originator, i.Reference, i.QuoteId
            FROM Invoices i
            INNER JOIN Contacts c ON i.ContactId = c.ContactId
            INNER JOIN Users u ON i.Code = u.Code
            WHERE 1=1";

        var parameters = new Dictionary<string, object>();

        if (request.DateFrom.HasValue)
        {
            sql += " AND i.InvoiceDate >= @DateFrom";
            parameters["DateFrom"] = request.DateFrom.Value;
        }

        if (request.DateTo.HasValue)
        {
            sql += " AND i.InvoiceDate <= @DateTo";
            parameters["DateTo"] = request.DateTo.Value;
        }

        if (!string.IsNullOrEmpty(request.CustomerName))
        {
            sql += " AND c.CompanyName LIKE @CustomerName";
            parameters["CustomerName"] = $"%{request.CustomerName}%";
        }

        if (!string.IsNullOrEmpty(request.OriginatorCode))
        {
            sql += " AND i.Code = @OriginatorCode";
            parameters["OriginatorCode"] = request.OriginatorCode;
        }

        if (request.QuoteId.HasValue)
        {
            sql += " AND i.QuoteId = @QuoteId";
            parameters["QuoteId"] = request.QuoteId.Value;
        }

        if (!string.IsNullOrEmpty(request.Status))
        {
            sql += " AND COALESCE(i.Status, 'Pending') = @Status";
            parameters["Status"] = request.Status;
        }

        if (context.AccessibleDivisions?.Any() == true)
        {
            sql += " AND i.DivisionId IN (SELECT value FROM STRING_SPLIT(@DivisionIds, ','))";
            parameters["DivisionIds"] = string.Join(",", context.AccessibleDivisions);
        }

        sql += " ORDER BY i.InvoiceDate DESC";

        var dt = await _db.ExecuteQueryAsync(sql, parameters);
        var records = _db.DataTableToList(dt);

        decimal totalAmount = records.Sum(r => r.ContainsKey("AmountIncGST") ? Convert.ToDecimal(r["AmountIncGST"]) : 0);
        decimal totalExGST = records.Sum(r => r.ContainsKey("AmountExGST") ? Convert.ToDecimal(r["AmountExGST"]) : 0);
        decimal totalGST = records.Sum(r => r.ContainsKey("GST") ? Convert.ToDecimal(r["GST"]) : 0);

        var dateRange = request.DateFrom.HasValue && request.DateTo.HasValue
            ? $"{request.DateFrom.Value:dd/MM/yyyy} - {request.DateTo.Value:dd/MM/yyyy}"
            : "All Time";

        var customerFilter = !string.IsNullOrEmpty(request.CustomerName)
            ? $" for customer '{request.CustomerName}'"
            : "";

        return new ReportResult
        {
            Title = $"Invoice Report{customerFilter}: {dateRange}",
            TotalRecords = records.Count,
            TotalAmount = totalAmount,
            Records = records,
            SummaryText = $"Found {records.Count} invoices. Total: ${totalAmount:N2} (Ex GST: ${totalExGST:N2}, GST: ${totalGST:N2})"
        };
    }

    public async Task<List<Invoice>> GetLatestInvoicesAsync(int days, McpContext context)
    {
        var dateFrom = DateTime.Now.AddDays(-days);
        return await GetInvoicesAsync(dateFrom: dateFrom, limit: 100, context: context);
    }

    private Invoice MapInvoiceFromDataRow(DataRow row)
    {
        return new Invoice
        {
            InvoiceId = Convert.ToInt32(row["InvoiceId"]),
            InvoiceNumber = row["InvoiceNumber"].ToString()!,
            CompanyName = row["CompanyName"].ToString()!,
            Amount = Convert.ToDecimal(row["Amount"]),
            AmountExGST = Convert.ToDecimal(row["AmountExGST"]),
            GST = Convert.ToDecimal(row["GST"]),
            Status = row["Status"].ToString()!,
            InvoiceDate = Convert.ToDateTime(row["InvoiceDate"]),
            DueDate = row["DueDate"] == DBNull.Value ? null : Convert.ToDateTime(row["DueDate"]),
            Originator = row["Originator"].ToString()!,
            Reference = row["Reference"]?.ToString(),
            QuoteId = row["QuoteId"] == DBNull.Value ? null : Convert.ToInt32(row["QuoteId"]),
            PurchaseOrderId = row["PurchaseOrderId"] == DBNull.Value ? null : Convert.ToInt32(row["PurchaseOrderId"]),
            ContactId = Convert.ToInt32(row["ContactId"])
        };
    }
}
