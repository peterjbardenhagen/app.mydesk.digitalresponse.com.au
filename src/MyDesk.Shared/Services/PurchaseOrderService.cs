using System.Data;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class PurchaseOrderService
{
    private readonly DatabaseService _db;
    private readonly ILogger<PurchaseOrderService> _logger;

    public PurchaseOrderService(DatabaseService db, ILogger<PurchaseOrderService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // ── Shared SELECT fragment ──────────────────────────────────────────────
    private const string SelectCols = @"
        p.POid,
        ISNULL(p.Code,'')           AS Code,
        ISNULL(u.Name,'')           AS OriginatorName,
        ISNULL(p.Project,'')        AS Project,
        ISNULL(p.ContactId,0)       AS ContactId,
        ISNULL(c.Company,'')         AS SupplierName,
        ISNULL(p.DivisionId,0)      AS DivisionId,
        ISNULL(d.Division,'')       AS DivisionName,
        p.PODate,
        ISNULL(p.POStatusId,1)      AS POStatusId,
        ISNULL(s.POStatus,'')       AS StatusName,
        ISNULL(p.GST,0)             AS GST,
        p.POPaymentTypeId,
        ISNULL(pt.POPaymentType,'') AS PaymentType,
        p.Terms, p.DateRequired,
        p.DeliverToLocationId,
        ISNULL(loc.Company,'')      AS LocationName,
        ISNULL(p.DeliverToLocation,'') AS DeliverToLocation,
        p.IntroText, p.InternalNotes,
        ISNULL(p.PriceExTotal,0)    AS PriceExTotal,
        ISNULL(p.PriceGSTTotal,0)   AS PriceGSTTotal,
        ISNULL(p.PriceIncTotal,0)   AS PriceIncTotal,
        ISNULL(p.RFQid,0)           AS RFQid,
        ISNULL(p.Qid,0)             AS Qid,
        ISNULL(p.HasCapEx,0)        AS HasCapEx
    FROM PurchaseOrders p
    LEFT JOIN Users u                    ON p.Code = u.Code
    LEFT JOIN PurchaseOrderStatus s      ON p.POStatusId = s.POStatusId
    LEFT JOIN Contacts cn                ON p.ContactId  = cn.ContactId
    LEFT JOIN Companies c                ON cn.CompanyId = c.CompanyId
    LEFT JOIN Divisions d                ON p.DivisionId = d.DivisionId
    LEFT JOIN PurchaseOrderPaymentTypes pt ON p.POPaymentTypeId = pt.POPaymentTypeId
    LEFT JOIN Locations loc              ON p.DeliverToLocationId = loc.LocationId";

    // ── List ───────────────────────────────────────────────────────────────

    public async Task<List<PurchaseOrder>> GetPurchaseOrdersAsync(
        DateTime? dateFrom = null, DateTime? dateTo = null,
        string? supplier = null, int statusId = 0,
        string? originatorCode = null, int? divisionId = null,
        bool pendingApprovalOnly = false, int limit = 500)
    {
        var sql = $"SELECT TOP {limit} {SelectCols} WHERE 1=1";
        var p   = new Dictionary<string, object?>();

        if (pendingApprovalOnly)    { sql += " AND p.POStatusId = 2"; }
        else if (statusId > 0)      { sql += " AND p.POStatusId = @Sid"; p["Sid"] = statusId; }
        if (dateFrom.HasValue)      { sql += " AND p.PODate >= @F"; p["F"] = dateFrom.Value; }
        if (dateTo.HasValue)        { sql += " AND p.PODate <= @T"; p["T"] = dateTo.Value; }
        if (!string.IsNullOrEmpty(supplier))       { sql += " AND c.Company LIKE @S"; p["S"] = $"%{supplier}%"; }
        if (!string.IsNullOrEmpty(originatorCode)) { sql += " AND p.Code = @OC"; p["OC"] = originatorCode; }
        if (divisionId.HasValue)    { sql += " AND p.DivisionId = @DivId"; p["DivId"] = divisionId.Value; }

        sql += " ORDER BY p.PODate DESC, p.POid DESC";
        return (await _db.QueryAsync(sql, p)).Map(MapPO);
    }

    public async Task<PurchaseOrder?> GetPurchaseOrderAsync(int poId)
    {
        var sql = $"SELECT {SelectCols} WHERE p.POid = @Id";
        var dt  = await _db.QueryAsync(sql, new() { ["Id"] = poId });
        return dt.Rows.Count == 0 ? null : MapPO(dt.Rows[0]);
    }

    // ── Line items ─────────────────────────────────────────────────────────

    public async Task<List<POLineItem>> GetLineItemsAsync(int poId)
    {
        var dt = await _db.QueryAsync(@"
            SELECT pc.POItemId, pc.POid,
                   pc.PartCodeId,
                   ISNULL(pc.Quantity,1)      AS Quantity,
                   ISNULL(pc.Description,'')  AS Description,
                   ISNULL(pc.PriceEx,0)       AS PriceEx,
                   ISNULL(pc.PriceExSubTotal,0) AS PriceExSubTotal,
                   pc.POProductTypeId,
                   ISNULL(pt.POProductType,'') AS ProductTypeName,
                   ISNULL(pt.IsCapEx,0)        AS IsCapEx
            FROM PurchaseOrderContents pc
            LEFT JOIN PurchaseOrderProductTypes pt ON pc.POProductTypeId = pt.POProductTypeId
            WHERE pc.POid = @Id
            ORDER BY pc.POItemId",
            new() { ["Id"] = poId });
        return dt.Map(r => new POLineItem
        {
            POItemId        = Convert.ToInt32(r["POItemId"]),
            POid            = Convert.ToInt32(r["POid"]),
            PartCodeId      = r["PartCodeId"] == DBNull.Value ? null : Convert.ToInt32(r["PartCodeId"]),
            Quantity        = Convert.ToInt32(r["Quantity"]),
            Description     = r["Description"]?.ToString() ?? "",
            PriceEx         = Convert.ToDecimal(r["PriceEx"]),
            PriceExSubTotal = Convert.ToDecimal(r["PriceExSubTotal"]),
            POProductTypeId = r["POProductTypeId"] == DBNull.Value ? null : Convert.ToInt32(r["POProductTypeId"]),
            ProductTypeName = r["ProductTypeName"]?.ToString(),
            IsCapEx         = r["IsCapEx"] != DBNull.Value && Convert.ToBoolean(r["IsCapEx"]),
        });
    }

    // ── Create ─────────────────────────────────────────────────────────────

    public async Task<int> CreatePurchaseOrderAsync(PurchaseOrder po, List<POLineItem> lines, string userCode)
    {
        var id = await _db.InsertAsync(@"
            INSERT INTO PurchaseOrders
                (Code, Project, ContactId, DivisionId, PODate, POStatusId,
                 GST, POPaymentTypeId, Terms, DateRequired,
                 DeliverToLocationId, DeliverToLocation,
                 IntroText, InternalNotes,
                 PriceExTotal, PriceGSTTotal, PriceIncTotal,
                 RFQid, Qid, HasCapEx)
            VALUES
                (@Code, @Project, @ContactId, @DivisionId, @PODate, 1,
                 @GST, @POPaymentTypeId, @Terms, @DateRequired,
                 @DeliverToLocationId, @DeliverToLocation,
                 @IntroText, @InternalNotes,
                 @PriceExTotal, @PriceGSTTotal, @PriceIncTotal,
                 @RFQid, @Qid, @HasCapEx)",
            BuildParams(po, userCode));

        foreach (var line in lines.Where(l => l.Quantity > 0))
            await InsertLineAsync(id, line);

        await WriteAuditAsync(id, userCode, "PO created");
        return id;
    }

    // ── Update ─────────────────────────────────────────────────────────────

    public async Task UpdatePurchaseOrderAsync(PurchaseOrder po, List<POLineItem> lines, string userCode)
    {
        var p = BuildParams(po, userCode);
        p["POid"] = po.POid;
        await _db.ExecuteAsync(@"
            UPDATE PurchaseOrders SET
                Project = @Project, ContactId = @ContactId, DivisionId = @DivisionId,
                GST = @GST, POPaymentTypeId = @POPaymentTypeId,
                Terms = @Terms, DateRequired = @DateRequired,
                DeliverToLocationId = @DeliverToLocationId, DeliverToLocation = @DeliverToLocation,
                IntroText = @IntroText, InternalNotes = @InternalNotes,
                PriceExTotal = @PriceExTotal, PriceGSTTotal = @PriceGSTTotal, PriceIncTotal = @PriceIncTotal,
                Qid = @Qid, HasCapEx = @HasCapEx
            WHERE POid = @POid", p);

        await _db.ExecuteAsync("DELETE FROM PurchaseOrderContents WHERE POid = @Id", new() { ["Id"] = po.POid });
        foreach (var line in lines.Where(l => l.Quantity > 0))
            await InsertLineAsync(po.POid, line);

        await WriteAuditAsync(po.POid, userCode, "PO updated");
    }

    // ── Status ─────────────────────────────────────────────────────────────

    public async Task UpdateStatusAsync(int poId, int statusId, string userCode, string statusName)
    {
        await _db.ExecuteAsync(
            "UPDATE PurchaseOrders SET POStatusId = @S WHERE POid = @Id",
            new() { ["S"] = statusId, ["Id"] = poId });
        await WriteAuditAsync(poId, userCode, $"Status changed to {statusName}");
    }

    public async Task ApproveAsync(int poId, string userCode)
    {
        await _db.ExecuteAsync(
            "UPDATE PurchaseOrders SET POStatusId = 3 WHERE POid = @Id",
            new() { ["Id"] = poId });
        await WriteAuditAsync(poId, userCode, "PO approved");
    }

    public async Task DeclineAsync(int poId, string userCode)
    {
        await _db.ExecuteAsync(
            "UPDATE PurchaseOrders SET POStatusId = 5 WHERE POid = @Id",
            new() { ["Id"] = poId });
        await WriteAuditAsync(poId, userCode, "PO declined");
    }

    // ── Delete ─────────────────────────────────────────────────────────────

    public async Task DeleteAsync(int poId)
    {
        await _db.ExecuteAsync("DELETE FROM PurchaseOrderContents WHERE POid = @Id", new() { ["Id"] = poId });
        await _db.ExecuteAsync("DELETE FROM PurchaseOrderAudit    WHERE POid = @Id", new() { ["Id"] = poId });
        await _db.ExecuteAsync("DELETE FROM PurchaseOrders        WHERE POid = @Id", new() { ["Id"] = poId });
    }

    // ── Audit ──────────────────────────────────────────────────────────────

    public async Task<List<POAuditEntry>> GetAuditAsync(int poId)
    {
        var dt = await _db.QueryAsync(@"
            SELECT pa.Code, ISNULL(u.Name,'') AS UserName, pa.Action, pa.DateEntered
            FROM PurchaseOrderAudit pa
            LEFT JOIN Users u ON pa.Code = u.Code
            WHERE pa.POid = @Id
            ORDER BY pa.DateEntered DESC",
            new() { ["Id"] = poId });
        return dt.Map(r => new POAuditEntry
        {
            Code        = r["Code"]?.ToString(),
            UserName    = r["UserName"]?.ToString(),
            Action      = r["Action"]?.ToString(),
            DateEntered = r["DateEntered"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["DateEntered"]),
        });
    }

    // ── Supplier Invoice Entries ───────────────────────────────────────────

    public async Task<List<POInvoiceEntry>> GetInvoiceEntriesAsync(int poId)
    {
        try
        {
            var dt = await _db.QueryAsync(
                @"SELECT PurchaseOrderInvoiceId, POid, InvoiceNumber, Amount, InvoiceDate
                  FROM PurchaseOrderInvoices
                  WHERE POid = @Id
                  ORDER BY PurchaseOrderInvoiceId",
                new() { ["Id"] = poId });
            return dt.Map(r => new POInvoiceEntry
            {
                PurchaseOrderInvoiceId = Convert.ToInt32(r["PurchaseOrderInvoiceId"]),
                POid = Convert.ToInt32(r["POid"]),
                InvoiceNumber = r["InvoiceNumber"]?.ToString(),
                Amount = r["Amount"] == DBNull.Value ? 0m : Convert.ToDecimal(r["Amount"]),
                InvoiceDate = r["InvoiceDate"] == DBNull.Value ? null : (DateTime?)Convert.ToDateTime(r["InvoiceDate"]),
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PurchaseOrderInvoices table may not exist");
            return new List<POInvoiceEntry>();
        }
    }

    public async Task SaveInvoiceEntriesAsync(int poId, List<POInvoiceEntry> entries, string userCode)
    {
        // Ensure table exists
        await _db.ExecuteAsync(@"
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'PurchaseOrderInvoices')
            CREATE TABLE PurchaseOrderInvoices (
                PurchaseOrderInvoiceId INT IDENTITY(1,1) PRIMARY KEY,
                POid INT NOT NULL,
                InvoiceNumber NVARCHAR(100) NULL,
                Amount DECIMAL(18,2) NULL,
                InvoiceDate DATETIME NULL,
                DateEntered DATETIME NOT NULL DEFAULT GETDATE(),
                EnteredBy NVARCHAR(10) NULL
            )");

        // Delete existing and re-insert (matches legacy _Proc behaviour)
        await _db.ExecuteAsync("DELETE FROM PurchaseOrderInvoices WHERE POid = @Id", new() { ["Id"] = poId });

        foreach (var e in entries)
        {
            await _db.InsertAsync(@"
                INSERT INTO PurchaseOrderInvoices (POid, InvoiceNumber, Amount, InvoiceDate, EnteredBy)
                VALUES (@POid, @Num, @Amt, @Dt, @Usr)",
                new()
                {
                    ["POid"] = poId,
                    ["Num"] = (object?)e.InvoiceNumber ?? DBNull.Value,
                    ["Amt"] = e.Amount,
                    ["Dt"] = (object?)e.InvoiceDate ?? DBNull.Value,
                    ["Usr"] = userCode,
                });
        }

        await WriteAuditAsync(poId, userCode, $"Supplier invoice details updated ({entries.Count} entries)");
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private Task InsertLineAsync(int poId, POLineItem l) =>
        _db.InsertAsync(@"
            INSERT INTO PurchaseOrderContents
                (POid, PartCodeId, Quantity, Description, PriceEx, PriceExSubTotal, POProductTypeId)
            VALUES
                (@POid, @PartCodeId, @Qty, @Description, @PriceEx, @PriceExSubTotal, @POProductTypeId)",
            new()
            {
                ["POid"]           = poId,
                ["PartCodeId"]     = (object?)l.PartCodeId ?? DBNull.Value,
                ["Qty"]            = l.Quantity,
                ["Description"]    = l.Description,
                ["PriceEx"]        = l.PriceEx,
                ["PriceExSubTotal"]= l.PriceExSubTotal,
                ["POProductTypeId"]= (object?)l.POProductTypeId ?? DBNull.Value,
            });

    private Task WriteAuditAsync(int poId, string code, string action) =>
        _db.InsertAsync(
            "INSERT INTO PurchaseOrderAudit (POid, Code, Action, DateEntered) VALUES (@Id, @C, @A, @D)",
            new() { ["Id"] = poId, ["C"] = code, ["A"] = action, ["D"] = DateTime.Now });

    private static Dictionary<string, object?> BuildParams(PurchaseOrder po, string userCode) => new()
    {
        ["Code"]              = userCode,
        ["Project"]           = po.Project,
        ["ContactId"]         = po.ContactId,
        ["DivisionId"]        = po.DivisionId,
        ["PODate"]            = po.PODate == default ? DateTime.Now : po.PODate,
        ["GST"]               = po.GST,
        ["POPaymentTypeId"]   = (object?)po.POPaymentTypeId ?? DBNull.Value,
        ["Terms"]             = (object?)po.Terms ?? DBNull.Value,
        ["DateRequired"]      = po.DateRequired == default ? (object)DBNull.Value : po.DateRequired,
        ["DeliverToLocationId"] = (object?)po.DeliverToLocationId ?? DBNull.Value,
        ["DeliverToLocation"] = (object?)po.DeliverToLocation ?? DBNull.Value,
        ["IntroText"]         = (object?)po.IntroText ?? DBNull.Value,
        ["InternalNotes"]     = (object?)po.InternalNotes ?? DBNull.Value,
        ["PriceExTotal"]      = po.PriceExTotal,
        ["PriceGSTTotal"]     = po.PriceGSTTotal,
        ["PriceIncTotal"]     = po.PriceIncTotal,
        ["RFQid"]             = po.RFQid,
        ["Qid"]               = po.Qid,
        ["HasCapEx"]          = po.HasCapEx,
    };

    private static PurchaseOrder MapPO(DataRow r) => new()
    {
        POid              = Convert.ToInt32(r["POid"]),
        Code              = r["Code"]?.ToString() ?? "",
        OriginatorName    = r["OriginatorName"]?.ToString() ?? "",
        Project           = r["Project"]?.ToString() ?? "",
        ContactId         = Convert.ToInt32(r["ContactId"]),
        SupplierName      = r["SupplierName"]?.ToString() ?? "",
        DivisionId        = Convert.ToInt32(r["DivisionId"]),
        DivisionName      = r["DivisionName"]?.ToString() ?? "",
        PODate            = r["PODate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["PODate"]),
        POStatusId        = Convert.ToInt32(r["POStatusId"]),
        StatusName        = r["StatusName"]?.ToString() ?? "",
        GST               = r["GST"] != DBNull.Value && Convert.ToBoolean(r["GST"]),
        POPaymentTypeId   = r["POPaymentTypeId"] == DBNull.Value ? null : Convert.ToInt32(r["POPaymentTypeId"]),
        PaymentType       = r["PaymentType"]?.ToString(),
        Terms             = r["Terms"] == DBNull.Value ? null : r["Terms"]?.ToString(),
        DateRequired      = r["DateRequired"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["DateRequired"]),
        DeliverToLocationId = r["DeliverToLocationId"] == DBNull.Value ? null : Convert.ToInt32(r["DeliverToLocationId"]),
        LocationName      = r["LocationName"]?.ToString(),
        DeliverToLocation = r["DeliverToLocation"]?.ToString(),
        IntroText         = r["IntroText"] == DBNull.Value ? null : r["IntroText"]?.ToString(),
        InternalNotes     = r["InternalNotes"] == DBNull.Value ? null : r["InternalNotes"]?.ToString(),
        PriceExTotal      = Convert.ToDecimal(r["PriceExTotal"]),
        PriceGSTTotal     = Convert.ToDecimal(r["PriceGSTTotal"]),
        PriceIncTotal     = Convert.ToDecimal(r["PriceIncTotal"]),
        RFQid             = Convert.ToInt32(r["RFQid"]),
        Qid               = Convert.ToInt32(r["Qid"]),
        HasCapEx          = r["HasCapEx"] != DBNull.Value && Convert.ToBoolean(r["HasCapEx"]),
    };
}
