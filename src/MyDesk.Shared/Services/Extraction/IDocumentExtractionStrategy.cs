using System;
using System.Threading;
using System.Threading.Tasks;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services.Extraction;

/// <summary>
/// A canonical, schema-enforced result returned by every extraction strategy
/// (PdfPig deterministic, Azure Document Intelligence, GPT-5.4 Vision).
/// </summary>
public class ExtractedDocument
{
    public string?  DocumentType    { get; set; }   // Quote / Invoice / Receipt / PurchaseOrder
    public string?  SupplierName    { get; set; }
    public string?  SupplierAbn     { get; set; }
    public string?  SupplierEmail   { get; set; }
    public DateTime? DocumentDate   { get; set; }
    public string?  Currency        { get; set; } = "AUD";
    public string?  ReferenceNumber { get; set; }
    public List<ExtractedLineItem> LineItems { get; set; } = new();
    public decimal? Subtotal        { get; set; }
    public decimal? GstAmount       { get; set; }
    public decimal? TotalAmount     { get; set; }

    /// <summary>0..1 self-reported confidence from the extractor.</summary>
    public double   Confidence      { get; set; }

    /// <summary>Did the math reconcile (sum of lines + GST ≈ total)?</summary>
    public bool     AuditPassed     { get; set; }

    public List<string> Discrepancies { get; set; } = new();
    public string   StrategyUsed    { get; set; } = "";
    public string?  RawText         { get; set; }
}

public class ExtractedLineItem
{
    public string?  Description { get; set; }
    public decimal? Quantity    { get; set; }
    public decimal? UnitPrice   { get; set; }
    public decimal? LineTotal   { get; set; }
    public string?  ProductCode { get; set; }
}

public enum ExtractionStrategyKind
{
    /// <summary>Free, deterministic, fast. Good for digital PDFs.</summary>
    PdfPig,

    /// <summary>Azure AI Document Intelligence prebuilt-invoice / prebuilt-receipt.</summary>
    AzureDocumentIntelligence,

    /// <summary>GPT-5.4 multimodal vision (existing Azure OpenAI deployment).</summary>
    GptVision,
}

/// <summary>
/// Strategy contract. Each implementation is registered as <see cref="IDocumentExtractionStrategy"/>
/// and selected by <see cref="DocumentExtractionService"/> based on file type, size, configuration.
/// </summary>
public interface IDocumentExtractionStrategy
{
    ExtractionStrategyKind Kind { get; }

    /// <summary>True if this strategy is willing to attempt the file.</summary>
    bool CanHandle(string contentType, long sizeBytes, bool digitallyGenerated);

    Task<ExtractedDocument> ExtractAsync(
        Stream content,
        string contentType,
        string? fileName,
        CancellationToken cancellationToken = default);
}
