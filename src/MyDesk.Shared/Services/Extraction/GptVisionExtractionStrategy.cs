using System.Text.Json;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services.Extraction;

/// <summary>
/// "Tier 2" Agentic AI extraction. Sends the document image (or PDF page render)
/// to GPT-5.4 Mini in JSON-mode. Used for messy scans, screenshots and any PDF
/// that the deterministic Tier 1 couldn't extract with high enough confidence.
///
/// We never ask the model to do math - it only finds numbers. The C# auditor
/// runs immediately after to verify Sum(items) + GST ≈ Total within 5 cents.
/// </summary>
public class GptVisionExtractionStrategy : IDocumentExtractionStrategy
{
    private readonly IAiVisionClient _vision;

    public GptVisionExtractionStrategy(IAiVisionClient vision) => _vision = vision;

    public ExtractionStrategyKind Kind => ExtractionStrategyKind.GptVision;

    public bool CanHandle(string contentType, long sizeBytes, bool digitallyGenerated)
    {
        if (!_vision.IsConfigured) return false;
        // Azure OpenAI's chat-completion image_url part REJECTS application/pdf
        // ("Expected base64-encoded data URL with an image MIME type"), so this
        // strategy can only handle real image MIME types. Use DocIntelExtractionStrategy
        // for PDFs and scanned receipts.
        return contentType is "image/jpeg" or "image/jpg" or "image/png" or "image/webp";
    }

    private const string SystemPrompt = """
        You are a financial document extraction specialist. Extract data from
        the document image into the requested JSON schema. Find numbers - do
        not calculate them. Use null for any field you cannot find.
        Respond with valid JSON only - no commentary, no markdown.
        """;

    private const string UserPrompt = """
        Extract this financial document into JSON matching exactly this schema:
        {
          "documentType": "Quote|Invoice|Receipt|PurchaseOrder",
          "supplierName": "string",
          "supplierAbn": "11-digit Australian Business Number or null",
          "supplierEmail": "string or null",
          "documentDate": "YYYY-MM-DD or null",
          "currency": "AUD",
          "referenceNumber": "string or null",
          "lineItems": [
            { "description": "string", "quantity": number|null, "unitPrice": number|null, "lineTotal": number|null, "productCode": "string or null" }
          ],
          "subtotal": number|null,
          "gstAmount": number|null,
          "totalAmount": number|null
        }

        Important rules:
        - Use Australian conventions (GST = 10%, ABN = 11 digits, dates dd/mm/yyyy preserved as ISO).
        - Never invent values. If unsure, use null.
        - Numbers must be plain decimals (no $ or , - return 1234.56 not "$1,234.56").
        - lineItems should be the actual table rows - skip headers / subtotal rows / freight rows.
        """;

    public async Task<ExtractedDocument> ExtractAsync(
        Stream content,
        string contentType,
        string? fileName,
        CancellationToken cancellationToken = default)
    {
        await using var ms = new MemoryStream();
        await content.CopyToAsync(ms, cancellationToken);

        var result = await _vision.ExtractJsonAsync(
            SystemPrompt, UserPrompt, ms.ToArray(), contentType, cancellationToken);

        var doc = new ExtractedDocument
        {
            StrategyUsed = nameof(GptVisionExtractionStrategy),
            Currency     = "AUD",
        };

        if (!result.IsSuccess)
        {
            doc.Discrepancies.Add($"AI vision extraction failed: {result.Error ?? "no detail"}");
            doc.Confidence = 0;
            doc.AuditPassed = false;
            return doc;
        }

        try
        {
            var json = StripJsonFence(result.Content);
            using var parsed = JsonDocument.Parse(json);
            var root = parsed.RootElement;

            doc.DocumentType    = TryGetString(root, "documentType");
            doc.SupplierName    = TryGetString(root, "supplierName");
            doc.SupplierAbn     = TryGetString(root, "supplierAbn");
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
                    doc.LineItems.Add(new ExtractedLineItem
                    {
                        Description = TryGetString(li, "description"),
                        Quantity    = TryGetDecimal(li, "quantity"),
                        UnitPrice   = TryGetDecimal(li, "unitPrice"),
                        LineTotal   = TryGetDecimal(li, "lineTotal"),
                        ProductCode = TryGetString(li, "productCode"),
                    });
                }
            }

            // Vision results have to earn confidence through the auditor.
            doc.Confidence = 0.75;
        }
        catch (Exception ex)
        {
            doc.Discrepancies.Add($"AI returned malformed JSON: {ex.Message}");
            doc.Confidence = 0;
        }

        ExtractionAuditor.Audit(doc);
        return doc;
    }

    private static string StripJsonFence(string s)
    {
        s = s.Trim();
        if (s.StartsWith("```"))
        {
            var firstNewline = s.IndexOf('\n');
            if (firstNewline > 0) s = s[(firstNewline + 1)..];
            if (s.EndsWith("```")) s = s[..^3];
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
