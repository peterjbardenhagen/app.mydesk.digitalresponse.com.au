using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;


namespace MyDesk.Shared.Services.Integrations;

/// <summary>
/// Orchestrates accounting sync across all enabled providers (Xero, QuickBooks, MYOB).
/// Also owns the DDL for sync tracking tables.
/// </summary>
public class AccountingSyncManager
{
    private readonly DatabaseService _db;
    private readonly IAccountingSettingsService _settings;
    private readonly XeroSyncService _xero;
    private readonly QuickBooksSyncService _qbo;
    private readonly MyobSyncService _myob;
    private readonly ILogger<AccountingSyncManager> _logger;

    public AccountingSyncManager(
        DatabaseService db,
        IAccountingSettingsService settings,
        XeroSyncService xero,
        QuickBooksSyncService qbo,
        MyobSyncService myob,
        ILogger<AccountingSyncManager> logger)
    {
        _db       = db;
        _settings = settings;
        _xero     = xero;
        _qbo      = qbo;
        _myob     = myob;
        _logger   = logger;
    }

    // ── Table provisioning ─────────────────────────────────────────────────────

    public async Task EnsureTablesAsync()
    {
        await _db.ExecuteNonQueryAsync(@"
IF OBJECT_ID('AccountingSyncRecords') IS NULL
CREATE TABLE AccountingSyncRecords (
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    TenantId   UNIQUEIDENTIFIER NULL,
    Provider   NVARCHAR(50)  NOT NULL,
    EntityType NVARCHAR(50)  NOT NULL,
    ExternalId NVARCHAR(200) NOT NULL,
    InternalId NVARCHAR(200) NOT NULL DEFAULT '',
    SyncedAt   DATETIME      NOT NULL DEFAULT GETDATE(),
    Direction  NVARCHAR(10)  NOT NULL DEFAULT 'Both',
    LastStatus NVARCHAR(50)  NOT NULL DEFAULT '',
    ErrorMessage NVARCHAR(MAX) NULL
);

IF OBJECT_ID('AccountingSyncLog') IS NULL
CREATE TABLE AccountingSyncLog (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    TenantId     UNIQUEIDENTIFIER NULL,
    Provider     NVARCHAR(50)  NOT NULL,
    EntityType   NVARCHAR(50)  NOT NULL,
    Direction    NVARCHAR(10)  NOT NULL DEFAULT 'Both',
    Count        INT           NOT NULL DEFAULT 0,
    Status       NVARCHAR(50)  NOT NULL DEFAULT '',
    StartedAt    DATETIME      NOT NULL DEFAULT GETDATE(),
    CompletedAt  DATETIME      NULL,
    ErrorMessage NVARCHAR(MAX) NULL
);

-- Back-fill ExternalRef column on Invoices if missing (used for cross-provider tracking)
IF OBJECT_ID('Invoices') IS NOT NULL
BEGIN
    IF COL_LENGTH('Invoices','ExternalRef') IS NULL
        ALTER TABLE Invoices ADD ExternalRef NVARCHAR(200) NULL;
END;");
    }

    // ── Orchestration ──────────────────────────────────────────────────────────

    public async Task RunScheduledSyncAsync()
    {
        _logger.LogInformation("AccountingSyncManager: scheduled sync starting");
        var cfg = _settings.Current;

        if (cfg.EnableXeroIntegration && cfg.Xero.Enabled && cfg.Xero.IsConnected)
        {
            _logger.LogInformation("AccountingSyncManager: running Xero sync");
            try { await _xero.RunFullSyncAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Xero full sync failed"); }
        }

        if (cfg.EnableQuickBooksIntegration && cfg.QuickBooks.Enabled && cfg.QuickBooks.IsConnected)
        {
            _logger.LogInformation("AccountingSyncManager: running QuickBooks sync");
            try { await _qbo.RunFullSyncAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "QuickBooks full sync failed"); }
        }

        if (cfg.EnableMYOBIntegration && cfg.MYOB.Enabled && cfg.MYOB.IsConnected)
        {
            _logger.LogInformation("AccountingSyncManager: running MYOB sync");
            try { await _myob.RunFullSyncAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "MYOB full sync failed"); }
        }

        _logger.LogInformation("AccountingSyncManager: scheduled sync complete");
    }

    // ── Status ─────────────────────────────────────────────────────────────────

    public async Task<List<AccountingSyncStatus>> GetSyncStatusAsync()
    {
        var cfg    = _settings.Current;
        var result = new List<AccountingSyncStatus>();

        result.Add(await BuildStatusAsync("Xero",        cfg.Xero,        cfg.EnableXeroIntegration));
        result.Add(await BuildStatusAsync("QuickBooks",  cfg.QuickBooks,  cfg.EnableQuickBooksIntegration));
        result.Add(await BuildStatusAsync("MYOB",        cfg.MYOB,        cfg.EnableMYOBIntegration));

        return result;
    }

    private async Task<AccountingSyncStatus> BuildStatusAsync(string provider, IntegrationSettings intSettings, bool featureEnabled)
    {
        var status = new AccountingSyncStatus
        {
            Provider    = provider,
            Enabled     = featureEnabled && intSettings.Enabled,
            IsConnected = intSettings.IsConnected,
            LastSync    = intSettings.LastSync,
            Status      = !featureEnabled ? "Feature disabled"
                        : !intSettings.Enabled ? "Disabled"
                        : !intSettings.IsConnected ? "Not connected"
                        : "Connected",
        };

        try
        {
            var dt = await _db.QueryAsync(@"
                SELECT TOP 10 Id, Provider, EntityType, Direction, Count, Status, StartedAt, CompletedAt, ErrorMessage
                FROM AccountingSyncLog
                WHERE Provider = @P
                ORDER BY StartedAt DESC",
                new() { ["P"] = provider });

            status.RecentLogs = dt.Map(row => new SyncLogEntry
            {
                Id          = row["Id"] is int i ? i : 0,
                Provider    = row["Provider"]?.ToString() ?? "",
                EntityType  = row["EntityType"]?.ToString() ?? "",
                Direction   = row["Direction"]?.ToString() ?? "",
                Count       = row["Count"] is int c ? c : 0,
                Status      = row["Status"]?.ToString() ?? "",
                StartedAt   = row["StartedAt"] is DateTime sa ? sa : DateTime.MinValue,
                CompletedAt = row["CompletedAt"] as DateTime?,
                ErrorMessage = row["ErrorMessage"]?.ToString(),
            });
        }
        catch
        {
            // Table may not exist yet
        }

        return status;
    }
}
