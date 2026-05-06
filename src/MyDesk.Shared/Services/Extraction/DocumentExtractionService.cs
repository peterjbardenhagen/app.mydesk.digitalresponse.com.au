using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services.Extraction;

/// <summary>
/// The Triage / Pipe-and-Filter dispatcher described in the requirements doc.
///
/// Step 1 - Triage: probe the file (PDF? image? digitally-generated?).
/// Step 2 - Tier 1: try the deterministic PdfPig strategy first (zero cost).
/// Step 3 - If confidence below threshold OR file is an image, fall back to
///          Tier 2 (GPT-5.4 Mini Vision).
/// Step 4 - Auditor: every strategy already calls <see cref="ExtractionAuditor"/>
///          before returning, so the result has AuditPassed + Discrepancies set.
/// Step 5 - JSON delivery via <see cref="ToJson"/>.
/// </summary>
public class DocumentExtractionService
{
    /// <summary>From the spec: confidence below this triggers AI fallback.</summary>
    public const double ConfidenceThreshold = 0.90;

    private readonly IReadOnlyList<IDocumentExtractionStrategy> _strategies;
    private readonly ILogger<DocumentExtractionService>? _logger;

    public DocumentExtractionService(
        IEnumerable<IDocumentExtractionStrategy> strategies,
        ILogger<DocumentExtractionService>? logger = null)
    {
        _strategies = strategies.ToList();
        _logger     = logger;
    }

    public async Task<ExtractedDocument> ProcessAsync(
        Stream content,
        string contentType,
        string? fileName = null,
        CancellationToken cancellationToken = default)
    {
        // Buffer to memory so we can re-read between strategies.
        await using var buffered = new MemoryStream();
        await content.CopyToAsync(buffered, cancellationToken);
        buffered.Position = 0;

        var digitallyGenerated = contentType == "application/pdf"
            && IsDigitallyGeneratedPdf(buffered);
        buffered.Position = 0;

        // -------- Tier 1: deterministic --------
        var tier1 = _strategies.FirstOrDefault(s =>
            s.Kind == ExtractionStrategyKind.PdfPig &&
            s.CanHandle(contentType, buffered.Length, digitallyGenerated));

        if (tier1 != null)
        {
            _logger?.LogInformation("Triage Tier 1 (PdfPig) attempt for {File}", fileName);
            buffered.Position = 0;
            var result = await tier1.ExtractAsync(buffered, contentType, fileName, cancellationToken);

            // If Tier 1 reaches the threshold AND the math reconciles, we're done.
            if (result.Confidence >= ConfidenceThreshold && result.AuditPassed)
            {
                _logger?.LogInformation("Tier 1 succeeded with confidence {Conf}", result.Confidence);
                return result;
            }

            _logger?.LogInformation(
                "Tier 1 returned confidence={Conf} auditPassed={Audit} - falling back to Tier 2",
                result.Confidence, result.AuditPassed);
        }

        // -------- Tier 2: Azure Document Intelligence + OpenAI --------
        // Preferred for PDFs (digital, scanned, password-free) and image receipts.
        // Mirrors the proven SupplierQuoteParseService pipeline used by Copy Supplier Quote.
        var tier2 = _strategies.FirstOrDefault(s =>
            s.Kind == ExtractionStrategyKind.AzureDocumentIntelligence &&
            s.CanHandle(contentType, buffered.Length, digitallyGenerated));

        if (tier2 != null)
        {
            _logger?.LogInformation("Triage Tier 2 (Document Intelligence + OpenAI) attempt for {File}", fileName);
            buffered.Position = 0;
            var result = await tier2.ExtractAsync(buffered, contentType, fileName, cancellationToken);

            // If DocIntel produced something usable, return it (caller decides on confidence).
            // Otherwise fall through to vision (only useful for true images, since GptVision
            // refuses PDFs).
            if (result.Confidence > 0 || result.LineItems.Count > 0 || result.TotalAmount.HasValue)
            {
                return result;
            }

            _logger?.LogInformation("Tier 2 (DocIntel) yielded no usable data - trying Tier 3 (GPT Vision)");
        }

        // -------- Tier 3: AI Vision (images only) --------
        var tier3 = _strategies.FirstOrDefault(s =>
            s.Kind == ExtractionStrategyKind.GptVision &&
            s.CanHandle(contentType, buffered.Length, digitallyGenerated));

        if (tier3 != null)
        {
            _logger?.LogInformation("Triage Tier 3 (GPT Vision) attempt for {File}", fileName);
            buffered.Position = 0;
            var result = await tier3.ExtractAsync(buffered, contentType, fileName, cancellationToken);
            return result;
        }

        // -------- No strategy could handle this file --------
        var fallback = new ExtractedDocument
        {
            StrategyUsed = "None",
            Confidence   = 0,
            AuditPassed  = false,
        };
        fallback.Discrepancies.Add(
            $"No extraction strategy can handle content type \"{contentType}\". " +
            "Supported: application/pdf, image/jpeg, image/png. " +
            "Configure Azure OpenAI in appsettings.json to enable AI fallback for images and scans.");
        return fallback;
    }

    /// <summary>
    /// Heuristic: read the first ~4KB of the PDF and look for the /Font dictionary.
    /// Digitally generated PDFs always have at least one font; pure scans don't.
    /// </summary>
    private static bool IsDigitallyGeneratedPdf(Stream pdfStream)
    {
        try
        {
            var buffer = new byte[Math.Min(8192, pdfStream.Length)];
            var read = pdfStream.Read(buffer, 0, buffer.Length);
            var head = System.Text.Encoding.ASCII.GetString(buffer, 0, read);
            return head.Contains("/Font") || head.Contains("/Type /Font");
        }
        catch
        {
            return false;
        }
    }

    /// <summary>Convenience JSON output for API responses.</summary>
    public static string ToJson(ExtractedDocument doc) =>
        System.Text.Json.JsonSerializer.Serialize(doc, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });
}
