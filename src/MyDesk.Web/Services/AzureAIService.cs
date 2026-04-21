using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace MyDesk.Web.Services;

/// <summary>
/// Service for Azure OpenAI chat completions and embeddings.
/// Reads configuration from the "Azure" section of appsettings.json.
/// </summary>
public class AzureAIService
{
    private readonly AzureAIOptions _opts;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<AzureAIService> _logger;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AzureAIService(IOptions<AzureAIOptions> opts, IHttpClientFactory httpFactory, ILogger<AzureAIService> logger)
    {
        _opts = opts.Value;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public bool IsConfigured => _opts.IsConfigured;

    /// <summary>
    /// Sends a chat conversation to Azure OpenAI and returns the assistant reply.
    /// </summary>
    /// <param name="messages">Ordered conversation messages (role + content).</param>
    /// <param name="maxTokens">Maximum tokens in the reply.</param>
    /// <param name="temperature">Sampling temperature (0–2, default 0.7).</param>
    public async Task<AzureAIReply> ChatAsync(
        IEnumerable<AzureChatMessage> messages,
        int maxTokens = 1000,
        double temperature = 0.7)
    {
        if (!IsConfigured)
            return AzureAIReply.Error("Azure OpenAI is not configured. Set Azure:OpenAIEndpoint, Azure:OpenAIModel, Azure:OpenAIApiVersion, and Azure:OpenAIApiKey in appsettings.json.");

        try
        {
            var client = _httpFactory.CreateClient("AzureAI");
            client.DefaultRequestHeaders.Remove("api-key");
            client.DefaultRequestHeaders.Add("api-key", _opts.OpenAIApiKey);

            var payload = new
            {
                messages = messages.Select(m => new { role = m.Role, content = m.Content }),
                max_completion_tokens = maxTokens,
                temperature
            };

            var body = new StringContent(JsonSerializer.Serialize(payload, _json), Encoding.UTF8, "application/json");

            _logger.LogDebug("Azure AI request to {Url}", _opts.ChatCompletionsUrl);
            var response = await client.PostAsync(_opts.ChatCompletionsUrl, body);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Azure AI returned {Status}: {Body}", response.StatusCode, responseText);
                return AzureAIReply.Error($"Azure OpenAI error {(int)response.StatusCode}: {response.ReasonPhrase}");
            }

            using var doc = JsonDocument.Parse(responseText);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "(empty response)";

            return AzureAIReply.Success(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure AI chat request failed");
            return AzureAIReply.Error($"Request failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Returns a vector embedding for the given text using the configured embedding deployment.
    /// </summary>
    public async Task<float[]?> GetEmbeddingAsync(string text)
    {
        if (!IsConfigured || string.IsNullOrEmpty(_opts.OpenAIEmbeddingDeployment))
            return null;

        try
        {
            var client = _httpFactory.CreateClient("AzureAI");
            client.DefaultRequestHeaders.Remove("api-key");
            client.DefaultRequestHeaders.Add("api-key", _opts.OpenAIApiKey);

            var payload = new
            {
                input = text,
                dimensions = _opts.OpenAIEmbeddingDimensions
            };

            var body = new StringContent(JsonSerializer.Serialize(payload, _json), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(_opts.EmbeddingsUrl, body);

            if (!response.IsSuccessStatusCode) return null;

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var embedding = doc.RootElement
                .GetProperty("data")[0]
                .GetProperty("embedding")
                .EnumerateArray()
                .Select(v => v.GetSingle())
                .ToArray();

            return embedding;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure AI embedding request failed");
            return null;
        }
    }
}

public record AzureChatMessage(string Role, string Content)
{
    public static AzureChatMessage System(string content)    => new("system",    content);
    public static AzureChatMessage User(string content)      => new("user",      content);
    public static AzureChatMessage Assistant(string content) => new("assistant", content);
}

public record AzureAIReply(bool IsSuccess, string Content)
{
    public static AzureAIReply Success(string content) => new(true,  content);
    public static AzureAIReply Error(string message)   => new(false, message);
}
