using System.Data;
using Techlight.MyDesk.MCP.Models;

namespace Techlight.MyDesk.MCP.Services;

public class PurchaseOrderService
{
    private readonly DatabaseService _db;
    private readonly ILogger<PurchaseOrderService> _logger;

    public PurchaseOrderService(DatabaseService db, ILogger<PurchaseOrderService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PurchaseOrder?> GetPurchaseOrderByIdAsync(int poId, McpContext context)
    {
        var sql = @"
            SELECT po.PurchaseOrderId, po.PurchaseOrderNumber, s.CompanyName as SupplierName,
                   po.Reference, po.AmountIncGST as Amount, po.AmountExGST,
                   COALESCE(po.Status, 'Draft') as Status, po.PODate, 
                   po.ExpectedDeliveryDate as ExpectedDelivery,
                   u.Name as Originator, po.QuoteId, po.ContactId
            FROM PurchaseOrders po
            INNER JOIN Contacts c ON po.ContactId = c.ContactId
            INNER JOIN Companies s ON c.CompanyId = s.CompanyId
            INNER JOIN Users u ON po.Code = u.Code
            WHERE po.PurchaseOrderId = @PurchaseOrderId
            AND po.DivisionId IN (SELECT value FROM STRING_SPLIT(@DivisionIds, ','))";

        var dt = await _db.ExecuteQueryAsync(sql, new Dictionary<string, object>
        {
            ["PurchaseOrderId"] = poId,
            ["DivisionIds"] = string.Join(",", context.AccessibleDivisions)
        });

        if (dt.Rows.Count == 0) return null;

        return MapPurchaseOrderFromDataRow(dt.Rows[0]);
    }

    public async Task<List<PurchaseOrder>> GetPurchaseOrdersAsync(DateTime? dateFrom = null, DateTime? dateTo = null,
        string? supplierName = null, string? status = null, string? originatorCode = null,
        int? quoteId = null, int? limit = 50, McpContext? context = null)
    {
        var sql = @"
            SELECT TOP (@Limit) po.PurchaseOrderId, po.PurchaseOrderNumber, s.CompanyName as SupplierName,
                   po.Reference, po.AmountIncGST as Amount, po.AmountExGST,
                   COALESCE(po.Status, 'Draft') as Status, po.PODate, 
                   po.ExpectedDeliveryDate as ExpectedDelivery,
                   u.Name as Originator, po.QuoteId, po.ContactId
            FROM PurchaseOrders po
            INNER JOIN Contacts c ON po.ContactId = c.ContactId
            INNER JOIN Companies s ON c.CompanyId = s.CompanyId
            INNER JOIN Users u ON po.Code = u.Code
            WHERE 1=1";

        var parameters = new Dictionary<string, object>
        {
            ["Limit"] = limit ?? 50
        };

        if (dateFrom.HasValue)
        {
            sql += " AND po.PODate >= @DateFrom";
            parameters["DateFrom"] = dateFrom.Value;
        }

        if (dateTo.HasValue)
        {
            sql += " AND po.PODate <= @DateTo";
            parameters["DateTo"] = dateTo.Value;
        }

        if (!string.IsNullOrEmpty(supplierName))
        {
            sql += " AND s.CompanyName LIKE @SupplierName";
            parameters["SupplierName"] = $"%{supplierName}%";
        }

        if (!string.IsNullOrEmpty(status))
        {
            sql += " AND COALESCE(po.Status, 'Draft') = @Status";
            parameters["Status"] = status;
        }

        if (!string.IsNullOrEmpty(originatorCode))
        {
            sql += " AND po.Code = @OriginatorCode";
            parameters["OriginatorCode"] = originatorCode;
        }

        if (quoteId.HasValue)
        {
            sql += " AND po.QuoteId = @QuoteId";
            parameters["QuoteId"] = quoteId.Value;
        }

        if (context?.AccessibleDivisions?.Any() == true)
        {
            sql += " AND po.DivisionId IN (SELECT value FROM STRING_SPLIT(@DivisionIds, ','))";
            parameters["DivisionIds"] = string.Join(",", context.AccessibleDivisions);
        }

        sql += " ORDER BY po.PODate DESC";

        var dt = await _db.ExecuteQueryAsync(sql, parameters);
        return dt.AsEnumerable().Select(MapPurchaseOrderFromDataRow).ToList();
    }

    public async Task<PurchaseOrder> UpdatePurchaseOrderStatusAsync(int poId, string newStatus, string? notes, McpContext context)
    {
        var validStatuses = new[] { "Draft", "Pending", "Ordered", "Partially Received", 
                                     "Received", "Cancelled", "Completed" };
        
        if (!validStatuses.Contains(newStatus))
            throw new ArgumentException($"Invalid status. Valid statuses: {string.Join(", ", validStatuses)}");

        var sql = @"
            UPDATE PurchaseOrders 
            SET Status = @Status, ModifiedDate = GETDATE(), ModifiedBy = @UserCode
            WHERE PurchaseOrderId = @PurchaseOrderId";

        await _db.ExecuteNonQueryAsync(sql, new Dictionary<string, object>
        {
            ["PurchaseOrderId"] = poId,
            ["Status"] = newStatus,
            ["UserCode"] = context.UserCode
        });

        // Add comment if notes provided
        if (!string.IsNullOrEmpty(notes))
        {
            await AddPOCommentAsync(poId, notes, context);
        }

        return (await GetPurchaseOrderByIdAsync(poId, context))!;
    }

    private async Task AddPOCommentAsync(int poId, string comment, McpContext context)
    {
        var sql = @"
            INSERT INTO Comments (TableId, ItemId, FromCode, Comment, DateEntered)
            VALUES (10, @ItemId, @FromCode, @Comment, GETDATE())";

        await _db.ExecuteNonQueryAsync(sql, new Dictionary<string, object>
        {
            ["ItemId"] = poId,
            ["FromCode"] = context.UserCode,
            ["Comment"] = $"Status updated: {comment}"
        });
    }

    public async Task<ReportResult> GeneratePOReportAsync(DateTime? dateFrom = null, DateTime? dateTo = null,
        string? supplierName = null, string? status = null, McpContext? context = null)
    {
        var sql = @"
            SELECT po.PurchaseOrderId, po.PurchaseOrderNumber, s.CompanyName as SupplierName,
                   po.Reference, po.AmountIncGST, po.AmountExGST,
                   po.Status, po.PODate, po.ExpectedDeliveryDate,
                   u.Name as Originator
            FROM PurchaseOrders po
            INNER JOIN Contacts c ON po.ContactId = c.ContactId
            INNER JOIN Companies s ON c.CompanyId = s.CompanyId
            INNER JOIN Users u ON po.Code = u.Code
            WHERE 1=1";

        var parameters = new Dictionary<string, object>();

        if (dateFrom.HasValue)
        {
            sql += " AND po.PODate >= @DateFrom";
            parameters["DateFrom"] = dateFrom.Value;
        }

        if (dateTo.HasValue)
        {
            sql += " AND po.PODate <= @DateTo";
            parameters["DateTo"] = dateTo.Value;
        }

        if (!string.IsNullOrEmpty(supplierName))
        {
            sql += " AND s.CompanyName LIKE @SupplierName";
            parameters["SupplierName"] = $"%{supplierName}%";
        }

        if (!string.IsNullOrEmpty(status))
        {
            sql += " AND COALESCE(po.Status, 'Draft') = @Status";
            parameters["Status"] = status;
        }

        if (context?.AccessibleDivisions?.Any() == true)
        {
            sql += " AND po.DivisionId IN (SELECT value FROM STRING_SPLIT(@DivisionIds, ','))";
            parameters["DivisionIds"] = string.Join(",", context.AccessibleDivisions);
        }

        sql += " ORDER BY po.PODate DESC";

        var dt = await _db.ExecuteQueryAsync(sql, parameters);
        var records = _db.DataTableToList(dt);

        decimal totalAmount = records.Sum(r => r.ContainsKey("AmountIncGST") ? Convert.ToDecimal(r["AmountIncGST"]) : 0);

        var dateRange = dateFrom.HasValue && dateTo.HasValue
            ? $"{dateFrom.Value:dd/MM/yyyy} - {dateTo.Value:dd/MM/yyyy}"
            : "All Time";

        return new ReportResult
        {
            Title = $"Purchase Order Report: {dateRange}",
            TotalRecords = records.Count,
            TotalAmount = totalAmount,
            Records = records,
            SummaryText = $"Found {records.Count} purchase orders totaling ${totalAmount:N2}"
        };
    }

    private PurchaseOrder MapPurchaseOrderFromDataRow(DataRow row)
    {
        return new PurchaseOrder
        {
            PurchaseOrderId = Convert.ToInt32(row["PurchaseOrderId"]),
            PurchaseOrderNumber = row["PurchaseOrderNumber"].ToString()!,
            SupplierName = row["SupplierName"].ToString()!,
            Reference = row["Reference"]?.ToString(),
            Amount = Convert.ToDecimal(row["Amount"]),
            AmountExGST = Convert.ToDecimal(row["AmountExGST"]),
            Status = row["Status"].ToString()!,
            PODate = Convert.ToDateTime(row["PODate"]),
            ExpectedDelivery = row["ExpectedDelivery"] == DBNull.Value ? null : Convert.ToDateTime(row["ExpectedDelivery"]),
            Originator = row["Originator"].ToString()!,
            QuoteId = row["QuoteId"] == DBNull.Value ? null : Convert.ToInt32(row["QuoteId"]),
            ContactId = Convert.ToInt32(row["ContactId"])
        };
    }
}
