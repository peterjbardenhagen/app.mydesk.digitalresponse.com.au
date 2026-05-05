using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MyDesk.Shared.Models;

namespace MyDesk.Web.Services;

public class OutlookInboxService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly AzureAIOptions _aiOptions;
    private readonly ILogger<OutlookInboxService> _logger;

    public OutlookInboxService(
        IHttpClientFactory httpFactory,
        IOptions<AzureAIOptions> aiOptions,
        ILogger<OutlookInboxService> logger)
    {
        _httpFactory = httpFactory;
        _aiOptions = aiOptions.Value;
        _logger = logger;
    }

    public async Task<OutlookInboxSnapshot> GetSnapshotAsync(PlatformSettings settings, CancellationToken ct = default)
    {
        var mailbox = SelectMailbox(settings);
        if (mailbox is null)
        {
            return new OutlookInboxSnapshot(false, null,
                "No Outlook integration is connected. Connect Outlook in Integrations to view your inbox.",
                Array.Empty<OutlookInboxMessage>(),
                Array.Empty<OutlookEmailRecommendation>(),
                "");
        }

        if (string.IsNullOrWhiteSpace(mailbox.Settings.AccessToken))
        {
            return new OutlookInboxSnapshot(true, mailbox.DisplayName,
                $"{mailbox.DisplayName} is enabled, but there is no stored Microsoft Graph access token. Reconnect it in Integrations.",
                Array.Empty<OutlookInboxMessage>(),
                Array.Empty<OutlookEmailRecommendation>(),
                "");
        }

        try
        {
            var messages = await FetchInboxMessagesAsync(mailbox.Settings.AccessToken!, ct);
            var unread = messages.Where(x => !x.IsRead).ToList();
            var recommendations = unread
                .Where(x => string.Equals(x.Importance, "high", StringComparison.OrdinalIgnoreCase))
                .Take(5)
                .Select(x => new OutlookEmailRecommendation(
                    x.Subject,
                    x.From,
                    $"Unread high-priority email from {x.From}. Review and respond as soon as possible.",
                    x.WebLink))
                .ToList();

            var summary = await BuildUnreadSummaryAsync(mailbox.DisplayName, unread, ct);

            return new OutlookInboxSnapshot(true, mailbox.DisplayName, null, messages, recommendations, summary);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load Outlook inbox for {Mailbox}", mailbox.DisplayName);
            return new OutlookInboxSnapshot(true, mailbox.DisplayName,
                $"Outlook is connected, but inbox access failed: {ex.Message}",
                Array.Empty<OutlookInboxMessage>(),
                Array.Empty<OutlookEmailRecommendation>(),
                "");
        }
    }

    private static ConnectedMailbox? SelectMailbox(PlatformSettings settings)
    {
        var mailboxes = new[]
        {
            new ConnectedMailbox("My Outlook", settings.MyOutlook),
            new ConnectedMailbox("Shared Outlook Mailbox", settings.Outlook)
        };

        return mailboxes.FirstOrDefault(x => x.Settings.Enabled || x.Settings.IsConnected);
    }

    private async Task<List<OutlookInboxMessage>> FetchInboxMessagesAsync(string accessToken, CancellationToken ct)
    {
        var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        const string url = "https://graph.microsoft.com/v1.0/me/mailFolders/inbox/messages?$top=25&$orderby=receivedDateTime desc&$select=id,subject,from,receivedDateTime,isRead,importance,hasAttachments,bodyPreview,webLink";
        using var response = await http.GetAsync(url, ct);
        var payload = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Microsoft Graph returned {(int)response.StatusCode} {response.ReasonPhrase}.");

        using var doc = JsonDocument.Parse(payload);
        var items = new List<OutlookInboxMessage>();
        if (!doc.RootElement.TryGetProperty("value", out var value) || value.ValueKind != JsonValueKind.Array)
            return items;

        foreach (var item in value.EnumerateArray())
        {
            var from = item.TryGetProperty("from", out var fromEl)
                       && fromEl.TryGetProperty("emailAddress", out var emailEl)
                       && emailEl.TryGetProperty("address", out var addressEl)
                ? addressEl.GetString() ?? "Unknown"
                : "Unknown";

            var received = item.TryGetProperty("receivedDateTime", out var receivedEl)
                && DateTimeOffset.TryParse(receivedEl.GetString(), out var dto)
                    ? dto.LocalDateTime
                    : DateTime.Now;

            items.Add(new OutlookInboxMessage(
                item.TryGetProperty("id", out var idEl) ? idEl.GetString() ?? string.Empty : string.Empty,
                from,
                item.TryGetProperty("subject", out var subjectEl) ? subjectEl.GetString() ?? "(No subject)" : "(No subject)",
                received,
                item.TryGetProperty("isRead", out var readEl) && readEl.GetBoolean(),
                item.TryGetProperty("importance", out var importanceEl) ? importanceEl.GetString() ?? "normal" : "normal",
                item.TryGetProperty("hasAttachments", out var attachEl) && attachEl.GetBoolean(),
                item.TryGetProperty("bodyPreview", out var previewEl) ? previewEl.GetString() ?? string.Empty : string.Empty,
                item.TryGetProperty("webLink", out var linkEl) ? linkEl.GetString() ?? string.Empty : string.Empty));
        }

        return items;
    }

    private async Task<string> BuildUnreadSummaryAsync(string mailboxName, IReadOnlyList<OutlookInboxMessage> unread, CancellationToken ct)
    {
        if (unread.Count == 0)
            return $"{mailboxName} has no unread emails right now.";

        if (!_aiOptions.IsConfigured)
            return BuildFallbackSummary(mailboxName, unread);

        var prompt = new StringBuilder();
        prompt.AppendLine($"Mailbox: {mailboxName}");
        prompt.AppendLine("Unread emails:");

        foreach (var email in unread.Take(12))
        {
            prompt.AppendLine($"- From: {email.From}");
            prompt.AppendLine($"  Subject: {email.Subject}");
            prompt.AppendLine($"  Importance: {email.Importance}");
            prompt.AppendLine($"  Received: {email.ReceivedAt:dd MMM yyyy HH:mm}");
            prompt.AppendLine($"  Preview: {email.BodyPreview}");
        }

        var http = _httpFactory.CreateClient();
        http.DefaultRequestHeaders.Add("api-key", _aiOptions.OpenAIApiKey);

        var body = new
        {
            messages = new object[]
            {
                new
                {
                    role = "system",
                    content = "You summarise inboxes in Australian English. Return a concise summary of unread emails in 3 to 5 bullet points, then a short final line calling out any urgent follow-up."
                },
                new
                {
                    role = "user",
                    content = prompt.ToString()
                }
            },
            max_completion_tokens = 500,
            temperature = 0.2
        };

        using var response = await http.PostAsync(
            _aiOptions.ChatCompletionsUrl,
            new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json"),
            ct);

        var payload = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Azure AI inbox summary returned {Status}: {Body}", response.StatusCode, payload);
            return BuildFallbackSummary(mailboxName, unread);
        }

        using var doc = JsonDocument.Parse(payload);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
        return string.IsNullOrWhiteSpace(content) ? BuildFallbackSummary(mailboxName, unread) : content.Trim();
    }

    private static string BuildFallbackSummary(string mailboxName, IReadOnlyList<OutlookInboxMessage> unread)
    {
        var highPriority = unread.Count(x => string.Equals(x.Importance, "high", StringComparison.OrdinalIgnoreCase));
        var latestSenders = string.Join(", ", unread.Select(x => x.From).Distinct(StringComparer.OrdinalIgnoreCase).Take(4));

        return $"{mailboxName} has {unread.Count} unread emails. {highPriority} are marked high priority. Recent senders include {latestSenders}. Focus first on unread high-priority messages and anything received today.";
    }

    private sealed record ConnectedMailbox(string DisplayName, IntegrationSettings Settings);
}

public record OutlookInboxSnapshot(
    bool IsConnected,
    string? MailboxName,
    string? StatusMessage,
    IReadOnlyList<OutlookInboxMessage> Messages,
    IReadOnlyList<OutlookEmailRecommendation> Recommendations,
    string Summary);

public record OutlookInboxMessage(
    string Id,
    string From,
    string Subject,
    DateTime ReceivedAt,
    bool IsRead,
    string Importance,
    bool HasAttachments,
    string BodyPreview,
    string WebLink);

public record OutlookEmailRecommendation(
    string Subject,
    string From,
    string Recommendation,
    string WebLink);
