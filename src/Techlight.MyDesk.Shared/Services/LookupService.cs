using System.Data;
using Microsoft.Extensions.Logging;
using Techlight.MyDesk.Shared.Models;

namespace Techlight.MyDesk.Shared.Services;

/// <summary>
/// Provides read-only lookup data for dropdowns and reference lists.
/// </summary>
public class LookupService
{
    private readonly DatabaseService _db;
    private readonly ILogger<LookupService> _logger;

    public LookupService(DatabaseService db, ILogger<LookupService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Division>> GetDivisionsAsync()
    {
        var dt = await _db.QueryAsync("SELECT DivisionId, ISNULL(DivisionName, '') AS DivisionName FROM Divisions ORDER BY DivisionName");
        return dt.Map(r => new Division
        {
            DivisionId = Convert.ToInt32(r["DivisionId"]),
            DivisionName = r["DivisionName"]?.ToString() ?? ""
        });
    }

    public async Task<List<QuoteStatus>> GetQuoteStatusesAsync()
    {
        var dt = await _db.QueryAsync("SELECT QuoteStatusId, ISNULL(QuoteStatus, '') AS StatusName FROM QuoteStatus ORDER BY QuoteStatusId");
        return dt.Map(r => new QuoteStatus
        {
            QuoteStatusId = Convert.ToInt32(r["QuoteStatusId"]),
            StatusName = r["StatusName"]?.ToString() ?? ""
        });
    }

    public async Task<List<InvoiceStatus>> GetInvoiceStatusesAsync()
    {
        var dt = await _db.QueryAsync("SELECT InvoiceStatusId, ISNULL(InvoiceStatus, '') AS StatusName FROM InvoiceStatus ORDER BY InvoiceStatusId");
        return dt.Map(r => new InvoiceStatus
        {
            InvoiceStatusId = Convert.ToInt32(r["InvoiceStatusId"]),
            StatusName = r["StatusName"]?.ToString() ?? ""
        });
    }

    public async Task<List<POStatus>> GetPoStatusesAsync()
    {
        var dt = await _db.QueryAsync("SELECT POStatusId, ISNULL(POStatus, '') AS StatusName FROM PurchaseOrderStatus ORDER BY POStatusId");
        return dt.Map(r => new POStatus
        {
            POStatusId = Convert.ToInt32(r["POStatusId"]),
            StatusName = r["StatusName"]?.ToString() ?? ""
        });
    }

    public async Task<List<Contact>> GetContactLookupsAsync()
    {
        var dt = await _db.QueryAsync(@"
            SELECT TOP 1000 ContactId, ISNULL(FirstName, '') AS FirstName,
                   ISNULL(Surname, '') AS Surname, ISNULL(CompanyName, '') AS CompanyName
            FROM Contacts ORDER BY CompanyName, Surname");
        return dt.Map(r => new Contact
        {
            ContactId   = Convert.ToInt32(r["ContactId"]),
            FirstName   = r["FirstName"]?.ToString() ?? "",
            Surname     = r["Surname"]?.ToString() ?? "",
            CompanyName = r["CompanyName"]?.ToString() ?? "",
        });
    }

    public async Task<List<UserLookup>> GetUsersAsync()
    {
        var dt = await _db.QueryAsync(
            "SELECT ISNULL(Code,'') AS Code, ISNULL(Name,'') AS Name FROM Users ORDER BY Name");
        return dt.Map(r => new UserLookup
        {
            Code = r["Code"]?.ToString() ?? "",
            Name = r["Name"]?.ToString() ?? "",
        });
    }

    public async Task<List<Company>> GetCompaniesAsync()
    {
        var dt = await _db.QueryAsync(
            "SELECT TOP 2000 CompanyId, ISNULL(Company,'') AS Company FROM Companies ORDER BY Company");
        return dt.Map(r => new Company
        {
            CompanyId   = Convert.ToInt32(r["CompanyId"]),
            CompanyName = r["Company"]?.ToString() ?? "",
        });
    }

    public async Task<List<Location>> GetLocationsAsync()
    {
        var dt = await _db.QueryAsync(
            "SELECT LocationId, ISNULL(LocationName,'') AS LocationName FROM Locations ORDER BY LocationName");
        return dt.Map(r => new Location
        {
            LocationId   = Convert.ToInt32(r["LocationId"]),
            LocationName = r["LocationName"]?.ToString() ?? "",
        });
    }

    public async Task<List<POProductType>> GetPOProductTypesAsync()
    {
        var dt = await _db.QueryAsync(@"
            SELECT POProductTypeId,
                   ISNULL(POProductType,'') AS POProductTypeName,
                   ISNULL(IsCapEx,0)        AS IsCapEx
            FROM PurchaseOrderProductTypes ORDER BY POProductTypeId");
        return dt.Map(r => new POProductType
        {
            POProductTypeId   = Convert.ToInt32(r["POProductTypeId"]),
            POProductTypeName = r["POProductTypeName"]?.ToString() ?? "",
            IsCapEx           = r["IsCapEx"] != DBNull.Value && Convert.ToBoolean(r["IsCapEx"]),
        });
    }
}
