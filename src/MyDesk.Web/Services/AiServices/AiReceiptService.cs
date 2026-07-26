using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace MyDesk.Web.Services.AiServices;

/// <summary>
/// AI-powered receipt intelligence service.
/// Phase 8: Multi-model OCR ensemble, ML categorization, anomaly detection.
/// Current: thin wrapper around OpenAI Vision API.
/// Future: custom ML pipeline with active learning.
/// </summary>
public sealed class AiReceiptService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<AiReceiptService> _logger;
    private readonly AiReceiptOptions _opts;

    public AiReceiptService(
        IHttpClientFactory httpFactory,
        IOptions<AiReceiptOptions> opts,
        ILogger<AiReceiptService> logger)
    {
        _httpFactory = httpFactory;
        _logger = logger;
        _opts = opts.Value;
    }

    /// <summary>Extract structured data from a receipt image.</summary>
    public async Task<ReceiptExtractionResult?> ExtractAsync(
        Stream imageStream, string fileName, CancellationToken ct = default)
    {
        // Phase 8.1 – Multi-model OCR ensemble
        // Currently: OpenAI GPT-4 Vision (primary)
        // Planned ensemble:
        //   Primary:  OpenAI GPT-4 Vision   → best general accuracy
        //   Backup:   Azure Form Recognizer  → table extraction
        //   Fallback: Tesseract.js           → offline / cost-sensitive
        //   Selection: confidence-scoring router

        using var client = _httpFactory.CreateClient("OpenAiVision");
        
        // Convert image to base64
        using var ms = new MemoryStream();
        await imageStream.CopyToAsync(ms, ct);
        var b64 = Convert.ToBase64String(ms.ToArray());
        var mediaType = GetMediaType(fileName);

        var payload = new
        {
            model = _opts.Model ?? "gpt-4o",
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = _opts.SystemPrompt ?? DefaultPrompt },
                        new { type = "image_url", image_url = new { url = $"data:{mediaType};base64,{b64}" } }
                    }
                }
            },
            max_tokens = _opts.MaxTokens,
            temperature = 0.0
        };

        var json = JsonContent.Create(payload);
        var response = await client.PostAsync("/v1/chat/completions", json, ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OpenAI Vision returned {Status}", response.StatusCode);
            return null;
        }

        var body = await response.Content.ReadFromJsonAsync<OpenAiResponse>(ct);

        // Phase 8.2 – ML categorized result + confidence scoring
        // Planned pipeline:
        //   Receipt → OCR → Feature Extraction → ML Classifier →
        //   Category Prediction → User Confirmation → Active Learning

        return ParseResult(body);
    }

    private string GetMediaType(string fileName)
    {
        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            _ => "image/jpeg"
        };
    }

    private ReceiptExtractionResult? ParseResult(OpenAiResponse? response)
    {
        if (response?.Choices is not { Length: > 0 }) return null;

        var text = response.Choices[0].Message?.Content;
        if (string.IsNullOrWhiteSpace(text)) return null;

        // The model returns JSON inside a markdown code block
        var json = ExtractJson(text);
        if (json is null) return null;

        try
        {
            var extracted = JsonSerializer.Deserialize<ReceiptExtractionResult>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            return extracted;
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse OCR JSON from OpenAI response");
            return null;
        }
    }

    private static string? ExtractJson(string text)
    {
        var start = text.IndexOf("```json", StringComparison.Ordinal);
        if (start < 0) start = text.IndexOf('{');
        else start += 7;

        var end = text.IndexOf("```", start + 1, StringComparison.Ordinal);
        if (end < 0) end = text.LastIndexOf('}') + 1;
        if (end <= start) return null;

        return text[start..end].Trim();
    }

    // Default prompt – tuned for Australian receipts
    private const string DefaultPrompt = """
        Extract the following fields from this Australian receipt and return them as JSON.
        Fields: supplierName, transactionDate (YYYY-MM-DD), grossAmount (numeric),
        gstAmount (numeric – the GST component, not the total),
        currency (default AUD), abn (Australian Business Number if visible),
        receiptType (TaxInvoice | Receipt | CreditNote | EFTPOS),
        description (short merchant category description).
        If a field is not visible, omit it rather than guessing.
        Confidence: return extractionConfidence (0-1) for each field.
        """;

    // Phase 8.3 – anomaly detection ready stub
    public AnomalyReport DetectAnomalies(ReceiptExtractionResult extracted)
        => new()
        {
            IsDuplicate = false,   // Phase 8.3 – perceptual hashing
            IsOutlier = false,     // Phase 8.3 – statistical outlier detection
            PolicyViolations = [], // Phase 8.3 – rule engine
            RiskScore = 0.0
        };
}

// ─── Configuration ────────────────────────────────────────────────

public sealed class AiReceiptOptions
{
    public string? Model { get; set; }
    public string? SystemPrompt { get; set; }
    public int MaxTokens { get; set; } = 1000;
}

// ─── DTOs ─────────────────────────────────────────────────────────

public sealed class ReceiptExtractionResult
{
    public string? SupplierName { get; set; }
    public string? TransactionDate { get; set; }
    public decimal? GrossAmount { get; set; }
    public decimal? GstAmount { get; set; }
    public string? Currency { get; set; }
    public string? Abn { get; set; }
    public string? ReceiptType { get; set; }
    public string? Description { get; set; }
    public double? ExtractionConfidence { get; set; }
}

public sealed class AnomalyReport
{
    public bool IsDuplicate { get; set; }
    public bool IsOutlier { get; set; }
    public string[] PolicyViolations { get; set; } = [];
    public double RiskScore { get; set; }
}

// ─── OpenAI response types ────────────────────────────────────────

internal sealed class OpenAiResponse
{
    public OpenAiChoice[]? Choices { get; set; }
}

internal sealed class OpenAiChoice
{
    public OpenAiMessage? Message { get; set; }
}

internal sealed class OpenAiMessage
{
    public string? Content { get; set; }
}
