using System;

namespace MyDesk.Shared.Models;

/// <summary>
/// A recorded business expense.
///
/// <para>
/// <b>Multi-currency:</b> the expense's primary <see cref="Amount"/> is in the
/// currency of the receipt (<see cref="Currency"/>, default <c>AUD</c>). For non-AUD
/// expenses we also record the AUD-equivalent (<see cref="AmountAud"/> +
/// <see cref="ExchangeRate"/>) so the books and accounts team can see actual cost
/// to the business. The AUD figure should ideally come from the receipt itself
/// (the bank conversion line); if it's not on the receipt the user enters it
/// manually after looking up the matching bank transaction (until the future
/// banking integration auto-matches it).
/// </para>
/// <para>
/// <b>GST:</b> <see cref="HasGst"/> indicates whether GST applies. For Australian
/// expenses GST is typically 10% and <see cref="Gst"/> equals <c>Amount/11</c>
/// (10% of the GST-exclusive component). For overseas expenses GST is generally
/// not applicable and <see cref="HasGst"/> defaults to false.
/// </para>
/// </summary>
public class Expense
{
    public int ExpenseId { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public string Description { get; set; } = "";

    /// <summary>Amount in the receipt's currency (gross — including any tax).</summary>
    public decimal Amount { get; set; }

    /// <summary>Legacy tax-amount field (kept for migration compatibility).</summary>
    public decimal TaxAmount { get; set; }

    /// <summary>GST component of <see cref="Amount"/> when <see cref="HasGst"/> is true.</summary>
    public decimal Gst { get; set; }

    /// <summary>Convenience alias for back-compat with existing code that used the older property name.</summary>
    public decimal GST { get => Gst; set => Gst = value; }

    /// <summary>Gross total (typically equals <see cref="Amount"/>).</summary>
    public decimal Total { get; set; }

    // ── Multi-currency ────────────────────────────────────────────────────
    /// <summary>ISO currency code of the receipt (default <c>AUD</c>). Stored upper case.</summary>
    public string Currency { get; set; } = "AUD";

    /// <summary>True when GST applies (10% in AU). Defaults to true for AUD expenses, false for overseas.</summary>
    public bool HasGst { get; set; } = true;

    /// <summary>Exchange rate applied (foreign currency units per 1 AUD). Null for AUD expenses.</summary>
    public decimal? ExchangeRate { get; set; }

    /// <summary>Equivalent amount in AUD for the books. Equals <see cref="Amount"/> when Currency == AUD.</summary>
    public decimal AmountAud { get; set; }

    /// <summary>How the AUD figure was derived: <c>receipt</c>, <c>bank</c>, <c>manual</c>, <c>integration</c>.</summary>
    public string AmountAudSource { get; set; } = "receipt";

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
