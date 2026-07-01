using System.Text;
using System.Text.Json;
using MyDesk.Shared.Models;

namespace MyDesk.Web.AI.Providers;

/// <summary>
/// Provider implementation for any service that exposes an OpenAI-compatible
/// /chat/completions endpoint with tool-calling support.
///
/// Supported backends (all use identical request/response format):
///   • Azure AI Foundry  – api-key auth, deployment-in-URL model routing
///   • OpenAI            – Bearer auth, model in request body
///   • Google Gemini     – Bearer auth, via the Gemini OpenAI-compat endpoint
///   • Openrouter        – Bearer auth, model in body, free + paid models
///   • Opencode / Zen    – Bearer auth, model in body
///   • Ollama            – no auth or Bearer, self-hosted or cloud
/// </summary>
public sealed class OpenAiCompatibleProvider : IAiChatProvider
{
    private readonly AiProviderConfig _cfg;
    private readonly IHttpClientFactory _http;
    private readonly ILogger<OpenAiCompatibleProvider> _logger;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public bool   IsConfigured => _cfg.IsConfigured;
    public string ProviderName => _cfg.Provider;
    public string ModelId      => _cfg.ChatModel;

    public OpenAiCompatibleProvider(
        AiProviderConfig cfg,
        IHttpClientFactory http,
        ILogger<OpenAiCompatibleProvider> logger)
    {
        _cfg    = cfg;
        _http   = http;
        _logger = logger;
    }

    public async Task<AiChatResponse> ChatWithToolsAsync(
        IReadOnlyList<object> messages,
        IReadOnlyList<object>? toolDefs,
        int maxTokens,
        double temperature,
        CancellationToken ct = default)
    {
        if (!IsConfigured)
            return Err("AI provider is not configured. Set up your AI provider in Settings → AI Provider.");

        var url = _cfg.ResolveChatUrl();
        if (string.IsNullOrWhiteSpace(url))
            return Err($"Cannot resolve endpoint URL for provider '{_cfg.Provider}'.");

        var (authHeader, authValue) = _cfg.ResolveAuthHeader();

        var payload = BuildPayload(messages, toolDefs, maxTokens, temperature);
        var body    = new StringContent(JsonSerializer.Serialize(payload, _json), Encoding.UTF8, "application/json");

        var client = _http.CreateClient("AiProvider");
        client.DefaultRequestHeaders.Remove(authHeader);
        client.DefaultRequestHeaders.TryAddWithoutValidation(authHeader, authValue);

        // Openrouter wants a site URL header for attribution
        if (_cfg.Provider == MyDesk.Shared.Models.AiProviderKind.Openrouter)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation("HTTP-Referer", "https://mydesk.digitalresponse.com.au");
            client.DefaultRequestHeaders.TryAddWithoutValidation("X-Title", "MyDesk");
        }

        HttpResponseMessage resp;
        try
        {
            resp = await client.PostAsync(url, body, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI provider HTTP call failed ({Provider})", _cfg.Provider);
            return Err($"Network error calling {_cfg.Provider}: {ex.Message}");
        }

        var raw = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("{Provider} returned {Status}: {Body}", _cfg.Provider, resp.StatusCode,
                raw.Length > 600 ? raw[..600] : raw);
            return Err($"AI error {(int)resp.StatusCode} ({_cfg.Provider}): {resp.ReasonPhrase}");
        }

        return ParseResponse(raw);
    }

    // ── Payload builder ─────────────────────────────────────────────────────

    private object BuildPayload(
        IReadOnlyList<object> messages,
        IReadOnlyList<object>? toolDefs,
        int maxTokens, double temperature)
    {
        // Include model field for all non-Azure providers
        var modelField = _cfg.RequestBodyModel;

        if (toolDefs?.Count > 0)
        {
            return modelField is not null
                ? new { model = modelField, messages, tools = toolDefs, tool_choice = "auto",
                        max_completion_tokens = maxTokens, temperature }
                : (object)new { messages, tools = toolDefs, tool_choice = "auto",
                                max_completion_tokens = maxTokens, temperature };
        }

        return modelField is not null
            ? new { model = modelField, messages, max_completion_tokens = maxTokens, temperature }
            : (object)new { messages, max_completion_tokens = maxTokens, temperature };
    }

    // ── Response parser ─────────────────────────────────────────────────────

    private AiChatResponse ParseResponse(string rawJson)
    {
        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var msg = doc.RootElement.GetProperty("choices")[0].GetProperty("message");

            var content = msg.TryGetProperty("content", out var cEl) && cEl.ValueKind == JsonValueKind.String
                ? cEl.GetString() ?? ""
                : "";

            // Raw assistant turn — must be re-included verbatim in the next request
            var rawDict = new Dictionary<string, object?> { ["role"] = "assistant", ["content"] = content };

            var toolCalls = new List<AiRawToolCall>();
            if (msg.TryGetProperty("tool_calls", out var tcEl) && tcEl.ValueKind == JsonValueKind.Array)
            {
                rawDict["tool_calls"] = JsonSerializer.Deserialize<JsonElement>(tcEl.GetRawText());

                foreach (var tc in tcEl.EnumerateArray())
                {
                    var id   = tc.GetProperty("id").GetString() ?? "";
                    var fn   = tc.GetProperty("function");
                    var name = fn.GetProperty("name").GetString() ?? "";
                    var args = fn.GetProperty("arguments").GetString() ?? "{}";
                    toolCalls.Add(new AiRawToolCall(id, name, args));
                }
            }

            return new AiChatResponse(content, toolCalls, rawDict);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI provider response");
            return Err($"Could not parse response from {_cfg.Provider}: {ex.Message}");
        }
    }

    private static AiChatResponse Err(string msg) =>
        new(msg, Array.Empty<AiRawToolCall>(), new Dictionary<string, object?> { ["role"] = "assistant", ["content"] = msg });
}
