using System.Data;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class LogService
{
    private readonly DatabaseService _db;

    public LogService(DatabaseService db)
    {
        _db = db;
    }

    public async Task EnsureTableAsync()
    {
        var sql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ApplicationLogs')
            BEGIN
                CREATE TABLE ApplicationLogs (
                    LogId INT IDENTITY(1,1) PRIMARY KEY,
                    Timestamp DATETIME NOT NULL DEFAULT GETDATE(),
                    Level NVARCHAR(50) NOT NULL,
                    Source NVARCHAR(255) NULL,
                    Message NVARCHAR(MAX) NOT NULL,
                    Exception NVARCHAR(MAX) NULL
                );
                CREATE INDEX IX_ApplicationLogs_Timestamp ON ApplicationLogs(Timestamp DESC);
            END";
        await _db.ExecuteAsync(sql);
    }

    public async Task<List<LogEntry>> GetLogsAsync(LogType logType, LogLevel logLevel, string? searchTerm)
    {
        var sql = @"
            SELECT TOP 500 LogId, Timestamp, Level, Source, Message, Exception
            FROM ApplicationLogs
            WHERE 1=1";

        var parameters = new Dictionary<string, object?>();

        // Filter by log type
        if (logType == LogType.Error)
        {
            sql += " AND Level = 'Error'";
        }

        // Filter by log level
        if (logLevel == LogLevel.Error)
        {
            sql += " AND Level IN ('Error', 'Critical')";
        }
        else if (logLevel == LogLevel.Warning)
        {
            sql += " AND Level IN ('Error', 'Critical', 'Warning')";
        }
        else if (logLevel == LogLevel.Info)
        {
            sql += " AND Level IN ('Error', 'Critical', 'Warning', 'Info')";
        }

        // Search filter
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            sql += " AND (Message LIKE @search OR Source LIKE @search OR Exception LIKE @search)";
            parameters["search"] = $"%{searchTerm}%";
        }

        sql += " ORDER BY Timestamp DESC";

        var dt = await _db.QueryAsync(sql, parameters);
        return dt.Map(r => new LogEntry
        {
            Timestamp = r["Timestamp"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["Timestamp"]),
            Level = r["Level"]?.ToString() ?? "",
            Source = r["Source"]?.ToString() ?? "",
            Message = r["Message"]?.ToString() ?? "",
            Exception = r["Exception"]?.ToString()
        });
    }

    public async Task<int> PurgeLogsAsync(PurgeOption option)
    {
        var sql = "";
        var parameters = new Dictionary<string, object?>();

        switch (option)
        {
            case PurgeOption.OlderThan30Days:
                sql = "DELETE FROM ApplicationLogs WHERE Timestamp < DATEADD(day, -30, GETDATE())";
                break;
            case PurgeOption.OlderThan7Days:
                sql = "DELETE FROM ApplicationLogs WHERE Timestamp < DATEADD(day, -7, GETDATE())";
                break;
            case PurgeOption.ErrorLogsOnly:
                sql = "DELETE FROM ApplicationLogs WHERE Level IN ('Error', 'Critical')";
                break;
            case PurgeOption.AllLogs:
                sql = "DELETE FROM ApplicationLogs";
                break;
        }

        if (string.IsNullOrEmpty(sql)) return 0;

        return await _db.ExecuteAsync(sql, parameters);
    }
}
