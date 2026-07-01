using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using MyDesk.Shared;
using MyDesk.Shared.Models;

namespace MyDesk.Web.Services;

public class SupplierQuoteParseResult
{
    public List<SupplierQuoteLine> Lines { get; set; } = new();
    public SupplierQuoteHeader Header { get; set; } = new();

    /// <summary>Raw text returned by Document Intelligence (truncated when necessary). For diagnostics.</summary>
    public string ExtractedText { get; set; } = string.Empty;

    public string? ErrorMessage { get; set; }
    public bool IsSuccess => string.IsNullOrEmpty(ErrorMessage);
}

public class SupplierQuoteHeader
{
    public string SupplierName { get; set; } = "";
    public string SupplierAbn { get; set; } = "";
    public string SupplierEmail { get; set; } = "";
    public string SupplierPhone { get; set; } = "";
    public string QuoteNumber { get; set; } = "";
    public string Reference { get; set; } = "";
    public string CustomerName { get; set; } = "";
    public string Attention { get; set; } = "";
    public DateTime? QuoteDate { get; set; }
    public int ValidityDays { get; set; }
    public string Terms { get; set; } = "";
    public string Notes { get; set; } = "";
    public decimal SubTotal { get; set; }
    public decimal Gst { get; set; }
    public decimal Total { get; set; }
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

/// <summary>
/// Parses an uploaded supplier quote (PDF or image) into structured line items.
/// Uses Azure AI Document Intelligence (prebuilt-layout) for OCR + layout extraction
/// and Azure OpenAI to interpret the result into our line-item schema.
/// </summary>
public class SupplierQuoteParseService
{
    private readonly AzureAIOptions _opts;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<SupplierQuoteParseService> _logger;
    private readonly PlatformSettings _settings;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
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
    public bool IsDocIntelConfigured => _opts.IsDocIntelConfigured;

    public async Task<SupplierQuoteParseResult> ParseFilesAsync(IEnumerable<Microsoft.AspNetCore.Components.Forms.IBrowserFile> files)
    {
        var result = new SupplierQuoteParseResult();
        var fileList = files?.ToList() ?? new List<Microsoft.AspNetCore.Components.Forms.IBrowserFile>();

        if (fileList.Count == 0)
        {
            result.ErrorMessage = "No files were uploaded.";
            return result;
        }

        if (!IsConfigured)
        {
            result.ErrorMessage = "Azure OpenAI is not configured. Add an endpoint/key under \"AzureAI\" in appsettings.json.";
            return result;
        }

        try
        {
            var allText = new StringBuilder();

            foreach (var file in fileList)
            {
                var ext = Path.GetExtension(file.Name).ToLowerInvariant();
                var mime = string.IsNullOrEmpty(file.ContentType) ? GuessMime(ext) : file.ContentType;

                if (!IsSupported(ext))
                {
                    _logger.LogWarning("Skipping unsupported file: {FileName}", file.Name);
                    continue;
                }

                _logger.LogInformation("Extracting text from {FileName} ({Ext})", file.Name, ext);

                byte[] bytes;
                await using (var stream = file.OpenReadStream(maxAllowedSize: 50 * 1024 * 1024))
                using (var ms = new MemoryStream())
                {
                    await stream.CopyToAsync(ms);
                    bytes = ms.ToArray();
                }

                string fileText;
                if (ext == ".pdf")
                {
                    // Free path: PdfPig works well for digitally-generated PDFs (supplier quotes
                    // exported from accounting software). Falls back to DocIntel for scanned docs.
                    fileText = await ExtractPdfTextWithPdfPigAsync(bytes);

                    if (string.IsNullOrWhiteSpace(fileText) || fileText.Length < 150)
                    {
                        if (IsDocIntelConfigured)
                        {
                            var (docText, docErr) = await ExtractWithDocIntelAsync(bytes, mime, file.Name);
                            if (!string.IsNullOrEmpty(docErr)) { result.ErrorMessage = docErr; return result; }
                            fileText = docText;
                        }
                        // Proceed with whatever PdfPig returned; the "no text" check below will handle it.
                    }
                }
                else
                {
                    // Images (JPG/PNG/TIFF) need OCR — DocIntel is required.
                    if (!IsDocIntelConfigured)
                    {
                        result.ErrorMessage = $"Image files ({ext}) require Azure Document Intelligence for OCR. " +
                            "Configure DocIntelEndpoint/DocIntelKey in appsettings.json, or upload a digital PDF instead.";
                        return result;
                    }
                    var (imgText, imgErr) = await ExtractWithDocIntelAsync(bytes, mime, file.Name);
                    if (!string.IsNullOrEmpty(imgErr)) { result.ErrorMessage = imgErr; return result; }
                    fileText = imgText;
                }

                if (!string.IsNullOrWhiteSpace(fileText))
                {
                    allText.AppendLine($"=== File: {file.Name} ===");
                    allText.AppendLine(fileText);
                    allText.AppendLine();
                }
            }

            var extractedText = allText.ToString().Trim();
            result.ExtractedText = Truncate(extractedText, 20000);

            if (string.IsNullOrWhiteSpace(extractedText))
            {
                result.ErrorMessage = "Document Intelligence ran but returned no text. The file may be empty, password-protected, or scanned at a very low quality.";
                return result;
            }

            _logger.LogInformation("Extracted {Length} characters; sending to Azure OpenAI for line-item parsing", extractedText.Length);

            var (header, lines) = await ParseQuoteWithAIAsync(extractedText);
            result.Header = header;

            // Apply default GP margin from PlatformSettings if AI didn't compute a sell price.
            var defaultMarginPct = _settings.GrossProfitMarginPercent > 0 ? _settings.GrossProfitMarginPercent : 30m;
            foreach (var line in lines)
            {
                if (line.Quantity <= 0) line.Quantity = 1;
                if (line.UnitCost < 0) line.UnitCost = 0;

                if (line.NettPrice <= 0 && line.UnitCost > 0)
                {
                    line.NettPrice = ApplyMargin(line.UnitCost, defaultMarginPct);
                }
                if (string.IsNullOrWhiteSpace(line.Type)) line.Type = "Supply";
            }

            result.Lines = lines;

            if (lines.Count == 0)
            {
                result.ErrorMessage = "No line items could be identified in the document. The supplier's layout may be unusual – please check the extracted text below and add lines manually.";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing supplier quote files");
            result.ErrorMessage = $"Error parsing files: {ex.Message}";
        }

        return result;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Free tier: PdfPig — zero cost, instant, works for digital PDFs
    // ────────────────────────────────────────────────────────────────────────
    private static async Task<string> ExtractPdfTextWithPdfPigAsync(byte[] bytes)
    {
        try
        {
            // Reuse the shared PdfPigExtractionStrategy which already handles text + layout.
            var strategy = new MyDesk.Shared.Services.Extraction.PdfPigExtractionStrategy();
            await using var ms = new MemoryStream(bytes);
            var doc = await strategy.ExtractAsync(ms, "application/pdf", null);
            return doc.RawText ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Azure Document Intelligence – prebuilt-layout
    // POST {endpoint}/documentintelligence/documentModels/prebuilt-layout:analyze?api-version=...
    //   body: raw bytes, Content-Type: application/pdf | image/jpeg | image/png ...
    // -> 202, header `Operation-Location: <pollUrl>`
    // Poll {pollUrl} until status == "succeeded" -> analyzeResult.content
    // ────────────────────────────────────────────────────────────────────────
    private async Task<(string text, string? error)> ExtractWithDocIntelAsync(byte[] bytes, string mime, string fileName)
    {
        var client = _httpFactory.CreateClient("DocIntel");
        client.Timeout = TimeSpan.FromMinutes(2);
        client.DefaultRequestHeaders.Remove("Ocp-Apim-Subscription-Key");
        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _opts.EffectiveDocIntelKey);

        var request = new HttpRequestMessage(HttpMethod.Post, _opts.DocIntelLayoutAnalyzeUrl)
        {
            Content = new ByteArrayContent(bytes)
        };
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(mime);

        var startResponse = await client.SendAsync(request);
        if (startResponse.StatusCode != System.Net.HttpStatusCode.Accepted)
        {
            var errorBody = await startResponse.Content.ReadAsStringAsync();
            _logger.LogError("DocIntel start failed for {FileName}. Status {Status}: {Body}", fileName, startResponse.StatusCode, errorBody);
            return (string.Empty, $"Document Intelligence rejected the upload ({(int)startResponse.StatusCode}). {ShortenError(errorBody)}");
        }

        if (!startResponse.Headers.TryGetValues("Operation-Location", out var opLocs))
        {
            return (string.Empty, "Document Intelligence did not return an Operation-Location header.");
        }
        var pollUrl = opLocs.First();

        // Poll up to ~60 seconds.
        for (var attempt = 0; attempt < 30; attempt++)
        {
            await Task.Delay(attempt == 0 ? 800 : 2000);

            using var poll = new HttpRequestMessage(HttpMethod.Get, pollUrl);
            poll.Headers.Add("Ocp-Apim-Subscription-Key", _opts.EffectiveDocIntelKey);
            using var pollResp = await client.SendAsync(poll);
            var pollBody = await pollResp.Content.ReadAsStringAsync();

            if (!pollResp.IsSuccessStatusCode)
            {
                _logger.LogError("DocIntel poll failed for {FileName}. Status {Status}: {Body}", fileName, pollResp.StatusCode, pollBody);
                return (string.Empty, $"Document Intelligence polling failed ({(int)pollResp.StatusCode}). {ShortenError(pollBody)}");
            }

            using var doc = JsonDocument.Parse(pollBody);
            var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;

            if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase))
            {
                return (string.Empty, $"Document Intelligence reported a failure analysing {fileName}.");
            }
            if (string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase))
            {
                var content = string.Empty;
                if (doc.RootElement.TryGetProperty("analyzeResult", out var ar))
                {
                    if (ar.TryGetProperty("content", out var c)) content = c.GetString() ?? string.Empty;

                    // Append a tabular hint per detected table to help the LLM align columns.
                    if (ar.TryGetProperty("tables", out var tables) && tables.ValueKind == JsonValueKind.Array)
                    {
                        var tableText = new StringBuilder();
                        var tIndex = 0;
                        foreach (var t in tables.EnumerateArray())
                        {
                            tableText.AppendLine();
                            tableText.AppendLine($"-- Table {++tIndex} --");
                            if (t.TryGetProperty("cells", out var cells) && cells.ValueKind == JsonValueKind.Array)
                            {
                                int? lastRow = null;
                                var rowSb = new StringBuilder();
                                foreach (var cell in cells.EnumerateArray()
                                    .OrderBy(x => x.GetProperty("rowIndex").GetInt32())
                                    .ThenBy(x => x.GetProperty("columnIndex").GetInt32()))
                                {
                                    var rowIdx = cell.GetProperty("rowIndex").GetInt32();
                                    var cellContent = cell.TryGetProperty("content", out var cc) ? cc.GetString() ?? "" : "";
                                    if (lastRow != null && rowIdx != lastRow)
                                    {
                                        tableText.AppendLine(rowSb.ToString().TrimEnd('|', ' '));
                                        rowSb.Clear();
                                    }
                                    rowSb.Append(cellContent.Replace("\n", " ").Trim()).Append(" | ");
                                    lastRow = rowIdx;
                                }
                                if (rowSb.Length > 0) tableText.AppendLine(rowSb.ToString().TrimEnd('|', ' '));
                            }
                        }
                        if (tableText.Length > 0)
                        {
                            content = (content ?? string.Empty) + "\n" + tableText;
                        }
                    }
                }

                return (content ?? string.Empty, null);
            }
        }

        return (string.Empty, "Document Intelligence timed out before completing analysis.");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Azure OpenAI – interpret the extracted text into structured JSON.
    // Uses response_format=json_object so the model returns parseable JSON.
    // ────────────────────────────────────────────────────────────────────────
    private async Task<(SupplierQuoteHeader header, List<SupplierQuoteLine> lines)> ParseQuoteWithAIAsync(string extractedText)
    {
        var client = _httpFactory.CreateClient("AzureAI");
        client.Timeout = TimeSpan.FromMinutes(2);
        client.DefaultRequestHeaders.Remove("api-key");
        client.DefaultRequestHeaders.Add("api-key", _opts.OpenAIApiKey);

        var systemPrompt = @"You are an expert at reading Australian supplier quotes and tax invoices.
Given raw text extracted from a supplier's PDF, produce a single JSON object with this exact shape:

{
  ""header"": {
    ""supplierName"": string,
    ""supplierAbn"": string,           // 11-digit ABN with no spaces, or """"
    ""supplierEmail"": string,
    ""supplierPhone"": string,
    ""quoteNumber"": string,           // the supplier's quote/reference number
    ""reference"": string,              // project/site/job name if any
    ""customerName"": string,           // who the quote is addressed to
    ""attention"": string,              // attention line
    ""quoteDate"": string,              // ISO yyyy-MM-dd or """"
    ""validityDays"": number,           // 0 if not stated
    ""terms"": string,                  // payment terms / delivery terms
    ""notes"": string,                  // any short notes that should appear on the new quote
    ""subTotal"": number,               // ex-GST, in AUD
    ""gst"": number,                    // GST amount
    ""total"": number                   // inc-GST total
  },
  ""lines"": [
    {
      ""type"": ""Supply"" | ""Labour"" | ""Delivery"",
      ""description"": string,
      ""quantity"": number,
      ""unitCost"": number,             // ex-GST cost per unit (what the supplier charges us)
      ""nettPrice"": number             // 0 if not present – we will calculate
    }
  ]
}

Rules:
- All prices are EX-GST decimal numbers. Do NOT include $ signs or commas.
- Treat the supplier's price column as our cost (unitCost) – NOT our sell price.
- Set nettPrice to 0 unless the document already contains a sell price column.
- If the same description spans multiple rows (sub-bullets), merge them into one description.
- Skip pure header / total / sub-total / freight summary rows. Freight charged as a real line item should be type=""Delivery"".
- Default type=""Supply"" when in doubt.
- Output JSON only. No markdown, no explanations.";

        var payload = new
        {
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = $"Extract a structured supplier quote from this text:\n\n{Truncate(extractedText, 40000)}" }
            },
            response_format = new { type = "json_object" },
            max_completion_tokens = 8000,
            temperature = 0.1
        };

        var body = new StringContent(JsonSerializer.Serialize(payload, _json), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(_opts.ChatCompletionsUrl, body);
        var responseText = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Azure OpenAI parsing returned {Status}: {Body}", response.StatusCode, responseText);
            return (new SupplierQuoteHeader(), new List<SupplierQuoteLine>());
        }

        try
        {
            using var doc = JsonDocument.Parse(responseText);
            var content = doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "{}";

            // Be forgiving if the model wraps the JSON in markdown fences.
            content = StripMarkdownFences(content);

            var parsed = JsonSerializer.Deserialize<ParsedDoc>(content, _json) ?? new ParsedDoc();
            var header = parsed.Header ?? new SupplierQuoteHeader();
            var lines = (parsed.Lines ?? new List<SupplierQuoteLine>())
                .Where(l => !string.IsNullOrWhiteSpace(l.Description))
                .ToList();

            // ABN sanity – strip spaces/dashes.
            header.SupplierAbn = Regex.Replace(header.SupplierAbn ?? "", @"[^\d]", "");
            return (header, lines);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse AI response as JSON: {Response}", responseText);
            return (new SupplierQuoteHeader(), new List<SupplierQuoteLine>());
        }
    }

    private static decimal ApplyMargin(decimal cost, decimal marginPct)
    {
        if (marginPct >= 100m) marginPct = 99m; // avoid divide-by-zero
        if (marginPct < 0) marginPct = 0;
        return Math.Round(cost / (1m - (marginPct / 100m)), 2, MidpointRounding.AwayFromZero);
    }

    private static bool IsSupported(string ext) =>
        ext is ".pdf" or ".jpg" or ".jpeg" or ".png" or ".tif" or ".tiff" or ".bmp" or ".heif";

    private static string GuessMime(string ext) => ext switch
    {
        ".pdf"  => "application/pdf",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".png"  => "image/png",
        ".tif" or ".tiff" => "image/tiff",
        ".bmp"  => "image/bmp",
        ".heif" => "image/heif",
        _ => "application/octet-stream",
    };

    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s ?? string.Empty : s.Substring(0, max);

    private static string ShortenError(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return "";
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("error", out var err))
            {
                if (err.TryGetProperty("message", out var m)) return m.GetString() ?? "";
            }
        }
        catch { /* not JSON, fall through */ }
        return body.Length > 300 ? body.Substring(0, 300) + "…" : body;
    }

    private static string StripMarkdownFences(string s)
    {
        if (string.IsNullOrEmpty(s)) return "{}";
        s = s.Trim();
        if (s.StartsWith("```"))
        {
            var firstNl = s.IndexOf('\n');
            if (firstNl > 0) s = s.Substring(firstNl + 1);
            if (s.EndsWith("```")) s = s.Substring(0, s.Length - 3);
        }
        return s.Trim();
    }

    private sealed class ParsedDoc
    {
        public SupplierQuoteHeader? Header { get; set; }
        public List<SupplierQuoteLine>? Lines { get; set; }
    }
}
