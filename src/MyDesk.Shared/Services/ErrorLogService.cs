using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;
using Dapper;

namespace MyDesk.Shared.Services;

public class ErrorLogService
{
    private readonly DatabaseService _db;
    private readonly ILogger<ErrorLogService> _logger;

    public ErrorLogService(DatabaseService db, ILogger<ErrorLogService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EnsureTableAsync()
    {
        var sql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ErrorLogs')
            BEGIN
                CREATE TABLE ErrorLogs (
                    ErrorLogId INT IDENTITY(1,1) PRIMARY KEY,
                    ErrorDate DATETIME NOT NULL DEFAULT GETDATE(),
                    Severity NVARCHAR(20) NOT NULL DEFAULT 'Error',
                    ExceptionType NVARCHAR(255) NULL,
                    Message NVARCHAR(MAX) NOT NULL,
                    StackTrace NVARCHAR(MAX) NULL,
                    InnerException NVARCHAR(MAX) NULL,
                    RequestUrl NVARCHAR(500) NULL,
                    HttpMethod NVARCHAR(10) NULL,
                    UserAgent NVARCHAR(500) NULL,
                    IPAddress NVARCHAR(45) NULL,
                    UserId NVARCHAR(100) NULL,
                    UserName NVARCHAR(100) NULL,
                    CorrelationId NVARCHAR(100) NULL,
                    Source NVARCHAR(100) NULL,
                    IsResolved BIT NOT NULL DEFAULT 0,
                    ResolvedBy NVARCHAR(100) NULL,
                    ResolvedDate DATETIME NULL,
                    ResolutionNotes NVARCHAR(1000) NULL,
                    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                );
                CREATE INDEX IX_ErrorLogs_ErrorDate ON ErrorLogs(ErrorDate DESC);
                CREATE INDEX IX_ErrorLogs_Severity ON ErrorLogs(Severity);
                CREATE INDEX IX_ErrorLogs_IsResolved ON ErrorLogs(IsResolved);
                CREATE INDEX IX_ErrorLogs_UserId ON ErrorLogs(UserId);
                CREATE INDEX IX_ErrorLogs_Source ON ErrorLogs(Source);
            END";
        await _db.ExecuteAsync(sql);
    }

    public async Task<int> LogErrorAsync(ErrorLog errorLog)
    {
        const string sql = @"
            INSERT INTO ErrorLogs (ErrorDate, Severity, ExceptionType, Message, StackTrace, InnerException, 
                                   RequestUrl, HttpMethod, UserAgent, IPAddress, UserId, UserName, CorrelationId, Source)
            VALUES (@ErrorDate, @Severity, @ExceptionType, @Message, @StackTrace, @InnerException, 
                    @RequestUrl, @HttpMethod, @UserAgent, @IPAddress, @UserId, @UserName, @CorrelationId, @Source);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";

        return await _db.ExecuteScalarAsync<int>(sql, new
        {
            errorLog.ErrorDate,
            errorLog.Severity,
            errorLog.ExceptionType,
            errorLog.Message,
            errorLog.StackTrace,
            errorLog.InnerException,
            errorLog.RequestUrl,
            errorLog.HttpMethod,
            errorLog.UserAgent,
            errorLog.IPAddress,
            errorLog.UserId,
            errorLog.UserName,
            errorLog.CorrelationId,
            errorLog.Source
        });
    }

    public async Task<List<ErrorLog>> GetErrorLogsAsync(int page = 1, int pageSize = 50, string? severity = null, bool? isResolved = null)
    {
        var sql = "SELECT * FROM ErrorLogs WHERE 1=1";
        if (!string.IsNullOrEmpty(severity)) sql += " AND Severity = @Severity";
        if (isResolved.HasValue) sql += " AND IsResolved = @IsResolved";
        sql += " ORDER BY ErrorDate DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

        var offset = (page - 1) * pageSize;
        return (await _db.QueryAsync<ErrorLog>(sql, new { Severity = severity, IsResolved = isResolved, Offset = offset, PageSize = pageSize })).ToList();
    }

    public async Task<int> GetErrorCountAsync(string? severity = null, bool? isResolved = null)
    {
        var sql = "SELECT COUNT(*) FROM ErrorLogs WHERE 1=1";
        if (!string.IsNullOrEmpty(severity)) sql += " AND Severity = @Severity";
        if (isResolved.HasValue) sql += " AND IsResolved = @IsResolved";
        return await _db.ExecuteScalarAsync<int>(sql, new { Severity = severity, IsResolved = isResolved });
    }

    public async Task MarkAsResolvedAsync(int errorLogId, string resolvedBy, string? notes = null)
    {
        const string sql = "UPDATE ErrorLogs SET IsResolved = 1, ResolvedBy = @ResolvedBy, ResolvedDate = GETDATE(), ResolutionNotes = @Notes WHERE ErrorLogId = @ErrorLogId";
        await _db.ExecuteObjAsync(sql, new { ErrorLogId = errorLogId, ResolvedBy = resolvedBy, Notes = notes });
    }

    public async Task<int> PurgeOldErrorsAsync(int daysToKeep = 90)
    {
        const string sql = "DELETE FROM ErrorLogs WHERE ErrorDate < DATEADD(day, -@Days, GETDATE()) AND IsResolved = 1";
        return await _db.ExecuteObjAsync(sql, new { Days = daysToKeep });
    }
}
