using Microsoft.Extensions.Logging;
using Techlight.MyDesk.Shared.Models;

namespace Techlight.MyDesk.Shared.Services;

/// <summary>
/// User admin CRUD. Uses legacy Users table (Code/PW/Name/Active/Deleted).
/// NOTE: passwords are still plain text in the legacy schema. Hashing is a
/// scheduled follow-up, not a blocker for admin functionality.
/// </summary>
public class UserService
{
    private readonly DatabaseService _db;
    private readonly ILogger<UserService> _logger;

    public UserService(DatabaseService db, ILogger<UserService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<User>> GetAllAsync(bool includeInactive = false)
    {
        var sql = @"
            SELECT u.UserId, u.Code, u.Name, u.Email, u.UserTypeId,
                   u.DivisionId, ut.UserType AS UserRole,
                   ISNULL(u.Active,0) AS Active, ISNULL(u.Deleted,0) AS Deleted
            FROM Users u
            LEFT JOIN UserTypes ut ON ut.UserTypeId = u.UserTypeId
            WHERE ISNULL(u.Deleted,0) = 0" +
            (includeInactive ? "" : " AND ISNULL(u.Active,0) = 1") +
            " ORDER BY u.Name";
        var dt = await _db.QueryAsync(sql);
        return dt.Map(r => new User
        {
            UserId    = (int)r["UserId"],
            Code      = r["Code"]?.ToString() ?? "",
            Name      = r["Name"]?.ToString() ?? "",
            Email     = r["Email"]?.ToString(),
            UserTypeId = r["UserTypeId"] == DBNull.Value ? 0 : Convert.ToInt32(r["UserTypeId"]),
            DivisionId = r["DivisionId"] == DBNull.Value ? null : Convert.ToInt32(r["DivisionId"]),
            UserRole   = r["UserRole"]?.ToString(),
            IsAdmin    = r["UserTypeId"] != DBNull.Value && Convert.ToInt32(r["UserTypeId"]) == 1,
            IsManager  = r["UserTypeId"] != DBNull.Value && Convert.ToInt32(r["UserTypeId"]) == 2,
        }).ToList();
    }

    public async Task<User?> GetAsync(int userId)
    {
        var dt = await _db.QueryAsync(
            "SELECT TOP 1 * FROM Users WHERE UserId = @id",
            new() { ["id"] = userId });
        if (dt.Rows.Count == 0) return null;
        var r = dt.Rows[0];
        return new User
        {
            UserId     = (int)r["UserId"],
            Code       = r["Code"]?.ToString() ?? "",
            Name       = r["Name"]?.ToString() ?? "",
            Email      = r.Table.Columns.Contains("Email") ? r["Email"]?.ToString() : null,
            Password   = r.Table.Columns.Contains("PW") ? r["PW"]?.ToString() : null,
            UserTypeId = r["UserTypeId"] == DBNull.Value ? 0 : Convert.ToInt32(r["UserTypeId"]),
            DivisionId = r.Table.Columns.Contains("DivisionId") && r["DivisionId"] != DBNull.Value
                         ? Convert.ToInt32(r["DivisionId"]) : null,
        };
    }

    public async Task<int> CreateAsync(User user, string password)
    {
        var sql = @"INSERT INTO Users (Code, Name, Email, PW, UserTypeId, DivisionId, Active, Deleted)
                    VALUES (@Code, @Name, @Email, @PW, @UserTypeId, @DivisionId, 1, 0);
                    SELECT CAST(SCOPE_IDENTITY() AS int);";
        var id = await _db.ScalarAsync<int>(sql, new()
        {
            ["Code"] = user.Code,
            ["Name"] = user.Name,
            ["Email"] = (object?)user.Email ?? DBNull.Value,
            ["PW"] = password,
            ["UserTypeId"] = user.UserTypeId == 0 ? 3 : user.UserTypeId,
            ["DivisionId"] = (object?)user.DivisionId ?? DBNull.Value,
        });
        _logger.LogInformation("Created user {Code} ({Name}) -> UserId={Id}", user.Code, user.Name, id);
        return id;
    }

    public async Task UpdateAsync(User user, string? newPassword)
    {
        var sql = @"UPDATE Users SET
                        Code = @Code,
                        Name = @Name,
                        Email = @Email,
                        UserTypeId = @UserTypeId,
                        DivisionId = @DivisionId
                        " + (string.IsNullOrEmpty(newPassword) ? "" : ", PW = @PW, DatePasswordChanged = GETDATE()") + @"
                    WHERE UserId = @UserId";
        var p = new Dictionary<string, object?>
        {
            ["UserId"] = user.UserId,
            ["Code"] = user.Code,
            ["Name"] = user.Name,
            ["Email"] = (object?)user.Email ?? DBNull.Value,
            ["UserTypeId"] = user.UserTypeId,
            ["DivisionId"] = (object?)user.DivisionId ?? DBNull.Value,
        };
        if (!string.IsNullOrEmpty(newPassword)) p["PW"] = newPassword;
        await _db.ExecuteAsync(sql, p);
        _logger.LogInformation("Updated user {Id} ({Code})", user.UserId, user.Code);
    }

    public async Task SetActiveAsync(int userId, bool active)
    {
        await _db.ExecuteAsync(
            "UPDATE Users SET Active = @a WHERE UserId = @id",
            new() { ["a"] = active ? 1 : 0, ["id"] = userId });
        _logger.LogInformation("User {Id} Active={Active}", userId, active);
    }

    public async Task SoftDeleteAsync(int userId)
    {
        await _db.ExecuteAsync(
            "UPDATE Users SET Deleted = 1, Active = 0 WHERE UserId = @id",
            new() { ["id"] = userId });
        _logger.LogInformation("Soft-deleted user {Id}", userId);
    }

    public async Task<List<UserType>> GetUserTypesAsync()
    {
        var dt = await _db.QueryAsync("SELECT UserTypeId, UserType FROM UserTypes ORDER BY UserTypeId");
        return dt.Map(r => new UserType
        {
            UserTypeId = (int)r["UserTypeId"],
            TypeName = r["UserType"]?.ToString() ?? ""
        }).ToList();
    }
}

public class UserType
{
    public int UserTypeId { get; set; }
    public string TypeName { get; set; } = string.Empty;
}
