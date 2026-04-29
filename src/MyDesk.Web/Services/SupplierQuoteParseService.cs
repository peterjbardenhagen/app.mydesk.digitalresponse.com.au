using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MyDesk.Shared;
using MyDesk.Shared.Models;

namespace MyDesk.Web.Services;

public class SupplierQuoteParseResult
{
    public List<SupplierQuoteLine> Lines { get; set; } = new();
    public string? ErrorMessage { get; set; }
    public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
}

public class SupplierQuoteLine
{
    public string Type { get; set; } = "Supply";
    public string Description { get; set; } = "";
    public decimal Quantity { get; set; } = 1;
    public decimal Units { get; set; }
    public decimal Days { get; set; }
    public decimal UnitCost { get; set; }
    public decimal NettPrice { get; set; }
}

public class SupplierQuoteParseService
{
    private readonly AzureAIOptions _opts;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<SupplierQuoteParseService> _logger;
    private readonly PlatformSettings _settings;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SupplierQuoteParseService(
        IOptions<AzureAIOptions> opts,
        IHttpClientFactory httpFactory,
        ILogger<SupplierQuoteParseService> logger,
        PlatformSettingsService settingsSvc)
    {
        _opts = opts.Value;
        _httpFactory = httpFactory;
        _logger = logger;
        _settings = settingsSvc.Current;
    }

    public bool IsConfigured => _opts.IsConfigured;

    public async Task<SupplierQuoteParseResult> ParseFilesAsync(IEnumerable<Microsoft.AspNetCore.Components.Forms.IBrowserFile> files)
    {
        var result = new SupplierQuoteParseResult();

        if (!IsConfigured)
        {
            result.ErrorMessage = "Azure OpenAI is not configured. Please check Azure settings in appsettings.json.";
            return result;
        }

        try
        {
            var allTextContent = new StringBuilder();

            foreach (var file in files)
            {
                var ext = Path.GetExtension(file.Name).ToLowerInvariant();
                if (ext == ".pdf")
                {
                    var text = await ExtractTextFromPdfAsync(file);
                    if (!string.IsNullOrEmpty(text))
                        allTextContent.AppendLine(text);
                }
                else if (ext is ".jpg" or ".jpeg" or ".png" or ".gif" or ".webp" or ".bmp")
                {
                    var text = await ExtractTextFromImageAsync(file);
                    if (!string.IsNullOrEmpty(text))
                        allTextContent.AppendLine(text);
                }
            }

            var extractedText = allTextContent.ToString();
            if (string.IsNullOrWhiteSpace(extractedText))
            {
                result.ErrorMessage = "No text could be extracted from the uploaded files.";
                return result;
            }

            var lines = await ParseQuoteLinesWithAIAsync(extractedText);
            
            var margin = _settings.GrossProfitMarginPercent > 0 ? _settings.GrossProfitMarginPercent : 30m;
            foreach (var line in lines)
            {
                line.UnitCost = line.UnitCost > 0 ? line.UnitCost : 0;
                if (line.UnitCost > 0)
                {
                    line.NettPrice = line.UnitCost / (1 - (margin / 100));
                }
            }
            
            result.Lines = lines;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing supplier quote files");
            result.ErrorMessage = $"Error parsing files: {ex.Message}";
        }

        return result;
    }

    private async Task<string> ExtractTextFromPdfAsync(Microsoft.AspNetCore.Components.Forms.IBrowserFile file)
    {
        await using var stream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var base64 = Convert.ToBase64String(ms.ToArray());

        try
        {
            var client = _httpFactory.CreateClient("AzureAI");
            client.DefaultRequestHeaders.Remove("api-key");
            client.DefaultRequestHeaders.Add("api-key", _opts.OpenAIApiKey);

            var payload = new
            {
                model = _opts.OpenAIModel,
                messages = new object[]
                {
                    new { role = "system", content = "You are a document analysis assistant. Extract all text content from the provided PDF document. Return ONLY the raw extracted text without any formatting or explanations." },
                    new { role = "user", content = new object[] { new { type = "input_image", image_url = new { url = $"data:application/pdf;base64,{base64}" } } } }
                }
            };

            var body = new StringContent(JsonSerializer.Serialize(payload, _json), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(_opts.ChatCompletionsUrl, body);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Azure AI PDF extraction returned {Status}: {Body}", response.StatusCode, responseText);
                return string.Empty;
            }

            using var doc = JsonDocument.Parse(responseText);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PDF extraction failed for {FileName}", file.Name);
            return string.Empty;
        }
    }

    private async Task<string> ExtractTextFromImageAsync(Microsoft.AspNetCore.Components.Forms.IBrowserFile file)
    {
        await using var stream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024);
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        var base64 = Convert.ToBase64String(ms.ToArray());
        var mimeType = file.ContentType;

        try
        {
            var client = _httpFactory.CreateClient("AzureAI");
            client.DefaultRequestHeaders.Remove("api-key");
            client.DefaultRequestHeaders.Add("api-key", _opts.OpenAIApiKey);

            var payload2 = new
            {
                model = _opts.OpenAIModel,
                messages = new object[]
                {
                    new { role = "system", content = "You are a document analysis assistant. Extract all text content from the provided image of a quote or invoice. Return ONLY the raw extracted text without any formatting or explanations. Include all line items with descriptions, quantities, unit prices, and totals." },
                    new { role = "user", content = new object[] { new { type = "input_image", image_url = new { url = $"data:{mimeType};base64,{base64}" } } } }
                }
            };

            var body = new StringContent(JsonSerializer.Serialize(payload2, _json), Encoding.UTF8, "application/json");
            var response = await client.PostAsync(_opts.ChatCompletionsUrl, body);
            var responseText = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Azure AI image extraction returned {Status}: {Body}", response.StatusCode, responseText);
                return string.Empty;
            }

            using var doc = JsonDocument.Parse(responseText);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Image extraction failed for {FileName}", file.Name);
            return string.Empty;
        }
    }

    private async Task<List<SupplierQuoteLine>> ParseQuoteLinesWithAIAsync(string extractedText)
    {
        var client = _httpFactory.CreateClient("AzureAI");
        client.DefaultRequestHeaders.Remove("api-key");
        client.DefaultRequestHeaders.Add("api-key", _opts.OpenAIApiKey);

        var systemPrompt = @"You are a quote parsing assistant. Your task is to extract line items from supplier quotes.
For each line item, extract: Description, Quantity, Unit Cost, and Nett Price.
Return ONLY a JSON array with no other text. Each item should have: description (string), quantity (decimal), unitCost (decimal), nettPrice (decimal), type (string, default to 'Supply').
If you cannot determine a value, use 0 for numeric fields.
Example format: [{""description"":""Item Name"",""quantity"":1,""unitCost"":10.00,""nettPrice"":15.00,""type"":""Supply""}]";

        var userPrompt = $"Extract all line items from this supplier quote:\n\n{extractedText}";

        var payload = new
        {
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            max_completion_tokens = 8000,
            temperature = 0.2
        };

        var body = new StringContent(JsonSerializer.Serialize(payload, _json), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(_opts.ChatCompletionsUrl, body);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Azure AI parsing returned {Status}: {Body}", response.StatusCode, responseText);
            return new List<SupplierQuoteLine>();
        }

        try
        {
            using var doc = JsonDocument.Parse(responseText);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "[]";

            var lines = JsonSerializer.Deserialize<List<SupplierQuoteLine>>(content, _json);
            return lines ?? new List<SupplierQuoteLine>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse AI response as JSON");
            return new List<SupplierQuoteLine>();
        }
    }
}