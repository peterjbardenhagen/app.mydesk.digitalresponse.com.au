using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class EmailService
{
    private readonly DatabaseService _db;
    private readonly ActivityService? _activity;
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;
    private readonly PlatformSettings? _platformSettings;
    private readonly ICurrentTenantAccessor? _tenantAccessor;

    public EmailService(DatabaseService db, ActivityService activity,
        IConfiguration config, ILogger<EmailService> logger)
    {
        _db       = db;
        _activity = activity;
        _config   = config;
        _logger   = logger;
    }

    public EmailService(DatabaseService db, ActivityService activity,
        IConfiguration config, ILogger<EmailService> logger, PlatformSettings platformSettings)
    {
        _db                = db;
        _activity          = activity;
        _config            = config;
        _logger            = logger;
        _platformSettings  = platformSettings;
    }

    public EmailService(DatabaseService db, ActivityService activity,
        IConfiguration config, ILogger<EmailService> logger,
        PlatformSettings platformSettings, ICurrentTenantAccessor tenantAccessor)
    {
        _db               = db;
        _activity         = activity;
        _config           = config;
        _logger           = logger;
        _platformSettings = platformSettings;
        _tenantAccessor   = tenantAccessor;
    }

    /// <summary>
    /// Returns the redirect address if outbound email must be diverted, otherwise null.
    /// Triggers:
    ///   * The current tenant is the Demo MyDesk sandbox (always redirects).
    ///   * <c>Email:RedirectAllTo</c> is set in configuration (overrides everything).
    ///   * <c>Email:RedirectInDevelopment</c> is true and we're in Development.
    /// </summary>
    private string? GetEmailRedirectTarget()
    {
        // Explicit config override always wins (used by test runs / staging).
        var redirectAll = _config["Email:RedirectAllTo"];
        if (!string.IsNullOrWhiteSpace(redirectAll)) return redirectAll;

        // Demo tenant always redirects to peter@bardenhagen.xyz.
        if (_tenantAccessor?.TenantId == TenantConstants.DemoTenantId)
            return "peter@bardenhagen.xyz";

        return null;
    }

    private string GetPlatformName() => _platformSettings?.CompanyName ?? _config["PlatformSettings:CompanyName"] ?? "MyDesk";
    
    private string GetFromDomain()
    {
        var website = _platformSettings?.CompanyWebsite ?? _config["PlatformSettings:CompanyWebsite"] ?? "mydesk.com.au";
        return website.Replace("https://", "").Replace("http://", "").TrimEnd('/');
    }

    public async Task EnsureTablesAsync()
    {
        try
        {
            await _db.ExecuteNonQueryAsync(@"
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
                SELECT q.Reference, co.Company, c.FirstName, u.Name AS OriginatorName, u.Email AS OriginatorEmail
                FROM Quotes q
                LEFT JOIN Contacts c ON c.ContactId = q.ContactId
                LEFT JOIN Companies co ON co.CompanyId = c.CompanyId
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

            var platformName = GetPlatformName();
            var emailSubject = subject ?? $"Quote — {reference}";
            var emailBody    = message ?? BuildQuoteBody(quoteId, reference, company, firstName, originator, platformName);

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

    private string BuildQuoteBody(int quoteId, string reference, string company,
        string firstName, string originator, string platformName)
    {
        var host = _config["App:BaseUrl"] ?? "https://mydesk.digitalresponse.com.au";
        var approveLink = $"{host}/quotes/{quoteId}/action/approve";
        var declineLink = $"{host}/quotes/{quoteId}/action/decline";
        
        var signature = BuildEmailSignature(originator);
        
        return $"""
        <div style="font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;line-height:1.5;color:#0f172a;">
            <p>Dear {(string.IsNullOrEmpty(firstName) ? "Sir/Madam" : firstName)},</p>
            <p>Thank you for your enquiry. Please find attached your quote from <strong>{platformName}</strong>.</p>
            <p><strong>Quote Reference:</strong> {reference}</p>
            <p><strong>Company:</strong> {company}</p>
            
            <div style="background:#f8fafc;border:1px solid #e2e8f0;border-radius:8px;padding:16px;margin:24px 0;">
                <p style="margin:0 0 12px;font-weight:600;color:#475569;">Please review and respond:</p>
                <a href="{approveLink}" style="display:inline-block;background:#00a0a0;color:#ffffff;padding:10px 24px;text-decoration:none;border-radius:6px;font-weight:600;font-size:14px;margin-right:12px;">&#10003; Accept Quote</a>
                <a href="{declineLink}" style="display:inline-block;background:#ef4444;color:#ffffff;padding:10px 24px;text-decoration:none;border-radius:6px;font-weight:600;font-size:14px;">&#10007; Decline Quote</a>
            </div>

            <p>If you have any questions or would like to discuss this quote, please don't hesitate to contact us.</p>
            
            <p>Kind regards,<br/>{originator}</p>
            
            {signature}
        </div>
        """;
    }

    private string BuildEmailSignature(string senderName)
    {
        var domain = GetFromDomain();
        var phone = _config["Email:SignaturePhone"] ?? "";
        var email = _config["Email:SignatureEmail"] ?? "";
        var address = _config["Email:SignatureAddress"] ?? "";
        var website = $"https://{domain}";
        var logoUrl = _config["Email:SignatureLogoUrl"] ?? $"https://{domain}/images/logo.png";
        var tagline = _config["Email:SignatureTagline"] ?? "";
        var senderTitle = _config["Email:SignatureTitle"] ?? "";
        
        var titleRow = !string.IsNullOrEmpty(senderTitle) ? $"""
                        <tr>
                            <td style="padding-bottom:8px;">
                                <span style="font-size:9px;font-weight:700;color:#00a0a0;text-transform:uppercase;letter-spacing:0.1em;">{senderTitle}</span>
                            </td>
                        </tr>
        """ : "";
        
        var phoneRow = !string.IsNullOrEmpty(phone) ? $"""
                        <tr>
                            <td style="font-size:10px;font-weight:500;color:#475569;padding-bottom:3px;">
                                <a href="tel:{phone.Replace(" ", "")}" style="color:#475569;text-decoration:none;">{phone}</a>
                            </td>
                        </tr>
        """ : "";
        
        var emailRow = !string.IsNullOrEmpty(email) ? $"""
                        <tr>
                            <td style="font-size:10px;font-weight:500;color:#475569;padding-bottom:3px;">
                                <a href="mailto:{email}" style="color:#475569;text-decoration:none;">{email}</a>
                            </td>
                        </tr>
        """ : "";
        
        var addressRow = !string.IsNullOrEmpty(address) ? $"""
                        <tr>
                            <td style="font-size:10px;font-weight:500;color:#64748b;padding-bottom:3px;">{address}</td>
                        </tr>
        """ : "";
        
        var taglineBlock = !string.IsNullOrEmpty(tagline) ? $"""
                    <table cellpadding="0" cellspacing="0" border="0" style="margin-top:10px;max-width:340px;">
                        <tr>
                            <td style="padding-top:8px;border-top:1px solid #e2e8f0;">
                                <span style="font-size:9px;font-style:italic;font-weight:500;color:#334155;">"{tagline}"</span>
                            </td>
                        </tr>
                    </table>
        """ : "";
        
        return $"""
        <table cellpadding="0" cellspacing="0" border="0" style="background:#ffffff;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;line-height:1.4;margin-top:20px;">
            <tr>
                <td style="padding:0;">
                    <table cellpadding="0" cellspacing="0" border="0">
                        <tr>
                            <td style="padding-bottom:8px;">
                                <a href="{website}" style="text-decoration:none;display:block;">
                                    <img src="{logoUrl}" alt="{domain}" width="44" height="44" style="display:block;border:0;" />
                                </a>
                            </td>
                        </tr>
                    </table>
                    <table cellpadding="0" cellspacing="0" border="0">
                        <tr>
                            <td style="padding-bottom:1px;">
                                <span style="font-size:15px;font-weight:800;color:#0f172a;letter-spacing:-0.02em;">{senderName}</span>
                            </td>
                        </tr>
                        {titleRow}
                    </table>
                    <table cellpadding="0" cellspacing="0" border="0" style="width:100%;">
                        <tr>
                            <td style="height:1px;background:linear-gradient(90deg,#00a0a0 0%,transparent 100%);padding:0;"></td>
                        </tr>
                    </table>
                    <table cellpadding="0" cellspacing="0" border="0" style="margin-top:8px;">
                        {phoneRow}
                        {emailRow}
                        {addressRow}
                    </table>
                    <table cellpadding="0" cellspacing="0" border="0" style="margin-top:6px;">
                        <tr>
                            <td style="font-size:10px;font-weight:500;color:#475569;padding-bottom:3px;">
                                <a href="{website}" style="color:#00a0a0;text-decoration:none;font-weight:600;">{domain}</a>
                            </td>
                        </tr>
                    </table>
                    {taglineBlock}
                </td>
            </tr>
        </table>
        <table cellpadding="0" cellspacing="0" border="0" style="margin-top:10px;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;">
            <tr>
                <td>
                    <span style="font-size:8px;color:#94a3b8;line-height:1.3;display:block;max-width:340px;">
                        <strong style="color:#64748b;">Disclaimer:</strong> This email and any attachments may be confidential. If received in error, please delete and inform the sender by return email.
                    </span>
                </td>
            </tr>
        </table>
        """;
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

            var platformName = GetPlatformName();
            var emailSubject = subject ?? $"Invoice — {invNum}";
            var emailBody    = message ?? $"""
                <div style="font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;line-height:1.5;color:#0f172a;">
                    <p>Dear {(string.IsNullOrEmpty(firstName) ? "Sir/Madam" : firstName)},</p>
                    <p>Please find attached Invoice <strong>{invNum}</strong> from {platformName}.</p>
                    <p>If you have any questions, please don't hesitate to contact us.</p>
                    <p>Kind regards,<br/>{originator}</p>
                    {BuildEmailSignature(originator)}
                </div>
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

            var platformName = GetPlatformName();
            var emailSubject = subject ?? $"Purchase Order — {poNum}";
            var emailBody    = message ?? $"""
                <div style="font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;line-height:1.5;color:#0f172a;">
                    <p>Dear {supplier},</p>
                    <p>Please find attached Purchase Order <strong>{poNum}</strong> from {platformName}.</p>
                    <p>Kind regards,<br/>{originator}</p>
                    {BuildEmailSignature(originator)}
                </div>
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

    public bool IsOutboundEnabled
    {
        get
        {
            if (string.Equals(_config["EMAIL_DEV_OVERRIDE_DISABLE"], "true", StringComparison.OrdinalIgnoreCase))
                return false;
            if (_platformSettings?.DisableAllEmails == true)
                return false;
            if (string.Equals(_config["Platform:DisableAllEmails"], "true", StringComparison.OrdinalIgnoreCase))
                return false;
            if (string.Equals(_config["Email:DisableOutbound"], "true", StringComparison.OrdinalIgnoreCase))
                return false;
            return true;
        }
    }

    public async Task SendAsync(
        string to, string subject, string htmlBody,
        string? fromEmail = null, string? fromName = null,
        byte[]? attachment = null, string? attachmentName = null,
        string? bcc = null)
    {
        if (!IsOutboundEnabled) return;

        // ── Redirect guard ──────────────────────────────────────────────────
        // If we're in the Demo tenant (or Email:RedirectAllTo is configured), every
        // recipient is replaced with the safe address and the original recipients
        // are noted in the subject + body so the test reader can see what would have
        // happened in production.
        var redirectTarget = GetEmailRedirectTarget();
        if (!string.IsNullOrWhiteSpace(redirectTarget))
        {
            _logger.LogInformation(
                "Email redirected: original to={OriginalTo} bcc={OriginalBcc} -> {RedirectTo} (tenant={Tenant})",
                to, bcc ?? "-", redirectTarget, _tenantAccessor?.TenantName ?? "n/a");

            subject = $"[REDIRECTED:{to}] {subject}";
            htmlBody =
                $"<div style='background:#fde68a;color:#78350f;padding:10px;border-radius:6px;margin-bottom:14px;font-family:sans-serif;'>" +
                $"<strong>This email was redirected.</strong><br/>" +
                $"Original To: <code>{System.Net.WebUtility.HtmlEncode(to)}</code><br/>" +
                (string.IsNullOrWhiteSpace(bcc) ? "" : $"Original Bcc: <code>{System.Net.WebUtility.HtmlEncode(bcc)}</code><br/>") +
                $"Tenant: <code>{System.Net.WebUtility.HtmlEncode(_tenantAccessor?.TenantName ?? "n/a")}</code>" +
                $"</div>" + htmlBody;
            to  = redirectTarget;
            bcc = null;
        }

        var platformName = GetPlatformName();
        var smtpHost    = _config["Email:SmtpHost"] ?? "localhost";
        var smtpPort    = int.Parse(_config["Email:SmtpPort"] ?? "25");
        var smtpUser    = _config["Email:SmtpUser"];
        var smtpPass    = _config["Email:SmtpPass"];
        var defaultFrom = _config["Email:FromAddress"] ?? $"noreply@{GetFromDomain()}";
        var displayName = fromName ?? _config["Email:FromName"] ?? $"{platformName} MyDesk";
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
        if (!string.IsNullOrWhiteSpace(bcc)) msg.Bcc.Add(bcc);

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
            await _db.ExecuteNonQueryAsync(
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

    public async Task SendPasswordResetEmailAsync(string email, string resetLink)
    {
        var platformName = GetPlatformName();
        var subject = $"Password Reset - {platformName} MyDesk";
        var body = $"""
            <div style="font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,Helvetica,Arial,sans-serif;line-height:1.5;color:#0f172a;">
                <p>Dear User,</p>
                <p>You have requested a password reset for your {platformName} MyDesk account.</p>
                <p>Please click the link below to reset your password:</p>
                <p><a href="{resetLink}" style="display:inline-block;background:#00a0a0;color:#ffffff;padding:10px 24px;text-decoration:none;border-radius:6px;font-weight:600;font-size:14px;margin:12px 0;">Reset Password</a></p>
                <p>If you did not request this password reset, please ignore this email.</p>
                <p>This link will expire in 24 hours.</p>
                <p>Kind regards,<br/>{platformName} MyDesk Team</p>
                {BuildEmailSignature($"{platformName} MyDesk Team")}
            </div>
            """;

        await SendAsync(email, subject, body);
        _logger.LogInformation("Password reset email sent to {Email}", email);
    }
}
