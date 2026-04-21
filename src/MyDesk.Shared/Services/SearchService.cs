using Microsoft.Extensions.Logging;

namespace MyDesk.Shared.Services;

/// <summary>
/// Global search across Quotes, Invoices, Purchase Orders, Contacts, Companies, and Despatch.
/// Returns unified SearchResult rows sorted newest-first.
/// </summary>
public class SearchService
{
    private readonly DatabaseService _db;
    private readonly ILogger<SearchService> _logger;

    public SearchService(DatabaseService db, ILogger<SearchService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<SearchResult>> SearchAsync(string query, int maxPerType = 20)
    {
        if (string.IsNullOrWhiteSpace(query)) return new();
        var q = $"%{query.Trim()}%";
        var results = new List<SearchResult>();

        // Quotes
        try
        {
            var dt = await _db.QueryAsync($@"
                SELECT TOP (@Limit)
                    q.Qid AS Id,
                    ISNULL(q.Reference,'') AS Title,
                    ISNULL(co.Company, ISNULL(c.CCompany,'')) AS Subtitle,
                    q.QuoteDate AS [Date],
                    ISNULL(qs.QuoteStatus,'') AS Status,
                    ISNULL(q.NettPriceTotal, 0) AS Amount
                FROM Quotes q
                LEFT JOIN Contacts c   ON c.ContactId = q.ContactId
                LEFT JOIN Companies co ON co.CompanyId = q.CompanyId
                LEFT JOIN QuoteStatus qs ON qs.QuoteStatusId = q.QuoteStatusId
                WHERE q.Reference LIKE @Q
                   OR c.CCompany LIKE @Q
                   OR co.Company LIKE @Q
                   OR CAST(q.Qid AS VARCHAR(20)) LIKE @Q
                ORDER BY q.QuoteDate DESC",
                new() { ["Q"] = q, ["Limit"] = maxPerType });

            foreach (System.Data.DataRow r in dt.Rows)
            {
                results.Add(new SearchResult
                {
                    EntityType = "Quote",
                    Id = Convert.ToInt32(r["Id"]),
                    Title = r["Title"]?.ToString() ?? "",
                    Subtitle = r["Subtitle"]?.ToString() ?? "",
                    Date = r["Date"] == DBNull.Value ? null : Convert.ToDateTime(r["Date"]),
                    Status = r["Status"]?.ToString(),
                    Amount = r["Amount"] == DBNull.Value ? null : Convert.ToDecimal(r["Amount"]),
                    Url = $"/quotes/{r["Id"]}"
                });
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Quote search failed"); }

        // Invoices
        try
        {
            var dt = await _db.QueryAsync($@"
                SELECT TOP (@Limit)
                    i.InvoiceId AS Id,
                    ISNULL(i.CCompany, ISNULL(co.Company, '')) AS Title,
                    ISNULL(i.InvCompany, '') AS Subtitle,
                    i.InvoiceDate AS [Date],
                    ISNULL(ins.InvoiceStatus,'') AS Status,
                    ISNULL(i.NettPriceTotal, 0) + ISNULL(i.GSTTotal, 0) AS Amount
                FROM Invoices i
                LEFT JOIN Companies co ON co.CompanyId = i.CompanyId
                LEFT JOIN InvoiceStatus ins ON ins.InvoiceStatusId = i.InvoiceStatusId
                WHERE i.CCompany LIKE @Q
                   OR i.InvCompany LIKE @Q
                   OR co.Company LIKE @Q
                   OR CAST(i.InvoiceId AS VARCHAR(20)) LIKE @Q
                ORDER BY i.InvoiceDate DESC",
                new() { ["Q"] = q, ["Limit"] = maxPerType });

            foreach (System.Data.DataRow r in dt.Rows)
            {
                results.Add(new SearchResult
                {
                    EntityType = "Invoice",
                    Id = Convert.ToInt32(r["Id"]),
                    Title = $"Invoice #{r["Id"]} — " + (r["Title"]?.ToString() ?? ""),
                    Subtitle = r["Subtitle"]?.ToString() ?? "",
                    Date = r["Date"] == DBNull.Value ? null : Convert.ToDateTime(r["Date"]),
                    Status = r["Status"]?.ToString(),
                    Amount = r["Amount"] == DBNull.Value ? null : Convert.ToDecimal(r["Amount"]),
                    Url = $"/invoices/{r["Id"]}"
                });
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Invoice search failed"); }

        // Purchase Orders
        try
        {
            var dt = await _db.QueryAsync($@"
                SELECT TOP (@Limit)
                    po.POid AS Id,
                    ISNULL(s.Company, '') AS Title,
                    ISNULL(po.Project, '') AS Subtitle,
                    po.PODate AS [Date],
                    ISNULL(pos.POStatus,'') AS Status,
                    ISNULL(po.PriceIncTotal, 0) AS Amount
                FROM PurchaseOrders po
                LEFT JOIN Companies s ON s.CompanyId = po.SupplierId
                LEFT JOIN PurchaseOrderStatus pos ON pos.POStatusId = po.POStatusId
                WHERE s.Company LIKE @Q
                   OR po.Project LIKE @Q
                   OR CAST(po.POid AS VARCHAR(20)) LIKE @Q
                ORDER BY po.PODate DESC",
                new() { ["Q"] = q, ["Limit"] = maxPerType });

            foreach (System.Data.DataRow r in dt.Rows)
            {
                results.Add(new SearchResult
                {
                    EntityType = "PurchaseOrder",
                    Id = Convert.ToInt32(r["Id"]),
                    Title = $"PO #{r["Id"]} — " + (r["Title"]?.ToString() ?? ""),
                    Subtitle = r["Subtitle"]?.ToString() ?? "",
                    Date = r["Date"] == DBNull.Value ? null : Convert.ToDateTime(r["Date"]),
                    Status = r["Status"]?.ToString(),
                    Amount = r["Amount"] == DBNull.Value ? null : Convert.ToDecimal(r["Amount"]),
                    Url = $"/purchase-orders/{r["Id"]}"
                });
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "PO search failed"); }

        // Contacts
        try
        {
            var dt = await _db.QueryAsync($@"
                SELECT TOP (@Limit)
                    c.ContactId AS Id,
                    ISNULL(c.FirstName,'') + ' ' + ISNULL(c.Surname,'') AS Title,
                    ISNULL(co.Company, ISNULL(c.CCompany,'')) AS Subtitle,
                    CAST(NULL AS DATETIME) AS [Date],
                    ISNULL(c.Email,'') AS Status
                FROM Contacts c
                LEFT JOIN Companies co ON co.CompanyId = c.CCompanyId
                WHERE c.FirstName LIKE @Q
                   OR c.Surname LIKE @Q
                   OR c.Email LIKE @Q
                   OR c.Phone LIKE @Q
                   OR c.Mobile LIKE @Q
                   OR co.Company LIKE @Q
                ORDER BY c.ContactId DESC",
                new() { ["Q"] = q, ["Limit"] = maxPerType });

            foreach (System.Data.DataRow r in dt.Rows)
            {
                results.Add(new SearchResult
                {
                    EntityType = "Contact",
                    Id = Convert.ToInt32(r["Id"]),
                    Title = r["Title"]?.ToString() ?? "",
                    Subtitle = r["Subtitle"]?.ToString() ?? "",
                    Date = null,
                    Status = r["Status"]?.ToString(),
                    Url = $"/contacts/{r["Id"]}/edit"
                });
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Contact search failed"); }

        // Despatch (delivery notes) — optional table
        try
        {
            var dt = await _db.QueryAsync($@"
                SELECT TOP (@Limit)
                    d.DespatchId AS Id,
                    ISNULL(d.DeliveryTo,'') AS Title,
                    ISNULL(d.Notes,'') AS Subtitle,
                    d.DespatchDate AS [Date]
                FROM Despatch d
                WHERE d.DeliveryTo LIKE @Q
                   OR d.Notes LIKE @Q
                   OR CAST(d.DespatchId AS VARCHAR(20)) LIKE @Q
                ORDER BY d.DespatchDate DESC",
                new() { ["Q"] = q, ["Limit"] = maxPerType });

            foreach (System.Data.DataRow r in dt.Rows)
            {
                results.Add(new SearchResult
                {
                    EntityType = "Despatch",
                    Id = Convert.ToInt32(r["Id"]),
                    Title = $"Despatch #{r["Id"]}",
                    Subtitle = r["Title"]?.ToString() ?? "",
                    Date = r["Date"] == DBNull.Value ? null : Convert.ToDateTime(r["Date"]),
                    Url = $"/despatch/{r["Id"]}"
                });
            }
        }
        catch (Exception ex) { _logger.LogDebug(ex, "Despatch search skipped"); }

        // Sort newest first (null dates at the end)
        return results
            .OrderByDescending(r => r.Date ?? DateTime.MinValue)
            .ThenBy(r => r.EntityType)
            .ToList();
    }
}

public class SearchResult
{
    public string EntityType { get; set; } = "";
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Subtitle { get; set; } = "";
    public DateTime? Date { get; set; }
    public string? Status { get; set; }
    public decimal? Amount { get; set; }
    public string Url { get; set; } = "";
}
