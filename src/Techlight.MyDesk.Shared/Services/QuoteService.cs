using System.Data;
using Microsoft.Extensions.Logging;
using Techlight.MyDesk.Shared.Models;

namespace Techlight.MyDesk.Shared.Services;

public class QuoteService
{
    private readonly DatabaseService _db;
    private readonly ILogger<QuoteService> _logger;

    public QuoteService(DatabaseService db, ILogger<QuoteService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ── Queries ────────────────────────────────────────────────────────────

    public async Task<List<Quote>> GetQuotesAsync(
        DateTime? dateFrom = null, DateTime? dateTo = null,
        string? customerName = null, int statusId = 555,
        string? keyword = null, string? originatorCode = null,
        int? divisionId = null, int limit = 500)
    {
        var sql = @"
            SELECT TOP (@Limit) q.Qid, ISNULL(q.Reference,'') AS Reference,
                   ISNULL(c.CompanyName,'') AS CompanyName,
                   ISNULL(qs.QuoteStatus,'') AS QuoteStatus,
                   ISNULL(q.UnitCostTotal,0) AS UnitCostTotal,
                   ISNULL(q.NettPriceTotal,0) AS NettPriceTotal,
                   ISNULL(q.Margin,0) AS Margin,
                   q.QuoteDate, ISNULL(u.Name,'') AS Originator,
                   ISNULL(q.ContactId,0) AS ContactId,
                   ISNULL(q.DivisionId,0) AS DivisionId,
                   ISNULL(q.Code,'') AS Code,
                   ISNULL(q.QuoteStatusId,0) AS QuoteStatusId,
                   q.Attention, q.Delivery, ISNULL(q.Validity,30) AS Validity,
                   q.QuoteNumber, q.CustomerNotes, q.InternalNotes, q.Terms
            FROM Quotes q
            LEFT JOIN Contacts c ON q.ContactId = c.ContactId
            LEFT JOIN QuoteStatus qs ON q.QuoteStatusId = qs.QuoteStatusId
            LEFT JOIN Users u ON q.Code = u.Code
            WHERE 1=1";

        var p = new Dictionary<string, object?> { ["Limit"] = limit };

        if (statusId == 555)
        {
            sql += " AND q.QuoteStatusId NOT IN (4,5)";
        }
        else if (statusId > 0)
        {
            sql += " AND q.QuoteStatusId = @StatusId";
            p["StatusId"] = statusId;
        }

        if (dateFrom.HasValue)     { sql += " AND q.QuoteDate >= @DateFrom"; p["DateFrom"] = dateFrom.Value; }
        if (dateTo.HasValue)       { sql += " AND q.QuoteDate <= @DateTo";   p["DateTo"]   = dateTo.Value; }
        if (!string.IsNullOrEmpty(customerName)) { sql += " AND c.CompanyName LIKE @Cust"; p["Cust"] = $"%{customerName}%"; }
        if (!string.IsNullOrEmpty(originatorCode)) { sql += " AND q.Code = @Code"; p["Code"] = originatorCode; }
        if (divisionId.HasValue)   { sql += " AND q.DivisionId = @DivId"; p["DivId"] = divisionId.Value; }
        if (!string.IsNullOrEmpty(keyword))
        {
            sql += @" AND (q.Reference LIKE @Kw OR q.InternalNotes LIKE @Kw OR q.CustomerNotes LIKE @Kw
                       OR CAST(q.Qid AS NVARCHAR) LIKE @Kw OR u.Name LIKE @Kw)";
            p["Kw"] = $"%{keyword}%";
        }

        sql += " ORDER BY q.Qid DESC";

        var dt = await _db.QueryAsync(sql, p);
        return dt.Map(MapQuote);
    }

    public async Task<Quote?> GetQuoteAsync(int qid)
    {
        var dt = await _db.QueryAsync(@"
            SELECT q.Qid, ISNULL(q.Reference,'') AS Reference,
                   ISNULL(c.CompanyName,'') AS CompanyName,
                   ISNULL(c.FirstName+' '+c.Surname,'') AS ContactName,
                   ISNULL(d.DivisionName,'') AS DivisionName,
                   ISNULL(qs.QuoteStatus,'') AS QuoteStatus,
                   ISNULL(q.UnitCostTotal,0) AS UnitCostTotal,
                   ISNULL(q.NettPriceTotal,0) AS NettPriceTotal,
                   ISNULL(q.Margin,0) AS Margin,
                   q.QuoteDate, ISNULL(u.Name,'') AS Originator,
                   q.CustomerNotes, q.InternalNotes, q.Terms,
                   ISNULL(q.ContactId,0) AS ContactId,
                   ISNULL(q.DivisionId,0) AS DivisionId,
                   ISNULL(q.Code,'') AS Code,
                   ISNULL(q.QuoteStatusId,0) AS QuoteStatusId,
                   q.Attention, q.Delivery, ISNULL(q.Validity,30) AS Validity,
                   q.QuoteNumber, q.SenderCode
            FROM Quotes q
            LEFT JOIN Contacts c ON q.ContactId = c.ContactId
            LEFT JOIN Divisions d ON q.DivisionId = d.DivisionId
            LEFT JOIN QuoteStatus qs ON q.QuoteStatusId = qs.QuoteStatusId
            LEFT JOIN Users u ON q.Code = u.Code
            WHERE q.Qid = @Qid", new() { ["Qid"] = qid });

        return dt.Rows.Count == 0 ? null : MapQuote(dt.Rows[0]);
    }

    public async Task<List<QuoteLineItem>> GetLineItemsAsync(int qid)
    {
        var dt = await _db.QueryAsync(@"
            SELECT QuoteItemId, Qid,
                   ISNULL(ProductCode,'') AS ProductCode,
                   ISNULL(Description,'') AS Description,
                   ISNULL(Type,'Supply') AS Type,
                   ISNULL(Quantity,0) AS Quantity,
                   ISNULL(Units,0) AS Units,
                   ISNULL(Days,0) AS Days,
                   ISNULL(UnitCost,0) AS UnitCost,
                   ISNULL(MinNettPrice,0) AS MinNettPrice,
                   ISNULL(NettPrice,0) AS NettPrice,
                   ISNULL(UnitCostSubTotal,0) AS UnitCostSubTotal,
                   ISNULL(ExtNettPrice,0) AS ExtNettPrice
            FROM QuoteContents WHERE Qid = @Qid ORDER BY QuoteItemId",
            new() { ["Qid"] = qid });

        return dt.Map(r => new QuoteLineItem
        {
            QuoteItemId     = Convert.ToInt32(r["QuoteItemId"]),
            Qid             = Convert.ToInt32(r["Qid"]),
            ProductCode     = r["ProductCode"]?.ToString(),
            Type            = r["Type"]?.ToString(),
            Description     = r["Description"]?.ToString() ?? "",
            Quantity        = Convert.ToDecimal(r["Quantity"]),
            Units           = Convert.ToDecimal(r["Units"]),
            Days            = Convert.ToDecimal(r["Days"]),
            UnitCost        = Convert.ToDecimal(r["UnitCost"]),
            MinNettPrice    = Convert.ToDecimal(r["MinNettPrice"]),
            NettPrice       = Convert.ToDecimal(r["NettPrice"]),
            UnitCostSubTotal= Convert.ToDecimal(r["UnitCostSubTotal"]),
            ExtNettPrice    = Convert.ToDecimal(r["ExtNettPrice"]),
        });
    }

    public async Task<List<QuoteThirdPartyItem>> GetThirdPartyItemsAsync(int qid)
    {
        var dt = await _db.QueryAsync(@"
            SELECT QuoteThirdPartyId, QuoteId,
                   ISNULL(Description,'') AS Description,
                   ISNULL(Supplier,'') AS Supplier,
                   ISNULL(SupplierPartNumber,'') AS SupplierPartNumber,
                   ISNULL(OurPartNumber,'') AS OurPartNumber,
                   ISNULL(Quantity,0) AS Quantity,
                   ISNULL(Type,'') AS Type,
                   ISNULL(UnitCost,0) AS UnitCost,
                   ISNULL(NettPrice,0) AS NettPrice,
                   ISNULL(ExtNettPrice,0) AS ExtNettPrice,
                   ISNULL(TotalCost,0) AS TotalCost
            FROM QuoteThirdPartyContents WHERE QuoteId = @Qid ORDER BY QuoteThirdPartyId",
            new() { ["Qid"] = qid });

        return dt.Map(r => new QuoteThirdPartyItem
        {
            QuoteThirdPartyId   = Convert.ToInt32(r["QuoteThirdPartyId"]),
            QuoteId             = Convert.ToInt32(r["QuoteId"]),
            Description         = r["Description"]?.ToString(),
            Supplier            = r["Supplier"]?.ToString(),
            SupplierPartNumber  = r["SupplierPartNumber"]?.ToString(),
            OurPartNumber       = r["OurPartNumber"]?.ToString(),
            Quantity            = Convert.ToDecimal(r["Quantity"]),
            Type                = r["Type"]?.ToString(),
            UnitCost            = Convert.ToDecimal(r["UnitCost"]),
            NettPrice           = Convert.ToDecimal(r["NettPrice"]),
            ExtNettPrice        = Convert.ToDecimal(r["ExtNettPrice"]),
            TotalCost           = Convert.ToDecimal(r["TotalCost"]),
        });
    }

    public async Task<List<QuoteAuditEntry>> GetAuditAsync(int qid)
    {
        var dt = await _db.QueryAsync(@"
            SELECT qa.Qid, qa.Code, ISNULL(u.Name,'') AS UserName,
                   ISNULL(qa.Action,'') AS Action, qa.DateEntered
            FROM QuoteAudit qa
            LEFT JOIN Users u ON u.Code = qa.Code
            WHERE qa.Qid = @Qid ORDER BY qa.DateEntered DESC",
            new() { ["Qid"] = qid });

        return dt.Map(r => new QuoteAuditEntry
        {
            Qid         = Convert.ToInt32(r["Qid"]),
            Code        = r["Code"]?.ToString(),
            UserName    = r["UserName"]?.ToString(),
            Action      = r["Action"]?.ToString(),
            DateEntered = r["DateEntered"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["DateEntered"]),
        });
    }

    // ── Writes ─────────────────────────────────────────────────────────────

    public async Task<int> CreateQuoteAsync(
        Quote quote, List<QuoteLineItem> items,
        List<QuoteThirdPartyItem> tpItems, string code)
    {
        var qid = await _db.InsertAsync(@"
            INSERT INTO Quotes (QuoteDate, Code, ContactId, DivisionId, QuoteStatusId,
                QuoteNumber, Reference, Terms, Delivery, Validity,
                Attention, InternalNotes, CustomerNotes,
                UnitCostTotal, NettPriceTotal, Margin)
            VALUES (GETDATE(), @Code, @ContactId, @DivisionId, 1,
                @QuoteNumber, @Reference, @Terms, @Delivery, @Validity,
                @Attention, @InternalNotes, @CustomerNotes, 0, 0, 0)",
            new()
            {
                ["Code"]          = code,
                ["ContactId"]     = quote.ContactId,
                ["DivisionId"]    = quote.DivisionId > 0 ? quote.DivisionId : 1,
                ["QuoteNumber"]   = (object?)quote.QuoteNumber   ?? DBNull.Value,
                ["Reference"]     = (object?)quote.Reference     ?? DBNull.Value,
                ["Terms"]         = (object?)quote.Terms         ?? "F.I.S. via general road freight",
                ["Delivery"]      = (object?)quote.Delivery      ?? DBNull.Value,
                ["Validity"]      = quote.Validity > 0 ? quote.Validity : 30,
                ["Attention"]     = (object?)quote.Attention     ?? DBNull.Value,
                ["InternalNotes"] = (object?)quote.InternalNotes ?? DBNull.Value,
                ["CustomerNotes"] = (object?)quote.CustomerNotes ?? DBNull.Value,
            });

        await SaveLineItemsAsync(qid, items);
        await SaveThirdPartyItemsAsync(qid, tpItems);
        await RecalcTotalsAsync(qid);
        await AddAuditAsync(qid, code, "Created");

        _logger.LogInformation("Created Quote #{Qid} for {Code}", qid, code);
        return qid;
    }

    public async Task UpdateQuoteAsync(
        Quote quote, List<QuoteLineItem> items,
        List<QuoteThirdPartyItem> tpItems)
    {
        await _db.ExecuteAsync(@"
            UPDATE Quotes SET
                ContactId     = @ContactId,
                DivisionId    = @DivisionId,
                QuoteStatusId = @StatusId,
                QuoteNumber   = @QuoteNumber,
                Reference     = @Reference,
                Terms         = @Terms,
                Delivery      = @Delivery,
                Validity      = @Validity,
                Attention     = @Attention,
                InternalNotes = @InternalNotes,
                CustomerNotes = @CustomerNotes
            WHERE Qid = @Qid",
            new()
            {
                ["Qid"]          = quote.Qid,
                ["ContactId"]    = quote.ContactId,
                ["DivisionId"]   = quote.DivisionId > 0 ? quote.DivisionId : 1,
                ["StatusId"]     = quote.QuoteStatusId > 0 ? quote.QuoteStatusId : 1,
                ["QuoteNumber"]  = (object?)quote.QuoteNumber   ?? DBNull.Value,
                ["Reference"]    = (object?)quote.Reference     ?? DBNull.Value,
                ["Terms"]        = (object?)quote.Terms         ?? DBNull.Value,
                ["Delivery"]     = (object?)quote.Delivery      ?? DBNull.Value,
                ["Validity"]     = quote.Validity > 0 ? quote.Validity : 30,
                ["Attention"]    = (object?)quote.Attention     ?? DBNull.Value,
                ["InternalNotes"]= (object?)quote.InternalNotes ?? DBNull.Value,
                ["CustomerNotes"]= (object?)quote.CustomerNotes ?? DBNull.Value,
            });

        await _db.ExecuteAsync("DELETE FROM QuoteApproval WHERE Qid = @q", new() { ["q"] = quote.Qid });
        await SaveLineItemsAsync(quote.Qid, items);
        await SaveThirdPartyItemsAsync(quote.Qid, tpItems);
        await RecalcTotalsAsync(quote.Qid);
        await AddAuditAsync(quote.Qid, quote.Code, "Updated");

        _logger.LogInformation("Updated Quote #{Qid}", quote.Qid);
    }

    public async Task<int> CopyQuoteAsync(int srcQid, string byCode)
    {
        var src = await GetQuoteAsync(srcQid);
        if (src == null) throw new InvalidOperationException($"Quote #{srcQid} not found");
        var srcItems = await GetLineItemsAsync(srcQid);

        var newQid = await _db.InsertAsync(@"
            INSERT INTO Quotes (QuoteDate, Code, ContactId, DivisionId, QuoteStatusId,
                QuoteNumber, Reference, Terms, Delivery, Validity, Attention,
                InternalNotes, CustomerNotes, UnitCostTotal, NettPriceTotal, Margin)
            VALUES (GETDATE(), @Code, @ContactId, @DivisionId, @StatusId,
                @QuoteNumber, @Reference, @Terms, @Delivery, @Validity, @Attention,
                @InternalNotes, @CustomerNotes, @Cost, @Price, @Margin)",
            new()
            {
                ["Code"]          = src.Code,
                ["ContactId"]     = src.ContactId,
                ["DivisionId"]    = src.DivisionId,
                ["StatusId"]      = src.QuoteStatusId,
                ["QuoteNumber"]   = (object?)src.QuoteNumber   ?? DBNull.Value,
                ["Reference"]     = $"COPY of {src.Reference}",
                ["Terms"]         = (object?)src.Terms         ?? DBNull.Value,
                ["Delivery"]      = (object?)src.Delivery      ?? DBNull.Value,
                ["Validity"]      = src.Validity,
                ["Attention"]     = (object?)src.Attention     ?? DBNull.Value,
                ["InternalNotes"] = (object?)src.InternalNotes ?? DBNull.Value,
                ["CustomerNotes"] = (object?)src.CustomerNotes ?? DBNull.Value,
                ["Cost"]          = src.UnitCostTotal,
                ["Price"]         = src.NettPriceTotal,
                ["Margin"]        = src.Margin,
            });

        await SaveLineItemsAsync(newQid, srcItems);
        await AddAuditAsync(newQid, byCode, $"Copied from #{srcQid}");

        _logger.LogInformation("Copied Quote #{Src} -> #{New} by {Code}", srcQid, newQid, byCode);
        return newQid;
    }

    public async Task DeleteQuoteAsync(int qid)
    {
        await _db.ExecuteAsync("DELETE FROM QuoteThirdPartyContents WHERE QuoteId = @q", new() { ["q"] = qid });
        await _db.ExecuteAsync("DELETE FROM QuoteContents WHERE Qid = @q",               new() { ["q"] = qid });
        await _db.ExecuteAsync("DELETE FROM QuoteApproval WHERE Qid = @q",               new() { ["q"] = qid });
        await _db.ExecuteAsync("DELETE FROM QuoteAudit WHERE Qid = @q",                  new() { ["q"] = qid });
        await _db.ExecuteAsync("DELETE FROM Quotes WHERE Qid = @q",                      new() { ["q"] = qid });
        _logger.LogInformation("Deleted Quote #{Qid}", qid);
    }

    public async Task ApproveAsync(int qid, string code, bool isDirectorOrFinalApprover)
    {
        await _db.ExecuteAsync("UPDATE Quotes SET QuoteStatusId = 9 WHERE Qid = @q", new() { ["q"] = qid });
        await _db.ExecuteAsync(
            "INSERT INTO QuoteApproval (Qid, Code) VALUES (@q, @c)",
            new() { ["q"] = qid, ["c"] = code });

        if (isDirectorOrFinalApprover)
        {
            await _db.ExecuteAsync("UPDATE Quotes SET QuoteStatusId = 10 WHERE Qid = @q", new() { ["q"] = qid });
            await AddAuditAsync(qid, code, "Approved — fully approved");
            _logger.LogInformation("Quote #{Qid} fully approved by {Code}", qid, code);
        }
        else
        {
            await AddAuditAsync(qid, code, "Approved");
            _logger.LogInformation("Quote #{Qid} approved (pending chain) by {Code}", qid, code);
        }
    }

    public async Task DeclineAsync(int qid, string code)
    {
        await _db.ExecuteAsync(
            "INSERT INTO QuoteApproval (Qid, Code) VALUES (@q, @c)",
            new() { ["q"] = qid, ["c"] = code });
        await _db.ExecuteAsync("UPDATE Quotes SET QuoteStatusId = 11 WHERE Qid = @q", new() { ["q"] = qid });
        await AddAuditAsync(qid, code, "Declined");
        _logger.LogInformation("Quote #{Qid} declined by {Code}", qid, code);
    }

    public async Task UpdateStatusAsync(int qid, int newStatusId, string statusName, string code)
    {
        await _db.ExecuteAsync(
            "UPDATE Quotes SET QuoteStatusId = @s WHERE Qid = @q",
            new() { ["q"] = qid, ["s"] = newStatusId });
        await AddAuditAsync(qid, code, $"Status changed to {statusName}");
        _logger.LogInformation("Quote #{Qid} status → {Status} by {Code}", qid, statusName, code);
    }

    // ── Private helpers ────────────────────────────────────────────────────

    private async Task SaveLineItemsAsync(int qid, List<QuoteLineItem> items)
    {
        await _db.ExecuteAsync("DELETE FROM QuoteContents WHERE Qid = @q", new() { ["q"] = qid });
        foreach (var i in items)
        {
            if (i.Quantity <= 0 && !(i.Units > 0 && i.Days > 0)) continue;
            var effQty = i.Units > 0 && i.Days > 0 ? i.Units * i.Days : i.Quantity;
            var costSub  = i.UnitCost  * effQty;
            var nettSub  = i.NettPrice * effQty;
            await _db.ExecuteAsync(@"
                INSERT INTO QuoteContents
                    (Qid, ProductCode, Description, Type, Quantity, Units, Days,
                     UnitCost, MinNettPrice, NettPrice, UnitCostSubTotal, ExtNettPrice)
                VALUES (@Qid, @Pc, @Desc, @Type, @Qty, @Units, @Days,
                        @Cost, @MinNett, @Nett, @CostSub, @NettSub)",
                new()
                {
                    ["Qid"]     = qid,
                    ["Pc"]      = (object?)i.ProductCode ?? DBNull.Value,
                    ["Desc"]    = i.Description,
                    ["Type"]    = (object?)i.Type ?? "Supply",
                    ["Qty"]     = i.Quantity,
                    ["Units"]   = i.Units,
                    ["Days"]    = i.Days,
                    ["Cost"]    = i.UnitCost,
                    ["MinNett"] = i.MinNettPrice,
                    ["Nett"]    = i.NettPrice,
                    ["CostSub"] = costSub,
                    ["NettSub"] = nettSub,
                });
        }
    }

    private async Task SaveThirdPartyItemsAsync(int qid, List<QuoteThirdPartyItem> items)
    {
        await _db.ExecuteAsync("DELETE FROM QuoteThirdPartyContents WHERE QuoteId = @q", new() { ["q"] = qid });
        foreach (var i in items)
        {
            if (i.Quantity <= 0) continue;
            await _db.ExecuteAsync(@"
                INSERT INTO QuoteThirdPartyContents
                    (QuoteId, Description, Supplier, SupplierPartNumber, OurPartNumber,
                     Quantity, Type, UnitCost, NettPrice, ExtNettPrice, TotalCost)
                VALUES (@Qid, @Desc, @Sup, @SpN, @OpN,
                        @Qty, @Type, @Cost, @Nett, @ExtNett, @TotCost)",
                new()
                {
                    ["Qid"]      = qid,
                    ["Desc"]     = (object?)i.Description         ?? DBNull.Value,
                    ["Sup"]      = (object?)i.Supplier            ?? DBNull.Value,
                    ["SpN"]      = (object?)i.SupplierPartNumber   ?? DBNull.Value,
                    ["OpN"]      = (object?)i.OurPartNumber        ?? DBNull.Value,
                    ["Qty"]      = i.Quantity,
                    ["Type"]     = (object?)i.Type ?? DBNull.Value,
                    ["Cost"]     = i.UnitCost,
                    ["Nett"]     = i.NettPrice,
                    ["ExtNett"]  = i.NettPrice * i.Quantity,
                    ["TotCost"]  = i.UnitCost  * i.Quantity,
                });
        }
    }

    private async Task RecalcTotalsAsync(int qid)
    {
        await _db.ExecuteAsync(@"
            DECLARE @Cost  DECIMAL(18,4) =
                (SELECT ISNULL(SUM(UnitCostSubTotal),0) FROM QuoteContents WHERE Qid = @q) +
                (SELECT ISNULL(SUM(TotalCost),0)        FROM QuoteThirdPartyContents WHERE QuoteId = @q);
            DECLARE @Price DECIMAL(18,4) =
                (SELECT ISNULL(SUM(ExtNettPrice),0) FROM QuoteContents WHERE Qid = @q) +
                (SELECT ISNULL(SUM(ExtNettPrice),0) FROM QuoteThirdPartyContents WHERE QuoteId = @q);
            UPDATE Quotes SET
                UnitCostTotal  = @Cost,
                NettPriceTotal = @Price,
                Margin = CASE WHEN @Price > 0 THEN (@Price - @Cost) / @Price * 100 ELSE 0 END
            WHERE Qid = @q",
            new() { ["q"] = qid });
    }

    private async Task AddAuditAsync(int qid, string code, string action)
    {
        await _db.ExecuteAsync(
            "INSERT INTO QuoteAudit (Qid, Code, Action, DateEntered) VALUES (@q, @c, @a, GETDATE())",
            new() { ["q"] = qid, ["c"] = code, ["a"] = action });
    }

    private static Quote MapQuote(DataRow r)
    {
        bool HasCol(string n) => r.Table.Columns.Contains(n);
        T? Opt<T>(string n) where T : class => HasCol(n) && r[n] != DBNull.Value ? (T?)r[n] : null;

        return new Quote
        {
            Qid            = Convert.ToInt32(r["Qid"]),
            Reference      = r["Reference"]?.ToString() ?? "",
            CompanyName    = r["CompanyName"]?.ToString() ?? "",
            ContactName    = HasCol("ContactName") ? r["ContactName"]?.ToString() : null,
            DivisionName   = HasCol("DivisionName") ? r["DivisionName"]?.ToString() : null,
            QuoteStatus    = r["QuoteStatus"]?.ToString() ?? "",
            UnitCostTotal  = Convert.ToDecimal(r["UnitCostTotal"]),
            NettPriceTotal = Convert.ToDecimal(r["NettPriceTotal"]),
            Margin         = Convert.ToDecimal(r["Margin"]),
            QuoteDate      = r["QuoteDate"] == DBNull.Value ? DateTime.Today : Convert.ToDateTime(r["QuoteDate"]),
            Originator     = r["Originator"]?.ToString() ?? "",
            CustomerNotes  = HasCol("CustomerNotes") ? r["CustomerNotes"]?.ToString() : null,
            InternalNotes  = HasCol("InternalNotes") ? r["InternalNotes"]?.ToString() : null,
            Terms          = HasCol("Terms") ? r["Terms"]?.ToString() : null,
            ContactId      = Convert.ToInt32(r["ContactId"]),
            DivisionId     = Convert.ToInt32(r["DivisionId"]),
            Code           = r["Code"]?.ToString() ?? "",
            QuoteStatusId  = Convert.ToInt32(r["QuoteStatusId"]),
            Attention      = HasCol("Attention") ? r["Attention"]?.ToString() : null,
            Delivery       = HasCol("Delivery")  ? r["Delivery"]?.ToString()  : null,
            Validity       = HasCol("Validity")  && r["Validity"] != DBNull.Value ? Convert.ToInt32(r["Validity"]) : 30,
            QuoteNumber    = HasCol("QuoteNumber") ? r["QuoteNumber"]?.ToString() : null,
            SenderCode     = HasCol("SenderCode")  ? r["SenderCode"]?.ToString()  : null,
        };
    }
}
