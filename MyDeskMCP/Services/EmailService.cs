using System.Net.Mail;
using System.Net;
using Techlight.MyDesk.MCP.Models;

namespace Techlight.MyDesk.MCP.Services;

public class EmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly DatabaseService _db;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger, DatabaseService db)
    {
        _configuration = configuration;
        _logger = logger;
        _db = db;
    }

    public async Task<bool> EmailQuoteAsync(int quoteId, string toEmail, string? subject = null, 
        string? message = null, bool includePdf = true, McpContext? context = null)
    {
        try
        {
            // Get quote details
            var quoteSql = @"
                SELECT q.Qid, q.Reference, c.CompanyName, c.Email, u.Name as Originator, u.Email as OriginatorEmail
                FROM Quotes q
                INNER JOIN Contacts c ON q.ContactId = c.ContactId
                INNER JOIN Users u ON q.Code = u.Code
                WHERE q.Qid = @QuoteId";

            var dt = await _db.ExecuteQueryAsync(quoteSql, new Dictionary<string, object>
            {
                ["QuoteId"] = quoteId
            });

            if (dt.Rows.Count == 0)
                throw new Exception($"Quote {quoteId} not found");

            var row = dt.Rows[0];
            var reference = row["Reference"].ToString()!;
            var companyName = row["CompanyName"].ToString()!;
            var originator = row["Originator"].ToString()!;
            var originatorEmail = row["OriginatorEmail"]?.ToString();

            // Build email
            var emailSubject = subject ?? $"Quote {quoteId} - {reference}";
            var emailBody = message ?? $@"
                <h3>Quote from {companyName}</h3>
                <p>Please find attached Quote {quoteId} - {reference}</p>
                <p>If you have any questions, please contact {originator} at {originatorEmail}</p>
                <p>---<br/>
                This quote was sent via Techlight MyDesk</p>";

            // Send email
            await SendEmailAsync(toEmail, emailSubject, emailBody, originatorEmail, includePdf ? quoteId : null);

            // Log the email
            await LogEmailAsync(quoteId, toEmail, emailSubject, context?.UserCode ?? "SYSTEM");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error emailing quote {QuoteId}", quoteId);
            return false;
        }
    }

    public async Task<bool> SendQuoteToContactAsync(int quoteId, string? additionalMessage = null, McpContext? context = null)
    {
        try
        {
            // Get quote and contact details
            var sql = @"
                SELECT q.Qid, q.Reference, c.CompanyName, c.Email, c.FirstName, c.Surname,
                       u.Name as Originator, u.Email as OriginatorEmail
                FROM Quotes q
                INNER JOIN Contacts c ON q.ContactId = c.ContactId
                INNER JOIN Users u ON q.Code = u.Code
                WHERE q.Qid = @QuoteId";

            var dt = await _db.ExecuteQueryAsync(sql, new Dictionary<string, object>
            {
                ["QuoteId"] = quoteId
            });

            if (dt.Rows.Count == 0)
                throw new Exception($"Quote {quoteId} not found");

            var row = dt.Rows[0];
            var contactEmail = row["Email"]?.ToString();
            
            if (string.IsNullOrEmpty(contactEmail))
                throw new Exception("Contact has no email address");

            var firstName = row["FirstName"]?.ToString() ?? "";
            var reference = row["Reference"].ToString()!;
            var companyName = row["CompanyName"].ToString()!;
            var originator = row["Originator"].ToString()!;

            var emailBody = $@"
                <p>Dear {firstName},</p>
                <p>Please find attached your quote from {companyName}.</p>
                <p>Quote Reference: {reference}</p>";

            if (!string.IsNullOrEmpty(additionalMessage))
            {
                emailBody += $"<p>{additionalMessage}</p>";
            }

            emailBody += $@"
                <p>If you have any questions, please don't hesitate to contact me.</p>
                <p>Best regards,<br/>{originator}</p>";

            var emailSubject = $"Quote - {reference}";

            await SendEmailAsync(contactEmail, emailSubject, emailBody, row["OriginatorEmail"]?.ToString(), quoteId);
            await LogEmailAsync(quoteId, contactEmail, emailSubject, context?.UserCode ?? "SYSTEM");

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending quote {QuoteId} to contact", quoteId);
            return false;
        }
    }

    private async Task SendEmailAsync(string toEmail, string subject, string body, string? fromEmail = null, int? quoteId = null)
    {
        // Get SMTP settings from configuration
        var smtpHost = _configuration["Email:SmtpHost"] ?? "localhost";
        var smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "25");
        var smtpUser = _configuration["Email:SmtpUser"];
        var smtpPass = _configuration["Email:SmtpPass"];
        var fromAddress = fromEmail ?? _configuration["Email:FromAddress"] ?? "noreply@techlight.com.au";

        using var client = new SmtpClient(smtpHost, smtpPort);
        
        if (!string.IsNullOrEmpty(smtpUser) && !string.IsNullOrEmpty(smtpPass))
        {
            client.Credentials = new NetworkCredential(smtpUser, smtpPass);
            client.EnableSsl = true;
        }

        var mailMessage = new MailMessage
        {
            From = new MailAddress(fromAddress, "Techlight MyDesk"),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };
        mailMessage.To.Add(toEmail);

        // If quoteId provided, you could generate and attach PDF here
        // For now, this is a placeholder - you'd integrate with your PDF generation
        if (quoteId.HasValue)
        {
            _logger.LogInformation("PDF attachment for quote {QuoteId} would be generated here", quoteId.Value);
            // In production, generate PDF and add: mailMessage.Attachments.Add(new Attachment(pdfStream, $"Quote_{quoteId}.pdf"));
        }

        await client.SendMailAsync(mailMessage);
    }

    private async Task LogEmailAsync(int quoteId, string toEmail, string subject, string sentBy)
    {
        try
        {
            var sql = @"
                INSERT INTO EmailLog (QuoteId, ToEmail, Subject, SentBy, SentDate, Status)
                VALUES (@QuoteId, @ToEmail, @Subject, @SentBy, GETDATE(), 'Sent')";

            await _db.ExecuteNonQueryAsync(sql, new Dictionary<string, object>
            {
                ["QuoteId"] = quoteId,
                ["ToEmail"] = toEmail,
                ["Subject"] = subject,
                ["SentBy"] = sentBy
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging email for quote {QuoteId}", quoteId);
        }
    }

    public async Task<string> GenerateQuoteSummaryAsync(int quoteId)
    {
        // Get quote with line items for a text summary
        var sql = @"
            SELECT q.Qid, q.Reference, c.CompanyName, q.NettPriceTotal, q.QuoteDate
            FROM Quotes q
            INNER JOIN Contacts c ON q.ContactId = c.ContactId
            WHERE q.Qid = @QuoteId";

        var dt = await _db.ExecuteQueryAsync(sql, new Dictionary<string, object>
        {
            ["QuoteId"] = quoteId
        });

        if (dt.Rows.Count == 0)
            return "Quote not found";

        var row = dt.Rows[0];
        var total = Convert.ToDecimal(row["NettPriceTotal"]);

        var summary = $@"
Quote Summary:
--------------
Quote ID: {row["Qid"]}
Reference: {row["Reference"]}
Customer: {row["CompanyName"]}
Date: {Convert.ToDateTime(row["QuoteDate"]):dd/MM/yyyy}
Total Amount: ${total:N2}

To view full details, please log into Techlight MyDesk.";

        return summary;
    }
}
