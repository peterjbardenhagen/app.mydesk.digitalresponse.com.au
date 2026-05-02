using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class StaffWhereaboutsService
{
    private readonly DatabaseService _db;

    public StaffWhereaboutsService(DatabaseService db)
    {
        _db = db;
    }

    public async Task EnsureTableAsync()
    {
        var sql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StaffWhereabouts')
            BEGIN
                CREATE TABLE StaffWhereabouts (
                    WhereaboutId INT IDENTITY(1,1) PRIMARY KEY,
                    UserId INT NOT NULL,
                    UserName NVARCHAR(100) NOT NULL,
                    WeekStartDate DATE NOT NULL,
                    DayOfWeek INT NOT NULL,
                    TimeSlot INT NOT NULL,
                    Status NVARCHAR(50) NOT NULL DEFAULT 'Available',
                    Location NVARCHAR(200) NULL,
                    Notes NVARCHAR(500) NULL,
                    UpdatedAt DATETIME NOT NULL DEFAULT GETDATE()
                );
                CREATE UNIQUE INDEX IX_StaffWhereabouts_UserWeekDayTime ON StaffWhereabouts(UserId, WeekStartDate, DayOfWeek, TimeSlot);
                CREATE INDEX IX_StaffWhereabouts_WeekStartDate ON StaffWhereabouts(WeekStartDate DESC);
            END";
        await _db.ExecuteAsync(sql);
    }

    public async Task SetWhereaboutsAsync(int userId, string userName, DateTime weekStartDate, int dayOfWeek, int timeSlot, string status, string? location, string? notes)
    {
        const string sql = @"
            IF EXISTS (SELECT 1 FROM StaffWhereabouts WHERE UserId = @UserId AND WeekStartDate = @WeekStartDate AND DayOfWeek = @DayOfWeek AND TimeSlot = @TimeSlot)
            BEGIN
                UPDATE StaffWhereabouts 
                SET Status = @Status, Location = @Location, Notes = @Notes, UpdatedAt = GETDATE()
                WHERE UserId = @UserId AND WeekStartDate = @WeekStartDate AND DayOfWeek = @DayOfWeek AND TimeSlot = @TimeSlot;
            END
            ELSE
            BEGIN
                INSERT INTO StaffWhereabouts (UserId, UserName, WeekStartDate, DayOfWeek, TimeSlot, Status, Location, Notes, UpdatedAt)
                VALUES (@UserId, @UserName, @WeekStartDate, @DayOfWeek, @TimeSlot, @Status, @Location, @Notes, GETDATE());
            END";

        await _db.ExecuteObjAsync(sql, new
        {
            UserId = userId,
            UserName = userName,
            WeekStartDate = weekStartDate.Date,
            DayOfWeek = dayOfWeek,
            TimeSlot = timeSlot,
            Status = status,
            Location = location,
            Notes = notes
        });
    }

    public async Task<List<StaffWhereabouts>> GetWeekWhereaboutsAsync(DateTime weekStartDate, int? userId = null)
    {
        var sql = @"
            SELECT * FROM StaffWhereabouts 
            WHERE WeekStartDate = @WeekStartDate";
        
        if (userId.HasValue)
            sql += " AND UserId = @UserId";
        
        sql += " ORDER BY UserId, DayOfWeek, TimeSlot";

        return (await _db.QueryAsync<StaffWhereabouts>(sql, new
        {
            WeekStartDate = weekStartDate.Date,
            UserId = userId
        })).ToList();
    }

    public async Task<List<StaffWhereabouts>> GetUserWhereaboutsAsync(int userId, DateTime weekStartDate)
    {
        const string sql = @"
            SELECT * FROM StaffWhereabouts 
            WHERE UserId = @UserId AND WeekStartDate = @WeekStartDate
            ORDER BY DayOfWeek, TimeSlot";

        return (await _db.QueryAsync<StaffWhereabouts>(sql, new
        {
            UserId = userId,
            WeekStartDate = weekStartDate.Date
        })).ToList();
    }

    public async Task<List<User>> GetAllStaffAsync()
    {
        const string sql = "SELECT UserId, Name, Code, Email FROM Users WHERE Active = 1 ORDER BY Name";
        return (await _db.QueryAsync<User>(sql, new { })).ToList();
    }
}
