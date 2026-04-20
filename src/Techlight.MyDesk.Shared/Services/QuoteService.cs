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

    public async Task<List<Quote>> GetQuotesAsync(
        DateTime? dateFrom = null, DateTime? dateTo = null,
        string? customerName = null, string? status = null,
        string? originatorCode = null, int limit = 500)
    {
        var sql = @"
            SELECT TOP (@Limit) q.Qid, ISNULL(q.Reference, '') AS Reference,
                   ISNULL(c.CompanyName, '') AS CompanyName,
                   ISNULL(q.Reference, '') AS Project,
                   ISNULL(qs.QuoteStatus, '') AS QuoteStatus,
                   ISNULL(q.UnitCostTotal, 0) AS UnitCostTotal,
                   ISNULL(q.NettPriceTotal, 0) AS NettPriceTotal,
                   ISNULL(q.Margin, 0) AS Margin,
                   q.QuoteDate, ISNULL(u.Name, '') AS Originator,
                   q.CustomerNotes, q.InternalNotes, q.Terms,
                   ISNULL(q.ContactId, 0) AS ContactId,
                   ISNULL(q.DivisionId, 0) AS DivisionId,
                   ISNULL(q.Code, '') AS Code,
                   ISNULL(q.QuoteStatusId, 0) AS QuoteStatusId
            FROM Quotes q
            LEFT JOIN Contacts c ON q.ContactId = c.ContactId
            LEFT JOIN QuoteStatus qs ON q.QuoteStatusId = qs.QuoteStatusId
            LEFT JOIN Users u ON q.Code = u.Code
            WHERE 1=1";

        var parameters = new Dictionary<string, object?> { ["Limit"] = limit };

        if (dateFrom.HasValue) { sql += " AND q.QuoteDate >= @DateFrom"; parameters["DateFrom"] = dateFrom.Value; }
        if (dateTo.HasValue) { sql += " AND q.QuoteDate <= @DateTo"; parameters["DateTo"] = dateTo.Value; }
        if (!string.IsNullOrEmpty(customerName)) { sql += " AND c.CompanyName LIKE @CustomerName"; parameters["CustomerName"] = $"%{customerName}%"; }
        if (!string.IsNullOrEmpty(status)) { sql += " AND qs.QuoteStatus = @Status"; parameters["Status"] = status; }
        if (!string.IsNullOrEmpty(originatorCode)) { sql += " AND q.Code = @OriginatorCode"; parameters["OriginatorCode"] = originatorCode; }

        sql += " ORDER BY q.QuoteDate DESC";

        var dt = await _db.QueryAsync(sql, parameters);
        return dt.Map(MapQuote);
    }

    public async Task<Quote?> GetQuoteAsync(int qid)
    {
        var sql = @"
            SELECT q.Qid, ISNULL(q.Reference, '') AS Reference,
                   ISNULL(c.CompanyName, '') AS CompanyName,
                   ISNULL(q.Reference, '') AS Project,
                   ISNULL(qs.QuoteStatus, '') AS QuoteStatus,
                   ISNULL(q.UnitCostTotal, 0) AS UnitCostTotal,
                   ISNULL(q.NettPriceTotal, 0) AS NettPriceTotal,
                   ISNULL(q.Margin, 0) AS Margin,
                   q.QuoteDate, ISNULL(u.Name, '') AS Originator,
                   q.CustomerNotes, q.InternalNotes, q.Terms,
                   ISNULL(q.ContactId, 0) AS ContactId,
                   ISNULL(q.DivisionId, 0) AS DivisionId,
                   ISNULL(q.Code, '') AS Code,
                   ISNULL(q.QuoteStatusId, 0) AS QuoteStatusId
            FROM Quotes q
            LEFT JOIN Contacts c ON q.ContactId = c.ContactId
            LEFT JOIN QuoteStatus qs ON q.QuoteStatusId = qs.QuoteStatusId
            LEFT JOIN Users u ON q.Code = u.Code
            WHERE q.Qid = @Qid";

        var dt = await _db.QueryAsync(sql, new() { ["Qid"] = qid });
        return dt.Rows.Count == 0 ? null : MapQuote(dt.Rows[0]);
    }

    public async Task<List<QuoteLineItem>> GetQuoteLineItemsAsync(int qid)
    {
        var sql = @"
            SELECT QuoteContentsId, Qid,
                   ISNULL(Description, '') AS Description,
                   ISNULL(Quantity, 0) AS Quantity,
                   ISNULL(Units, 0) AS Units,
                   ISNULL(Days, 0) AS Days,
                   ISNULL(UnitCost, 0) AS UnitCost,
                   ISNULL(UnitPrice, 0) AS UnitPrice,
                   ISNULL(LineTotal, 0) AS LineTotal
            FROM QuoteContents
            WHERE Qid = @Qid
            ORDER BY QuoteContentsId";

        var dt = await _db.QueryAsync(sql, new() { ["Qid"] = qid });
        return dt.Map(r => new QuoteLineItem
        {
            QuoteContentsId = Convert.ToInt32(r["QuoteContentsId"]),
            Qid = Convert.ToInt32(r["Qid"]),
            Description = r["Description"]?.ToString() ?? "",
            Quantity = Convert.ToDecimal(r["Quantity"]),
            Units = Convert.ToDecimal(r["Units"]),
            Days = Convert.ToDecimal(r["Days"]),
            UnitCost = Convert.ToDecimal(r["UnitCost"]),
            UnitPrice = Convert.ToDecimal(r["UnitPrice"]),
            LineTotal = Convert.ToDecimal(r["LineTotal"]),
        });
    }

    public async Task<int> CreateQuoteAsync(Quote quote, string originatorCode)
    {
        var sql = @"
            INSERT INTO Quotes (Reference, ContactId, DivisionId, QuoteStatusId, Code,
                                UnitCostTotal, NettPriceTotal, Margin, QuoteDate,
                                CustomerNotes, InternalNotes, Terms)
            VALUES (@Reference, @ContactId, @DivisionId, @QuoteStatusId, @Code,
                    @UnitCostTotal, @NettPriceTotal, @Margin, @QuoteDate,
                    @CustomerNotes, @InternalNotes, @Terms)";

        return await _db.InsertAsync(sql, new()
        {
            ["Reference"] = quote.Reference,
            ["ContactId"] = quote.ContactId,
            ["DivisionId"] = quote.DivisionId,
            ["QuoteStatusId"] = quote.QuoteStatusId > 0 ? quote.QuoteStatusId : 1,
            ["Code"] = originatorCode,
            ["UnitCostTotal"] = quote.UnitCostTotal,
            ["NettPriceTotal"] = quote.NettPriceTotal,
            ["Margin"] = quote.Margin,
            ["QuoteDate"] = quote.QuoteDate == default ? DateTime.Now : quote.QuoteDate,
            ["CustomerNotes"] = (object?)quote.CustomerNotes ?? DBNull.Value,
            ["InternalNotes"] = (object?)quote.InternalNotes ?? DBNull.Value,
            ["Terms"] = (object?)quote.Terms ?? DBNull.Value,
        });
    }

    public async Task<int> UpdateQuoteAsync(Quote quote)
    {
        var sql = @"
            UPDATE Quotes SET
                Reference = @Reference,
                ContactId = @ContactId,
                DivisionId = @DivisionId,
                QuoteStatusId = @QuoteStatusId,
                UnitCostTotal = @UnitCostTotal,
                NettPriceTotal = @NettPriceTotal,
                Margin = @Margin,
                CustomerNotes = @CustomerNotes,
                InternalNotes = @InternalNotes,
                Terms = @Terms
            WHERE Qid = @Qid";

        return await _db.ExecuteAsync(sql, new()
        {
            ["Qid"] = quote.Qid,
            ["Reference"] = quote.Reference,
            ["ContactId"] = quote.ContactId,
            ["DivisionId"] = quote.DivisionId,
            ["QuoteStatusId"] = quote.QuoteStatusId,
            ["UnitCostTotal"] = quote.UnitCostTotal,
            ["NettPriceTotal"] = quote.NettPriceTotal,
            ["Margin"] = quote.Margin,
            ["CustomerNotes"] = (object?)quote.CustomerNotes ?? DBNull.Value,
            ["InternalNotes"] = (object?)quote.InternalNotes ?? DBNull.Value,
            ["Terms"] = (object?)quote.Terms ?? DBNull.Value,
        });
    }

    private static Quote MapQuote(DataRow r) => new()
    {
        Qid = Convert.ToInt32(r["Qid"]),
        Reference = r["Reference"]?.ToString() ?? "",
        CompanyName = r["CompanyName"]?.ToString() ?? "",
        Project = r["Project"]?.ToString(),
        QuoteStatus = r["QuoteStatus"]?.ToString() ?? "",
        UnitCostTotal = Convert.ToDecimal(r["UnitCostTotal"]),
        NettPriceTotal = Convert.ToDecimal(r["NettPriceTotal"]),
        Margin = Convert.ToDecimal(r["Margin"]),
        QuoteDate = r["QuoteDate"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["QuoteDate"]),
        Originator = r["Originator"]?.ToString() ?? "",
        CustomerNotes = r["CustomerNotes"] == DBNull.Value ? null : r["CustomerNotes"]?.ToString(),
        InternalNotes = r["InternalNotes"] == DBNull.Value ? null : r["InternalNotes"]?.ToString(),
        Terms = r["Terms"] == DBNull.Value ? null : r["Terms"]?.ToString(),
        ContactId = Convert.ToInt32(r["ContactId"]),
        DivisionId = Convert.ToInt32(r["DivisionId"]),
        Code = r["Code"]?.ToString() ?? "",
        QuoteStatusId = Convert.ToInt32(r["QuoteStatusId"]),
    };
}
