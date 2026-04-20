using System.Data;
using Microsoft.Extensions.Logging;
using Techlight.MyDesk.Shared.Models;

namespace Techlight.MyDesk.Shared.Services;

public class ContactService
{
    private readonly DatabaseService _db;
    private readonly ILogger<ContactService> _logger;

    public ContactService(DatabaseService db, ILogger<ContactService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Contact>> GetContactsAsync(string? search = null, int limit = 500)
    {
        var sql = @"
            SELECT TOP (@Limit) c.ContactId,
                   ISNULL(c.FirstName, '') AS FirstName,
                   ISNULL(c.Surname, '') AS Surname,
                   ISNULL(c.CompanyName, '') AS CompanyName,
                   c.Position, c.Email, c.Phone, c.Mobile, c.Fax,
                   c.Address1, c.Address2, c.Suburb, c.PostCode,
                   c.CustomerCode, c.SupplierCode,
                   ISNULL(c.Code, '') AS Originator,
                   ISNULL(c.CompanyId, 0) AS CompanyId
            FROM Contacts c
            WHERE 1=1";

        var parameters = new Dictionary<string, object?> { ["Limit"] = limit };

        if (!string.IsNullOrEmpty(search))
        {
            sql += " AND (c.FirstName LIKE @Search OR c.Surname LIKE @Search OR c.CompanyName LIKE @Search OR c.Email LIKE @Search)";
            parameters["Search"] = $"%{search}%";
        }

        sql += " ORDER BY c.CompanyName, c.Surname, c.FirstName";

        var dt = await _db.QueryAsync(sql, parameters);
        return dt.Map(MapContact);
    }

    public async Task<Contact?> GetContactAsync(int contactId)
    {
        var sql = @"
            SELECT c.ContactId,
                   ISNULL(c.FirstName, '') AS FirstName,
                   ISNULL(c.Surname, '') AS Surname,
                   ISNULL(c.CompanyName, '') AS CompanyName,
                   c.Position, c.Email, c.Phone, c.Mobile, c.Fax,
                   c.Address1, c.Address2, c.Suburb, c.PostCode,
                   c.CustomerCode, c.SupplierCode,
                   ISNULL(c.Code, '') AS Originator,
                   ISNULL(c.CompanyId, 0) AS CompanyId
            FROM Contacts c
            WHERE c.ContactId = @Id";

        var dt = await _db.QueryAsync(sql, new() { ["Id"] = contactId });
        return dt.Rows.Count == 0 ? null : MapContact(dt.Rows[0]);
    }

    public async Task<int> CreateContactAsync(Contact contact, string originatorCode)
    {
        var sql = @"
            INSERT INTO Contacts (FirstName, Surname, CompanyName, Position, Email, Phone, Mobile, Fax,
                                  Address1, Address2, Suburb, PostCode, CustomerCode, SupplierCode, Code, CompanyId)
            VALUES (@FirstName, @Surname, @CompanyName, @Position, @Email, @Phone, @Mobile, @Fax,
                    @Address1, @Address2, @Suburb, @PostCode, @CustomerCode, @SupplierCode, @Code, @CompanyId)";

        return await _db.InsertAsync(sql, new()
        {
            ["FirstName"] = contact.FirstName,
            ["Surname"] = contact.Surname,
            ["CompanyName"] = contact.CompanyName,
            ["Position"] = (object?)contact.Position ?? DBNull.Value,
            ["Email"] = (object?)contact.Email ?? DBNull.Value,
            ["Phone"] = (object?)contact.Phone ?? DBNull.Value,
            ["Mobile"] = (object?)contact.Mobile ?? DBNull.Value,
            ["Fax"] = (object?)contact.Fax ?? DBNull.Value,
            ["Address1"] = (object?)contact.Address1 ?? DBNull.Value,
            ["Address2"] = (object?)contact.Address2 ?? DBNull.Value,
            ["Suburb"] = (object?)contact.Suburb ?? DBNull.Value,
            ["PostCode"] = (object?)contact.PostCode ?? DBNull.Value,
            ["CustomerCode"] = (object?)contact.CustomerCode ?? DBNull.Value,
            ["SupplierCode"] = (object?)contact.SupplierCode ?? DBNull.Value,
            ["Code"] = originatorCode,
            ["CompanyId"] = contact.CompanyId,
        });
    }

    public async Task<int> UpdateContactAsync(Contact contact)
    {
        var sql = @"
            UPDATE Contacts SET
                FirstName = @FirstName, Surname = @Surname, CompanyName = @CompanyName,
                Position = @Position, Email = @Email, Phone = @Phone, Mobile = @Mobile, Fax = @Fax,
                Address1 = @Address1, Address2 = @Address2, Suburb = @Suburb, PostCode = @PostCode,
                CustomerCode = @CustomerCode, SupplierCode = @SupplierCode, CompanyId = @CompanyId
            WHERE ContactId = @ContactId";

        return await _db.ExecuteAsync(sql, new()
        {
            ["ContactId"] = contact.ContactId,
            ["FirstName"] = contact.FirstName,
            ["Surname"] = contact.Surname,
            ["CompanyName"] = contact.CompanyName,
            ["Position"] = (object?)contact.Position ?? DBNull.Value,
            ["Email"] = (object?)contact.Email ?? DBNull.Value,
            ["Phone"] = (object?)contact.Phone ?? DBNull.Value,
            ["Mobile"] = (object?)contact.Mobile ?? DBNull.Value,
            ["Fax"] = (object?)contact.Fax ?? DBNull.Value,
            ["Address1"] = (object?)contact.Address1 ?? DBNull.Value,
            ["Address2"] = (object?)contact.Address2 ?? DBNull.Value,
            ["Suburb"] = (object?)contact.Suburb ?? DBNull.Value,
            ["PostCode"] = (object?)contact.PostCode ?? DBNull.Value,
            ["CustomerCode"] = (object?)contact.CustomerCode ?? DBNull.Value,
            ["SupplierCode"] = (object?)contact.SupplierCode ?? DBNull.Value,
            ["CompanyId"] = contact.CompanyId,
        });
    }

    private static Contact MapContact(DataRow r) => new()
    {
        ContactId = Convert.ToInt32(r["ContactId"]),
        FirstName = r["FirstName"]?.ToString() ?? "",
        Surname = r["Surname"]?.ToString() ?? "",
        CompanyName = r["CompanyName"]?.ToString() ?? "",
        Position = r["Position"] == DBNull.Value ? null : r["Position"]?.ToString(),
        Email = r["Email"] == DBNull.Value ? null : r["Email"]?.ToString(),
        Phone = r["Phone"] == DBNull.Value ? null : r["Phone"]?.ToString(),
        Mobile = r["Mobile"] == DBNull.Value ? null : r["Mobile"]?.ToString(),
        Fax = r["Fax"] == DBNull.Value ? null : r["Fax"]?.ToString(),
        Address1 = r["Address1"] == DBNull.Value ? null : r["Address1"]?.ToString(),
        Address2 = r["Address2"] == DBNull.Value ? null : r["Address2"]?.ToString(),
        Suburb = r["Suburb"] == DBNull.Value ? null : r["Suburb"]?.ToString(),
        PostCode = r["PostCode"] == DBNull.Value ? null : r["PostCode"]?.ToString(),
        CustomerCode = r["CustomerCode"] == DBNull.Value ? null : r["CustomerCode"]?.ToString(),
        SupplierCode = r["SupplierCode"] == DBNull.Value ? null : r["SupplierCode"]?.ToString(),
        Originator = r["Originator"]?.ToString() ?? "",
        CompanyId = Convert.ToInt32(r["CompanyId"]),
    };
}
