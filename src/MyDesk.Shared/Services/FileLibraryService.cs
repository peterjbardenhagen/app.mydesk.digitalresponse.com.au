using System.Data;
using Microsoft.Extensions.Logging;

namespace MyDesk.Shared.Services;

public class FileLibraryService
{
    private readonly DatabaseService _db;
    private readonly ILogger<FileLibraryService> _logger;

    public FileLibraryService(DatabaseService db, ILogger<FileLibraryService> logger)
    { _db = db; _logger = logger; }

    /// <summary>
    /// Idempotently ensures the FileLibrary table exists and contains every column
    /// the service writes to. Safe to call on every startup — pre-v3 installs that
    /// were created without <c>ModifiedAt</c> / <c>IsShared</c> get those columns
    /// added in place. Called from the Program.cs SafeInit chain.
    /// </summary>
    public async Task EnsureTableAsync()
    {
        var sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FileLibrary')
BEGIN
    CREATE TABLE FileLibrary (
        FileId               UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        ParentFolderId       UNIQUEIDENTIFIER NULL,
        CompanyId            INT NULL,
        Name                 NVARCHAR(255) NOT NULL,
        IsFolder             BIT NOT NULL DEFAULT 0,
        FilePath             NVARCHAR(500) NULL,
        FileSize             BIGINT NULL,
        ContentType          NVARCHAR(100) NULL,
        SharedWithCompanies  NVARCHAR(MAX) NULL,
        SharedWithContacts   NVARCHAR(MAX) NULL,
        IsPublic             BIT NOT NULL DEFAULT 0,
        IsShared             BIT NOT NULL DEFAULT 0,
        CreatedBy            NVARCHAR(50) NOT NULL,
        CreatedAt            DATETIME2 NOT NULL DEFAULT GETDATE(),
        ModifiedAt           DATETIME2 NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_FileLibrary_Parent   ON FileLibrary(ParentFolderId);
    CREATE INDEX IX_FileLibrary_Company  ON FileLibrary(CompanyId);
    CREATE INDEX IX_FileLibrary_IsFolder ON FileLibrary(IsFolder);
END
ELSE
BEGIN
    -- Pre-v3 installs may be missing some of these. Add idempotently.
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('FileLibrary') AND name = 'ModifiedAt')
        ALTER TABLE FileLibrary ADD ModifiedAt DATETIME2 NOT NULL DEFAULT GETDATE();

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('FileLibrary') AND name = 'IsShared')
        ALTER TABLE FileLibrary ADD IsShared BIT NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('FileLibrary') AND name = 'IsPublic')
        ALTER TABLE FileLibrary ADD IsPublic BIT NOT NULL DEFAULT 0;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('FileLibrary') AND name = 'FilePath')
        ALTER TABLE FileLibrary ADD FilePath NVARCHAR(500) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('FileLibrary') AND name = 'FileSize')
        ALTER TABLE FileLibrary ADD FileSize BIGINT NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('FileLibrary') AND name = 'ContentType')
        ALTER TABLE FileLibrary ADD ContentType NVARCHAR(100) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('FileLibrary') AND name = 'SharedWithCompanies')
        ALTER TABLE FileLibrary ADD SharedWithCompanies NVARCHAR(MAX) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('FileLibrary') AND name = 'SharedWithContacts')
        ALTER TABLE FileLibrary ADD SharedWithContacts NVARCHAR(MAX) NULL;

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('FileLibrary') AND name = 'CreatedAt')
        ALTER TABLE FileLibrary ADD CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE();
END";
        await _db.ExecuteNonQueryAsync(sql);
    }

    public async Task<List<FileLibraryItem>> GetRootFoldersAsync(int? companyId = null, string sortBy = "newest")
    {
        var orderBy = GetSortClause(sortBy);
        var dt = await _db.QueryAsync(@"
            SELECT fl.*, 
                   co.Company AS CompanyName
            FROM FileLibrary fl
            LEFT JOIN Companies co ON co.CompanyId = fl.CompanyId
            WHERE fl.ParentFolderId IS NULL
              AND (fl.CompanyId = @CompanyId OR (@CompanyId IS NULL AND fl.CompanyId IS NULL))
            ORDER BY fl.IsFolder DESC, " + orderBy,
            new() { ["CompanyId"] = companyId.HasValue ? (object)companyId.Value : DBNull.Value });

        return dt.Rows.Cast<DataRow>().Select(MapRow).ToList();
    }

    public async Task<List<FileLibraryItem>> GetFolderContentsAsync(Guid folderId, string sortBy = "newest")
    {
        var orderBy = GetSortClause(sortBy);
        var dt = await _db.QueryAsync(@"
            SELECT fl.*, 
                   co.Company AS CompanyName
            FROM FileLibrary fl
            LEFT JOIN Companies co ON co.CompanyId = fl.CompanyId
            WHERE fl.ParentFolderId = @Id
            ORDER BY fl.IsFolder DESC, " + orderBy,
            new() { ["Id"] = folderId });

        return dt.Rows.Cast<DataRow>().Select(MapRow).ToList();
    }

    private static string GetSortClause(string sortBy) => sortBy switch
    {
        "oldest" => "fl.CreatedAt ASC",
        "alpha" => "fl.Name ASC",
        _ => "fl.CreatedAt DESC"
    };

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
        await _db.ExecuteNonQueryAsync(@"
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

        await _db.ExecuteNonQueryAsync(@"
            DELETE FROM FileLibrary WHERE FileId = @Id",
            new() { ["Id"] = fileId });

        _logger.LogInformation("File/Folder deleted: {FileId} by {UserCode}", fileId, userCode);
    }

    public async Task SaveItemAsync(FileLibraryItem item, string userCode)
    {
        var now = DateTime.Now;
        if (item.CreatedAt < new DateTime(1753, 1, 1)) item.CreatedAt = now;
        if (item.ModifiedAt < new DateTime(1753, 1, 1)) item.ModifiedAt = now;

        var existing = await GetItemAsync(item.FileId);
        if (existing == null)
        {
            await _db.ExecuteNonQueryAsync(@"
                INSERT INTO FileLibrary (FileId, ParentFolderId, CompanyId, Name, IsFolder, FilePath, FileSize, ContentType, SharedWithCompanies, SharedWithContacts, IsPublic, CreatedBy, CreatedAt, ModifiedAt)
                VALUES (@FileId, @ParentFolderId, @CompanyId, @Name, @IsFolder, @FilePath, @FileSize, @ContentType, @SharedWithCompanies, @SharedWithContacts, @IsPublic, @CreatedBy, @CreatedAt, @ModifiedAt)",
                new() {
                    ["FileId"] = item.FileId,
                    ["ParentFolderId"] = item.ParentFolderId.HasValue ? (object)item.ParentFolderId.Value : DBNull.Value,
                    ["CompanyId"] = item.CompanyId.HasValue ? (object)item.CompanyId.Value : DBNull.Value,
                    ["Name"] = item.Name,
                    ["IsFolder"] = item.IsFolder,
                    ["FilePath"] = item.FilePath ?? (object)DBNull.Value,
                    ["FileSize"] = item.FileSize.HasValue ? (object)item.FileSize.Value : DBNull.Value,
                    ["ContentType"] = item.ContentType ?? (object)DBNull.Value,
                    ["SharedWithCompanies"] = item.SharedWithCompanies ?? (object)DBNull.Value,
                    ["SharedWithContacts"] = item.SharedWithContacts ?? (object)DBNull.Value,
                    ["IsPublic"] = item.IsPublic,
                    ["CreatedBy"] = item.CreatedBy,
                    ["CreatedAt"] = item.CreatedAt,
                    ["ModifiedAt"] = item.ModifiedAt
                });
            _logger.LogInformation("File item inserted: {Name} by {UserCode}", item.Name, userCode);
        }
        else
        {
            await _db.ExecuteNonQueryAsync(@"
                UPDATE FileLibrary 
                SET ParentFolderId = @ParentFolderId, CompanyId = @CompanyId, Name = @Name, IsFolder = @IsFolder,
                    FilePath = @FilePath, FileSize = @FileSize, ContentType = @ContentType,
                    SharedWithCompanies = @SharedWithCompanies, SharedWithContacts = @SharedWithContacts,
                    IsPublic = @IsPublic, ModifiedAt = @ModifiedAt
                WHERE FileId = @FileId",
                new() {
                    ["FileId"] = item.FileId,
                    ["ParentFolderId"] = item.ParentFolderId.HasValue ? (object)item.ParentFolderId.Value : DBNull.Value,
                    ["CompanyId"] = item.CompanyId.HasValue ? (object)item.CompanyId.Value : DBNull.Value,
                    ["Name"] = item.Name,
                    ["IsFolder"] = item.IsFolder,
                    ["FilePath"] = item.FilePath ?? (object)DBNull.Value,
                    ["FileSize"] = item.FileSize.HasValue ? (object)item.FileSize.Value : DBNull.Value,
                    ["ContentType"] = item.ContentType ?? (object)DBNull.Value,
                    ["SharedWithCompanies"] = item.SharedWithCompanies ?? (object)DBNull.Value,
                    ["SharedWithContacts"] = item.SharedWithContacts ?? (object)DBNull.Value,
                    ["IsPublic"] = item.IsPublic,
                    ["ModifiedAt"] = now
                });
            _logger.LogInformation("File item updated: {Name} by {UserCode}", item.Name, userCode);
        }
    }

    public async Task ShareItemAsync(Guid fileId, List<int>? companyIds = null, List<int>? contactIds = null)
    {
        var sharedCompanies = companyIds != null ? System.Text.Json.JsonSerializer.Serialize(companyIds) : null;
        var sharedContacts = contactIds != null ? System.Text.Json.JsonSerializer.Serialize(contactIds) : null;
        
        await _db.ExecuteNonQueryAsync(@"
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
            IsPublic = r.Table.Columns.Contains("IsPublic") && r["IsPublic"] != DBNull.Value ? Convert.ToBoolean(r["IsPublic"]) : false,
            CreatedBy = r["CreatedBy"]?.ToString() ?? "",
            CreatedAt = r.Table.Columns.Contains("CreatedAt") && r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]) : DateTime.Now,
            ModifiedAt = r.Table.Columns.Contains("ModifiedAt") && r["ModifiedAt"] != DBNull.Value ? Convert.ToDateTime(r["ModifiedAt"]) : DateTime.Now,
            CompanyName = r.Table.Columns.Contains("CompanyName") ? r["CompanyName"]?.ToString() : null
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
    public long SizeBytes { get; set; }
    public string? ContentType { get; set; }
    public string? SharedWithCompanies { get; set; } // JSON array of CompanyId
    public string? SharedWithContacts { get; set; } // JSON array of ContactId
    public bool IsPublic { get; set; }
    public string CreatedBy { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime ModifiedAt { get; set; } = DateTime.Now;
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
