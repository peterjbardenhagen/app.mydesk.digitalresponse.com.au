using Microsoft.Extensions.Logging;

namespace Techlight.MyDesk.Shared.Services;

public class NoticeboardService
{
    private readonly DatabaseService _db;
    private readonly ILogger<NoticeboardService> _logger;

    public NoticeboardService(DatabaseService db, ILogger<NoticeboardService> logger)
    { _db = db; _logger = logger; }

    public async Task<List<Notice>> GetAllAsync()
    {
        var dt = await _db.QueryAsync(@"
            SELECT NoticeboardId, Title, Notice, DatePosted,
                   ExpiryDate, PostedBy
            FROM Noticeboard
            ORDER BY DatePosted DESC");
        return dt.Map(r => new Notice
        {
            NoticeboardId = Convert.ToInt32(r["NoticeboardId"]),
            Title         = r.Table.Columns.Contains("Title") ? r["Title"]?.ToString() ?? "" : "",
            Body          = r.Table.Columns.Contains("Notice") ? r["Notice"]?.ToString() : null,
            DatePosted    = r["DatePosted"] == DBNull.Value ? null : Convert.ToDateTime(r["DatePosted"]),
            ExpiryDate    = r.Table.Columns.Contains("ExpiryDate") && r["ExpiryDate"] != DBNull.Value
                            ? Convert.ToDateTime(r["ExpiryDate"]) : (DateTime?)null,
            PostedBy      = r.Table.Columns.Contains("PostedBy") ? r["PostedBy"]?.ToString() : null,
        }).ToList();
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
        await _db.ExecuteAsync(
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
        await _db.ExecuteAsync("DELETE FROM Noticeboard WHERE NoticeboardId = @id", new() { ["id"] = id });
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
