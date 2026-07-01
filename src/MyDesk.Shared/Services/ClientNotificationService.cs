using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

/// <summary>
/// Sends SMS and email notifications to clients.
/// SMS: Twilio primary, falls back to email if SmsFallbackToEmail is set.
/// Email: SendGrid primary, falls back to SMTP if SmtpFallbackEnabled is set.
/// </summary>
public class ClientNotificationService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<ClientNotificationService> _logger;
    private NotificationSettings _cfg;

    public ClientNotificationService(
        IHttpClientFactory httpFactory,
        PlatformSettingsService platformSettings,
        ILogger<ClientNotificationService> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
        _cfg = platformSettings.Current.Notifications;
    }

    // Refresh config (PlatformSettings is scoped, call this when settings may have changed)
    public void RefreshConfig(NotificationSettings cfg) => _cfg = cfg;

    // -------------------------------------------------------------------------
    // SMS
    // -------------------------------------------------------------------------

    public async Task<NotificationResult> SendSmsAsync(string toNumber, string message)
    {
        if (!_cfg.EnableSms)
            return NotificationResult.Skipped("SMS notifications are disabled");

        var result = await TrySendTwilioAsync(toNumber, message);
        if (result.Success) return result;

        _logger.LogWarning("Twilio SMS to {To} failed: {Error}", toNumber, result.Error);

        if (_cfg.SmsFallbackToEmail && !string.IsNullOrWhiteSpace(toNumber))
        {
            // Best-effort: log that we cannot look up the email here — callers should use SendSmsOrEmailAsync
            return NotificationResult.Failure($"SMS failed ({result.Error}); no email fallback address provided — use SendSmsOrEmailAsync");
        }

        return result;
    }

    /// <summary>Sends SMS; if it fails, sends email to the fallback address.</summary>
    public async Task<NotificationResult> SendSmsOrEmailAsync(
        string toNumber, string toEmail, string subject, string message)
    {
        if (_cfg.EnableSms && !string.IsNullOrWhiteSpace(toNumber))
        {
            var smsResult = await TrySendTwilioAsync(toNumber, message);
            if (smsResult.Success) return smsResult;

            _logger.LogWarning("Twilio SMS to {To} failed: {Error} — falling back to email", toNumber, smsResult.Error);
        }

        if (!string.IsNullOrWhiteSpace(toEmail))
            return await SendEmailAsync(toEmail, subject, message);

        return NotificationResult.Failure("No valid SMS or email destination");
    }

    // -------------------------------------------------------------------------
    // Email
    // -------------------------------------------------------------------------

    public async Task<NotificationResult> SendEmailAsync(string toEmail, string subject, string body)
    {
        if (!_cfg.EnableEmail)
            return NotificationResult.Skipped("Email notifications are disabled");

        if (_cfg.EmailPrimaryProvider == "SendGrid" && !string.IsNullOrWhiteSpace(_cfg.SendGridApiKey))
        {
            var result = await TrySendGridAsync(toEmail, subject, body);
            if (result.Success) return result;

            _logger.LogWarning("SendGrid email to {To} failed: {Error}", toEmail, result.Error);

            if (_cfg.SmtpFallbackEnabled)
            {
                _logger.LogInformation("Falling back to SMTP for {To}", toEmail);
                return await TrySendSmtpAsync(toEmail, subject, body);
            }

            return result;
        }

        // SMTP direct
        return await TrySendSmtpAsync(toEmail, subject, body);
    }

    // -------------------------------------------------------------------------
    // Notification helpers (common triggers)
    // -------------------------------------------------------------------------

    public async Task<NotificationResult> NotifyInvoiceCreatedAsync(
        string toEmail, string? toPhone, string customerName, string invoiceNumber, decimal amount, DateTime dueDate)
    {
        if (!_cfg.NotifyOnInvoiceCreated) return NotificationResult.Skipped("Trigger disabled");

        var subject = $"Invoice {invoiceNumber} — {amount:C0} due {dueDate:dd MMM yyyy}";
        var body = $"Dear {customerName},\n\nPlease find your invoice {invoiceNumber} for {amount:C0} attached.\n\nPayment is due by {dueDate:dd MMM yyyy}.\n\nThank you for your business.";

        return await SendSmsOrEmailAsync(toPhone ?? "", toEmail, subject, body);
    }

    public async Task<NotificationResult> NotifyInvoiceOverdueAsync(
        string toEmail, string? toPhone, string customerName, string invoiceNumber, decimal amount, int daysOverdue)
    {
        if (!_cfg.NotifyOnInvoiceOverdue) return NotificationResult.Skipped("Trigger disabled");

        var subject = $"Overdue: Invoice {invoiceNumber} — {amount:C0} ({daysOverdue} days overdue)";
        var body = $"Dear {customerName},\n\nThis is a reminder that invoice {invoiceNumber} for {amount:C0} is now {daysOverdue} days overdue.\n\nPlease arrange payment at your earliest convenience.";

        return await SendSmsOrEmailAsync(toPhone ?? "", toEmail, subject, body);
    }

    public async Task<NotificationResult> NotifyDespatchAsync(
        string toEmail, string? toPhone, string customerName, string reference, string trackingInfo)
    {
        if (!_cfg.NotifyOnDespatch) return NotificationResult.Skipped("Trigger disabled");

        var subject = $"Your order {reference} has been despatched";
        var body = $"Dear {customerName},\n\nYour order {reference} has been despatched. {(string.IsNullOrWhiteSpace(trackingInfo) ? "" : $"Tracking: {trackingInfo}")}";

        return await SendSmsOrEmailAsync(toPhone ?? "", toEmail, subject, body);
    }

    public async Task<NotificationResult> NotifyJobStatusChangeAsync(
        string toEmail, string? toPhone, string customerName, string jobReference, string newStatus)
    {
        if (!_cfg.NotifyOnJobStatusChange) return NotificationResult.Skipped("Trigger disabled");

        var subject = $"Job {jobReference} — status updated to {newStatus}";
        var body = $"Dear {customerName},\n\nYour job {jobReference} has been updated. Current status: {newStatus}.";

        return await SendSmsOrEmailAsync(toPhone ?? "", toEmail, subject, body);
    }

    public async Task<NotificationResult> NotifyQuoteSentAsync(
        string toEmail, string? toPhone, string customerName, string quoteNumber, decimal amount, DateTime expiry)
    {
        if (!_cfg.NotifyOnQuoteSent) return NotificationResult.Skipped("Trigger disabled");

        var subject = $"Quote {quoteNumber} — {amount:C0} (valid until {expiry:dd MMM yyyy})";
        var body = $"Dear {customerName},\n\nPlease find quote {quoteNumber} for {amount:C0} attached. This quote is valid until {expiry:dd MMM yyyy}.\n\nIf you have any questions, please don't hesitate to contact us.";

        return await SendSmsOrEmailAsync(toPhone ?? "", toEmail, subject, body);
    }

    // -------------------------------------------------------------------------
    // Provider implementations
    // -------------------------------------------------------------------------

    private async Task<NotificationResult> TrySendTwilioAsync(string toNumber, string message)
    {
        if (string.IsNullOrWhiteSpace(_cfg.TwilioAccountSid) ||
            string.IsNullOrWhiteSpace(_cfg.TwilioAuthToken) ||
            string.IsNullOrWhiteSpace(_cfg.TwilioFromNumber))
            return NotificationResult.Failure("Twilio credentials not configured");

        try
        {
            var client = _httpFactory.CreateClient();
            var credentials = Convert.ToBase64String(
                Encoding.ASCII.GetBytes($"{_cfg.TwilioAccountSid}:{_cfg.TwilioAuthToken}"));
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

            var body = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["From"] = _cfg.TwilioFromNumber,
                ["To"]   = toNumber,
                ["Body"] = message
            });

            var response = await client.PostAsync(
                $"https://api.twilio.com/2010-04-01/Accounts/{_cfg.TwilioAccountSid}/Messages.json",
                body);

            if (response.IsSuccessStatusCode)
                return NotificationResult.Ok("SMS sent via Twilio");

            var err = await response.Content.ReadAsStringAsync();
            return NotificationResult.Failure($"Twilio HTTP {response.StatusCode}: {err[..Math.Min(err.Length, 200)]}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Twilio send failed to {To}", toNumber);
            return NotificationResult.Failure(ex.Message);
        }
    }

    private async Task<NotificationResult> TrySendGridAsync(string toEmail, string subject, string body)
    {
        try
        {
            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _cfg.SendGridApiKey);

            var fromEmail = string.IsNullOrWhiteSpace(_cfg.SendGridFromEmail) ? "noreply@mydesk.au" : _cfg.SendGridFromEmail;
            var fromName  = string.IsNullOrWhiteSpace(_cfg.SendGridFromName) ? "MyDesk" : _cfg.SendGridFromName;

            var payload = new
            {
                personalizations = new[] { new { to = new[] { new { email = toEmail } } } },
                from    = new { email = fromEmail, name = fromName },
                subject = subject,
                content = new[] { new { type = "text/plain", value = body } }
            };

            var json    = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("https://api.sendgrid.com/v3/mail/send", content);

            if (response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.Accepted)
                return NotificationResult.Ok("Email sent via SendGrid");

            var err = await response.Content.ReadAsStringAsync();
            return NotificationResult.Failure($"SendGrid HTTP {response.StatusCode}: {err[..Math.Min(err.Length, 200)]}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SendGrid send failed to {To}", toEmail);
            return NotificationResult.Failure(ex.Message);
        }
    }

    private async Task<NotificationResult> TrySendSmtpAsync(string toEmail, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(_cfg.SmtpHost))
            return NotificationResult.Failure("SMTP host not configured");

        try
        {
            using var client = new SmtpClient(_cfg.SmtpHost, _cfg.SmtpPort)
            {
                EnableSsl    = _cfg.SmtpUseSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Credentials  = string.IsNullOrWhiteSpace(_cfg.SmtpUsername)
                    ? null
                    : new NetworkCredential(_cfg.SmtpUsername, _cfg.SmtpPassword)
            };

            var fromAddr  = _cfg.SmtpFromEmail ?? "noreply@mydesk.au";
            var fromName  = _cfg.SmtpFromName ?? "MyDesk";
            var mailMsg   = new MailMessage(new MailAddress(fromAddr, fromName), new MailAddress(toEmail))
            {
                Subject = subject,
                Body    = body,
                IsBodyHtml = false
            };

            await client.SendMailAsync(mailMsg);
            return NotificationResult.Ok("Email sent via SMTP");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SMTP send failed to {To}", toEmail);
            return NotificationResult.Failure(ex.Message);
        }
    }
}

public record NotificationResult(bool Success, string Message, string? Error = null)
{
    public static NotificationResult Ok(string msg) => new(true, msg);
    public static NotificationResult Failure(string err) => new(false, "", err);
    public static NotificationResult Skipped(string reason) => new(true, $"Skipped: {reason}");
}
