using System.Data;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class TimesheetService
{
    private readonly DatabaseService _db;
    private readonly ILogger<TimesheetService> _logger;

    public TimesheetService(DatabaseService db, ILogger<TimesheetService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EnsureTableAsync()
    {
        var sql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Timesheets')
            BEGIN
                CREATE TABLE Timesheets (
                    TimesheetId INT IDENTITY(1,1) PRIMARY KEY,
                    UserId INT NOT NULL,
                    UserName NVARCHAR(100) NOT NULL,
                    WeekStartDate DATE NOT NULL,
                    SubmittedAt DATETIME NULL,
                    SubmittedTo NVARCHAR(100) NULL,
                    Status NVARCHAR(50) NOT NULL DEFAULT 'Draft',
                    ManagerNotes NVARCHAR(500) NULL,
                    CreatedAt DATETIME NOT NULL DEFAULT GETDATE(),
                    ModifiedAt DATETIME NOT NULL DEFAULT GETDATE()
                );
                CREATE INDEX IX_Timesheets_UserId ON Timesheets(UserId);
                CREATE INDEX IX_Timesheets_WeekStartDate ON Timesheets(WeekStartDate DESC);
                CREATE INDEX IX_Timesheets_Status ON Timesheets(Status);
            END;

            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TimesheetEntries')
            BEGIN
                CREATE TABLE TimesheetEntries (
                    TimesheetEntryId INT IDENTITY(1,1) PRIMARY KEY,
                    TimesheetId INT NOT NULL,
                    EntryDate DATE NOT NULL,
                    Hours INT NOT NULL DEFAULT 0,
                    Minutes INT NOT NULL DEFAULT 0,
                    TimeType NVARCHAR(50) NOT NULL DEFAULT 'Billable',
                    CompanyId INT NULL,
                    ProjectId INT NULL,
                    Description NVARCHAR(500) NULL,
                    TaskName NVARCHAR(200) NULL,
                    CONSTRAINT FK_TimesheetEntries_Timesheets FOREIGN KEY (TimesheetId) REFERENCES Timesheets(TimesheetId) ON DELETE CASCADE
                );
                CREATE INDEX IX_TimesheetEntries_TimesheetId ON TimesheetEntries(TimesheetId);
                CREATE INDEX IX_TimesheetEntries_EntryDate ON TimesheetEntries(EntryDate);
            END";
        await _db.ExecuteAsync(sql);
    }

    public async Task<List<TimesheetSummary>> GetTimesheetsAsync(int? userId = null, string? status = null, int page = 1, int pageSize = 20)
    {
        var sql = @"
            SELECT t.TimesheetId, t.UserName, t.WeekStartDate, t.Status, t.SubmittedAt,
                   ISNULL(SUM(te.Hours * 60 + te.Minutes), 0) AS TotalMinutes,
                   ISNULL(SUM(CASE WHEN te.TimeType = 'Billable' THEN te.Hours * 60 + te.Minutes ELSE 0 END), 0) AS BillableMinutes,
                   ISNULL(SUM(CASE WHEN te.TimeType = 'Non-Billable' THEN te.Hours * 60 + te.Minutes ELSE 0 END), 0) AS NonBillableMinutes
            FROM Timesheets t
            LEFT JOIN TimesheetEntries te ON t.TimesheetId = te.TimesheetId
            WHERE 1=1";
        
        var parameters = new Dictionary<string, object?>();
        
        if (userId.HasValue)
        {
            sql += " AND t.UserId = @UserId";
            parameters["UserId"] = userId.Value;
        }
        
        if (!string.IsNullOrEmpty(status))
        {
            sql += " AND t.Status = @Status";
            parameters["Status"] = status;
        }
        
        sql += " GROUP BY t.TimesheetId, t.UserName, t.WeekStartDate, t.Status, t.SubmittedAt";
        sql += " ORDER BY t.WeekStartDate DESC OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";
        
        var offset = (page - 1) * pageSize;
        parameters["Offset"] = offset;
        parameters["PageSize"] = pageSize;
        
        var dt = await _db.QueryAsync(sql, parameters);
        var result = new List<TimesheetSummary>();
        
        foreach (DataRow row in dt.Rows)
        {
            var totalMin = Convert.ToInt32(row["TotalMinutes"]);
            var billableMin = Convert.ToInt32(row["BillableMinutes"]);
            var nonBillableMin = Convert.ToInt32(row["NonBillableMinutes"]);
            
            result.Add(new TimesheetSummary
            {
                TimesheetId = Convert.ToInt32(row["TimesheetId"]),
                UserName = row["UserName"]?.ToString() ?? "",
                WeekStartDate = Convert.ToDateTime(row["WeekStartDate"]),
                Status = row["Status"]?.ToString() ?? "Draft",
                SubmittedAt = row["SubmittedAt"] != DBNull.Value ? Convert.ToDateTime(row["SubmittedAt"]) : null,
                TotalHours = totalMin / 60,
                BillableHours = billableMin / 60,
                NonBillableHours = nonBillableMin / 60
            });
        }
        
        return result;
    }

    public async Task<Timesheet?> GetTimesheetAsync(int timesheetId)
    {
        var sql = @"
            SELECT * FROM Timesheets WHERE TimesheetId = @Id";
        
        var dt = await _db.QueryAsync(sql, new() { ["Id"] = timesheetId });
        if (dt.Rows.Count == 0) return null;
        
        var row = dt.Rows[0];
        var timesheet = new Timesheet
        {
            TimesheetId = Convert.ToInt32(row["TimesheetId"]),
            UserId = Convert.ToInt32(row["UserId"]),
            UserName = row["UserName"]?.ToString() ?? "",
            WeekStartDate = Convert.ToDateTime(row["WeekStartDate"]),
            SubmittedAt = row["SubmittedAt"] != DBNull.Value ? Convert.ToDateTime(row["SubmittedAt"]) : null,
            SubmittedTo = row["SubmittedTo"]?.ToString(),
            Status = row["Status"]?.ToString() ?? "Draft",
            ManagerNotes = row["ManagerNotes"]?.ToString(),
            CreatedAt = Convert.ToDateTime(row["CreatedAt"]),
            ModifiedAt = Convert.ToDateTime(row["ModifiedAt"])
        };
        
        // Load entries
        var entriesDt = await _db.QueryAsync(@"
            SELECT te.*, c.Company AS CompanyName, p.ProjectName 
            FROM TimesheetEntries te
            LEFT JOIN Companies c ON te.CompanyId = c.CompanyId
            LEFT JOIN Projects p ON te.ProjectId = p.ProjectId
            WHERE te.TimesheetId = @TimesheetId
            ORDER BY te.EntryDate",
            new() { ["TimesheetId"] = timesheetId });
        
        foreach (DataRow entryRow in entriesDt.Rows)
        {
            timesheet.Entries.Add(new TimesheetEntry
            {
                TimesheetEntryId = Convert.ToInt32(entryRow["TimesheetEntryId"]),
                TimesheetId = Convert.ToInt32(entryRow["TimesheetId"]),
                EntryDate = Convert.ToDateTime(entryRow["EntryDate"]),
                Hours = Convert.ToInt32(entryRow["Hours"]),
                Minutes = Convert.ToInt32(entryRow["Minutes"]),
                TimeType = entryRow["TimeType"]?.ToString() ?? "Billable",
                CompanyId = entryRow["CompanyId"] != DBNull.Value ? Convert.ToInt32(entryRow["CompanyId"]) : null,
                CompanyName = entryRow["CompanyName"]?.ToString(),
                ProjectId = entryRow["ProjectId"] != DBNull.Value ? Convert.ToInt32(entryRow["ProjectId"]) : null,
                ProjectName = entryRow["ProjectName"]?.ToString(),
                Description = entryRow["Description"]?.ToString(),
                TaskName = entryRow["TaskName"]?.ToString()
            });
        }
        
        return timesheet;
    }

    public async Task<int> SaveTimesheetAsync(Timesheet timesheet)
    {
        if (timesheet.TimesheetId == 0)
        {
            // Create new
            var sql = @"
                INSERT INTO Timesheets (UserId, UserName, WeekStartDate, Status, CreatedAt, ModifiedAt)
                VALUES (@UserId, @UserName, @WeekStartDate, @Status, GETDATE(), GETDATE());
                SELECT CAST(SCOPE_IDENTITY() AS INT)";
            
            timesheet.TimesheetId = await _db.ScalarAsync<int>(sql, new()
            {
                ["UserId"] = timesheet.UserId,
                ["UserName"] = timesheet.UserName,
                ["WeekStartDate"] = timesheet.WeekStartDate,
                ["Status"] = timesheet.Status
            });
        }
        else
        {
            // Update
            await _db.ExecuteAsync(@"
                UPDATE Timesheets 
                SET Status = @Status, ManagerNotes = @ManagerNotes, ModifiedAt = GETDATE()
                WHERE TimesheetId = @Id",
                new()
                {
                    ["Id"] = timesheet.TimesheetId,
                    ["Status"] = timesheet.Status,
                    ["ManagerNotes"] = (object?)timesheet.ManagerNotes ?? DBNull.Value
                });
        }
        
        return timesheet.TimesheetId;
    }

    public async Task SaveEntryAsync(TimesheetEntry entry)
    {
        if (entry.TimesheetEntryId == 0)
        {
            await _db.ExecuteAsync(@"
                INSERT INTO TimesheetEntries (TimesheetId, EntryDate, Hours, Minutes, TimeType, CompanyId, ProjectId, Description, TaskName)
                VALUES (@TimesheetId, @EntryDate, @Hours, @Minutes, @TimeType, @CompanyId, @ProjectId, @Description, @TaskName)",
                new()
                {
                    ["TimesheetId"] = entry.TimesheetId,
                    ["EntryDate"] = entry.EntryDate,
                    ["Hours"] = entry.Hours,
                    ["Minutes"] = entry.Minutes,
                    ["TimeType"] = entry.TimeType,
                    ["CompanyId"] = (object?)entry.CompanyId ?? DBNull.Value,
                    ["ProjectId"] = (object?)entry.ProjectId ?? DBNull.Value,
                    ["Description"] = (object?)entry.Description ?? DBNull.Value,
                    ["TaskName"] = (object?)entry.TaskName ?? DBNull.Value
                });
        }
        else
        {
            await _db.ExecuteAsync(@"
                UPDATE TimesheetEntries
                SET EntryDate = @EntryDate, Hours = @Hours, Minutes = @Minutes, TimeType = @TimeType,
                    CompanyId = @CompanyId, ProjectId = @ProjectId, Description = @Description, TaskName = @TaskName
                WHERE TimesheetEntryId = @Id",
                new()
                {
                    ["Id"] = entry.TimesheetEntryId,
                    ["EntryDate"] = entry.EntryDate,
                    ["Hours"] = entry.Hours,
                    ["Minutes"] = entry.Minutes,
                    ["TimeType"] = entry.TimeType,
                    ["CompanyId"] = (object?)entry.CompanyId ?? DBNull.Value,
                    ["ProjectId"] = (object?)entry.ProjectId ?? DBNull.Value,
                    ["Description"] = (object?)entry.Description ?? DBNull.Value,
                    ["TaskName"] = (object?)entry.TaskName ?? DBNull.Value
                });
        }
    }

    public async Task SubmitTimesheetAsync(int timesheetId, string submittedTo)
    {
        await _db.ExecuteAsync(@"
            UPDATE Timesheets
            SET Status = 'Submitted', SubmittedAt = GETDATE(), SubmittedTo = @SubmittedTo, ModifiedAt = GETDATE()
            WHERE TimesheetId = @Id",
            new() { ["Id"] = timesheetId, ["SubmittedTo"] = submittedTo });
    }

    public async Task DeleteEntryAsync(int entryId)
    {
        await _db.ExecuteAsync("DELETE FROM TimesheetEntries WHERE TimesheetEntryId = @Id",
            new() { ["Id"] = entryId });
    }

    public static DateTime GetMonday(DateTime date)
    {
        var daysToMonday = ((int)date.DayOfWeek - (int)DayOfWeek.Monday + 7) % 7;
        return date.AddDays(-daysToMonday);
    }
}
