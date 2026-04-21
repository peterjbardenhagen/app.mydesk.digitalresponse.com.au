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

    public async Task<List<LogEntry>> GetLogsAsync(LogType logType, LogLevel logLevel, string? searchTerm)
    {
        var sql = @"
            SELECT LogId, Timestamp, Level, Source, Message, Exception
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

        sql += " ORDER BY Timestamp DESC LIMIT 500";

        var dt = await _db.QueryAsync(sql, parameters);
        return dt.Rows.Cast<DataRow>().Select(r => new LogEntry
        {
            Timestamp = r["Timestamp"] == DBNull.Value ? DateTime.MinValue : Convert.ToDateTime(r["Timestamp"]),
            Level = r["Level"]?.ToString() ?? "",
            Source = r["Source"]?.ToString() ?? "",
            Message = r["Message"]?.ToString() ?? "",
            Exception = r["Exception"]?.ToString()
        }).ToList();
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

        return await _db.ExecuteAsync(sql, parameters);
    }
}
