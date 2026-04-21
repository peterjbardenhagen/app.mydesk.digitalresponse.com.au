using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

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
                   ISNULL(u.Active,0) AS Active, ISNULL(u.Deleted,0) AS Deleted,
                   ISNULL(u.UserRoleId, 2) AS UserRoleId
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
            Active     = r["Active"] != DBNull.Value && Convert.ToBoolean(r["Active"]),
            Role       = (RoleType)(r["UserRoleId"] == DBNull.Value ? 2 : Convert.ToInt32(r["UserRoleId"])),
        }).ToList();
    }

    public async Task<User?> GetAsync(int userId)
    {
        var dt = await _db.QueryAsync(
            "SELECT TOP 1 *, ISNULL(UserRoleId, 2) AS UserRoleId FROM Users WHERE UserId = @id",
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
            Role       = (RoleType)(r["UserRoleId"] == DBNull.Value ? 2 : Convert.ToInt32(r["UserRoleId"])),
            Active     = r.Table.Columns.Contains("Active") && r["Active"] != DBNull.Value && Convert.ToBoolean(r["Active"]),
        };
    }

    public async Task<int> CreateAsync(User user, string password)
    {
        var sql = @"INSERT INTO Users (Code, Name, Email, PW, UserTypeId, DivisionId, Active, Deleted, UserRoleId)
                    VALUES (@Code, @Name, @Email, @PW, @UserTypeId, @DivisionId, 1, 0, @UserRoleId);
                    SELECT CAST(SCOPE_IDENTITY() AS int);";
        var id = await _db.ScalarAsync<int>(sql, new()
        {
            ["Code"] = user.Code,
            ["Name"] = user.Name,
            ["Email"] = (object?)user.Email ?? DBNull.Value,
            ["PW"] = password,
            ["UserTypeId"] = user.UserTypeId == 0 ? 3 : user.UserTypeId,
            ["DivisionId"] = (object?)user.DivisionId ?? DBNull.Value,
            ["UserRoleId"] = (int)user.Role,
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
                        DivisionId = @DivisionId,
                        UserRoleId = @UserRoleId
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
            ["UserRoleId"] = (int)user.Role,
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

    public async Task<User?> GetByCodeAsync(string code)
    {
        var dt = await _db.QueryAsync(
            @"SELECT TOP 1 u.*, ut.UserType AS UserRole
              FROM Users u
              LEFT JOIN UserTypes ut ON ut.UserTypeId = u.UserTypeId
              WHERE u.Code = @Code AND ISNULL(u.Deleted,0) = 0",
            new() { ["Code"] = code });
        if (dt.Rows.Count == 0) return null;
        var r = dt.Rows[0];
        return new User
        {
            UserId     = (int)r["UserId"],
            Code       = r["Code"]?.ToString() ?? "",
            Name       = r["Name"]?.ToString() ?? "",
            Email      = r.Table.Columns.Contains("Email") ? r["Email"]?.ToString() : null,
            Phone      = r.Table.Columns.Contains("Phone") ? r["Phone"]?.ToString() : null,
            Mobile     = r.Table.Columns.Contains("Mobile") ? r["Mobile"]?.ToString() : null,
            UserTypeId = r["UserTypeId"] == DBNull.Value ? 0 : Convert.ToInt32(r["UserTypeId"]),
            DivisionId = r.Table.Columns.Contains("DivisionId") && r["DivisionId"] != DBNull.Value
                         ? Convert.ToInt32(r["DivisionId"]) : null,
            UserRole   = r.Table.Columns.Contains("UserRole") ? r["UserRole"]?.ToString() : null,
        };
    }

    public async Task<bool> ChangePasswordAsync(string userCode, string currentPassword, string newPassword)
    {
        // Verify current password matches
        var dt = await _db.QueryAsync(
            "SELECT PW FROM Users WHERE Code = @Code AND ISNULL(Deleted,0) = 0",
            new() { ["Code"] = userCode });
        if (dt.Rows.Count == 0) return false;

        var storedPw = dt.Rows[0]["PW"]?.ToString() ?? "";
        if (!string.Equals(storedPw, currentPassword, StringComparison.Ordinal))
            return false;

        await _db.ExecuteAsync(
            "UPDATE Users SET PW = @PW, DatePasswordChanged = GETDATE() WHERE Code = @Code",
            new() { ["PW"] = newPassword, ["Code"] = userCode });

        _logger.LogInformation("Password changed for user {Code}", userCode);
        return true;
    }

    public async Task ResetPasswordAsync(string userCode, string newPassword)
    {
        await _db.ExecuteAsync(
            "UPDATE Users SET PW = @PW, DatePasswordChanged = GETDATE() WHERE Code = @Code",
            new() { ["PW"] = newPassword, ["Code"] = userCode });
        _logger.LogInformation("Password reset for user {Code}", userCode);
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
