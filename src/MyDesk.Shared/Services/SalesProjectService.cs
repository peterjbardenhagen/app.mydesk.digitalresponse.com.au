using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

/// <summary>
/// In-memory Sales Projects (sales pipeline) service ported from legacy MyDesk.
/// TODO: persist to database
/// </summary>
public class SalesProjectService
{
    private readonly ILogger<SalesProjectService> _logger;
    private static readonly object _lock = new();
    private static readonly List<SalesProject> _store = SeedData();

    public SalesProjectService(ILogger<SalesProjectService> logger)
    {
        _logger = logger;
    }

    public Task<List<SalesProject>> GetAllAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_store
                .OrderByDescending(p => p.CreatedDate)
                .ToList());
        }
    }

    public Task<SalesProject?> GetByIdAsync(int id)
    {
        lock (_lock)
        {
            return Task.FromResult(_store.FirstOrDefault(p => p.Id == id));
        }
    }

    public Task<SalesProject> CreateAsync(SalesProject project)
    {
        lock (_lock)
        {
            project.Id = _store.Count == 0 ? 1 : _store.Max(p => p.Id) + 1;
            project.CreatedDate = DateTime.Now;
            _store.Add(project);
            _logger.LogInformation("Sales project {Name} created (id={Id})", project.ProjectName, project.Id);
            return Task.FromResult(project);
        }
    }

    public Task<bool> UpdateAsync(SalesProject project)
    {
        lock (_lock)
        {
            var existing = _store.FirstOrDefault(p => p.Id == project.Id);
            if (existing == null) return Task.FromResult(false);
            existing.ProjectName = project.ProjectName;
            existing.Customer = project.Customer;
            existing.EstimatedValue = project.EstimatedValue;
            existing.ProbabilityPercent = project.ProbabilityPercent;
            var stageChanged = existing.Stage != project.Stage;
            existing.Stage = project.Stage;
            existing.ExpectedCloseDate = project.ExpectedCloseDate;
            existing.ActualCloseDate = project.ActualCloseDate;
            existing.OwnerUserCode = project.OwnerUserCode;
            existing.OwnerName = project.OwnerName;
            existing.Notes = project.Notes;
            existing.LinkedQuoteIds = project.LinkedQuoteIds;
            existing.LinkedInvoiceIds = project.LinkedInvoiceIds;
            existing.LinkedPoIds = project.LinkedPoIds;
            if (stageChanged && (project.Stage == SalesStage.Won || project.Stage == SalesStage.Lost))
                existing.ActualCloseDate ??= DateTime.Today;
            return Task.FromResult(true);
        }
    }

    public Task<bool> DeleteAsync(int id)
    {
        lock (_lock)
        {
            var existing = _store.FirstOrDefault(p => p.Id == id);
            if (existing == null) return Task.FromResult(false);
            _store.Remove(existing);
            return Task.FromResult(true);
        }
    }

    public Task<List<SalesProject>> GetByStatusAsync(SalesStage stage)
    {
        lock (_lock)
        {
            return Task.FromResult(_store.Where(p => p.Stage == stage).ToList());
        }
    }

    public Task<WinLossStats> GetWinLossStatsAsync()
    {
        lock (_lock)
        {
            var won = _store.Where(p => p.Stage == SalesStage.Won).ToList();
            var lost = _store.Where(p => p.Stage == SalesStage.Lost).ToList();
            var stats = new WinLossStats
            {
                WonCount = won.Count,
                LostCount = lost.Count,
                WonValue = won.Sum(p => p.EstimatedValue),
                LostValue = lost.Sum(p => p.EstimatedValue)
            };
            return Task.FromResult(stats);
        }
    }

    private static List<SalesProject> SeedData()
    {
        return new List<SalesProject>
        {
            new()
            {
                Id = 1,
                ProjectName = "M1 Freeway VMS rollout",
                Customer = "VicRoads",
                EstimatedValue = 320_000m,
                ProbabilityPercent = 70,
                Stage = SalesStage.Proposal,
                ExpectedCloseDate = DateTime.Today.AddDays(35),
                OwnerName = "Sam Rep",
                OwnerUserCode = "SR",
                Notes = "Strong interest, awaiting board sign-off.",
                CreatedDate = DateTime.Now.AddDays(-25),
                LinkedQuoteIds = new() { 1001 }
            },
            new()
            {
                Id = 2,
                ProjectName = "City Council bollard refresh",
                Customer = "Brisbane City Council",
                EstimatedValue = 145_000m,
                ProbabilityPercent = 40,
                Stage = SalesStage.Qualified,
                ExpectedCloseDate = DateTime.Today.AddDays(60),
                OwnerName = "Pat Rep",
                OwnerUserCode = "PR",
                CreatedDate = DateTime.Now.AddDays(-10)
            },
            new()
            {
                Id = 3,
                ProjectName = "WestConnex maintenance contract",
                Customer = "Transurban",
                EstimatedValue = 580_000m,
                ProbabilityPercent = 100,
                Stage = SalesStage.Won,
                ExpectedCloseDate = DateTime.Today.AddDays(-30),
                ActualCloseDate = DateTime.Today.AddDays(-15),
                OwnerName = "Sam Rep",
                OwnerUserCode = "SR",
                CreatedDate = DateTime.Now.AddDays(-90),
                LinkedQuoteIds = new() { 1015 },
                LinkedInvoiceIds = new() { 5550 }
            },
            new()
            {
                Id = 4,
                ProjectName = "QLD truck mounted attenuator hire",
                Customer = "Acme Civil",
                EstimatedValue = 95_000m,
                ProbabilityPercent = 0,
                Stage = SalesStage.Lost,
                ExpectedCloseDate = DateTime.Today.AddDays(-20),
                ActualCloseDate = DateTime.Today.AddDays(-18),
                OwnerName = "Pat Rep",
                OwnerUserCode = "PR",
                Notes = "Lost on price.",
                CreatedDate = DateTime.Now.AddDays(-55)
            },
            new()
            {
                Id = 5,
                ProjectName = "Smart traffic light pilot",
                Customer = "Department of Transport",
                EstimatedValue = 220_000m,
                ProbabilityPercent = 25,
                Stage = SalesStage.Lead,
                ExpectedCloseDate = DateTime.Today.AddDays(120),
                OwnerName = "Sam Rep",
                OwnerUserCode = "SR",
                CreatedDate = DateTime.Now.AddDays(-3)
            }
        };
    }
}
