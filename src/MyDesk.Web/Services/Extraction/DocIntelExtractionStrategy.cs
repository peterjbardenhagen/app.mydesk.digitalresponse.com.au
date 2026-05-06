using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MyDesk.Shared.Models;
using MyDesk.Shared.Services.Extraction;

namespace MyDesk.Web.Services.Extraction;

/// <summary>
/// "Tier 1.5" extraction — Azure AI Document Intelligence (prebuilt-layout) for OCR
/// + Azure OpenAI in JSON-mode to interpret the resulting text.
///
/// This is the same proven two-stage pipeline used by <see cref="SupplierQuoteParseService"/>
/// for "Copy Supplier Quote", which works reliably across digital PDFs, scanned PDFs,
/// and image receipts. We use it for receipts/invoices because Azure OpenAI's chat
/// vision endpoint refuses <c>application/pdf</c> in <c>image_url</c> (only PNG/JPEG),
/// so we cannot send PDFs straight to GPT Vision.
/// </summary>
public class DocIntelExtractionStrategy : IDocumentExtractionStrategy
{
    private readonly AzureAIOptions _opts;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<DocIntelExtractionStrategy> _logger;

    private static readonly JsonSerializerOptions _json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    public DocIntelExtractionStrategy(
        IOptions<AzureAIOptions> opts,
        IHttpClientFactory httpFactory,
        ILogger<DocIntelExtractionStrategy> logger)
    {
        _opts = opts.Value;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    public ExtractionStrategyKind Kind => ExtractionStrategyKind.AzureDocumentIntelligence;

    public bool CanHandle(string contentType, long sizeBytes, bool digitallyGenerated)
    {
        if (!_opts.IsDocIntelConfigured || !_opts.IsConfigured) return false;
        if (sizeBytes > 50 * 1024 * 1024) return false;

        return contentType is "application/pdf"
            or "image/jpeg" or "image/jpg" or "image/png"
            or "image/tiff" or "image/tif" or "image/bmp" or "image/heif";
    }

    public async Task<ExtractedDocument> ExtractAsync(
        Stream content,
        string contentType,
        string? fileName,
        CancellationToken cancellationToken = default)
    {
        var doc = new ExtractedDocument
        {
            StrategyUsed = nameof(DocIntelExtractionStrategy),
            Currency = "AUD",
        };

        try
        {
            await using var ms = new MemoryStream();
            await content.CopyToAsync(ms, cancellationToken);
            var bytes = ms.ToArray();

            // Stage 1 — Document Intelligence layout OCR + tables.
            var (text, error) = await ExtractWithDocIntelAsync(bytes, contentType, fileName, cancellationToken);
            if (!string.IsNullOrEmpty(error))
            {
                doc.Discrepancies.Add(error);
                doc.Confidence = 0;
                doc.AuditPassed = false;
                return doc;
            }

            doc.RawText = Truncate(text, 8000);

            if (string.IsNullOrWhiteSpace(text))
            {
                doc.Discrepancies.Add("Document Intelligence returned no text. The file may be empty, password-protected, or scanned at a very low quality.");
                doc.Confidence = 0;
                return doc;
            }

            // Stage 2 — Azure OpenAI parses the text into our schema.
            await ParseWithOpenAIAsync(text, doc, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DocIntelExtractionStrategy failed for {File}", fileName);
            doc.Discrepancies.Add($"Document Intelligence extraction failed: {ex.Message}");
            doc.Confidence = 0;
            doc.AuditPassed = false;
            return doc;
        }

        ExtractionAuditor.Audit(doc);
        return doc;
    }

    // ────────────────────────────────────────────────────────────────────────
    // Stage 1: Azure Document Intelligence – prebuilt-layout
    // ────────────────────────────────────────────────────────────────────────
    private async Task<(string text, string? error)> ExtractWithDocIntelAsync(
        byte[] bytes, string mime, string? fileName, CancellationToken ct)
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

        var startResponse = await client.SendAsync(request, ct);
        if (startResponse.StatusCode != System.Net.HttpStatusCode.Accepted)
        {
            var errorBody = await startResponse.Content.ReadAsStringAsync(ct);
            _logger.LogError("DocIntel start failed for {File}. Status {Status}: {Body}", fileName, startResponse.StatusCode, errorBody);
            return (string.Empty, $"Document Intelligence rejected the upload ({(int)startResponse.StatusCode}). {ShortenError(errorBody)}");
        }

        if (!startResponse.Headers.TryGetValues("Operation-Location", out var opLocs))
            return (string.Empty, "Document Intelligence did not return an Operation-Location header.");

        var pollUrl = opLocs.First();

        for (var attempt = 0; attempt < 30; attempt++)
        {
            await Task.Delay(attempt == 0 ? 800 : 2000, ct);

            using var poll = new HttpRequestMessage(HttpMethod.Get, pollUrl);
            poll.Headers.Add("Ocp-Apim-Subscription-Key", _opts.EffectiveDocIntelKey);
            using var pollResp = await client.SendAsync(poll, ct);
            var pollBody = await pollResp.Content.ReadAsStringAsync(ct);

            if (!pollResp.IsSuccessStatusCode)
            {
                _logger.LogError("DocIntel poll failed for {File}. Status {Status}: {Body}", fileName, pollResp.StatusCode, pollBody);
                return (string.Empty, $"Document Intelligence polling failed ({(int)pollResp.StatusCode}). {ShortenError(pollBody)}");
            }

            using var d = JsonDocument.Parse(pollBody);
            var status = d.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;

            if (string.Equals(status, "failed", StringComparison.OrdinalIgnoreCase))
                return (string.Empty, $"Document Intelligence reported a failure analysing {fileName}.");

            if (string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase))
            {
                var contentText = string.Empty;
                if (d.RootElement.TryGetProperty("analyzeResult", out var ar))
                {
                    if (ar.TryGetProperty("content", out var c)) contentText = c.GetString() ?? string.Empty;

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
                            contentText = (contentText ?? string.Empty) + "\n" + tableText;
                    }
                }
                return (contentText ?? string.Empty, null);
            }
        }

        return (string.Empty, "Document Intelligence timed out before completing analysis.");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Stage 2: Azure OpenAI in JSON mode – converts extracted text to structured fields.
    // ────────────────────────────────────────────────────────────────────────
    private const string SystemPrompt = @"You are an expert at reading Australian receipts, tax invoices, and supplier purchase documents.
Given raw text extracted from a receipt/invoice, produce a single JSON object with this exact shape:

{
  ""documentType"": ""Receipt"" | ""Invoice"" | ""Quote"" | ""PurchaseOrder"",
  ""supplierName"": string,
  ""supplierAbn"": string,           // 11-digit ABN with no spaces, or """"
  ""supplierEmail"": string,
  ""documentDate"": string,           // ISO yyyy-MM-dd or """"
  ""currency"": ""AUD"",
  ""referenceNumber"": string,        // invoice/receipt/order number
  ""lineItems"": [
    {
      ""description"": string,
      ""quantity"": number|null,
      ""unitPrice"": number|null,    // ex-GST per-unit price
      ""lineTotal"": number|null,    // ex-GST line subtotal
      ""productCode"": string|null
    }
  ],
  ""subtotal"": number|null,         // ex-GST total
  ""gstAmount"": number|null,        // GST (~10% in AU)
  ""totalAmount"": number|null       // inc-GST total paid
}

Rules:
- Treat documents from retailers (Officeworks, JB Hi-Fi, Bunnings, Kogan, etc.) as 'Receipt'.
- All money values are plain decimals (no $ or commas). Use null when truly absent.
- Find numbers; do NOT calculate them. The system audits the math after.
- Skip pure header / total / freight summary rows in lineItems – include only real product rows.
- documentDate must be a real date from the document (purchase / issue / order date).
- Output JSON only, no markdown, no commentary.";

    private async Task ParseWithOpenAIAsync(string extractedText, ExtractedDocument doc, CancellationToken ct)
    {
        var client = _httpFactory.CreateClient("AzureAI");
        client.Timeout = TimeSpan.FromMinutes(2);
        client.DefaultRequestHeaders.Remove("api-key");
        client.DefaultRequestHeaders.Add("api-key", _opts.OpenAIApiKey);

        var payload = new
        {
            messages = new[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user",   content = $"Extract this receipt/invoice into JSON:\n\n{Truncate(extractedText, 40000)}" }
            },
            response_format = new { type = "json_object" },
            max_completion_tokens = 6000,
            temperature = 0.1
        };

        var body = new StringContent(JsonSerializer.Serialize(payload, _json), Encoding.UTF8, "application/json");
        var response = await client.PostAsync(_opts.ChatCompletionsUrl, body, ct);
        var responseText = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Azure OpenAI parsing returned {Status}: {Body}", response.StatusCode, responseText);
            doc.Discrepancies.Add($"Azure OpenAI returned {(int)response.StatusCode} parsing the extracted text.");
            doc.Confidence = 0.2; // Some text was extracted, but parsing failed.
            return;
        }

        try
        {
            using var d = JsonDocument.Parse(responseText);
            var content = d.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "{}";

            content = StripMarkdownFences(content);
            using var parsed = JsonDocument.Parse(content);
            var root = parsed.RootElement;

            doc.DocumentType    = TryGetString(root, "documentType");
            doc.SupplierName    = TryGetString(root, "supplierName");
            doc.SupplierAbn     = System.Text.RegularExpressions.Regex.Replace(TryGetString(root, "supplierAbn") ?? "", @"[^\d]", "");
            doc.SupplierEmail   = TryGetString(root, "supplierEmail");
            doc.DocumentDate    = TryGetDate(root,   "documentDate");
            doc.Currency        = TryGetString(root, "currency") ?? "AUD";
            doc.ReferenceNumber = TryGetString(root, "referenceNumber");
            doc.Subtotal        = TryGetDecimal(root, "subtotal");
            doc.GstAmount       = TryGetDecimal(root, "gstAmount");
            doc.TotalAmount     = TryGetDecimal(root, "totalAmount");

            if (root.TryGetProperty("lineItems", out var items) && items.ValueKind == JsonValueKind.Array)
            {
                foreach (var li in items.EnumerateArray())
                {
                    var description = TryGetString(li, "description");
                    if (string.IsNullOrWhiteSpace(description)) continue;
                    doc.LineItems.Add(new ExtractedLineItem
                    {
                        Description = description,
                        Quantity    = TryGetDecimal(li, "quantity"),
                        UnitPrice   = TryGetDecimal(li, "unitPrice"),
                        LineTotal   = TryGetDecimal(li, "lineTotal"),
                        ProductCode = TryGetString(li, "productCode"),
                    });
                }
            }

            // Confidence depends on whether we got the headline numbers we need.
            var hasMoney = doc.TotalAmount.HasValue || doc.Subtotal.HasValue;
            var hasSupplier = !string.IsNullOrWhiteSpace(doc.SupplierName);
            doc.Confidence = (hasMoney, hasSupplier) switch
            {
                (true, true)  => 0.85,
                (true, false) => 0.70,
                (false, true) => 0.55,
                _             => 0.30,
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse OpenAI response: {Body}", responseText);
            doc.Discrepancies.Add($"AI returned malformed JSON: {ex.Message}");
            doc.Confidence = 0.2;
        }
    }

    // ────────────────────────────────────────────────────────────────────────
    // Helpers
    // ────────────────────────────────────────────────────────────────────────
    private static string Truncate(string s, int max) =>
        string.IsNullOrEmpty(s) || s.Length <= max ? s ?? string.Empty : s.Substring(0, max);

    private static string ShortenError(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return "";
        try
        {
            using var d = JsonDocument.Parse(body);
            if (d.RootElement.TryGetProperty("error", out var err) &&
                err.TryGetProperty("message", out var m))
                return m.GetString() ?? "";
        }
        catch { /* not JSON */ }
        return body.Length > 300 ? body.Substring(0, 300) + "…" : body;
    }

    private static string StripMarkdownFences(string s)
    {
        if (string.IsNullOrEmpty(s)) return "{}";
        s = s.Trim();
        if (s.StartsWith("```"))
        {
            var nl = s.IndexOf('\n');
            if (nl > 0) s = s.Substring(nl + 1);
            if (s.EndsWith("```")) s = s.Substring(0, s.Length - 3);
        }
        return s.Trim();
    }

    private static string? TryGetString(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.String => v.GetString(),
            JsonValueKind.Null   => null,
            _                    => v.ToString()
        };
    }

    private static decimal? TryGetDecimal(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var v)) return null;
        return v.ValueKind switch
        {
            JsonValueKind.Number => v.GetDecimal(),
            JsonValueKind.String => decimal.TryParse(v.GetString(), out var d) ? d : null,
            _                    => null
        };
    }

    private static DateTime? TryGetDate(JsonElement el, string prop)
    {
        var s = TryGetString(el, prop);
        return DateTime.TryParse(s, out var d) ? d : null;
    }
}
