using System.Data;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

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
        var dt = await _db.QueryAsync("SELECT DivisionId, ISNULL(Division, '') AS DivisionName FROM Divisions ORDER BY Division");
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
            SELECT TOP 1000 c.ContactId,
                   ISNULL(c.FirstName, '') AS FirstName,
                   ISNULL(c.Surname, '') AS Surname,
                   ISNULL(co.Company, '') AS CompanyName
            FROM Contacts c
            LEFT JOIN Companies co ON co.CompanyId = c.CompanyId
            ORDER BY co.Company, c.Surname");
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
        var dt = await _db.QueryAsync(@"
            SELECT LocationId, 
                   ISNULL(Company,'') AS LocationName,
                   Address1, Address2, Suburb, State, PostCode
            FROM Locations ORDER BY Company");
        return dt.Map(r => new Location
        {
            LocationId   = Convert.ToInt32(r["LocationId"]),
            LocationName = r["LocationName"]?.ToString() ?? "",
            Address1     = r["Address1"]?.ToString(),
            Address2     = r["Address2"]?.ToString(),
            Suburb       = r["Suburb"]?.ToString(),
            State        = r["State"]?.ToString(),
            PostCode     = r["PostCode"]?.ToString(),
        });
    }

    public async Task<List<POProductType>> GetPOProductTypesAsync()
    {
        var dt = await _db.QueryAsync(@"
            SELECT POProductTypeId,
                   ISNULL(POProductType,'') AS POProductTypeName,
                   ISNULL(CapEx,0)          AS IsCapEx
            FROM PurchaseOrderProductTypes ORDER BY POProductTypeId");
        return dt.Map(r => new POProductType
        {
            POProductTypeId   = Convert.ToInt32(r["POProductTypeId"]),
            POProductTypeName = r["POProductTypeName"]?.ToString() ?? "",
            IsCapEx           = r["IsCapEx"] != DBNull.Value && Convert.ToBoolean(r["IsCapEx"]),
        });
    }

    public async Task<List<JobOrderStatus>> GetJobOrderStatusesAsync()
    {
        var dt = await _db.QueryAsync(
            "SELECT JobOrderStatusId, ISNULL(JobOrderStatus,'') AS StatusName FROM JobOrderStatus ORDER BY JobOrderStatusId");
        return dt.Map(r => new JobOrderStatus
        {
            JobOrderStatusId = Convert.ToInt32(r["JobOrderStatusId"]),
            StatusName = r["StatusName"]?.ToString() ?? ""
        });
    }

    public async Task<List<Product>> GetProductsAsync()
    {
        var dt = await _db.QueryAsync("SELECT ProductId, ProductCode, ProductName, ProductDesc, UnitCost, NettPrice FROM Products ORDER BY ProductName");
        return dt.Map(r => new Product
        {
            ProductId = Convert.ToInt32(r["ProductId"]),
            ProductName = r["ProductName"]?.ToString() ?? "",
            Description = r["ProductDesc"]?.ToString(),
            UnitCost = r["UnitCost"] == DBNull.Value ? 0m : Convert.ToDecimal(r["UnitCost"]),
            UnitPrice = r["NettPrice"] == DBNull.Value ? 0m : Convert.ToDecimal(r["NettPrice"])
        });
    }

    public List<string> GetCarriers() => new() 
    { 
        "General Road Freight", "Express Overnight", "Customer Pickup", "Local Delivery", 
        "Toll", "TNT / FedEx", "StarTrack", "Australia Post", "Courier" 
    };
}
