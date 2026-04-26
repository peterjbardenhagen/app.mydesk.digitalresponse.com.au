using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

/// <summary>
/// SMTP email sending with EmailLog persistence and UserActivity tracking.
/// Ported from MyDeskMCP/Services/EmailService.cs into the shared library.
/// Config keys: Email:SmtpHost, Email:SmtpPort, Email:SmtpUser, Email:SmtpPass,
///              Email:FromAddress, Email:FromName
/// </summary>
public class EmailService
{
    private readonly DatabaseService _db;
    private readonly ActivityService? _activity;
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private readonly PlatformSettings? _platformSettings;

    public EmailService(DatabaseService db, ActivityService activity,
        IConfiguration config, ILogger<EmailService> logger)
    {
        _db       = db;
        _activity = activity;
        _config   = config;
        _logger   = logger;
    }

    // Constructor for when PlatformSettingsService is available
    public EmailService(DatabaseService db, ActivityService activity,
        IConfiguration config, ILogger<EmailService> logger, PlatformSettings platformSettings)
    {
        _db                = db;
        _activity          = activity;
        _config            = config;
        _logger            = logger;
        _platformSettings  = platformSettings;
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
                       ISNULL(co.Company,'')       AS Company,
                       ISNULL(c.FirstName,'')      AS FirstName,
                       ISNULL(u.Name,'')           AS OriginatorName,
                       ISNULL(u.Email,'')          AS OriginatorEmail
                FROM Quotes q
                LEFT JOIN Contacts c ON c.ContactId = q.ContactId
                LEFT JOIN Companies co ON co.CompanyId = q.CompanyId
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

            var status = IsOutboundEnabled ? "Sent" : "Blocked (prod safety)";
            await LogEmailAsync("Quote", quoteId, toEmail, emailSubject, senderCode, status);
            await _activity.LogAsync(senderCode, "Quote", quoteId,
                IsOutboundEnabled ? $"Emailed quote to {toEmail}" : $"Quote email blocked (prod safety) for {toEmail}", reference);

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
                SELECT CAST(i.InvoiceId AS NVARCHAR(20)) AS InvoiceNumber,
                       COALESCE(NULLIF(co.Company, ''), NULLIF(i.InvCompany, ''), NULLIF(i.DelCompany, ''), '') AS Company,
                       ISNULL(c.FirstName,'')      AS FirstName,
                       ISNULL(u.Name,'')           AS OriginatorName,
                       ISNULL(u.Email,'')          AS OriginatorEmail
                FROM Invoices i
                LEFT JOIN Contacts c ON c.ContactId = i.ContactId
                LEFT JOIN Companies co ON co.CompanyId = i.CompanyId
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
                SELECT CAST(p.POid AS NVARCHAR(20)) AS PONumber,
                       COALESCE(NULLIF(s.Company,''), NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(c.FirstName,''), ' ', ISNULL(c.Surname,'')))), ''), '') AS SupplierName,
                       ISNULL(u.Name,'')                   AS OriginatorName,
                       ISNULL(u.Email,'')                  AS OriginatorEmail
                FROM PurchaseOrders p
                LEFT JOIN Contacts c ON c.ContactId = p.ContactId
                LEFT JOIN Companies s ON s.CompanyId = c.CompanyId
                LEFT JOIN Users u    ON u.Code = p.Code
                WHERE p.POid = @Id",
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

    /// <summary>
    /// Returns true if outbound email sending is enabled for this environment.
    /// Checks in order:
    /// 1. Hidden developer-only override (EMAIL_DEV_OVERRIDE_DISABLE=true) - highest priority
    /// 2. Platform Settings kill switch (DisableAllEmails)
    /// 3. Configuration setting (Email:DisableOutbound)
    /// </summary>
    public bool IsOutboundEnabled
    {
        get
        {
            // Hidden developer-only kill switch - only developers know about this
            // This takes precedence over everything for absolute safety during testing
            if (string.Equals(_config["EMAIL_DEV_OVERRIDE_DISABLE"], "true", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Emails disabled via hidden developer override (EMAIL_DEV_OVERRIDE_DISABLE=true)");
                return false;
            }

            // Platform Settings kill switch - accessible to admins via UI
            if (_platformSettings?.DisableAllEmails == true)
            {
                _logger.LogWarning("Emails disabled via Platform Settings (DisableAllEmails=true)");
                return false;
            }

            // Fallback to config check for platform settings if not injected
            if (string.Equals(_config["Platform:DisableAllEmails"], "true", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Emails disabled via Platform Settings config (Platform:DisableAllEmails=true)");
                return false;
            }

            // Configuration-based disable (legacy)
            if (string.Equals(_config["Email:DisableOutbound"], "true", StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Emails disabled via configuration (Email:DisableOutbound=true)");
                return false;
            }

            return true;
        }
    }

    public async Task SendAsync(
        string to, string subject, string htmlBody,
        string? fromEmail = null, string? fromName = null,
        byte[]? attachment = null, string? attachmentName = null)
    {
        // Production safety: if outbound is disabled, log and return without sending.
        if (!IsOutboundEnabled)
        {
            _logger.LogWarning("Outbound email disabled (Email:DisableOutbound=true). Skipping send to {To} — subject: {Subject}",
                to, subject);
            return;
        }

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
        string toEmail, string subject, string sentBy, string status = "Sent")
    {
        try
        {
            await _db.ExecuteAsync(
                @"INSERT INTO EmailLog (EntityType, EntityId, ToEmail, Subject, SentBy, SentDate, Status)
                  VALUES (@Type, @Id, @To, @Subject, @By, GETDATE(), @Status)",
                new()
                {
                    ["Type"]    = entityType,
                    ["Id"]      = entityId,
                    ["To"]      = toEmail,
                    ["Subject"] = subject,
                    ["By"]      = sentBy,
                    ["Status"]  = status,
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

    public async Task SendPasswordResetEmailAsync(string email, string resetLink)
    {
        var subject = "Password Reset - Techlight MyDesk";
        var body = $"""
            <p>Dear User,</p>
            <p>You have requested a password reset for your Techlight MyDesk account.</p>
            <p>Please click the link below to reset your password:</p>
            <p><a href="{resetLink}">Reset Password</a></p>
            <p>If you did not request this password reset, please ignore this email.</p>
            <p>This link will expire in 24 hours.</p>
            <p>Kind regards,<br/>Techlight MyDesk Team</p>
            <p style="color:#999;font-size:12px;">Sent via Techlight MyDesk.</p>
            """;

        await SendAsync(email, subject, body);
        _logger.LogInformation("Password reset email sent to {Email}", email);
    }
}
