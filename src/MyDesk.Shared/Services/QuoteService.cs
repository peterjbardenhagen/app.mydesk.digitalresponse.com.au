using System.Data;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class QuoteService
{
    private readonly DatabaseService _db;

    public QuoteService(DatabaseService db)
    {
        _db = db;
    }

    public async Task<List<Quote>> GetQuotesAsync(DateTime? dateFrom, DateTime? dateTo, string? customerName, int statusId, string? keyword)
    {
        // Legacy Access-migrated schema:
        //   Contacts.CCompany was dropped; company name lives on Companies.Company (joined via CompanyId).
        //   Quotes has no 'Deleted' flag — soft-delete is not modelled in the migrated schema.
        //   QuoteStatus.QuoteStatus provides the human-readable status label.
        var sql = @"
            SELECT q.*,
                   ISNULL(co.Company, '')         AS CompanyName,
                   ISNULL(u.Name, '')             AS Originator,
                   ISNULL(qs.QuoteStatus, '')     AS QuoteStatusName
            FROM Quotes q
            LEFT JOIN Contacts    c  ON q.ContactId     = c.ContactId
            LEFT JOIN Companies   co ON c.CompanyId     = co.CompanyId
            LEFT JOIN Users       u  ON q.Code          = u.Code
            LEFT JOIN QuoteStatus qs ON q.QuoteStatusId = qs.QuoteStatusId
            WHERE (@c IS NULL OR co.Company LIKE '%' + @c + '%')
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
                   ISNULL(co.Company, '')         AS CompanyName,
                   ISNULL(u.Name, '')             AS Originator,
                   ISNULL(qs.QuoteStatus, '')     AS QuoteStatusName
            FROM Quotes q
            LEFT JOIN Contacts    c  ON q.ContactId     = c.ContactId
            LEFT JOIN Companies   co ON c.CompanyId     = co.CompanyId
            LEFT JOIN Users       u  ON q.Code          = u.Code
            LEFT JOIN QuoteStatus qs ON q.QuoteStatusId = qs.QuoteStatusId
            WHERE q.Qid = @id", new() { ["id"] = id });
        
        if (dt.Rows.Count == 0) return null;
        return MapQuote(dt.Rows[0]);
    }

    public async Task<List<QuoteLineItem>> GetLineItemsAsync(int qid)
    {
        var dt = await _db.QueryAsync("SELECT * FROM QuoteItems WHERE Qid = @q AND (Deleted IS NULL OR Deleted = 0)", new() { ["q"] = qid });
        return dt.Rows.Cast<DataRow>().Select(r => new QuoteLineItem
        {
            QuoteItemId = Convert.ToInt32(r["QuoteItemId"]),
            Qid = qid,
            Description = r["Description"]?.ToString() ?? "",
            Quantity = Convert.ToDecimal(r[r.Table.Columns.Contains("Quantity") ? "Quantity" : "Qty"]),
            NettPrice = Convert.ToDecimal(r["NettPrice"]),
            ExtNettPrice = Convert.ToDecimal(r.Table.Columns.Contains("ExtNettPrice") ? r["ExtNettPrice"] : 0),
            Type = r["Type"]?.ToString(),
            ProductCode = r["ProductCode"]?.ToString(),
            Units = Convert.ToDecimal(r.Table.Columns.Contains("Units") ? r["Units"] : 0),
            Days = Convert.ToDecimal(r.Table.Columns.Contains("Days") ? r["Days"] : 0),
            UnitCost = Convert.ToDecimal(r["UnitCost"]),
            MinNettPrice = Convert.ToDecimal(r.Table.Columns.Contains("MinNettPrice") ? r["MinNettPrice"] : 0)
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
        // Placeholder implementation
        return 0;
    }

    public async Task UpdateQuoteAsync(Quote q, List<QuoteLineItem> items, List<QuoteThirdPartyItem> tpItems)
    {
        // Placeholder implementation
    }

    public async Task<int> CopyQuoteAsync(int id, string userCode)
    {
        // Placeholder implementation
        return 0;
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
        // Legacy schema has no 'Deleted' flag on Quotes; do a cascade delete of dependents then Quote.
        // Audit trail is preserved by UpdateStatusAsync calls elsewhere.
        await _db.ExecuteAsync("DELETE FROM QuoteItems WHERE Qid = @id", new() { ["id"] = id });
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
            QuoteNumber = HasCol("QuoteNumber") ? r["QuoteNumber"]?.ToString() : $"Q{r["Qid"]}",
            QuoteStatusId = HasCol("QuoteStatusId") ? Convert.ToInt32(r["QuoteStatusId"]) : 0,
            ContactId = HasCol("ContactId") ? Convert.ToInt32(r["ContactId"]) : 0,
            DivisionId = HasCol("DivisionId") ? Convert.ToInt32(r["DivisionId"]) : 0,
            Attention = HasCol("Attention") ? r["Attention"]?.ToString() : "",
            Delivery = HasCol("Delivery") ? r["Delivery"]?.ToString() : "",
            Validity = HasCol("Validity") ? Convert.ToInt32(r["Validity"]) : 30,
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
    }
}
