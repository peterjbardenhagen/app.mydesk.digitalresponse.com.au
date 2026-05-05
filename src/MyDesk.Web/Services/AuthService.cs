using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using MyDesk.Shared.Models;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Services;

public class AuthService
{
    private readonly UserService _userService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(UserService userService, ILogger<AuthService> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    public async Task<User?> ValidateLoginAsync(string login, string password)
    {
        try
        {
            // Use UserService's VerifyLoginAsync which handles password hashing and legacy support
            return await _userService.VerifyLoginAsync(login, password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login validation error for {Login}", login);
            return null;
        }
    }

    public async Task SignInAsync(HttpContext context, User user, TenantMembership tenant, bool rememberMe = false)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.NameIdentifier, user.Code),
            new("UserId", user.UserId.ToString()),
            new("UserTypeId", user.UserTypeId.ToString()),
            new(TenantConstants.TenantIdClaim, tenant.TenantId.ToString()),
            new(TenantConstants.TenantNameClaim, tenant.TenantName),
            new("tenant_slug", tenant.TenantSlug),
        };

        // Map UserTypeId → Role claims
        // DB: 1=Staff, 2=Manager, 3=Director, 4=Administrator, 5=Super Administrator
        switch (user.UserTypeId)
        {
            case 3: // Director
                claims.Add(new Claim(ClaimTypes.Role, "Director"));
                claims.Add(new Claim(ClaimTypes.Role, "Administrator")); // Directors have admin access
                break;
            case 4: // Administrator
                claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
                break;
            case 5: // Super Administrator — all of Admin + Tenant management
                claims.Add(new Claim(ClaimTypes.Role, "SuperAdministrator"));
                claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
                claims.Add(new Claim(ClaimTypes.Role, "Director"));
                break;
        }
        claims.Add(new Claim("UserTypeId", user.UserTypeId.ToString()));

        // Legacy compatibility
        if (user.IsAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            claims.Add(new Claim(ClaimTypes.Role, "Administrator"));
            claims.Add(new Claim(ClaimTypes.Role, "Director"));
        }
        if (user.IsManager) claims.Add(new Claim(ClaimTypes.Role, "Manager"));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        // Set session expiration based on rememberMe flag
        var expiration = rememberMe 
            ? DateTimeOffset.UtcNow.AddDays(30)  // 30 days if remember me is checked
            : DateTimeOffset.UtcNow.AddHours(8); // 8 hours for regular session

        await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal,
            new AuthenticationProperties { IsPersistent = true, ExpiresUtc = expiration });
    }
}
