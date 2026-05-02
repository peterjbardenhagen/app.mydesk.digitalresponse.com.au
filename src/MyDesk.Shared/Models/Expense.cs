using System;

namespace MyDesk.Shared.Models;

public class Expense
{
    public int ExpenseId { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public string Description { get; set; } = "";
    public decimal Amount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal GST { get; set; }
    public decimal Total { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public string Category { get; set; } = "General";
    public string Status { get; set; } = "Pending";
    public string? FileName { get; set; }
    public string? FilePath { get; set; }
    public string? AIProcessingResult { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public enum ExpenseStatus
{
    Pending,
    Approved,
    Rejected,
    Paid
}
