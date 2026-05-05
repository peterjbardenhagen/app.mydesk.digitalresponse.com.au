using System.Data;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

/// <summary>
/// Enforces multi-tenant data isolation at the SQL Server level.
///
/// On startup this service walks every table that has a <c>TenantId</c> column and:
///   1. Backfills any NULL TenantId rows to the legacy default (Techlight).
///   2. Sets the column to <c>NOT NULL</c> with a <c>DEFAULT</c> that pulls
///      the current tenant from <c>SESSION_CONTEXT('TenantId')</c> — meaning
///      services that don't explicitly set TenantId on insert get the right
///      value automatically.
///   3. Adds a foreign key to <c>Tenants</c>.
///   4. Applies a SQL Server <b>Row-Level Security</b> policy with FILTER +
///      BLOCK predicates so that:
///         * SELECT/UPDATE/DELETE only see rows for the current tenant.
///         * INSERT/UPDATE rows that don't match the current tenant are blocked.
///         * The "Bypass" flag (<c>SESSION_CONTEXT('BypassTenantIsolation')</c>)
///           short-circuits the predicate for system / migration / anon flows.
///
/// The session context is set per connection by <see cref="DatabaseService.ApplyTenantSessionContextAsync"/>,
/// so this protects ALL data-access paths — Dapper, raw ADO.NET, EF Core and any
/// future tools — without per-query changes.
///
/// Tables that are intentionally <i>not</i> RLS-protected here (typically because
/// they need cross-tenant visibility during auth/admin flows):
///   * Tenants, TenantHostnames, PlatformSettingsEntities (the tenant catalogue itself)
///   * UserTenants (the membership map — read during /login/select-tenant before any tenant is selected)
///   * Users, UserTypes, UserRoles, RolePermissions (global identity tables)
///   * ErrorLog, ActivityLog, EmailLog, AiAudit, EntityAudit (system-wide audit trails)
///   * IndustryMultiples, lookup/reference tables
///
/// Tables explicitly skipped above are listed in <see cref="OptOutTables"/>.
/// </summary>
public class TenantIsolationService
{
    private readonly DatabaseService _db;
    private readonly ILogger<TenantIsolationService> _logger;

    /// <summary>
    /// Tables with a <c>TenantId</c> column that should NOT be RLS-protected
    /// (because cross-tenant reads are part of their normal use).
    /// Match is case-insensitive.
    /// </summary>
    private static readonly HashSet<string> OptOutTables = new(StringComparer.OrdinalIgnoreCase)
    {
        "Tenants",                   // PK is TenantId; tenant catalogue itself
        "TenantHostnames",           // host -> tenant lookup at login (pre-auth)
        "PlatformSettingsEntities",  // tenant catalogue
        "UserTenants",               // membership rows; readable during tenant-switch
        // Identity / lookup
        "Users", "UserTypes", "UserRoles", "RolePermissions",
        // System audit / error trails
        "ErrorLog", "ErrorLogs", "ActivityLog", "EmailLog", "AiAudit", "EntityAudit",
        // Hangfire's own schema is kept on its 'HangFire' schema namespace and
        // doesn't have a TenantId column — included here only as a guard.
    };

    public TenantIsolationService(DatabaseService db, ILogger<TenantIsolationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EnforceAsync()
    {
        try
        {
            await EnsurePredicateFunctionAsync();
            var tables = await GetTenantScopedTablesAsync();
            _logger.LogInformation("Tenant isolation: scanning {Count} tenant-scoped tables", tables.Count);

            foreach (var t in tables)
            {
                if (OptOutTables.Contains(t))
                {
                    _logger.LogDebug("Tenant isolation: skipping opt-out table {Table}", t);
                    continue;
                }
                try
                {
                    await BackfillNullsAsync(t);
                    await EnsureNotNullDefaultAsync(t);
                    await EnsureForeignKeyAsync(t);
                    await ApplyRowLevelSecurityAsync(t);
                }
                catch (Exception ex)
                {
                    // Don't let one table block the rest of the policy roll-out.
                    _logger.LogWarning(ex, "Tenant isolation: failed to enforce on {Table}", t);
                }
            }

            _logger.LogInformation("Tenant isolation: enforcement complete");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tenant isolation: enforcement aborted");
        }
    }

    /// <summary>
    /// Returns the names of every base table containing a column named TenantId
    /// (uniqueidentifier). Excludes views and Hangfire's schema.
    /// </summary>
    private async Task<List<string>> GetTenantScopedTablesAsync()
    {
        var dt = await _db.QueryAsync(@"
SELECT t.TABLE_NAME
FROM INFORMATION_SCHEMA.TABLES t
INNER JOIN INFORMATION_SCHEMA.COLUMNS c
    ON c.TABLE_NAME = t.TABLE_NAME AND c.TABLE_SCHEMA = t.TABLE_SCHEMA
WHERE t.TABLE_TYPE = 'BASE TABLE'
  AND t.TABLE_SCHEMA = 'dbo'
  AND c.COLUMN_NAME = 'TenantId'
  AND c.DATA_TYPE = 'uniqueidentifier'
ORDER BY t.TABLE_NAME;");
        var result = new List<string>();
        foreach (DataRow r in dt.Rows) result.Add(r["TABLE_NAME"].ToString()!);
        return result;
    }

    /// <summary>
    /// Creates / refreshes the shared predicate function used by every RLS policy.
    /// Returns 1 row (allowed) when:
    ///   * BypassTenantIsolation session flag is 1 (system / migrations / login pre-auth), OR
    ///   * the row's TenantId equals SESSION_CONTEXT('TenantId').
    /// </summary>
    private async Task EnsurePredicateFunctionAsync()
    {
        // Drop any stored procedures that reference fn_TenantPredicate, then the function itself
        await _db.ExecuteNonQueryAsync(@"
-- 1. Drop all procedures referencing fn_TenantPredicate (via sql_expression_dependencies)
DECLARE @dep NVARCHAR(200);
DECLARE dep_cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT OBJECT_NAME(referencing_id)
    FROM sys.sql_expression_dependencies
    WHERE referenced_entity_name = 'fn_TenantPredicate'
      AND OBJECTPROPERTY(referencing_id, 'IsProcedure') = 1;
OPEN dep_cur;
FETCH NEXT FROM dep_cur INTO @dep;
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC('DROP PROCEDURE IF EXISTS [dbo].[' + @dep + ']');
    FETCH NEXT FROM dep_cur INTO @dep;
END
CLOSE dep_cur; DEALLOCATE dep_cur;

-- 2. Drop any RLS security policies (identified by name convention or scanning all policies)
DECLARE @pol NVARCHAR(200);
DECLARE pol_cur CURSOR LOCAL FAST_FORWARD FOR
    SELECT name FROM sys.security_policies
    WHERE OBJECT_SCHEMA_NAME(object_id) = 'dbo';
OPEN pol_cur;
FETCH NEXT FROM pol_cur INTO @pol;
WHILE @@FETCH_STATUS = 0
BEGIN
    BEGIN TRY EXEC('DROP SECURITY POLICY IF EXISTS [dbo].[' + @pol + ']'); END TRY BEGIN CATCH END CATCH
    FETCH NEXT FROM pol_cur INTO @pol;
END
CLOSE pol_cur; DEALLOCATE pol_cur;

-- 3. Now safe to drop the function
IF OBJECT_ID(N'dbo.fn_TenantPredicate', N'IF') IS NOT NULL
    DROP FUNCTION dbo.fn_TenantPredicate;");

        await _db.ExecuteNonQueryAsync(@"
CREATE FUNCTION dbo.fn_TenantPredicate(@TenantId UNIQUEIDENTIFIER)
RETURNS TABLE
WITH SCHEMABINDING
AS RETURN
    SELECT 1 AS allowed
    WHERE
        CAST(SESSION_CONTEXT(N'BypassTenantIsolation') AS BIT) = 1
     OR @TenantId = TRY_CAST(SESSION_CONTEXT(N'TenantId') AS UNIQUEIDENTIFIER);");
    }

    private async Task BackfillNullsAsync(string table)
    {
        // Anything still NULL is legacy data — assign to Techlight (the original single tenant).
        await _db.ExecuteNonQueryAsync(
            $"UPDATE [{table}] SET TenantId = @T WHERE TenantId IS NULL",
            new() { ["T"] = TenantConstants.TechlightTenantId });
    }

    /// <summary>
    /// Make TenantId NOT NULL and add a DEFAULT that picks up the current
    /// SESSION_CONTEXT TenantId (so existing INSERTs that don't mention TenantId
    /// still produce correctly-tagged rows).
    /// </summary>
    private async Task EnsureNotNullDefaultAsync(string table)
    {
        var tableObjectId = await _db.ScalarAsync<int?>(
            "SELECT OBJECT_ID(@T)", new() { ["T"] = table });

        if (tableObjectId is null) return;

        // Set column NOT NULL — must drop any index on TenantId first, then recreate.
        await _db.ExecuteNonQueryAsync($@"
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = '{table}' AND COLUMN_NAME = 'TenantId' AND IS_NULLABLE = 'YES'
)
BEGIN
    -- Drop all non-PK indexes that include TenantId so ALTER COLUMN can proceed
    DECLARE @idx NVARCHAR(200);
    DECLARE idx_cur CURSOR LOCAL FAST_FORWARD FOR
        SELECT i.name
        FROM sys.indexes i
        INNER JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
        INNER JOIN sys.columns c       ON c.object_id  = ic.object_id AND c.column_id = ic.column_id
        WHERE i.object_id = {tableObjectId}
          AND c.name = 'TenantId'
          AND i.is_primary_key = 0;
    OPEN idx_cur;
    FETCH NEXT FROM idx_cur INTO @idx;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        EXEC('DROP INDEX [' + @idx + '] ON [{table}]');
        FETCH NEXT FROM idx_cur INTO @idx;
    END
    CLOSE idx_cur; DEALLOCATE idx_cur;

    ALTER TABLE [{table}] ALTER COLUMN TenantId UNIQUEIDENTIFIER NOT NULL;

    -- Recreate the standard TenantId index
    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_{table}_TenantId' AND object_id = OBJECT_ID('{table}'))
        CREATE INDEX [IX_{table}_TenantId] ON [{table}](TenantId);
END");

        // Drop any existing DEFAULT constraint on the column, then add ours.
        await _db.ExecuteNonQueryAsync($@"
DECLARE @df NVARCHAR(200);
SELECT @df = dc.name
FROM sys.default_constraints dc
INNER JOIN sys.columns c
    ON c.default_object_id = dc.object_id
WHERE OBJECT_NAME(dc.parent_object_id) = '{table}'
  AND c.name = 'TenantId';
IF @df IS NOT NULL EXEC('ALTER TABLE [{table}] DROP CONSTRAINT ' + @df);

ALTER TABLE [{table}] ADD CONSTRAINT DF_{table}_TenantId
    DEFAULT (TRY_CAST(SESSION_CONTEXT(N'TenantId') AS UNIQUEIDENTIFIER)) FOR TenantId;");
    }

    /// <summary>FK to Tenants(TenantId) — wrap in try/catch so existing data inconsistency doesn't kill startup.</summary>
    private async Task EnsureForeignKeyAsync(string table)
    {
        var fkName = $"FK_{table}_Tenants";
        await _db.ExecuteNonQueryAsync($@"
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = '{fkName}')
BEGIN TRY
    ALTER TABLE [{table}] WITH NOCHECK
        ADD CONSTRAINT {fkName} FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId);
END TRY
BEGIN CATCH
    -- Existing data may violate; skip silently. The RLS policy still protects access.
END CATCH");
    }

    /// <summary>
    /// Drop any existing security policy for this table, then recreate one with
    /// FILTER + BLOCK predicates pointing at the shared <c>fn_TenantPredicate</c>.
    /// </summary>
    private async Task ApplyRowLevelSecurityAsync(string table)
    {
        var policyName = $"sp_TenantIsolation_{table}";

        await _db.ExecuteNonQueryAsync($@"
IF EXISTS (SELECT 1 FROM sys.security_policies WHERE name = '{policyName}')
    DROP SECURITY POLICY {policyName};");

        await _db.ExecuteNonQueryAsync($@"
CREATE SECURITY POLICY {policyName}
    ADD FILTER PREDICATE dbo.fn_TenantPredicate(TenantId) ON dbo.[{table}],
    ADD BLOCK PREDICATE  dbo.fn_TenantPredicate(TenantId) ON dbo.[{table}] AFTER INSERT,
    ADD BLOCK PREDICATE  dbo.fn_TenantPredicate(TenantId) ON dbo.[{table}] AFTER UPDATE,
    ADD BLOCK PREDICATE  dbo.fn_TenantPredicate(TenantId) ON dbo.[{table}] BEFORE UPDATE,
    ADD BLOCK PREDICATE  dbo.fn_TenantPredicate(TenantId) ON dbo.[{table}] BEFORE DELETE
WITH (STATE = ON);");

        _logger.LogDebug("Tenant isolation: policy applied on {Table}", table);
    }

    /// <summary>Convenience for admin-tools / migrations that need to disable RLS temporarily.</summary>
    public async Task SetPolicyStateAsync(bool enabled)
    {
        var dt = await _db.QueryAsync(
            "SELECT name FROM sys.security_policies WHERE name LIKE 'sp_TenantIsolation_%'");
        foreach (DataRow r in dt.Rows)
        {
            var name = r["name"].ToString()!;
            await _db.ExecuteNonQueryAsync($"ALTER SECURITY POLICY {name} WITH (STATE = {(enabled ? "ON" : "OFF")});");
        }
        _logger.LogInformation("Tenant isolation: policies {State}", enabled ? "ENABLED" : "DISABLED");
    }
}
