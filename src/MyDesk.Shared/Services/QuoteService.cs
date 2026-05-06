using System.Data;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class QuoteService
{
    private readonly DatabaseService _db;
    private readonly ActivityService _activityService;
    private readonly ApprovalService? _approvals;

    public QuoteService(DatabaseService db, ActivityService activityService, ApprovalService? approvals = null)
    {
        _db = db;
        _activityService = activityService;
        _approvals = approvals;
    }

    public async Task<List<Quote>> GetQuotesAsync(DateTime? dateFrom, DateTime? dateTo, string? customerName, int? contactId, int statusId, string? keyword)
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
              AND (@contact IS NULL OR q.ContactId = @contact)
              AND (@k IS NULL OR q.Reference LIKE '%' + @k + '%' OR q.CustomerNotes LIKE '%' + @k + '%')
              AND (@f IS NULL OR q.QuoteDate >= @f)
              AND (@t IS NULL OR q.QuoteDate <= @t)
              AND (@s = 0 OR q.QuoteStatusId = @status OR (@s = 555 AND q.QuoteStatusId NOT IN (4, 5, 9, 10)))
            ORDER BY q.Qid DESC";
        
        var dt = await _db.QueryAsync(sql, new() { 
            ["c"] = (object?)customerName ?? DBNull.Value,
            ["contact"] = (object?)contactId ?? DBNull.Value,
            ["k"] = (object?)keyword ?? DBNull.Value,
            ["f"] = (object?)dateFrom ?? DBNull.Value,
            ["t"] = (object?)dateTo ?? DBNull.Value,
            ["s"] = statusId,
            ["status"] = statusId
        });
        return dt.Rows.Cast<DataRow>().Select(MapQuote).ToList();
    }

    public async Task<List<Contact>> GetContactsWithQuotesAsync()
    {
        var sql = @"
            SELECT c.ContactId, c.FirstName, c.Surname, co.Company AS CompanyName
            FROM Quotes q
            INNER JOIN Contacts c ON q.ContactId = c.ContactId
            LEFT JOIN Companies co ON c.CompanyId = co.CompanyId
            GROUP BY c.ContactId, c.FirstName, c.Surname, co.Company
            ORDER BY co.Company, c.Surname, c.FirstName";
        
        var dt = await _db.QueryAsync(sql);
        return dt.Rows.Cast<DataRow>().Select(r => new Contact
        {
            ContactId = Convert.ToInt32(r["ContactId"]),
            FirstName = r["FirstName"]?.ToString() ?? "",
            Surname = r["Surname"]?.ToString() ?? "",
            CompanyName = r["CompanyName"]?.ToString() ?? ""
        }).ToList();
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
            await _db.ExecuteNonQueryAsync(@"
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
        await _db.ExecuteNonQueryAsync(@"
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

        await _db.ExecuteNonQueryAsync("DELETE FROM QuoteContents WHERE Qid = @qid", new() { ["qid"] = q.Qid });
        foreach (var item in items)
        {
            await _db.ExecuteNonQueryAsync(@"
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

    /// <summary>
    /// Records an approval at the current user's level in the chain. When the chain is
    /// complete (no more line managers above), the quote moves to "Manager Approved" (status 8).
    /// Otherwise it moves to "Pending Manager Approval" (status 7) so the next manager can sign off.
    /// </summary>
    public async Task ApproveAsync(int id, string userCode, bool isDirectorOrFinalApprover)
    {
        if (_approvals != null)
        {
            var level = _approvals.CompletedLevels("Quote", id) + 1;
            _approvals.RecordApproval("Quote", id, level, userCode, null, "Approved");

            var fullyApproved = isDirectorOrFinalApprover ||
                                await IsQuoteFullyApprovedAsync(id);
            if (fullyApproved)
            {
                await UpdateStatusAsync(id, 8, "Manager Approved", userCode);
            }
            else
            {
                await UpdateStatusAsync(id, 7, "Pending Manager Approval", userCode);
            }
            return;
        }

        // Fallback: legacy behaviour (no chain)
        await UpdateStatusAsync(id, 8, "Manager Approved", userCode);
    }

    public async Task DeclineAsync(int id, string userCode)
    {
        if (_approvals != null)
        {
            var level = _approvals.CompletedLevels("Quote", id) + 1;
            _approvals.RecordApproval("Quote", id, level, userCode, null, "Declined");
        }
        await UpdateStatusAsync(id, 9, "Manager Declined", userCode);
    }

    /// <summary>
    /// Walks the LineManagerCode chain starting from <paramref name="currentUserCode"/>,
    /// skipping any users who have already signed off this quote, and returns the next
    /// approver's UserCode (or null if the chain is complete).
    /// </summary>
    public Task<string?> GetNextQuoteApproverAsync(int quoteId, string currentUserCode)
    {
        if (_approvals == null) return Task.FromResult<string?>(null);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var cursor  = currentUserCode;
        while (!string.IsNullOrEmpty(cursor) && visited.Add(cursor))
        {
            var next = _approvals.NextLineManager(cursor);
            if (string.IsNullOrEmpty(next)) return Task.FromResult<string?>(null);
            if (!_approvals.HasApproval("Quote", quoteId, next))
                return Task.FromResult<string?>(next);
            cursor = next;
        }
        return Task.FromResult<string?>(null);
    }

    /// <summary>
    /// True when every required approval level has signed off — i.e. there is no further
    /// line manager above the most-recent approver who has not yet approved.
    /// </summary>
    public Task<bool> IsQuoteFullyApprovedAsync(int quoteId)
    {
        if (_approvals == null) return Task.FromResult(false);
        var last = _approvals.LastApproval("Quote", quoteId);
        if (last == null) return Task.FromResult(false);
        var next = _approvals.NextLineManager(last.ApproverCode);
        if (string.IsNullOrEmpty(next)) return Task.FromResult(true);
        return Task.FromResult(_approvals.HasApproval("Quote", quoteId, next));
    }

    public List<ApprovalEntry> GetApprovalHistory(int quoteId) =>
        _approvals?.GetEntries("Quote", quoteId) ?? new List<ApprovalEntry>();

    /// <summary>Returns quotes that are awaiting approval (status = 9).</summary>
    public async Task<List<Quote>> GetPendingApprovalQuotesAsync()
    {
        return await GetQuotesAsync(null, null, null, null, statusId: 9, keyword: null);
    }

    public async Task UpdateStatusAsync(int id, int statusId, string statusName, string userCode)
    {
        await _db.ExecuteNonQueryAsync("UPDATE Quotes SET QuoteStatusId = @s WHERE Qid = @id", new() { ["s"] = statusId, ["id"] = id });
        await AddAuditAsync(id, userCode, $"Status changed to {statusName}");
    }

    public async Task DeleteQuoteAsync(int id)
    {
        await _db.ExecuteNonQueryAsync("DELETE FROM QuoteContents WHERE Qid = @id", new() { ["id"] = id });
        await _db.ExecuteNonQueryAsync("DELETE FROM QuoteThirdPartyItems WHERE Qid = @id", new() { ["id"] = id });
        await _db.ExecuteNonQueryAsync("DELETE FROM QuoteAudit WHERE Qid = @id", new() { ["id"] = id });
        await _db.ExecuteNonQueryAsync("DELETE FROM Quotes WHERE Qid = @id", new() { ["id"] = id });
    }

    private static Quote MapQuote(DataRow r)
    {
        bool HasCol(string n) => r.Table.Columns.Contains(n);
        var quote = new Quote
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

        quote.Margin = CalculateMargin(quote.UnitCostTotal, quote.NettPriceTotal);
        return quote;
    }

    private static decimal CalculateMargin(decimal costTotal, decimal nettPriceTotal) =>
        nettPriceTotal > 0 ? ((nettPriceTotal - costTotal) / nettPriceTotal) * 100m : 0m;

    private async Task AddAuditAsync(int qid, string code, string action)
    {
        await _db.ExecuteNonQueryAsync(
            "INSERT INTO QuoteAudit (Qid, Code, Action, DateEntered) VALUES (@q, @c, @a, GETDATE())",
            new() { ["q"] = qid, ["c"] = code, ["a"] = action });

        await _activityService.LogAsync(code, "Quote", qid, action);
    }
}
