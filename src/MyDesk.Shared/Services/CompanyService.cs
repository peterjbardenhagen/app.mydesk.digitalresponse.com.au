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
                   Phone, Fax, Email, Website,
                   CustomerCode, SupplierCode,
                   ABN, IsCustomer, IsSupplier, Notes,
                   InvAddress1, InvAddress2, InvSuburb, InvState, InvPostCode,
                   DelAddress1, DelAddress2, DelSuburb, DelState, DelPostCode
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

    public async Task<List<Company>> GetCompaniesForDropdownAsync()
    {
        var dt = await _db.QueryAsync(@"
            SELECT CompanyId, Company 
            FROM Companies 
            WHERE ISNULL(Company, '') <> ''
            ORDER BY Company");
        return dt.Map(r => new Company
        {
            CompanyId = Convert.ToInt32(r["CompanyId"]),
            CompanyName = r["Company"]?.ToString() ?? ""
        });
    }

    public async Task<int> SaveCompanyAsync(Company c)
    {
        if (c.CompanyId == 0)
            return await _db.InsertAsync(
                @"INSERT INTO Companies (Company, Address1, Address2, Suburb, State, PostCode, Phone, Fax, Email, Website, ABN,
                                           CustomerCode, SupplierCode, IsCustomer, IsSupplier, Notes,
                                           InvAddress1, InvAddress2, InvSuburb, InvState, InvPostCode,
                                           DelAddress1, DelAddress2, DelSuburb, DelState, DelPostCode)
                  VALUES (@n, @a1, @a2, @sub, @state, @pc, @ph, @fax, @email, @web, @abn,
                          @custCode, @suppCode, @isCust, @isSupp, @notes,
                          @inv1, @inv2, @invSub, @invState, @invPC,
                          @del1, @del2, @delSub, @delState, @delPC)",
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
                    ["abn"]   = (object?)c.ABN      ?? DBNull.Value,
                    ["custCode"] = (object?)c.CustomerCode ?? DBNull.Value,
                    ["suppCode"] = (object?)c.SupplierCode ?? DBNull.Value,
                    ["isCust"]   = c.IsCustomer,
                    ["isSupp"]   = c.IsSupplier,
                    ["notes"]    = (object?)c.Notes ?? DBNull.Value,
                    ["inv1"]  = (object?)c.InvAddress1 ?? DBNull.Value,
                    ["inv2"]  = (object?)c.InvAddress2 ?? DBNull.Value,
                    ["invSub"]= (object?)c.InvSuburb   ?? DBNull.Value,
                    ["invState"]=(object?)c.InvState   ?? DBNull.Value,
                    ["invPC"] = (object?)c.InvPostCode ?? DBNull.Value,
                    ["del1"]  = (object?)c.DelAddress1 ?? DBNull.Value,
                    ["del2"]  = (object?)c.DelAddress2 ?? DBNull.Value,
                    ["delSub"]= (object?)c.DelSuburb   ?? DBNull.Value,
                    ["delState"]=(object?)c.DelState   ?? DBNull.Value,
                    ["delPC"] = (object?)c.DelPostCode ?? DBNull.Value,
                });
        await _db.ExecuteNonQueryAsync(
            @"UPDATE Companies SET Company=@n, Address1=@a1, Address2=@a2, Suburb=@sub,
              State=@state, PostCode=@pc, Phone=@ph, Fax=@fax, Email=@email, Website=@web, ABN=@abn,
              CustomerCode=@custCode, SupplierCode=@suppCode, IsCustomer=@isCust, IsSupplier=@isSupp, Notes=@notes,
              InvAddress1=@inv1, InvAddress2=@inv2, InvSuburb=@invSub, InvState=@invState, InvPostCode=@invPC,
              DelAddress1=@del1, DelAddress2=@del2, DelSuburb=@delSub, DelState=@delState, DelPostCode=@delPC
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
                ["abn"]   = (object?)c.ABN      ?? DBNull.Value,
                ["custCode"] = (object?)c.CustomerCode ?? DBNull.Value,
                ["suppCode"] = (object?)c.SupplierCode ?? DBNull.Value,
                ["isCust"]   = c.IsCustomer,
                ["isSupp"]   = c.IsSupplier,
                ["notes"]    = (object?)c.Notes ?? DBNull.Value,
                ["inv1"]  = (object?)c.InvAddress1 ?? DBNull.Value,
                ["inv2"]  = (object?)c.InvAddress2 ?? DBNull.Value,
                ["invSub"]= (object?)c.InvSuburb   ?? DBNull.Value,
                ["invState"]=(object?)c.InvState   ?? DBNull.Value,
                ["invPC"] = (object?)c.InvPostCode ?? DBNull.Value,
                ["del1"]  = (object?)c.DelAddress1 ?? DBNull.Value,
                ["del2"]  = (object?)c.DelAddress2 ?? DBNull.Value,
                ["delSub"]= (object?)c.DelSuburb   ?? DBNull.Value,
                ["delState"]=(object?)c.DelState   ?? DBNull.Value,
                ["delPC"] = (object?)c.DelPostCode ?? DBNull.Value,
                ["id"]    = c.CompanyId,
            });
        return c.CompanyId;
    }

    public async Task DeleteCompanyAsync(int id) =>
        await _db.ExecuteNonQueryAsync("DELETE FROM Companies WHERE CompanyId = @id", new() { ["id"] = id });

    public async Task NormaliseAndDedupeAsync()
    {
        // Normalise company names
        var companies = await GetCompaniesAsync(null, 5000);
        foreach (var c in companies)
        {
            var normalised = NormaliseCompanyName(c.CompanyName);
            if (normalised != c.CompanyName)
            {
                await _db.ExecuteNonQueryAsync("UPDATE Companies SET Company = @n WHERE CompanyId = @id",
                    new() { ["n"] = normalised, ["id"] = c.CompanyId });
            }
        }

        // Find and merge duplicates
        var allCompanies = await GetCompaniesAsync(null, 5000);
        var groups = allCompanies
            .GroupBy(c => NormaliseCompanyName(c.CompanyName), StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();

        foreach (var group in groups)
        {
            var sorted = group.OrderByDescending(c => 
                (!string.IsNullOrEmpty(c.Phone) ? 1 : 0) +
                (!string.IsNullOrEmpty(c.Email) ? 1 : 0) +
                (!string.IsNullOrEmpty(c.Address1) ? 1 : 0)).ToList();
            
            var keep = sorted.First();
            var duplicates = sorted.Skip(1).ToList();

            foreach (var dup in duplicates)
            {
                // Update invoices referencing duplicate to point to kept company
                await _db.ExecuteNonQueryAsync("UPDATE Invoices SET InvCompany = @name WHERE InvCompany = @dupName",
                    new() { ["name"] = keep.CompanyName, ["dupName"] = dup.CompanyName });
                
                // Delete duplicate
                await DeleteCompanyAsync(dup.CompanyId);
            }
        }
    }

    private static string NormaliseCompanyName(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return name;
        
        var normalised = name.Trim()
            .Replace("  ", " ")
            .Replace("pty Ltd", "Pty Ltd")
            .Replace("PTY LTD", "Pty Ltd")
            .Replace("Pty. Ltd.", "Pty Ltd")
            .Replace("pty ltd", "Pty Ltd")
            .Replace("&", "&")
            .Trim();
        
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(normalised.ToLower());
    }

    public async Task<List<CompanyImportItem>> GetCompaniesFromInvoicesAsync()
    {
        var sql = @"
            SELECT 
                COALESCE(NULLIF(i.InvCompany, ''), NULLIF(i.DelCompany, ''), 'Unknown Customer') AS CompanyName,
                MAX(i.InvAddress) AS InvAddress,
                MAX(i.DelAddress) AS DelAddress,
                MAX(COALESCE(co.Phone, '')) AS Phone,
                MAX(COALESCE(co.Email, '')) AS Email,
                COUNT(*) AS InvoiceCount,
                SUM(ISNULL(i.NettPriceTotal, 0)) AS TotalValue,
                MAX(co.CompanyId) AS ExistingCompanyId,
                MAX(co.Company) AS ExistingCompanyName
            FROM Invoices i
            LEFT JOIN Companies co ON COALESCE(NULLIF(i.InvCompany, ''), NULLIF(i.DelCompany, '')) = co.Company
            WHERE i.InvoiceStatusId NOT IN (2,3,4,5)
            GROUP BY COALESCE(NULLIF(i.InvCompany, ''), NULLIF(i.DelCompany, ''), 'Unknown Customer')
            ORDER BY TotalValue DESC";

        var dt = await _db.QueryAsync(sql);
        return dt.Map(r => new CompanyImportItem
        {
            CompanyName = r["CompanyName"]?.ToString() ?? "",
            InvAddress = r["InvAddress"]?.ToString(),
            DelAddress = r["DelAddress"]?.ToString(),
            Phone = r["Phone"]?.ToString(),
            Email = r["Email"]?.ToString(),
            InvoiceCount = Convert.ToInt32(r["InvoiceCount"]),
            TotalValue = Convert.ToDecimal(r["TotalValue"]),
            ExistingCompanyId = r["ExistingCompanyId"] != DBNull.Value ? Convert.ToInt32(r["ExistingCompanyId"]) : null,
            ExistingCompanyName = r["ExistingCompanyName"]?.ToString(),
            Selected = r["ExistingCompanyId"] == DBNull.Value
        });
    }

    public async Task<int> ImportCompanyFromInvoiceAsync(CompanyImportItem item)
    {
        var addressParts = ParseAddress(item.InvAddress);
        var delAddressParts = ParseAddress(item.DelAddress);

        return await _db.InsertAsync(@"
            INSERT INTO Companies (Company, Address1, Address2, Suburb, State, PostCode, Phone, Email,
                                   InvAddress1, InvAddress2, InvSuburb, InvState, InvPostCode,
                                   DelAddress1, DelAddress2, DelSuburb, DelState, DelPostCode, IsCustomer)
            VALUES (@n, @a1, @a2, @sub, @state, @pc, @ph, @email,
                    @inv1, @inv2, @invSub, @invState, @invPC,
                    @del1, @del2, @delSub, @delState, @delPC, 1)",
            new()
            {
                ["n"] = item.CompanyName,
                ["a1"] = addressParts.a1,
                ["a2"] = addressParts.a2,
                ["sub"] = addressParts.sub,
                ["state"] = addressParts.state,
                ["pc"] = addressParts.pc,
                ["ph"] = (object?)item.Phone ?? DBNull.Value,
                ["email"] = (object?)item.Email ?? DBNull.Value,
                ["inv1"] = addressParts.a1,
                ["inv2"] = addressParts.a2,
                ["invSub"] = addressParts.sub,
                ["invState"] = addressParts.state,
                ["invPC"] = addressParts.pc,
                ["del1"] = delAddressParts.a1,
                ["del2"] = delAddressParts.a2,
                ["delSub"] = delAddressParts.sub,
                ["delState"] = delAddressParts.state,
                ["delPC"] = delAddressParts.pc,
            });
    }

    public async Task UpdateCompanyFromInvoiceAsync(int companyId, CompanyImportItem item)
    {
        var addressParts = ParseAddress(item.InvAddress);
        var delAddressParts = ParseAddress(item.DelAddress);

        await _db.ExecuteNonQueryAsync(@"
            UPDATE Companies SET 
                Phone = COALESCE(NULLIF(Phone, ''), @ph),
                Email = COALESCE(NULLIF(Email, ''), @email),
                InvAddress1 = COALESCE(NULLIF(InvAddress1, ''), @inv1),
                InvAddress2 = COALESCE(NULLIF(InvAddress2, ''), @inv2),
                InvSuburb = COALESCE(NULLIF(InvSuburb, ''), @invSub),
                InvState = COALESCE(NULLIF(InvState, ''), @invState),
                InvPostCode = COALESCE(NULLIF(InvPostCode, ''), @invPC),
                DelAddress1 = COALESCE(NULLIF(DelAddress1, ''), @del1),
                DelAddress2 = COALESCE(NULLIF(DelAddress2, ''), @del2),
                DelSuburb = COALESCE(NULLIF(DelSuburb, ''), @delSub),
                DelState = COALESCE(NULLIF(DelState, ''), @delState),
                DelPostCode = COALESCE(NULLIF(DelPostCode, ''), @delPC)
            WHERE CompanyId = @id",
            new()
            {
                ["id"] = companyId,
                ["ph"] = (object?)item.Phone ?? DBNull.Value,
                ["email"] = (object?)item.Email ?? DBNull.Value,
                ["inv1"] = addressParts.a1,
                ["inv2"] = addressParts.a2,
                ["invSub"] = addressParts.sub,
                ["invState"] = addressParts.state,
                ["invPC"] = addressParts.pc,
                ["del1"] = delAddressParts.a1,
                ["del2"] = delAddressParts.a2,
                ["delSub"] = delAddressParts.sub,
                ["delState"] = delAddressParts.state,
                ["delPC"] = delAddressParts.pc,
            });
    }

    private static (string? a1, string? a2, string? sub, string? state, string? pc) ParseAddress(string? fullAddress)
    {
        if (string.IsNullOrWhiteSpace(fullAddress)) return (null, null, null, null, null);
        
        var parts = fullAddress.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList();
        string? a1 = parts.Count > 0 ? parts[0] : null;
        string? a2 = parts.Count > 1 ? parts[1] : null;
        string? sub = parts.Count > 2 ? parts[2] : null;
        string? state = parts.Count > 3 ? parts[3] : null;
        string? pc = parts.Count > 4 ? parts[4] : null;
        
        if (pc != null && pc.Length <= 4 && int.TryParse(pc, out _))
        {
            state = parts.Count > 3 ? parts[3] : null;
        }
        
        return (a1, a2, sub, state, pc);
    }

    private static Company MapCompany(DataRow r)
    {
        string name = "";
        if (r.Table.Columns.Contains("CompanyName") && r["CompanyName"] != DBNull.Value) name = r["CompanyName"].ToString() ?? "";
        else if (r.Table.Columns.Contains("Company") && r["Company"] != DBNull.Value)   name = r["Company"].ToString() ?? "";

        return new Company
        {
            CompanyId = Convert.ToInt32(r["CompanyId"]),
            CompanyName = name,
            Address1 = GetString(r, "Address1"),
            Address2 = GetString(r, "Address2"),
            Suburb   = GetString(r, "Suburb"),
            State    = GetString(r, "State"),
            PostCode = GetString(r, "PostCode"),
            Phone    = GetString(r, "Phone"),
            Fax      = GetString(r, "Fax"),
            Email    = GetString(r, "Email"),
            Website  = GetString(r, "Website"),
            ABN      = GetString(r, "ABN"),
            CustomerCode = GetString(r, "CustomerCode"),
            SupplierCode = GetString(r, "SupplierCode"),
            IsCustomer = r.Table.Columns.Contains("IsCustomer") && r["IsCustomer"] != DBNull.Value && Convert.ToBoolean(r["IsCustomer"]),
            IsSupplier = r.Table.Columns.Contains("IsSupplier") && r["IsSupplier"] != DBNull.Value && Convert.ToBoolean(r["IsSupplier"]),
            Notes    = GetString(r, "Notes"),
            InvAddress1 = GetString(r, "InvAddress1"),
            InvAddress2 = GetString(r, "InvAddress2"),
            InvSuburb   = GetString(r, "InvSuburb"),
            InvState    = GetString(r, "InvState"),
            InvPostCode = GetString(r, "InvPostCode"),
            DelAddress1 = GetString(r, "DelAddress1"),
            DelAddress2 = GetString(r, "DelAddress2"),
            DelSuburb   = GetString(r, "DelSuburb"),
            DelState    = GetString(r, "DelState"),
            DelPostCode = GetString(r, "DelPostCode"),
        };
    }

    private static string? GetString(DataRow r, string col) =>
        r.Table.Columns.Contains(col) && r[col] != DBNull.Value ? r[col].ToString() : null;
}
