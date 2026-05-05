namespace MyDesk.Shared.Services;

public interface ICurrentTenantAccessor
{
    Guid? TenantId { get; }
    string? TenantName { get; }
    int? UserId { get; }
    string? UserCode { get; }
    bool BypassTenantIsolation { get; }
}

/// <summary>
/// AsyncLocal override used by background jobs (Hangfire) and other contexts that
/// don't have an HttpContext. Wrap an operation in <c>using TenantImpersonation.For(tenantId)</c>
/// and any service that respects this override (CurrentTenantAccessor) will report
/// that tenant for the duration of the scope.
///
/// <see cref="SystemBypass"/> is a special mode for startup migrations / schema
/// enforcement / seeding — it sets the <c>BypassTenantIsolation</c> session flag
/// which tells SQL Row-Level Security policies to allow cross-tenant access.
/// </summary>
public static class TenantImpersonation
{
    private static readonly AsyncLocal<TenantOverride?> _current = new();

    public static TenantOverride? Current => _current.Value;

    public static IDisposable For(Guid tenantId, string? tenantName = null, int? userId = null, string? userCode = null)
    {
        var prior = _current.Value;
        _current.Value = new TenantOverride(tenantId, tenantName, userId, userCode, BypassIsolation: false);
        return new Scope(() => _current.Value = prior);
    }

    /// <summary>
    /// Run the wrapped block as an unscoped system caller — RLS bypass enabled,
    /// no specific tenant. Use only for trusted server-side work (migrations,
    /// schema enforcement, seeding pre-tenant tables).
    /// </summary>
    public static IDisposable SystemBypass()
    {
        var prior = _current.Value;
        _current.Value = new TenantOverride(Guid.Empty, "system", null, "system", BypassIsolation: true);
        return new Scope(() => _current.Value = prior);
    }

    private sealed class Scope : IDisposable
    {
        private readonly Action _onDispose;
        public Scope(Action onDispose) { _onDispose = onDispose; }
        public void Dispose() => _onDispose();
    }
}

public sealed record TenantOverride(Guid TenantId, string? TenantName, int? UserId, string? UserCode, bool BypassIsolation);
