using System.Data;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

public class ExpenseService
{
    private readonly DatabaseService _db;
    private readonly ILogger<ExpenseService> _logger;

    public ExpenseService(DatabaseService db, ILogger<ExpenseService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task EnsureTableAsync()
    {
        var sql = @"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Expenses')
            BEGIN
                CREATE TABLE Expenses (
                    ExpenseId INT IDENTITY(1,1) PRIMARY KEY,
                    Date DATETIME NOT NULL DEFAULT GETDATE(),
                    Description NVARCHAR(500) NOT NULL,
                    Amount DECIMAL(18,2) NOT NULL DEFAULT 0,
                    GST DECIMAL(18,2) NOT NULL DEFAULT 0,
                    Total DECIMAL(18,2) NOT NULL DEFAULT 0,
                    SupplierId INT NULL,
                    Category NVARCHAR(100) NULL,
                    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
                    FileName NVARCHAR(255) NULL,
                    FilePath NVARCHAR(MAX) NULL,
                    AIProcessingResult NVARCHAR(MAX) NULL,
                    CreatedBy NVARCHAR(100) NULL,
                    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                );
                CREATE INDEX IX_Expenses_Date ON Expenses(Date DESC);
            END";
        await _db.ExecuteAsync(sql);
    }

    public async Task<List<Expense>> GetExpensesAsync(string? searchTerm = null)
    {
        var sql = @"
            SELECT e.*, c.Company AS SupplierName
            FROM Expenses e
            LEFT JOIN Companies c ON e.ContactId = c.CompanyId
            WHERE 1=1";
        
        var parameters = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            sql += " AND (e.Description LIKE @search OR c.Company LIKE @search OR e.Code LIKE @search)";
            parameters["search"] = $"%{searchTerm}%";
        }
        sql += " ORDER BY e.ExpenseDate DESC";

        var dt = await _db.QueryAsync(sql, parameters);
        return dt.Map(r => new Expense
        {
            ExpenseId = Convert.ToInt32(r["Eid"]),
            Date = Convert.ToDateTime(r["ExpenseDate"]),
            Description = r["Description"]?.ToString() ?? "",
            Amount = Convert.ToDecimal(r["CostIncGST"]), // Using CostIncGST as the main amount
            GST = Convert.ToDecimal(r["GST"]),
            Total = Convert.ToDecimal(r["CostIncGST"]),
            SupplierId = r["ContactId"] == DBNull.Value ? null : Convert.ToInt32(r["ContactId"]),
            SupplierName = r["SupplierName"]?.ToString(),
            Category = r["Code"]?.ToString() ?? "General", // Using Code as Category
            Status = "Paid", // Assuming paid for existing records or handle as needed
            FileName = r["Receipt"]?.ToString(),
            CreatedBy = r["ContactId"]?.ToString(),
            CreatedAt = Convert.ToDateTime(r["DateEntered"])
        });
    }

    public async Task<Expense?> GetExpenseAsync(int id)
    {
        var sql = "SELECT * FROM Expenses WHERE Eid = @Id";
        var dt = await _db.QueryAsync(sql, new() { ["Id"] = id });
        var rows = dt.Map(r => new Expense
        {
            ExpenseId = Convert.ToInt32(r["Eid"]),
            Date = Convert.ToDateTime(r["ExpenseDate"]),
            Description = r["Description"]?.ToString() ?? "",
            Amount = Convert.ToDecimal(r["CostIncGST"]),
            GST = Convert.ToDecimal(r["GST"]),
            Total = Convert.ToDecimal(r["CostIncGST"]),
            SupplierId = r["ContactId"] == DBNull.Value ? null : Convert.ToInt32(r["ContactId"]),
            Category = r["Code"]?.ToString() ?? "General",
            FileName = r["Receipt"]?.ToString(),
            CreatedAt = Convert.ToDateTime(r["DateEntered"])
        });
        return rows.FirstOrDefault();
    }

    public async Task<int> SaveExpenseAsync(Expense expense)
    {
        if (expense.ExpenseId == 0)
        {
            var sql = @"
                INSERT INTO Expenses (ExpenseDate, Description, CostIncGST, GST, ContactId, Code, FileName, DateEntered)
                VALUES (@Date, @Description, @Amount, @GST, @SupplierId, @Category, @FileName, GETDATE());
                SELECT CAST(SCOPE_IDENTITY() AS int);";
            return await _db.ScalarAsync<int>(sql, MapParameters(expense));
        }
        else
        {
            var sql = @"
                UPDATE Expenses SET 
                    ExpenseDate = @Date, Description = @Description, CostIncGST = @Amount, GST = @GST, 
                    ContactId = @SupplierId, Code = @Category, FileName = @FileName
                WHERE Eid = @ExpenseId";
            await _db.ExecuteAsync(sql, MapParameters(expense));
            return expense.ExpenseId;
        }
    }

    public async Task DeleteExpenseAsync(int id)
    {
        await _db.ExecuteAsync("DELETE FROM Expenses WHERE Eid = @Id", new() { ["Id"] = id });
    }

    private Dictionary<string, object?> MapParameters(Expense e) => new()
    {
        ["ExpenseId"] = e.ExpenseId,
        ["Date"] = e.Date,
        ["Description"] = e.Description,
        ["Amount"] = e.Amount,
        ["GST"] = e.GST,
        ["SupplierId"] = (object?)e.SupplierId ?? DBNull.Value,
        ["Category"] = e.Category,
        ["FileName"] = (object?)e.FileName ?? DBNull.Value
    };
}
