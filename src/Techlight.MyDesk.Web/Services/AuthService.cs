using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Techlight.MyDesk.Shared.Models;
using Techlight.MyDesk.Shared.Services;

namespace Techlight.MyDesk.Web.Services;

public class AuthService
{
    private readonly DatabaseService _db;
    private readonly ILogger<AuthService> _logger;

    public AuthService(DatabaseService db, ILogger<AuthService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<User?> ValidateLoginAsync(string login, string password)
    {
        try
        {
            // Match against Name OR Code (case-insensitive via SQL default collation).
            // PW is the plaintext password column in the legacy schema.
            // Only allow Active, non-deleted users.
            // Name/Code: case-INsensitive (CI). Password: case-SENSITIVE (CS).
            var dt = await _db.QueryAsync(
                @"SELECT TOP 1 * FROM Users
                  WHERE (Name COLLATE Latin1_General_CI_AS = @Login
                         OR Code COLLATE Latin1_General_CI_AS = @Login)
                    AND PW COLLATE Latin1_General_CS_AS = @Password
                    AND ISNULL(Active, 0) = 1
                    AND ISNULL(Deleted, 0) = 0",
                new() { ["Login"] = login, ["Password"] = password });

            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            var userTypeId = row.Table.Columns.Contains("UserTypeId") && row["UserTypeId"] != DBNull.Value
                ? Convert.ToInt32(row["UserTypeId"]) : 0;

            return new User
            {
                UserId = Convert.ToInt32(row["UserId"]),
                Code = row["Code"]?.ToString() ?? "",
                Name = row["Name"]?.ToString() ?? "",
                Email = row.Table.Columns.Contains("Email") ? row["Email"]?.ToString() : null,
                UserTypeId = userTypeId,
                // UserTypeId == 1 is Director/Admin in legacy schema
                IsAdmin = userTypeId == 1,
                IsManager = userTypeId == 2,
                DivisionId = row.Table.Columns.Contains("DivisionId") && row["DivisionId"] != DBNull.Value
                    ? Convert.ToInt32(row["DivisionId"]) : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login validation error for {Login}", login);
            return null;
        }
    }

    public async Task SignInAsync(HttpContext context, User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.NameIdentifier, user.Code),
            new("UserId", user.UserId.ToString()),
            new("UserTypeId", user.UserTypeId.ToString()),
            new(ClaimTypes.Role, user.IsAdmin ? "Admin" : user.IsManager ? "Manager" : "User"),
        };

        if (user.IsAdmin) claims.Add(new Claim(ClaimTypes.Role, "Director"));
        if (user.UserTypeId == 1) claims.Add(new Claim(ClaimTypes.Role, "Director"));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });
    }
}
