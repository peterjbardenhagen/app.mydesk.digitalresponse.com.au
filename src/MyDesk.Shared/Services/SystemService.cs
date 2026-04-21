using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

/// <summary>
/// System/setup admin: Divisions, Locations, Parameters, currencies, financial years, etc.
/// Simple CRUD for reference data.
/// </summary>
public class SystemService
{
    private readonly DatabaseService _db;
    private readonly ILogger<SystemService> _logger;

    public SystemService(DatabaseService db, ILogger<SystemService> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ---------- Divisions ----------
    public async Task<List<Division>> GetDivisionsAsync()
    {
        var dt = await _db.QueryAsync("SELECT DivisionId, Division AS DivisionName FROM Divisions ORDER BY Division");
        return dt.Map(r => new Division
        {
            DivisionId = (int)r["DivisionId"],
            DivisionName = r["DivisionName"]?.ToString() ?? ""
        }).ToList();
    }

    public async Task<int> SaveDivisionAsync(Division d)
    {
        if (d.DivisionId == 0)
        {
            var nextId = await _db.ScalarAsync<int>("SELECT ISNULL(MAX(DivisionId), 0) + 1 FROM Divisions");
            await _db.ExecuteAsync("INSERT INTO Divisions (DivisionId, Division) VALUES (@id, @n)",
                new() { ["id"] = nextId, ["n"] = d.DivisionName });
            return nextId;
        }
        await _db.ExecuteAsync("UPDATE Divisions SET Division = @n WHERE DivisionId = @id",
            new() { ["n"] = d.DivisionName, ["id"] = d.DivisionId });
        return d.DivisionId;
    }

    public async Task DeleteDivisionAsync(int id) =>
        await _db.ExecuteAsync("DELETE FROM Divisions WHERE DivisionId = @id", new() { ["id"] = id });

    // ---------- Locations ----------
    public async Task<List<Location>> GetLocationsAsync()
    {
        var dt = await _db.QueryAsync("SELECT LocationId, ISNULL(LocationName,'') AS LocationName FROM Locations ORDER BY LocationName");
        return dt.Map(r => new Location
        {
            LocationId = (int)r["LocationId"],
            LocationName = r["LocationName"]?.ToString() ?? ""
        }).ToList();
    }

    public async Task<int> SaveLocationAsync(Location l)
    {
        if (l.LocationId == 0)
        {
            var nextId = await _db.ScalarAsync<int>("SELECT ISNULL(MAX(LocationId), 0) + 1 FROM Locations");
            await _db.ExecuteAsync("INSERT INTO Locations (LocationId, LocationName) VALUES (@id, @n)",
                new() { ["id"] = nextId, ["n"] = l.LocationName });
            return nextId;
        }
        await _db.ExecuteAsync("UPDATE Locations SET LocationName = @n WHERE LocationId = @id",
            new() { ["n"] = l.LocationName, ["id"] = l.LocationId });
        return l.LocationId;
    }

    public async Task DeleteLocationAsync(int id) =>
        await _db.ExecuteAsync("DELETE FROM Locations WHERE LocationId = @id", new() { ["id"] = id });

    // ---------- Parameters (key/value system settings) ----------
    public async Task<Dictionary<string, string?>> GetParametersAsync()
    {
        var dt = await _db.QueryAsync("SELECT TOP 1 * FROM Parameters");
        var result = new Dictionary<string, string?>();
        if (dt.Rows.Count == 0) return result;
        foreach (System.Data.DataColumn col in dt.Columns)
        {
            result[col.ColumnName] = dt.Rows[0][col]?.ToString();
        }
        return result;
    }

    // ---------- User Roles ----------
    public async Task<List<UserRole>> GetUserRolesAsync()
    {
        var dt = await _db.QueryAsync("SELECT UserRoleId, UserRole AS RoleName FROM UserRoles ORDER BY UserRole");
        return dt.Map(r => new UserRole
        {
            UserRoleId = (int)r["UserRoleId"],
            RoleName = r["RoleName"]?.ToString() ?? ""
        }).ToList();
    }

    // ---------- Currencies ----------
    public async Task<List<(int Id, string Code, string Name, decimal Rate)>> GetCurrenciesAsync()
    {
        var dt = await _db.QueryAsync("SELECT * FROM Currency ORDER BY Currency");
        var list = new List<(int, string, string, decimal)>();
        foreach (System.Data.DataRow r in dt.Rows)
        {
            var id = r.Table.Columns.Contains("CurrencyId") ? Convert.ToInt32(r["CurrencyId"]) : 0;
            var code = r.Table.Columns.Contains("Currency") ? r["Currency"]?.ToString() ?? "" : "";
            var name = r.Table.Columns.Contains("CurrencyName") ? r["CurrencyName"]?.ToString() ?? "" : code;
            var rate = r.Table.Columns.Contains("ExchangeRate") && r["ExchangeRate"] != DBNull.Value
                ? Convert.ToDecimal(r["ExchangeRate"]) : 1m;
            list.Add((id, code, name, rate));
        }
        return list;
    }

    // ---------- Quote Status ----------
    public async Task<List<QuoteStatus>> GetQuoteStatusesAdminAsync()
    {
        var dt = await _db.QueryAsync("SELECT QuoteStatusId, ISNULL(QuoteStatus,'') AS StatusName FROM QuoteStatus ORDER BY QuoteStatusId");
        return dt.Map(r => new QuoteStatus
        {
            QuoteStatusId = Convert.ToInt32(r["QuoteStatusId"]),
            StatusName    = r["StatusName"]?.ToString() ?? ""
        }).ToList();
    }

    public async Task SaveQuoteStatusAsync(QuoteStatus s)
    {
        if (s.QuoteStatusId == 0)
            await _db.InsertAsync("INSERT INTO QuoteStatus (QuoteStatus) VALUES (@n)", new() { ["n"] = s.StatusName });
        else
            await _db.ExecuteAsync("UPDATE QuoteStatus SET QuoteStatus = @n WHERE QuoteStatusId = @id",
                new() { ["n"] = s.StatusName, ["id"] = s.QuoteStatusId });
    }

    public async Task DeleteQuoteStatusAsync(int id) =>
        await _db.ExecuteAsync("DELETE FROM QuoteStatus WHERE QuoteStatusId = @id", new() { ["id"] = id });

    // ---------- Invoice Status ----------
    public async Task<List<InvoiceStatus>> GetInvoiceStatusesAdminAsync()
    {
        var dt = await _db.QueryAsync("SELECT InvoiceStatusId, ISNULL(InvoiceStatus,'') AS StatusName FROM InvoiceStatus ORDER BY InvoiceStatusId");
        return dt.Map(r => new InvoiceStatus
        {
            InvoiceStatusId = Convert.ToInt32(r["InvoiceStatusId"]),
            StatusName      = r["StatusName"]?.ToString() ?? ""
        }).ToList();
    }

    public async Task SaveInvoiceStatusAsync(InvoiceStatus s)
    {
        if (s.InvoiceStatusId == 0)
            await _db.InsertAsync("INSERT INTO InvoiceStatus (InvoiceStatus) VALUES (@n)", new() { ["n"] = s.StatusName });
        else
            await _db.ExecuteAsync("UPDATE InvoiceStatus SET InvoiceStatus = @n WHERE InvoiceStatusId = @id",
                new() { ["n"] = s.StatusName, ["id"] = s.InvoiceStatusId });
    }

    public async Task DeleteInvoiceStatusAsync(int id) =>
        await _db.ExecuteAsync("DELETE FROM InvoiceStatus WHERE InvoiceStatusId = @id", new() { ["id"] = id });

    // ---------- PO Status ----------
    public async Task<List<POStatus>> GetPOStatusesAdminAsync()
    {
        var dt = await _db.QueryAsync("SELECT POStatusId, ISNULL(POStatus,'') AS StatusName FROM PurchaseOrderStatus ORDER BY POStatusId");
        return dt.Map(r => new POStatus
        {
            POStatusId = Convert.ToInt32(r["POStatusId"]),
            StatusName = r["StatusName"]?.ToString() ?? ""
        }).ToList();
    }

    public async Task SavePOStatusAsync(POStatus s)
    {
        if (s.POStatusId == 0)
        {
            var nextId = await _db.ScalarAsync<int>("SELECT ISNULL(MAX(POStatusId), 0) + 1 FROM PurchaseOrderStatus");
            await _db.ExecuteAsync("INSERT INTO PurchaseOrderStatus (POStatusId, POStatus) VALUES (@id, @n)", new() { ["id"] = nextId, ["n"] = s.StatusName });
        }
        else
            await _db.ExecuteAsync("UPDATE PurchaseOrderStatus SET POStatus = @n WHERE POStatusId = @id",
                new() { ["n"] = s.StatusName, ["id"] = s.POStatusId });
    }

    public async Task DeletePOStatusAsync(int id) =>
        await _db.ExecuteAsync("DELETE FROM PurchaseOrderStatus WHERE POStatusId = @id", new() { ["id"] = id });

    // ---------- User Roles (Save + Delete) ----------
    public async Task SaveUserRoleAsync(UserRole r)
    {
        if (r.UserRoleId == 0)
            await _db.InsertAsync("INSERT INTO UserRoles (UserRole) VALUES (@n)", new() { ["n"] = r.RoleName });
        else
            await _db.ExecuteAsync("UPDATE UserRoles SET UserRole = @n WHERE UserRoleId = @id",
                new() { ["n"] = r.RoleName, ["id"] = r.UserRoleId });
    }

    public async Task DeleteUserRoleAsync(int id) =>
        await _db.ExecuteAsync("DELETE FROM UserRoles WHERE UserRoleId = @id", new() { ["id"] = id });

    // ---------- Activity Types ----------
    public async Task<List<ActivityType>> GetActivityTypesAsync()
    {
        var dt = await _db.QueryAsync("SELECT ActivityTypeId, ISNULL(ActivityType,'') AS ActivityTypeName FROM ActivityTypes ORDER BY ActivityType");
        return dt.Map(r => new ActivityType
        {
            ActivityTypeId   = Convert.ToInt32(r["ActivityTypeId"]),
            ActivityTypeName = r["ActivityTypeName"]?.ToString() ?? ""
        }).ToList();
    }

    public async Task SaveActivityTypeAsync(ActivityType a)
    {
        if (a.ActivityTypeId == 0)
            await _db.InsertAsync("INSERT INTO ActivityTypes (ActivityType) VALUES (@n)", new() { ["n"] = a.ActivityTypeName });
        else
            await _db.ExecuteAsync("UPDATE ActivityTypes SET ActivityType = @n WHERE ActivityTypeId = @id",
                new() { ["n"] = a.ActivityTypeName, ["id"] = a.ActivityTypeId });
    }

    public async Task DeleteActivityTypeAsync(int id) =>
        await _db.ExecuteAsync("DELETE FROM ActivityTypes WHERE ActivityTypeId = @id", new() { ["id"] = id });

    // ---------- Job Order Status ----------
    public async Task<List<JobOrderStatus>> GetJobOrderStatusesAsync()
    {
        var dt = await _db.QueryAsync("SELECT JobOrderStatusId, ISNULL(JobOrderStatus,'') AS StatusName FROM JobOrderStatus ORDER BY JobOrderStatusId");
        return dt.Map(r => new JobOrderStatus
        {
            JobOrderStatusId = Convert.ToInt32(r["JobOrderStatusId"]),
            StatusName       = r["StatusName"]?.ToString() ?? ""
        }).ToList();
    }

    public async Task SaveJobOrderStatusAsync(JobOrderStatus s)
    {
        if (s.JobOrderStatusId == 0)
            await _db.InsertAsync("INSERT INTO JobOrderStatus (JobOrderStatus) VALUES (@n)", new() { ["n"] = s.StatusName });
        else
            await _db.ExecuteAsync("UPDATE JobOrderStatus SET JobOrderStatus = @n WHERE JobOrderStatusId = @id",
                new() { ["n"] = s.StatusName, ["id"] = s.JobOrderStatusId });
    }

    public async Task DeleteJobOrderStatusAsync(int id) =>
        await _db.ExecuteAsync("DELETE FROM JobOrderStatus WHERE JobOrderStatusId = @id", new() { ["id"] = id });

    // ---------- Part Codes ----------
    public async Task<List<PartCode>> GetPartCodesAsync()
    {
        var dt = await _db.QueryAsync("SELECT PartCodeId, ISNULL(PartCode,'') AS PartCodeName, ISNULL(Description,'') AS Description FROM PartCodes ORDER BY PartCode");
        return dt.Map(r => new PartCode
        {
            PartCodeId   = Convert.ToInt32(r["PartCodeId"]),
            PartCodeName = r["PartCodeName"]?.ToString() ?? "",
            Description  = r["Description"]?.ToString()
        }).ToList();
    }

    public async Task SavePartCodeAsync(PartCode p)
    {
        if (p.PartCodeId == 0)
            await _db.InsertAsync("INSERT INTO PartCodes (PartCode, Description) VALUES (@n, @d)",
                new() { ["n"] = p.PartCodeName, ["d"] = (object?)p.Description ?? DBNull.Value });
        else
            await _db.ExecuteAsync("UPDATE PartCodes SET PartCode = @n, Description = @d WHERE PartCodeId = @id",
                new() { ["n"] = p.PartCodeName, ["d"] = (object?)p.Description ?? DBNull.Value, ["id"] = p.PartCodeId });
    }

    public async Task DeletePartCodeAsync(int id) =>
        await _db.ExecuteAsync("DELETE FROM PartCodes WHERE PartCodeId = @id", new() { ["id"] = id });

    // ---------- Currency Rates ----------
    public async Task<List<CurrencyRate>> GetCurrencyRatesAsync()
    {
        var dt = await _db.QueryAsync("SELECT CurrencyId, ISNULL(Currency,'') AS Code, ISNULL(CurrencyName,'') AS Name, ISNULL(ExchangeRate,1) AS Rate FROM Currency ORDER BY Currency");
        return dt.Map(r => new CurrencyRate
        {
            CurrencyId = Convert.ToInt32(r["CurrencyId"]),
            Code       = r["Code"]?.ToString() ?? "",
            Name       = r["Name"]?.ToString() ?? "",
            Rate       = r["Rate"] != DBNull.Value ? Convert.ToDecimal(r["Rate"]) : 1m
        }).ToList();
    }

    public async Task SaveCurrencyRateAsync(CurrencyRate c)
    {
        if (c.CurrencyId == 0)
            await _db.InsertAsync("INSERT INTO Currency (Currency, CurrencyName, ExchangeRate) VALUES (@code, @name, @rate)",
                new() { ["code"] = c.Code, ["name"] = c.Name, ["rate"] = c.Rate });
        else
            await _db.ExecuteAsync("UPDATE Currency SET Currency = @code, CurrencyName = @name, ExchangeRate = @rate WHERE CurrencyId = @id",
                new() { ["code"] = c.Code, ["name"] = c.Name, ["rate"] = c.Rate, ["id"] = c.CurrencyId });
    }

    public async Task DeleteCurrencyRateAsync(int id) =>
        await _db.ExecuteAsync("DELETE FROM Currency WHERE CurrencyId = @id", new() { ["id"] = id });

    // ---------- System Parameters (single-row settings) ----------
    public async Task<SystemParameters?> GetSystemParametersAsync()
    {
        var dt = await _db.QueryAsync("SELECT TOP 1 ParameterId, UploadFrom, ISNULL(MinimumValue,0) AS MinimumValue FROM Parameters");
        if (dt.Rows.Count == 0) return null;
        var r = dt.Rows[0];
        return new SystemParameters
        {
            ParameterId  = Convert.ToInt32(r["ParameterId"]),
            UploadFrom   = r["UploadFrom"] == DBNull.Value ? null : Convert.ToDateTime(r["UploadFrom"]),
            MinimumValue = r["MinimumValue"] != DBNull.Value ? Convert.ToDecimal(r["MinimumValue"]) : 0m
        };
    }

    public async Task SaveSystemParametersAsync(SystemParameters p)
    {
        if (p.ParameterId == 0)
            await _db.InsertAsync("INSERT INTO Parameters (UploadFrom, MinimumValue) VALUES (@u, @m)",
                new() { ["u"] = (object?)p.UploadFrom ?? DBNull.Value, ["m"] = p.MinimumValue });
        else
            await _db.ExecuteAsync("UPDATE Parameters SET UploadFrom = @u, MinimumValue = @m WHERE ParameterId = @id",
                new() { ["u"] = (object?)p.UploadFrom ?? DBNull.Value, ["m"] = p.MinimumValue, ["id"] = p.ParameterId });
    }

    // ---------- Table stats for Setup home ----------
    public async Task<List<(string Table, int RowCount)>> GetTableStatsAsync()
    {
        var dt = await _db.QueryAsync(@"
            SELECT t.name AS TableName, SUM(p.rows) AS [RowCount]
            FROM sys.tables t
            JOIN sys.partitions p ON t.object_id = p.object_id AND p.index_id IN (0,1)
            GROUP BY t.name
            ORDER BY t.name");
        return dt.Map(r => (
            r["TableName"]!.ToString()!,
            Convert.ToInt32(r["RowCount"])
        )).ToList();
    }
}
