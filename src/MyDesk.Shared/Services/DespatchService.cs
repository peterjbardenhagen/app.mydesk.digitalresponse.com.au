using Microsoft.Extensions.Logging;

namespace MyDesk.Shared.Services;

public class DespatchService
{
    private readonly DatabaseService _db;
    private readonly ILogger<DespatchService> _logger;

    public DespatchService(DatabaseService db, ILogger<DespatchService> logger)
    { _db = db; _logger = logger; }

    public async Task<List<DespatchRow>> GetAllAsync(int limit = 500)
    {
        var sql = $@"
            SELECT TOP {limit} d.DespatchId, d.DespatchDate, d.Carrier,
                   COALESCE(co.Company, i.InvCompany, i.DelCompany, 'No Customer') AS CompanyName,
                   i.InvoiceId
            FROM Despatch d
            LEFT JOIN Invoices i ON i.InvoiceId = d.InvoiceId
            LEFT JOIN Companies co ON co.CompanyId = i.CompanyId
            ORDER BY d.DespatchId DESC";
        var dt = await _db.QueryAsync(sql);
        return dt.Map(r => new DespatchRow
        {
            DespatchId    = Convert.ToInt32(r["DespatchId"]),
            DespatchDate  = r["DespatchDate"] != DBNull.Value ? Convert.ToDateTime(r["DespatchDate"]) : (DateTime?)null,
            CompanyName   = r["CompanyName"]?.ToString() ?? "No Customer",
            InvoiceId     = r["InvoiceId"] == DBNull.Value ? null : Convert.ToInt32(r["InvoiceId"]),
            InvoiceNumber = "",
            Carrier       = r["Carrier"]?.ToString() ?? "",
            TrackingNumber = "",
            Notes         = ""
        }).ToList();
    }
}

public class DespatchRow
{
    public int DespatchId { get; set; }
    public DateTime? DespatchDate { get; set; }
    public string CompanyName { get; set; } = "";
    public int? InvoiceId { get; set; }
    public string? InvoiceNumber { get; set; }
    public string? TrackingNumber { get; set; }
    public string? Carrier { get; set; }
    public string? Notes { get; set; }
}
