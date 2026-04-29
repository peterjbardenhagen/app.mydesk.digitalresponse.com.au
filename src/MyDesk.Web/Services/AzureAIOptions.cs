namespace MyDesk.Web.Services;

/// <summary>
/// Typed options for the Azure section in appsettings.json.
/// Bound from configuration key "Azure".
/// </summary>
public class AzureAIOptions
{
    public const string Section = "Azure";

    public string OpenAIApiKey { get; set; } = string.Empty;

    /// <summary>Base endpoint, e.g. https://peter-mmk98bxd-eastus2.cognitiveservices.azure.com</summary>
    public string OpenAIEndpoint { get; set; } = string.Empty;

    /// <summary>Deployment/model name, e.g. gpt-5.4-scan</summary>
    public string OpenAIModel { get; set; } = string.Empty;

    /// <summary>API version, e.g. 2025-01-01-preview</summary>
    public string OpenAIApiVersion { get; set; } = string.Empty;

    /// <summary>Embedding deployment name, e.g. text-embedding-3-large</summary>
    public string OpenAIEmbeddingDeployment { get; set; } = string.Empty;

    /// <summary>Whisper deployment name, e.g. whisper-1</summary>
    public string OpenAIWhisperDeployment { get; set; } = string.Empty;

    public int OpenAIEmbeddingDimensions { get; set; } = 1536;

    /// <summary>
    /// Builds the full chat completions URL from the component parts.
    /// Format: {endpoint}/openai/deployments/{model}/chat/completions?api-version={version}
    /// </summary>
    public string ChatCompletionsUrl =>
        $"{OpenAIEndpoint.TrimEnd('/')}/openai/deployments/{OpenAIModel}/chat/completions?api-version={OpenAIApiVersion}";

    /// <summary>
    /// Builds the full embeddings URL from the component parts.
    /// </summary>
    public string EmbeddingsUrl =>
        $"{OpenAIEndpoint.TrimEnd('/')}/openai/deployments/{OpenAIEmbeddingDeployment}/embeddings?api-version={OpenAIApiVersion}";

    /// <summary>
    /// Builds the full transcriptions URL for Whisper.
    /// </summary>
    public string TranscriptionsUrl =>
        $"{OpenAIEndpoint.TrimEnd('/')}/openai/deployments/{OpenAIWhisperDeployment}/audio/transcriptions?api-version={OpenAIApiVersion}";

    public bool IsConfigured =>
        !string.IsNullOrEmpty(OpenAIApiKey) &&
        !string.IsNullOrEmpty(OpenAIEndpoint) &&
        !string.IsNullOrEmpty(OpenAIModel) &&
        !string.IsNullOrEmpty(OpenAIApiVersion);
}
