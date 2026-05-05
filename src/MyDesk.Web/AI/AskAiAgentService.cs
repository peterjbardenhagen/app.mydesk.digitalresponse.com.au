using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MyDesk.Web.Services;

namespace MyDesk.Web.AI;

/// <summary>
/// Multi-turn Ask AI agent. Sends a prompt to Azure OpenAI with the registered
/// <see cref="IAiTool"/>s exposed as OpenAI-compatible tool definitions, then
/// runs a tool-calling loop:
///   1. Send conversation + tools to the model.
///   2. If the assistant returns <c>tool_calls</c>, execute each tool, append
///      tool results to the conversation, then loop.
///   3. Otherwise return the assistant's text reply (plus any chart / table /
///      notice renderables produced by the tools).
///
/// Bleeding-edge bits:
///   * Tool calling (function calling) — the model decides which tools to invoke.
///   * Renderables — tools can attach typed UI artefacts (charts, tables) so the
///     UI can show them inline alongside the natural-language answer. This is the
///     pattern OpenAI calls "structured outputs / canvases".
///   * The agent runs <c>maxIterations</c> tool-calling rounds (default 4),
///     enabling multi-step reasoning ("query A, then chart B, then schedule C").
///
/// Tenant safety: every tool runs in the caller's request scope, so tenant
/// isolation (RLS + EF filter) applies automatically inside tool implementations.
/// </summary>
public class AskAiAgentService
{
    private readonly AzureAIOptions _opts;
    private readonly IHttpClientFactory _httpFactory;
    private readonly IEnumerable<IAiTool> _tools;
    private readonly ILogger<AskAiAgentService> _logger;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public AskAiAgentService(
        IOptions<AzureAIOptions> opts,
        IHttpClientFactory httpFactory,
        IEnumerable<IAiTool> tools,
        ILogger<AskAiAgentService> logger)
    {
        _opts = opts.Value;
        _httpFactory = httpFactory;
        _tools = tools.ToList();
        _logger = logger;
    }

    /// <summary>List of tool names registered with the agent. Useful for the UI / debugging.</summary>
    public IReadOnlyList<string> AvailableToolNames => _tools.Select(t => t.Name).ToList();

    public async Task<AskAiReply> AskAsync(
        string userPrompt,
        string? systemPrompt = null,
        int maxIterations = 4,
        int maxTokens = 1500,
        CancellationToken ct = default)
    {
        if (!_opts.IsConfigured)
        {
            return new AskAiReply(
                "Ask AI is not configured. Set the Azure OpenAI keys in appsettings.json.",
                Array.Empty<AiRenderable>(),
                Array.Empty<AskAiToolTrace>());
        }

        var renderables = new List<AiRenderable>();
        var trace       = new List<AskAiToolTrace>();

        // Build the initial conversation. The system prompt orients the model around
        // MyDesk's tools and tells it to prefer charts when the user asks visual questions.
        var sys = systemPrompt ?? """
            You are the MyDesk Assistant — an embedded AI for an Australian business
            management platform. Always answer in the context of the *current tenant only*
            (the platform enforces tenant isolation; never speculate about other tenants).
            
            You have access to a set of tools. Use them to look up real data when the
            question is about quotes, invoices, the pipeline, or anything in the database.
            When the user asks for trends / wins-vs-losses / breakdowns, prefer a tool
            call that returns a chart. When the user asks you to schedule something
            recurring, use the schedule tool — do not invent a schedule yourself.
            
            After tool calls, write a concise plain-language answer (Australian English,
            max ~120 words) that references the data you retrieved.
            """;

        var conversation = new List<JsonObject2>
        {
            Message("system", sys),
            Message("user",   userPrompt)
        };

        var toolDefs = _tools.Select(t => new
        {
            type = "function",
            function = new
            {
                name = t.Name,
                description = t.Description,
                parameters = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(t.ParametersSchema))
            }
        }).ToArray();

        for (int iter = 0; iter < maxIterations; iter++)
        {
            var (assistantText, toolCalls, raw) = await CallModelAsync(conversation, toolDefs, maxTokens, ct);

            // Append the assistant turn to the transcript verbatim — required by the API
            // when the next message is a tool result.
            conversation.Add(raw);

            if (toolCalls.Count == 0)
            {
                // No more tool calls — return final text + renderables collected so far.
                return new AskAiReply(assistantText, renderables, trace);
            }

            // Execute each tool call sequentially and append a "tool" message per result.
            foreach (var call in toolCalls)
            {
                var tool = _tools.FirstOrDefault(t =>
                    string.Equals(t.Name, call.Name, StringComparison.OrdinalIgnoreCase));

                AiToolResult result;
                try
                {
                    if (tool is null)
                    {
                        result = new AiToolResult(JsonSerializer.Serialize(new { error = $"Unknown tool '{call.Name}'." }));
                    }
                    else
                    {
                        var argsEl = string.IsNullOrWhiteSpace(call.ArgumentsJson)
                            ? JsonDocument.Parse("{}").RootElement
                            : JsonDocument.Parse(call.ArgumentsJson).RootElement;
                        result = await tool.ExecuteAsync(argsEl, ct);
                        if (result.Renderable is not null) renderables.Add(result.Renderable);
                    }
                    trace.Add(new AskAiToolTrace(call.Name, call.ArgumentsJson, Truncate(result.ContentJson, 400), Success: true));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ask AI tool '{Tool}' threw", call.Name);
                    result = new AiToolResult(JsonSerializer.Serialize(new { error = ex.Message }));
                    trace.Add(new AskAiToolTrace(call.Name, call.ArgumentsJson, ex.Message, Success: false));
                }

                conversation.Add(new JsonObject2
                {
                    ["role"]         = "tool",
                    ["tool_call_id"] = call.Id,
                    ["name"]         = call.Name,
                    ["content"]      = result.ContentJson,
                });
            }
        }

        // Hit the iteration cap — return whatever we have with a soft warning.
        return new AskAiReply(
            "I reached the maximum number of tool-calling rounds without the model finishing. " +
            "Here's what I gathered so far.",
            renderables, trace);
    }

    // ──────────────────────────────────────────────────────────────────────
    // HTTP / response parsing
    // ──────────────────────────────────────────────────────────────────────

    private record ToolCall(string Id, string Name, string ArgumentsJson);

    private async Task<(string Text, List<ToolCall> ToolCalls, JsonObject2 RawAssistantTurn)>
        CallModelAsync(List<JsonObject2> conversation, object[] toolDefs, int maxTokens, CancellationToken ct)
    {
        var http = _httpFactory.CreateClient("AzureAI");
        http.DefaultRequestHeaders.Remove("api-key");
        http.DefaultRequestHeaders.Add("api-key", _opts.OpenAIApiKey);

        var payload = new
        {
            messages = conversation.Select(m => m.AsAnonymous()).ToArray(),
            tools = toolDefs,
            tool_choice = "auto",
            max_completion_tokens = maxTokens,
            temperature = 0.4
        };

        var body = new StringContent(JsonSerializer.Serialize(payload, _json), Encoding.UTF8, "application/json");
        var resp = await http.PostAsync(_opts.ChatCompletionsUrl, body, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);

        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Azure AI returned {Status}: {Body}", resp.StatusCode, Truncate(text, 800));
            return ($"AI error {(int)resp.StatusCode}: {resp.ReasonPhrase}", new(), Message("assistant", text));
        }

        using var doc = JsonDocument.Parse(text);
        var msg = doc.RootElement.GetProperty("choices")[0].GetProperty("message");

        var content = msg.TryGetProperty("content", out var cEl) && cEl.ValueKind == JsonValueKind.String
            ? cEl.GetString() ?? string.Empty
            : string.Empty;

        // Build the raw assistant turn (must be re-included in the next request verbatim).
        var rawAssistant = new JsonObject2 { ["role"] = "assistant", ["content"] = (object?)content ?? "" };

        var toolCalls = new List<ToolCall>();
        if (msg.TryGetProperty("tool_calls", out var tcEl) && tcEl.ValueKind == JsonValueKind.Array)
        {
            // Persist the original tool_calls array on the assistant message so the API
            // accepts the subsequent tool messages.
            rawAssistant["tool_calls"] = JsonSerializer.Deserialize<JsonElement>(tcEl.GetRawText());

            foreach (var tc in tcEl.EnumerateArray())
            {
                var id = tc.GetProperty("id").GetString() ?? "";
                var fn = tc.GetProperty("function");
                var name = fn.GetProperty("name").GetString() ?? "";
                var argsJson = fn.GetProperty("arguments").GetString() ?? "{}";
                toolCalls.Add(new ToolCall(id, name, argsJson));
            }
        }

        return (content, toolCalls, rawAssistant);
    }

    private static JsonObject2 Message(string role, string content) =>
        new() { ["role"] = role, ["content"] = content };

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max) + "…";

    /// <summary>
    /// Lightweight ordered-key/value bag used because we need to interleave
    /// strings + nested JSON elements (tool_calls) when constructing turns.
    /// Serialised via <see cref="AsAnonymous"/>.
    /// </summary>
    private sealed class JsonObject2 : Dictionary<string, object?>
    {
        public object AsAnonymous()
        {
            // Re-serialise + deserialise so nested JsonElements turn into a clean tree
            // for the outgoing payload.
            var s = JsonSerializer.Serialize((IDictionary<string, object?>)this, _json);
            return JsonSerializer.Deserialize<JsonElement>(s);
        }
    }
}
