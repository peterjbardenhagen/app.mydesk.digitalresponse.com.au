using Microsoft.Extensions.Logging;

namespace Techlight.MyDesk.Shared.Services;

public class DespatchService
{
    private readonly DatabaseService _db;
    private readonly ILogger<DespatchService> _logger;

    public DespatchService(DatabaseService db, ILogger<DespatchService> logger)
    { _db = db; _logger = logger; }

    public async Task<List<DespatchRow>> GetAllAsync(int limit = 500)
    {
        var sql = $@"
            SELECT TOP {limit} d.*,
                   c.Company AS CompanyName,
                   i.InvoiceNumber AS InvoiceNumber
            FROM Despatch d
            LEFT JOIN Contacts c ON c.ContactId = d.ContactId
            LEFT JOIN Invoices i ON i.InvoiceId = d.InvoiceId
            ORDER BY d.DespatchId DESC";
        var dt = await _db.QueryAsync(sql);
        return dt.Map(r => new DespatchRow
        {
            DespatchId    = Convert.ToInt32(r["DespatchId"]),
            DespatchDate  = r.Table.Columns.Contains("DespatchDate") && r["DespatchDate"] != DBNull.Value
                            ? Convert.ToDateTime(r["DespatchDate"]) : (DateTime?)null,
            CompanyName   = r["CompanyName"]?.ToString() ?? "",
            InvoiceNumber = r["InvoiceNumber"]?.ToString(),
            TrackingNumber= r.Table.Columns.Contains("TrackingNumber") ? r["TrackingNumber"]?.ToString() : null,
            Carrier       = r.Table.Columns.Contains("Carrier") ? r["Carrier"]?.ToString() : null,
            Notes         = r.Table.Columns.Contains("Notes") ? r["Notes"]?.ToString() : null,
        }).ToList();
    }
}

public class DespatchRow
{
    public int DespatchId { get; set; }
    public DateTime? DespatchDate { get; set; }
    public string CompanyName { get; set; } = "";
    public string? InvoiceNumber { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    public string? Notes { get; set; }
}
