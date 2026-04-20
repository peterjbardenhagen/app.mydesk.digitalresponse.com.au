using System.Data;
using Microsoft.Extensions.Logging;
using Techlight.MyDesk.Shared.Models;

namespace Techlight.MyDesk.Shared.Services;

public class InvoiceService
{
    private readonly DatabaseService _db;
    private readonly ILogger<InvoiceService> _logger;

    public InvoiceService(DatabaseService db, ILogger<InvoiceService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Invoice>> GetInvoicesAsync(
        DateTime? dateFrom = null, DateTime? dateTo = null,
        string? customerName = null, string? status = null,
        string? originatorCode = null, int limit = 500)
    {
        var sql = @"
            SELECT TOP (@Limit) i.InvoiceId,
                   ISNULL(i.InvoiceNumber, '') AS InvoiceNumber,
                   ISNULL(c.CompanyName, '') AS CompanyName,
                   ISNULL(i.Amount, 0) AS Amount,
                   ISNULL(i.AmountExGST, 0) AS AmountExGST,
                   ISNULL(i.GST, 0) AS GST,
                   ISNULL(ists.InvoiceStatus, '') AS Status,
                   i.InvoiceDate, i.DueDate,
                   ISNULL(u.Name, '') AS Originator,
                   i.Reference, i.QuoteId, i.PurchaseOrderId,
                   ISNULL(i.ContactId, 0) AS ContactId,
                   ISNULL(i.InvoiceStatusId, 0) AS InvoiceStatusId
            FROM Invoices i
            LEFT JOIN Contacts c ON i.ContactId = c.ContactId
            LEFT JOIN InvoiceStatus ists ON i.InvoiceStatusId = ists.InvoiceStatusId
            LEFT JOIN Users u ON i.Code = u.Code
            WHERE 1=1";

        var parameters = new Dictionary<string, object?> { ["Limit"] = limit };

        if (dateFrom.HasValue) { sql += " AND i.InvoiceDate >= @DateFrom"; parameters["DateFrom"] = dateFrom.Value; }
        if (dateTo.HasValue) { sql += " AND i.InvoiceDate <= @DateTo"; parameters["DateTo"] = dateTo.Value; }
        if (!string.IsNullOrEmpty(customerName)) { sql += " AND c.CompanyName LIKE @CustomerName"; parameters["CustomerName"] = $"%{customerName}%"; }
        if (!string.IsNullOrEmpty(status)) { sql += " AND ists.InvoiceStatus = @Status"; parameters["Status"] = status; }
        if (!string.IsNullOrEmpty(originatorCode)) { sql += " AND i.Code = @OriginatorCode"; parameters["OriginatorCode"] = originatorCode; }

        sql += " ORDER BY i.InvoiceDate DESC";

        var dt = await _db.QueryAsync(sql, parameters);
        return dt.Map(MapInvoice);
    }

    public async Task<Invoice?> GetInvoiceAsync(int invoiceId)
    {
        var sql = @"
            SELECT i.InvoiceId,
                   ISNULL(i.InvoiceNumber, '') AS InvoiceNumber,
                   ISNULL(c.CompanyName, '') AS CompanyName,
                   ISNULL(i.Amount, 0) AS Amount,
                   ISNULL(i.AmountExGST, 0) AS AmountExGST,
                   ISNULL(i.GST, 0) AS GST,
                   ISNULL(ists.InvoiceStatus, '') AS Status,
                   i.InvoiceDate, i.DueDate,
                   ISNULL(u.Name, '') AS Originator,
                   i.Reference, i.QuoteId, i.PurchaseOrderId,
                   ISNULL(i.ContactId, 0) AS ContactId,
                   ISNULL(i.InvoiceStatusId, 0) AS InvoiceStatusId
            FROM Invoices i
            LEFT JOIN Contacts c ON i.ContactId = c.ContactId
            LEFT JOIN InvoiceStatus ists ON i.InvoiceStatusId = ists.InvoiceStatusId
            LEFT JOIN Users u ON i.Code = u.Code
            WHERE i.InvoiceId = @InvoiceId";

        var dt = await _db.QueryAsync(sql, new() { ["InvoiceId"] = invoiceId });
        return dt.Rows.Count == 0 ? null : MapInvoice(dt.Rows[0]);
    }

    public async Task<int> CreateInvoiceAsync(Invoice inv, string originatorCode)
    {
        var sql = @"
            INSERT INTO Invoices (InvoiceNumber, ContactId, InvoiceStatusId, Code,
                                  Amount, AmountExGST, GST, InvoiceDate, DueDate,
                                  Reference, QuoteId, PurchaseOrderId)
            VALUES (@InvoiceNumber, @ContactId, @InvoiceStatusId, @Code,
                    @Amount, @AmountExGST, @GST, @InvoiceDate, @DueDate,
                    @Reference, @QuoteId, @PurchaseOrderId)";

        return await _db.InsertAsync(sql, new()
        {
            ["InvoiceNumber"] = inv.InvoiceNumber,
            ["ContactId"] = inv.ContactId,
            ["InvoiceStatusId"] = inv.InvoiceStatusId > 0 ? inv.InvoiceStatusId : 1,
            ["Code"] = originatorCode,
            ["Amount"] = inv.Amount,
            ["AmountExGST"] = inv.AmountExGST,
            ["GST"] = inv.GST,
            ["InvoiceDate"] = inv.InvoiceDate == default ? DateTime.Now : inv.InvoiceDate,
            ["DueDate"] = (object?)inv.DueDate ?? DBNull.Value,
            ["Reference"] = (object?)inv.Reference ?? DBNull.Value,
            ["QuoteId"] = (object?)inv.QuoteId ?? DBNull.Value,
            ["PurchaseOrderId"] = (object?)inv.PurchaseOrderId ?? DBNull.Value,
        });
    }

    public async Task<int> UpdateInvoiceAsync(Invoice inv)
    {
        var sql = @"
            UPDATE Invoices SET
                InvoiceNumber = @InvoiceNumber,
                ContactId = @ContactId,
                InvoiceStatusId = @InvoiceStatusId,
                Amount = @Amount,
                AmountExGST = @AmountExGST,
                GST = @GST,
                InvoiceDate = @InvoiceDate,
                DueDate = @DueDate,
                Reference = @Reference
            WHERE InvoiceId = @InvoiceId";

        return await _db.ExecuteAsync(sql, new()
        {
            ["InvoiceId"] = inv.InvoiceId,
            ["InvoiceNumber"] = inv.InvoiceNumber,
            ["ContactId"] = inv.ContactId,
            ["InvoiceStatusId"] = inv.InvoiceStatusId,
            ["Amount"] = inv.Amount,
            ["AmountExGST"] = inv.AmountExGST,
            ["GST"] = inv.GST,
            ["InvoiceDate"] = inv.InvoiceDate,
            ["DueDate"] = (object?)inv.DueDate ?? DBNull.Value,
            ["Reference"] = (object?)inv.Reference ?? DBNull.Value,
        });
    }

    private static Invoice MapInvoice(DataRow r) => new()
    {
        InvoiceId = Convert.ToInt32(r["InvoiceId"]),
        InvoiceNumber = r["InvoiceNumber"]?.ToString() ?? "",
        CompanyName = r["CompanyName"]?.ToString() ?? "",
        Amount = Convert.ToDecimal(r["Amount"]),
        AmountExGST = Convert.ToDecimal(r["AmountExGST"]),
        GST = Convert.ToDecimal(r["GST"]),
        Status = r["Status"]?.ToString() ?? "",
        InvoiceDate = r["InvoiceDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["InvoiceDate"]),
        DueDate = r["DueDate"] == DBNull.Value ? null : Convert.ToDateTime(r["DueDate"]),
        Originator = r["Originator"]?.ToString() ?? "",
        Reference = r["Reference"] == DBNull.Value ? null : r["Reference"]?.ToString(),
        QuoteId = r["QuoteId"] == DBNull.Value ? null : Convert.ToInt32(r["QuoteId"]),
        PurchaseOrderId = r["PurchaseOrderId"] == DBNull.Value ? null : Convert.ToInt32(r["PurchaseOrderId"]),
        ContactId = Convert.ToInt32(r["ContactId"]),
        InvoiceStatusId = Convert.ToInt32(r["InvoiceStatusId"]),
    };
}
