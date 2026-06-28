using System;
using System.Data;
using System.Threading.Tasks;

namespace MyDesk.Shared.Services;

/// <summary>
/// Handles end-of-month accounting processes including bank reconciliation, 
/// expense categorisation, and automated statement processing.
/// </summary>
public class EndOfMonthAccountingService
{
    private readonly DatabaseService _db;
    private readonly BankReconciliationService _bankReconciliation;

    public EndOfMonthAccountingService(DatabaseService db, BankReconciliationService bankReconciliation)
    {
        _db = db;
        _bankReconciliation = bankReconciliation;
    }

    public async Task<EndOfMonthSummary> GetSummaryAsync(int companyId, DateTime month)
    {
        var summary = new EndOfMonthSummary { Month = month };

        // Get invoice totals
        var invoiceSql = $@"
            SELECT 
                COUNT(*) AS InvoiceCount,
                SUM(ISNULL(NettPriceTotal, 0) + ISNULL(GSTTotal, 0)) AS TotalAmount,
                SUM(ISNULL(GSTTotal, 0)) AS GSTTotal
            FROM Invoices 
            WHERE CompanyId = {companyId} 
              AND YEAR(InvoiceDate) = {month.Year}
              AND MONTH(InvoiceDate) = {month.Month}
              AND InvoiceStatusId IN (2, 3, 4)";
        var invoiceDt = await _db.QueryAsync(invoiceSql);
        if (invoiceDt.Rows.Count > 0)
        {
            summary.InvoiceCount = Convert.ToInt32(invoiceDt.Rows[0]["InvoiceCount"]);
            summary.InvoiceTotal = Convert.ToDecimal(invoiceDt.Rows[0]["TotalAmount"]);
            summary.InvoiceGST = Convert.ToDecimal(invoiceDt.Rows[0]["GSTTotal"]);
        }

        // Get expense totals
        var expenseSql = $@"
            SELECT 
                COUNT(*) AS ExpenseCount,
                SUM(ISNULL(AmountExGst, 0) + ISNULL(GstAmount, 0)) AS TotalAmount
            FROM Expenses 
            WHERE CompanyId = {companyId}
              AND YEAR(ExpenseDate) = {month.Year}
              AND MONTH(ExpenseDate) = {month.Month}";
        var expenseDt = await _db.QueryAsync(expenseSql);
        if (expenseDt.Rows.Count > 0)
        {
            summary.ExpenseCount = Convert.ToInt32(expenseDt.Rows[0]["ExpenseCount"]);
            summary.ExpenseTotal = Convert.ToDecimal(expenseDt.Rows[0]["TotalAmount"]);
        }

        // Get bank reconciliation status
        var bankSql = $@"
            SELECT COUNT(*) AS StatementCount, 
                   SUM(CASE WHEN Status = 'Reconciled' THEN 1 ELSE 0 END) AS ReconciledCount
            FROM BankStatements 
            WHERE CompanyId = {companyId}
              AND YEAR(StatementDate) = {month.Year}
              AND MONTH(StatementDate) = {month.Month}";
        var bankDt = await _db.QueryAsync(bankSql);
        if (bankDt.Rows.Count > 0)
        {
            summary.StatementCount = Convert.ToInt32(bankDt.Rows[0]["StatementCount"]);
            summary.ReconciledCount = Convert.ToInt32(bankDt.Rows[0]["ReconciledCount"]);
        }

        return summary;
    }
}