using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace MyDesk.Web.Services;

public class BudgetService
{
    private readonly DatabaseService _db;
    private readonly ComplianceAuditService _audit;

    public BudgetService(DatabaseService db, ComplianceAuditService audit)
    {
        _db = db;
        _audit = audit;
    }

    public async Task<DataRow?> GetBudgetAsync(int tenantId, int departmentId, int fiscalYear)
    {
        var dt = await _db.QueryAsync(
            @"SELECT BudgetId, TenantId, DepartmentId, FiscalYear, AllocatedAmount,
                     SpentAmount, EncumberedAmount, AllowOverspend, ThresholdAlertPercentage,
                     CatExpense, CatTravel, CatMeals, CatOther, [Status], CreatedAt, UpdatedAt
              FROM DepartmentBudgets
              WHERE TenantId = @TenantId AND DepartmentId = @DepartmentId AND FiscalYear = @FiscalYear",
            new()
            {
                ["TenantId"] = tenantId,
                ["DepartmentId"] = departmentId,
                ["FiscalYear"] = fiscalYear
            });

        return dt.Rows.Count > 0 ? dt.Rows[0] : null;
    }

    public async Task<int> CreateBudgetAsync(int tenantId, int departmentId, int fiscalYear,
        decimal allocatedAmount, bool allowOverspend = false, int thresholdPercent = 80)
    {
        if (allocatedAmount <= 0)
            throw new ArgumentException("Budget amount must be positive");

        await _db.ExecuteNonQueryAsync(
            @"INSERT INTO DepartmentBudgets (TenantId, DepartmentId, FiscalYear, AllocatedAmount,
                                            AllowOverspend, ThresholdAlertPercentage)
              VALUES (@TenantId, @DeptId, @Year, @Amount, @AllowOverspend, @Threshold)",
            new()
            {
                ["TenantId"] = tenantId,
                ["DeptId"] = departmentId,
                ["Year"] = fiscalYear,
                ["Amount"] = allocatedAmount,
                ["AllowOverspend"] = allowOverspend ? 1 : 0,
                ["Threshold"] = thresholdPercent
            });

        var dtId = await _db.QueryAsync(
            "SELECT MAX(BudgetId) as Id FROM DepartmentBudgets WHERE TenantId = @TenantId",
            new() { ["TenantId"] = tenantId });

        int budgetId = (int)dtId.Rows[0]["Id"];

        await _audit.LogAsync("BudgetCreated", "System", new
        {
            tenantId,
            departmentId,
            fiscalYear,
            allocatedAmount,
            allowOverspend
        });

        return budgetId;
    }

    public async Task AddExpenseAsync(int tenantId, int departmentId, int fiscalYear,
        decimal amount, string category)
    {
        var budget = await GetBudgetAsync(tenantId, departmentId, fiscalYear);
        if (budget == null)
            throw new InvalidOperationException("Budget not found");

        var catColumn = category switch
        {
            "Travel" => "CatTravel",
            "Meals" => "CatMeals",
            "Expense" => "CatExpense",
            _ => "CatOther"
        };

        await _db.ExecuteNonQueryAsync(
            $@"UPDATE DepartmentBudgets
               SET SpentAmount = SpentAmount + @Amount,
                   {catColumn} = {catColumn} + @Amount,
                   UpdatedAt = GETUTCDATE()
               WHERE TenantId = @TenantId AND DepartmentId = @DepartmentId AND FiscalYear = @FiscalYear",
            new()
            {
                ["Amount"] = amount,
                ["TenantId"] = tenantId,
                ["DepartmentId"] = departmentId,
                ["FiscalYear"] = fiscalYear
            });

        await _audit.LogAsync("BudgetExpenseAdded", "System", new
        {
            tenantId,
            departmentId,
            fiscalYear,
            amount,
            category
        });
    }

    public async Task EncumberAmountAsync(int tenantId, int departmentId, int fiscalYear, decimal amount)
    {
        await _db.ExecuteNonQueryAsync(
            @"UPDATE DepartmentBudgets
               SET EncumberedAmount = EncumberedAmount + @Amount,
                   UpdatedAt = GETUTCDATE()
               WHERE TenantId = @TenantId AND DepartmentId = @DepartmentId AND FiscalYear = @FiscalYear",
            new()
            {
                ["Amount"] = amount,
                ["TenantId"] = tenantId,
                ["DepartmentId"] = departmentId,
                ["FiscalYear"] = fiscalYear
            });
    }

    public async Task<bool> CanApproveAsync(int tenantId, int departmentId, int fiscalYear, decimal amount)
    {
        var budget = await GetBudgetAsync(tenantId, departmentId, fiscalYear);
        if (budget == null)
            return true;  // No budget defined, allow

        decimal allocated = (decimal)budget["AllocatedAmount"];
        decimal spent = (decimal)budget["SpentAmount"];
        decimal encumbered = (decimal)budget["EncumberedAmount"];
        bool allowOverspend = (bool)budget["AllowOverspend"];

        decimal remaining = allocated - spent - encumbered;

        if (allowOverspend)
            return true;

        return amount <= remaining;
    }

    public async Task<decimal> GetRemainingBudgetAsync(int tenantId, int departmentId, int fiscalYear)
    {
        var budget = await GetBudgetAsync(tenantId, departmentId, fiscalYear);
        if (budget == null)
            return decimal.MaxValue;

        decimal allocated = (decimal)budget["AllocatedAmount"];
        decimal spent = (decimal)budget["SpentAmount"];
        decimal encumbered = (decimal)budget["EncumberedAmount"];

        return allocated - spent - encumbered;
    }

    public async Task<int> GetBudgetAlertPercentageAsync(int tenantId, int departmentId, int fiscalYear)
    {
        var budget = await GetBudgetAsync(tenantId, departmentId, fiscalYear);
        if (budget == null)
            return 100;

        decimal allocated = (decimal)budget["AllocatedAmount"];
        decimal spent = (decimal)budget["SpentAmount"];

        return (int)((spent / allocated) * 100);
    }

    public async Task<DataTable> GetDepartmentBudgetsAsync(int tenantId, int? departmentId = null)
    {
        var query = @"SELECT BudgetId, TenantId, DepartmentId, FiscalYear, AllocatedAmount,
                            SpentAmount, EncumberedAmount, AllowOverspend, ThresholdAlertPercentage,
                            [Status], CreatedAt, UpdatedAt
                     FROM DepartmentBudgets
                     WHERE TenantId = @TenantId";

        var parms = new Dictionary<string, object> { ["TenantId"] = tenantId };

        if (departmentId.HasValue)
        {
            query += " AND DepartmentId = @DepartmentId";
            parms["DepartmentId"] = departmentId.Value;
        }

        query += " ORDER BY FiscalYear DESC, DepartmentId";

        return await _db.QueryAsync(query, parms);
    }
}
