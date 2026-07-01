using Microsoft.Extensions.Logging;

namespace MyDesk.Shared.Services;

/// <summary>
/// Persists Ask AI conversation messages per user so history survives page navigation
/// and browser refresh. Stores raw role/content pairs (same shape as the OpenAI API)
/// plus an optional JSON tool-trace blob for the debug panel.
/// </summary>
public class AiConversationService
{
    private readonly DatabaseService _db;
    private readonly ILogger<AiConversationService> _logger;

    public AiConversationService(DatabaseService db, ILogger<AiConversationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EnsureTableAsync()
    {
        try
        {
            await _db.ExecuteNonQueryAsync(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AiConversationHistory')
                CREATE TABLE AiConversationHistory (
                    MessageId   INT IDENTITY(1,1) PRIMARY KEY,
                    UserCode    NVARCHAR(50)  NOT NULL,
                    SessionId   NVARCHAR(50)  NULL,
                    Role        NVARCHAR(20)  NOT NULL,
                    Content     NVARCHAR(MAX) NOT NULL,
                    ToolTrace   NVARCHAR(MAX) NULL,
                    CreatedAt   DATETIME      NOT NULL DEFAULT GETDATE()
                );
                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AiConversationHistory_UserCode')
                    CREATE INDEX IX_AiConversationHistory_UserCode
                        ON AiConversationHistory (UserCode, CreatedAt DESC);");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not ensure AiConversationHistory table");
        }
    }

    /// <summary>Returns the most recent <paramref name="limit"/> messages for the user, oldest-first.</summary>
    public async Task<List<AiConversationMessage>> GetRecentAsync(string userCode, int limit = 40)
    {
        try
        {
            var rows = await _db.QueryAsync(@"
                SELECT MessageId, UserCode, SessionId, Role, Content, ToolTrace, CreatedAt
                FROM (
                    SELECT TOP (@Limit) MessageId, UserCode, SessionId, Role, Content, ToolTrace, CreatedAt
                    FROM AiConversationHistory
                    WHERE UserCode = @User
                    ORDER BY CreatedAt DESC
                ) sub
                ORDER BY CreatedAt ASC",
                new Dictionary<string, object?> { ["User"] = userCode, ["Limit"] = limit });

            return rows.Map(r => new AiConversationMessage
            {
                MessageId = Convert.ToInt32(r["MessageId"]),
                UserCode  = r["UserCode"]?.ToString() ?? "",
                SessionId = r["SessionId"]?.ToString(),
                Role      = r["Role"]?.ToString() ?? "user",
                Content   = r["Content"]?.ToString() ?? "",
                ToolTrace = r["ToolTrace"]?.ToString(),
                CreatedAt = Convert.ToDateTime(r["CreatedAt"]),
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load AI conversation history for user {User}", userCode);
            return new();
        }
    }

    public async Task AppendAsync(string userCode, string sessionId, string role, string content, string? toolTrace = null)
    {
        try
        {
            await _db.InsertAsync(@"
                INSERT INTO AiConversationHistory (UserCode, SessionId, Role, Content, ToolTrace)
                VALUES (@User, @Session, @Role, @Content, @Trace)",
                new Dictionary<string, object?>
                {
                    ["User"]    = userCode,
                    ["Session"] = (object?)sessionId ?? DBNull.Value,
                    ["Role"]    = role,
                    ["Content"] = content,
                    ["Trace"]   = (object?)toolTrace ?? DBNull.Value,
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not save AI message for user {User}", userCode);
        }
    }

    public async Task ClearHistoryAsync(string userCode)
    {
        try
        {
            await _db.ExecuteNonQueryAsync(
                "DELETE FROM AiConversationHistory WHERE UserCode = @User",
                new Dictionary<string, object?> { ["User"] = userCode });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not clear AI history for user {User}", userCode);
        }
    }

    /// <summary>Deletes history older than <paramref name="days"/> days (all users). Called from a scheduled job.</summary>
    public async Task PurgeOldAsync(int days = 90)
    {
        try
        {
            await _db.ExecuteNonQueryAsync(
                "DELETE FROM AiConversationHistory WHERE CreatedAt < DATEADD(DAY, -@Days, GETDATE())",
                new Dictionary<string, object?> { ["Days"] = days });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not purge old AI history");
        }
    }
}

public class AiConversationMessage
{
    public int     MessageId { get; set; }
    public string  UserCode  { get; set; } = "";
    public string? SessionId { get; set; }
    public string  Role      { get; set; } = "user";
    public string  Content   { get; set; } = "";
    public string? ToolTrace { get; set; }
    public DateTime CreatedAt { get; set; }
}
