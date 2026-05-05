using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using MyDesk.Shared.Models;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Services;

/// <summary>
/// Resolves the current tenant for the active scope.
///
/// Resolution order:
///   1) <see cref="TenantImpersonation"/> AsyncLocal override (used by Hangfire jobs, tests).
///   2) HttpContext User claims (cookie or API-key principal).
///   3) Nothing — caller is anonymous, BypassTenantIsolation = true.
/// </summary>
public class CurrentTenantAccessor : ICurrentTenantAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentTenantAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? TenantId
    {
        get
        {
            if (TenantImpersonation.Current is { } o) return o.TenantId;
            var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue(TenantConstants.TenantIdClaim);
            return Guid.TryParse(value, out var tenantId) ? tenantId : null;
        }
    }

    public string? TenantName
    {
        get
        {
            if (TenantImpersonation.Current is { } o) return o.TenantName;
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(TenantConstants.TenantNameClaim);
        }
    }

    public int? UserId
    {
        get
        {
            if (TenantImpersonation.Current is { } o) return o.UserId;
            var value = _httpContextAccessor.HttpContext?.User?.FindFirstValue("UserId");
            return int.TryParse(value, out var userId) ? userId : null;
        }
    }

    public string? UserCode
    {
        get
        {
            if (TenantImpersonation.Current is { } o) return o.UserCode;
            return _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        }
    }

    public bool BypassTenantIsolation
    {
        get
        {
            // Bypass is granted ONLY through two trusted paths:
            //   1) An explicit TenantImpersonation.SystemBypass() scope (startup migrations,
            //      schema enforcement, the seeder before any user is impersonated).
            //   2) Genuinely no HttpContext — i.e. the host process itself outside any
            //      request scope (Hangfire's own scheduler bookkeeping, app startup).
            //
            // Anonymous HTTP requests (login page, /robots.txt, hostname-based branding
            // lookups) do NOT get bypass. RLS-protected tables are tenant-scoped data and
            // anon users have no business reading any of it. The login flow only touches
            // opt-out tables (Tenants, TenantHostnames, UserTenants, Users) which are not
            // RLS-protected anyway, so it works without a bypass.
            if (TenantImpersonation.Current is { } o) return o.BypassIsolation;
            return _httpContextAccessor.HttpContext == null;
        }
    }
}
