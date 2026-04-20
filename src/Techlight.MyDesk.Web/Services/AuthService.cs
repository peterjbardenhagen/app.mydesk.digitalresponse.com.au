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

    public async Task<User?> ValidateLoginAsync(string code, string password)
    {
        try
        {
            var dt = await _db.QueryAsync(
                "SELECT * FROM Users WHERE Code = @Code AND Password = @Password",
                new() { ["Code"] = code, ["Password"] = password });

            if (dt.Rows.Count == 0) return null;

            var row = dt.Rows[0];
            return new User
            {
                UserId = Convert.ToInt32(row["UserId"]),
                Code = row["Code"]?.ToString() ?? "",
                Name = row["Name"]?.ToString() ?? "",
                Email = row["Email"]?.ToString(),
                IsAdmin = row.Table.Columns.Contains("Admin") && Convert.ToBoolean(row["Admin"] == DBNull.Value ? false : row["Admin"]),
                IsManager = row.Table.Columns.Contains("Manager") && Convert.ToBoolean(row["Manager"] == DBNull.Value ? false : row["Manager"]),
                UserTypeId = row.Table.Columns.Contains("UserTypeId") ? Convert.ToInt32(row["UserTypeId"] == DBNull.Value ? 0 : row["UserTypeId"]) : 0,
                DivisionId = row.Table.Columns.Contains("DivisionId") && row["DivisionId"] != DBNull.Value ? Convert.ToInt32(row["DivisionId"]) : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login validation error for user {Code}", code);
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
