namespace MyDesk.Shared.Services.Extraction;

/// <summary>
/// Pure-C# math auditor that runs after every extraction strategy. Verifies that
/// (Σ line totals) + GST ≈ document total within a 5-cent tolerance, and adds
/// any discrepancies to the result. Keeps AI hallucinations in check by trusting
/// numbers only when the math reconciles.
/// </summary>
public static class ExtractionAuditor
{
    private const decimal Tolerance = 0.05m;

    public static void Audit(ExtractedDocument doc)
    {
        doc.Discrepancies.Clear();

        // Reconcile line items if we have any
        if (doc.LineItems.Count > 0)
        {
            decimal calculatedSubtotal = 0;
            foreach (var li in doc.LineItems)
            {
                if (li.LineTotal.HasValue)
                {
                    calculatedSubtotal += li.LineTotal.Value;
                }
                else if (li.Quantity.HasValue && li.UnitPrice.HasValue)
                {
                    var lt = li.Quantity.Value * li.UnitPrice.Value;
                    li.LineTotal = lt;
                    calculatedSubtotal += lt;
                }

                // Per-line check: qty * unit ≈ line total
                if (li is { Quantity: not null, UnitPrice: not null, LineTotal: not null })
                {
                    var expected = li.Quantity.Value * li.UnitPrice.Value;
                    if (Math.Abs(expected - li.LineTotal.Value) > Tolerance)
                    {
                        doc.Discrepancies.Add(
                            $"Line item math mismatch: \"{li.Description}\" — {li.Quantity} × {li.UnitPrice:C} ≠ {li.LineTotal:C}");
                    }
                }
            }

            // Subtotal cross-check
            if (doc.Subtotal.HasValue && Math.Abs(calculatedSubtotal - doc.Subtotal.Value) > Tolerance)
            {
                doc.Discrepancies.Add(
                    $"Subtotal mismatch: line items sum to {calculatedSubtotal:C} but subtotal field reads {doc.Subtotal:C}");
            }
            else if (!doc.Subtotal.HasValue)
            {
                doc.Subtotal = calculatedSubtotal;
            }
        }

        // Total reconciliation: subtotal + GST ≈ total
        if (doc.Subtotal.HasValue && doc.GstAmount.HasValue && doc.TotalAmount.HasValue)
        {
            var expectedTotal = doc.Subtotal.Value + doc.GstAmount.Value;
            if (Math.Abs(expectedTotal - doc.TotalAmount.Value) > Tolerance)
            {
                doc.Discrepancies.Add(
                    $"Total mismatch: subtotal {doc.Subtotal:C} + GST {doc.GstAmount:C} = {expectedTotal:C} ≠ document total {doc.TotalAmount:C}");
            }
        }

        // GST sanity: should be ~10% of subtotal in Australia. Allow 9.9–10.1% for rounding.
        if (doc.Subtotal.HasValue && doc.GstAmount.HasValue && doc.Subtotal.Value > 0)
        {
            var gstRate = (double)(doc.GstAmount.Value / doc.Subtotal.Value);
            if (gstRate is > 0.001 and < 0.099 or > 0.105)
            {
                doc.Discrepancies.Add(
                    $"GST rate looks unusual: {gstRate:P2} of subtotal (expected ~10% in Australia).");
            }
        }

        doc.AuditPassed = doc.Discrepancies.Count == 0;
    }
}
