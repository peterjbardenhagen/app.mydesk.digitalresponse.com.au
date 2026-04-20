using System.Data;
using Microsoft.Extensions.Logging;
using Techlight.MyDesk.Shared.Models;

namespace Techlight.MyDesk.Shared.Services;

public class CompanyService
{
    private readonly DatabaseService _db;
    private readonly ILogger<CompanyService> _logger;

    public CompanyService(DatabaseService db, ILogger<CompanyService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Company>> GetCompaniesAsync(string? search = null, int limit = 500)
    {
        var sql = @"
            SELECT TOP (@Limit) CompanyId, ISNULL(CompanyName, '') AS CompanyName,
                   Address1, Address2, Suburb, State, PostCode,
                   Phone, Fax, Email, Website, ABN
            FROM Companies
            WHERE 1=1";

        var parameters = new Dictionary<string, object?> { ["Limit"] = limit };

        if (!string.IsNullOrEmpty(search))
        {
            sql += " AND CompanyName LIKE @Search";
            parameters["Search"] = $"%{search}%";
        }

        sql += " ORDER BY CompanyName";

        var dt = await _db.QueryAsync(sql, parameters);
        return dt.Map(MapCompany);
    }

    public async Task<Company?> GetCompanyAsync(int companyId)
    {
        var dt = await _db.QueryAsync(
            "SELECT * FROM Companies WHERE CompanyId = @Id",
            new() { ["Id"] = companyId });
        return dt.Rows.Count == 0 ? null : MapCompany(dt.Rows[0]);
    }

    private static Company MapCompany(DataRow r) => new()
    {
        CompanyId = Convert.ToInt32(r["CompanyId"]),
        CompanyName = r["CompanyName"]?.ToString() ?? "",
        Address1 = r.Table.Columns.Contains("Address1") && r["Address1"] != DBNull.Value ? r["Address1"].ToString() : null,
        Address2 = r.Table.Columns.Contains("Address2") && r["Address2"] != DBNull.Value ? r["Address2"].ToString() : null,
        Suburb = r.Table.Columns.Contains("Suburb") && r["Suburb"] != DBNull.Value ? r["Suburb"].ToString() : null,
        State = r.Table.Columns.Contains("State") && r["State"] != DBNull.Value ? r["State"].ToString() : null,
        PostCode = r.Table.Columns.Contains("PostCode") && r["PostCode"] != DBNull.Value ? r["PostCode"].ToString() : null,
        Phone = r.Table.Columns.Contains("Phone") && r["Phone"] != DBNull.Value ? r["Phone"].ToString() : null,
        Fax = r.Table.Columns.Contains("Fax") && r["Fax"] != DBNull.Value ? r["Fax"].ToString() : null,
        Email = r.Table.Columns.Contains("Email") && r["Email"] != DBNull.Value ? r["Email"].ToString() : null,
        Website = r.Table.Columns.Contains("Website") && r["Website"] != DBNull.Value ? r["Website"].ToString() : null,
        ABN = r.Table.Columns.Contains("ABN") && r["ABN"] != DBNull.Value ? r["ABN"].ToString() : null,
    };
}
