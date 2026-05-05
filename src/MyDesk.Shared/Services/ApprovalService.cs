using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

/// <summary>
/// In-memory approval chain registry: stores LineManagerCode + IsCapExApprover
/// flags per user (companion to the existing Users table — no schema change),
/// records signed-off levels for each Quote/PO, and exposes "approver chain"
/// helpers used by QuoteService and PurchaseOrderService.
/// </summary>
public class ApprovalService
{
    private readonly ILogger<ApprovalService> _logger;
    private readonly UserService _users;

    // ── In-memory state (no DB schema changes) ────────────────────────────────
    private static readonly object _lock = new();
    private static readonly List<UserApprovalSettings> _settings = SeedSettings();
    private static readonly List<ApprovalEntry> _entries = new();
    private static int _nextEntryId = 1;

    // Approval thresholds (mirror the legacy MyDesk rules).
    public const decimal CapExThreshold = 5_000m;
    public const int QuoteMaxLevels    = 2; // line manager + (optional) director
    public const int PoMaxLevels       = 3; // line manager + capex approver + director

    public ApprovalService(ILogger<ApprovalService> logger, UserService users)
    {
        _logger = logger;
        _users  = users;
    }

    // ── Settings helpers ──────────────────────────────────────────────────────
    public IReadOnlyList<UserApprovalSettings> AllSettings()
    {
        lock (_lock) return _settings.ToArray();
    }

    public UserApprovalSettings GetSettings(string userCode)
    {
        lock (_lock)
        {
            return _settings.FirstOrDefault(s =>
                       string.Equals(s.UserCode, userCode, StringComparison.OrdinalIgnoreCase))
                   ?? new UserApprovalSettings { UserCode = userCode };
        }
    }

    public void UpsertSettings(UserApprovalSettings s)
    {
        if (string.IsNullOrWhiteSpace(s.UserCode)) return;
        lock (_lock)
        {
            var existing = _settings.FirstOrDefault(x =>
                string.Equals(x.UserCode, s.UserCode, StringComparison.OrdinalIgnoreCase));
            if (existing == null) _settings.Add(s);
            else
            {
                existing.LineManagerCode = s.LineManagerCode;
                existing.IsCapExApprover = s.IsCapExApprover;
            }
        }
    }

    // ── Chain walking ────────────────────────────────────────────────────────
    /// <summary>Returns the next manager up the chain from <paramref name="currentUserCode"/>, or null if top reached.</summary>
    public string? NextLineManager(string currentUserCode)
    {
        var s = GetSettings(currentUserCode);
        return string.IsNullOrWhiteSpace(s.LineManagerCode) ? null : s.LineManagerCode;
    }

    public string? FindCapExApprover()
    {
        lock (_lock)
        {
            return _settings.FirstOrDefault(x => x.IsCapExApprover)?.UserCode;
        }
    }

    // ── Approval entries ─────────────────────────────────────────────────────
    public List<ApprovalEntry> GetEntries(string entityType, int entityId)
    {
        lock (_lock)
        {
            return _entries
                .Where(e => e.EntityType == entityType && e.EntityId == entityId)
                .OrderBy(e => e.Level)
                .ToList();
        }
    }

    public void RecordApproval(string entityType, int entityId, int level, string approverCode,
        string? approverName, string decision = "Approved", string? notes = null)
    {
        lock (_lock)
        {
            _entries.Add(new ApprovalEntry
            {
                EntryId       = _nextEntryId++,
                EntityType    = entityType,
                EntityId      = entityId,
                Level         = level,
                ApproverCode  = approverCode,
                ApproverName  = approverName,
                Decision      = decision,
                Notes         = notes,
                Timestamp     = DateTime.Now,
            });
        }
        _logger.LogInformation("Approval recorded: {Type}#{Id} L{Level} by {Approver} = {Decision}",
            entityType, entityId, level, approverCode, decision);
    }

    public bool HasApproval(string entityType, int entityId, string approverCode)
    {
        lock (_lock)
        {
            return _entries.Any(e =>
                e.EntityType == entityType &&
                e.EntityId   == entityId   &&
                string.Equals(e.ApproverCode, approverCode, StringComparison.OrdinalIgnoreCase) &&
                e.Decision == "Approved");
        }
    }

    public int CompletedLevels(string entityType, int entityId)
    {
        return GetEntries(entityType, entityId).Count(e => e.Decision == "Approved");
    }

    public ApprovalEntry? LastApproval(string entityType, int entityId)
    {
        lock (_lock)
        {
            return _entries
                .Where(e => e.EntityType == entityType && e.EntityId == entityId && e.Decision == "Approved")
                .OrderByDescending(e => e.Timestamp)
                .FirstOrDefault();
        }
    }

    // ── Pending approval queries (used by /approvals/pending) ────────────────
    /// <summary>
    /// Returns quote/PO ids that are currently waiting on <paramref name="approverCode"/>.
    /// Combines two scenarios:
    ///   1. Item submitted by a direct report and not yet approved by this user.
    ///   2. Item already approved by a lower level whose chain points to this user.
    /// </summary>
    public List<(string EntityType, int EntityId)> PendingFor(string approverCode)
    {
        if (string.IsNullOrEmpty(approverCode)) return new();
        lock (_lock)
        {
            // Find all entities where the most recent approver's NextLineManager is this user.
            var byEntity = _entries
                .Where(e => e.Decision == "Approved")
                .GroupBy(e => (e.EntityType, e.EntityId))
                .Select(g => new
                {
                    Key = g.Key,
                    Last = g.OrderByDescending(x => x.Timestamp).First()
                });

            var pending = new List<(string, int)>();
            foreach (var row in byEntity)
            {
                var nextMgr = NextLineManager(row.Last.ApproverCode);
                if (!string.IsNullOrEmpty(nextMgr) &&
                    string.Equals(nextMgr, approverCode, StringComparison.OrdinalIgnoreCase) &&
                    !HasApproval(row.Key.EntityType, row.Key.EntityId, approverCode))
                {
                    pending.Add(row.Key);
                }
            }
            return pending;
        }
    }

    /// <summary>
    /// Items where the last approval is &gt; <paramref name="hours"/> ago and the next
    /// approver has not yet acted. Used by the dashboard "stalled approvals" widget.
    /// </summary>
    public List<(string EntityType, int EntityId, string LastApprover, DateTime LastAt, string? NextApprover)>
        StalledApprovals(double hours = 24)
    {
        var cutoff = DateTime.Now.AddHours(-hours);
        lock (_lock)
        {
            var grouped = _entries
                .Where(e => e.Decision == "Approved")
                .GroupBy(e => (e.EntityType, e.EntityId))
                .Select(g => new
                {
                    Key = g.Key,
                    Last = g.OrderByDescending(x => x.Timestamp).First()
                })
                .Where(x => x.Last.Timestamp <= cutoff)
                .ToList();

            var stalled = new List<(string, int, string, DateTime, string?)>();
            foreach (var row in grouped)
            {
                var nextMgr = NextLineManager(row.Last.ApproverCode);
                if (!string.IsNullOrEmpty(nextMgr) &&
                    !HasApproval(row.Key.EntityType, row.Key.EntityId, nextMgr))
                {
                    stalled.Add((row.Key.EntityType, row.Key.EntityId,
                        row.Last.ApproverCode, row.Last.Timestamp, nextMgr));
                }
            }
            return stalled;
        }
    }

    // ── Seed data (development/demo) ─────────────────────────────────────────
    private static List<UserApprovalSettings> SeedSettings() => new()
    {
        new() { UserCode = "PB",    LineManagerCode = "MD",  IsCapExApprover = false },
        new() { UserCode = "JD",    LineManagerCode = "MD",  IsCapExApprover = false },
        new() { UserCode = "MD",    LineManagerCode = null,  IsCapExApprover = true  },
    };
}
