using System.Text.Json;

namespace MyDesk.Web.AI.Providers;

/// <summary>
/// Abstraction over any OpenAI-compatible chat completions provider.
/// Implementations: <see cref="OpenAiCompatibleProvider"/> (covers Azure, OpenAI,
/// Gemini compat endpoint, Openrouter, Opencode, Ollama).
/// </summary>
public interface IAiChatProvider
{
    bool   IsConfigured  { get; }
    string ProviderName  { get; }
    string ModelId       { get; }

    /// <summary>
    /// Sends a conversation (with optional tool definitions) to the model and returns
    /// the assistant reply plus any tool calls it requested.
    ///
    /// <paramref name="messages"/> elements must be serialisable as OpenAI message objects
    /// (dictionaries with "role" / "content" / optional "tool_calls" / "tool_call_id").
    /// <paramref name="toolDefs"/> elements must be serialisable as OpenAI function-tool objects.
    /// </summary>
    Task<AiChatResponse> ChatWithToolsAsync(
        IReadOnlyList<object> messages,
        IReadOnlyList<object>? toolDefs,
        int maxTokens,
        double temperature,
        CancellationToken ct = default);
}

/// <summary>Response from a single chat completions round-trip.</summary>
public record AiChatResponse(
    string                   Text,
    IReadOnlyList<AiRawToolCall> ToolCalls,
    /// <summary>
    /// The raw assistant turn exactly as returned by the API (must be re-sent verbatim
    /// in the next request when continuing a tool-calling loop).
    /// </summary>
    object                   RawAssistantTurn);

/// <summary>A single tool call requested by the model.</summary>
public record AiRawToolCall(string Id, string Name, string ArgumentsJson);
