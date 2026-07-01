using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

/// <summary>
/// Manages strategic Business Goals + KPIs + Roadmap docs + AI coaching check-ins.
/// Restricted to Director / Administrator roles at the page level (page-level @attribute [Authorize]).
/// </summary>
public class BusinessGoalsService
{
    private readonly DatabaseService _db;

    public BusinessGoalsService(DatabaseService db) => _db = db;

    public async Task<List<BusinessGoal>> GetGoalsAsync(string? status = null)
    {
        var sql = @"SELECT * FROM BusinessGoals WHERE 1=1"
                + (string.IsNullOrEmpty(status) ? "" : " AND Status = @Status")
                + " ORDER BY CASE Priority WHEN 'Critical' THEN 1 WHEN 'High' THEN 2 WHEN 'Medium' THEN 3 ELSE 4 END, TargetDate";
        var goals = (await _db.QueryAsync<BusinessGoal>(sql, new { Status = status })).ToList();
        if (goals.Count == 0) return goals;

        var goalIds = goals.Select(g => g.GoalId).ToArray();
        var kpis = (await _db.QueryAsync<BusinessGoalKpi>(
            "SELECT * FROM BusinessGoalKpis WHERE GoalId IN @Ids ORDER BY GoalId, KpiId",
            new { Ids = goalIds })).ToList();
        foreach (var g in goals) g.Kpis = kpis.Where(k => k.GoalId == g.GoalId).ToList();
        return goals;
    }

    public async Task<BusinessGoal?> GetGoalAsync(int id)
    {
        var goal = await _db.QueryFirstOrDefaultAsync<BusinessGoal>(
            "SELECT * FROM BusinessGoals WHERE GoalId = @Id", new { Id = id });
        if (goal == null) return null;
        goal.Kpis = (await _db.QueryAsync<BusinessGoalKpi>(
            "SELECT * FROM BusinessGoalKpis WHERE GoalId = @Id ORDER BY KpiId", new { Id = id })).ToList();
        return goal;
    }

    public async Task<int> CreateGoalAsync(BusinessGoal g)
    {
        const string sql = @"INSERT INTO BusinessGoals
            (Title, Description, Category, HorizonYears, TargetDate, StartDate, OwnerUserId, OwnerName,
             Priority, Status, ProgressPercent, Confidence, SuccessCriteria, AiCoachingEnabled, CreatedBy)
            VALUES (@Title, @Description, @Category, @HorizonYears, @TargetDate, @StartDate, @OwnerUserId, @OwnerName,
                    @Priority, @Status, @ProgressPercent, @Confidence, @SuccessCriteria, @AiCoachingEnabled, @CreatedBy);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        return await _db.ExecuteScalarAsync<int>(sql, g);
    }

    public async Task UpdateGoalAsync(BusinessGoal g) =>
        await _db.ExecuteObjAsync(@"UPDATE BusinessGoals SET
            Title=@Title, Description=@Description, Category=@Category, HorizonYears=@HorizonYears,
            TargetDate=@TargetDate, OwnerUserId=@OwnerUserId, OwnerName=@OwnerName, Priority=@Priority,
            Status=@Status, ProgressPercent=@ProgressPercent, Confidence=@Confidence,
            SuccessCriteria=@SuccessCriteria, AiCoachingEnabled=@AiCoachingEnabled, UpdatedAt=GETDATE()
            WHERE GoalId=@GoalId", g);

    public async Task DeleteGoalAsync(int id) =>
        await _db.ExecuteObjAsync("DELETE FROM BusinessGoals WHERE GoalId = @Id", new { Id = id });

    // ------ KPIs ------
    public async Task<int> AddKpiAsync(BusinessGoalKpi k)
    {
        const string sql = @"INSERT INTO BusinessGoalKpis
            (GoalId, Name, MetricType, DataSource, DataKey, BaselineValue, TargetValue, CurrentValue, Unit,
             Direction, UpdateCadence, Status)
            VALUES (@GoalId, @Name, @MetricType, @DataSource, @DataKey, @BaselineValue, @TargetValue, @CurrentValue, @Unit,
                    @Direction, @UpdateCadence, @Status);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        return await _db.ExecuteScalarAsync<int>(sql, k);
    }

    public async Task UpdateKpiAsync(BusinessGoalKpi k) =>
        await _db.ExecuteObjAsync(@"UPDATE BusinessGoalKpis SET
            Name=@Name, MetricType=@MetricType, DataSource=@DataSource, DataKey=@DataKey,
            BaselineValue=@BaselineValue, TargetValue=@TargetValue, CurrentValue=@CurrentValue, Unit=@Unit,
            Direction=@Direction, UpdateCadence=@UpdateCadence, LastUpdated=GETDATE(), Status=@Status,
            UpdatedAt=GETDATE() WHERE KpiId=@KpiId", k);

    public async Task DeleteKpiAsync(int kpiId) =>
        await _db.ExecuteObjAsync("DELETE FROM BusinessGoalKpis WHERE KpiId = @Id", new { Id = kpiId });

    // ------ Roadmap docs ------
    public async Task<List<BusinessRoadmap>> GetRoadmapsAsync(int? goalId = null)
    {
        var sql = "SELECT * FROM BusinessRoadmaps"
                + (goalId.HasValue ? " WHERE GoalId = @GoalId" : "")
                + " ORDER BY UploadedAt DESC";
        return (await _db.QueryAsync<BusinessRoadmap>(sql, new { GoalId = goalId })).ToList();
    }

    public async Task<int> AddRoadmapAsync(BusinessRoadmap r)
    {
        const string sql = @"INSERT INTO BusinessRoadmaps
            (GoalId, Title, Description, FromYear, ToYear, FilePath, FileName, ContentType, SizeBytes,
             UploadedBy, IsCurrent, Notes)
            VALUES (@GoalId, @Title, @Description, @FromYear, @ToYear, @FilePath, @FileName, @ContentType, @SizeBytes,
                    @UploadedBy, @IsCurrent, @Notes);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        return await _db.ExecuteScalarAsync<int>(sql, r);
    }

    public async Task DeleteRoadmapAsync(int id) =>
        await _db.ExecuteObjAsync("DELETE FROM BusinessRoadmaps WHERE RoadmapId = @Id", new { Id = id });

    // ------ Check-ins / AI coaching ------
    public async Task<List<GoalCheckIn>> GetCheckInsAsync(int goalId, int take = 20) =>
        (await _db.QueryAsync<GoalCheckIn>(
            "SELECT TOP " + take + " * FROM GoalCheckIns WHERE GoalId = @Id ORDER BY CheckInDate DESC",
            new { Id = goalId })).ToList();

    public async Task<int> AddCheckInAsync(GoalCheckIn c)
    {
        const string sql = @"INSERT INTO GoalCheckIns
            (GoalId, CheckInDate, Source, AuthorName, ProgressPercent, Status, Summary, Recommendation, DiscrepancyJson)
            VALUES (@GoalId, @CheckInDate, @Source, @AuthorName, @ProgressPercent, @Status, @Summary, @Recommendation, @DiscrepancyJson);
            SELECT CAST(SCOPE_IDENTITY() AS INT);";
        return await _db.ExecuteScalarAsync<int>(sql, c);
    }

    /// <summary>
    /// Generates a deterministic coaching nudge based on KPI vs target progress.
    /// Replace with a Semantic Kernel plugin call once the AI agent is wired.
    /// </summary>
    public string DraftCoachingNudge(BusinessGoal goal)
    {
        if (goal.Kpis.Count == 0)
            return $"No KPIs are tracked against \"{goal.Title}\" — without KPIs the AI can't measure drift. Consider adding 1–3 measurable indicators.";

        var lagging = goal.Kpis.Where(k => k.ProgressPercent < 50).ToList();
        if (lagging.Count == 0)
            return $"All {goal.Kpis.Count} KPIs are tracking healthily. Maintain current cadence and review next month.";

        var worst = lagging.OrderBy(k => k.ProgressPercent).First();
        return $"\"{worst.Name}\" is at {worst.ProgressPercent}% of target ({worst.CurrentValue:N0} of {worst.TargetValue:N0}). " +
               $"This is the most off-track KPI for \"{goal.Title}\". " +
               $"Recommend a focused weekly stand-up on this metric until it crosses 75%.";
    }
}
