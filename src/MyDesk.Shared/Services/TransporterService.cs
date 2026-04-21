using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class TransporterService
{
    private readonly DatabaseService _db;
    private readonly ILogger<TransporterService> _logger;

    public TransporterService(DatabaseService db, ILogger<TransporterService> logger)
    { _db = db; _logger = logger; }

    public async Task<int?> QuoteToJobOrderAsync(int quoteId, string userCode)
    {
        try
        {
            // Get quote details
            var quoteDt = await _db.QueryAsync(@"
                SELECT q.*, c.ContactId, c.CompanyId
                FROM Quotes q
                LEFT JOIN Contacts c ON c.ContactId = q.ContactId
                WHERE q.Qid = @Id", new() { ["Id"] = quoteId });

            if (quoteDt.Rows.Count == 0) return null;
            var q = quoteDt.Rows[0];

            // Create job order
            var sql = @"
                INSERT INTO JobOrders (DateAccepted, DivisionId, ContactId, CompanyId, Qid,
                                        JobOrderStatusId, OriginatorId, Notes, Code)
                VALUES (GETDATE(), @DivisionId, @ContactId, @CompanyId, @Qid,
                        1, @OriginatorId, @Notes, @Code);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var p = new Dictionary<string, object?>
            {
                ["DivisionId"] = q["DivisionId"] == DBNull.Value ? null : q["DivisionId"],
                ["ContactId"] = q["ContactId"] == DBNull.Value ? null : q["ContactId"],
                ["CompanyId"] = q["CompanyId"] == DBNull.Value ? null : q["CompanyId"],
                ["Qid"] = quoteId,
                ["OriginatorId"] = DBNull.Value, // Will need to get from user context
                ["Notes"] = $"Converted from Quote #{quoteId}",
                ["Code"] = userCode
            };

            var dt = await _db.QueryAsync(sql, p);
            var jobOrderId = Convert.ToInt32(dt.Rows[0][0]);

            // Copy line items
            var linesDt = await _db.QueryAsync(@"
                SELECT Description, Qty, NettPrice
                FROM QuoteContents
                WHERE Qid = @Id", new() { ["Id"] = quoteId });

            foreach (System.Data.DataRow line in linesDt.Rows)
            {
                await _db.ExecuteAsync(@"
                    INSERT INTO JobOrderContents (JobOrderId, Qty, Description, Price)
                    VALUES (@JobOrderId, @Qty, @Description, @Price)", new()
                {
                    ["JobOrderId"] = jobOrderId,
                    ["Qty"] = line["Qty"] == DBNull.Value ? 0 : Convert.ToInt32(line["Qty"]),
                    ["Description"] = line["Description"]?.ToString() ?? "",
                    ["Price"] = line["NettPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(line["NettPrice"])
                });
            }

            return jobOrderId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QuoteToJobOrderAsync failed for QuoteId {QuoteId}", quoteId);
            return null;
        }
    }

    public async Task<int?> JobOrderToInvoiceAsync(int jobOrderId, string userCode)
    {
        try
        {
            // Get job order details
            var joDt = await _db.QueryAsync(@"
                SELECT jo.*, c.ContactId, c.CompanyId
                FROM JobOrders jo
                LEFT JOIN Contacts c ON c.ContactId = jo.ContactId
                WHERE jo.JobOrderId = @Id", new() { ["Id"] = jobOrderId });

            if (joDt.Rows.Count == 0) return null;
            var jo = joDt.Rows[0];

            // Create invoice
            var sql = @"
                INSERT INTO Invoices (ContactId, CompanyId, DivisionId, InvoiceDate, InvoiceStatusId,
                                     Terms, Attention, Delivery, Code)
                VALUES (@ContactId, @CompanyId, @DivisionId, GETDATE(), 1,
                        'F.I.S. via general road freight', @Attention, @Delivery, @Code);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var p = new Dictionary<string, object?>
            {
                ["ContactId"] = jo["ContactId"] == DBNull.Value ? null : jo["ContactId"],
                ["CompanyId"] = jo["CompanyId"] == DBNull.Value ? null : jo["CompanyId"],
                ["DivisionId"] = jo["DivisionId"] == DBNull.Value ? null : jo["DivisionId"],
                ["Attention"] = DBNull.Value,
                ["Delivery"] = DBNull.Value,
                ["Code"] = userCode
            };

            var dt = await _db.QueryAsync(sql, p);
            var invoiceId = Convert.ToInt32(dt.Rows[0][0]);

            // Copy line items
            var linesDt = await _db.QueryAsync(@"
                SELECT Description, Qty, Price
                FROM JobOrderContents
                WHERE JobOrderId = @Id", new() { ["Id"] = jobOrderId });

            foreach (System.Data.DataRow line in linesDt.Rows)
            {
                await _db.ExecuteAsync(@"
                    INSERT INTO InvoiceLineItems (InvoiceId, Quantity, Description, UnitPrice)
                    VALUES (@InvoiceId, @Qty, @Description, @Price)", new()
                {
                    ["InvoiceId"] = invoiceId,
                    ["Qty"] = line["Qty"] == DBNull.Value ? 0 : Convert.ToInt32(line["Qty"]),
                    ["Description"] = line["Description"]?.ToString() ?? "",
                    ["Price"] = line["Price"] == DBNull.Value ? 0m : Convert.ToDecimal(line["Price"])
                });
            }

            return invoiceId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JobOrderToInvoiceAsync failed for JobOrderId {JobOrderId}", jobOrderId);
            return null;
        }
    }

    public async Task<int?> QuoteToInvoiceAsync(int quoteId, string userCode)
    {
        try
        {
            // Get quote details
            var quoteDt = await _db.QueryAsync(@"
                SELECT q.*, c.ContactId, c.CompanyId
                FROM Quotes q
                LEFT JOIN Contacts c ON c.ContactId = q.ContactId
                WHERE q.Qid = @Id", new() { ["Id"] = quoteId });

            if (quoteDt.Rows.Count == 0) return null;
            var q = quoteDt.Rows[0];

            // Create invoice
            var sql = @"
                INSERT INTO Invoices (ContactId, CompanyId, DivisionId, InvoiceDate, InvoiceStatusId,
                                     Terms, Attention, Delivery, Code)
                VALUES (@ContactId, @CompanyId, @DivisionId, GETDATE(), 1,
                        'F.I.S. via general road freight', @Attention, @Delivery, @Code);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            var p = new Dictionary<string, object?>
            {
                ["ContactId"] = q["ContactId"] == DBNull.Value ? null : q["ContactId"],
                ["CompanyId"] = q["CompanyId"] == DBNull.Value ? null : q["CompanyId"],
                ["DivisionId"] = q["DivisionId"] == DBNull.Value ? null : q["DivisionId"],
                ["Attention"] = q["Attention"] == DBNull.Value ? null : q["Attention"],
                ["Delivery"] = q["Delivery"] == DBNull.Value ? null : q["Delivery"],
                ["Code"] = userCode
            };

            var dt = await _db.QueryAsync(sql, p);
            var invoiceId = Convert.ToInt32(dt.Rows[0][0]);

            // Copy line items
            var linesDt = await _db.QueryAsync(@"
                SELECT Description, Qty, NettPrice
                FROM QuoteContents
                WHERE Qid = @Id", new() { ["Id"] = quoteId });

            foreach (System.Data.DataRow line in linesDt.Rows)
            {
                await _db.ExecuteAsync(@"
                    INSERT INTO InvoiceLineItems (InvoiceId, Quantity, Description, UnitPrice)
                    VALUES (@InvoiceId, @Qty, @Description, @Price)", new()
                {
                    ["InvoiceId"] = invoiceId,
                    ["Qty"] = line["Qty"] == DBNull.Value ? 0 : Convert.ToInt32(line["Qty"]),
                    ["Description"] = line["Description"]?.ToString() ?? "",
                    ["Price"] = line["NettPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(line["NettPrice"])
                });
            }

            return invoiceId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "QuoteToInvoiceAsync failed for QuoteId {QuoteId}", quoteId);
            return null;
        }
    }
}
