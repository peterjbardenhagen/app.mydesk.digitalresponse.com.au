using System.Data;
using Microsoft.Extensions.Logging;
using Techlight.MyDesk.Shared.Models;

namespace Techlight.MyDesk.Shared.Services;

public class PurchaseOrderService
{
    private readonly DatabaseService _db;
    private readonly ILogger<PurchaseOrderService> _logger;

    public PurchaseOrderService(DatabaseService db, ILogger<PurchaseOrderService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<PurchaseOrder>> GetPurchaseOrdersAsync(
        DateTime? dateFrom = null, DateTime? dateTo = null,
        string? supplierName = null, string? status = null,
        string? originatorCode = null, int limit = 500)
    {
        var sql = @"
            SELECT TOP (@Limit) p.PurchaseOrderId,
                   ISNULL(p.PurchaseOrderNumber, '') AS PurchaseOrderNumber,
                   ISNULL(c.CompanyName, '') AS SupplierName,
                   p.Reference,
                   ISNULL(p.Amount, 0) AS Amount,
                   ISNULL(p.AmountExGST, 0) AS AmountExGST,
                   ISNULL(ps.POStatus, '') AS Status,
                   p.PODate, p.ExpectedDelivery,
                   ISNULL(u.Name, '') AS Originator,
                   p.QuoteId,
                   ISNULL(p.ContactId, 0) AS ContactId,
                   ISNULL(p.POStatusId, 0) AS POStatusId
            FROM PurchaseOrders p
            LEFT JOIN Contacts c ON p.ContactId = c.ContactId
            LEFT JOIN PurchaseOrderStatus ps ON p.POStatusId = ps.POStatusId
            LEFT JOIN Users u ON p.Code = u.Code
            WHERE 1=1";

        var parameters = new Dictionary<string, object?> { ["Limit"] = limit };

        if (dateFrom.HasValue) { sql += " AND p.PODate >= @DateFrom"; parameters["DateFrom"] = dateFrom.Value; }
        if (dateTo.HasValue) { sql += " AND p.PODate <= @DateTo"; parameters["DateTo"] = dateTo.Value; }
        if (!string.IsNullOrEmpty(supplierName)) { sql += " AND c.CompanyName LIKE @SupplierName"; parameters["SupplierName"] = $"%{supplierName}%"; }
        if (!string.IsNullOrEmpty(status)) { sql += " AND ps.POStatus = @Status"; parameters["Status"] = status; }
        if (!string.IsNullOrEmpty(originatorCode)) { sql += " AND p.Code = @OriginatorCode"; parameters["OriginatorCode"] = originatorCode; }

        sql += " ORDER BY p.PODate DESC";

        var dt = await _db.QueryAsync(sql, parameters);
        return dt.Map(MapPO);
    }

    public async Task<PurchaseOrder?> GetPurchaseOrderAsync(int poId)
    {
        var sql = @"
            SELECT p.PurchaseOrderId,
                   ISNULL(p.PurchaseOrderNumber, '') AS PurchaseOrderNumber,
                   ISNULL(c.CompanyName, '') AS SupplierName,
                   p.Reference,
                   ISNULL(p.Amount, 0) AS Amount,
                   ISNULL(p.AmountExGST, 0) AS AmountExGST,
                   ISNULL(ps.POStatus, '') AS Status,
                   p.PODate, p.ExpectedDelivery,
                   ISNULL(u.Name, '') AS Originator,
                   p.QuoteId,
                   ISNULL(p.ContactId, 0) AS ContactId,
                   ISNULL(p.POStatusId, 0) AS POStatusId
            FROM PurchaseOrders p
            LEFT JOIN Contacts c ON p.ContactId = c.ContactId
            LEFT JOIN PurchaseOrderStatus ps ON p.POStatusId = ps.POStatusId
            LEFT JOIN Users u ON p.Code = u.Code
            WHERE p.PurchaseOrderId = @Id";

        var dt = await _db.QueryAsync(sql, new() { ["Id"] = poId });
        return dt.Rows.Count == 0 ? null : MapPO(dt.Rows[0]);
    }

    public async Task<int> CreatePurchaseOrderAsync(PurchaseOrder po, string originatorCode)
    {
        var sql = @"
            INSERT INTO PurchaseOrders (PurchaseOrderNumber, ContactId, POStatusId, Code,
                                        Amount, AmountExGST, PODate, ExpectedDelivery,
                                        Reference, QuoteId)
            VALUES (@PurchaseOrderNumber, @ContactId, @POStatusId, @Code,
                    @Amount, @AmountExGST, @PODate, @ExpectedDelivery,
                    @Reference, @QuoteId)";

        return await _db.InsertAsync(sql, new()
        {
            ["PurchaseOrderNumber"] = po.PurchaseOrderNumber,
            ["ContactId"] = po.ContactId,
            ["POStatusId"] = po.POStatusId > 0 ? po.POStatusId : 1,
            ["Code"] = originatorCode,
            ["Amount"] = po.Amount,
            ["AmountExGST"] = po.AmountExGST,
            ["PODate"] = po.PODate == default ? DateTime.Now : po.PODate,
            ["ExpectedDelivery"] = (object?)po.ExpectedDelivery ?? DBNull.Value,
            ["Reference"] = (object?)po.Reference ?? DBNull.Value,
            ["QuoteId"] = (object?)po.QuoteId ?? DBNull.Value,
        });
    }

    public async Task<int> UpdatePurchaseOrderAsync(PurchaseOrder po)
    {
        var sql = @"
            UPDATE PurchaseOrders SET
                PurchaseOrderNumber = @PurchaseOrderNumber,
                ContactId = @ContactId,
                POStatusId = @POStatusId,
                Amount = @Amount,
                AmountExGST = @AmountExGST,
                PODate = @PODate,
                ExpectedDelivery = @ExpectedDelivery,
                Reference = @Reference
            WHERE PurchaseOrderId = @PurchaseOrderId";

        return await _db.ExecuteAsync(sql, new()
        {
            ["PurchaseOrderId"] = po.PurchaseOrderId,
            ["PurchaseOrderNumber"] = po.PurchaseOrderNumber,
            ["ContactId"] = po.ContactId,
            ["POStatusId"] = po.POStatusId,
            ["Amount"] = po.Amount,
            ["AmountExGST"] = po.AmountExGST,
            ["PODate"] = po.PODate,
            ["ExpectedDelivery"] = (object?)po.ExpectedDelivery ?? DBNull.Value,
            ["Reference"] = (object?)po.Reference ?? DBNull.Value,
        });
    }

    private static PurchaseOrder MapPO(DataRow r) => new()
    {
        PurchaseOrderId = Convert.ToInt32(r["PurchaseOrderId"]),
        PurchaseOrderNumber = r["PurchaseOrderNumber"]?.ToString() ?? "",
        SupplierName = r["SupplierName"]?.ToString() ?? "",
        Reference = r["Reference"] == DBNull.Value ? null : r["Reference"]?.ToString(),
        Amount = Convert.ToDecimal(r["Amount"]),
        AmountExGST = Convert.ToDecimal(r["AmountExGST"]),
        Status = r["Status"]?.ToString() ?? "",
        PODate = r["PODate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["PODate"]),
        ExpectedDelivery = r["ExpectedDelivery"] == DBNull.Value ? null : Convert.ToDateTime(r["ExpectedDelivery"]),
        Originator = r["Originator"]?.ToString() ?? "",
        QuoteId = r["QuoteId"] == DBNull.Value ? null : Convert.ToInt32(r["QuoteId"]),
        ContactId = Convert.ToInt32(r["ContactId"]),
        POStatusId = Convert.ToInt32(r["POStatusId"]),
    };
}
