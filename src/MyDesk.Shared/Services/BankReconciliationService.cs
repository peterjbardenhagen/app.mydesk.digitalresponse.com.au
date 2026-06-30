using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MyDesk.Shared.Services;

public class BankReconciliationService
{
    private readonly DatabaseService _db;
    private readonly ILogger<BankReconciliationService> _logger;

    public BankReconciliationService(DatabaseService db, ILogger<BankReconciliationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<BankStatement>> GetBankStatementsAsync(int companyId)
    {
        var sql = $@"
            SELECT StatementId, BankName, AccountNumber, StatementDate, OpeningBalance, ClosingBalance, ClosingDate, TransactionCount, Status
            FROM BankStatements 
            WHERE CompanyId = {companyId}
            ORDER BY StatementDate DESC";
        var dt = await _db.QueryAsync(sql);
        return dt.Map(MapBankStatement);
    }

    public async Task<List<ReconciliationTransaction>> GetBankTransactionsAsync(int statementId)
    {
        var sql = $@"
            SELECT t.TransactionId, t.Date, t.Description, t.Amount, t.Balance, t.Type, t.Reference, t.Status,
                   i.InvoiceNum, po.PONum
            FROM BankTransactions t
            LEFT JOIN Invoices i ON t.InvoiceId = i.InvoiceId
            LEFT JOIN PurchaseOrders po ON t.PurchaseOrderId = po.PurchaseOrderId
            WHERE t.StatementId = {statementId}
            ORDER BY t.Date ASC";
        var dt = await _db.QueryAsync(sql);
        return dt.Map(MapBankTransaction);
    }

    public async Task<ReconciliationResult> ProcessBankReconciliationAsync(int companyId, int statementId)
    {
        var transactions = await GetBankTransactionsAsync(statementId);
        var unmatched = new List<ReconciliationTransaction>();
        var matched = new List<ReconciliationTransaction>();

        foreach (var tx in transactions)
        {
            if (tx.InvoiceId.HasValue || tx.PurchaseOrderId.HasValue)
            {
                matched.Add(tx);
            }
            else
            {
                unmatched.Add(tx);
            }
        }

        return new ReconciliationResult
        {
            StatementId = statementId,
            TotalTransactions = transactions.Count,
            MatchedCount = matched.Count,
            UnmatchedCount = unmatched.Count,
            MatchedTransactions = matched,
            UnmatchedTransactions = unmatched,
            ReconciliationDate = DateTime.Now
        };
    }

    private static BankStatement MapBankStatement(DataRow r) => new()
    {
        StatementId = Convert.ToInt32(r["StatementId"]),
        BankName = r["BankName"]?.ToString() ?? "",
        AccountNumber = r["AccountNumber"]?.ToString() ?? "",
        StatementDate = r["StatementDate"] != DBNull.Value ? Convert.ToDateTime(r["StatementDate"]) : DateTime.MinValue,
        OpeningBalance = r["OpeningBalance"] != DBNull.Value ? Convert.ToDecimal(r["OpeningBalance"]) : 0,
        ClosingBalance = r["ClosingBalance"] != DBNull.Value ? Convert.ToDecimal(r["ClosingBalance"]) : 0,
        ClosingDate = r["ClosingDate"] != DBNull.Value ? Convert.ToDateTime(r["ClosingDate"]) : DateTime.MinValue,
        TransactionCount = r["TransactionCount"] != DBNull.Value ? Convert.ToInt32(r["TransactionCount"]) : 0,
        Status = r["Status"]?.ToString() ?? "Pending"
    };

    private static ReconciliationTransaction MapBankTransaction(DataRow r) => new()
    {
        TransactionId = Convert.ToInt32(r["TransactionId"]),
        Date = r["Date"] != DBNull.Value ? Convert.ToDateTime(r["Date"]) : DateTime.MinValue,
        Description = r["Description"]?.ToString() ?? "",
        Amount = r["Amount"] != DBNull.Value ? Convert.ToDecimal(r["Amount"]) : 0,
        Balance = r["Balance"] != DBNull.Value ? Convert.ToDecimal(r["Balance"]) : 0,
        Type = r["Type"]?.ToString() ?? "",
        Reference = r["Reference"]?.ToString() ?? "",
        Status = r["Status"]?.ToString() ?? "Pending",
        InvoiceNum = r["InvoiceNum"]?.ToString() ?? "",
        PONum = r["PONum"]?.ToString() ?? ""
    };
}

public class BankStatement
{
    public int StatementId { get; set; }
    public string BankName { get; set; } = "";
    public string AccountNumber { get; set; } = "";
    public DateTime StatementDate { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ClosingBalance { get; set; }
    public DateTime ClosingDate { get; set; }
    public int TransactionCount { get; set; }
    public string Status { get; set; } = "Pending";
}

// Renamed from BankTransaction to avoid clash with MyDesk.Shared.Models.BankTransaction
public class ReconciliationTransaction
{
    public int TransactionId { get; set; }
    public DateTime Date { get; set; }
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal Balance { get; set; }
    public string Type { get; set; } = "";
    public string Reference { get; set; } = "";
    public string Status { get; set; } = "Pending";
    public int? InvoiceId { get; set; }
    public int? PurchaseOrderId { get; set; }
    public string InvoiceNum { get; set; } = "";
    public string PONum { get; set; } = "";
}

public class ReconciliationResult
{
    public int StatementId { get; set; }
    public int TotalTransactions { get; set; }
    public int MatchedCount { get; set; }
    public int UnmatchedCount { get; set; }
    public DateTime ReconciliationDate { get; set; }
    public List<ReconciliationTransaction> MatchedTransactions { get; set; } = new();
    public List<ReconciliationTransaction> UnmatchedTransactions { get; set; } = new();
}