using Microsoft.Extensions.Logging;

namespace MyDesk.Shared.Services;

/// <summary>
/// AI interaction audit logging for compliance.
/// Per Proposal #272: "Every interaction is logged for compliance."
/// </summary>
public class AiAuditService
{
    private readonly DatabaseService _db;
    private readonly ILogger<AiAuditService> _logger;

    public AiAuditService(DatabaseService db, ILogger<AiAuditService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EnsureTableAsync()
    {
        try
        {
            await _db.ExecuteAsync(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'AiInteractionAudit')
                CREATE TABLE AiInteractionAudit (
                    AuditId INT IDENTITY(1,1) PRIMARY KEY,
                    UserCode NVARCHAR(10) NULL,
                    Channel NVARCHAR(50) NOT NULL,  -- web, telegram, voice, api
                    Prompt NVARCHAR(MAX) NULL,
                    Response NVARCHAR(MAX) NULL,
                    ToolsUsed NVARCHAR(500) NULL,
                    Success BIT NOT NULL DEFAULT 1,
                    DurationMs INT NULL,
                    DateEntered DATETIME NOT NULL DEFAULT GETDATE()
                )");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not ensure AiInteractionAudit table");
        }
    }

    public async Task LogAsync(
        string userCode, string channel, string prompt, string response,
        string? toolsUsed = null, bool success = true, int? durationMs = null)
    {
        try
        {
            await _db.InsertAsync(@"
                INSERT INTO AiInteractionAudit
                    (UserCode, Channel, Prompt, Response, ToolsUsed, Success, DurationMs)
                VALUES
                    (@User, @Ch, @Prompt, @Response, @Tools, @Success, @Dur)",
                new()
                {
                    ["User"]     = (object?)userCode ?? DBNull.Value,
                    ["Ch"]       = channel,
                    ["Prompt"]   = (object?)prompt ?? DBNull.Value,
                    ["Response"] = (object?)response ?? DBNull.Value,
                    ["Tools"]    = (object?)toolsUsed ?? DBNull.Value,
                    ["Success"]  = success,
                    ["Dur"]      = (object?)durationMs ?? DBNull.Value,
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log AI interaction");
        }
    }

    public async Task<List<AiAuditEntry>> GetRecentAsync(int limit = 100, string? userCode = null)
    {
        var where = userCode != null ? "WHERE UserCode = @User" : "";
        var p = userCode != null ? new Dictionary<string, object?> { ["User"] = userCode } : new();

        var dt = await _db.QueryAsync($@"
            SELECT TOP {limit} AuditId, UserCode, Channel, Prompt, Response,
                   ToolsUsed, Success, DurationMs, DateEntered
            FROM AiInteractionAudit
            {where}
            ORDER BY DateEntered DESC", p);

        return dt.Map(r => new AiAuditEntry
        {
            AuditId     = Convert.ToInt32(r["AuditId"]),
            UserCode    = r["UserCode"]?.ToString(),
            Channel     = r["Channel"]?.ToString() ?? "",
            Prompt      = r["Prompt"]?.ToString(),
            Response    = r["Response"]?.ToString(),
            ToolsUsed   = r["ToolsUsed"]?.ToString(),
            Success     = r["Success"] != DBNull.Value && Convert.ToBoolean(r["Success"]),
            DurationMs  = r["DurationMs"] == DBNull.Value ? null : (int?)Convert.ToInt32(r["DurationMs"]),
            DateEntered = Convert.ToDateTime(r["DateEntered"]),
        });
    }
}

public class AiAuditEntry
{
    public int AuditId { get; set; }
    public string? UserCode { get; set; }
    public string Channel { get; set; } = "";
    public string? Prompt { get; set; }
    public string? Response { get; set; }
    public string? ToolsUsed { get; set; }
    public bool Success { get; set; }
    public int? DurationMs { get; set; }
    public DateTime DateEntered { get; set; }
}
