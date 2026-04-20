using Microsoft.Extensions.Logging;

namespace Techlight.MyDesk.Shared.Services;

public class JobOrderService
{
    private readonly DatabaseService _db;
    private readonly ILogger<JobOrderService> _logger;

    public JobOrderService(DatabaseService db, ILogger<JobOrderService> logger)
    { _db = db; _logger = logger; }

    public async Task<List<JobOrderRow>> GetAllAsync()
    {
        var dt = await _db.QueryAsync(@"
            SELECT j.JobOrderId, j.JobOrderNumber, j.JobOrderDate,
                   c.Company AS CompanyName,
                   s.JobOrderStatus AS StatusName,
                   u.Name AS Originator
            FROM JobOrders j
            LEFT JOIN Contacts c ON c.ContactId = j.ContactId
            LEFT JOIN JobOrderStatus s ON s.JobOrderStatusId = j.JobOrderStatusId
            LEFT JOIN Users u ON u.UserId = j.OriginatorId
            ORDER BY j.JobOrderId DESC");
        return dt.Map(r => new JobOrderRow
        {
            JobOrderId     = Convert.ToInt32(r["JobOrderId"]),
            JobOrderNumber = r["JobOrderNumber"]?.ToString() ?? "",
            JobOrderDate   = r["JobOrderDate"] == DBNull.Value ? null : Convert.ToDateTime(r["JobOrderDate"]),
            CompanyName    = r["CompanyName"]?.ToString() ?? "",
            StatusName     = r["StatusName"]?.ToString(),
            Originator     = r["Originator"]?.ToString(),
        }).ToList();
    }
}

public class JobOrderRow
{
    public int JobOrderId { get; set; }
    public string JobOrderNumber { get; set; } = "";
    public DateTime? JobOrderDate { get; set; }
    public string CompanyName { get; set; } = "";
    public string? StatusName { get; set; }
    public string? Originator { get; set; }
}
