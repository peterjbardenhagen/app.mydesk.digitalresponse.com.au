using System.Text.Json;
using MyDesk.Web.AI.Providers;

namespace MyDesk.Web.AI;

/// <summary>
/// Multi-turn Ask AI agent. Uses the <see cref="AiProviderFactory"/> to resolve the
/// active provider for the current tenant (Azure AI Foundry, OpenAI, Gemini, Ollama,
/// Openrouter, Opencode — all via the <see cref="IAiChatProvider"/> abstraction).
///
/// Flow:
///   1. Build conversation messages + tool definitions.
///   2. Send to model via provider. If model returns tool_calls, execute each tool.
///   3. Append tool results and repeat (up to maxIterations).
///   4. Return final text reply + any chart/table renderables produced by the tools.
///
/// Tenant safety: every tool runs in the caller's request scope so tenant isolation
/// (EF Core global query filter + SQL RLS) applies inside tool implementations.
/// </summary>
public class AskAiAgentService
{
    private readonly AiProviderFactory      _providerFactory;
    private readonly IEnumerable<IAiTool>   _tools;
    private readonly ILogger<AskAiAgentService> _logger;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public AskAiAgentService(
        AiProviderFactory providerFactory,
        IEnumerable<IAiTool> tools,
        ILogger<AskAiAgentService> logger)
    {
        _providerFactory = providerFactory;
        _tools           = tools.ToList();
        _logger          = logger;
    }

    /// <summary>List of tool names registered with the agent.</summary>
    public IReadOnlyList<string> AvailableToolNames => _tools.Select(t => t.Name).ToList();

    public async Task<AskAiReply> AskAsync(
        string userPrompt,
        string? systemPrompt = null,
        int maxIterations = 4,
        int maxTokens     = 1500,
        CancellationToken ct = default)
    {
        var provider = _providerFactory.Resolve();

        if (!provider.IsConfigured)
        {
            return new AskAiReply(
                "Ask AI is not configured. Go to Settings → AI Provider and enter your API key.",
                Array.Empty<AiRenderable>(),
                Array.Empty<AskAiToolTrace>());
        }

        var renderables = new List<AiRenderable>();
        var trace       = new List<AskAiToolTrace>();

        var sys = systemPrompt ?? $"""
            You are the MyDesk Assistant — an embedded AI for an Australian business
            management platform. Always answer in the context of the *current tenant only*
            (the platform enforces tenant isolation; never speculate about other tenants).

            You have access to a set of tools. Use them to look up real data when the
            question is about quotes, invoices, the pipeline, customers, or anything in the database.
            When the user asks for trends / wins-vs-losses / breakdowns, prefer a tool
            call that returns a chart. When the user asks to schedule something recurring,
            use the schedule tool — do not invent a schedule yourself.

            After tool calls, write a concise plain-language answer (Australian English,
            max ~120 words) that references the data you retrieved.
            """;

        // Build the tool definitions array (OpenAI function-call format)
        var toolDefs = _tools.Select(t => (object)new
        {
            type = "function",
            function = new
            {
                name        = t.Name,
                description = t.Description,
                parameters  = JsonSerializer.Deserialize<JsonElement>(JsonSerializer.Serialize(t.ParametersSchema))
            }
        }).ToList();

        // Conversation history as plain dictionaries (OpenAI message format)
        var conversation = new List<object>
        {
            new Dictionary<string, object?> { ["role"] = "system", ["content"] = sys },
            new Dictionary<string, object?> { ["role"] = "user",   ["content"] = userPrompt },
        };

        for (int iter = 0; iter < maxIterations; iter++)
        {
            var resp = await provider.ChatWithToolsAsync(conversation, toolDefs, maxTokens, 0.4, ct);

            // Append the raw assistant turn — required by the API in the next request
            conversation.Add(resp.RawAssistantTurn);

            if (resp.ToolCalls.Count == 0)
                return new AskAiReply(resp.Text, renderables, trace);

            // Execute tool calls
            foreach (var call in resp.ToolCalls)
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
                    trace.Add(new AskAiToolTrace(call.Name, call.ArgumentsJson,
                        Truncate(result.ContentJson, 400), Success: true));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Ask AI tool '{Tool}' threw", call.Name);
                    result = new AiToolResult(JsonSerializer.Serialize(new { error = ex.Message }));
                    trace.Add(new AskAiToolTrace(call.Name, call.ArgumentsJson, ex.Message, Success: false));
                }

                conversation.Add(new Dictionary<string, object?>
                {
                    ["role"]         = "tool",
                    ["tool_call_id"] = call.Id,
                    ["name"]         = call.Name,
                    ["content"]      = result.ContentJson,
                });
            }
        }

        return new AskAiReply(
            "I reached the maximum number of tool-calling rounds. Here's what I gathered so far.",
            renderables, trace);
    }

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s : s[..max] + "…";
}
