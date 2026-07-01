using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MyDesk.Shared.Services;

/// <summary>
/// Per-user intelligence profile: Composio API key, coaching preferences,
/// Telegram integration, connected external apps, and coaching session history.
/// </summary>
public class UserIntelligenceService
{
    private readonly DatabaseService _db;
    private readonly ILogger<UserIntelligenceService> _logger;

    public UserIntelligenceService(DatabaseService db, ILogger<UserIntelligenceService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EnsureTablesAsync()
    {
        try
        {
            await _db.ExecuteNonQueryAsync(@"
                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserIntelligenceProfiles')
                CREATE TABLE UserIntelligenceProfiles (
                    UserCode            NVARCHAR(50)  NOT NULL PRIMARY KEY,
                    ComposioApiKey      NVARCHAR(500) NULL,
                    ComposioConnectedApps NVARCHAR(MAX) NULL,
                    RoleContext         NVARCHAR(500) NULL,
                    FocusAreas          NVARCHAR(MAX) NULL,
                    CoachingStyle       NVARCHAR(50)  NULL DEFAULT 'balanced',
                    LastCoachingAt      DATETIME      NULL,
                    TelegramChatId      NVARCHAR(100) NULL,
                    TelegramEnabled     BIT           NOT NULL DEFAULT 0,
                    CreatedAt           DATETIME      NOT NULL DEFAULT GETDATE(),
                    UpdatedAt           DATETIME      NOT NULL DEFAULT GETDATE()
                );

                IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserCoachingSessions')
                CREATE TABLE UserCoachingSessions (
                    SessionId    INT           IDENTITY(1,1) PRIMARY KEY,
                    UserCode     NVARCHAR(50)  NOT NULL,
                    SessionType  NVARCHAR(50)  NOT NULL DEFAULT 'daily',
                    Insight      NVARCHAR(MAX) NOT NULL,
                    DataSnapshot NVARCHAR(MAX) NULL,
                    CreatedAt    DATETIME      NOT NULL DEFAULT GETDATE()
                );

                IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_UserCoachingSessions_UserCode')
                    CREATE INDEX IX_UserCoachingSessions_UserCode
                        ON UserCoachingSessions (UserCode, CreatedAt DESC);");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not ensure UserIntelligence tables");
        }
    }

    public async Task<UserIntelligenceProfile> GetProfileAsync(string userCode)
    {
        try
        {
            var dt = await _db.QueryAsync(
                "SELECT * FROM UserIntelligenceProfiles WHERE UserCode = @Code",
                new Dictionary<string, object?> { ["Code"] = userCode });

            if (dt.Rows.Count == 0)
                return new UserIntelligenceProfile { UserCode = userCode };

            var r = dt.Rows[0];
            return new UserIntelligenceProfile
            {
                UserCode           = userCode,
                ComposioApiKey     = r["ComposioApiKey"]?.ToString(),
                ComposioConnectedApps = r["ComposioConnectedApps"]?.ToString(),
                RoleContext        = r["RoleContext"]?.ToString(),
                FocusAreas         = r["FocusAreas"]?.ToString(),
                CoachingStyle      = r["CoachingStyle"]?.ToString() ?? "balanced",
                LastCoachingAt     = r["LastCoachingAt"] == DBNull.Value ? null : Convert.ToDateTime(r["LastCoachingAt"]),
                TelegramChatId     = r["TelegramChatId"]?.ToString(),
                TelegramEnabled    = r["TelegramEnabled"] != DBNull.Value && Convert.ToBoolean(r["TelegramEnabled"]),
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load intelligence profile for {User}", userCode);
            return new UserIntelligenceProfile { UserCode = userCode };
        }
    }

    public async Task SaveProfileAsync(UserIntelligenceProfile profile)
    {
        try
        {
            await _db.ExecuteNonQueryAsync(@"
                MERGE UserIntelligenceProfiles AS target
                USING (SELECT @Code AS UserCode) AS source ON target.UserCode = source.UserCode
                WHEN MATCHED THEN UPDATE SET
                    ComposioApiKey        = @ComposioKey,
                    ComposioConnectedApps = @ConnectedApps,
                    RoleContext           = @Role,
                    FocusAreas            = @Focus,
                    CoachingStyle         = @Style,
                    TelegramChatId        = @TgChatId,
                    TelegramEnabled       = @TgEnabled,
                    UpdatedAt             = GETDATE()
                WHEN NOT MATCHED THEN INSERT
                    (UserCode, ComposioApiKey, ComposioConnectedApps, RoleContext, FocusAreas,
                     CoachingStyle, TelegramChatId, TelegramEnabled)
                VALUES
                    (@Code, @ComposioKey, @ConnectedApps, @Role, @Focus,
                     @Style, @TgChatId, @TgEnabled);",
                new Dictionary<string, object?>
                {
                    ["Code"]          = profile.UserCode,
                    ["ComposioKey"]   = (object?)profile.ComposioApiKey   ?? DBNull.Value,
                    ["ConnectedApps"] = (object?)profile.ComposioConnectedApps ?? DBNull.Value,
                    ["Role"]          = (object?)profile.RoleContext       ?? DBNull.Value,
                    ["Focus"]         = (object?)profile.FocusAreas        ?? DBNull.Value,
                    ["Style"]         = profile.CoachingStyle ?? "balanced",
                    ["TgChatId"]      = (object?)profile.TelegramChatId   ?? DBNull.Value,
                    ["TgEnabled"]     = profile.TelegramEnabled,
                });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not save intelligence profile for {User}", profile.UserCode);
        }
    }

    public async Task MarkLastCoachingAsync(string userCode)
    {
        try
        {
            await _db.ExecuteNonQueryAsync(
                "UPDATE UserIntelligenceProfiles SET LastCoachingAt = GETDATE(), UpdatedAt = GETDATE() WHERE UserCode = @Code",
                new Dictionary<string, object?> { ["Code"] = userCode });
        }
        catch { /* best effort */ }
    }

    public async Task SaveCoachingSessionAsync(string userCode, string sessionType, string insight, object? dataSnapshot = null)
    {
        try
        {
            var snapshotJson = dataSnapshot is null ? null : JsonSerializer.Serialize(dataSnapshot);
            await _db.InsertAsync(@"
                INSERT INTO UserCoachingSessions (UserCode, SessionType, Insight, DataSnapshot)
                VALUES (@Code, @Type, @Insight, @Snapshot)",
                new Dictionary<string, object?>
                {
                    ["Code"]     = userCode,
                    ["Type"]     = sessionType,
                    ["Insight"]  = insight,
                    ["Snapshot"] = (object?)snapshotJson ?? DBNull.Value,
                });

            await MarkLastCoachingAsync(userCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not save coaching session for {User}", userCode);
        }
    }

    public async Task<List<UserCoachingSession>> GetRecentSessionsAsync(string userCode, int limit = 10)
    {
        try
        {
            var dt = await _db.QueryAsync(@"
                SELECT TOP (@Limit) SessionId, UserCode, SessionType, Insight, DataSnapshot, CreatedAt
                FROM UserCoachingSessions
                WHERE UserCode = @Code
                ORDER BY CreatedAt DESC",
                new Dictionary<string, object?> { ["Code"] = userCode, ["Limit"] = limit });

            return dt.Map(r => new UserCoachingSession
            {
                SessionId   = Convert.ToInt32(r["SessionId"]),
                UserCode    = r["UserCode"]?.ToString() ?? "",
                SessionType = r["SessionType"]?.ToString() ?? "daily",
                Insight     = r["Insight"]?.ToString() ?? "",
                CreatedAt   = Convert.ToDateTime(r["CreatedAt"]),
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load coaching sessions for {User}", userCode);
            return new();
        }
    }

    /// <summary>Resolves the effective Composio API key: user's own key first, then platform key from appsettings.</summary>
    public string? ResolveComposioKey(string userCode, string profileKey, string? platformKey)
    {
        if (!string.IsNullOrWhiteSpace(profileKey)) return profileKey;
        return string.IsNullOrWhiteSpace(platformKey) ? null : platformKey;
    }
}

public class UserIntelligenceProfile
{
    public string  UserCode              { get; set; } = "";
    public string? ComposioApiKey        { get; set; }
    public string? ComposioConnectedApps { get; set; }
    public string? RoleContext           { get; set; }
    public string? FocusAreas            { get; set; }
    public string  CoachingStyle         { get; set; } = "balanced";
    public DateTime? LastCoachingAt      { get; set; }
    public string? TelegramChatId        { get; set; }
    public bool    TelegramEnabled       { get; set; }
}

public class UserCoachingSession
{
    public int     SessionId   { get; set; }
    public string  UserCode    { get; set; } = "";
    public string  SessionType { get; set; } = "daily";
    public string  Insight     { get; set; } = "";
    public DateTime CreatedAt  { get; set; }
}
