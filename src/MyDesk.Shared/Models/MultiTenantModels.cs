namespace MyDesk.Shared.Models;

public static class TenantConstants
{
    public static readonly Guid TechlightTenantId        = Guid.Parse("11111111-1111-1111-1111-111111111111");
    public static readonly Guid DigitalResponseTenantId  = Guid.Parse("22222222-2222-2222-2222-222222222222");
    public static readonly Guid DemoTenantId             = Guid.Parse("33333333-3333-3333-3333-333333333333");

    public const string TenantIdClaim   = "tenant_id";
    public const string TenantNameClaim = "tenant_name";
    public const string TenantSlugClaim = "tenant_slug";

    /// <summary>Slugs for hard-coded built-in tenants.</summary>
    public const string TechlightSlug       = "techlight";
    public const string DigitalResponseSlug = "digital-response";
    public const string DemoSlug            = "demo";
}

public class Tenant
{
    public Guid TenantId { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Subdomain { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? AddressLine1 { get; set; }
    public string? Suburb { get; set; }
    public string? State { get; set; }
    public string? PostCode { get; set; }
    public string Country { get; set; } = "Australia";
    public string? ABN { get; set; }
    public string SubscriptionPlan { get; set; } = "Foundation";
    public int MaxUsers { get; set; } = 10;
    public int StorageLimitMB { get; set; } = 1024;
    public bool IsTrial { get; set; }
    public DateTime? TrialExpiresAt { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsSuspended { get; set; }
    public DateTime? SuspendedAt { get; set; }
    public string? SuspendedReason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    // Hostnames mapped to this tenant (e.g. techlight.digitalresponse.com.au, localhost).
    // The login page uses Request.Host to find the tenant + apply branding.
    public List<TenantHostname> Hostnames { get; set; } = new();
}

public class TenantHostname
{
    public int TenantHostnameId { get; set; }
    public Guid TenantId { get; set; }
    public string Hostname { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class UserTenant
{
    public int UserTenantId { get; set; }
    public int UserId { get; set; }
    public Guid TenantId { get; set; }
    public string Role { get; set; } = "User";
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public string? InvitedBy { get; set; }
    public DateTime? InvitedAt { get; set; }
    public DateTime? AcceptedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string? TenantName { get; set; }
    public string? TenantSlug { get; set; }
}

public class PlatformSettingsEntity
{
    public Guid TenantId { get; set; }
    public string SettingsJson { get; set; } = "{}";
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public string? UpdatedBy { get; set; }
}

public class TenantMembership
{
    public int UserTenantId { get; set; }
    public int UserId { get; set; }
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string TenantSlug { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}
