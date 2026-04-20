using Microsoft.Extensions.Logging;
using Techlight.MyDesk.Shared.Models;

namespace Techlight.MyDesk.Shared.Services;

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
            return await _db.InsertAsync("INSERT INTO Divisions (Division) VALUES (@n)", new() { ["n"] = d.DivisionName });
        await _db.ExecuteAsync("UPDATE Divisions SET Division = @n WHERE DivisionId = @id",
            new() { ["n"] = d.DivisionName, ["id"] = d.DivisionId });
        return d.DivisionId;
    }

    public async Task DeleteDivisionAsync(int id) =>
        await _db.ExecuteAsync("DELETE FROM Divisions WHERE DivisionId = @id", new() { ["id"] = id });

    // ---------- Locations ----------
    public async Task<List<Location>> GetLocationsAsync()
    {
        var dt = await _db.QueryAsync("SELECT LocationId, Location AS LocationName FROM Locations ORDER BY Location");
        return dt.Map(r => new Location
        {
            LocationId = (int)r["LocationId"],
            LocationName = r["LocationName"]?.ToString() ?? ""
        }).ToList();
    }

    public async Task<int> SaveLocationAsync(Location l)
    {
        if (l.LocationId == 0)
            return await _db.InsertAsync("INSERT INTO Locations (Location) VALUES (@n)", new() { ["n"] = l.LocationName });
        await _db.ExecuteAsync("UPDATE Locations SET Location = @n WHERE LocationId = @id",
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
