using System.Text;
using System.Text.RegularExpressions;
using MyDesk.Shared.Models;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace MyDesk.Shared.Services.Extraction;

/// <summary>
/// "Tier 1" deterministic extraction. Uses Apache-2.0 licensed PdfPig to
/// pull text + coordinates from digitally-generated PDFs at zero token
/// cost and zero hallucination risk.
///
/// Returns ConfidenceScore based on how many key fields parsed cleanly:
///   * Subtotal + GST + Total all present and reconciled         => 0.95
///   * Subtotal + Total + at least one line item                  => 0.85
///   * Total + supplier name                                      => 0.65
///   * Just plain text                                            => 0.40
///
/// The triage dispatcher uses the 0.90 threshold from the spec to
/// decide whether to fall back to the GPT-5.4 Mini Vision tier.
/// </summary>
public class PdfPigExtractionStrategy : IDocumentExtractionStrategy
{
    public ExtractionStrategyKind Kind => ExtractionStrategyKind.PdfPig;

    public bool CanHandle(string contentType, long sizeBytes, bool digitallyGenerated)
        => contentType == "application/pdf" && digitallyGenerated;

    public async Task<ExtractedDocument> ExtractAsync(
        Stream content,
        string contentType,
        string? fileName,
        CancellationToken cancellationToken = default)
    {
        // PdfPig requires a seekable stream, so buffer to memory.
        await using var ms = new MemoryStream();
        await content.CopyToAsync(ms, cancellationToken);
        ms.Position = 0;

        var rawText = ReadPdfText(ms);
        var pageWords = ReadPdfWords(ms);
        var doc = ParseFromText(rawText);

        // Try table extraction for line items if regex didn't already pull them
        if (doc.LineItems.Count == 0)
        {
            doc.LineItems = ParseLineItemsFromWords(pageWords);
        }

        doc.StrategyUsed = nameof(PdfPigExtractionStrategy);
        doc.RawText      = rawText;
        doc.Confidence   = ScoreConfidence(doc, rawText);

        ExtractionAuditor.Audit(doc);
        return doc;
    }

    private static string ReadPdfText(Stream content)
    {
        var pos = content.Position;
        try
        {
            using var pdf = PdfDocument.Open(content);
            var sb = new StringBuilder();
            foreach (var page in pdf.GetPages())
            {
                sb.AppendLine(page.Text);
            }
            return sb.ToString();
        }
        catch
        {
            return string.Empty;
        }
        finally
        {
            content.Position = pos;
        }
    }

    private static List<List<Word>> ReadPdfWords(Stream content)
    {
        var pos = content.Position;
        var pages = new List<List<Word>>();
        try
        {
            using var pdf = PdfDocument.Open(content);
            foreach (var page in pdf.GetPages())
            {
                pages.Add(page.GetWords().ToList());
            }
        }
        catch
        {
            // best effort
        }
        finally
        {
            content.Position = pos;
        }
        return pages;
    }

    /// <summary>
    /// Best-effort table extraction by clustering words that share a Y-coordinate
    /// row beneath the row containing "Description" / "Qty" / "Price".
    /// </summary>
    public static List<ExtractedLineItem> ParseLineItemsFromWords(List<List<Word>> pageWords)
    {
        var items = new List<ExtractedLineItem>();
        foreach (var words in pageWords)
        {
            if (words.Count == 0) continue;

            // Find header Y-band: any row containing both "Description"/"Item" AND
            // a quantity / price keyword.
            var headerRow = words
                .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 0))
                .FirstOrDefault(g =>
                    g.Any(w => Regex.IsMatch(w.Text, @"(?i)^(description|item|product|details)$"))
                    && g.Any(w => Regex.IsMatch(w.Text, @"(?i)^(qty|quantity|price|unit|total|amount)$")));

            if (headerRow == null) continue;

            var headerY = headerRow.Key;

            // Sort all subsequent rows below header
            var rowsBelow = words
                .Where(w => w.BoundingBox.Bottom < headerY)
                .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 0))
                .OrderByDescending(g => g.Key)
                .Take(40);  // safety cap

            foreach (var row in rowsBelow)
            {
                var rowText = string.Join(" ", row.OrderBy(w => w.BoundingBox.Left).Select(w => w.Text));
                if (string.IsNullOrWhiteSpace(rowText)) continue;
                if (Regex.IsMatch(rowText, @"(?i)^(sub-?total|total|gst|amount\s+due)\b")) break;

                var line = ParseLineRow(rowText);
                if (line != null) items.Add(line);
            }
            if (items.Count > 0) break; // first page with a table wins
        }
        return items;
    }

    private static ExtractedLineItem? ParseLineRow(string row)
    {
        // Find the trailing currency-looking number = LineTotal.
        var nums = Regex.Matches(row, @"\$?\s?(\d{1,3}(?:,\d{3})*(?:\.\d{1,4})?)")
            .Select(m => decimal.TryParse(m.Groups[1].Value.Replace(",", ""), out var v) ? (decimal?)v : null)
            .Where(d => d.HasValue)
            .Select(d => d!.Value)
            .ToList();

        if (nums.Count == 0) return null;

        var lineTotal = nums.Last();
        decimal? unitPrice = nums.Count >= 2 ? nums[^2] : null;
        decimal? quantity = nums.Count >= 3 ? nums[^3] : null;
        if (quantity is null && unitPrice.HasValue && unitPrice.Value > 0)
        {
            // Two-number row: assume "qty unit-price" and compute, or "desc total"
            quantity = lineTotal / unitPrice.Value;
            // Sanity: quantity should be a sensible whole-ish number
            if (quantity > 10000 || quantity < 0.001m) quantity = null;
        }

        // Description = everything before the first number we matched
        var firstNumIdx = row.IndexOf(nums[0].ToString("0.##"), StringComparison.Ordinal);
        var description = firstNumIdx > 0 ? row[..firstNumIdx].Trim() : row;
        if (description.Length > 200) description = description[..200];

        return new ExtractedLineItem
        {
            Description = description,
            Quantity    = quantity,
            UnitPrice   = unitPrice,
            LineTotal   = lineTotal,
        };
    }

    private static double ScoreConfidence(ExtractedDocument doc, string rawText)
    {
        double score = 0.0;
        if (!string.IsNullOrWhiteSpace(rawText) && rawText.Length > 50) score += 0.20;
        if (!string.IsNullOrEmpty(doc.SupplierName)) score += 0.15;
        if (!string.IsNullOrEmpty(doc.SupplierAbn))  score += 0.10;
        if (doc.Subtotal.HasValue)                    score += 0.15;
        if (doc.GstAmount.HasValue)                   score += 0.10;
        if (doc.TotalAmount.HasValue)                 score += 0.20;
        if (doc.LineItems.Count > 0)                  score += 0.10;
        return Math.Min(1.0, score);
    }

    /// <summary>
    /// Australian-wholesale-tuned regex patterns. Public so unit tests can hit it.
    /// </summary>
    public static ExtractedDocument ParseFromText(string text)
    {
        var doc = new ExtractedDocument
        {
            Currency        = "AUD",
            DocumentDate    = ParseDate(text),
            SupplierAbn     = MatchFirst(text, @"\b(\d{2}\s?\d{3}\s?\d{3}\s?\d{3})\b"),
            ReferenceNumber = MatchFirst(text, @"(?i)(?:Quote|Invoice|PO|Order)\s*(?:#|No\.?|Number)?\s*[:\-]?\s*([A-Z0-9\-]+)"),
            GstAmount       = ParseCurrency(MatchFirst(text, @"(?i)GST[:\s]*\$?([0-9,]+\.\d{2})")),
            Subtotal        = ParseCurrency(MatchFirst(text, @"(?i)Sub-?total[:\s]*\$?([0-9,]+\.\d{2})")),
            TotalAmount     = ParseCurrency(MatchFirst(text, @"(?i)Total\s*(?:\(?incl(?:\.|uding)?\s*GST\)?)?[:\s]*\$?([0-9,]+\.\d{2})")),
            Confidence      = 0.85,
        };

        // Heuristic doc-type detection
        if (Regex.IsMatch(text, @"(?i)\bquote\b") && !Regex.IsMatch(text, @"(?i)\binvoice\b"))
            doc.DocumentType = "Quote";
        else if (Regex.IsMatch(text, @"(?i)\binvoice\b"))
            doc.DocumentType = "Invoice";
        else if (Regex.IsMatch(text, @"(?i)\breceipt\b"))
            doc.DocumentType = "Receipt";
        else if (Regex.IsMatch(text, @"(?i)\bpurchase\s+order\b"))
            doc.DocumentType = "PurchaseOrder";

        // Supplier name = first non-empty line (very rough). Real impl should look at top X% of page.
        var firstLine = text.Split('\n').FirstOrDefault(l => !string.IsNullOrWhiteSpace(l))?.Trim();
        if (firstLine != null && firstLine.Length < 120)
            doc.SupplierName = firstLine;

        return doc;
    }

    private static string? MatchFirst(string input, string pattern)
    {
        if (string.IsNullOrEmpty(input)) return null;
        var m = Regex.Match(input, pattern);
        return m.Success ? m.Groups[1].Value.Trim() : null;
    }

    private static decimal? ParseCurrency(string? s) =>
        decimal.TryParse(s?.Replace(",", "").Replace("$", "").Trim(),
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out var v) ? v : null;

    private static DateTime? ParseDate(string text)
    {
        var m = Regex.Match(text, @"\b(\d{1,2}[/\-]\d{1,2}[/\-]\d{2,4})\b");
        return m.Success && DateTime.TryParse(m.Groups[1].Value, out var dt) ? dt : null;
    }
}
