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
                    Description NVARCHAR(500) NOT NULL DEFAULT '',
                    Amount DECIMAL(18,2) NOT NULL DEFAULT 0,
                    TaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
                    GST DECIMAL(18,2) NOT NULL DEFAULT 0,
                    Total DECIMAL(18,2) NOT NULL DEFAULT 0,
                    SupplierId INT NULL,
                    SupplierName NVARCHAR(200) NULL,
                    Category NVARCHAR(100) NOT NULL DEFAULT 'General',
                    Status NVARCHAR(50) NOT NULL DEFAULT 'Pending',
                    FileName NVARCHAR(255) NULL,
                    FilePath NVARCHAR(MAX) NULL,
                    AIProcessingResult NVARCHAR(MAX) NULL,
                    CreatedBy NVARCHAR(100) NULL,
                    CreatedAt DATETIME NOT NULL DEFAULT GETDATE()
                );
                CREATE INDEX IX_Expenses_Date ON Expenses(Date DESC);
            END
            ELSE
            BEGIN
                -- Migrate old schema: rename ExpenseDate to Date
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'ExpenseDate')
                   AND NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'Date')
                BEGIN
                    EXEC sp_rename 'Expenses.ExpenseDate', 'Date', 'COLUMN';
                END

                -- Add missing columns
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'SupplierName')
                    ALTER TABLE Expenses ADD SupplierName NVARCHAR(200) NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'TaxAmount')
                    ALTER TABLE Expenses ADD TaxAmount DECIMAL(18,2) NOT NULL DEFAULT 0;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'FilePath')
                    ALTER TABLE Expenses ADD FilePath NVARCHAR(MAX) NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'AIProcessingResult')
                    ALTER TABLE Expenses ADD AIProcessingResult NVARCHAR(MAX) NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'CreatedBy')
                    ALTER TABLE Expenses ADD CreatedBy NVARCHAR(100) NULL;

                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Expenses') AND name = 'CreatedAt')
                    ALTER TABLE Expenses ADD CreatedAt DATETIME NOT NULL DEFAULT GETDATE();
            END";
        await _db.ExecuteAsync(sql);
    }

    public async Task<List<Expense>> GetExpensesAsync(string? searchTerm = null)
    {
        var sql = "SELECT * FROM Expenses WHERE 1=1";
        
        var parameters = new Dictionary<string, object?>();
        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            sql += " AND (Description LIKE @search OR SupplierName LIKE @search OR Category LIKE @search)";
            parameters["search"] = $"%{searchTerm}%";
        }
        sql += " ORDER BY Date DESC";

        var dt = await _db.QueryAsync(sql, parameters);
        return dt.Map(r => new Expense
        {
            ExpenseId = Convert.ToInt32(r["ExpenseId"]),
            Date = Convert.ToDateTime(r["Date"]),
            Description = r["Description"]?.ToString() ?? "",
            Amount = r["Amount"] != DBNull.Value ? Convert.ToDecimal(r["Amount"]) : 0,
            TaxAmount = r["TaxAmount"] != DBNull.Value ? Convert.ToDecimal(r["TaxAmount"]) : 0,
            GST = r["GST"] != DBNull.Value ? Convert.ToDecimal(r["GST"]) : 0,
            Total = r["Total"] != DBNull.Value ? Convert.ToDecimal(r["Total"]) : 0,
            SupplierId = r["SupplierId"] == DBNull.Value ? null : Convert.ToInt32(r["SupplierId"]),
            SupplierName = r["SupplierName"]?.ToString(),
            Category = r["Category"]?.ToString() ?? "General",
            Status = r["Status"]?.ToString() ?? "Pending",
            FileName = r["FileName"]?.ToString(),
            FilePath = r["FilePath"]?.ToString(),
            AIProcessingResult = r["AIProcessingResult"]?.ToString(),
            CreatedBy = r["CreatedBy"]?.ToString(),
            CreatedAt = r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]) : DateTime.Now
        });
    }

    public async Task<Expense?> GetExpenseAsync(int id)
    {
        var sql = "SELECT * FROM Expenses WHERE ExpenseId = @Id";
        var dt = await _db.QueryAsync(sql, new() { ["Id"] = id });
        var rows = dt.Map(r => new Expense
        {
            ExpenseId = Convert.ToInt32(r["ExpenseId"]),
            Date = Convert.ToDateTime(r["Date"]),
            Description = r["Description"]?.ToString() ?? "",
            Amount = r["Amount"] != DBNull.Value ? Convert.ToDecimal(r["Amount"]) : 0,
            TaxAmount = r["TaxAmount"] != DBNull.Value ? Convert.ToDecimal(r["TaxAmount"]) : 0,
            GST = r["GST"] != DBNull.Value ? Convert.ToDecimal(r["GST"]) : 0,
            Total = r["Total"] != DBNull.Value ? Convert.ToDecimal(r["Total"]) : 0,
            SupplierId = r["SupplierId"] == DBNull.Value ? null : Convert.ToInt32(r["SupplierId"]),
            SupplierName = r["SupplierName"]?.ToString(),
            Category = r["Category"]?.ToString() ?? "General",
            Status = r["Status"]?.ToString() ?? "Pending",
            FileName = r["FileName"]?.ToString(),
            FilePath = r["FilePath"]?.ToString(),
            CreatedBy = r["CreatedBy"]?.ToString(),
            CreatedAt = r["CreatedAt"] != DBNull.Value ? Convert.ToDateTime(r["CreatedAt"]) : DateTime.Now
        });
        return rows.FirstOrDefault();
    }

    public async Task<int> SaveExpenseAsync(Expense expense)
    {
        if (expense.ExpenseId == 0)
        {
            var sql = @"
                INSERT INTO Expenses (Date, Description, Amount, TaxAmount, GST, Total, SupplierId, SupplierName, Category, Status, FileName, FilePath, CreatedBy, CreatedAt)
                VALUES (@Date, @Description, @Amount, @TaxAmount, @GST, @Total, @SupplierId, @SupplierName, @Category, @Status, @FileName, @FilePath, @CreatedBy, @CreatedAt);
                SELECT CAST(SCOPE_IDENTITY() AS int);";
            return await _db.ScalarAsync<int>(sql, MapParameters(expense));
        }
        else
        {
            var sql = @"
                UPDATE Expenses SET 
                    Date = @Date, Description = @Description, Amount = @Amount, TaxAmount = @TaxAmount,
                    GST = @GST, Total = @Total, SupplierId = @SupplierId, SupplierName = @SupplierName,
                    Category = @Category, Status = @Status, FileName = @FileName, FilePath = @FilePath
                WHERE ExpenseId = @ExpenseId";
            await _db.ExecuteAsync(sql, MapParameters(expense));
            return expense.ExpenseId;
        }
    }

    public async Task DeleteExpenseAsync(int id)
    {
        await _db.ExecuteAsync("DELETE FROM Expenses WHERE ExpenseId = @Id", new() { ["Id"] = id });
    }

    private Dictionary<string, object?> MapParameters(Expense e) => new()
    {
        ["ExpenseId"] = e.ExpenseId,
        ["Date"] = e.Date,
        ["Description"] = e.Description,
        ["Amount"] = e.Amount,
        ["TaxAmount"] = e.TaxAmount,
        ["GST"] = e.GST,
        ["Total"] = e.Total,
        ["SupplierId"] = (object?)e.SupplierId ?? DBNull.Value,
        ["SupplierName"] = (object?)e.SupplierName ?? DBNull.Value,
        ["Category"] = e.Category,
        ["Status"] = e.Status,
        ["FileName"] = (object?)e.FileName ?? DBNull.Value,
        ["FilePath"] = (object?)e.FilePath ?? DBNull.Value,
        ["CreatedBy"] = (object?)e.CreatedBy ?? DBNull.Value,
        ["CreatedAt"] = e.CreatedAt
    };
}
