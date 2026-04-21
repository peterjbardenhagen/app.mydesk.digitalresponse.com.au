using System.Text;
using System.Text.Json;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Services;

/// <summary>
/// Telegram bot integration per Proposal #272.
/// Enables mobile/voice access: "Hey Techlight, what's our cash position?"
/// 
/// Configure webhook: POST https://api.telegram.org/bot{TOKEN}/setWebhook?url={YOUR_URL}/api/telegram/webhook
/// </summary>
public class TelegramBotService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<TelegramBotService> _logger;
    private readonly AzureAIService _ai;
    private readonly McpIntegrationService _mcp;
    private readonly AiAuditService _audit;

    public TelegramBotService(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<TelegramBotService> logger,
        AzureAIService ai,
        McpIntegrationService mcp,
        AiAuditService audit)
    {
        _http = httpClientFactory.CreateClient();
        _config = config;
        _logger = logger;
        _ai = ai;
        _mcp = mcp;
        _audit = audit;
    }

    public bool IsConfigured => !string.IsNullOrEmpty(_config["Telegram:BotToken"]);

    private string Token => _config["Telegram:BotToken"] ?? "";
    private string ApiBase => $"https://api.telegram.org/bot{Token}";

    /// <summary>
    /// Handle incoming Telegram update (webhook POST).
    /// </summary>
    public async Task HandleUpdateAsync(JsonElement update)
    {
        if (!IsConfigured) return;

        try
        {
            if (!update.TryGetProperty("message", out var message)) return;
            if (!message.TryGetProperty("chat", out var chat)) return;
            if (!message.TryGetProperty("text", out var textEl)) return;

            var chatId = chat.GetProperty("id").GetInt64();
            var text = textEl.GetString() ?? "";
            var from = message.TryGetProperty("from", out var fromEl) ? fromEl : default;
            var username = from.ValueKind == JsonValueKind.Object && from.TryGetProperty("username", out var u)
                ? u.GetString() ?? "anonymous" : "anonymous";

            // Check allowlist
            var allowedUsers = _config["Telegram:AllowedUsers"]?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            if (allowedUsers.Length > 0 && !allowedUsers.Contains(username, StringComparer.OrdinalIgnoreCase))
            {
                await SendMessageAsync(chatId, "⛔ Access denied. Your Telegram username is not authorized.");
                return;
            }

            // Handle commands
            if (text.StartsWith("/start"))
            {
                await SendMessageAsync(chatId,
                    "👋 *Welcome to Techlight AI*\n\n" +
                    "I can help you with:\n" +
                    "• Business data queries\n" +
                    "• MYOB reconciliation\n" +
                    "• Invoice & quote lookups\n" +
                    "• Cash flow & reporting\n\n" +
                    "Just ask me anything in plain English!\n\n" +
                    "Example: _\"What's our cash position?\"_");
                return;
            }

            if (text.StartsWith("/help"))
            {
                await SendMessageAsync(chatId,
                    "*Commands:*\n" +
                    "/start — Welcome message\n" +
                    "/help — Show this help\n" +
                    "/briefing — Morning business briefing\n" +
                    "/reconcile — Run MYOB reconciliation\n" +
                    "/debtors — Who owes us money\n\n" +
                    "Or ask any question in plain English.");
                return;
            }

            // Typing indicator
            await SendChatActionAsync(chatId, "typing");

            // Build context and query AI
            var startTime = DateTime.Now;
            var userCode = $"tg:{username}";

            var context = await _mcp.GetCombinedContextAsync(userCode);
            var systemPrompt = "You are Techlight AI via Telegram. Be concise and practical. " +
                               "Use Markdown formatting. Respond in under 400 words.\n\n" +
                               "Current business context:\n" + context;

            var messages = new List<AzureChatMessage>
            {
                AzureChatMessage.System(systemPrompt),
                AzureChatMessage.User(text),
            };

            var reply = await _ai.ChatAsync(messages);
            var duration = (int)(DateTime.Now - startTime).TotalMilliseconds;

            await _audit.LogAsync(userCode, "telegram", text, reply.Content,
                success: reply.IsSuccess, durationMs: duration);

            await SendMessageAsync(chatId, reply.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telegram update handling failed");
        }
    }

    public async Task SendMessageAsync(long chatId, string text)
    {
        if (!IsConfigured) return;

        try
        {
            var payload = new
            {
                chat_id = chatId,
                text,
                parse_mode = "Markdown",
                disable_web_page_preview = true,
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            await _http.PostAsync($"{ApiBase}/sendMessage", content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Telegram message");
        }
    }

    private async Task SendChatActionAsync(long chatId, string action)
    {
        if (!IsConfigured) return;
        try
        {
            var payload = new { chat_id = chatId, action };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            await _http.PostAsync($"{ApiBase}/sendChatAction", content);
        }
        catch { /* non-critical */ }
    }

    public async Task<bool> SetWebhookAsync(string url)
    {
        if (!IsConfigured) return false;
        try
        {
            var response = await _http.GetAsync($"{ApiBase}/setWebhook?url={Uri.EscapeDataString(url)}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set Telegram webhook");
            return false;
        }
    }
}
