using System.Data;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;
using MyDesk.Shared.Services.Extraction;

namespace MyDesk.Shared.Services;

/// <summary>
/// Service for persisting and retrieving financial extractions.
/// Implements the PRD schema: FinancialDocuments, FinancialMetadata, FinancialLineItems.
/// </summary>
public class FinancialExtractionService
{
    private readonly DatabaseService _db;
    private readonly ILogger<FinancialExtractionService> _logger;

    public FinancialExtractionService(DatabaseService db, ILogger<FinancialExtractionService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Ensure database tables exist (call from Program.cs on startup).
    /// </summary>
    public async Task EnsureTableAsync()
    {
        try
        {
            var sql = @"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FinancialDocuments')
                BEGIN
                    CREATE TABLE FinancialDocuments (
                        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                        TenantId UNIQUEIDENTIFIER NOT NULL,
                        DocumentType INT NOT NULL DEFAULT 0,
                        Status INT NOT NULL DEFAULT 0,
                        SourceUrl NVARCHAR(500) NULL,
                        ExtractionMethod NVARCHAR(50) NOT NULL DEFAULT '',
                        ConfidenceScore FLOAT NOT NULL DEFAULT 0,
                        AuditPassed BIT NOT NULL DEFAULT 0,
                        DiscrepanciesJson NVARCHAR(MAX) NULL,
                        CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
                        CreatedBy NVARCHAR(100) NULL,
                        VerifiedAt DATETIME2 NULL
                    );
                    CREATE INDEX IX_FinancialDocuments_TenantId ON FinancialDocuments(TenantId);
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FinancialMetadata')
                BEGIN
                    CREATE TABLE FinancialMetadata (
                        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                        DocumentId UNIQUEIDENTIFIER NOT NULL,
                        SupplierName NVARCHAR(200) NULL,
                        SupplierAbn NVARCHAR(50) NULL,
                        SupplierEmail NVARCHAR(200) NULL,
                        DocumentDate DATETIME2 NULL,
                        ReferenceNumber NVARCHAR(100) NULL,
                        Currency NVARCHAR(10) NOT NULL DEFAULT 'AUD',
                        Subtotal DECIMAL(18,2) NOT NULL DEFAULT 0,
                        GstAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                        TotalAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                        RawText NTEXT NULL
                    );
                    ALTER TABLE FinancialMetadata ADD CONSTRAINT FK_FinancialMetadata_DocumentId
                        FOREIGN KEY (DocumentId) REFERENCES FinancialDocuments(Id) ON DELETE CASCADE;
                END

                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FinancialLineItems')
                BEGIN
                    CREATE TABLE FinancialLineItems (
                        Id UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
                        DocumentId UNIQUEIDENTIFIER NOT NULL,
                        Description NVARCHAR(500) NULL,
                        Quantity DECIMAL(18,4) NOT NULL DEFAULT 0,
                        UnitPrice DECIMAL(18,4) NOT NULL DEFAULT 0,
                        LineTotal DECIMAL(18,2) NOT NULL DEFAULT 0,
                        ProductCode NVARCHAR(100) NULL
                    );
                    ALTER TABLE FinancialLineItems ADD CONSTRAINT FK_FinancialLineItems_DocumentId
                        FOREIGN KEY (DocumentId) REFERENCES FinancialDocuments(Id) ON DELETE CASCADE;
                END";

            await _db.ExecuteAsync(sql);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to ensure financial extraction tables exist");
        }
    }

    /// <summary>
    /// Save an extracted document and all its metadata/line items to the database.
    /// Returns the new document ID.
    /// </summary>
    public async Task<Guid> SaveExtractionAsync(
        ExtractedDocument extracted,
        Guid tenantId,
        string? createdBy = null,
        string? sourceUrl = null)
    {
        var docId = Guid.NewGuid();

        // 1. Save parent document
        await _db.ExecuteAsync(
            @"INSERT INTO FinancialDocuments (Id, TenantId, DocumentType, Status, SourceUrl,
              ExtractionMethod, ConfidenceScore, AuditPassed, DiscrepanciesJson, CreatedBy)
              VALUES (@Id, @TenantId,
                CASE WHEN @DocType = 'Quote' THEN 0 WHEN @DocType = 'Invoice' THEN 1
                     WHEN @DocType = 'Receipt' THEN 2 WHEN @DocType = 'PurchaseOrder' THEN 3 ELSE 0 END,
                CASE WHEN @AuditPassed = 1 THEN CASE WHEN @Confidence >= 0.90 THEN 1 ELSE 3 END ELSE 3 END,
                @SourceUrl, @Method, @Confidence, @AuditPassed, @Discrepancies, @CreatedBy)",
            new Dictionary<string, object?>
            {
                ["Id"] = docId,
                ["TenantId"] = tenantId,
                ["DocType"] = extracted.DocumentType ?? "",
                ["Status"] = extracted.AuditPassed && extracted.Confidence >= 0.90 ? 1 : 3,
                ["SourceUrl"] = sourceUrl,
                ["Method"] = extracted.StrategyUsed,
                ["Confidence"] = extracted.Confidence,
                ["AuditPassed"] = extracted.AuditPassed ? 1 : 0,
                ["Discrepancies"] = extracted.Discrepancies.Count > 0
                    ? JsonSerializer.Serialize(extracted.Discrepancies)
                    : null,
                ["CreatedBy"] = createdBy
            });

        // 2. Save metadata
        await _db.ExecuteAsync(
            @"INSERT INTO FinancialMetadata (Id, DocumentId, SupplierName, SupplierAbn, SupplierEmail,
              DocumentDate, ReferenceNumber, Currency, Subtotal, GstAmount, TotalAmount, RawText)
              VALUES (NEWID(), @DocId, @SupplierName, @SupplierAbn, @SupplierEmail,
                @DocumentDate, @ReferenceNumber, @Currency, @Subtotal, @GstAmount, @TotalAmount, @RawText)",
            new Dictionary<string, object?>
            {
                ["DocId"] = docId,
                ["SupplierName"] = extracted.SupplierName,
                ["SupplierAbn"] = extracted.SupplierAbn,
                ["SupplierEmail"] = extracted.SupplierEmail,
                ["DocumentDate"] = extracted.DocumentDate,
                ["ReferenceNumber"] = extracted.ReferenceNumber,
                ["Currency"] = extracted.Currency ?? "AUD",
                ["Subtotal"] = extracted.Subtotal ?? 0m,
                ["GstAmount"] = extracted.GstAmount ?? 0m,
                ["TotalAmount"] = extracted.TotalAmount ?? 0m,
                ["RawText"] = extracted.RawText
            });

        // 3. Save line items
        foreach (var item in extracted.LineItems)
        {
            await _db.ExecuteAsync(
                @"INSERT INTO FinancialLineItems (Id, DocumentId, Description, Quantity, UnitPrice, LineTotal, ProductCode)
                  VALUES (NEWID(), @DocId, @Description, @Quantity, @UnitPrice, @LineTotal, @ProductCode)",
                new Dictionary<string, object?>
                {
                    ["DocId"] = docId,
                    ["Description"] = item.Description,
                    ["Quantity"] = item.Quantity ?? 0m,
                    ["UnitPrice"] = item.UnitPrice ?? 0m,
                    ["LineTotal"] = item.LineTotal ?? 0m,
                    ["ProductCode"] = item.ProductCode
                });
        }

        return docId;
    }

    /// <summary>
    /// Get all documents for a tenant with optional status/document type filter.
    /// </summary>
    public async Task<List<FinancialDocument>> GetDocumentsAsync(
        Guid tenantId,
        int? status = null,
        int? documentType = null,
        string? search = null)
    {
        var sql = @"
            SELECT d.Id, d.TenantId, d.DocumentType, d.Status, d.SourceUrl,
                   d.ExtractionMethod, d.ConfidenceScore, d.AuditPassed, d.CreatedAt,
                   d.CreatedBy, d.VerifiedAt,
                   m.SupplierName, m.ReferenceNumber, m.TotalAmount, m.Currency
            FROM FinancialDocuments d
            LEFT JOIN FinancialMetadata m ON d.Id = m.DocumentId
            WHERE d.TenantId = @TenantId";
        var parameters = new Dictionary<string, object?> { ["TenantId"] = tenantId };

        if (status.HasValue)
        {
            sql += " AND d.Status = @Status";
            parameters["Status"] = status.Value;
        }
        if (documentType.HasValue)
        {
            sql += " AND d.DocumentType = @DocType";
            parameters["DocType"] = documentType.Value;
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            sql += @" AND (m.SupplierName LIKE @Search OR m.ReferenceNumber LIKE @Search)";
            parameters["Search"] = $"%{search}%";
        }

        sql += " ORDER BY d.CreatedAt DESC";

        var dt = await _db.QueryAsync(sql, parameters);
        return dt.Map(row => new FinancialDocument
        {
            Id = (Guid)row["Id"],
            TenantId = (Guid)row["TenantId"],
            DocumentType = Convert.ToInt32(row["DocumentType"]),
            Status = Convert.ToInt32(row["Status"]),
            SourceUrl = row["SourceUrl"]?.ToString(),
            ExtractionMethod = row["ExtractionMethod"]?.ToString() ?? "",
            ConfidenceScore = Convert.ToDouble(row["ConfidenceScore"]),
            AuditPassed = Convert.ToBoolean(row["AuditPassed"]),
            CreatedAt = (DateTime)row["CreatedAt"],
            CreatedBy = row["CreatedBy"]?.ToString(),
            VerifiedAt = row["VerifiedAt"] is DBNull ? null : (DateTime?)row["VerifiedAt"],
            SupplierName = row["SupplierName"]?.ToString(),
            ReferenceNumber = row["ReferenceNumber"]?.ToString(),
            TotalAmount = row["TotalAmount"] is DBNull ? 0m : Convert.ToDecimal(row["TotalAmount"]),
            Currency = row["Currency"]?.ToString() ?? "AUD"
        });
    }

    /// <summary>
    /// Get full extracted document with metadata and line items.
    /// </summary>
    public async Task<(FinancialDocument? Doc, FinancialMetadata? Meta, List<FinancialLineItem> Items)> GetDocumentAsync(Guid documentId)
    {
        // Get document + metadata
        var docSql = @"
            SELECT d.*, m.SupplierName, m.SupplierAbn, m.SupplierEmail, m.DocumentDate,
                   m.ReferenceNumber, m.Currency, m.Subtotal, m.GstAmount, m.TotalAmount, m.RawText
            FROM FinancialDocuments d
            LEFT JOIN FinancialMetadata m ON d.Id = m.DocumentId
            WHERE d.Id = @Id";
        var dt = await _db.QueryAsync(docSql, new Dictionary<string, object?> { ["Id"] = documentId });

        if (dt.Rows.Count == 0) return (null, null, new());

        var row = dt.Rows[0];
        var doc = new FinancialDocument
        {
            Id = (Guid)row["Id"],
            TenantId = (Guid)row["TenantId"],
            DocumentType = Convert.ToInt32(row["DocumentType"]),
            Status = Convert.ToInt32(row["Status"]),
            ExtractionMethod = row["ExtractionMethod"]?.ToString() ?? "",
            ConfidenceScore = Convert.ToDouble(row["ConfidenceScore"]),
            AuditPassed = Convert.ToBoolean(row["AuditPassed"]),
            CreatedAt = (DateTime)row["CreatedAt"],
            CreatedBy = row["CreatedBy"]?.ToString()
        };
        var meta = new FinancialMetadata
        {
            DocumentId = documentId,
            SupplierName = row["SupplierName"]?.ToString(),
            SupplierAbn = row["SupplierAbn"]?.ToString(),
            SupplierEmail = row["SupplierEmail"]?.ToString(),
            DocumentDate = row["DocumentDate"] is DBNull ? null : (DateTime?)row["DocumentDate"],
            ReferenceNumber = row["ReferenceNumber"]?.ToString(),
            Currency = row["Currency"]?.ToString() ?? "AUD",
            Subtotal = row["Subtotal"] is DBNull ? 0m : Convert.ToDecimal(row["Subtotal"]),
            GstAmount = row["GstAmount"] is DBNull ? 0m : Convert.ToDecimal(row["GstAmount"]),
            TotalAmount = row["TotalAmount"] is DBNull ? 0m : Convert.ToDecimal(row["TotalAmount"]),
            RawText = row["RawText"]?.ToString()
        };

        // Get line items
        var itemsDt = await _db.QueryAsync(
            "SELECT * FROM FinancialLineItems WHERE DocumentId = @Id ORDER BY LineTotal DESC",
            new Dictionary<string, object?> { ["Id"] = documentId });
        var items = itemsDt.Map(r => new FinancialLineItem
        {
            Id = (Guid)r["Id"],
            DocumentId = documentId,
            Description = r["Description"]?.ToString(),
            Quantity = r["Quantity"] is DBNull ? 0m : Convert.ToDecimal(r["Quantity"]),
            UnitPrice = r["UnitPrice"] is DBNull ? 0m : Convert.ToDecimal(r["UnitPrice"]),
            LineTotal = r["LineTotal"] is DBNull ? 0m : Convert.ToDecimal(r["LineTotal"]),
            ProductCode = r["ProductCode"]?.ToString()
        });

        return (doc, meta, items);
    }

    /// <summary>
    /// Update a document's status (e.g., mark as Verified after user correction).
    /// </summary>
    public async Task UpdateStatusAsync(Guid documentId, int newStatus, string? verifiedBy = null)
    {
        var sql = "UPDATE FinancialDocuments SET Status = @Status, VerifiedAt = @VerifiedAt";
        var parameters = new Dictionary<string, object?>
        {
            ["Status"] = newStatus,
            ["VerifiedAt"] = newStatus == 1 ? DateTime.Now : (object?)null,
            ["Id"] = documentId
        };

        if (verifiedBy != null)
        {
            sql += ", CreatedBy = @VerifiedBy";
            parameters["VerifiedBy"] = verifiedBy;
        }

        sql += " WHERE Id = @Id";
        await _db.ExecuteAsync(sql, parameters);
    }

    /// <summary>
    /// Update line items after user correction (self-correction UI).
    /// Deletes old items and re-inserts the corrected ones.
    /// </summary>
    public async Task UpdateLineItemsAsync(Guid documentId, List<FinancialLineItem> items)
    {
        await _db.ExecuteAsync("DELETE FROM FinancialLineItems WHERE DocumentId = @Id",
            new Dictionary<string, object?> { ["Id"] = documentId });

        foreach (var item in items)
        {
            await _db.ExecuteAsync(
                @"INSERT INTO FinancialLineItems (Id, DocumentId, Description, Quantity, UnitPrice, LineTotal, ProductCode)
                  VALUES (@Id, @DocId, @Description, @Quantity, @UnitPrice, @LineTotal, @ProductCode)",
                new Dictionary<string, object?>
                {
                    ["Id"] = Guid.NewGuid(),
                    ["DocId"] = documentId,
                    ["Description"] = item.Description,
                    ["Quantity"] = item.Quantity,
                    ["UnitPrice"] = item.UnitPrice,
                    ["LineTotal"] = item.LineTotal,
                    ["ProductCode"] = item.ProductCode
                });
        }
    }
}
