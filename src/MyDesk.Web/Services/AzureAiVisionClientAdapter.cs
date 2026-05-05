using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MyDesk.Shared.Services.Extraction;

namespace MyDesk.Web.Services;

public class AzureAiVisionClientAdapter : IAiVisionClient
{
    private readonly AzureAIOptions _opts;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<AzureAiVisionClientAdapter> _logger;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AzureAiVisionClientAdapter(
        IOptions<AzureAIOptions> opts,
        IHttpClientFactory httpFactory,
        ILogger<AzureAiVisionClientAdapter> logger)
    {
        _opts = opts.Value;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public bool IsConfigured => _opts.IsConfigured;

    public async Task<AiVisionResult> ExtractJsonAsync(
        string systemPrompt,
        string userPrompt,
        byte[] imageBytes,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return AiVisionResult.Fail("Azure OpenAI is not configured.");

        try
        {
            var client = _httpFactory.CreateClient("AzureAI");
            client.DefaultRequestHeaders.Remove("api-key");
            client.DefaultRequestHeaders.Add("api-key", _opts.OpenAIApiKey);

            var base64 = Convert.ToBase64String(imageBytes);

            var messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = userPrompt },
                        new
                        {
                            type = "image_url",
                            image_url = new
                            {
                                url = $"data:{contentType};base64,{base64}",
                                detail = "high"
                            }
                        }
                    }
                }
            };

            var payload = new
            {
                messages,
                max_completion_tokens = 4096,
                temperature = 0.1
            };

            var body = new StringContent(
                JsonSerializer.Serialize(payload, _json),
                Encoding.UTF8,
                "application/json");

            _logger.LogDebug("Azure AI Vision request to {Url}", _opts.ChatCompletionsUrl);
            var response = await client.PostAsync(_opts.ChatCompletionsUrl, body, cancellationToken);
            var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Azure AI Vision returned {Status}: {Body}", response.StatusCode, responseText);
                return AiVisionResult.Fail($"Azure OpenAI error {(int)response.StatusCode}: {response.ReasonPhrase}");
            }

            using var doc = JsonDocument.Parse(responseText);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "(empty response)";

            return AiVisionResult.Ok(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure AI Vision request failed");
            return AiVisionResult.Fail($"Vision request failed: {ex.Message}");
        }
    }
}
