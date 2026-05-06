using System.Data;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class ContactService
{
    private readonly DatabaseService _db;
    private readonly ILogger<ContactService> _logger;

    public ContactService(DatabaseService db, ILogger<ContactService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<Contact>> GetContactsAsync(string? search = null, int limit = 500, bool onlyWithRecords = false)
    {
        var sql = $@"
            SELECT TOP {limit} c.ContactId,
                   ISNULL(c.FirstName, '') AS FirstName,
                   ISNULL(c.Surname, '') AS Surname,
                   ISNULL(co.Company, '') AS CompanyName,
                   c.Position, c.Email, c.Phone, c.Mobile, c.Fax,
                   c.Address1, c.Address2, c.Suburb, c.PostCode,
                   ISNULL(co.CustomerCode, '') AS CustomerCode,
                   ISNULL(co.SupplierCode, '') AS SupplierCode,
                   ISNULL(c.Code, '') AS Originator,
                   ISNULL(c.CompanyId, 0) AS CompanyId
            FROM Contacts c
            LEFT JOIN Companies co ON co.CompanyId = c.CompanyId
            WHERE 1=1";

        var parameters = new Dictionary<string, object?>();

        if (onlyWithRecords)
        {
            sql += @" AND EXISTS (
                SELECT 1 FROM Invoices i WHERE (i.Attention = c.FirstName + ' ' + c.Surname OR i.ContactId = c.ContactId)
                UNION
                SELECT 1 FROM Quotes q WHERE q.ContactId = c.ContactId
                UNION
                SELECT 1 FROM PurchaseOrders po WHERE po.ContactId = c.ContactId
            )";
        }

        if (!string.IsNullOrEmpty(search))
        {
            sql += " AND (c.FirstName LIKE @Search OR c.Surname LIKE @Search OR co.Company LIKE @Search OR c.Email LIKE @Search)";
            parameters["Search"] = $"%{search}%";
        }

        sql += " ORDER BY co.Company, c.Surname, c.FirstName";

        var dt = await _db.QueryAsync(sql, parameters);
        return dt.Map(MapContact);
    }

    public async Task<Contact?> GetContactAsync(int contactId)
    {
        var sql = @"
            SELECT c.ContactId,
                   ISNULL(c.FirstName, '') AS FirstName,
                   ISNULL(c.Surname, '') AS Surname,
                   ISNULL(co.Company, '') AS CompanyName,
                   c.Position, c.Email, c.Phone, c.Mobile, c.Fax,
                   c.Address1, c.Address2, c.Suburb, c.PostCode,
                   ISNULL(co.CustomerCode, '') AS CustomerCode,
                   ISNULL(co.SupplierCode, '') AS SupplierCode,
                   ISNULL(c.Code, '') AS Originator,
                   ISNULL(c.CompanyId, 0) AS CompanyId
            FROM Contacts c
            LEFT JOIN Companies co ON co.CompanyId = c.CompanyId
            WHERE c.ContactId = @Id";

        var dt = await _db.QueryAsync(sql, new() { ["Id"] = contactId });
        return dt.Rows.Count == 0 ? null : MapContact(dt.Rows[0]);
    }

    public async Task<int> CreateContactAsync(Contact contact, string originatorCode)
    {
        var sql = @"
            INSERT INTO Contacts (FirstName, Surname, Position, Email, Phone, Mobile, Fax,
                                  Address1, Address2, Suburb, PostCode, Code, CompanyId)
            VALUES (@FirstName, @Surname, @Position, @Email, @Phone, @Mobile, @Fax,
                    @Address1, @Address2, @Suburb, @PostCode, @Code, @CompanyId)";

        return await _db.InsertAsync(sql, new()
        {
            ["FirstName"] = contact.FirstName,
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
            ["Code"] = originatorCode,
            ["CompanyId"] = contact.CompanyId,
        });
    }

    public class ImportPreviewResult
    {
        public int NewCompanies { get; set; }
        public int NewContacts { get; set; }
    }

    public class ImportResult
    {
        public int CompaniesImported { get; set; }
        public int ContactsImported { get; set; }
    }

    public async Task<ImportPreviewResult> PreviewImportFromInvoicesAsync()
    {
        // Find companies in invoices that don't exist in Companies table
        var newCompaniesDt = await _db.QueryAsync(@"
            SELECT DISTINCT i.InvCompany AS CompanyName
            FROM Invoices i
            LEFT JOIN Companies c ON i.CompanyId = c.CompanyId
            WHERE i.InvCompany IS NOT NULL AND i.InvCompany <> ''
              AND (c.CompanyId IS NULL OR c.Company <> i.InvCompany)
            ORDER BY i.InvCompany");
        
        // Find contacts in invoices that don't exist in Contacts table
        var newContactsDt = await _db.QueryAsync(@"
            SELECT DISTINCT i.Attention AS FullName, i.InvCompany AS CompanyName
            FROM Invoices i
            LEFT JOIN Contacts c ON i.Attention = (c.FirstName + ' ' + c.Surname)
            WHERE i.Attention IS NOT NULL AND i.Attention <> ''
              AND c.ContactId IS NULL
            ORDER BY i.Attention");
        
        return new ImportPreviewResult
        {
            NewCompanies = newCompaniesDt.Rows.Count,
            NewContacts = newContactsDt.Rows.Count
        };
    }

    public async Task<ImportResult> ImportFromInvoicesAsync()
    {
        int companiesImported = 0;
        int contactsImported = 0;
        
        // Import companies
        var companiesDt = await _db.QueryAsync(@"
            SELECT DISTINCT i.InvCompany AS CompanyName, i.InvAddress AS Address
            FROM Invoices i
            LEFT JOIN Companies c ON i.CompanyId = c.CompanyId
            WHERE i.InvCompany IS NOT NULL AND i.InvCompany <> ''
              AND (c.CompanyId IS NULL OR c.Company <> i.InvCompany)");
        
        foreach (DataRow row in companiesDt.Rows)
        {
            var companyName = row["CompanyName"]?.ToString();
            if (string.IsNullOrEmpty(companyName)) continue;
            
            // Check if company already exists
            var existing = await _db.QueryAsync(
                "SELECT CompanyId FROM Companies WHERE Company = @Name",
                new() { ["Name"] = companyName });
            
            if (existing.Rows.Count > 0) continue;
            
            await _db.ExecuteNonQueryAsync(
                "INSERT INTO Companies (Company, Address1) VALUES (@Name, @Addr)",
                new() { ["Name"] = companyName, ["Addr"] = row["Address"]?.ToString() ?? "" });
            companiesImported++;
        }
        
        // Import contacts
        var contactsDt = await _db.QueryAsync(@"
            SELECT DISTINCT i.Attention AS FullName, i.InvCompany AS CompanyName
            FROM Invoices i
            LEFT JOIN Contacts c ON i.Attention = (c.FirstName + ' ' + c.Surname)
            WHERE i.Attention IS NOT NULL AND i.Attention <> ''
              AND c.ContactId IS NULL");
        
        foreach (DataRow row in contactsDt.Rows)
        {
            var fullName = row["FullName"]?.ToString();
            if (string.IsNullOrEmpty(fullName)) continue;
            
            // Split name
            var nameParts = fullName.Split(' ', 2);
            var firstName = nameParts[0];
            var surname = nameParts.Length > 1 ? nameParts[1] : "";
            
            // Find company ID
            int? companyId = null;
            var companyName = row["CompanyName"]?.ToString();
            if (!string.IsNullOrEmpty(companyName))
            {
                var companyDt = await _db.QueryAsync(
                    "SELECT CompanyId FROM Companies WHERE Company = @Name",
                    new() { ["Name"] = companyName });
                if (companyDt.Rows.Count > 0)
                    companyId = Convert.ToInt32(companyDt.Rows[0]["CompanyId"]);
            }
            
            await _db.ExecuteNonQueryAsync(@"
                INSERT INTO Contacts (FirstName, Surname, CompanyId, Code)
                VALUES (@First, @Last, @Cid, 'Import')",
                new() { 
                    ["First"] = firstName, 
                    ["Last"] = surname, 
                    ["Cid"] = (object?)companyId ?? DBNull.Value 
                });
            contactsImported++;
        }
        
        return new ImportResult
        {
            CompaniesImported = companiesImported,
            ContactsImported = contactsImported
        };
    }

    public async Task<int> UpdateContactAsync(Contact contact)
    {
        var sql = @"
            UPDATE Contacts SET
                FirstName = @FirstName, Surname = @Surname,
                Position = @Position, Email = @Email, Phone = @Phone, Mobile = @Mobile, Fax = @Fax,
                Address1 = @Address1, Address2 = @Address2, Suburb = @Suburb, PostCode = @PostCode,
                CompanyId = @CompanyId
            WHERE ContactId = @ContactId";

        return await _db.ExecuteNonQueryAsync(sql, new()
        {
            ["ContactId"] = contact.ContactId,
            ["FirstName"] = contact.FirstName,
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
            ["CompanyId"] = contact.CompanyId,
        });
    }

    public async Task<int> NormaliseAndDedupeAsync()
    {
        // 1. Assign contacts to companies based on name match
        var sql = @"
            UPDATE Contacts 
            SET CompanyId = co.CompanyId
            FROM Contacts c
            INNER JOIN Companies co ON c.FirstName + ' ' + c.Surname LIKE '%' + co.Company + '%' -- Basic heuristic
            WHERE c.CompanyId IS NULL OR c.CompanyId = 0";
        return await _db.ExecuteNonQueryAsync(sql);
    }

    public async Task DeleteContactAsync(int id) =>
        await _db.ExecuteNonQueryAsync("DELETE FROM Contacts WHERE ContactId = @id", new() { ["id"] = id });



    public async Task<List<ContactNote>> GetContactNotesAsync(int contactId)
    {
        var sql = "SELECT NoteId, ContactId, Date, NoteType, NoteText, CreatedBy FROM ContactNotes WHERE ContactId = @Id ORDER BY Date DESC";
        var dt = await _db.QueryAsync(sql, new() { ["Id"] = contactId });
        return dt.Rows.Cast<DataRow>().Select(r => new ContactNote
        {
            NoteId = Convert.ToInt32(r["NoteId"]),
            ContactId = Convert.ToInt32(r["ContactId"]),
            Date = Convert.ToDateTime(r["Date"]),
            NoteType = r["NoteType"]?.ToString() ?? "",
            NoteText = r["NoteText"]?.ToString() ?? "",
            CreatedBy = r["CreatedBy"]?.ToString() ?? ""
        }).ToList();
    }

    public async Task<int> AddContactNoteAsync(ContactNote note)
    {
        var sql = "INSERT INTO ContactNotes (ContactId, Date, NoteType, NoteText, CreatedBy) VALUES (@ContactId, @Date, @NoteType, @NoteText, @CreatedBy)";
        return await _db.InsertAsync(sql, new()
        {
            ["ContactId"] = note.ContactId,
            ["Date"] = note.Date,
            ["NoteType"] = note.NoteType,
            ["NoteText"] = note.NoteText,
            ["CreatedBy"] = note.CreatedBy
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
        PortalUsername = r.Table.Columns.Contains("PortalUsername") && r["PortalUsername"] != DBNull.Value ? r["PortalUsername"]?.ToString() : null,
        IsPortalEnabled = r.Table.Columns.Contains("IsPortalEnabled") && r["IsPortalEnabled"] != DBNull.Value && Convert.ToBoolean(r["IsPortalEnabled"]),
        PortalAccessExpires = r.Table.Columns.Contains("PortalAccessExpires") && r["PortalAccessExpires"] != DBNull.Value ? Convert.ToDateTime(r["PortalAccessExpires"]) : null,
    };

    public async Task<Contact?> ValidatePortalCredentialsAsync(string username, string password)
    {
        var sql = @"
            SELECT c.ContactId, c.FirstName, c.Surname, c.CompanyId, co.Company,
                   c.PortalUsername, c.PortalPasswordHash, c.IsPortalEnabled, c.PortalAccessExpires
            FROM Contacts c
            LEFT JOIN Companies co ON co.CompanyId = c.CompanyId
            WHERE (c.PortalUsername = @Username OR c.Email = @Username)
              AND c.PortalPasswordHash IS NOT NULL";

        var dt = await _db.QueryAsync(sql, new() { ["Username"] = username });
        if (dt.Rows.Count == 0) return null;

        var hashedPassword = HashPassword(password);
        var contact = MapContact(dt.Rows[0]);

        if (contact.PortalPasswordHash != hashedPassword) return null;
        return contact;
    }

    public async Task<Contact?> GetCurrentPortalContactAsync()
    {
        return null;
    }

    public async Task UpdatePortalLoginAsync(int contactId)
    {
        var sql = "UPDATE Contacts SET PortalLastLogin = GETDATE() WHERE ContactId = @ContactId";
        await _db.ExecuteNonQueryAsync(sql, new() { ["ContactId"] = contactId });
    }

    public async Task SetPortalCredentialsAsync(int contactId, string username, string password, DateTime? expires = null)
    {
        var sql = @"
            UPDATE Contacts SET 
                PortalUsername = @Username,
                PortalPasswordHash = @PasswordHash,
                IsPortalEnabled = 1,
                PortalAccessExpires = @Expires
            WHERE ContactId = @ContactId";

        await _db.ExecuteNonQueryAsync(sql, new()
        {
            ["ContactId"] = contactId,
            ["Username"] = username,
            ["PasswordHash"] = HashPassword(password),
            ["Expires"] = (object?)expires ?? DBNull.Value,
        });
    }

    public async Task DisablePortalAsync(int contactId)
    {
        var sql = @"
            UPDATE Contacts SET 
                IsPortalEnabled = 0,
                PortalUsername = NULL,
                PortalPasswordHash = NULL
            WHERE ContactId = @ContactId";

        await _db.ExecuteNonQueryAsync(sql, new() { ["ContactId"] = contactId });
    }

    private static string HashPassword(string password)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(password + "MyDeskPortalSalt2024");
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
