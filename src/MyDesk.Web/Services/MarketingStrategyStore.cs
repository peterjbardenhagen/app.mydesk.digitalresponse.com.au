using System.Text.Json;
using MyDesk.Shared.Models;

namespace MyDesk.Web.Services;

/// <summary>
/// JSON-backed persistence for marketing strategy documents.
/// Stored alongside Config for easy backup — not a heavy migration.
/// </summary>
public class MarketingStrategyStore
{
    private readonly string _path;
    private readonly object _lock = new();

    public MarketingStrategyStore(IWebHostEnvironment env)
    {
        var dir = Path.Combine(env.ContentRootPath, "Config");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "marketing-strategies.json");
    }

    public List<MarketingStrategy> GetAll()
    {
        lock (_lock) return Load();
    }

    public MarketingStrategy? Get(string id)
    {
        lock (_lock) return Load().FirstOrDefault(s => s.Id == id);
    }

    public MarketingStrategy Save(MarketingStrategy s)
    {
        lock (_lock)
        {
            var all = Load();
            var existing = all.FirstOrDefault(x => x.Id == s.Id);
            s.UpdatedAt = DateTime.Now;
            if (existing != null) all.Remove(existing);
            else s.CreatedAt = DateTime.Now;
            all.Add(s);
            Persist(all);
            return s;
        }
    }

    public void Delete(string id)
    {
        lock (_lock)
        {
            var all = Load();
            all.RemoveAll(s => s.Id == id);
            Persist(all);
        }
    }

    public Task<MarketingStrategy?> GetCurrentStrategyAsync()
    {
        lock (_lock) return Task.FromResult(Load().FirstOrDefault());
    }

    public Task<List<StrategicObjective>> GetObjectivesAsync()
    {
        lock (_lock) return Task.FromResult(new List<StrategicObjective>());
    }

    public Task<List<MarketingTactic>> GetTacticsAsync()
    {
        lock (_lock) return Task.FromResult(new List<MarketingTactic>());
    }

    public Task<StrategyStats?> GetStatsAsync()
    {
        lock (_lock) return Task.FromResult(new StrategyStats());
    }

    public Task<StrategicObjective?> CreateObjectiveAsync(StrategicObjective objective)
    {
        lock (_lock) return Task.FromResult(objective);
    }

    public Task<StrategicObjective?> UpdateObjectiveAsync(StrategicObjective objective)
    {
        lock (_lock) return Task.FromResult(objective);
    }

    public Task<StrategicObjective?> UpdateObjectiveProgressAsync(string objectiveId, decimal progress)
    {
        lock (_lock) return Task.FromResult<StrategicObjective?>(null);
    }

    public Task<StrategicObjective?> MarkObjectiveCompleteAsync(string objectiveId)
    {
        lock (_lock) return Task.FromResult<StrategicObjective?>(null);
    }

    public Task DeleteObjectiveAsync(string objectiveId)
    {
        lock (_lock) return Task.CompletedTask;
    }

    public Task<MarketingTactic?> CreateTacticAsync(MarketingTactic tactic)
    {
        lock (_lock) return Task.FromResult(tactic);
    }

    private List<MarketingStrategy> Load()
    {
        if (!File.Exists(_path)) return new();
        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<List<MarketingStrategy>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch { return new(); }
    }

    private void Persist(List<MarketingStrategy> all)
    {
        var json = JsonSerializer.Serialize(all,
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
    }
}
