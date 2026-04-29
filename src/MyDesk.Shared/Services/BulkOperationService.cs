using System.Data;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Collections.Generic;

namespace MyDesk.Shared.Services;

public class BulkOperationService
{
    private readonly DatabaseService _db;
    private readonly EmailService _emailSvc;
    private readonly ILogger<BulkOperationService> _logger;

    public BulkOperationService(DatabaseService db, EmailService emailSvc, ILogger<BulkOperationService> logger)
    { _db = db; _emailSvc = emailSvc; _logger = logger; }

    // ── Bulk Email ─────────────────────────────────────────────
    public async Task<Result> BulkEmailAsync(string userCode, List<int> invoiceIds, string subject, string body)
    {
        int sent = 0, failed = 0;
        var errors = new List<string>();

        foreach (var id in invoiceIds)
        {
            try
            {
                var dt = await _db.QueryAsync(@"
                    SELECT InvoiceNumber, Email, InvCompany 
                    FROM Invoices 
                    WHERE InvoiceId = @Id",
                    new() { ["Id"] = id });

                if (dt.Rows.Count == 0) { failed++; continue; }

                var row = dt.Rows[0];
                var email = row["Email"]?.ToString();
                var invNum = row["InvoiceNumber"]?.ToString() ?? id.ToString();
                var company = row["InvCompany"]?.ToString() ?? "";

                if (string.IsNullOrEmpty(email))
                {
                    errors.Add($"Invoice {invNum}: No email address");
                    failed++;
                    continue;
                }

                var personalizedBody = body.Replace("{InvoiceNumber}", invNum)
                                           .Replace("{Company}", company);

                await _emailSvc.SendAsync(email, subject, personalizedBody, personalizedBody);
                sent++;
            }
            catch (Exception ex)
            {
                errors.Add($"Invoice {id}: {ex.Message}");
                failed++;
            }
        }

        _logger.LogInformation("Bulk email: {Sent} sent, {Failed} failed by {UserCode}", sent, failed, userCode);
        return new(sent, failed, errors);
    }

    // ── Bulk Status Update ─────────────────────────────────────────────
    public async Task<Result> BulkUpdateStatusAsync(string userCode, List<int> invoiceIds, int newStatusId)
    {
        int updated = 0, failed = 0;
        var errors = new List<string>();

        foreach (var id in invoiceIds)
        {
            try
            {
                await _db.ExecuteAsync(@"
                    UPDATE Invoices SET InvoiceStatusId = @StatusId, DateModified = GETDATE()
                    WHERE InvoiceId = @Id",
                    new() { ["StatusId"] = newStatusId, ["Id"] = id });
                updated++;
            }
            catch (Exception ex)
            {
                errors.Add($"Invoice {id}: {ex.Message}");
                failed++;
            }
        }

        _logger.LogInformation("Bulk status update: {Updated} updated, {Failed} failed by {UserCode}", updated, failed, userCode);
        return new(updated, failed, errors);
    }

    // ── Bulk Export to CSV (simpler than Excel) ─────────────────────
    public async Task<string> BulkExportInvoicesAsync(List<int>? invoiceIds = null, string? filter = null)
    {
        var sql = @"
            SELECT i.InvoiceId, i.InvoiceNumber, i.InvCompany AS Company, 
                   i.InvoiceDate, i.NettPriceTotal, i.InvoiceStatusId,
                   s.InvoiceStatus, u.Name AS Originator
            FROM Invoices i
            LEFT JOIN InvoiceStatus s ON s.InvoiceStatusId = i.InvoiceStatusId
            LEFT JOIN Users u ON u.Code = i.Code
            WHERE 1=1";

        if (invoiceIds != null && invoiceIds.Any())
            sql += " AND i.InvoiceId IN @Ids";
        if (!string.IsNullOrEmpty(filter))
            sql += " AND (i.InvCompany LIKE @Filter OR i.InvoiceNumber LIKE @Filter)";
        sql += " ORDER BY i.InvoiceDate DESC";

        var parameters = new Dictionary<string, object?>
        {
            ["Ids"] = invoiceIds ?? new List<int>(),
            ["Filter"] = string.IsNullOrEmpty(filter) ? DBNull.Value : $"%{filter}%"
        };

        var dt = await _db.QueryAsync(sql, parameters);

        var csv = new StringBuilder();
        csv.AppendLine("Invoice #,Company,Date,Amount,Status,Originator");

        foreach (DataRow r in dt.Rows)
        {
            var invNum = r["InvoiceNumber"]?.ToString() ?? "";
            var company = r["Company"]?.ToString() ?? "";
            var date = r["InvoiceDate"] != DBNull.Value ? Convert.ToDateTime(r["InvoiceDate"]).ToString("yyyy-MM-dd") : "";
            var amount = r["NettPriceTotal"] != DBNull.Value ? Convert.ToDecimal(r["NettPriceTotal"]).ToString() : "0";
            var status = r["InvoiceStatus"]?.ToString() ?? "";
            var originator = r["Originator"]?.ToString() ?? "";

            csv.AppendLine($"\"{invNum}\",\"{company}\",\"{date}\",\"{amount}\",\"{status}\",\"{originator}\"");
        }

        return csv.ToString();
    }

    public record Result(int SuccessCount, int FailedCount, List<string> Errors);
}
