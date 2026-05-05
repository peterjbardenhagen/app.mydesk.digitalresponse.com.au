using System.Data;
using System.Text.Json;
using Cronos;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

/// <summary>
/// CRUD + scheduling service for <see cref="ScheduledTask"/>.
///
/// Persistence: <c>ScheduledTasks</c> table (created idempotently on startup).
/// Execution: Hangfire's recurring-job scheduler. Each task gets a Hangfire job id
/// of <c>"tenant-{TenantId:N}-task-{ScheduledTaskId}"</c>; updates re-register the
/// job, deletes remove it.
///
/// The Hangfire registration itself is wired up in <c>Program.cs</c> via
/// <see cref="IScheduledTaskRegistrar"/> so MyDesk.Shared has no dependency on
/// Hangfire packages.
/// </summary>
public class ScheduledTaskService
{
    private readonly DatabaseService _db;
    private readonly ICurrentTenantAccessor _tenant;
    private readonly ILogger<ScheduledTaskService> _logger;
    private readonly IScheduledTaskRegistrar? _registrar;

    public ScheduledTaskService(
        DatabaseService db,
        ICurrentTenantAccessor tenant,
        ILogger<ScheduledTaskService> logger,
        IScheduledTaskRegistrar? registrar = null)
    {
        _db = db;
        _tenant = tenant;
        _logger = logger;
        _registrar = registrar;
    }

    public async Task EnsureTablesAsync()
    {
        var sql = @"
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ScheduledTasks')
BEGIN
    CREATE TABLE ScheduledTasks (
        ScheduledTaskId  INT IDENTITY(1,1) PRIMARY KEY,
        TenantId         UNIQUEIDENTIFIER NOT NULL,
        Name             NVARCHAR(200) NOT NULL,
        Description      NVARCHAR(1000) NULL,
        ActionType       NVARCHAR(50)  NOT NULL,
        ParametersJson   NVARCHAR(MAX) NOT NULL DEFAULT '{}',
        Recurrence       NVARCHAR(20)  NOT NULL DEFAULT 'Daily',
        HourOfDay        INT NOT NULL DEFAULT 9,
        MinuteOfHour     INT NOT NULL DEFAULT 0,
        DayOfWeek        INT NULL,
        DayOfMonth       INT NULL,
        CronExpression   NVARCHAR(100) NULL,
        TimeZoneId       NVARCHAR(100) NULL,
        IsEnabled        BIT NOT NULL DEFAULT 1,
        LastRunAt        DATETIME NULL,
        NextRunAt        DATETIME NULL,
        LastStatus       NVARCHAR(20) NULL,
        LastResult       NVARCHAR(MAX) NULL,
        CreatedAt        DATETIME NOT NULL DEFAULT GETDATE(),
        CreatedBy        NVARCHAR(100) NULL,
        UpdatedAt        DATETIME NOT NULL DEFAULT GETDATE()
    );
    CREATE INDEX IX_ScheduledTasks_TenantId ON ScheduledTasks(TenantId);
END

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ScheduledTaskRuns')
BEGIN
    CREATE TABLE ScheduledTaskRuns (
        ScheduledTaskRunId INT IDENTITY(1,1) PRIMARY KEY,
        ScheduledTaskId    INT NOT NULL,
        TenantId           UNIQUEIDENTIFIER NOT NULL,
        StartedAt          DATETIME NOT NULL DEFAULT GETDATE(),
        FinishedAt         DATETIME NULL,
        Status             NVARCHAR(20) NOT NULL DEFAULT 'Running',
        Result             NVARCHAR(MAX) NULL,
        ErrorMessage       NVARCHAR(MAX) NULL
    );
    CREATE INDEX IX_ScheduledTaskRuns_TaskId ON ScheduledTaskRuns(ScheduledTaskId, StartedAt DESC);
END";
        await _db.ExecuteNonQueryAsync(sql);
    }

    public async Task<List<ScheduledTask>> ListAsync()
    {
        var dt = await _db.QueryAsync(
            "SELECT * FROM ScheduledTasks WHERE TenantId = @TenantId ORDER BY Name",
            new() { ["TenantId"] = RequireTenant() });
        return dt.Map(Map);
    }

    public async Task<ScheduledTask?> GetAsync(int id)
    {
        var dt = await _db.QueryAsync(
            "SELECT * FROM ScheduledTasks WHERE ScheduledTaskId = @Id AND TenantId = @TenantId",
            new() { ["Id"] = id, ["TenantId"] = RequireTenant() });
        return dt.Map(Map).FirstOrDefault();
    }

    public async Task<int> CreateAsync(ScheduledTask task)
    {
        task.TenantId = RequireTenant();
        ValidateOrThrow(task);
        const string sql = @"
INSERT INTO ScheduledTasks (TenantId, Name, Description, ActionType, ParametersJson,
    Recurrence, HourOfDay, MinuteOfHour, DayOfWeek, DayOfMonth, CronExpression, TimeZoneId,
    IsEnabled, CreatedBy)
OUTPUT INSERTED.ScheduledTaskId
VALUES (@TenantId, @Name, @Description, @ActionType, @ParametersJson,
    @Recurrence, @HourOfDay, @MinuteOfHour, @DayOfWeek, @DayOfMonth, @CronExpression, @TimeZoneId,
    @IsEnabled, @CreatedBy);";
        var id = await _db.ScalarAsync<int>(sql, new()
        {
            ["TenantId"] = task.TenantId,
            ["Name"] = task.Name,
            ["Description"] = (object?)task.Description ?? DBNull.Value,
            ["ActionType"] = task.ActionType,
            ["ParametersJson"] = task.ParametersJson,
            ["Recurrence"] = task.Recurrence,
            ["HourOfDay"] = task.HourOfDay,
            ["MinuteOfHour"] = task.MinuteOfHour,
            ["DayOfWeek"] = (object?)task.DayOfWeek ?? DBNull.Value,
            ["DayOfMonth"] = (object?)task.DayOfMonth ?? DBNull.Value,
            ["CronExpression"] = (object?)task.CronExpression ?? DBNull.Value,
            ["TimeZoneId"] = (object?)task.TimeZoneId ?? DBNull.Value,
            ["IsEnabled"] = task.IsEnabled,
            ["CreatedBy"] = (object?)task.CreatedBy ?? _tenant.UserCode ?? "system",
        });
        task.ScheduledTaskId = id;
        await SyncWithSchedulerAsync(task);
        return id;
    }

    public async Task UpdateAsync(ScheduledTask task)
    {
        task.TenantId = RequireTenant();
        ValidateOrThrow(task);
        const string sql = @"
UPDATE ScheduledTasks
SET Name = @Name, Description = @Description, ActionType = @ActionType,
    ParametersJson = @ParametersJson, Recurrence = @Recurrence,
    HourOfDay = @HourOfDay, MinuteOfHour = @MinuteOfHour,
    DayOfWeek = @DayOfWeek, DayOfMonth = @DayOfMonth,
    CronExpression = @CronExpression, TimeZoneId = @TimeZoneId,
    IsEnabled = @IsEnabled, UpdatedAt = GETDATE()
WHERE ScheduledTaskId = @Id AND TenantId = @TenantId;";
        await _db.ExecuteNonQueryAsync(sql, new()
        {
            ["Id"] = task.ScheduledTaskId,
            ["TenantId"] = task.TenantId,
            ["Name"] = task.Name,
            ["Description"] = (object?)task.Description ?? DBNull.Value,
            ["ActionType"] = task.ActionType,
            ["ParametersJson"] = task.ParametersJson,
            ["Recurrence"] = task.Recurrence,
            ["HourOfDay"] = task.HourOfDay,
            ["MinuteOfHour"] = task.MinuteOfHour,
            ["DayOfWeek"] = (object?)task.DayOfWeek ?? DBNull.Value,
            ["DayOfMonth"] = (object?)task.DayOfMonth ?? DBNull.Value,
            ["CronExpression"] = (object?)task.CronExpression ?? DBNull.Value,
            ["TimeZoneId"] = (object?)task.TimeZoneId ?? DBNull.Value,
            ["IsEnabled"] = task.IsEnabled,
        });
        await SyncWithSchedulerAsync(task);
    }

    public async Task DeleteAsync(int id)
    {
        var task = await GetAsync(id);
        if (task == null) return;
        await _db.ExecuteNonQueryAsync(
            "DELETE FROM ScheduledTasks WHERE ScheduledTaskId = @Id AND TenantId = @TenantId",
            new() { ["Id"] = id, ["TenantId"] = RequireTenant() });
        if (_registrar != null) await _registrar.RemoveAsync(task);
    }

    public async Task SetEnabledAsync(int id, bool enabled)
    {
        await _db.ExecuteNonQueryAsync(
            "UPDATE ScheduledTasks SET IsEnabled = @E, UpdatedAt = GETDATE() WHERE ScheduledTaskId = @Id AND TenantId = @TenantId",
            new() { ["E"] = enabled, ["Id"] = id, ["TenantId"] = RequireTenant() });
        var task = await GetAsync(id);
        if (task != null) await SyncWithSchedulerAsync(task);
    }

    public async Task RecordRunAsync(int taskId, Guid tenantId, string status, string? result, string? error)
    {
        var ok = string.Equals(status, "Success", StringComparison.OrdinalIgnoreCase);
        var truncated = result is null ? null : result.Length > 4000 ? result.Substring(0, 4000) + "…" : result;
        await _db.ExecuteNonQueryAsync(@"
UPDATE ScheduledTasks SET LastRunAt = GETDATE(), LastStatus = @Status, LastResult = @Result
WHERE ScheduledTaskId = @Id AND TenantId = @TenantId;

INSERT INTO ScheduledTaskRuns (ScheduledTaskId, TenantId, StartedAt, FinishedAt, Status, Result, ErrorMessage)
VALUES (@Id, @TenantId, GETDATE(), GETDATE(), @Status, @Result, @Error);",
            new()
            {
                ["Id"] = taskId,
                ["TenantId"] = tenantId,
                ["Status"] = status,
                ["Result"] = (object?)truncated ?? DBNull.Value,
                ["Error"]  = (object?)error ?? DBNull.Value,
            });
    }

    public async Task<List<ScheduledTaskRun>> RecentRunsAsync(int taskId, int limit = 20)
    {
        var dt = await _db.QueryAsync(
            "SELECT TOP (" + limit + ") * FROM ScheduledTaskRuns WHERE ScheduledTaskId = @Id AND TenantId = @TenantId ORDER BY StartedAt DESC",
            new() { ["Id"] = taskId, ["TenantId"] = RequireTenant() });
        return dt.Map(r => new ScheduledTaskRun
        {
            ScheduledTaskRunId = Convert.ToInt32(r["ScheduledTaskRunId"]),
            ScheduledTaskId = Convert.ToInt32(r["ScheduledTaskId"]),
            TenantId = Guid.Parse(r["TenantId"].ToString()!),
            StartedAt = Convert.ToDateTime(r["StartedAt"]),
            FinishedAt = r["FinishedAt"] as DateTime?,
            Status = r["Status"]?.ToString() ?? "",
            Result = r["Result"] as string,
            ErrorMessage = r["ErrorMessage"] as string,
        });
    }

    /// <summary>Build a 5-field cron expression from the task's Recurrence + hour/minute/dow/dom.</summary>
    public static string BuildCron(ScheduledTask t)
    {
        var rec = Enum.TryParse<ScheduleRecurrence>(t.Recurrence, true, out var r) ? r : ScheduleRecurrence.Daily;
        return rec switch
        {
            ScheduleRecurrence.Hourly  => $"{t.MinuteOfHour} * * * *",
            ScheduleRecurrence.Daily   => $"{t.MinuteOfHour} {t.HourOfDay} * * *",
            ScheduleRecurrence.Weekly  => $"{t.MinuteOfHour} {t.HourOfDay} * * {t.DayOfWeek ?? 1}",
            ScheduleRecurrence.Monthly => $"{t.MinuteOfHour} {t.HourOfDay} {t.DayOfMonth ?? 1} * *",
            ScheduleRecurrence.Cron    => string.IsNullOrWhiteSpace(t.CronExpression) ? "0 9 * * *" : t.CronExpression!,
            _                          => "0 9 * * *",
        };
    }

    /// <summary>Throws ArgumentException if the task is misconfigured.</summary>
    public static void ValidateOrThrow(ScheduledTask t)
    {
        if (string.IsNullOrWhiteSpace(t.Name))
            throw new ArgumentException("Name is required.");
        if (t.HourOfDay < 0 || t.HourOfDay > 23)
            throw new ArgumentException("HourOfDay must be 0..23.");
        if (t.MinuteOfHour < 0 || t.MinuteOfHour > 59)
            throw new ArgumentException("MinuteOfHour must be 0..59.");

        var cron = BuildCron(t);
        try { CronExpression.Parse(cron); }
        catch (Exception ex) { throw new ArgumentException($"Invalid schedule (cron='{cron}'): {ex.Message}"); }
    }

    private async Task SyncWithSchedulerAsync(ScheduledTask task)
    {
        if (_registrar == null)
        {
            _logger.LogDebug("No IScheduledTaskRegistrar registered — skipping Hangfire sync");
            return;
        }
        try
        {
            if (task.IsEnabled) await _registrar.AddOrUpdateAsync(task);
            else await _registrar.RemoveAsync(task);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to sync task {Id} with scheduler", task.ScheduledTaskId);
        }
    }

    private Guid RequireTenant() =>
        _tenant.TenantId ?? throw new InvalidOperationException("ScheduledTaskService requires an authenticated tenant.");

    private static ScheduledTask Map(DataRow r) => new()
    {
        ScheduledTaskId = Convert.ToInt32(r["ScheduledTaskId"]),
        TenantId = Guid.Parse(r["TenantId"].ToString()!),
        Name = r["Name"]?.ToString() ?? "",
        Description = r["Description"] as string,
        ActionType = r["ActionType"]?.ToString() ?? "AskAi",
        ParametersJson = r["ParametersJson"]?.ToString() ?? "{}",
        Recurrence = r["Recurrence"]?.ToString() ?? "Daily",
        HourOfDay = r["HourOfDay"] != DBNull.Value ? Convert.ToInt32(r["HourOfDay"]) : 9,
        MinuteOfHour = r["MinuteOfHour"] != DBNull.Value ? Convert.ToInt32(r["MinuteOfHour"]) : 0,
        DayOfWeek = r["DayOfWeek"] as int?,
        DayOfMonth = r["DayOfMonth"] as int?,
        CronExpression = r["CronExpression"] as string,
        TimeZoneId = r["TimeZoneId"] as string,
        IsEnabled = r["IsEnabled"] != DBNull.Value && Convert.ToBoolean(r["IsEnabled"]),
        LastRunAt = r["LastRunAt"] as DateTime?,
        NextRunAt = r["NextRunAt"] as DateTime?,
        LastStatus = r["LastStatus"] as string,
        LastResult = r["LastResult"] as string,
        CreatedAt = Convert.ToDateTime(r["CreatedAt"]),
        CreatedBy = r["CreatedBy"] as string,
        UpdatedAt = Convert.ToDateTime(r["UpdatedAt"]),
    };
}

/// <summary>
/// Adapter implemented in MyDesk.Web (where Hangfire lives) — keeps Shared free
/// of Hangfire dependencies.
/// </summary>
public interface IScheduledTaskRegistrar
{
    Task AddOrUpdateAsync(ScheduledTask task);
    Task RemoveAsync(ScheduledTask task);
}
