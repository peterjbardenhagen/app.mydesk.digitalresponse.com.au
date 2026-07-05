using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

/// <summary>
/// User admin CRUD. Uses legacy Users table (Code/PW/Name/Active/Deleted).
/// Passwords are now hashed using BCrypt for security.
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

    /// <summary>
    /// Hash a password using BCrypt
    /// </summary>
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
    }

    /// <summary>
    /// Verify a password against a hash
    /// </summary>
    public bool VerifyPassword(string password, string hash)
    {
        // Check if the password is plain text (legacy) or hashed
        if (!hash.StartsWith("$2a$") && !hash.StartsWith("$2b$"))
        {
            // Legacy plain text password - compare directly
            return string.Equals(password, hash, StringComparison.Ordinal);
        }
        // Hashed password - use BCrypt verification
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    /// <summary>
    /// Verify login credentials (code or name + password).
    /// Accepts Users.Code OR Users.Name (case-insensitive) with Users.PW.
    /// </summary>
    public async Task<User?> VerifyLoginAsync(string login, string password)
    {
        var loginTrimmed = login.Trim();

        // Try to get user by Code first (case-insensitive via UPPER)
        var dt1 = await _db.QueryAsync(
            @"SELECT TOP 1 u.UserId, u.Code, u.Name, u.Email, u.UserTypeId, u.DivisionId,
                     ISNULL(u.Active,0) AS Active, ISNULL(u.Deleted,0) AS Deleted, ISNULL(u.UserRoleId, 2) AS UserRoleId
              FROM Users u
              WHERE UPPER(u.Code) = UPPER(@Login) AND ISNULL(u.Deleted,0) = 0",
            new() { ["Login"] = loginTrimmed });

        User? user = null;
        if (dt1.Rows.Count > 0)
        {
            var r = dt1.Rows[0];
            user = new User
            {
                UserId     = (int)r["UserId"],
                Code       = r["Code"]?.ToString() ?? "",
                Name       = r["Name"]?.ToString() ?? "",
                Email      = r["Email"]?.ToString(),
                UserTypeId = r["UserTypeId"] == DBNull.Value ? 0 : Convert.ToInt32(r["UserTypeId"]),
                DivisionId = r["DivisionId"] as int?,
                Active     = Convert.ToInt32(r["Active"]) == 1,
                Role       = (RoleType)(r["UserRoleId"] == DBNull.Value ? 2 : Convert.ToInt32(r["UserRoleId"])),
            };
        }

        // If not found by code, try by Name (case-insensitive)
        if (user == null)
        {
            var dt2 = await _db.QueryAsync(
                @"SELECT TOP 1 u.UserId, u.Code, u.Name, u.Email, u.UserTypeId, u.DivisionId,
                         ISNULL(u.Active,0) AS Active, ISNULL(u.Deleted,0) AS Deleted, ISNULL(u.UserRoleId, 2) AS UserRoleId
                  FROM Users u
                  WHERE UPPER(u.Name) = UPPER(@Login) AND ISNULL(u.Deleted,0) = 0",
                new() { ["Login"] = loginTrimmed });

            if (dt2.Rows.Count > 0)
            {
                var r = dt2.Rows[0];
                user = new User
                {
                    UserId     = (int)r["UserId"],
                    Code       = r["Code"]?.ToString() ?? "",
                    Name       = r["Name"]?.ToString() ?? "",
                    Email      = r["Email"]?.ToString(),
                    UserTypeId = r["UserTypeId"] == DBNull.Value ? 0 : Convert.ToInt32(r["UserTypeId"]),
                    DivisionId = r["DivisionId"] as int?,
                    Active     = Convert.ToInt32(r["Active"]) == 1,
                    Role       = (RoleType)(r["UserRoleId"] == DBNull.Value ? 2 : Convert.ToInt32(r["UserRoleId"])),
                };
            }
        }

        if (user == null) return null;

        // Check account is active and not deleted
        if (!user.Active) return null;

        // Get stored password
        var dtPw = await _db.QueryAsync(
            "SELECT PW, ISNULL(Deleted, 0) AS Deleted FROM Users WHERE UserId = @UserId",
            new() { ["UserId"] = user.UserId });

        if (dtPw.Rows.Count == 0) return null;

        var storedPassword = dtPw.Rows[0]["PW"]?.ToString() ?? "";
        var deleted        = Convert.ToInt32(dtPw.Rows[0]["Deleted"]) == 1;

        if (deleted) return null;

        // Verify password (supports both legacy plain-text and BCrypt hashed)
        if (!VerifyPassword(password, storedPassword))
            return null;

        return user;
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
            ["PW"] = HashPassword(password),
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
        if (!string.IsNullOrEmpty(newPassword)) p["PW"] = HashPassword(newPassword);
        await _db.ExecuteNonQueryAsync(sql, p);
        _logger.LogInformation("Updated user {Id} ({Code})", user.UserId, user.Code);
    }

    public async Task SetActiveAsync(int userId, bool active)
    {
        await _db.ExecuteNonQueryAsync(
            "UPDATE Users SET Active = @a WHERE UserId = @id",
            new() { ["a"] = active ? 1 : 0, ["id"] = userId });
        _logger.LogInformation("User {Id} Active={Active}", userId, active);
    }

    public async Task SoftDeleteAsync(int userId)
    {
        await _db.ExecuteNonQueryAsync(
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

    public async Task<User?> GetByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var dt = await _db.QueryAsync(
            @"SELECT TOP 1 u.*, ISNULL(u.UserRoleId, 2) AS UserRoleId
              FROM Users u
              WHERE u.Email = @Email AND ISNULL(u.Deleted,0) = 0 AND ISNULL(u.Active,0) = 1",
            new() { ["Email"] = email.Trim() });
        if (dt.Rows.Count == 0) return null;
        var r = dt.Rows[0];
        return new User
        {
            UserId     = (int)r["UserId"],
            Code       = r["Code"]?.ToString() ?? "",
            Name       = r["Name"]?.ToString() ?? "",
            Email      = r["Email"]?.ToString(),
            UserTypeId = r["UserTypeId"] == DBNull.Value ? 0 : Convert.ToInt32(r["UserTypeId"]),
            Role       = (RoleType)(r["UserRoleId"] == DBNull.Value ? 2 : Convert.ToInt32(r["UserRoleId"])),
            Active     = true,
        };
    }

    /// <summary>
    /// Resets a user's password by their email address.
    /// Returns the user's Code if found and reset, null otherwise.
    /// </summary>
    public async Task<(User? User, string NewPassword)?> ResetPasswordByEmailAsync(string email)
    {
        var user = await GetByEmailAsync(email);
        if (user == null) return null;

        // Generate a readable temporary password
        var adjectives = new[] { "Quick", "Bright", "Swift", "Clear", "Sharp" };
        var nouns      = new[] { "Desk", "Light", "Cloud", "Spark", "Wave" };
        var rng        = new Random();
        var newPassword = $"{adjectives[rng.Next(adjectives.Length)]}{nouns[rng.Next(nouns.Length)]}{rng.Next(100, 999)}!";

        await _db.ExecuteNonQueryAsync(
            "UPDATE Users SET PW = @PW, DatePasswordChanged = GETDATE() WHERE UserId = @UserId",
            new() { ["PW"] = HashPassword(newPassword), ["UserId"] = user.UserId });

        _logger.LogInformation("Password reset via forgot-password for user {Code} ({Email})", user.Code, email);
        return (user, newPassword);
    }

    public async Task<bool> ChangePasswordAsync(string userCode, string currentPassword, string newPassword)
    {
        // Verify current password matches
        var dt = await _db.QueryAsync(
            "SELECT PW FROM Users WHERE Code = @Code AND ISNULL(Deleted,0) = 0",
            new() { ["Code"] = userCode });
        if (dt.Rows.Count == 0) return false;

        var storedPw = dt.Rows[0]["PW"]?.ToString() ?? "";
        if (!VerifyPassword(currentPassword, storedPw))
            return false;

        await _db.ExecuteNonQueryAsync(
            "UPDATE Users SET PW = @PW, DatePasswordChanged = GETDATE() WHERE Code = @Code",
            new() { ["PW"] = HashPassword(newPassword), ["Code"] = userCode });

        _logger.LogInformation("Password changed for user {Code}", userCode);
        return true;
    }

    public async Task ResetPasswordAsync(string userCode, string newPassword)
    {
        await _db.ExecuteNonQueryAsync(
            "UPDATE Users SET PW = @PW, DatePasswordChanged = GETDATE() WHERE Code = @Code",
            new() { ["PW"] = HashPassword(newPassword), ["Code"] = userCode });
        _logger.LogInformation("Password reset for user {Code}", userCode);
    }

    public async Task<User?> GetUserByEmailOrCodeAsync(string emailOrCode)
    {
        var dt = await _db.QueryAsync(
            "SELECT TOP 1 UserId, Code, Name, Email, IsAdmin, IsManager, DivisionId, UserRole, UserTypeId, Active FROM Users WHERE (Email = @Value OR Code = @Value) AND ISNULL(Deleted,0) = 0",
            new() { ["Value"] = emailOrCode });
        if (dt.Rows.Count == 0) return null;
        var row = dt.Rows[0];
        return new User
        {
            UserId = (int)row["UserId"],
            Code = row["Code"]?.ToString() ?? "",
            Name = row["Name"]?.ToString() ?? "",
            Email = row["Email"]?.ToString(),
            IsAdmin = row["IsAdmin"] != DBNull.Value && (bool)row["IsAdmin"],
            IsManager = row["IsManager"] != DBNull.Value && (bool)row["IsManager"],
            DivisionId = row["DivisionId"] != DBNull.Value ? (int)row["DivisionId"] : null,
            UserRole = row["UserRole"]?.ToString(),
            UserTypeId = row["UserTypeId"] != DBNull.Value ? (int)row["UserTypeId"] : 0,
            Active = row["Active"] != DBNull.Value ? (bool)row["Active"] : true
        };
    }

    public async Task CreatePasswordResetTokenAsync(int userId, string tokenHash, DateTime expiresAt)
    {
        await _db.ExecuteNonQueryAsync(
            "INSERT INTO PasswordResetTokens (UserId, Token, ExpiresAt, IsUsed, CreatedAt) VALUES (@UserId, @Token, @ExpiresAt, 0, GETUTCDATE())",
            new() { ["UserId"] = userId, ["Token"] = tokenHash, ["ExpiresAt"] = expiresAt });
    }

    public async Task<bool> ResetPasswordByTokenAsync(string tokenHash, string newPassword)
    {
        var dt = await _db.QueryAsync(
            "SELECT UserId FROM PasswordResetTokens WHERE Token = @Token AND ExpiresAt > GETUTCDATE() AND IsUsed = 0",
            new() { ["Token"] = tokenHash });
        if (dt.Rows.Count == 0) return false;

        var userId = (int)dt.Rows[0]["UserId"];
        await _db.ExecuteNonQueryAsync(
            "UPDATE PasswordResetTokens SET IsUsed = 1 WHERE Token = @Token",
            new() { ["Token"] = tokenHash });
        await _db.ExecuteNonQueryAsync(
            "UPDATE Users SET PW = @PW, DatePasswordChanged = GETDATE() WHERE UserId = @UserId",
            new() { ["PW"] = HashPassword(newPassword), ["UserId"] = userId });
        return true;
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
