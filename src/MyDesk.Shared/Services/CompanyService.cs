using System.Data;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

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
            SELECT TOP (@Limit) CompanyId, ISNULL(Company, '') AS CompanyName,
                   Address1, Address2, Suburb, State, PostCode,
                   Phone, Fax, Email, Website
            FROM Companies
            WHERE 1=1";

        var parameters = new Dictionary<string, object?> { ["Limit"] = limit };

        if (!string.IsNullOrEmpty(search))
        {
            sql += " AND Company LIKE @Search";
            parameters["Search"] = $"%{search}%";
        }

        sql += " ORDER BY Company";

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

    public async Task<int> SaveCompanyAsync(Company c)
    {
        if (c.CompanyId == 0)
            return await _db.InsertAsync(
                @"INSERT INTO Companies (Company, Address1, Address2, Suburb, State, PostCode, Phone, Fax, Email, Website)
                  VALUES (@n, @a1, @a2, @sub, @state, @pc, @ph, @fax, @email, @web)",
                new()
                {
                    ["n"]     = c.CompanyName,
                    ["a1"]    = (object?)c.Address1 ?? DBNull.Value,
                    ["a2"]    = (object?)c.Address2 ?? DBNull.Value,
                    ["sub"]   = (object?)c.Suburb   ?? DBNull.Value,
                    ["state"] = (object?)c.State    ?? DBNull.Value,
                    ["pc"]    = (object?)c.PostCode  ?? DBNull.Value,
                    ["ph"]    = (object?)c.Phone    ?? DBNull.Value,
                    ["fax"]   = (object?)c.Fax      ?? DBNull.Value,
                    ["email"] = (object?)c.Email    ?? DBNull.Value,
                    ["web"]   = (object?)c.Website  ?? DBNull.Value,
                });
        await _db.ExecuteAsync(
            @"UPDATE Companies SET Company=@n, Address1=@a1, Address2=@a2, Suburb=@sub,
              State=@state, PostCode=@pc, Phone=@ph, Fax=@fax, Email=@email, Website=@web
              WHERE CompanyId=@id",
            new()
            {
                ["n"]     = c.CompanyName,
                ["a1"]    = (object?)c.Address1 ?? DBNull.Value,
                ["a2"]    = (object?)c.Address2 ?? DBNull.Value,
                ["sub"]   = (object?)c.Suburb   ?? DBNull.Value,
                ["state"] = (object?)c.State    ?? DBNull.Value,
                ["pc"]    = (object?)c.PostCode  ?? DBNull.Value,
                ["ph"]    = (object?)c.Phone    ?? DBNull.Value,
                ["fax"]   = (object?)c.Fax      ?? DBNull.Value,
                ["email"] = (object?)c.Email    ?? DBNull.Value,
                ["web"]   = (object?)c.Website  ?? DBNull.Value,
                ["id"]    = c.CompanyId,
            });
        return c.CompanyId;
    }

    public async Task DeleteCompanyAsync(int id) =>
        await _db.ExecuteAsync("DELETE FROM Companies WHERE CompanyId = @id", new() { ["id"] = id });

    private static Company MapCompany(DataRow r)
    {
        // Handle both aliased "CompanyName" (list) and raw "Company" (GetById SELECT *)
        string name = "";
        if (r.Table.Columns.Contains("CompanyName") && r["CompanyName"] != DBNull.Value) name = r["CompanyName"].ToString() ?? "";
        else if (r.Table.Columns.Contains("Company") && r["Company"] != DBNull.Value)   name = r["Company"].ToString() ?? "";

        return new Company
        {
            CompanyId = Convert.ToInt32(r["CompanyId"]),
            CompanyName = name,
            Address1 = r.Table.Columns.Contains("Address1") && r["Address1"] != DBNull.Value ? r["Address1"].ToString() : null,
            Address2 = r.Table.Columns.Contains("Address2") && r["Address2"] != DBNull.Value ? r["Address2"].ToString() : null,
            Suburb   = r.Table.Columns.Contains("Suburb")   && r["Suburb"]   != DBNull.Value ? r["Suburb"].ToString()   : null,
            State    = r.Table.Columns.Contains("State")    && r["State"]    != DBNull.Value ? r["State"].ToString()    : null,
            PostCode = r.Table.Columns.Contains("PostCode") && r["PostCode"] != DBNull.Value ? r["PostCode"].ToString() : null,
            Phone    = r.Table.Columns.Contains("Phone")    && r["Phone"]    != DBNull.Value ? r["Phone"].ToString()    : null,
            Fax      = r.Table.Columns.Contains("Fax")      && r["Fax"]      != DBNull.Value ? r["Fax"].ToString()      : null,
            Email    = r.Table.Columns.Contains("Email")    && r["Email"]    != DBNull.Value ? r["Email"].ToString()    : null,
            Website  = r.Table.Columns.Contains("Website")  && r["Website"]  != DBNull.Value ? r["Website"].ToString()  : null,
            ABN      = null, // Column not present in Companies table
        };
    }
}
