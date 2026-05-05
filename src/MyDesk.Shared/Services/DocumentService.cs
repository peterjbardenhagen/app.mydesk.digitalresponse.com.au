using System.Data;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class DocumentService
{
    private readonly DatabaseService _db;
    private readonly ILogger<DocumentService> _logger;

    public DocumentService(DatabaseService db, ILogger<DocumentService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<CustomerFile>> GetCustomerFilesAsync(int companyId)
    {
        var sql = @"
            SELECT DocumentId, FileName, FileSize, UploadedDate, CompanyId
            FROM Documents
            WHERE CompanyId = @CompanyId
            ORDER BY UploadedDate DESC";

        var dt = await _db.QueryAsync(sql, new() { ["CompanyId"] = companyId });
        return dt.Map(MapCustomerFile);
    }

    public async Task<int> UploadFileAsync(string fileName, byte[] content, int companyId, string uploadedBy)
    {
        var fileSize = FormatFileSize(content.Length);
        
        var sql = @"
            INSERT INTO Documents (FileName, FileSize, FileContent, CompanyId, UploadedBy, UploadedDate)
            VALUES (@FileName, @FileSize, @FileContent, @CompanyId, @UploadedBy, GETDATE());
            SELECT SCOPE_IDENTITY();";

        return await _db.InsertAsync(sql, new()
        {
            ["FileName"] = fileName,
            ["FileSize"] = fileSize,
            ["FileContent"] = content,
            ["CompanyId"] = companyId,
            ["UploadedBy"] = uploadedBy,
        });
    }

    public async Task<byte[]> DownloadFileAsync(int documentId)
    {
        var sql = "SELECT FileContent FROM Documents WHERE DocumentId = @Id";
        var dt = await _db.QueryAsync(sql, new() { ["Id"] = documentId });
        
        if (dt.Rows.Count == 0) return Array.Empty<byte>();
        
        var content = dt.Rows[0]["FileContent"];
        return content == DBNull.Value ? Array.Empty<byte>() : (byte[])content;
    }

    public async Task DeleteFileAsync(int documentId)
    {
        var sql = "DELETE FROM Documents WHERE DocumentId = @Id";
        await _db.ExecuteNonQueryAsync(sql, new() { ["Id"] = documentId });
    }

    private static CustomerFile MapCustomerFile(DataRow r) => new()
    {
        DocumentId = Convert.ToInt32(r["DocumentId"]),
        FileName = r["FileName"]?.ToString() ?? "",
        FileSize = r["FileSize"]?.ToString() ?? "",
        UploadedDate = Convert.ToDateTime(r["UploadedDate"]),
        CompanyId = Convert.ToInt32(r["CompanyId"]),
    };

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1) { order++; size /= 1024; }
        return $"{size:0.##} {sizes[order]}";
    }
}
