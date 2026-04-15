using System.Data;
using Techlight.MyDesk.MCP.Models;

namespace Techlight.MyDesk.MCP.Services;

public class QuoteService
{
    private readonly DatabaseService _db;
    private readonly ILogger<QuoteService> _logger;

    public QuoteService(DatabaseService db, ILogger<QuoteService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Quote?> GetQuoteByIdAsync(int qid, McpContext context)
    {
        var sql = @"
            SELECT q.Qid, q.Reference, c.CompanyName, q.Reference as Project, 
                   qs.QuoteStatus, q.UnitCostTotal, q.NettPriceTotal, q.Margin, 
                   q.QuoteDate, u.Name as Originator, q.CustomerNotes, 
                   q.InternalNotes, q.Terms, q.ContactId, q.DivisionId, 
                   q.Code, q.QuoteStatusId
            FROM Quotes q
            INNER JOIN Contacts c ON q.ContactId = c.ContactId
            INNER JOIN QuoteStatus qs ON q.QuoteStatusId = qs.QuoteStatusId
            INNER JOIN Users u ON q.Code = u.Code
            WHERE q.Qid = @Qid
            AND q.DivisionId IN (SELECT value FROM STRING_SPLIT(@DivisionIds, ','))";

        var dt = await _db.ExecuteQueryAsync(sql, new Dictionary<string, object>
        {
            ["Qid"] = qid,
            ["DivisionIds"] = string.Join(",", context.AccessibleDivisions)
        });

        if (dt.Rows.Count == 0) return null;

        return MapQuoteFromDataRow(dt.Rows[0]);
    }

    public async Task<List<Quote>> GetQuotesAsync(DateTime? dateFrom = null, DateTime? dateTo = null, 
        string? customerName = null, string? status = null, string? originatorCode = null, 
        int? limit = 50, McpContext? context = null)
    {
        var sql = @"
            SELECT TOP (@Limit) q.Qid, q.Reference, c.CompanyName, q.Reference as Project, 
                   qs.QuoteStatus, q.UnitCostTotal, q.NettPriceTotal, q.Margin, 
                   q.QuoteDate, u.Name as Originator, q.CustomerNotes, 
                   q.InternalNotes, q.Terms, q.ContactId, q.DivisionId, 
                   q.Code, q.QuoteStatusId
            FROM Quotes q
            INNER JOIN Contacts c ON q.ContactId = c.ContactId
            INNER JOIN QuoteStatus qs ON q.QuoteStatusId = qs.QuoteStatusId
            INNER JOIN Users u ON q.Code = u.Code
            WHERE q.QuoteStatusId NOT IN (4, 5)";

        var parameters = new Dictionary<string, object>
        {
            ["Limit"] = limit ?? 50
        };

        if (dateFrom.HasValue)
        {
            sql += " AND q.QuoteDate >= @DateFrom";
            parameters["DateFrom"] = dateFrom.Value;
        }

        if (dateTo.HasValue)
        {
            sql += " AND q.QuoteDate <= @DateTo";
            parameters["DateTo"] = dateTo.Value;
        }

        if (!string.IsNullOrEmpty(customerName))
        {
            sql += " AND c.CompanyName LIKE @CustomerName";
            parameters["CustomerName"] = $"%{customerName}%";
        }

        if (!string.IsNullOrEmpty(status))
        {
            sql += " AND qs.QuoteStatus = @Status";
            parameters["Status"] = status;
        }

        if (!string.IsNullOrEmpty(originatorCode))
        {
            sql += " AND q.Code = @OriginatorCode";
            parameters["OriginatorCode"] = originatorCode;
        }

        if (context?.AccessibleDivisions?.Any() == true)
        {
            sql += " AND q.DivisionId IN (SELECT value FROM STRING_SPLIT(@DivisionIds, ','))";
            parameters["DivisionIds"] = string.Join(",", context.AccessibleDivisions);
        }

        sql += " ORDER BY q.QuoteDate DESC";

        var dt = await _db.ExecuteQueryAsync(sql, parameters);
        return dt.AsEnumerable().Select(MapQuoteFromDataRow).ToList();
    }

    public async Task<List<QuoteLineItem>> GetQuoteLineItemsAsync(int qid)
    {
        var sql = @"
            SELECT QuoteContentsId, Qid, Description, Quantity, Units, Days, 
                   UnitCost, UnitPrice, (Quantity * UnitPrice) as LineTotal
            FROM QuoteContents
            WHERE Qid = @Qid
            UNION ALL
            SELECT QuoteThirdPartyContentsId as QuoteContentsId, QuoteId as Qid, 
                   Description, Quantity, 0 as Units, 0 as Days, 
                   UnitCost, UnitPrice, (Quantity * UnitPrice) as LineTotal
            FROM QuoteThirdPartyContents
            WHERE QuoteId = @Qid";

        var dt = await _db.ExecuteQueryAsync(sql, new Dictionary<string, object> { ["Qid"] = qid });
        
        return dt.AsEnumerable().Select(row => new QuoteLineItem
        {
            QuoteContentsId = Convert.ToInt32(row["QuoteContentsId"]),
            Qid = Convert.ToInt32(row["Qid"]),
            Description = row["Description"].ToString()!,
            Quantity = Convert.ToDecimal(row["Quantity"]),
            Units = Convert.ToDecimal(row["Units"]),
            Days = Convert.ToDecimal(row["Days"]),
            UnitCost = Convert.ToDecimal(row["UnitCost"]),
            UnitPrice = Convert.ToDecimal(row["UnitPrice"]),
            LineTotal = Convert.ToDecimal(row["LineTotal"])
        }).ToList();
    }

    public async Task<Quote> CreateQuoteAsync(CreateQuoteRequest request, McpContext context)
    {
        // Calculate totals from line items
        decimal totalCost = 0;
        decimal totalPrice = 0;
        
        foreach (var item in request.LineItems)
        {
            decimal qty = item.Quantity;
            decimal units = item.Units ?? 1;
            decimal days = item.Days ?? 1;
            
            totalCost += qty * units * days * item.UnitCost;
            totalPrice += qty * units * days * item.UnitPrice;
        }
        
        decimal margin = totalPrice > 0 ? ((totalPrice - totalCost) / totalPrice) * 100 : 0;

        // Insert quote
        var insertSql = @"
            INSERT INTO Quotes (ContactId, Code, Reference, CustomerNotes, InternalNotes, 
                              Terms, DivisionId, QuoteStatusId, UnitCostTotal, 
                              NettPriceTotal, Margin, QuoteDate)
            VALUES (@ContactId, @Code, @Reference, @CustomerNotes, @InternalNotes, 
                   @Terms, @DivisionId, 1, @UnitCostTotal, @NettPriceTotal, @Margin, GETDATE());
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var qid = await _db.ExecuteScalarAsync<int>(insertSql, new Dictionary<string, object>
        {
            ["ContactId"] = request.ContactId,
            ["Code"] = context.UserCode,
            ["Reference"] = request.Reference,
            ["CustomerNotes"] = (object?)request.CustomerNotes ?? DBNull.Value,
            ["InternalNotes"] = (object?)request.InternalNotes ?? DBNull.Value,
            ["Terms"] = (object?)request.Terms ?? DBNull.Value,
            ["DivisionId"] = request.DivisionId,
            ["UnitCostTotal"] = totalCost,
            ["NettPriceTotal"] = totalPrice,
            ["Margin"] = margin
        });

        // Insert line items
        foreach (var item in request.LineItems)
        {
            await InsertQuoteLineItemAsync(qid, item);
        }

        // Return the created quote
        var quote = await GetQuoteByIdAsync(qid, context);
        return quote!;
    }

    private async Task InsertQuoteLineItemAsync(int qid, QuoteLineItemRequest item)
    {
        var sql = @"
            INSERT INTO QuoteContents (Qid, Description, Quantity, Units, Days, UnitCost, UnitPrice)
            VALUES (@Qid, @Description, @Quantity, @Units, @Days, @UnitCost, @UnitPrice)";

        await _db.ExecuteNonQueryAsync(sql, new Dictionary<string, object>
        {
            ["Qid"] = qid,
            ["Description"] = item.Description,
            ["Quantity"] = item.Quantity,
            ["Units"] = item.Units ?? 1,
            ["Days"] = item.Days ?? 1,
            ["UnitCost"] = item.UnitCost,
            ["UnitPrice"] = item.UnitPrice
        });
    }

    public async Task<Quote> UpdateQuoteStatusAsync(int qid, string newStatus, string? notes, McpContext context)
    {
        // Get status ID
        var statusId = await GetQuoteStatusIdAsync(newStatus);
        
        if (statusId == 0)
            throw new ArgumentException($"Invalid status: {newStatus}");

        // Update quote
        var sql = @"
            UPDATE Quotes 
            SET QuoteStatusId = @StatusId
            WHERE Qid = @Qid";

        await _db.ExecuteNonQueryAsync(sql, new Dictionary<string, object>
        {
            ["Qid"] = qid,
            ["StatusId"] = statusId
        });

        // Add to quote history if notes provided
        if (!string.IsNullOrEmpty(notes))
        {
            await AddQuoteCommentAsync(qid, notes, context);
        }

        return (await GetQuoteByIdAsync(qid, context))!;
    }

    private async Task<int> GetQuoteStatusIdAsync(string statusName)
    {
        var sql = "SELECT QuoteStatusId FROM QuoteStatus WHERE QuoteStatus = @Status";
        var result = await _db.ExecuteScalarAsync<int?>(sql, new Dictionary<string, object>
        {
            ["Status"] = statusName
        });
        return result ?? 0;
    }

    private async Task AddQuoteCommentAsync(int qid, string comment, McpContext context)
    {
        var sql = @"
            INSERT INTO Comments (TableId, ItemId, FromCode, Comment, DateEntered)
            VALUES (6, @ItemId, @FromCode, @Comment, GETDATE())";

        await _db.ExecuteNonQueryAsync(sql, new Dictionary<string, object>
        {
            ["ItemId"] = qid,
            ["FromCode"] = context.UserCode,
            ["Comment"] = comment
        });
    }

    public async Task<ReportResult> GenerateQuoteReportAsync(DateTime dateFrom, DateTime dateTo, 
        string? originatorCode = null, string? customerName = null, McpContext? context = null)
    {
        var sql = @"
            SELECT q.Qid, q.Reference, c.CompanyName, qs.QuoteStatus, 
                   q.UnitCostTotal, q.NettPriceTotal, q.Margin, q.QuoteDate, u.Name as Originator
            FROM Quotes q
            INNER JOIN Contacts c ON q.ContactId = c.ContactId
            INNER JOIN QuoteStatus qs ON q.QuoteStatusId = qs.QuoteStatusId
            INNER JOIN Users u ON q.Code = u.Code
            WHERE q.QuoteDate BETWEEN @DateFrom AND @DateTo";

        var parameters = new Dictionary<string, object>
        {
            ["DateFrom"] = dateFrom,
            ["DateTo"] = dateTo
        };

        if (!string.IsNullOrEmpty(originatorCode))
        {
            sql += " AND q.Code = @OriginatorCode";
            parameters["OriginatorCode"] = originatorCode;
        }

        if (!string.IsNullOrEmpty(customerName))
        {
            sql += " AND c.CompanyName LIKE @CustomerName";
            parameters["CustomerName"] = $"%{customerName}%";
        }

        if (context?.AccessibleDivisions?.Any() == true)
        {
            sql += " AND q.DivisionId IN (SELECT value FROM STRING_SPLIT(@DivisionIds, ','))";
            parameters["DivisionIds"] = string.Join(",", context.AccessibleDivisions);
        }

        sql += " ORDER BY q.QuoteDate DESC";

        var dt = await _db.ExecuteQueryAsync(sql, parameters);
        var records = _db.DataTableToList(dt);

        decimal totalValue = records.Sum(r => r.ContainsKey("NettPriceTotal") ? Convert.ToDecimal(r["NettPriceTotal"]) : 0);

        return new ReportResult
        {
            Title = $"Quote Report: {dateFrom:dd/MM/yyyy} - {dateTo:dd/MM/yyyy}",
            TotalRecords = records.Count,
            TotalAmount = totalValue,
            Records = records,
            SummaryText = $"Found {records.Count} quotes totaling ${totalValue:N2}"
        };
    }

    private Quote MapQuoteFromDataRow(DataRow row)
    {
        return new Quote
        {
            Qid = Convert.ToInt32(row["Qid"]),
            Reference = row["Reference"].ToString()!,
            CompanyName = row["CompanyName"].ToString()!,
            Project = row["Project"]?.ToString(),
            QuoteStatus = row["QuoteStatus"].ToString()!,
            UnitCostTotal = Convert.ToDecimal(row["UnitCostTotal"]),
            NettPriceTotal = Convert.ToDecimal(row["NettPriceTotal"]),
            Margin = Convert.ToDecimal(row["Margin"]),
            QuoteDate = Convert.ToDateTime(row["QuoteDate"]),
            Originator = row["Originator"].ToString()!,
            CustomerNotes = row["CustomerNotes"]?.ToString(),
            InternalNotes = row["InternalNotes"]?.ToString(),
            Terms = row["Terms"]?.ToString(),
            ContactId = Convert.ToInt32(row["ContactId"]),
            DivisionId = Convert.ToInt32(row["DivisionId"]),
            Code = row["Code"].ToString()!,
            QuoteStatusId = Convert.ToInt32(row["QuoteStatusId"])
        };
    }
}
