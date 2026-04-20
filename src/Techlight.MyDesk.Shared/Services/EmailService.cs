using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Techlight.MyDesk.Shared.Services;

/// <summary>
/// SMTP email sending with EmailLog persistence and UserActivity tracking.
/// Ported from MyDeskMCP/Services/EmailService.cs into the shared library.
/// Config keys: Email:SmtpHost, Email:SmtpPort, Email:SmtpUser, Email:SmtpPass,
///              Email:FromAddress, Email:FromName
/// </summary>
public class EmailService
{
    private readonly DatabaseService _db;
    private readonly ActivityService _activity;
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(DatabaseService db, ActivityService activity,
        IConfiguration config, ILogger<EmailService> logger)
    {
        _db       = db;
        _activity = activity;
        _config   = config;
        _logger   = logger;
    }

    public async Task EnsureTablesAsync()
    {
        try
        {
            await _db.ExecuteAsync(@"
                IF OBJECT_ID(N'EmailLog', N'U') IS NULL
                BEGIN
                    CREATE TABLE EmailLog (
                        EmailLogId  INT IDENTITY(1,1) PRIMARY KEY,
                        EntityType  NVARCHAR(50)  NOT NULL,
                        EntityId    INT           NOT NULL,
                        ToEmail     NVARCHAR(255) NOT NULL,
                        Subject     NVARCHAR(500) NOT NULL,
                        SentBy      NVARCHAR(50)  NOT NULL,
                        SentDate    DATETIME      NOT NULL DEFAULT GETDATE(),
                        Status      NVARCHAR(50)  NOT NULL DEFAULT N'Sent'
                    );
                    CREATE INDEX IX_EmailLog_Entity ON EmailLog (EntityType, EntityId);
                END");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not ensure EmailLog table");
        }
    }

    public async Task<bool> EmailQuoteAsync(
        int quoteId, string toEmail, string? subject, string? message,
        string senderCode, byte[]? pdfBytes = null)
    {
        try
        {
            var dt = await _db.QueryAsync(@"
                SELECT ISNULL(q.Reference,'')      AS Reference,
                       ISNULL(c.Company,'')        AS Company,
                       ISNULL(c.FirstName,'')      AS FirstName,
                       ISNULL(u.Name,'')           AS OriginatorName,
                       ISNULL(u.Email,'')          AS OriginatorEmail
                FROM Quotes q
                LEFT JOIN Contacts c ON c.ContactId = q.ContactId
                LEFT JOIN Users u    ON u.Code = q.Code
                WHERE q.Qid = @Id",
                new() { ["Id"] = quoteId });

            if (dt.Rows.Count == 0) throw new InvalidOperationException($"Quote {quoteId} not found");

            var r            = dt.Rows[0];
            var reference    = r["Reference"].ToString()!;
            var company      = r["Company"].ToString()!;
            var firstName    = r["FirstName"].ToString()!;
            var originator   = r["OriginatorName"].ToString()!;
            var originEmail  = r["OriginatorEmail"].ToString()!;

            var emailSubject = subject ?? $"Quote — {reference}";
            var emailBody    = message ?? BuildQuoteBody(reference, company, firstName, originator);

            await SendAsync(toEmail, emailSubject, emailBody,
                fromEmail: string.IsNullOrEmpty(originEmail) ? null : originEmail,
                fromName:  originator,
                attachment: pdfBytes,
                attachmentName: pdfBytes != null ? $"Quote-{quoteId}.pdf" : null);

            await LogEmailAsync("Quote", quoteId, toEmail, emailSubject, senderCode);
            await _activity.LogAsync(senderCode, "Quote", quoteId,
                $"Emailed quote to {toEmail}", reference);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to email quote {QuoteId} to {To}", quoteId, toEmail);
            return false;
        }
    }

    public async Task<bool> EmailInvoiceAsync(
        int invoiceId, string toEmail, string? subject, string? message,
        string senderCode, byte[]? pdfBytes = null)
    {
        try
        {
            var dt = await _db.QueryAsync(@"
                SELECT ISNULL(i.InvoiceNumber,'')  AS InvoiceNumber,
                       ISNULL(c.Company,'')        AS Company,
                       ISNULL(c.FirstName,'')      AS FirstName,
                       ISNULL(u.Name,'')           AS OriginatorName,
                       ISNULL(u.Email,'')          AS OriginatorEmail
                FROM Invoices i
                LEFT JOIN Contacts c ON c.ContactId = i.ContactId
                LEFT JOIN Users u    ON u.Code = i.Code
                WHERE i.InvoiceId = @Id",
                new() { ["Id"] = invoiceId });

            if (dt.Rows.Count == 0) throw new InvalidOperationException($"Invoice {invoiceId} not found");

            var r           = dt.Rows[0];
            var invNum      = r["InvoiceNumber"].ToString()!;
            var firstName   = r["FirstName"].ToString()!;
            var originator  = r["OriginatorName"].ToString()!;
            var originEmail = r["OriginatorEmail"].ToString()!;

            var emailSubject = subject ?? $"Invoice — {invNum}";
            var emailBody    = message ?? $"""
                <p>Dear {(string.IsNullOrEmpty(firstName) ? "Sir/Madam" : firstName)},</p>
                <p>Please find attached Invoice <strong>{invNum}</strong> from Techlight.</p>
                <p>If you have any questions, please don't hesitate to contact us.</p>
                <p>Kind regards,<br/>{originator}</p>
                <p style="color:#999;font-size:12px;">Sent via Techlight MyDesk.</p>
                """;

            await SendAsync(toEmail, emailSubject, emailBody,
                fromEmail: string.IsNullOrEmpty(originEmail) ? null : originEmail,
                fromName:  originator,
                attachment: pdfBytes,
                attachmentName: pdfBytes != null ? $"Invoice-{invoiceId}.pdf" : null);

            await LogEmailAsync("Invoice", invoiceId, toEmail, emailSubject, senderCode);
            await _activity.LogAsync(senderCode, "Invoice", invoiceId,
                $"Emailed invoice to {toEmail}", invNum);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to email invoice {InvoiceId} to {To}", invoiceId, toEmail);
            return false;
        }
    }

    public async Task<bool> EmailPurchaseOrderAsync(
        int poId, string toEmail, string? subject, string? message,
        string senderCode, byte[]? pdfBytes = null)
    {
        try
        {
            var dt = await _db.QueryAsync(@"
                SELECT ISNULL(p.PurchaseOrderNumber,'')  AS PONumber,
                       ISNULL(c.CompanyName,'')          AS SupplierName,
                       ISNULL(u.Name,'')                 AS OriginatorName,
                       ISNULL(u.Email,'')                AS OriginatorEmail
                FROM PurchaseOrders p
                LEFT JOIN Contacts c ON c.ContactId = p.ContactId
                LEFT JOIN Users u    ON u.Code = p.Code
                WHERE p.PurchaseOrderId = @Id",
                new() { ["Id"] = poId });

            if (dt.Rows.Count == 0) throw new InvalidOperationException($"PO {poId} not found");

            var r           = dt.Rows[0];
            var poNum       = r["PONumber"].ToString()!;
            var supplier    = r["SupplierName"].ToString()!;
            var originator  = r["OriginatorName"].ToString()!;
            var originEmail = r["OriginatorEmail"].ToString()!;

            var emailSubject = subject ?? $"Purchase Order — {poNum}";
            var emailBody    = message ?? $"""
                <p>Dear {supplier},</p>
                <p>Please find attached Purchase Order <strong>{poNum}</strong> from Techlight.</p>
                <p>Kind regards,<br/>{originator}</p>
                <p style="color:#999;font-size:12px;">Sent via Techlight MyDesk.</p>
                """;

            await SendAsync(toEmail, emailSubject, emailBody,
                fromEmail: string.IsNullOrEmpty(originEmail) ? null : originEmail,
                fromName:  originator,
                attachment: pdfBytes,
                attachmentName: pdfBytes != null ? $"PurchaseOrder-{poId}.pdf" : null);

            await LogEmailAsync("PO", poId, toEmail, emailSubject, senderCode);
            await _activity.LogAsync(senderCode, "PO", poId,
                $"Emailed PO to {toEmail}", poNum);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to email PO {PoId} to {To}", poId, toEmail);
            return false;
        }
    }

    public async Task SendAsync(
        string to, string subject, string htmlBody,
        string? fromEmail = null, string? fromName = null,
        byte[]? attachment = null, string? attachmentName = null)
    {
        var smtpHost    = _config["Email:SmtpHost"] ?? "localhost";
        var smtpPort    = int.Parse(_config["Email:SmtpPort"] ?? "25");
        var smtpUser    = _config["Email:SmtpUser"];
        var smtpPass    = _config["Email:SmtpPass"];
        var defaultFrom = _config["Email:FromAddress"] ?? "noreply@techlight.com.au";
        var displayName = fromName ?? _config["Email:FromName"] ?? "Techlight MyDesk";
        var from        = !string.IsNullOrWhiteSpace(fromEmail) ? fromEmail : defaultFrom;

        using var client = new SmtpClient(smtpHost, smtpPort);
        if (!string.IsNullOrEmpty(smtpUser))
        {
            client.Credentials = new NetworkCredential(smtpUser, smtpPass);
            client.EnableSsl   = true;
        }

        var msg = new MailMessage
        {
            From       = new MailAddress(from, displayName),
            Subject    = subject,
            Body       = htmlBody,
            IsBodyHtml = true,
        };
        msg.To.Add(to);

        MemoryStream? ms = null;
        try
        {
            if (attachment != null && !string.IsNullOrEmpty(attachmentName))
            {
                ms = new MemoryStream(attachment);
                msg.Attachments.Add(new Attachment(ms, attachmentName, "application/pdf"));
            }
            await client.SendMailAsync(msg);
        }
        finally
        {
            ms?.Dispose();
            msg.Dispose();
        }
    }

    private async Task LogEmailAsync(string entityType, int entityId,
        string toEmail, string subject, string sentBy)
    {
        try
        {
            await _db.ExecuteAsync(
                @"INSERT INTO EmailLog (EntityType, EntityId, ToEmail, Subject, SentBy, SentDate, Status)
                  VALUES (@Type, @Id, @To, @Subject, @By, GETDATE(), N'Sent')",
                new()
                {
                    ["Type"]    = entityType,
                    ["Id"]      = entityId,
                    ["To"]      = toEmail,
                    ["Subject"] = subject,
                    ["By"]      = sentBy,
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not write EmailLog for {EntityType} {EntityId}", entityType, entityId);
        }
    }

    private static string BuildQuoteBody(string reference, string company,
        string firstName, string originator) =>
        $"""
        <p>Dear {(string.IsNullOrEmpty(firstName) ? "Sir/Madam" : firstName)},</p>
        <p>Please find attached your quote from <strong>Techlight</strong>.</p>
        <p><strong>Quote Reference:</strong> {reference}</p>
        <p>If you have any questions, please don't hesitate to contact us.</p>
        <p>Kind regards,<br/>{originator}</p>
        <p style="color:#999;font-size:12px;">Sent via Techlight MyDesk.</p>
        """;
}
