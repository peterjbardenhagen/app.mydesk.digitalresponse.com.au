using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

/// <summary>
/// In-memory Call Reports service ported from legacy MyDesk.
/// TODO: persist to database
/// </summary>
public class CallReportService
{
    private readonly ILogger<CallReportService> _logger;
    private static readonly object _lock = new();
    private static readonly List<CallReport> _store = SeedData();

    public CallReportService(ILogger<CallReportService> logger)
    {
        _logger = logger;
    }

    public Task<List<CallReport>> GetAllAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_store
                .OrderByDescending(c => c.CallDate)
                .ToList());
        }
    }

    public Task<CallReport?> GetByIdAsync(int id)
    {
        lock (_lock) return Task.FromResult(_store.FirstOrDefault(c => c.Id == id));
    }

    public Task<CallReport> CreateAsync(CallReport report)
    {
        lock (_lock)
        {
            report.Id = _store.Count == 0 ? 1 : _store.Max(c => c.Id) + 1;
            _store.Add(report);
            _logger.LogInformation("Call report {Id} created", report.Id);
            return Task.FromResult(report);
        }
    }

    public Task<bool> UpdateAsync(CallReport report)
    {
        lock (_lock)
        {
            var existing = _store.FirstOrDefault(c => c.Id == report.Id);
            if (existing == null) return Task.FromResult(false);
            existing.CallDate = report.CallDate;
            existing.ContactId = report.ContactId;
            existing.ContactName = report.ContactName;
            existing.CompanyId = report.CompanyId;
            existing.CompanyName = report.CompanyName;
            existing.OwnerUserCode = report.OwnerUserCode;
            existing.OwnerName = report.OwnerName;
            existing.CallType = report.CallType;
            existing.Subject = report.Subject;
            existing.Notes = report.Notes;
            existing.FollowUpDate = report.FollowUpDate;
            existing.FollowUpComplete = report.FollowUpComplete;
            existing.SalesProjectId = report.SalesProjectId;
            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteAsync(int id)
    {
        lock (_lock)
        {
            var existing = _store.FirstOrDefault(c => c.Id == id);
            if (existing == null) return Task.FromResult(false);
            _store.Remove(existing);
            return Task.FromResult(true);
        }
    }

    public Task<List<CallReport>> GetByContactAsync(int contactId)
    {
        lock (_lock)
        {
            return Task.FromResult(_store
                .Where(c => c.ContactId == contactId)
                .OrderByDescending(c => c.CallDate)
                .ToList());
        }
    }

    public Task<List<CallReport>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        lock (_lock)
        {
            return Task.FromResult(_store
                .Where(c => c.CallDate >= from && c.CallDate <= to)
                .OrderByDescending(c => c.CallDate)
                .ToList());
        }
    }

    /// <summary>
    /// Returns outstanding (not yet complete) follow-ups, optionally filtered by user.
    /// </summary>
    public Task<List<CallReport>> GetOpenFollowUpsAsync(string? userCode = null)
    {
        lock (_lock)
        {
            return Task.FromResult(_store
                .Where(c => c.FollowUpDate.HasValue && !c.FollowUpComplete)
                .Where(c => userCode == null || c.OwnerUserCode == userCode)
                .OrderBy(c => c.FollowUpDate)
                .ToList());
        }
    }

    private static List<CallReport> SeedData()
    {
        return new List<CallReport>
        {
            new()
            {
                Id = 1,
                CallDate = DateTime.Now.AddDays(-2),
                ContactId = 501, ContactName = "Jane Smith",
                CompanyId = 9001, CompanyName = "VicRoads",
                OwnerUserCode = "SR", OwnerName = "Sam Rep",
                CallType = CallType.Phone,
                Subject = "Discussed M1 VMS rollout timeline",
                Notes = "Customer keen, wants quote within 2 weeks. Budget around $300k.",
                FollowUpDate = DateTime.Today.AddDays(7),
                SalesProjectId = 1
            },
            new()
            {
                Id = 2,
                CallDate = DateTime.Now.AddDays(-5),
                ContactId = 502, ContactName = "Bob Lee",
                CompanyId = 9002, CompanyName = "Brisbane City Council",
                OwnerUserCode = "PR", OwnerName = "Pat Rep",
                CallType = CallType.Visit,
                Subject = "Site visit — bollard locations",
                Notes = "Visited 8 sites. Will email site map.",
                FollowUpDate = DateTime.Today.AddDays(-1), // overdue
                SalesProjectId = 2
            },
            new()
            {
                Id = 3,
                CallDate = DateTime.Now.AddDays(-10),
                ContactId = 503, ContactName = "Mary Wong",
                CompanyId = 9003, CompanyName = "Transurban",
                OwnerUserCode = "SR", OwnerName = "Sam Rep",
                CallType = CallType.Meeting,
                Subject = "Contract kick-off",
                Notes = "Reviewed deliverables. All on track.",
                FollowUpDate = DateTime.Today.AddDays(30),
                FollowUpComplete = false,
                SalesProjectId = 3
            },
            new()
            {
                Id = 4,
                CallDate = DateTime.Now.AddDays(-1),
                ContactId = 504, ContactName = "Tom Allen",
                CompanyId = 9004, CompanyName = "Acme Civil",
                OwnerUserCode = "PR", OwnerName = "Pat Rep",
                CallType = CallType.Email,
                Subject = "Quote follow-up",
                Notes = "Sent revised pricing.",
                FollowUpDate = DateTime.Today, // due today
                SalesProjectId = null
            }
        };
    }
}
