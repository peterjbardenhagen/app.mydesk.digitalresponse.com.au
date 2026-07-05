using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Services;

public class DepartmentService
{
    private readonly DatabaseService _db;
    private readonly ComplianceAuditService _audit;

    public DepartmentService(DatabaseService db, ComplianceAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<DataTable> GetDepartmentsAsync(int tenantId)
    {
        return await _db.QueryAsync(
            @"SELECT DepartmentId, TenantId, ParentDepartmentId, [Name], [Description],
                     ManagerUserId, [Status], CostCenter, CreatedAt, UpdatedAt
              FROM Departments
              WHERE TenantId = @TenantId
              ORDER BY ParentDepartmentId, [Name]",
            new() { ["TenantId"] = tenantId });
    }

    public async Task<DataRow?> GetDepartmentAsync(int tenantId, int departmentId)
    {
        var dt = await _db.QueryAsync(
            @"SELECT DepartmentId, TenantId, ParentDepartmentId, [Name], [Description],
                     ManagerUserId, [Status], CostCenter, CreatedAt, UpdatedAt
              FROM Departments
              WHERE TenantId = @TenantId AND DepartmentId = @DepartmentId",
            new() { ["TenantId"] = tenantId, ["DepartmentId"] = departmentId });
        return dt.Rows.Count > 0 ? dt.Rows[0] : null;
    }

    public async Task<int> CreateDepartmentAsync(int tenantId, string name, string? description = null,
        int? parentDepartmentId = null, int? managerUserId = null, string? costCenter = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Department name required");

        await _db.ExecuteNonQueryAsync(
            @"INSERT INTO Departments (TenantId, ParentDepartmentId, [Name], [Description],
                                       ManagerUserId, CostCenter)
              VALUES (@TenantId, @ParentId, @Name, @Desc, @ManagerId, @CostCenter)",
            new()
            {
                ["TenantId"] = tenantId,
                ["ParentId"] = (object?)parentDepartmentId ?? DBNull.Value,
                ["Name"] = name,
                ["Desc"] = (object?)description ?? DBNull.Value,
                ["ManagerId"] = (object?)managerUserId ?? DBNull.Value,
                ["CostCenter"] = (object?)costCenter ?? DBNull.Value
            });

        var dtId = await _db.QueryAsync(
            "SELECT MAX(DepartmentId) as Id FROM Departments WHERE TenantId = @TenantId",
            new() { ["TenantId"] = tenantId });

        int deptId = (int)dtId.Rows[0]["Id"];

        await _audit.LogAsync("DepartmentCreated", "System", new
        {
            tenantId,
            departmentId = deptId,
            departmentName = name,
            parentId = parentDepartmentId,
            managerId = managerUserId
        });

        return deptId;
    }

    public async Task UpdateDepartmentAsync(int tenantId, int departmentId, string? name = null,
        string? description = null, int? managerUserId = null, string? status = null)
    {
        var dept = await GetDepartmentAsync(tenantId, departmentId);
        if (dept == null)
            throw new InvalidOperationException("Department not found");

        name ??= dept["Name"].ToString();

        await _db.ExecuteNonQueryAsync(
            @"UPDATE Departments
              SET [Name] = @Name, [Description] = @Desc, ManagerUserId = @ManagerId,
                  [Status] = @Status, UpdatedAt = GETUTCDATE()
              WHERE TenantId = @TenantId AND DepartmentId = @DepartmentId",
            new()
            {
                ["Name"] = name,
                ["Desc"] = (object?)description ?? DBNull.Value,
                ["ManagerId"] = (object?)managerUserId ?? DBNull.Value,
                ["Status"] = (object?)status ?? "Active",
                ["TenantId"] = tenantId,
                ["DepartmentId"] = departmentId
            });

        await _audit.LogAsync("DepartmentUpdated", "System", new
        {
            tenantId,
            departmentId,
            name,
            managerUserId,
            status
        });
    }

    public async Task<List<int>> GetChildDepartmentsAsync(int tenantId, int parentDepartmentId)
    {
        var dt = await _db.QueryAsync(
            @"WITH DeptHierarchy AS (
                SELECT DepartmentId FROM Departments
                WHERE TenantId = @TenantId AND DepartmentId = @ParentId
                UNION ALL
                SELECT d.DepartmentId FROM Departments d
                INNER JOIN DeptHierarchy dh ON d.ParentDepartmentId = dh.DepartmentId
              )
              SELECT DepartmentId FROM DeptHierarchy WHERE DepartmentId != @ParentId",
            new() { ["TenantId"] = tenantId, ["ParentId"] = parentDepartmentId });

        return dt.Rows.Cast<DataRow>().Select(r => (int)r["DepartmentId"]).ToList();
    }

    public async Task<List<int>> GetDepartmentUsersAsync(int tenantId, int departmentId,
        bool includeSubdepartments = false)
    {
        var depts = new List<int> { departmentId };

        if (includeSubdepartments)
            depts.AddRange(await GetChildDepartmentsAsync(tenantId, departmentId));

        var deptList = string.Join(",", depts);
        var dt = await _db.QueryAsync(
            $@"SELECT DISTINCT UserId FROM Users
               WHERE TenantId = @TenantId AND PrimaryDepartmentId IN ({deptList})",
            new() { ["TenantId"] = tenantId });

        return dt.Rows.Cast<DataRow>().Select(r => (int)r["UserId"]).ToList();
    }
}
