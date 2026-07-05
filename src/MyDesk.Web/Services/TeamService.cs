using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MyDesk.Web.Services;

public class TeamService
{
    private readonly DatabaseService _db;
    private readonly ComplianceAuditService _audit;

    public TeamService(DatabaseService db, ComplianceAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<DataTable> GetTeamsAsync(int tenantId, int? departmentId = null)
    {
        var query = @"SELECT TeamId, TenantId, DepartmentId, [Name], [Description],
                            TeamLeadUserId, [Status], IsApprovalTeam, CreatedAt, UpdatedAt
                     FROM Teams
                     WHERE TenantId = @TenantId";

        var parms = new Dictionary<string, object> { ["TenantId"] = tenantId };

        if (departmentId.HasValue)
        {
            query += " AND DepartmentId = @DepartmentId";
            parms["DepartmentId"] = departmentId.Value;
        }

        query += " ORDER BY [Name]";

        return await _db.QueryAsync(query, parms);
    }

    public async Task<DataRow?> GetTeamAsync(int tenantId, int teamId)
    {
        var dt = await _db.QueryAsync(
            @"SELECT TeamId, TenantId, DepartmentId, [Name], [Description],
                     TeamLeadUserId, [Status], IsApprovalTeam, CreatedAt, UpdatedAt
              FROM Teams
              WHERE TenantId = @TenantId AND TeamId = @TeamId",
            new() { ["TenantId"] = tenantId, ["TeamId"] = teamId });
        return dt.Rows.Count > 0 ? dt.Rows[0] : null;
    }

    public async Task<int> CreateTeamAsync(int tenantId, int departmentId, string name,
        string? description = null, int? teamLeadUserId = null, bool isApprovalTeam = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Team name required");

        await _db.ExecuteNonQueryAsync(
            @"INSERT INTO Teams (TenantId, DepartmentId, [Name], [Description],
                                TeamLeadUserId, IsApprovalTeam)
              VALUES (@TenantId, @DeptId, @Name, @Desc, @LeadId, @IsApprovalTeam)",
            new()
            {
                ["TenantId"] = tenantId,
                ["DeptId"] = departmentId,
                ["Name"] = name,
                ["Desc"] = (object?)description ?? DBNull.Value,
                ["LeadId"] = (object?)teamLeadUserId ?? DBNull.Value,
                ["IsApprovalTeam"] = isApprovalTeam ? 1 : 0
            });

        var dtId = await _db.QueryAsync(
            "SELECT MAX(TeamId) as Id FROM Teams WHERE TenantId = @TenantId",
            new() { ["TenantId"] = tenantId });

        int teamId = (int)dtId.Rows[0]["Id"];

        await _audit.LogAsync("TeamCreated", "System", new
        {
            tenantId,
            teamId,
            teamName = name,
            departmentId,
            teamLeadUserId,
            isApprovalTeam
        });

        return teamId;
    }

    public async Task UpdateTeamAsync(int tenantId, int teamId, string? name = null,
        string? description = null, int? teamLeadUserId = null, bool? isApprovalTeam = null)
    {
        var team = await GetTeamAsync(tenantId, teamId);
        if (team == null)
            throw new InvalidOperationException("Team not found");

        name ??= team["Name"].ToString();

        await _db.ExecuteNonQueryAsync(
            @"UPDATE Teams
              SET [Name] = @Name, [Description] = @Desc, TeamLeadUserId = @LeadId,
                  IsApprovalTeam = @IsApprovalTeam, UpdatedAt = GETUTCDATE()
              WHERE TenantId = @TenantId AND TeamId = @TeamId",
            new()
            {
                ["Name"] = name,
                ["Desc"] = (object?)description ?? DBNull.Value,
                ["LeadId"] = (object?)teamLeadUserId ?? DBNull.Value,
                ["IsApprovalTeam"] = (isApprovalTeam.HasValue ? (isApprovalTeam.Value ? 1 : 0) : (int)team["IsApprovalTeam"]),
                ["TenantId"] = tenantId,
                ["TeamId"] = teamId
            });

        await _audit.LogAsync("TeamUpdated", "System", new
        {
            tenantId,
            teamId,
            name,
            teamLeadUserId,
            isApprovalTeam
        });
    }

    public async Task AddTeamMemberAsync(int tenantId, int teamId, int userId, string role = "Member")
    {
        if (!new[] { "Member", "Lead", "Manager" }.Contains(role))
            throw new ArgumentException($"Invalid role: {role}");

        var existing = await _db.QueryAsync(
            "SELECT TeamMemberId FROM TeamMembers WHERE TeamId = @TeamId AND UserId = @UserId",
            new() { ["TeamId"] = teamId, ["UserId"] = userId });

        if (existing.Rows.Count > 0)
            throw new InvalidOperationException("User is already a member of this team");

        await _db.ExecuteNonQueryAsync(
            @"INSERT INTO TeamMembers (TenantId, TeamId, UserId, [Role])
              VALUES (@TenantId, @TeamId, @UserId, @Role)",
            new()
            {
                ["TenantId"] = tenantId,
                ["TeamId"] = teamId,
                ["UserId"] = userId,
                ["Role"] = role
            });

        await _audit.LogAsync("TeamMemberAdded", "System", new
        {
            tenantId,
            teamId,
            userId,
            role
        });
    }

    public async Task RemoveTeamMemberAsync(int tenantId, int teamId, int userId)
    {
        await _db.ExecuteNonQueryAsync(
            @"DELETE FROM TeamMembers
              WHERE TenantId = @TenantId AND TeamId = @TeamId AND UserId = @UserId",
            new()
            {
                ["TenantId"] = tenantId,
                ["TeamId"] = teamId,
                ["UserId"] = userId
            });

        await _audit.LogAsync("TeamMemberRemoved", "System", new
        {
            tenantId,
            teamId,
            userId
        });
    }

    public async Task<DataTable> GetTeamMembersAsync(int tenantId, int teamId)
    {
        return await _db.QueryAsync(
            @"SELECT tm.TeamMemberId, tm.TenantId, tm.TeamId, tm.UserId, tm.[Role],
                     tm.[Status], tm.JoinedAt, u.[Name], u.Email
              FROM TeamMembers tm
              JOIN Users u ON tm.UserId = u.UserId
              WHERE tm.TenantId = @TenantId AND tm.TeamId = @TeamId
              ORDER BY u.[Name]",
            new() { ["TenantId"] = tenantId, ["TeamId"] = teamId });
    }

    public async Task<DataTable> GetUserTeamsAsync(int tenantId, int userId)
    {
        return await _db.QueryAsync(
            @"SELECT t.TeamId, t.TenantId, t.DepartmentId, t.[Name],
                     tm.[Role], tm.JoinedAt, d.[Name] as DepartmentName
              FROM TeamMembers tm
              JOIN Teams t ON tm.TeamId = t.TeamId
              JOIN Departments d ON t.DepartmentId = d.DepartmentId
              WHERE tm.TenantId = @TenantId AND tm.UserId = @UserId
              ORDER BY t.[Name]",
            new() { ["TenantId"] = tenantId, ["UserId"] = userId });
    }

    public async Task<List<int>> GetTeamUserIdsAsync(int tenantId, int teamId)
    {
        var dt = await _db.QueryAsync(
            @"SELECT UserId FROM TeamMembers
              WHERE TenantId = @TenantId AND TeamId = @TeamId",
            new() { ["TenantId"] = tenantId, ["TeamId"] = teamId });

        return dt.Rows.Cast<DataRow>().Select(r => (int)r["UserId"]).ToList();
    }
}
