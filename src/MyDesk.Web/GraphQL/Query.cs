using HotChocolate;
using HotChocolate.Authorization;
using HotChocolate.Data;
using Microsoft.EntityFrameworkCore;
using MyDesk.Shared.Data;
using MyDesk.Shared.Models;

namespace MyDesk.Web.GraphQL;

/// <summary>
/// GraphQL root Query — exposed at /graphql.
///
/// Authorization: all root fields require an authenticated user (cookie or API key).
/// Tenant scoping is enforced by the SQL TenantId session context applied in
/// <see cref="MyDesk.Shared.Services.DatabaseService"/>; EF Core queries through the
/// same connection respect that context once row-level security policies are added.
/// </summary>
[Authorize]
public class Query
{
    /// <summary>All tenants (admin only — typical client should call <c>currentTenant</c>).</summary>
    [Authorize(Roles = new[] { "Admin", "Administrator", "Director" })]
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<Tenant> Tenants([Service] MyDeskDbContext db) =>
        db.Tenants.AsNoTracking().Include(t => t.Hostnames);

    /// <summary>The tenant the current user is signed into.</summary>
    public async Task<Tenant?> CurrentTenant(
        [Service] MyDeskDbContext db,
        [Service] MyDesk.Shared.Services.ICurrentTenantAccessor tenantAccessor)
    {
        if (tenantAccessor.TenantId is not { } id) return null;
        return await db.Tenants
            .AsNoTracking()
            .Include(t => t.Hostnames)
            .FirstOrDefaultAsync(t => t.TenantId == id);
    }

    /// <summary>All users in the current tenant.</summary>
    [UseProjection]
    [UseFiltering]
    [UseSorting]
    public IQueryable<User> Users(
        [Service] MyDeskDbContext db,
        [Service] MyDesk.Shared.Services.ICurrentTenantAccessor tenantAccessor)
    {
        if (tenantAccessor.TenantId is not { } id) return Enumerable.Empty<User>().AsQueryable();
        return from u in db.Users.AsNoTracking()
               join ut in db.UserTenants.AsNoTracking() on u.UserId equals ut.UserId
               where ut.TenantId == id && ut.IsActive
               select u;
    }

    /// <summary>Membership map for the current user (which tenants they can switch to).</summary>
    public async Task<IReadOnlyList<UserTenant>> MyTenants(
        [Service] MyDeskDbContext db,
        [Service] MyDesk.Shared.Services.ICurrentTenantAccessor tenantAccessor)
    {
        if (tenantAccessor.UserId is not { } userId) return Array.Empty<UserTenant>();
        return await db.UserTenants
            .AsNoTracking()
            .Where(ut => ut.UserId == userId && ut.IsActive)
            .ToListAsync();
    }
}
