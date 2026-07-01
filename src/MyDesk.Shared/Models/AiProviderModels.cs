namespace MyDesk.Shared.Models;

/// <summary>
/// Per-tenant AI provider configuration stored inside PlatformSettings.
/// Supports Azure AI Foundry, OpenAI, Google Gemini (via OpenAI-compat endpoint),
/// Ollama (self-hosted or cloud), OpenRouter, and Opencode/Zen.
/// </summary>
public class AiProviderConfig
{
    /// <summary>
    /// Which provider to use for chat completions.
    /// All supported providers expose an OpenAI-compatible /chat/completions endpoint
    /// so the same wire protocol works for all of them.
    /// </summary>
    public string Provider { get; set; } = AiProviderKind.AzureAI;

    // ── Primary chat model ──────────────────────────────────────────────
    public string ChatModel { get; set; } = "";
    public string ApiKey    { get; set; } = "";

    /// <summary>
    /// Custom base URL. Required for Ollama and self-hosted deployments.
    /// For managed services leave blank — each provider has a sensible default.
    /// </summary>
    public string BaseUrl { get; set; } = "";

    // ── Azure AI Foundry specifics ──────────────────────────────────────
    public string AzureEndpoint      { get; set; } = "";
    public string AzureApiVersion    { get; set; } = "2025-01-01-preview";
    public string AzureDeployment    { get; set; } = "";

    // ── Vision models (multi-select; only vision-capable models go here) ─
    public List<string> VisionModels     { get; set; } = new();
    public string       ActiveVisionModel { get; set; } = "";

    // ── Image generation models (multi-select) ──────────────────────────
    public List<string> ImageGenModels     { get; set; } = new();
    public string       ActiveImageGenModel { get; set; } = "";

    // ── Quality vs Cost priority ────────────────────────────────────────
    /// <summary>Quality | Balanced | Cost</summary>
    public string Priority { get; set; } = AiPriorityKind.Balanced;

    // ── Derived ────────────────────────────────────────────────────────
    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ChatModel) &&
        (Provider == AiProviderKind.Ollama || !string.IsNullOrWhiteSpace(ApiKey));

    /// <summary>
    /// Resolves the chat completions URL based on provider type.
    /// All providers use OpenAI-compatible /chat/completions format.
    /// </summary>
    public string ResolveChatUrl()
    {
        var model = !string.IsNullOrWhiteSpace(AzureDeployment) ? AzureDeployment : ChatModel;
        return Provider switch
        {
            AiProviderKind.AzureAI =>
                $"{AzureEndpoint.TrimEnd('/')}/openai/deployments/{model}/chat/completions?api-version={AzureApiVersion}",

            AiProviderKind.OpenAI =>
                "https://api.openai.com/v1/chat/completions",

            AiProviderKind.Gemini =>
                "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions",

            AiProviderKind.Openrouter =>
                "https://openrouter.ai/api/v1/chat/completions",

            AiProviderKind.Opencode =>
                $"{(string.IsNullOrWhiteSpace(BaseUrl) ? "https://api.opencode.ai" : BaseUrl.TrimEnd('/'))}/v1/chat/completions",

            AiProviderKind.Ollama =>
                $"{(string.IsNullOrWhiteSpace(BaseUrl) ? "http://localhost:11434" : BaseUrl.TrimEnd('/'))}/v1/chat/completions",

            _ when !string.IsNullOrWhiteSpace(BaseUrl) =>
                $"{BaseUrl.TrimEnd('/')}/v1/chat/completions",

            _ => ""
        };
    }

    /// <summary>
    /// Returns the authentication header value (e.g. "Bearer key" or just the key for Azure).
    /// </summary>
    public (string HeaderName, string HeaderValue) ResolveAuthHeader() => Provider switch
    {
        AiProviderKind.AzureAI => ("api-key", ApiKey),
        _                       => ("Authorization", $"Bearer {ApiKey}")
    };

    /// <summary>
    /// The model name to send in the request body.
    /// For Azure, the model is encoded in the URL (deployment name), so the body field is omitted.
    /// </summary>
    public string? RequestBodyModel => Provider == AiProviderKind.AzureAI ? null : ChatModel;
}

public static class AiProviderKind
{
    public const string AzureAI    = "AzureAI";
    public const string OpenAI     = "OpenAI";
    public const string Gemini     = "Gemini";
    public const string Ollama     = "Ollama";
    public const string Openrouter = "Openrouter";
    public const string Opencode   = "Opencode";

    public static readonly string[] All =
        [AzureAI, OpenAI, Gemini, Ollama, Openrouter, Opencode];
}

public static class AiPriorityKind
{
    public const string Quality  = "Quality";
    public const string Balanced = "Balanced";
    public const string Cost     = "Cost";
}

/// <summary>
/// Curated catalogue of well-known models grouped by provider.
/// Used to populate dropdowns in the AI Provider Settings UI.
/// Includes free tiers from Openrouter and Opencode.
/// </summary>
public static class AiModelCatalog
{
    public record ModelEntry(
        string Id,
        string DisplayName,
        bool   IsFree,
        bool   SupportsVision,
        bool   SupportsImageGen,
        string Notes = "");

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<ModelEntry>> ByProvider =
        new Dictionary<string, IReadOnlyList<ModelEntry>>
        {
            [AiProviderKind.AzureAI] =
            [
                new("gpt-4o",                "GPT-4o",                   false, true,  false, "Latest multimodal flagship"),
                new("gpt-4o-mini",           "GPT-4o mini",              false, true,  false, "Fast + cheap, vision capable"),
                new("gpt-4.1",               "GPT-4.1",                  false, true,  false),
                new("o3",                    "o3 (reasoning)",            false, false, false, "Deep reasoning"),
                new("o4-mini",               "o4-mini (reasoning)",       false, false, false),
                new("dall-e-3",              "DALL-E 3",                  false, false, true,  "Image generation"),
            ],

            [AiProviderKind.OpenAI] =
            [
                new("gpt-4o",                "GPT-4o",                   false, true,  false),
                new("gpt-4o-mini",           "GPT-4o mini",              false, true,  false),
                new("gpt-4.1",               "GPT-4.1",                  false, true,  false),
                new("o3",                    "o3",                        false, false, false),
                new("o4-mini",               "o4-mini",                   false, false, false),
                new("dall-e-3",              "DALL-E 3",                  false, false, true),
                new("gpt-image-1",           "GPT Image 1",              false, false, true),
            ],

            [AiProviderKind.Gemini] =
            [
                new("gemini-2.5-pro",        "Gemini 2.5 Pro",           false, true,  false, "Google's best reasoning model"),
                new("gemini-2.5-flash",      "Gemini 2.5 Flash",         false, true,  false, "Fast + affordable"),
                new("gemini-2.0-flash",      "Gemini 2.0 Flash",         false, true,  false),
                new("gemini-1.5-pro",        "Gemini 1.5 Pro",           false, true,  false),
                new("gemini-1.5-flash",      "Gemini 1.5 Flash",         false, true,  false),
                new("imagen-3",              "Imagen 3",                  false, false, true,  "Image generation"),
            ],

            [AiProviderKind.Ollama] =
            [
                new("llama3.3",              "Llama 3.3 70B",            true,  false, false, "Self-hosted, free"),
                new("llama3.2-vision",       "Llama 3.2 Vision 11B",     true,  true,  false, "Vision capable"),
                new("qwen3",                 "Qwen3 (latest)",            true,  false, false),
                new("deepseek-r1",           "DeepSeek R1",               true,  false, false, "Reasoning model"),
                new("gemma3",                "Gemma 3 27B",               true,  false, false),
                new("phi4",                  "Phi-4",                     true,  false, false),
                new("mistral",               "Mistral 7B",                true,  false, false),
                new("stable-diffusion",      "Stable Diffusion",          true,  false, true,  "Image gen"),
            ],

            [AiProviderKind.Openrouter] =
            [
                // Free models
                new("deepseek/deepseek-chat:free",              "DeepSeek V3 (free)",          true,  false, false, "DeepSeek V3 — free tier"),
                new("deepseek/deepseek-r1:free",                "DeepSeek R1 (free)",          true,  false, false, "Reasoning — free tier"),
                new("deepseek/deepseek-r1-0528:free",           "DeepSeek R1 0528 (free)",     true,  false, false),
                new("meta-llama/llama-3.3-70b-instruct:free",   "Llama 3.3 70B (free)",        true,  false, false),
                new("google/gemma-3-27b-it:free",               "Gemma 3 27B (free)",          true,  false, false),
                new("microsoft/phi-4:free",                     "Phi-4 (free)",                true,  false, false),
                new("qwen/qwen3-235b-a22b:free",                "Qwen3 235B (free)",           true,  false, false, "MoE — very capable"),
                new("qwen/qwen-2.5-72b-instruct:free",          "Qwen 2.5 72B (free)",         true,  false, false),
                new("owl/alpha",                                 "Owl Alpha (free)",            true,  false, false, "Openrouter free model"),
                new("laguna-ai/laguna-v1:free",                 "Laguna v1 (free)",            true,  false, false, "Openrouter free model"),
                // Paid
                new("anthropic/claude-opus-4",                  "Claude Opus 4",               false, true,  false),
                new("anthropic/claude-sonnet-4-5",              "Claude Sonnet 4.5",           false, true,  false),
                new("anthropic/claude-haiku-4-5",               "Claude Haiku 4.5",            false, true,  false),
                new("openai/gpt-4o",                            "GPT-4o",                      false, true,  false),
                new("google/gemini-2.5-pro",                    "Gemini 2.5 Pro",              false, true,  false),
                new("openai/dall-e-3",                          "DALL-E 3 (image gen)",        false, false, true),
                new("black-forest-labs/flux-pro",               "FLUX Pro (image gen)",        false, false, true),
            ],

            [AiProviderKind.Opencode] =
            [
                new("owl-alpha",             "Owl Alpha",                 true,  false, false, "Free on Opencode"),
                new("laguna",                "Laguna",                    true,  false, false, "Free on Opencode"),
                new("zen",                   "Zen",                       true,  false, false, "Opencode Zen"),
                new("deepseek-v3-flash",     "DeepSeek V3 Flash",         true,  false, false, "Fast, free on Opencode"),
            ],
        };

    /// <summary>Returns models for the given provider that support vision.</summary>
    public static IEnumerable<ModelEntry> VisionCapable(string provider) =>
        ByProvider.TryGetValue(provider, out var list)
            ? list.Where(m => m.SupportsVision)
            : Enumerable.Empty<ModelEntry>();

    /// <summary>Returns models for the given provider that can generate images.</summary>
    public static IEnumerable<ModelEntry> ImageGen(string provider) =>
        ByProvider.TryGetValue(provider, out var list)
            ? list.Where(m => m.SupportsImageGen)
            : Enumerable.Empty<ModelEntry>();

    /// <summary>Free models sorted by priority: free first, then by display name.</summary>
    public static IEnumerable<ModelEntry> ForProvider(string provider, string priority = AiPriorityKind.Balanced)
    {
        if (!ByProvider.TryGetValue(provider, out var list)) return [];
        return priority == AiPriorityKind.Cost
            ? list.OrderBy(m => m.IsFree ? 0 : 1).ThenBy(m => m.DisplayName)
            : priority == AiPriorityKind.Quality
                ? list.OrderBy(m => m.IsFree ? 1 : 0).ThenBy(m => m.DisplayName)
                : list.OrderBy(m => m.DisplayName);
    }
}
