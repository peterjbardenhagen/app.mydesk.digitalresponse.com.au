using Microsoft.EntityFrameworkCore;
using MyDesk.Shared.Models;
using MyDesk.Shared.Services;

namespace MyDesk.Shared.Data;

/// <summary>
/// EF Core 10 DbContext.
///
/// Initial scope (multi-tenant + identity entities) — the rest of the codebase still
/// uses Dapper/ADO.NET via <c>DatabaseService</c>. New features should prefer this
/// context; legacy services can be migrated incrementally without breakage because
/// both layers map to the same physical tables.
///
/// Schema is owned by SQL migration scripts + <c>TenantService.EnsureTablesAsync</c>;
/// EF model-building below is configured to match those existing tables (no EF
/// migrations are generated against this DbContext).
///
/// <b>Tenant isolation</b>:
///   1. SQL Row-Level Security policies (applied by <c>TenantIsolationService</c>)
///      filter every query at the database level — defence in depth that protects
///      Dapper, ADO.NET, EF, and ad-hoc tooling alike.
///   2. EF Core <i>global query filters</i> below add an extra layer at the ORM
///      level so EF queries are explicit about tenant scope and so navigation
///      properties don't accidentally leak.
///   3. <c>SaveChanges</c> stamps <c>TenantId</c> on inserted entities that have
///      that column, using <see cref="ICurrentTenantAccessor.TenantId"/>.
/// </summary>
public class MyDeskDbContext : DbContext
{
    private readonly ICurrentTenantAccessor _tenant;

    public MyDeskDbContext(DbContextOptions<MyDeskDbContext> options, ICurrentTenantAccessor tenant)
        : base(options)
    {
        _tenant = tenant;
    }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<TenantHostname> TenantHostnames => Set<TenantHostname>();
    public DbSet<UserTenant> UserTenants => Set<UserTenant>();
    public DbSet<PlatformSettingsEntity> PlatformSettingsEntities => Set<PlatformSettingsEntity>();
    public DbSet<User> Users => Set<User>();
    public DbSet<ScheduledTask> ScheduledTasks => Set<ScheduledTask>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Tenant>(e =>
        {
            e.ToTable("Tenants");
            e.HasKey(x => x.TenantId);
            e.Property(x => x.TenantId).HasColumnType("uniqueidentifier");
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Slug).HasMaxLength(100).IsRequired();
            e.Property(x => x.Subdomain).HasMaxLength(100);
            e.Property(x => x.ContactEmail).HasMaxLength(200);
            e.Property(x => x.ContactPhone).HasMaxLength(50);
            e.Property(x => x.Country).HasMaxLength(100);
            e.Property(x => x.SubscriptionPlan).HasMaxLength(50);
            e.HasIndex(x => x.Slug).IsUnique();

            e.HasMany(x => x.Hostnames)
             .WithOne()
             .HasForeignKey(h => h.TenantId)
             .OnDelete(DeleteBehavior.Cascade);

            // No global filter on Tenants itself — used during /login/select-tenant
            // (cross-tenant catalogue read) and the tenant catalogue is opt-out of RLS.
        });

        b.Entity<TenantHostname>(e =>
        {
            e.ToTable("TenantHostnames");
            e.HasKey(x => x.TenantHostnameId);
            e.Property(x => x.Hostname).HasMaxLength(255).IsRequired();
            e.HasIndex(x => x.Hostname).IsUnique();
        });

        b.Entity<UserTenant>(e =>
        {
            e.ToTable("UserTenants");
            e.HasKey(x => x.UserTenantId);
            e.Property(x => x.Role).HasMaxLength(50);
            // TenantName / TenantSlug are projection helpers — not mapped.
            e.Ignore(x => x.TenantName);
            e.Ignore(x => x.TenantSlug);

            // No global filter — used during multi-tenant chooser before any tenant is selected.
        });

        b.Entity<PlatformSettingsEntity>(e =>
        {
            e.ToTable("PlatformSettingsEntities");
            e.HasKey(x => x.TenantId);
            e.Property(x => x.SettingsJson).HasColumnType("nvarchar(max)").IsRequired();
            e.Property(x => x.UpdatedBy).HasMaxLength(100);

            // PlatformSettings rows are read by host-based pre-auth resolution.
            // They are opt-out of RLS but EF still benefits from an explicit filter
            // for authenticated calls.
            e.HasQueryFilter(x => x.TenantId == _tenant.TenantId || _tenant.BypassTenantIsolation);
        });

        b.Entity<User>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.UserId);
            e.Property(x => x.Code).HasMaxLength(50).IsRequired();
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Email).HasMaxLength(200);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.Property(x => x.Mobile).HasMaxLength(50);
            // RoleType enum is not stored on the Users table.
            e.Ignore(x => x.Role);
            e.Property(x => x.UserRole).HasMaxLength(50);
        });

        b.Entity<ScheduledTask>(e =>
        {
            e.ToTable("ScheduledTasks");
            e.HasKey(x => x.ScheduledTaskId);
            e.Property(x => x.Name).HasMaxLength(200).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.ActionType).HasMaxLength(50).IsRequired();
            e.Property(x => x.Recurrence).HasMaxLength(20).IsRequired();
            e.Property(x => x.CronExpression).HasMaxLength(100);
            e.Property(x => x.TimeZoneId).HasMaxLength(100);
            e.Property(x => x.CreatedBy).HasMaxLength(100);
            e.Property(x => x.LastStatus).HasMaxLength(20);

            // Belt and braces: EF query filter on top of SQL RLS.
            e.HasQueryFilter(x => x.TenantId == _tenant.TenantId || _tenant.BypassTenantIsolation);
        });
    }

    /// <summary>
    /// Stamp <c>TenantId</c> on inserted entities (when not already set) so callers
    /// don't have to remember it on every <c>db.Add(...)</c>. Mirrors the SQL
    /// DEFAULT installed by <c>TenantIsolationService</c>.
    /// </summary>
    public override int SaveChanges()
    {
        StampTenant();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        StampTenant();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void StampTenant()
    {
        if (_tenant.TenantId is not { } tenantId) return;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Added) continue;
            var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "TenantId");
            if (prop?.CurrentValue is null || (prop.CurrentValue is Guid g && g == Guid.Empty))
            {
                prop!.CurrentValue = tenantId;
            }
        }
    }
}
