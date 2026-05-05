using Microsoft.Extensions.Logging;

namespace MyDesk.Shared.Services;

public class NoticeboardService
{
    private readonly DatabaseService _db;
    private readonly ILogger<NoticeboardService> _logger;

    public NoticeboardService(DatabaseService db, ILogger<NoticeboardService> logger)
    { _db = db; _logger = logger; }

    public async Task EnsureTableAsync()
    {
        try
        {
            await _db.ExecuteNonQueryAsync(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Noticeboard')
                CREATE TABLE Noticeboard (
                    NoticeboardId INT IDENTITY(1,1) PRIMARY KEY,
                    Title NVARCHAR(500) NOT NULL,
                    Notice NVARCHAR(MAX),
                    DatePosted DATETIME DEFAULT GETDATE(),
                    ExpiryDate DATETIME NULL,
                    PostedBy NVARCHAR(100)
                )");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not create Noticeboard table");
        }
    }

    public async Task<List<Notice>> GetAllAsync()
    {
        await EnsureTableAsync();
        
        var sql = @"
            SELECT TOP 100 NoticeboardId, Title, Notice, DatePosted,
                   ExpiryDate, PostedBy
            FROM Noticeboard
            WHERE @Today <= ISNULL(ExpiryDate, '2099-12-31')
            ORDER BY DatePosted DESC";
        
        try
        {
            var dt = await _db.QueryAsync(sql, new Dictionary<string, object?> { ["Today"] = DateTime.Today });
            return dt.Map(r => new Notice
            {
                NoticeboardId = Convert.ToInt32(r["NoticeboardId"]),
                Title         = r["Title"]?.ToString() ?? "",
                Body          = r["Notice"]?.ToString(),
                DatePosted    = r["DatePosted"] == DBNull.Value ? null : Convert.ToDateTime(r["DatePosted"]),
                ExpiryDate    = r["ExpiryDate"] != DBNull.Value ? Convert.ToDateTime(r["ExpiryDate"]) : null,
                PostedBy      = r["PostedBy"]?.ToString(),
            }).ToList();
        }
        catch
        {
            return new List<Notice>();
        }
    }

    public async Task<int> SaveAsync(Notice n)
    {
        if (n.NoticeboardId == 0)
        {
            return await _db.InsertAsync(
                @"INSERT INTO Noticeboard (Title, Notice, DatePosted, ExpiryDate, PostedBy)
                  VALUES (@Title, @Body, GETDATE(), @Expires, @By)",
                new()
                {
                    ["Title"] = n.Title,
                    ["Body"]  = (object?)n.Body ?? DBNull.Value,
                    ["Expires"] = (object?)n.ExpiryDate ?? DBNull.Value,
                    ["By"]    = (object?)n.PostedBy ?? DBNull.Value,
                });
        }
        await _db.ExecuteNonQueryAsync(
            @"UPDATE Noticeboard SET Title=@Title, Notice=@Body, ExpiryDate=@Expires
              WHERE NoticeboardId=@Id",
            new()
            {
                ["Id"] = n.NoticeboardId,
                ["Title"] = n.Title,
                ["Body"]  = (object?)n.Body ?? DBNull.Value,
                ["Expires"] = (object?)n.ExpiryDate ?? DBNull.Value,
            });
        return n.NoticeboardId;
    }

    public async Task DeleteAsync(int id) =>
        await _db.ExecuteNonQueryAsync("DELETE FROM Noticeboard WHERE NoticeboardId = @id", new() { ["id"] = id });
}

public class Notice
{
    public int NoticeboardId { get; set; }
    public string Title { get; set; } = "";
    public string? Body { get; set; }
    public DateTime? DatePosted { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? PostedBy { get; set; }
}
