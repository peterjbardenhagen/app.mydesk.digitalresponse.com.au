using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MyDesk.Shared.Data;
using MyDesk.Shared.Models;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Api.Controllers;

/// <summary>
/// Tenants REST endpoints — for external products that need to discover or
/// switch tenant context. Authenticated callers see only the tenants they
/// have membership in (cookie users) or the tenant the API key is scoped to.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize]
[Produces("application/json")]
public class TenantsController : ControllerBase
{
    private readonly MyDeskDbContext _db;
    private readonly ICurrentTenantAccessor _tenant;

    public TenantsController(MyDeskDbContext db, ICurrentTenantAccessor tenant)
    {
        _db = db;
        _tenant = tenant;
    }

    /// <summary>List tenants accessible to the caller.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<Tenant>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<Tenant>>> GetAll()
    {
        // API key (no UserId): just return the active tenant.
        if (_tenant.UserId is null)
        {
            if (_tenant.TenantId is not { } tid) return Ok(Array.Empty<Tenant>());
            return Ok(await _db.Tenants.AsNoTracking().Where(t => t.TenantId == tid).ToListAsync());
        }

        var memberships = _db.UserTenants
            .AsNoTracking()
            .Where(ut => ut.UserId == _tenant.UserId && ut.IsActive)
            .Select(ut => ut.TenantId);

        var tenants = await _db.Tenants
            .AsNoTracking()
            .Where(t => memberships.Contains(t.TenantId) && t.IsActive)
            .Include(t => t.Hostnames)
            .ToListAsync();

        return Ok(tenants);
    }

    /// <summary>Get the tenant the caller is currently signed into.</summary>
    [HttpGet("current")]
    [ProducesResponseType(typeof(Tenant), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Tenant>> GetCurrent()
    {
        if (_tenant.TenantId is not { } id) return NotFound();
        var tenant = await _db.Tenants
            .AsNoTracking()
            .Include(t => t.Hostnames)
            .FirstOrDefaultAsync(t => t.TenantId == id);
        return tenant is null ? NotFound() : Ok(tenant);
    }

    /// <summary>Resolve a tenant by hostname (used by login/branding flows).</summary>
    [HttpGet("by-host/{host}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(Tenant), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Tenant>> GetByHost(string host)
    {
        var clean = host.Split(':')[0].Trim().ToLowerInvariant();
        var tenant = await (
            from t in _db.Tenants.AsNoTracking()
            join h in _db.TenantHostnames.AsNoTracking() on t.TenantId equals h.TenantId
            where h.Hostname.ToLower() == clean && t.IsActive
            select t).FirstOrDefaultAsync();
        return tenant is null ? NotFound() : Ok(tenant);
    }
}
