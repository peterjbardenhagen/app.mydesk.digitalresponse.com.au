using System.Data;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Techlight.MyDesk.Shared.Services;

/// <summary>
/// Server-side PDF generation using QuestPDF.
/// Replaces the legacy ABCpdf screen-scraping approach from MyDeskASPNet.
/// Generates professional A4 PDFs for Quotes, Invoices and Purchase Orders.
/// </summary>
public class PdfService
{
    private readonly DatabaseService _db;
    private readonly ILogger<PdfService> _logger;

    private const string Brand     = "#007b8c";
    private const string BrandDark = "#005f6b";
    private const string RowAlt    = "#f8f9fa";
    private const string White     = "#ffffff";

    public PdfService(DatabaseService db, ILogger<PdfService> logger)
    {
        _db     = db;
        _logger = logger;
    }

    // ── Quote ────────────────────────────────────────────────────────────────

    public async Task<byte[]> GenerateQuotePdfAsync(int quoteId)
    {
        var hDt = await _db.QueryAsync(@"
            SELECT q.Qid,
                   ISNULL(q.QuoteNumber,'')                          AS QuoteNumber,
                   ISNULL(q.Reference,'')                            AS Reference,
                   q.QuoteDate,
                   ISNULL(q.Attention,'')                            AS Attention,
                   ISNULL(q.Validity,'')                             AS Validity,
                   ISNULL(q.Delivery,'')                             AS Delivery,
                   ISNULL(q.CustomerNotes,'')                        AS CustomerNotes,
                   ISNULL(q.Terms,'')                                AS Terms,
                   ISNULL(q.NettPriceTotal,0)                        AS NettPriceTotal,
                   ISNULL(c.Company,'')                              AS CompanyName,
                   ISNULL(LTRIM(RTRIM(
                       CONCAT(ISNULL(c.FirstName,''),' ',ISNULL(c.Surname,''))
                   )),'')                                            AS ContactName,
                   ISNULL(c.Address1,'')                             AS Address1,
                   ISNULL(c.Suburb,'')                               AS Suburb,
                   ISNULL(c.State,'')                                AS State,
                   ISNULL(c.Postcode,'')                             AS Postcode,
                   ISNULL(u.Name,'')                                 AS OriginatorName,
                   ISNULL(u.Email,'')                                AS OriginatorEmail
            FROM Quotes q
            LEFT JOIN Contacts c ON c.ContactId = q.ContactId
            LEFT JOIN Users u    ON u.Code = q.Code
            WHERE q.Qid = @Id",
            new() { ["Id"] = quoteId });

        if (hDt.Rows.Count == 0)
            throw new InvalidOperationException($"Quote {quoteId} not found");

        var h = hDt.Rows[0];

        var itemsDt = await _db.QueryAsync(@"
            SELECT ISNULL(Description,'') AS Description,
                   ISNULL(Quantity,0)     AS Quantity,
                   ISNULL(NettPrice,0)    AS NettPrice,
                   ISNULL(ExtNettPrice,0) AS ExtNettPrice
            FROM QuoteItems
            WHERE Qid = @Id AND ISNULL(Deleted,0) = 0
            ORDER BY QuoteItemId",
            new() { ["Id"] = quoteId });

        List<DataRow> tpRows = new();
        try
        {
            var tpDt = await _db.QueryAsync(@"
                SELECT ISNULL(Description,'') AS Description,
                       ISNULL(Quantity,0)     AS Quantity,
                       ISNULL(NettPrice,0)    AS NettPrice,
                       ISNULL(ExtNettPrice,0) AS ExtNettPrice
                FROM QuoteThirdPartyItems
                WHERE Qid = @Id
                ORDER BY QuoteThirdPartyItemId",
                new() { ["Id"] = quoteId });
            tpRows = tpDt.Rows.Cast<DataRow>().ToList();
        }
        catch { }

        var lineRows  = itemsDt.Rows.Cast<DataRow>().ToList();
        var nettTotal = Convert.ToDecimal(h["NettPriceTotal"]);
        var gst       = nettTotal * 0.1m;
        var docNum    = h["QuoteNumber"].ToString() is { Length: > 0 } qn ? qn : $"Q{quoteId}";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(1.5f, Unit.Centimetre);
                page.MarginVertical(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(s => s.FontFamily(Fonts.Arial).FontSize(9));

                page.Header().Element(c => DocHeader(c, "QUOTATION", docNum,
                    h["OriginatorName"].ToString()!, h["OriginatorEmail"].ToString()!));

                page.Content().PaddingTop(16).Column(col =>
                {
                    // Bill-to + doc details
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("BILL TO").Bold().FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                            left.Item().PaddingTop(3).Text(h["CompanyName"].ToString()!).Bold().FontSize(11);
                            if (!string.IsNullOrWhiteSpace(h["ContactName"].ToString()))
                                left.Item().Text(h["ContactName"].ToString()!).FontSize(9);
                            if (!string.IsNullOrWhiteSpace(h["Attention"].ToString()))
                                left.Item().Text($"Attn: {h["Attention"]}").FontSize(9).Italic();
                            var addr = h["Address1"].ToString()!;
                            if (!string.IsNullOrWhiteSpace(addr))
                                left.Item().Text(addr).FontSize(9);
                            var suburb = h["Suburb"].ToString()!;
                            if (!string.IsNullOrWhiteSpace(suburb))
                            {
                                var statePc = $"{h["State"]} {h["Postcode"]}".Trim();
                                left.Item().Text($"{suburb}  {statePc}".Trim()).FontSize(9);
                            }
                        });

                        row.ConstantItem(195).Column(right =>
                        {
                            DocDetailRow(right, "Date:",
                                h["QuoteDate"] == DBNull.Value ? "" : Convert.ToDateTime(h["QuoteDate"]).ToString("dd/MM/yyyy"));
                            DocDetailRow(right, "Reference:", h["Reference"].ToString()!);
                            DocDetailRow(right, "Validity:", h["Validity"].ToString()!);
                            DocDetailRow(right, "Delivery:", h["Delivery"].ToString()!);
                        });
                    });

                    // Line items
                    if (lineRows.Count > 0 || tpRows.Count > 0)
                    {
                        col.Item().PaddingTop(18).Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(6);
                                cols.ConstantColumn(35);
                                cols.ConstantColumn(78);
                                cols.ConstantColumn(78);
                            });

                            table.Header(h2 =>
                            {
                                h2.Cell().Background(Brand).Padding(5).Text("Description").Bold().FontSize(8.5f).FontColor(Colors.White);
                                h2.Cell().Background(Brand).Padding(5).AlignCenter().Text("Qty").Bold().FontSize(8.5f).FontColor(Colors.White);
                                h2.Cell().Background(Brand).Padding(5).AlignRight().Text("Unit Price").Bold().FontSize(8.5f).FontColor(Colors.White);
                                h2.Cell().Background(Brand).Padding(5).AlignRight().Text("Total").Bold().FontSize(8.5f).FontColor(Colors.White);
                            });

                            bool alt = false;
                            void ItemRow(DataRow r)
                            {
                                var bg = alt ? RowAlt : White;
                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5).Text(r["Description"].ToString()!).FontSize(9);
                                var qty = Convert.ToDecimal(r["Quantity"]);
                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5).AlignCenter()
                                    .Text(qty % 1 == 0 ? ((int)qty).ToString() : qty.ToString("N1")).FontSize(9);
                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5).AlignRight()
                                    .Text(Convert.ToDecimal(r["NettPrice"]).ToString("C2")).FontSize(9);
                                table.Cell().Background(bg).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(5).AlignRight()
                                    .Text(Convert.ToDecimal(r["ExtNettPrice"]).ToString("C2")).FontSize(9);
                                alt = !alt;
                            }

                            foreach (var r in lineRows) ItemRow(r);

                            if (tpRows.Count > 0)
                            {
                                table.Cell().ColumnSpan(4)
                                    .Background(Colors.Grey.Lighten3).PaddingHorizontal(5).PaddingVertical(3)
                                    .Text("Third Party Supply").Bold().FontSize(8).FontColor(Colors.Grey.Darken2);
                                alt = false;
                                foreach (var r in tpRows) ItemRow(r);
                            }
                        });
                    }

                    // Totals block
                    col.Item().PaddingTop(14).AlignRight().Width(225).Column(totals =>
                    {
                        TotalRow(totals, "Subtotal (ex GST)", nettTotal);
                        TotalRow(totals, "GST (10%)", gst);
                        totals.Item().Background(Brand).Row(r =>
                        {
                            r.RelativeItem().Padding(5)
                                .Text("TOTAL (inc. GST)").Bold().FontSize(10.5f).FontColor(Colors.White);
                            r.ConstantItem(90).AlignRight().Padding(5)
                                .Text((nettTotal + gst).ToString("C2")).Bold().FontSize(10.5f).FontColor(Colors.White);
                        });
                    });

                    // Notes + Terms
                    var notes = h["CustomerNotes"].ToString();
                    var terms = h["Terms"].ToString();
                    if (!string.IsNullOrWhiteSpace(notes))
                    {
                        col.Item().PaddingTop(18).Column(n =>
                        {
                            n.Item().Text("Notes").Bold().FontSize(9).FontColor(Colors.Grey.Darken2);
                            n.Item().PaddingTop(3).Text(notes!).FontSize(9);
                        });
                    }
                    if (!string.IsNullOrWhiteSpace(terms))
                    {
                        col.Item().PaddingTop(12).Column(n =>
                        {
                            n.Item().Text("Terms & Conditions").Bold().FontSize(9).FontColor(Colors.Grey.Darken2);
                            n.Item().PaddingTop(3).Text(terms!).FontSize(9);
                        });
                    }
                });

                DocFooter(page, h["OriginatorName"].ToString()!);
            });
        }).GeneratePdf();
    }

    // ── Invoice ──────────────────────────────────────────────────────────────

    public async Task<byte[]> GenerateInvoicePdfAsync(int invoiceId)
    {
        var dt = await _db.QueryAsync(@"
            SELECT i.InvoiceId,
                   ISNULL(i.InvoiceNumber,'')                        AS InvoiceNumber,
                   i.InvoiceDate, i.DueDate,
                   ISNULL(i.Amount,0)                                AS Amount,
                   ISNULL(i.AmountExGST,0)                           AS AmountExGST,
                   ISNULL(i.GST,0)                                   AS GST,
                   ISNULL(i.Reference,'')                            AS Reference,
                   ISNULL(ists.InvoiceStatus,'')                     AS StatusName,
                   ISNULL(c.Company,'')                              AS CompanyName,
                   ISNULL(LTRIM(RTRIM(
                       CONCAT(ISNULL(c.FirstName,''),' ',ISNULL(c.Surname,''))
                   )),'')                                            AS ContactName,
                   ISNULL(c.Address1,'')                             AS Address1,
                   ISNULL(c.Suburb,'')                               AS Suburb,
                   ISNULL(c.State,'')                                AS State,
                   ISNULL(c.Postcode,'')                             AS Postcode,
                   ISNULL(u.Name,'')                                 AS OriginatorName,
                   ISNULL(u.Email,'')                                AS OriginatorEmail
            FROM Invoices i
            LEFT JOIN Contacts c       ON c.ContactId = i.ContactId
            LEFT JOIN InvoiceStatus ists ON ists.InvoiceStatusId = i.InvoiceStatusId
            LEFT JOIN Users u          ON u.Code = i.Code
            WHERE i.InvoiceId = @Id",
            new() { ["Id"] = invoiceId });

        if (dt.Rows.Count == 0)
            throw new InvalidOperationException($"Invoice {invoiceId} not found");

        var h           = dt.Rows[0];
        var amtExGst    = Convert.ToDecimal(h["AmountExGST"]);
        var gst         = Convert.ToDecimal(h["GST"]);
        var total       = Convert.ToDecimal(h["Amount"]);
        var invNum      = h["InvoiceNumber"].ToString() is { Length: > 0 } n ? n : $"INV{invoiceId}";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(1.5f, Unit.Centimetre);
                page.MarginVertical(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(s => s.FontFamily(Fonts.Arial).FontSize(9));

                page.Header().Element(c => DocHeader(c, "INVOICE", $"#{invNum}",
                    h["OriginatorName"].ToString()!, h["OriginatorEmail"].ToString()!));

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("BILL TO").Bold().FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                            left.Item().PaddingTop(3).Text(h["CompanyName"].ToString()!).Bold().FontSize(11);
                            if (!string.IsNullOrWhiteSpace(h["ContactName"].ToString()))
                                left.Item().Text(h["ContactName"].ToString()!).FontSize(9);
                            var addr = h["Address1"].ToString()!;
                            if (!string.IsNullOrWhiteSpace(addr)) left.Item().Text(addr).FontSize(9);
                            var suburb = h["Suburb"].ToString()!;
                            if (!string.IsNullOrWhiteSpace(suburb))
                            {
                                var sp = $"{h["State"]} {h["Postcode"]}".Trim();
                                left.Item().Text($"{suburb}  {sp}".Trim()).FontSize(9);
                            }
                        });

                        row.ConstantItem(195).Column(right =>
                        {
                            DocDetailRow(right, "Invoice No:", invNum);
                            DocDetailRow(right, "Date:",
                                h["InvoiceDate"] == DBNull.Value ? "" : Convert.ToDateTime(h["InvoiceDate"]).ToString("dd/MM/yyyy"));
                            DocDetailRow(right, "Due Date:",
                                h["DueDate"] == DBNull.Value ? "" : Convert.ToDateTime(h["DueDate"]).ToString("dd/MM/yyyy"));
                            DocDetailRow(right, "Reference:", h["Reference"].ToString()!);
                            DocDetailRow(right, "Status:", h["StatusName"].ToString()!);
                        });
                    });

                    col.Item().PaddingTop(14).AlignRight().Width(225).Column(totals =>
                    {
                        TotalRow(totals, "Amount (ex GST)", amtExGst);
                        TotalRow(totals, "GST (10%)", gst);
                        totals.Item().Background(Brand).Row(r =>
                        {
                            r.RelativeItem().Padding(5)
                                .Text("TOTAL (inc. GST)").Bold().FontSize(10.5f).FontColor(Colors.White);
                            r.ConstantItem(90).AlignRight().Padding(5)
                                .Text(total.ToString("C2")).Bold().FontSize(10.5f).FontColor(Colors.White);
                        });
                    });
                });

                DocFooter(page, h["OriginatorName"].ToString()!);
            });
        }).GeneratePdf();
    }

    // ── Purchase Order ───────────────────────────────────────────────────────

    public async Task<byte[]> GeneratePurchaseOrderPdfAsync(int poId)
    {
        var dt = await _db.QueryAsync(@"
            SELECT p.PurchaseOrderId,
                   ISNULL(p.PurchaseOrderNumber,'')  AS PONumber,
                   p.PODate, p.ExpectedDelivery,
                   ISNULL(p.AmountExGST,0)           AS AmountExGST,
                   ISNULL(p.Amount,0)                AS Amount,
                   ISNULL(p.Reference,'')            AS Reference,
                   ISNULL(ps.POStatus,'')            AS StatusName,
                   ISNULL(c.CompanyName,'')          AS SupplierName,
                   ISNULL(u.Name,'')                 AS OriginatorName,
                   ISNULL(u.Email,'')                AS OriginatorEmail
            FROM PurchaseOrders p
            LEFT JOIN Contacts c          ON c.ContactId = p.ContactId
            LEFT JOIN PurchaseOrderStatus ps ON ps.POStatusId = p.POStatusId
            LEFT JOIN Users u             ON u.Code = p.Code
            WHERE p.PurchaseOrderId = @Id",
            new() { ["Id"] = poId });

        if (dt.Rows.Count == 0)
            throw new InvalidOperationException($"Purchase Order {poId} not found");

        var h         = dt.Rows[0];
        var amtExGst  = Convert.ToDecimal(h["AmountExGST"]);
        var gst       = amtExGst * 0.1m;
        var total     = Convert.ToDecimal(h["Amount"]);
        var poNum     = h["PONumber"].ToString() is { Length: > 0 } n ? n : $"PO{poId}";

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(1.5f, Unit.Centimetre);
                page.MarginVertical(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(s => s.FontFamily(Fonts.Arial).FontSize(9));

                page.Header().Element(c => DocHeader(c, "PURCHASE ORDER", $"#{poNum}",
                    h["OriginatorName"].ToString()!, h["OriginatorEmail"].ToString()!));

                page.Content().PaddingTop(16).Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("SUPPLIER").Bold().FontSize(7.5f).FontColor(Colors.Grey.Darken1);
                            left.Item().PaddingTop(3).Text(h["SupplierName"].ToString()!).Bold().FontSize(11);
                        });

                        row.ConstantItem(195).Column(right =>
                        {
                            DocDetailRow(right, "PO Number:", poNum);
                            DocDetailRow(right, "Date:",
                                h["PODate"] == DBNull.Value ? "" : Convert.ToDateTime(h["PODate"]).ToString("dd/MM/yyyy"));
                            DocDetailRow(right, "Expected Delivery:",
                                h["ExpectedDelivery"] == DBNull.Value ? "" : Convert.ToDateTime(h["ExpectedDelivery"]).ToString("dd/MM/yyyy"));
                            DocDetailRow(right, "Reference:", h["Reference"].ToString()!);
                            DocDetailRow(right, "Status:", h["StatusName"].ToString()!);
                        });
                    });

                    col.Item().PaddingTop(14).AlignRight().Width(225).Column(totals =>
                    {
                        TotalRow(totals, "Amount (ex GST)", amtExGst);
                        TotalRow(totals, "GST (10%)", gst);
                        totals.Item().Background(Brand).Row(r =>
                        {
                            r.RelativeItem().Padding(5)
                                .Text("TOTAL (inc. GST)").Bold().FontSize(10.5f).FontColor(Colors.White);
                            r.ConstantItem(90).AlignRight().Padding(5)
                                .Text(total.ToString("C2")).Bold().FontSize(10.5f).FontColor(Colors.White);
                        });
                    });
                });

                DocFooter(page, h["OriginatorName"].ToString()!);
            });
        }).GeneratePdf();
    }

    // ── Shared layout helpers ────────────────────────────────────────────────

    private static void DocHeader(IContainer container, string docType, string docNumber,
        string originator, string email)
    {
        container.Background(Brand).Row(row =>
        {
            row.RelativeItem().PaddingHorizontal(16).PaddingVertical(14).Column(left =>
            {
                left.Item().Text(docType).FontSize(20).Bold().FontColor(Colors.White);
                left.Item().PaddingTop(2).Text(docNumber).FontSize(10).FontColor(Colors.White).Italic();
            });
            row.ConstantItem(230).PaddingVertical(14).PaddingRight(16).AlignRight().Column(right =>
            {
                right.Item().Text("TECHLIGHT").FontSize(14).Bold().FontColor(Colors.White);
                if (!string.IsNullOrWhiteSpace(originator))
                    right.Item().PaddingTop(2).Text(originator).FontSize(9).FontColor(Colors.White).Italic();
                if (!string.IsNullOrWhiteSpace(email))
                    right.Item().Text(email).FontSize(9).FontColor(Colors.White).Italic();
            });
        });
    }

    private static void DocDetailRow(ColumnDescriptor col, string label, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        col.Item().Row(r =>
        {
            r.ConstantItem(100).Text(label).FontSize(9).FontColor(Colors.Grey.Darken1);
            r.RelativeItem().Text(value).FontSize(9);
        });
    }

    private static void TotalRow(ColumnDescriptor col, string label, decimal amount)
    {
        col.Item().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).Row(r =>
        {
            r.RelativeItem().Padding(4).Text(label).FontSize(9).FontColor(Colors.Grey.Darken1);
            r.ConstantItem(90).AlignRight().Padding(4).Text(amount.ToString("C2")).FontSize(9);
        });
    }

    private static void DocFooter(PageDescriptor page, string originator)
    {
        page.Footer().AlignCenter().Text(t =>
        {
            t.Span("Techlight MyDesk").FontSize(7.5f).FontColor(Colors.Grey.Medium);
            if (!string.IsNullOrWhiteSpace(originator))
            {
                t.Span("  ·  ").FontSize(7.5f).FontColor(Colors.Grey.Medium);
                t.Span(originator).FontSize(7.5f).FontColor(Colors.Grey.Medium);
            }
            t.Span("  ·  Page ").FontSize(7.5f).FontColor(Colors.Grey.Medium);
            t.CurrentPageNumber().FontSize(7.5f).FontColor(Colors.Grey.Medium);
            t.Span(" of ").FontSize(7.5f).FontColor(Colors.Grey.Medium);
            t.TotalPages().FontSize(7.5f).FontColor(Colors.Grey.Medium);
        });
    }
}
