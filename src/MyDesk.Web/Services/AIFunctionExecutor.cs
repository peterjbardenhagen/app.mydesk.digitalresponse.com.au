using System.Data;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Services;

/// <summary>
/// Executes functions/tools that the AI can call to perform tasks.
/// Provides direct database access for queries when MCP server is unavailable.
/// </summary>
public class AIFunctionExecutor
{
    private readonly DatabaseService _db;
    private readonly QuoteService _quoteService;
    private readonly InvoiceService _invoiceService;
    private readonly CompanyService _companyService;
    private readonly ExpenseService _expenseService;
    private readonly ILogger<AIFunctionExecutor> _logger;

    public AIFunctionExecutor(
        DatabaseService db,
        QuoteService quoteService,
        InvoiceService invoiceService,
        CompanyService companyService,
        ExpenseService expenseService,
        ILogger<AIFunctionExecutor> logger)
    {
        _db = db;
        _quoteService = quoteService;
        _invoiceService = invoiceService;
        _companyService = companyService;
        _expenseService = expenseService;
        _logger = logger;
    }

    /// <summary>
    /// Get quotes with optional filters
    /// </summary>
    public async Task<object> GetQuotesAsync(string? status = null, decimal? minAmount = null, int? limit = 10)
    {
        try
        {
            var sql = @"
                SELECT TOP (@l) q.Qid, q.Reference, q.QuoteDate, q.NettPriceTotal as NetTotalPrice, qs.QuoteStatus,
                       c.Company as CompanyName, u.Name as Originator
                FROM Quotes q
                LEFT JOIN QuoteStatus qs ON q.QuoteStatusId = qs.QuoteStatusId
                LEFT JOIN Contacts co ON q.ContactId = co.ContactId
                LEFT JOIN Companies c ON co.CompanyId = c.CompanyId
                LEFT JOIN Users u ON q.Code = u.Code
                WHERE 1=1";

            var parameters = new Dictionary<string, object?> { ["l"] = limit ?? 10 };

            if (!string.IsNullOrEmpty(status))
            {
                sql += " AND qs.QuoteStatus = @status";
                parameters["status"] = status;
            }

            if (minAmount.HasValue)
            {
                sql += " AND q.NettPriceTotal >= @min";
                parameters["min"] = minAmount.Value;
            }

            sql += " ORDER BY q.QuoteDate DESC";

            var dt = await _db.QueryAsync(sql, parameters);
            return DataTableToList(dt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quotes");
            return new { error = ex.Message };
        }
    }

    /// <summary>
    /// Get invoices with optional filters
    /// </summary>
    public async Task<object> GetInvoicesAsync(string? status = null, decimal? minAmount = null, int? limit = 10)
    {
        try
        {
            var sql = @"
                SELECT TOP (@l) i.InvoiceId, i.InvoiceNumber, i.InvoiceDate, i.NettPriceTotal as NetTotalPrice, s.InvoiceStatus as Status,
                       c.Company as CompanyName
                FROM Invoices i
                LEFT JOIN Companies c ON i.CompanyId = c.CompanyId
                LEFT JOIN InvoiceStatus s ON i.InvoiceStatusId = s.InvoiceStatusId
                WHERE 1=1";

            var parameters = new Dictionary<string, object?> { ["l"] = limit ?? 10 };

            if (!string.IsNullOrEmpty(status))
            {
                sql += " AND s.InvoiceStatus = @status";
                parameters["status"] = status;
            }

            if (minAmount.HasValue)
            {
                sql += " AND i.NettPriceTotal >= @min";
                parameters["min"] = minAmount.Value;
            }

            sql += " ORDER BY i.InvoiceDate DESC";

            var dt = await _db.QueryAsync(sql, parameters);
            return DataTableToList(dt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting invoices");
            return new { error = ex.Message };
        }
    }

    /// <summary>
    /// Get purchase orders with optional filters
    /// </summary>
    public async Task<object> GetPurchaseOrdersAsync(string? status = null, int? limit = 10)
    {
        try
        {
            var sql = @"
                SELECT TOP (@l) po.POId, po.PONumber, po.PODate, po.Total, po.Status,
                       c.Company as CompanyName
                FROM PurchaseOrders po
                LEFT JOIN Companies c ON po.CompanyId = c.CompanyId
                WHERE 1=1";

            var parameters = new Dictionary<string, object?> { ["l"] = limit ?? 10 };

            if (!string.IsNullOrEmpty(status))
            {
                sql += " AND po.Status = @status";
                parameters["status"] = status;
            }

            sql += " ORDER BY po.PODate DESC";

            var dt = await _db.QueryAsync(sql, parameters);
            return DataTableToList(dt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting purchase orders");
            return new { error = ex.Message };
        }
    }

    /// <summary>
    /// Get companies with optional search
    /// </summary>
    public async Task<object> GetCompaniesAsync(string? search = null, int? limit = 10)
    {
        try
        {
            var sql = @"
                SELECT TOP (@l) CompanyId, Company, Phone, Email
                FROM Companies
                WHERE 1=1";

            var parameters = new Dictionary<string, object?> { ["l"] = limit ?? 10 };

            if (!string.IsNullOrEmpty(search))
            {
                sql += " AND Company LIKE @search";
                parameters["search"] = $"%{search}%";
            }

            sql += " ORDER BY Company";

            var dt = await _db.QueryAsync(sql, parameters);
            return DataTableToList(dt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting companies");
            return new { error = ex.Message };
        }
    }

    /// <summary>
    /// Get expenses with optional filters
    /// </summary>
    public async Task<object> GetExpensesAsync(string? status = null, int? limit = 10)
    {
        try
        {
            var sql = @"
                SELECT TOP (@l) ExpenseId, Date, Description, Total, Status, Category, SupplierName
                FROM Expenses
                WHERE 1=1";

            var parameters = new Dictionary<string, object?> { ["l"] = limit ?? 10 };

            if (!string.IsNullOrEmpty(status))
            {
                sql += " AND Status = @status";
                parameters["status"] = status;
            }

            sql += " ORDER BY Date DESC";

            var dt = await _db.QueryAsync(sql, parameters);
            return DataTableToList(dt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expenses");
            return new { error = ex.Message };
        }
    }

    /// <summary>
    /// Get aged receivables (outstanding invoices)
    /// </summary>
    public async Task<object> GetAgedReceivablesAsync()
    {
        try
        {
            var sql = @"
                SELECT c.Company, i.InvoiceNumber, i.InvoiceDate, 
                       i.NettPriceTotal, DATEDIFF(day, i.InvoiceDate, GETDATE()) as DaysSinceInvoice
                FROM Invoices i
                LEFT JOIN Companies c ON i.CompanyId = c.CompanyId
                LEFT JOIN InvoiceStatus s ON i.InvoiceStatusId = s.InvoiceStatusId
                WHERE s.InvoiceStatus = 'ISSUED'
                ORDER BY DaysSinceInvoice DESC";

            var dt = await _db.QueryAsync(sql);
            return new
            {
                summary = $"Total outstanding: {dt.Rows.Cast<DataRow>().Sum(r => Convert.ToDecimal(r["NettPriceTotal"])).ToString("C")}",
                invoices = DataTableToList(dt)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting aged receivables");
            return new { error = ex.Message };
        }
    }

    /// <summary>
    /// Get dashboard summary statistics
    /// </summary>
    public async Task<object> GetDashboardStatsAsync()
    {
        try
        {
            var stats = new Dictionary<string, object>();
            
            // Quotes this month
            var quotesThisMonth = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(NettPriceTotal), 0) FROM Quotes WHERE MONTH(QuoteDate) = MONTH(GETDATE()) AND YEAR(QuoteDate) = YEAR(GETDATE())");
            stats["quotesThisMonth"] = quotesThisMonth;

            // Invoices outstanding
            var outstanding = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(i.NettPriceTotal), 0) FROM Invoices i LEFT JOIN InvoiceStatus s ON i.InvoiceStatusId = s.InvoiceStatusId WHERE s.InvoiceStatus = 'ISSUED'");
            stats["outstandingInvoices"] = outstanding;

            // Total expenses this month
            var expensesThisMonth = await _db.ScalarAsync<decimal>(
                "SELECT ISNULL(SUM(Total), 0) FROM Expenses WHERE MONTH(Date) = MONTH(GETDATE()) AND YEAR(Date) = YEAR(GETDATE())");
            stats["expensesThisMonth"] = expensesThisMonth;

            // Recent quotes count
            var recentQuotes = await _db.ScalarAsync<int>(
                "SELECT COUNT(*) FROM Quotes WHERE QuoteDate >= DATEADD(day, -30, GETDATE())");
            stats["recentQuotesCount"] = recentQuotes;

            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting dashboard stats");
            return new { error = ex.Message };
        }
    }

    private static List<Dictionary<string, object?>> DataTableToList(DataTable dt)
    {
        var list = new List<Dictionary<string, object?>>();
        foreach (DataRow row in dt.Rows)
        {
            var dict = new Dictionary<string, object?>();
            foreach (DataColumn col in dt.Columns)
            {
                dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
            }
            list.Add(dict);
        }
        return list;
    }

    /// <summary>
    /// Execute a custom read-only SQL query (for AI use)
    /// </summary>
    public async Task<object> ExecuteQueryAsync(string sql, Dictionary<string, object?>? parameters = null)
    {
        try
        {
            // Basic safety check - only allow SELECT statements
            var trimmedSql = sql.TrimStart().ToUpperInvariant();
            if (!trimmedSql.StartsWith("SELECT"))
            {
                return new { error = "Only SELECT queries are allowed" };
            }

            var dt = await _db.QueryAsync(sql, parameters);
            return DataTableToList(dt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing query");
            return new { error = ex.Message };
        }
    }
}
