using Microsoft.Data.SqlClient;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Services.Legal;

/// <summary>
/// Read-only connector for the Practice Evolve on-premise SQL Server database.
/// Queries the live PE schema without modifying any data.
/// </summary>
public class PracticeEvolveService
{
    private readonly PlatformSettingsService _settings;
    private readonly ILogger<PracticeEvolveService> _logger;

    public PracticeEvolveService(
        PlatformSettingsService settings,
        ILogger<PracticeEvolveService> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_settings.Current.PracticeEvolveConnectionString);

    private SqlConnection CreateConnection() =>
        new(_settings.Current.PracticeEvolveConnectionString);

    public async Task<List<PeTimesheet>> GetTimesheetsAsync(
        DateTime? from = null,
        DateTime? to = null,
        string? feeEarnerCode = null,
        CancellationToken ct = default)
    {
        if (!IsConfigured) return new();

        var results = new List<PeTimesheet>();
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync(ct);

            var sql = """
                SELECT
                    t.TimeID,
                    t.FeeEarnerCode,
                    ISNULL(u.FullName, t.FeeEarnerCode) AS FeeEarnerName,
                    t.FileNumber AS MatterNumber,
                    ISNULL(f.FileTitle, '') AS MatterName,
                    CONVERT(date, t.TimeDate) AS TimeDate,
                    t.Units,
                    ISNULL(t.ActivityCode, '') AS ActivityCode,
                    ISNULL(t.Narrative, '') AS Narrative,
                    ISNULL(t.Rate, 0) AS Rate,
                    ISNULL(t.Units * t.Rate / 10.0, 0) AS WipAmount,
                    ISNULL(t.Status, 'Draft') AS Status
                FROM   TimeSheet t
                LEFT JOIN Users u ON u.UserCode = t.FeeEarnerCode
                LEFT JOIN Files f ON f.FileNumber = t.FileNumber
                WHERE  1 = 1
                """;

            var filters = new List<string>();
            if (from.HasValue)  filters.Add("t.TimeDate >= @From");
            if (to.HasValue)    filters.Add("t.TimeDate <= @To");
            if (!string.IsNullOrWhiteSpace(feeEarnerCode)) filters.Add("t.FeeEarnerCode = @FeeEarner");

            if (filters.Count > 0)
                sql += " AND " + string.Join(" AND ", filters);

            sql += " ORDER BY t.TimeDate DESC";

            await using var cmd = new SqlCommand(sql, conn);
            if (from.HasValue)  cmd.Parameters.AddWithValue("@From",      from.Value.Date);
            if (to.HasValue)    cmd.Parameters.AddWithValue("@To",        to.Value.Date);
            if (!string.IsNullOrWhiteSpace(feeEarnerCode))
                cmd.Parameters.AddWithValue("@FeeEarner", feeEarnerCode);

            cmd.CommandTimeout = 30;
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                results.Add(new PeTimesheet
                {
                    Id            = reader["TimeID"].ToString() ?? "",
                    FeeEarnerCode = reader["FeeEarnerCode"].ToString() ?? "",
                    FeeEarnerName = reader["FeeEarnerName"].ToString() ?? "",
                    MatterNumber  = reader["MatterNumber"].ToString() ?? "",
                    MatterName    = reader["MatterName"].ToString() ?? "",
                    Date          = Convert.ToDateTime(reader["TimeDate"]),
                    Units         = Convert.ToInt32(reader["Units"]),
                    ActivityCode  = reader["ActivityCode"].ToString() ?? "",
                    Narrative     = reader["Narrative"].ToString() ?? "",
                    Rate          = Convert.ToDecimal(reader["Rate"]),
                    WipAmount     = Convert.ToDecimal(reader["WipAmount"]),
                    Status        = reader["Status"].ToString() ?? "Draft",
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PracticeEvolve GetTimesheets failed");
        }

        return results;
    }

    public async Task<List<PeMatter>> GetMattersAsync(string? search = null, CancellationToken ct = default)
    {
        if (!IsConfigured) return new();

        var results = new List<PeMatter>();
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync(ct);

            var sql = """
                SELECT TOP 200
                    f.FileNumber AS MatterNumber,
                    ISNULL(f.FileTitle, '') AS MatterName,
                    ISNULL(c.ClientName, '') AS ClientName,
                    ISNULL(u.FullName, f.ResponsibleLawyer) AS ResponsibleLawyer,
                    ISNULL(f.Status, '') AS Status
                FROM   Files f
                LEFT JOIN Clients  c ON c.ClientCode = f.ClientCode
                LEFT JOIN Users    u ON u.UserCode    = f.ResponsibleLawyer
                WHERE  1 = 1
                """;

            if (!string.IsNullOrWhiteSpace(search))
                sql += " AND (f.FileNumber LIKE @Search OR f.FileTitle LIKE @Search OR c.ClientName LIKE @Search)";

            sql += " ORDER BY f.FileNumber";

            await using var cmd = new SqlCommand(sql, conn);
            if (!string.IsNullOrWhiteSpace(search))
                cmd.Parameters.AddWithValue("@Search", $"%{search}%");

            cmd.CommandTimeout = 30;
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                results.Add(new PeMatter
                {
                    MatterNumber      = reader["MatterNumber"].ToString() ?? "",
                    MatterName        = reader["MatterName"].ToString() ?? "",
                    ClientName        = reader["ClientName"].ToString() ?? "",
                    ResponsibleLawyer = reader["ResponsibleLawyer"].ToString() ?? "",
                    Status            = reader["Status"].ToString() ?? "",
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PracticeEvolve GetMatters failed");
        }

        return results;
    }

    public async Task<PeDashboardStats> GetDashboardStatsAsync(CancellationToken ct = default)
    {
        if (!IsConfigured) return new();

        var stats = new PeDashboardStats();
        try
        {
            await using var conn = CreateConnection();
            await conn.OpenAsync(ct);

            const string sql = """
                SELECT
                    (SELECT COUNT(*) FROM Files WHERE Status = 'Open') AS OpenMatters,
                    (SELECT COUNT(*) FROM TimeSheet WHERE CONVERT(date,TimeDate) = CONVERT(date,GETDATE())) AS TodayEntries,
                    (SELECT ISNULL(SUM(Units),0) FROM TimeSheet
                     WHERE CONVERT(date,TimeDate) >= DATEADD(day,-6,CONVERT(date,GETDATE()))) AS WeekUnits,
                    (SELECT ISNULL(SUM(Units * Rate / 10.0),0) FROM TimeSheet
                     WHERE CONVERT(date,TimeDate) >= DATEADD(day,-6,CONVERT(date,GETDATE()))) AS WeekWip
                """;

            await using var cmd = new SqlCommand(sql, conn) { CommandTimeout = 10 };
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                stats.OpenMatters   = Convert.ToInt32(reader["OpenMatters"]);
                stats.TodayEntries  = Convert.ToInt32(reader["TodayEntries"]);
                stats.WeekUnits     = Convert.ToInt32(reader["WeekUnits"]);
                stats.WeekWip       = Convert.ToDecimal(reader["WeekWip"]);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PracticeEvolve GetDashboardStats failed");
        }

        return stats;
    }
}

public record PeTimesheet
{
    public string Id { get; init; } = "";
    public string FeeEarnerCode { get; init; } = "";
    public string FeeEarnerName { get; init; } = "";
    public string MatterNumber { get; init; } = "";
    public string MatterName { get; init; } = "";
    public DateTime Date { get; init; }
    public int Units { get; init; }
    public string ActivityCode { get; init; } = "";
    public string Narrative { get; init; } = "";
    public decimal Rate { get; init; }
    public decimal WipAmount { get; init; }
    public string Status { get; init; } = "Draft";
    public decimal Hours => Math.Round(Units / 10m, 2);
}

public record PeMatter
{
    public string MatterNumber { get; init; } = "";
    public string MatterName { get; init; } = "";
    public string ClientName { get; init; } = "";
    public string ResponsibleLawyer { get; init; } = "";
    public string Status { get; init; } = "";
}

public record PeDashboardStats
{
    public int OpenMatters { get; set; }
    public int TodayEntries { get; set; }
    public int WeekUnits { get; set; }
    public decimal WeekWip { get; set; }
    public decimal WeekHours => Math.Round(WeekUnits / 10m, 1);
}
