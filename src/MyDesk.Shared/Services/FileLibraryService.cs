using System.Data;
using Microsoft.Extensions.Logging;

namespace MyDesk.Shared.Services;

public class FileLibraryService
{
    private readonly DatabaseService _db;
    private readonly ILogger<FileLibraryService> _logger;

    public FileLibraryService(DatabaseService db, ILogger<FileLibraryService> logger)
    { _db = db; _logger = logger; }

    public async Task<List<FileLibraryItem>> GetRootFoldersAsync(int? companyId = null)
    {
        var dt = await _db.QueryAsync(@"
            SELECT fl.*, 
                   co.Company AS CompanyName
            FROM FileLibrary fl
            LEFT JOIN Companies co ON co.CompanyId = fl.CompanyId
            WHERE fl.ParentFolderId IS NULL
              AND (fl.CompanyId = @CompanyId OR (@CompanyId IS NULL AND fl.CompanyId IS NULL))
            ORDER BY fl.IsFolder DESC, fl.Name",
            new() { ["CompanyId"] = companyId.HasValue ? (object)companyId.Value : DBNull.Value });

        return dt.Rows.Cast<DataRow>().Select(MapRow).ToList();
    }

    public async Task<List<FileLibraryItem>> GetFolderContentsAsync(Guid folderId)
    {
        var dt = await _db.QueryAsync(@"
            SELECT fl.*, 
                   co.Company AS CompanyName
            FROM FileLibrary fl
            LEFT JOIN Companies co ON co.CompanyId = fl.CompanyId
            WHERE fl.ParentFolderId = @Id
            ORDER BY fl.IsFolder DESC, fl.Name",
            new() { ["Id"] = folderId });

        return dt.Rows.Cast<DataRow>().Select(MapRow).ToList();
    }

    public async Task<FileLibraryItem?> GetItemAsync(Guid fileId)
    {
        var dt = await _db.QueryAsync(@"
            SELECT fl.*, 
                   co.Company AS CompanyName
            FROM FileLibrary fl
            LEFT JOIN Companies co ON co.CompanyId = fl.CompanyId
            WHERE fl.FileId = @Id",
            new() { ["Id"] = fileId });

        return dt.Rows.Count == 0 ? null : MapRow(dt.Rows[0]);
    }

    public async Task<Guid> CreateFolderAsync(string name, Guid? parentFolderId = null, int? companyId = null, string createdBy = "")
    {
        var fileId = Guid.NewGuid();
        await _db.ExecuteAsync(@"
            INSERT INTO FileLibrary (FileId, ParentFolderId, CompanyId, Name, IsFolder, CreatedBy)
            VALUES (@FileId, @ParentId, @CompanyId, @Name, 1, @CreatedBy)",
            new() { ["FileId"] = fileId, 
                    ["ParentId"] = parentFolderId.HasValue ? (object)parentFolderId.Value : (object)DBNull.Value,
                    ["CompanyId"] = companyId.HasValue ? (object)companyId.Value : (object)DBNull.Value,
                    ["Name"] = name,
                    ["CreatedBy"] = createdBy });

        _logger.LogInformation("Folder created: {Name} by {CreatedBy}", name, createdBy);
        return fileId;
    }

    public async Task DeleteItemAsync(Guid fileId, string userCode)
    {
        // If folder, delete all children first (recursive)
        var children = await _db.QueryAsync(@"
            SELECT FileId FROM FileLibrary WHERE ParentFolderId = @Id",
            new() { ["Id"] = fileId });

        foreach (DataRow row in children.Rows)
        {
            await DeleteItemAsync((Guid)row["FileId"], userCode);
        }

        await _db.ExecuteAsync(@"
            DELETE FROM FileLibrary WHERE FileId = @Id",
            new() { ["Id"] = fileId });

        _logger.LogInformation("File/Folder deleted: {FileId} by {UserCode}", fileId, userCode);
    }

    public async Task ShareItemAsync(Guid fileId, List<int>? companyIds = null, List<int>? contactIds = null)
    {
        var sharedCompanies = companyIds != null ? System.Text.Json.JsonSerializer.Serialize(companyIds) : null;
        var sharedContacts = contactIds != null ? System.Text.Json.JsonSerializer.Serialize(contactIds) : null;
        
        await _db.ExecuteAsync(@"
            UPDATE FileLibrary 
            SET SharedWithCompanies = @Companies, SharedWithContacts = @Contacts, IsShared = 1
            WHERE FileId = @Id",
            new() { ["Id"] = fileId, ["Companies"] = sharedCompanies ?? (object)DBNull.Value, ["Contacts"] = sharedContacts ?? (object)DBNull.Value });
    }

    private static FileLibraryItem MapRow(DataRow r)
    {
        return new FileLibraryItem
        {
            FileId = (Guid)r["FileId"],
            ParentFolderId = r["ParentFolderId"] == DBNull.Value ? null : (Guid?)r["ParentFolderId"],
            CompanyId = r["CompanyId"] == DBNull.Value ? null : (int?)Convert.ToInt32(r["CompanyId"]),
            Name = r["Name"].ToString()!,
            IsFolder = Convert.ToBoolean(r["IsFolder"]),
            FilePath = r["FilePath"]?.ToString(),
            FileSize = r["FileSize"] == DBNull.Value ? null : (long?)Convert.ToInt64(r["FileSize"]),
            ContentType = r["ContentType"]?.ToString(),
            SharedWithCompanies = r["SharedWithCompanies"]?.ToString(),
            SharedWithContacts = r["SharedWithContacts"]?.ToString(),
            IsPublic = Convert.ToBoolean(r["IsPublic"]),
            CreatedBy = r["CreatedBy"]?.ToString() ?? "",
            CreatedAt = r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]) : DateTime.Now,
            ModifiedAt = r["ModifiedAt"] != DBNull.Value ? Convert.ToDateTime(r["ModifiedAt"]) : DateTime.Now,
            CompanyName = r["CompanyName"]?.ToString()
        };
    }
}

public class FileLibraryItem
{
    public Guid FileId { get; set; }
    public Guid? ParentFolderId { get; set; }
    public int? CompanyId { get; set; }
    public string Name { get; set; } = "";
    public bool IsFolder { get; set; }
    public string? FilePath { get; set; }
    public long? FileSize { get; set; }
    public string? ContentType { get; set; }
    public string? SharedWithCompanies { get; set; } // JSON array of CompanyId
    public string? SharedWithContacts { get; set; } // JSON array of ContactId
    public bool IsPublic { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string? CompanyName { get; set; }

    public string GetFormattedSize()
    {
        if (!FileSize.HasValue) return "";
        var size = FileSize.Value;
        return size switch
        {
            > 1024 * 1024 * 1024 => $"{size / (1024.0 * 1024.0 * 1024.0):F2} GB",
            > 1024 * 1024 => $"{size / (1024.0 * 1024.0):F2} MB",
            > 1024 => $"{size / 1024.0:F2} KB",
            _ => $"{size} B"
        };
    }

    public string GetIcon()
    {
        if (IsFolder) return "folder";
        return ContentType switch
        {
            string c when c.Contains("pdf") => "picture_as_pdf",
            string c when c.Contains("image") => "image",
            string c when c.Contains("word") || c.Contains("document") => "description",
            string c when c.Contains("excel") || c.Contains("spreadsheet") => "table_chart",
            string c when c.Contains("zip") || c.Contains("compressed") => "archive",
            _ => "insert_drive_file"
        };
    }
}
