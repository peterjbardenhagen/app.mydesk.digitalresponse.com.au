using System.Data;
using Techlight.MyDesk.MCP.Models;

namespace Techlight.MyDesk.MCP.Services;

public class ContactService
{
    private readonly DatabaseService _db;
    private readonly ILogger<ContactService> _logger;

    public ContactService(DatabaseService db, ILogger<ContactService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<Contact?> GetContactByIdAsync(int contactId, McpContext context)
    {
        var sql = @"
            SELECT c.ContactId, c.FirstName, c.Surname, co.CompanyName, 
                   c.Position, c.Email, c.Phone, c.Mobile, c.Fax,
                   c.Address1, c.Address2, c.Suburb, c.PostCode,
                   c.CustomerCode, c.SupplierCode, u.Name as Originator, 
                   c.CompanyId
            FROM Contacts c
            INNER JOIN Companies co ON c.CompanyId = co.CompanyId
            INNER JOIN Users u ON c.Code = u.Code
            WHERE c.ContactId = @ContactId
            AND c.Deleted = 0
            AND c.DivisionId IN (SELECT value FROM STRING_SPLIT(@DivisionIds, ','))";

        var dt = await _db.ExecuteQueryAsync(sql, new Dictionary<string, object>
        {
            ["ContactId"] = contactId,
            ["DivisionIds"] = string.Join(",", context.AccessibleDivisions)
        });

        if (dt.Rows.Count == 0) return null;

        return MapContactFromDataRow(dt.Rows[0]);
    }

    public async Task<List<Contact>> GetContactsAsync(string? searchTerm = null, string? companyName = null,
        string? originatorCode = null, string? letter = null, int? limit = 50, McpContext? context = null)
    {
        var sql = @"
            SELECT TOP (@Limit) c.ContactId, c.FirstName, c.Surname, co.CompanyName, 
                   c.Position, c.Email, c.Phone, c.Mobile, c.Fax,
                   c.Address1, c.Address2, c.Suburb, c.PostCode,
                   c.CustomerCode, c.SupplierCode, u.Name as Originator, 
                   c.CompanyId
            FROM Contacts c
            INNER JOIN Companies co ON c.CompanyId = co.CompanyId
            INNER JOIN Users u ON c.Code = u.Code
            WHERE c.Deleted = 0";

        var parameters = new Dictionary<string, object>
        {
            ["Limit"] = limit ?? 50
        };

        if (!string.IsNullOrEmpty(searchTerm))
        {
            sql += " AND (c.FirstName LIKE @Search OR c.Surname LIKE @Search OR c.Email LIKE @Search OR c.Phone LIKE @Search)";
            parameters["Search"] = $"%{searchTerm}%";
        }

        if (!string.IsNullOrEmpty(companyName))
        {
            sql += " AND co.CompanyName LIKE @CompanyName";
            parameters["CompanyName"] = $"%{companyName}%";
        }

        if (!string.IsNullOrEmpty(originatorCode))
        {
            sql += " AND c.Code = @OriginatorCode";
            parameters["OriginatorCode"] = originatorCode;
        }

        if (!string.IsNullOrEmpty(letter) && letter.Length == 1)
        {
            sql += " AND (LEFT(c.Surname, 1) = @Letter OR LEFT(co.CompanyName, 1) = @Letter)";
            parameters["Letter"] = letter.ToUpper();
        }

        if (context?.AccessibleDivisions?.Any() == true)
        {
            sql += " AND c.DivisionId IN (SELECT value FROM STRING_SPLIT(@DivisionIds, ','))";
            parameters["DivisionIds"] = string.Join(",", context.AccessibleDivisions);
        }

        sql += " ORDER BY c.Surname, c.FirstName";

        var dt = await _db.ExecuteQueryAsync(sql, parameters);
        return dt.AsEnumerable().Select(MapContactFromDataRow).ToList();
    }

    public async Task<List<Contact>> SearchContactsByNameAsync(string name, McpContext context)
    {
        var sql = @"
            SELECT TOP 10 c.ContactId, c.FirstName, c.Surname, co.CompanyName, 
                   c.Position, c.Email, c.Phone, u.Name as Originator
            FROM Contacts c
            INNER JOIN Companies co ON c.CompanyId = co.CompanyId
            INNER JOIN Users u ON c.Code = u.Code
            WHERE c.Deleted = 0
            AND (c.FirstName + ' ' + c.Surname LIKE @Name OR c.Surname LIKE @Name)
            AND c.DivisionId IN (SELECT value FROM STRING_SPLIT(@DivisionIds, ','))
            ORDER BY c.Surname, c.FirstName";

        var dt = await _db.ExecuteQueryAsync(sql, new Dictionary<string, object>
        {
            ["Name"] = $"%{name}%",
            ["DivisionIds"] = string.Join(",", context.AccessibleDivisions)
        });

        return dt.AsEnumerable().Select(MapContactFromDataRow).ToList();
    }

    public async Task<Contact> CreateContactAsync(Contact contact, McpContext context)
    {
        var sql = @"
            INSERT INTO Contacts (CompanyId, Code, FirstName, Surname, Position, Email, 
                                Phone, Mobile, Fax, Address1, Address2, Suburb, PostCode,
                                CustomerCode, SupplierCode, DivisionId, DateEntered, Deleted)
            VALUES (@CompanyId, @Code, @FirstName, @Surname, @Position, @Email, 
                   @Phone, @Mobile, @Fax, @Address1, @Address2, @Suburb, @PostCode,
                   @CustomerCode, @SupplierCode, @DivisionId, GETDATE(), 0);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        var contactId = await _db.ExecuteScalarAsync<int>(sql, new Dictionary<string, object>
        {
            ["CompanyId"] = contact.CompanyId,
            ["Code"] = context.UserCode,
            ["FirstName"] = (object?)contact.FirstName ?? DBNull.Value,
            ["Surname"] = contact.Surname,
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
            ["DivisionId"] = context.AccessibleDivisions.FirstOrDefault(1)
        });

        return (await GetContactByIdAsync(contactId, context))!;
    }

    private Contact MapContactFromDataRow(DataRow row)
    {
        return new Contact
        {
            ContactId = Convert.ToInt32(row["ContactId"]),
            FirstName = row["FirstName"].ToString() ?? string.Empty,
            Surname = row["Surname"].ToString()!,
            CompanyName = row["CompanyName"].ToString()!,
            Position = row["Position"]?.ToString(),
            Email = row["Email"]?.ToString(),
            Phone = row["Phone"]?.ToString(),
            Mobile = row["Mobile"]?.ToString(),
            Fax = row["Fax"]?.ToString(),
            Address1 = row["Address1"]?.ToString(),
            Address2 = row["Address2"]?.ToString(),
            Suburb = row["Suburb"]?.ToString(),
            PostCode = row["PostCode"]?.ToString(),
            CustomerCode = row["CustomerCode"]?.ToString(),
            SupplierCode = row["SupplierCode"]?.ToString(),
            Originator = row["Originator"].ToString()!,
            CompanyId = Convert.ToInt32(row["CompanyId"])
        };
    }
}
