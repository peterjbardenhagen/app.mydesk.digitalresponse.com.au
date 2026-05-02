using System.Data;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class QuoteService
{
    private readonly DatabaseService _db;
    private readonly ActivityService _activityService;

    public QuoteService(DatabaseService db, ActivityService activityService)
    {
        _db = db;
        _activityService = activityService;
    }

    public async Task<List<Quote>> GetQuotesAsync(DateTime? dateFrom, DateTime? dateTo, string? customerName, int statusId, string? keyword)
    {
        var sql = @"
            SELECT q.*,
                   ISNULL(d.QuotePrefix, 'QT-') + CAST(q.Qid AS NVARCHAR(20)) AS QuoteNum,
                   COALESCE(NULLIF(cco.Company, ''), '') AS CompanyName,
                   ISNULL(u.Name, '')             AS Originator,
                   ISNULL(qs.QuoteStatus, '')     AS QuoteStatusName
            FROM Quotes q
            LEFT JOIN Contacts    c  ON q.ContactId     = c.ContactId
            LEFT JOIN Companies   cco ON c.CompanyId    = cco.CompanyId
            LEFT JOIN Users       u  ON q.Code          = u.Code
            LEFT JOIN QuoteStatus qs ON q.QuoteStatusId = qs.QuoteStatusId
            LEFT JOIN Divisions   d  ON q.DivisionId    = d.DivisionId
            WHERE (@c IS NULL OR cco.Company LIKE '%' + @c + '%')
              AND (@k IS NULL OR q.Reference LIKE '%' + @k + '%' OR q.CustomerNotes LIKE '%' + @k + '%')
              AND (@f IS NULL OR q.QuoteDate >= @f)
              AND (@t IS NULL OR q.QuoteDate <= @t)
              AND (@s = 0 OR q.QuoteStatusId = @status OR (@s = 555 AND q.QuoteStatusId NOT IN (4,5,11)))
            ORDER BY q.Qid DESC";
        
        var dt = await _db.QueryAsync(sql, new() { 
            ["c"] = (object?)customerName ?? DBNull.Value,
            ["k"] = (object?)keyword ?? DBNull.Value,
            ["f"] = (object?)dateFrom ?? DBNull.Value,
            ["t"] = (object?)dateTo ?? DBNull.Value,
            ["s"] = statusId,
            ["status"] = statusId
        });
        return dt.Rows.Cast<DataRow>().Select(MapQuote).ToList();
    }

    public async Task<Quote?> GetQuoteAsync(int id)
    {
        var dt = await _db.QueryAsync(@"
            SELECT q.*,
                   ISNULL(d.QuotePrefix, 'QT-') + CAST(q.Qid AS NVARCHAR(20)) AS QuoteNum,
                   COALESCE(NULLIF(cco.Company, ''), '') AS CompanyName,
                   ISNULL(u.Name, '')             AS Originator,
                   ISNULL(qs.QuoteStatus, '')     AS QuoteStatusName
            FROM Quotes q
            LEFT JOIN Contacts    c  ON q.ContactId     = c.ContactId
            LEFT JOIN Companies   cco ON c.CompanyId    = cco.CompanyId
            LEFT JOIN Users       u  ON q.Code          = u.Code
            LEFT JOIN QuoteStatus qs ON q.QuoteStatusId = qs.QuoteStatusId
            LEFT JOIN Divisions   d  ON q.DivisionId    = d.DivisionId
            WHERE q.Qid = @id", new() { ["id"] = id });
        
        if (dt.Rows.Count == 0) return null;
        return MapQuote(dt.Rows[0]);
    }

    public async Task<List<QuoteLineItem>> GetLineItemsAsync(int qid)
    {
        var dt = await _db.QueryAsync("SELECT * FROM QuoteContents WHERE Qid = @q", new() { ["q"] = qid });
        return dt.Rows.Cast<DataRow>().Select(r => new QuoteLineItem
        {
            QuoteItemId = Convert.ToInt32(r["QuoteItemId"]),
            Qid = qid,
            Description = r["Description"]?.ToString() ?? "",
            Quantity = r.Table.Columns.Contains("Quantity") && r["Quantity"] != DBNull.Value ? Convert.ToDecimal(r["Quantity"]) : 0m,
            NettPrice = r["NettPrice"] != DBNull.Value ? Convert.ToDecimal(r["NettPrice"]) : 0m,
            ExtNettPrice = r.Table.Columns.Contains("ExtNettPrice") && r["ExtNettPrice"] != DBNull.Value ? Convert.ToDecimal(r["ExtNettPrice"]) : 0m,
            Type = r["Type"]?.ToString(),
            ProductCode = r["ProductCode"]?.ToString(),
            Units = r.Table.Columns.Contains("Units") && r["Units"] != DBNull.Value ? Convert.ToDecimal(r["Units"]) : 0m,
            Days = r.Table.Columns.Contains("Days") && r["Days"] != DBNull.Value ? Convert.ToDecimal(r["Days"]) : 0m,
            UnitCost = r["UnitCost"] != DBNull.Value ? Convert.ToDecimal(r["UnitCost"]) : 0m,
            MinNettPrice = r.Table.Columns.Contains("MinNettPrice") && r["MinNettPrice"] != DBNull.Value ? Convert.ToDecimal(r["MinNettPrice"]) : 0m
        }).ToList();
    }

    public async Task<List<QuoteThirdPartyItem>> GetThirdPartyItemsAsync(int qid)
    {
        var dt = await _db.QueryAsync("SELECT * FROM QuoteThirdPartyItems WHERE Qid = @q", new() { ["q"] = qid });
        return dt.Rows.Cast<DataRow>().Select(r => new QuoteThirdPartyItem
        {
            QuoteThirdPartyId = Convert.ToInt32(r["QuoteThirdPartyId"]),
            QuoteId = qid,
            Description = r["Description"]?.ToString() ?? "",
            Quantity = Convert.ToDecimal(r["Quantity"]),
            UnitCost = Convert.ToDecimal(r["UnitCost"]),
            NettPrice = Convert.ToDecimal(r["NettPrice"]),
            Supplier = r["Supplier"]?.ToString()
        }).ToList();
    }

    public async Task<List<QuoteAuditEntry>> GetAuditAsync(int qid)
    {
        var dt = await _db.QueryAsync("SELECT q.*, u.Name AS UserName FROM QuoteAudit q LEFT JOIN Users u ON q.Code = u.Code WHERE Qid = @q ORDER BY DateEntered DESC", new() { ["q"] = qid });
        return dt.Rows.Cast<DataRow>().Select(r => new QuoteAuditEntry
        {
            Qid = qid,
            UserName = r["UserName"]?.ToString() ?? r["Code"]?.ToString(),
            Action = r["Action"]?.ToString(),
            DateEntered = Convert.ToDateTime(r["DateEntered"])
        }).ToList();
    }

    public async Task<int> CreateQuoteAsync(Quote q, List<QuoteLineItem> items, List<QuoteThirdPartyItem> tpItems, string userCode)
    {
        var sql = @"
            INSERT INTO Quotes (Reference, ContactId, CompanyId, DivisionId, QuoteStatusId, QuoteDate, Code, 
                                Validity, Attention, Delivery, Terms, CustomerNotes, InternalNotes, 
                                UnitCostTotal, NettPriceTotal)
            VALUES (@Ref, @Cid, @Coid, @Did, 1, GETDATE(), @Code, 
                    @Val, @Att, @Del, @Terms, @CNotes, @INotes, 
                    @Cost, @Price);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        
        var qid = await _db.ScalarAsync<int>(sql, new() {
            ["Ref"] = q.Reference,
            ["Cid"] = q.ContactId,
            ["Coid"] = q.CompanyId,
            ["Did"] = q.DivisionId,
            ["Code"] = userCode,
            ["Val"] = q.Validity,
            ["Att"] = q.Attention,
            ["Del"] = q.Delivery,
            ["Terms"] = q.Terms,
            ["CNotes"] = q.CustomerNotes,
            ["INotes"] = q.InternalNotes,
            ["Cost"] = items.Sum(i => i.Quantity * i.UnitCost),
            ["Price"] = items.Sum(i => i.ExtNettPrice)
        });

        foreach (var item in items)
        {
            await _db.ExecuteAsync(@"
                INSERT INTO QuoteContents (Qid, Description, Quantity, NettPrice, UnitCost, ProductCode, Type, Units, Days, ExtNettPrice)
                VALUES (@qid, @desc, @qty, @price, @cost, @prod, @type, @u, @d, @ext)",
                new() {
                    ["qid"] = qid,
                    ["desc"] = item.Description,
                    ["qty"] = item.Quantity,
                    ["price"] = item.NettPrice,
                    ["cost"] = item.UnitCost,
                    ["prod"] = item.ProductCode,
                    ["type"] = item.Type,
                    ["u"] = item.Units,
                    ["d"] = item.Days,
                    ["ext"] = item.ExtNettPrice
                });
        }
        
        await AddAuditAsync(qid, userCode, "Quote Created");
        return qid;
    }

    public async Task UpdateQuoteAsync(Quote q, List<QuoteLineItem> items, List<QuoteThirdPartyItem> tpItems, string userCode)
    {
        await _db.ExecuteAsync(@"
            UPDATE Quotes SET
                Reference = @Ref, ContactId = @Cid, CompanyId = @Coid, DivisionId = @Did,
                Validity = @Val, Attention = @Att, Delivery = @Del, Terms = @Terms,
                CustomerNotes = @CNotes, InternalNotes = @INotes,
                UnitCostTotal = @Cost, NettPriceTotal = @Price
            WHERE Qid = @qid",
            new() {
                ["qid"] = q.Qid,
                ["Ref"] = q.Reference,
                ["Cid"] = q.ContactId,
                ["Coid"] = q.CompanyId,
                ["Did"] = q.DivisionId,
                ["Val"] = q.Validity,
                ["Att"] = q.Attention,
                ["Del"] = q.Delivery,
                ["Terms"] = q.Terms,
                ["CNotes"] = q.CustomerNotes,
                ["INotes"] = q.InternalNotes,
                ["Cost"] = items.Sum(i => i.Quantity * i.UnitCost),
                ["Price"] = items.Sum(i => i.ExtNettPrice)
            });

        await _db.ExecuteAsync("DELETE FROM QuoteContents WHERE Qid = @qid", new() { ["qid"] = q.Qid });
        foreach (var item in items)
        {
            await _db.ExecuteAsync(@"
                INSERT INTO QuoteContents (Qid, Description, Quantity, NettPrice, UnitCost, ProductCode, Type, Units, Days, ExtNettPrice)
                VALUES (@qid, @desc, @qty, @price, @cost, @prod, @type, @u, @d, @ext)",
                new() {
                    ["qid"] = q.Qid,
                    ["desc"] = item.Description,
                    ["qty"] = item.Quantity,
                    ["price"] = item.NettPrice,
                    ["cost"] = item.UnitCost,
                    ["prod"] = item.ProductCode,
                    ["type"] = item.Type,
                    ["u"] = item.Units,
                    ["d"] = item.Days,
                    ["ext"] = item.ExtNettPrice
                });
        }
        
        await AddAuditAsync(q.Qid, userCode, "Quote Updated");
    }

    public async Task<int> CopyQuoteAsync(int id, string userCode)
    {
        var old = await GetQuoteAsync(id);
        if (old == null) throw new Exception("Quote not found");
        var items = await GetLineItemsAsync(id);
        var tpItems = await GetThirdPartyItemsAsync(id);
        
        old.Reference = "Copy of " + old.Reference;
        old.QuoteDate = DateTime.Today;
        
        return await CreateQuoteAsync(old, items, tpItems, userCode);
    }

    public async Task ApproveAsync(int id, string userCode, bool isDirectorOrFinalApprover) 
        => await UpdateStatusAsync(id, 10, "Fully Approved", userCode);

    public async Task DeclineAsync(int id, string userCode) 
        => await UpdateStatusAsync(id, 11, "Declined", userCode);

    public async Task UpdateStatusAsync(int id, int statusId, string statusName, string userCode)
    {
        await _db.ExecuteAsync("UPDATE Quotes SET QuoteStatusId = @s WHERE Qid = @id", new() { ["s"] = statusId, ["id"] = id });
        await AddAuditAsync(id, userCode, $"Status changed to {statusName}");
    }

    public async Task DeleteQuoteAsync(int id)
    {
        await _db.ExecuteAsync("DELETE FROM QuoteContents WHERE Qid = @id", new() { ["id"] = id });
        await _db.ExecuteAsync("DELETE FROM QuoteThirdPartyItems WHERE Qid = @id", new() { ["id"] = id });
        await _db.ExecuteAsync("DELETE FROM QuoteAudit WHERE Qid = @id", new() { ["id"] = id });
        await _db.ExecuteAsync("DELETE FROM Quotes WHERE Qid = @id", new() { ["id"] = id });
    }

    private static Quote MapQuote(DataRow r)
    {
        bool HasCol(string n) => r.Table.Columns.Contains(n);
        return new Quote
        {
            Qid = Convert.ToInt32(r["Qid"]),
            Reference = r["Reference"]?.ToString() ?? "",
            CompanyName = r["CompanyName"]?.ToString() ?? "",
            QuoteStatus = HasCol("QuoteStatusName") ? (r["QuoteStatusName"]?.ToString() ?? "")
                         : (HasCol("QuoteStatus") ? r["QuoteStatus"]?.ToString() ?? "" : ""),
            UnitCostTotal = HasCol("UnitCostTotal") && r["UnitCostTotal"] != DBNull.Value ? Convert.ToDecimal(r["UnitCostTotal"]) : 0m,
            NettPriceTotal = HasCol("NettPriceTotal") && r["NettPriceTotal"] != DBNull.Value ? Convert.ToDecimal(r["NettPriceTotal"]) : 0m,
            QuoteDate = r["QuoteDate"] == DBNull.Value ? DateTime.Today : Convert.ToDateTime(r["QuoteDate"]),
            Originator = r["Originator"]?.ToString() ?? "",
            QuoteNumber = HasCol("QuoteNum") ? r["QuoteNum"]?.ToString() : (HasCol("QuoteNumber") ? r["QuoteNumber"]?.ToString() : $"Q{r["Qid"]}"),
            QuoteStatusId = HasCol("QuoteStatusId") && r["QuoteStatusId"] != DBNull.Value ? Convert.ToInt32(r["QuoteStatusId"]) : 0,
            ContactId = HasCol("ContactId") && r["ContactId"] != DBNull.Value ? Convert.ToInt32(r["ContactId"]) : 0,
            CompanyId = HasCol("CompanyId") && r["CompanyId"] != DBNull.Value ? Convert.ToInt32(r["CompanyId"]) : 0,
            DivisionId = HasCol("DivisionId") && r["DivisionId"] != DBNull.Value ? Convert.ToInt32(r["DivisionId"]) : 0,
            Attention = HasCol("Attention") && r["Attention"] != DBNull.Value ? r["Attention"]?.ToString() : "",
            Delivery = HasCol("Delivery") && r["Delivery"] != DBNull.Value ? r["Delivery"]?.ToString() : "",
            Validity = HasCol("Validity") && r["Validity"] != DBNull.Value ? Convert.ToInt32(r["Validity"]) : 30,
            Terms = HasCol("Terms") ? r["Terms"]?.ToString() : "",
            CustomerNotes = HasCol("CustomerNotes") ? r["CustomerNotes"]?.ToString() : "",
            InternalNotes = HasCol("InternalNotes") ? r["InternalNotes"]?.ToString() : ""
        };
    }

    private async Task AddAuditAsync(int qid, string code, string action)
    {
        await _db.ExecuteAsync(
            "INSERT INTO QuoteAudit (Qid, Code, Action, DateEntered) VALUES (@q, @c, @a, GETDATE())",
            new() { ["q"] = qid, ["c"] = code, ["a"] = action });

        await _activityService.LogAsync(code, "Quote", qid, action);
    }
}
