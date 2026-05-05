using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class JobOrderService
{
    private readonly DatabaseService _db;
    private readonly ILogger<JobOrderService> _logger;

    public JobOrderService(DatabaseService db, ILogger<JobOrderService> logger)
    { _db = db; _logger = logger; }

    public async Task<List<JobOrderRow>> GetAllAsync()
    {
        try
        {
            var dt = await _db.QueryAsync(@"
                SELECT j.JobOrderId,
                       CAST(j.JobOrderId AS NVARCHAR(20))       AS JobOrderNumber,
                       j.DateAccepted                           AS JobOrderDate,
                       ISNULL(j.Company, ISNULL(co.Company,'')) AS CompanyName,
                       ''                                       AS StatusName,
                       ISNULL(u.Name,'')                        AS Originator
                FROM JobOrders j
                LEFT JOIN Companies co ON co.CompanyId = j.CompanyId
                LEFT JOIN Users u      ON u.Code = j.Code
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "JobOrders list failed; returning empty list");
            return new List<JobOrderRow>();
        }
    }

    public async Task<JobOrderDetail?> GetJobOrderAsync(int id)
    {
        try
        {
            var dt = await _db.QueryAsync(@"
            SELECT j.*,
                   c.FirstName + ' ' + c.Surname AS ContactName,
                   c.Company AS ContactCompany,
                   co.Company AS CompanyName,
                   s.JobOrderStatus AS StatusName,
                   u.Name AS OriginatorName,
                   d.Division AS DivisionName,
                   q.Reference AS SourceQuoteRef
            FROM JobOrders j
            LEFT JOIN Contacts c ON c.ContactId = j.ContactId
            LEFT JOIN Companies co ON co.CompanyId = j.CompanyId
            LEFT JOIN JobOrderStatus s ON s.JobOrderStatusId = j.JobOrderStatusId
            LEFT JOIN Users u ON u.UserId = j.OriginatorId
            LEFT JOIN Divisions d ON d.DivisionId = j.DivisionId
            LEFT JOIN Quotes q ON q.Qid = j.Qid
            WHERE j.JobOrderId = @Id", new() { ["Id"] = id });

            if (dt.Rows.Count == 0) return null;
            var r = dt.Rows[0];
            return new JobOrderDetail
            {
                JobOrderId        = Convert.ToInt32(r["JobOrderId"]),
                JobOrderNumber    = r["JobOrderNumber"]?.ToString() ?? "",
                JobOrderDate      = r["JobOrderDate"] == DBNull.Value ? null : Convert.ToDateTime(r["JobOrderDate"]),
                DivisionId        = r["DivisionId"] != DBNull.Value ? Convert.ToInt32(r["DivisionId"]) : null,
                DivisionName      = r["DivisionName"]?.ToString(),
                Qid               = r["Qid"] != DBNull.Value ? Convert.ToInt32(r["Qid"]) : null,
                SourceQuoteRef    = r["SourceQuoteRef"]?.ToString(),
                ContactId         = r["ContactId"] != DBNull.Value ? Convert.ToInt32(r["ContactId"]) : null,
                ContactName       = r["ContactName"]?.ToString(),
                ContactCompany    = r["ContactCompany"]?.ToString(),
                CompanyId         = r["CompanyId"] != DBNull.Value ? Convert.ToInt32(r["CompanyId"]) : null,
                CompanyName       = r["CompanyName"]?.ToString(),
                OriginatorId      = r["OriginatorId"] != DBNull.Value ? Convert.ToInt32(r["OriginatorId"]) : null,
                OriginatorName    = r["OriginatorName"]?.ToString(),
                JobOrderStatusId  = r["JobOrderStatusId"] != DBNull.Value ? Convert.ToInt32(r["JobOrderStatusId"]) : 0,
                StatusName        = r["StatusName"]?.ToString(),
                Notes             = r["Notes"]?.ToString(),
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetJobOrderAsync failed for ID {Id}", id);
            return null;
        }
    }

    public async Task<List<JobOrderLineItem>> GetLineItemsAsync(int jobOrderId)
    {
        var dt = await _db.QueryAsync(@"
            SELECT jc.JobOrderContentId, jc.Qty, jc.Description,
                   p.ProductCat AS CategoryName,
                   jc.Qty * ISNULL(jc.Price,0) AS LineTotal
            FROM JobOrderContents jc
            LEFT JOIN ProductCat p ON p.ProductCatId = jc.ProductCatId
            WHERE jc.JobOrderId = @Id
            ORDER BY jc.JobOrderContentId",
            new() { ["Id"] = jobOrderId });

        return dt.Map(r => new JobOrderLineItem
        {
            JobOrderContentId = Convert.ToInt32(r["JobOrderContentId"]),
            Qty               = r["Qty"] != DBNull.Value ? Convert.ToInt32(r["Qty"]) : 0,
            Description       = r["Description"]?.ToString() ?? "",
            CategoryName      = r["CategoryName"]?.ToString(),
            LineTotal         = r["LineTotal"] != DBNull.Value ? Convert.ToDecimal(r["LineTotal"]) : 0m,
        }).ToList();
    }

    public async Task<int> SaveAsync(JobOrderDetail jobOrder, List<JobOrderLineItem> lineItems, string userCode)
    {
        if (jobOrder.JobOrderId == 0)
        {
            return await CreateAsync(jobOrder, lineItems, userCode);
        }
        return await UpdateAsync(jobOrder, lineItems, userCode);
    }

    private async Task<int> CreateAsync(JobOrderDetail jobOrder, List<JobOrderLineItem> lineItems, string userCode)
    {
        var sql = @"
            INSERT INTO JobOrders (DateAccepted, DivisionId, ContactId, CompanyId, Qid,
                                    JobOrderStatusId, OriginatorId, Notes, Code)
            VALUES (@DateAccepted, @DivisionId, @ContactId, @CompanyId, @Qid,
                    @JobOrderStatusId, @OriginatorId, @Notes, @Code);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var p = new Dictionary<string, object?>
        {
            ["DateAccepted"] = (object?)jobOrder.JobOrderDate ?? DBNull.Value,
            ["DivisionId"] = (object?)jobOrder.DivisionId ?? DBNull.Value,
            ["ContactId"] = (object?)jobOrder.ContactId ?? DBNull.Value,
            ["CompanyId"] = (object?)jobOrder.CompanyId ?? DBNull.Value,
            ["Qid"] = (object?)jobOrder.Qid ?? DBNull.Value,
            ["JobOrderStatusId"] = jobOrder.JobOrderStatusId > 0 ? jobOrder.JobOrderStatusId : 1,
            ["OriginatorId"] = (object?)jobOrder.OriginatorId ?? DBNull.Value,
            ["Notes"] = (object?)jobOrder.Notes ?? DBNull.Value,
            ["Code"] = userCode
        };

        var dt = await _db.QueryAsync(sql, p);
        var newId = Convert.ToInt32(dt.Rows[0][0]);

        if (lineItems.Any())
        {
            await SaveLineItemsAsync(newId, lineItems);
        }

        return newId;
    }

    private async Task<int> UpdateAsync(JobOrderDetail jobOrder, List<JobOrderLineItem> lineItems, string userCode)
    {
        var sql = @"
            UPDATE JobOrders SET
                DateAccepted = @DateAccepted,
                DivisionId = @DivisionId,
                ContactId = @ContactId,
                CompanyId = @CompanyId,
                Qid = @Qid,
                JobOrderStatusId = @JobOrderStatusId,
                OriginatorId = @OriginatorId,
                Notes = @Notes,
                Code = @Code
            WHERE JobOrderId = @JobOrderId";

        var p = new Dictionary<string, object?>
        {
            ["DateAccepted"] = (object?)jobOrder.JobOrderDate ?? DBNull.Value,
            ["DivisionId"] = (object?)jobOrder.DivisionId ?? DBNull.Value,
            ["ContactId"] = (object?)jobOrder.ContactId ?? DBNull.Value,
            ["CompanyId"] = (object?)jobOrder.CompanyId ?? DBNull.Value,
            ["Qid"] = (object?)jobOrder.Qid ?? DBNull.Value,
            ["JobOrderStatusId"] = jobOrder.JobOrderStatusId > 0 ? jobOrder.JobOrderStatusId : 1,
            ["OriginatorId"] = (object?)jobOrder.OriginatorId ?? DBNull.Value,
            ["Notes"] = (object?)jobOrder.Notes ?? DBNull.Value,
            ["Code"] = userCode,
            ["JobOrderId"] = jobOrder.JobOrderId
        };

        await _db.ExecuteNonQueryAsync(sql, p);

        // Delete existing line items and recreate
        await _db.ExecuteNonQueryAsync("DELETE FROM JobOrderContents WHERE JobOrderId = @Id", new() { ["Id"] = jobOrder.JobOrderId });

        if (lineItems.Any())
        {
            await SaveLineItemsAsync(jobOrder.JobOrderId, lineItems);
        }

        return jobOrder.JobOrderId;
    }

    private async Task SaveLineItemsAsync(int jobOrderId, List<JobOrderLineItem> lineItems)
    {
        foreach (var item in lineItems)
        {
            var sql = @"
                INSERT INTO JobOrderContents (JobOrderId, Qty, Description, ProductCatId, Price)
                VALUES (@JobOrderId, @Qty, @Description, @ProductCatId, @Price)";

            var p = new Dictionary<string, object?>
            {
                ["JobOrderId"] = jobOrderId,
                ["Qty"] = item.Qty,
                ["Description"] = item.Description,
                ["ProductCatId"] = DBNull.Value, // Will need to map CategoryName to ProductCatId
                ["Price"] = item.LineTotal / (item.Qty > 0 ? item.Qty : 1)
            };

            await _db.ExecuteNonQueryAsync(sql, p);
        }
    }

    public async Task UpdateStatusAsync(int jobOrderId, int statusId, string userCode)
    {
        var sql = @"
            UPDATE JobOrders SET
                JobOrderStatusId = @StatusId,
                Code = @Code
            WHERE JobOrderId = @JobOrderId";

        await _db.ExecuteNonQueryAsync(sql, new()
        {
            ["StatusId"] = statusId,
            ["Code"] = userCode,
            ["JobOrderId"] = jobOrderId
        });
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

public class JobOrderDetail
{
    public int JobOrderId { get; set; }
    public string JobOrderNumber { get; set; } = "";
    public DateTime? JobOrderDate { get; set; }
    public int? DivisionId { get; set; }
    public string? DivisionName { get; set; }
    public int? Qid { get; set; }
    public string? SourceQuoteRef { get; set; }
    public int? ContactId { get; set; }
    public string? ContactName { get; set; }
    public string? ContactCompany { get; set; }
    public int? CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public int? OriginatorId { get; set; }
    public string? OriginatorName { get; set; }
    public int JobOrderStatusId { get; set; }
    public string? StatusName { get; set; }
    public string? Notes { get; set; }
}

public class JobOrderLineItem
{
    public int JobOrderContentId { get; set; }
    public int Qty { get; set; }
    public string Description { get; set; } = "";
    public string? CategoryName { get; set; }
    public decimal LineTotal { get; set; }
}
