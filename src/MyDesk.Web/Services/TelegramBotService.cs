using System.Text;
using System.Text.Json;
using System.IO;
using MyDesk.Shared.Services;
using MyDesk.Shared.Models;

namespace MyDesk.Web.Services;

/// <summary>
/// Telegram bot integration supporting multiple bots (prod + dev) with per-tenant configuration.
/// 
/// Configuration (per tenant via PlatformSettings or appsettings.json):
///   "Telegram": {
///     "Bots": {
///       "prod": {
///         "BotToken": "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11",
///         "BotUsername": "mydeskdr_bot",
///         "AllowedUsers": ["peterb", "admin_user"],
///         "WebhookUrl": "https://mydesk.digitalresponse.com.au/api/telegram/webhook/prod"
///       },
///       "dev": {
///         "BotToken": "123456:ABC-DEF1234ghIkl-zyx57W2v1u123ew11",
///         "BotUsername": "mydeskdev_bot", 
///         "AllowedUsers": ["peterb", "dev_user"],
///         "WebhookUrl": "https://dev.digitalresponse.com.au/api/telegram/webhook/dev"
///       }
///     }
///   }
/// 
/// Set webhook: POST https://api.telegram.org/bot{TOKEN}/setWebhook?url={WEBHOOK_URL}
/// Delete webhook: POST https://api.telegram.org/bot{TOKEN}/deleteWebhook
/// Get webhook info: GET https://api.telegram.org/bot{TOKEN}/getWebhookInfo
/// </summary>
public class TelegramBotService
{
    private readonly HttpClient _http;
    private readonly IConfiguration _config;
    private readonly ILogger<TelegramBotService> _logger;
    private readonly AzureAIService _ai;
    private readonly McpIntegrationService _mcp;
    private readonly AiAuditService _audit;
    private readonly PlatformSettingsService _platformSettings;

    // Cached bot configurations per environment
    private Dictionary<string, TelegramBotConfig>? _botConfigs;

    public TelegramBotService(
        IHttpClientFactory httpClientFactory,
        IConfiguration config,
        ILogger<TelegramBotService> logger,
        AzureAIService ai,
        McpIntegrationService mcp,
        AiAuditService audit,
        PlatformSettingsService platformSettings)
    {
        _http = httpClientFactory.CreateClient();
        _config = config;
        _logger = logger;
        _ai = ai;
        _mcp = mcp;
        _audit = audit;
        _platformSettings = platformSettings;
    }

    /// <summary>
    /// Get all configured bot environments (prod, dev, etc.)
    /// </summary>
    public IEnumerable<string> GetConfiguredEnvironments()
    {
        var configs = GetBotConfigs();
        return configs.Keys;
    }

    /// <summary>
    /// Check if a specific bot environment is configured
    /// </summary>
    public bool IsConfigured(string environment = "prod")
    {
        var configs = GetBotConfigs();
        return configs.TryGetValue(environment, out var config) && !string.IsNullOrEmpty(config.BotToken);
    }

    /// <summary>
    /// Get bot configuration for a specific environment
    /// </summary>
    public TelegramBotConfig? GetBotConfig(string environment)
    {
        var configs = GetBotConfigs();
        return configs.TryGetValue(environment, out var config) ? config : null;
    }

    /// <summary>
    /// Handle incoming Telegram update (webhook POST) for a specific bot environment
    /// </summary>
    public async Task HandleUpdateAsync(string environment, JsonElement update)
    {
        if (!IsConfigured(environment)) 
        {
            _logger.LogWarning("Telegram bot '{Environment}' not configured, ignoring update", environment);
            return;
        }

        var config = GetBotConfig(environment)!;

        try
        {
            if (!update.TryGetProperty("message", out var message)) return;
            if (!message.TryGetProperty("chat", out var chat)) return;

            var chatId = chat.GetProperty("id").GetInt64();
            var from = message.TryGetProperty("from", out var fromEl) ? fromEl : default;
            var username = from.ValueKind == JsonValueKind.Object && from.TryGetProperty("username", out var u)
                ? u.GetString() ?? "anonymous" : "anonymous";

            string text = "";

                        if (message.TryGetProperty("voice", out var voice))
                        {
                            await SendChatActionAsync(config, chatId, "record_voice");
                var fileId = voice.GetProperty("file_id").GetString();
                text = await TranscribeVoiceAsync(config, fileId!) ?? "";
                if (string.IsNullOrEmpty(text))
                {
                    await SendMessageAsync(config, chatId, "🔇 Sorry, I couldn't understand that voice message.");
                    return;
                }
                await SendMessageAsync(config, chatId, $"🎤 _\"{text}\"_");
            }
            else if (message.TryGetProperty("text", out var textEl))
            {
                text = textEl.GetString() ?? "";
            }
            else
            {
                return;
            }

            // Check allowlist
            var allowedUsers = config.AllowedUsers ?? Array.Empty<string>();
            if (allowedUsers.Length > 0 && !allowedUsers.Contains(username, StringComparer.OrdinalIgnoreCase))
            {
                await SendMessageAsync(config, chatId, "⛔ Access denied. Your Telegram username is not authorized.");
                return;
            }

            // Handle commands
            if (text.StartsWith("/start"))
            {
                await SendMessageAsync(config, chatId,
                    $"👋 *Welcome to MyDesk AI ({environment.ToUpper()})*\n\n" +
                    "I can help you with:\n" +
                    "• Business data queries\n" +
                    "• MYOB reconciliation\n" +
                    "• Invoice & quote lookups\n" +
                    "• Cash flow & reporting\n\n" +
                    "Just ask me anything in plain English or send a voice message!\n\n" +
                    "Example: _\"What's our cash position?\"_");
                return;
            }

            if (text.StartsWith("/help"))
            {
                await SendMessageAsync(config, chatId,
                    $"*Commands ({environment.ToUpper()}):*\n" +
                    "/start — Welcome message\n" +
                    "/help — Show this help\n" +
                    "/briefing — Morning business briefing\n" +
                    "/reconcile — Run MYOB reconciliation\n" +
                    "/debtors — Who owes us money\n\n" +
                    "Or ask any question in plain English / Voice.");
                return;
            }

            // Typing indicator
            await SendChatActionAsync(config, chatId, "typing");

            // Build context and query AI
            var startTime = DateTime.Now;
            var userCode = $"tg:{environment}:{username}";

            var context = await _mcp.GetCombinedContextAsync(userCode);
            var systemPrompt = $"You are a helpful business AI assistant via Telegram ({environment} environment). Be concise and practical. " +
                               $"Use Markdown formatting. Respond in under 400 words.\n\n" +
                               $"Current business context:\n{context}";

            var messages = new List<AzureChatMessage>
            {
                AzureChatMessage.System(systemPrompt),
                AzureChatMessage.User(text),
            };

            var reply = await _ai.ChatAsync(messages);
            var duration = (int)(DateTime.Now - startTime).TotalMilliseconds;

            await _audit.LogAsync(userCode, $"telegram:{environment}", text, reply.Content,
                success: reply.IsSuccess, durationMs: duration);

            await SendMessageAsync(config, chatId, reply.Content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Telegram update handling failed for environment {Environment}", environment);
        }
    }

    /// <summary>
    /// Send a message via the specified bot
    /// </summary>
    public async Task SendMessageAsync(TelegramBotConfig config, long chatId, string text)
    {
        if (string.IsNullOrEmpty(config.BotToken)) return;

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
            var url = $"https://api.telegram.org/bot{config.BotToken}/sendMessage";
            await _http.PostAsync(url, content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send Telegram message via bot {BotUsername}", config.BotUsername);
        }
    }

    /// <summary>
    /// Send chat action (typing, record_voice, etc.)
    /// </summary>
    public async Task SendChatActionAsync(TelegramBotConfig config, long chatId, string action)
    {
        if (string.IsNullOrEmpty(config.BotToken)) return;
        try
        {
            var payload = new { chat_id = chatId, action };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
            await _http.PostAsync($"https://api.telegram.org/bot{config.BotToken}/sendChatAction", content);
        }
        catch { /* non-critical */ }
    }

    /// <summary>
    /// Transcribe a voice message using Azure AI
    /// </summary>
    private async Task<string?> TranscribeVoiceAsync(TelegramBotConfig config, string fileId)
    {
        try
        {
            var fileResponse = await _http.GetAsync($"https://api.telegram.org/bot{config.BotToken}/getFile?file_id={fileId}");
            if (!fileResponse.IsSuccessStatusCode) return null;

            using var fileDoc = JsonDocument.Parse(await fileResponse.Content.ReadAsStringAsync());
            var filePath = fileDoc.RootElement.GetProperty("result").GetProperty("file_path").GetString();
            if (string.IsNullOrEmpty(filePath)) return null;

            var downloadUrl = $"https://api.telegram.org/file/bot{config.BotToken}/{filePath}";
            using var audioStream = await _http.GetStreamAsync(downloadUrl);

            return await _ai.TranscribeAsync(audioStream, Path.GetFileName(filePath));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to transcribe Telegram voice message");
            return null;
        }
    }

    /// <summary>
    /// Set webhook for a specific bot environment
    /// </summary>
    public async Task<bool> SetWebhookAsync(string environment, string? customUrl = null)
    {
        if (!IsConfigured(environment)) return false;

        var config = GetBotConfig(environment)!;
        var url = customUrl ?? config.WebhookUrl;

        if (string.IsNullOrEmpty(url))
        {
            _logger.LogWarning("No webhook URL configured for {Environment}", environment);
            return false;
        }

        try
        {
            var response = await _http.GetAsync($"https://api.telegram.org/bot{config.BotToken}/setWebhook?url={Uri.EscapeDataString(url)}");
            var success = response.IsSuccessStatusCode;
            if (success)
            {
                _logger.LogInformation("Telegram webhook set for {Environment}: {Url}", environment, url);
            }
            else
            {
                var body = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to set webhook for {Environment}: {Body}", environment, body);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set Telegram webhook for {Environment}", environment);
            return false;
        }
    }

    /// <summary>
    /// Delete webhook for a specific bot environment
    /// </summary>
    public async Task<bool> DeleteWebhookAsync(string environment)
    {
        if (!IsConfigured(environment)) return false;

        var config = GetBotConfig(environment)!;
        try
        {
            var response = await _http.GetAsync($"https://api.telegram.org/bot{config.BotToken}/deleteWebhook");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete Telegram webhook for {Environment}", environment);
            return false;
        }
    }

    /// <summary>
    /// Get webhook info for a specific bot environment
    /// </summary>
    public async Task<JsonElement?> GetWebhookInfoAsync(string environment)
    {
        if (!IsConfigured(environment)) return null;

        var config = GetBotConfig(environment)!;
        try
        {
            var response = await _http.GetAsync($"https://api.telegram.org/bot{config.BotToken}/getWebhookInfo");
            if (!response.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return doc.RootElement.Clone();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get webhook info for {Environment}", environment);
            return null;
        }
    }

    /// <summary>
    /// Broadcast a message to all allowed users of a bot (useful for alerts)
    /// </summary>
    public async Task BroadcastAsync(string environment, string message, IEnumerable<long>? chatIds = null)
    {
        if (!IsConfigured(environment)) return;

        var config = GetBotConfig(environment)!;
        var targets = chatIds ?? config.AllowedChatIds ?? Array.Empty<long>();

        foreach (var chatId in targets)
        {
            await SendMessageAsync(config, chatId, message);
            await Task.Delay(50); // Rate limit
        }
    }

    private Dictionary<string, TelegramBotConfig> GetBotConfigs()
        {
            if (_botConfigs != null) return _botConfigs;

            _botConfigs = new Dictionary<string, TelegramBotConfig>(StringComparer.OrdinalIgnoreCase);

            // 1. Load from PlatformSettings (per-tenant, stored in DB) - NEW flexible approach
            var platformSettings = _platformSettings.Current;
            if (platformSettings?.TelegramBots != null && platformSettings.TelegramBots.Count > 0)
            {
                foreach (var kvp in platformSettings.TelegramBots)
                {
                    var config = kvp.Value;
                    if (!string.IsNullOrEmpty(config.BotToken))
                    {
                        config.Environment = kvp.Key;
                        _botConfigs[kvp.Key] = config;
                    }
                }
            }
            // 1b. Fallback to simple TelegramSettings (ProdBotToken/DevBotToken)
            else if (platformSettings?.Telegram != null)
            {
                var ts = platformSettings.Telegram;
                var baseUrl = ts.WebhookBaseUrl ?? "";
            
                // Prod bot
                if (!string.IsNullOrEmpty(ts.ProdBotToken))
                {
                    _botConfigs["prod"] = new TelegramBotConfig
                    {
                        BotToken = ts.ProdBotToken,
                        BotUsername = "mydesk_bot",
                        AllowedUsers = ts.AllowedUsers,
                        AllowedChatIds = ts.AllowedChatIds,
                        WebhookUrl = ts.ProdWebhookUrl ?? $"{baseUrl}/api/telegram/webhook/prod",
                        Environment = "prod",
                        Enabled = true
                    };
                }

                // Dev bot
                if (!string.IsNullOrEmpty(ts.DevBotToken))
                {
                    _botConfigs["dev"] = new TelegramBotConfig
                    {
                        BotToken = ts.DevBotToken,
                        BotUsername = "mydeskdev_bot",
                        AllowedUsers = ts.AllowedUsers,
                        AllowedChatIds = ts.AllowedChatIds,
                        WebhookUrl = ts.DevWebhookUrl ?? $"{baseUrl}/api/telegram/webhook/dev",
                        Environment = "dev",
                        Enabled = true
                    };
                }
            }

            // 2. Fallback to appsettings.json (global, not per-tenant)
            var appSettingsSection = _config.GetSection("Telegram:Bots");
            if (appSettingsSection.Exists())
            {
                foreach (var child in appSettingsSection.GetChildren())
                {
                    if (!_botConfigs.ContainsKey(child.Key))
                    {
                        var config = child.Get<TelegramBotConfig>();
                        if (config != null && !string.IsNullOrEmpty(config.BotToken))
                        {
                            _botConfigs[child.Key] = config;
                        }
                    }
                }
            }

            // 3. Legacy single-bot config (backward compatibility)
            var legacyToken = _config["Telegram:BotToken"];
            var legacyAllowed = _config["Telegram:AllowedUsers"]?.Split(',', StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            if (!string.IsNullOrEmpty(legacyToken) && !_botConfigs.ContainsKey("prod"))
            {
                _botConfigs["prod"] = new TelegramBotConfig
                {
                    BotToken = legacyToken,
                    BotUsername = "mydesk_bot",
                    AllowedUsers = legacyAllowed,
                    WebhookUrl = $"{_config["Telegram:WebhookBaseUrl"] ?? "https://mydesk.digitalresponse.com.au"}/api/telegram/webhook/prod"
                };
            }
            return _botConfigs;
                }
            }