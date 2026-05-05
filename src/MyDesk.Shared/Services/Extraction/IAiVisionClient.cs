namespace MyDesk.Shared.Services.Extraction;

/// <summary>
/// Abstraction over multimodal vision-capable LLMs (Azure GPT-5.4 Mini / GPT-4o).
/// Lets <see cref="GptVisionExtractionStrategy"/> live in MyDesk.Shared without
/// taking a hard dependency on the MyDesk.Web Azure HTTP client.
/// The implementation lives in MyDesk.Web and is registered into DI at startup.
/// </summary>
public interface IAiVisionClient
{
    bool IsConfigured { get; }

    /// <summary>
    /// Sends a system prompt + a user message containing an image and returns
    /// the raw model response (expected to be valid JSON when the prompt asks
    /// for JSON-mode output).
    /// </summary>
    Task<AiVisionResult> ExtractJsonAsync(
        string systemPrompt,
        string userPrompt,
        byte[] imageBytes,
        string contentType,
        CancellationToken cancellationToken = default);
}

public record AiVisionResult(bool IsSuccess, string Content, string? Error = null)
{
    public static AiVisionResult Ok(string content)  => new(true,  content, null);
    public static AiVisionResult Fail(string error)  => new(false, "",      error);
}
