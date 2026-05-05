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
    /// Optional dedicated endpoint for Azure Document Intelligence.
    /// Falls back to <see cref="OpenAIEndpoint"/> when the same multi-service Cognitive Services
    /// resource is used.
    /// </summary>
    public string DocumentIntelligenceEndpoint { get; set; } = string.Empty;

    /// <summary>
    /// Optional dedicated key for Azure Document Intelligence. Falls back to <see cref="OpenAIApiKey"/>.
    /// </summary>
    public string DocumentIntelligenceApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Document Intelligence REST API version. Defaults to a stable GA version.
    /// </summary>
    public string DocumentIntelligenceApiVersion { get; set; } = "2024-11-30";

    /// <summary>The endpoint that should be used for Document Intelligence calls.</summary>
    public string EffectiveDocIntelEndpoint =>
        string.IsNullOrWhiteSpace(DocumentIntelligenceEndpoint) ? OpenAIEndpoint : DocumentIntelligenceEndpoint;

    /// <summary>The key that should be used for Document Intelligence calls.</summary>
    public string EffectiveDocIntelKey =>
        string.IsNullOrWhiteSpace(DocumentIntelligenceApiKey) ? OpenAIApiKey : DocumentIntelligenceApiKey;

    /// <summary>
    /// Full URL to start a layout analysis on the prebuilt-layout model.
    /// </summary>
    public string DocIntelLayoutAnalyzeUrl =>
        $"{EffectiveDocIntelEndpoint.TrimEnd('/')}/documentintelligence/documentModels/prebuilt-layout:analyze?api-version={DocumentIntelligenceApiVersion}";

    /// <summary>True when Document Intelligence calls have a chance of succeeding.</summary>
    public bool IsDocIntelConfigured =>
        !string.IsNullOrEmpty(EffectiveDocIntelEndpoint) && !string.IsNullOrEmpty(EffectiveDocIntelKey);

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
