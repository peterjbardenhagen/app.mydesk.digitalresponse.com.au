using System.Data;
using Techlight.MyDesk.MCP.Models;

namespace Techlight.MyDesk.MCP.Services;

public class UserService
{
    private readonly DatabaseService _db;
    private readonly ILogger<UserService> _logger;

    public UserService(DatabaseService db, ILogger<UserService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<User?> GetUserByCodeAsync(string code)
    {
        var sql = @"
            SELECT u.UserId, u.Code, u.Name, u.Email, u.Admin, u.Manager, 
                   u.DivisionId, ur.UserRole
            FROM Users u
            LEFT JOIN UserRoles ur ON u.UserRoleId = ur.UserRoleId
            WHERE u.Code = @Code
            AND u.Deleted = 0";

        var dt = await _db.ExecuteQueryAsync(sql, new Dictionary<string, object>
        {
            ["Code"] = code
        });

        if (dt.Rows.Count == 0) return null;

        return MapUserFromDataRow(dt.Rows[0]);
    }

    public async Task<User?> GetUserByCredentialsAsync(string code, string password)
    {
        // In a real implementation, you'd hash and verify the password
        // This is a simplified version - you'd want to use proper password hashing
        var sql = @"
            SELECT u.UserId, u.Code, u.Name, u.Email, u.Admin, u.Manager, 
                   u.DivisionId, ur.UserRole
            FROM Users u
            LEFT JOIN UserRoles ur ON u.UserRoleId = ur.UserRoleId
            WHERE u.Code = @Code
            AND u.Password = @Password
            AND u.Deleted = 0";

        var dt = await _db.ExecuteQueryAsync(sql, new Dictionary<string, object>
        {
            ["Code"] = code,
            ["Password"] = password // Should be hashed in production
        });

        if (dt.Rows.Count == 0) return null;

        return MapUserFromDataRow(dt.Rows[0]);
    }

    public async Task<List<User>> GetUsersAsync(bool activeOnly = true, int? divisionId = null, int? limit = 100)
    {
        var sql = @"
            SELECT TOP (@Limit) u.UserId, u.Code, u.Name, u.Email, u.Admin, u.Manager, 
                   u.DivisionId, ur.UserRole
            FROM Users u
            LEFT JOIN UserRoles ur ON u.UserRoleId = ur.UserRoleId
            WHERE 1=1";

        var parameters = new Dictionary<string, object>
        {
            ["Limit"] = limit ?? 100
        };

        if (activeOnly)
        {
            sql += " AND u.Deleted = 0";
        }

        if (divisionId.HasValue)
        {
            sql += " AND u.DivisionId = @DivisionId";
            parameters["DivisionId"] = divisionId.Value;
        }

        sql += " ORDER BY u.Name";

        var dt = await _db.ExecuteQueryAsync(sql, parameters);
        return dt.AsEnumerable().Select(MapUserFromDataRow).ToList();
    }

    public async Task<List<int>> GetUserAccessibleDivisionsAsync(string userCode)
    {
        var sql = @"
            SELECT DivisionId 
            FROM UserDivisionAccess 
            WHERE Code = @Code AND CanAccess = 1";

        var dt = await _db.ExecuteQueryAsync(sql, new Dictionary<string, object>
        {
            ["Code"] = userCode
        });

        var divisions = dt.AsEnumerable()
            .Select(r => Convert.ToInt32(r["DivisionId"]))
            .ToList();

        // If no specific access defined, return user's default division
        if (!divisions.Any())
        {
            var user = await GetUserByCodeAsync(userCode);
            if (user?.DivisionId.HasValue == true)
            {
                divisions.Add(user.DivisionId.Value);
            }
        }

        return divisions;
    }

    public async Task<bool> ValidateApiKeyAsync(string apiKey)
    {
        var sql = "SELECT COUNT(*) FROM ApiKeys WHERE ApiKey = @ApiKey AND IsActive = 1 AND (ExpiresAt IS NULL OR ExpiresAt > GETDATE())";
        
        var count = await _db.ExecuteScalarAsync<int>(sql, new Dictionary<string, object>
        {
            ["ApiKey"] = apiKey
        });

        return count > 0;
    }

    public async Task<User?> GetUserFromApiKeyAsync(string apiKey)
    {
        var sql = @"
            SELECT u.UserId, u.Code, u.Name, u.Email, u.Admin, u.Manager, 
                   u.DivisionId, ur.UserRole
            FROM ApiKeys ak
            INNER JOIN Users u ON ak.UserId = u.UserId
            LEFT JOIN UserRoles ur ON u.UserRoleId = ur.UserRoleId
            WHERE ak.ApiKey = @ApiKey
            AND ak.IsActive = 1
            AND (ak.ExpiresAt IS NULL OR ak.ExpiresAt > GETDATE())";

        var dt = await _db.ExecuteQueryAsync(sql, new Dictionary<string, object>
        {
            ["ApiKey"] = apiKey
        });

        if (dt.Rows.Count == 0) return null;

        return MapUserFromDataRow(dt.Rows[0]);
    }

    private User MapUserFromDataRow(DataRow row)
    {
        return new User
        {
            UserId = Convert.ToInt32(row["UserId"]),
            Code = row["Code"].ToString()!,
            Name = row["Name"].ToString()!,
            Email = row["Email"]?.ToString(),
            IsAdmin = Convert.ToBoolean(row["Admin"]),
            IsManager = Convert.ToBoolean(row["Manager"]),
            DivisionId = row["DivisionId"] == DBNull.Value ? null : Convert.ToInt32(row["DivisionId"]),
            UserRole = row["UserRole"]?.ToString()
        };
    }
}
