using System.Data;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace MyDesk.Shared.Services;

/// <summary>
/// Server-side PDF generation using QuestPDF.
/// Replaces the legacy ABCpdf screen-scraping approach from MyDeskASPNet.
/// Generates professional A4 PDFs for Quotes, Invoices and Purchase Orders.
/// </summary>
public class PdfService
{
    private readonly DatabaseService _db;
    private readonly ILogger<PdfService> _logger;

    // ── Techlight Brand Colours ──────────────────────────────────────────────
    private const string TlDark      = "#08121a";   // Primary dark background
    private const string TlTeal      = "#008b8b";   // Primary teal
    private const string TlTealLight = "#00a0a0";   // Light teal (accents)
    private const string TlGold      = "#cca05a";   // Gold accent
    private const string TlGoldLight = "#e0b870";   // Light gold
    private const string TlGray50   = "#f8fafc";   // Row alt
    private const string TlGray200  = "#eaecf0";   // Border
    private const string TlGray500  = "#667085";   // Muted text
    private const string TlGray700  = "#344054";   // Body text
    private const string White       = "#ffffff";

    // ── Techlight Company Details ────────────────────────────────────────────
    private const string CompanyName    = "Techlight Pty Ltd";
    private const string CompanyAddress = "Level 5, 14 Banfield St";
    private const string CompanyCity    = "Chermside QLD 4032";
    private const string CompanyWeb     = "techlight.com.au";
    private const string CompanyPhone   = "0418 736 454";
    private const string CompanyEmail   = "bertb@techlight.com.au";

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
                   ISNULL(co.Company,'')                             AS CompanyName,
                   ISNULL(LTRIM(RTRIM(
                       CONCAT(ISNULL(c.FirstName,''),' ',ISNULL(c.Surname,''))
                   )),'')                                            AS ContactName,
                   ISNULL(c.Address1,'')                             AS Address1,
                   ISNULL(c.Suburb,'')                               AS Suburb,
                   ISNULL(c.State,'')                                AS State,
                   ISNULL(c.PostCode,'')                             AS Postcode,
                   ISNULL(u.Name,'')                                 AS OriginatorName,
                   ISNULL(u.Email,'')                                AS OriginatorEmail
            FROM Quotes q
            LEFT JOIN Contacts c ON c.ContactId = q.ContactId
            LEFT JOIN Companies co ON co.CompanyId = c.CompanyId
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
                            left.Item().Text("BILL TO").Bold().FontSize(7.5f).FontColor(TlTeal);
                            left.Item().PaddingTop(3).Text(h["CompanyName"].ToString()!).Bold().FontSize(11).FontColor(TlGray700);
                            if (!string.IsNullOrWhiteSpace(h["ContactName"].ToString()))
                                left.Item().Text(h["ContactName"].ToString()!).FontSize(9).FontColor(TlGray700);
                            if (!string.IsNullOrWhiteSpace(h["Attention"].ToString()))
                                left.Item().Text($"Attn: {h["Attention"]}").FontSize(9).Italic().FontColor(TlGray500);
                            var addr = h["Address1"].ToString()!;
                            if (!string.IsNullOrWhiteSpace(addr))
                                left.Item().Text(addr).FontSize(9).FontColor(TlGray500);
                            var suburb = h["Suburb"].ToString()!;
                            if (!string.IsNullOrWhiteSpace(suburb))
                            {
                                var statePc = $"{h["State"]} {h["Postcode"]}".Trim();
                                left.Item().Text($"{suburb}  {statePc}".Trim()).FontSize(9).FontColor(TlGray500);
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
                                h2.Cell().Background(TlDark).Padding(5).Text("Description").Bold().FontSize(8.5f).FontColor(Colors.White);
                                h2.Cell().Background(TlDark).Padding(5).AlignCenter().Text("Qty").Bold().FontSize(8.5f).FontColor(Colors.White);
                                h2.Cell().Background(TlDark).Padding(5).AlignRight().Text("Unit Price").Bold().FontSize(8.5f).FontColor(Colors.White);
                                h2.Cell().Background(TlDark).Padding(5).AlignRight().Text("Total").Bold().FontSize(8.5f).FontColor(Colors.White);
                            });

                            bool alt = false;
                            void ItemRow(DataRow r)
                            {
                                var bg = alt ? TlGray50 : White;
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
                                    .Background(TlGray200).PaddingHorizontal(5).PaddingVertical(3)
                                    .Text("Third Party Supply").Bold().FontSize(8).FontColor(TlGray700);
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
                        totals.Item().Background(TlTeal).Row(r =>
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
                            n.Item().Text("Notes").Bold().FontSize(9).FontColor(TlGray700);
                            n.Item().PaddingTop(3).Text(notes!).FontSize(9).FontColor(TlGray700);
                        });
                    }
                    if (!string.IsNullOrWhiteSpace(terms))
                    {
                        col.Item().PaddingTop(12).BorderTop(1).BorderColor(TlGray200).Column(n =>
                        {
                            n.Item().PaddingTop(8).Text("Terms & Conditions").Bold().FontSize(9).FontColor(TlGray700);
                            n.Item().PaddingTop(3).Text(terms!).FontSize(8).FontColor(TlGray500);
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
                   CAST(i.InvoiceId AS NVARCHAR(20))                 AS InvoiceNumber,
                   i.InvoiceDate, i.InvoiceDate                      AS DueDate,
                   ISNULL(i.NettPriceTotal + i.GSTTotal,0)           AS Amount,
                   ISNULL(i.NettPriceTotal,0)                        AS AmountExGST,
                   ISNULL(i.GSTTotal,0)                              AS GST,
                   ISNULL(i.CustomerPO,'')                           AS Reference,
                   ISNULL(ists.InvoiceStatus,'')                     AS StatusName,
                   COALESCE(NULLIF(co.Company, ''), NULLIF(i.InvCompany, ''), NULLIF(i.DelCompany, ''), '') AS CompanyName,
                   ISNULL(LTRIM(RTRIM(
                       CONCAT(ISNULL(c.FirstName,''),' ',ISNULL(c.Surname,''))
                   )),'')                                            AS ContactName,
                   ISNULL(c.Address1,'')                             AS Address1,
                   ISNULL(c.Suburb,'')                               AS Suburb,
                   ISNULL(c.State,'')                                AS State,
                   ISNULL(c.PostCode,'')                             AS Postcode,
                   ISNULL(u.Name,'')                                 AS OriginatorName,
                   ISNULL(u.Email,'')                                AS OriginatorEmail
            FROM Invoices i
            LEFT JOIN Contacts c       ON c.ContactId = i.ContactId
            LEFT JOIN Companies co     ON co.CompanyId = i.CompanyId
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
                            left.Item().Text("BILL TO").Bold().FontSize(7.5f).FontColor(TlTeal);
                            left.Item().PaddingTop(3).Text(h["CompanyName"].ToString()!).Bold().FontSize(11).FontColor(TlGray700);
                            if (!string.IsNullOrWhiteSpace(h["ContactName"].ToString()))
                                left.Item().Text(h["ContactName"].ToString()!).FontSize(9).FontColor(TlGray700);
                            var addr = h["Address1"].ToString()!;
                            if (!string.IsNullOrWhiteSpace(addr)) left.Item().Text(addr).FontSize(9).FontColor(TlGray500);
                            var suburb = h["Suburb"].ToString()!;
                            if (!string.IsNullOrWhiteSpace(suburb))
                            {
                                var sp = $"{h["State"]} {h["Postcode"]}".Trim();
                                left.Item().Text($"{suburb}  {sp}".Trim()).FontSize(9).FontColor(TlGray500);
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
                        totals.Item().Background(TlTeal).Row(r =>
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
            SELECT p.POid                          AS PurchaseOrderId,
                   CAST(p.POid AS NVARCHAR(20))      AS PONumber,
                   p.PODate, p.DateRequired          AS ExpectedDelivery,
                   ISNULL(p.PriceExTotal,0)          AS AmountExGST,
                   ISNULL(p.PriceIncTotal,0)         AS Amount,
                   ISNULL(p.Project,'')              AS Reference,
                   ISNULL(ps.POStatus,'')            AS StatusName,
                   COALESCE(NULLIF(s.Company,''), NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(c.FirstName,''), ' ', ISNULL(c.Surname,'')))), ''), '') AS SupplierName,
                   ISNULL(u.Name,'')                 AS OriginatorName,
                   ISNULL(u.Email,'')                AS OriginatorEmail
            FROM PurchaseOrders p
            LEFT JOIN Contacts c          ON c.ContactId = p.ContactId
            LEFT JOIN Companies s         ON s.CompanyId = c.CompanyId
            LEFT JOIN PurchaseOrderStatus ps ON ps.POStatusId = p.POStatusId
            LEFT JOIN Users u             ON u.Code = p.Code
            WHERE p.POid = @Id",
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
                            left.Item().Text("SUPPLIER").Bold().FontSize(7.5f).FontColor(TlTeal);
                            left.Item().PaddingTop(3).Text(h["SupplierName"].ToString()!).Bold().FontSize(11).FontColor(TlGray700);
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
                        totals.Item().Background(TlTeal).Row(r =>
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
        container.Column(col =>
        {
            // Top band: dark with logo mark SVG + company name right
            col.Item().Background(TlDark).Row(row =>
            {
                // Logo mark (circles drawn in QuestPDF) + wordmark
                row.RelativeItem().PaddingHorizontal(16).PaddingVertical(12).Row(logoRow =>
                {
                    logoRow.ConstantItem(44).AlignMiddle().Svg(
                        $@"<svg width='44' height='44' viewBox='0 0 44 44' xmlns='http://www.w3.org/2000/svg'>
                          <circle cx='22' cy='22' r='20' stroke='{TlTeal}' stroke-width='2' fill='none' />
                          <circle cx='22' cy='22' r='14' stroke='{TlTealLight}' stroke-width='1.5' fill='none' />
                          <circle cx='22' cy='22' r='8' fill='{TlDark}' />
                          <circle cx='22' cy='22' r='4' fill='{TlTealLight}' />
                        </svg>");
                    logoRow.RelativeItem().PaddingLeft(8).AlignMiddle().Column(lc =>
                    {
                        lc.Item().Text("Techlight").FontSize(18).Bold().FontColor(Colors.White).FontFamily(Fonts.Arial);
                        lc.Item().Text("Pty Ltd").FontSize(8).FontColor(TlTealLight).FontFamily(Fonts.Arial);
                    });
                });

                // Company details right-aligned
                row.ConstantItem(200).PaddingVertical(12).PaddingRight(16).AlignRight().Column(right =>
                {
                    right.Item().Text(CompanyAddress).FontSize(8).FontColor(TlGray500).FontFamily(Fonts.Arial);
                    right.Item().Text(CompanyCity).FontSize(8).FontColor(TlGray500).FontFamily(Fonts.Arial);
                    right.Item().Text(CompanyWeb).FontSize(8).FontColor(TlTealLight).FontFamily(Fonts.Arial);
                    if (!string.IsNullOrWhiteSpace(originator))
                        right.Item().PaddingTop(3).Text(originator).FontSize(8).FontColor(Colors.White).FontFamily(Fonts.Arial);
                    if (!string.IsNullOrWhiteSpace(email))
                        right.Item().Text(email).FontSize(8).FontColor(TlGray500).FontFamily(Fonts.Arial);
                });
            });

            // Gold accent bar + document type
            col.Item().Background(TlGold).Row(bar =>
            {
                bar.RelativeItem().PaddingHorizontal(16).PaddingVertical(8).Row(r =>
                {
                    r.RelativeItem().Text(docType).FontSize(14).Bold().FontColor(TlDark).FontFamily(Fonts.Arial);
                    r.ConstantItem(180).AlignRight().Text(docNumber).FontSize(11).FontColor(TlDark).Italic().FontFamily(Fonts.Arial);
                });
            });
        });
    }

    private static void DocDetailRow(ColumnDescriptor col, string label, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        col.Item().Row(r =>
        {
            r.ConstantItem(100).Text(label).FontSize(9).FontColor(TlGray500);
            r.RelativeItem().Text(value).FontSize(9).FontColor(TlGray700);
        });
    }

    private static void TotalRow(ColumnDescriptor col, string label, decimal amount)
    {
        col.Item().BorderBottom(0.5f).BorderColor(TlGray200).Row(r =>
        {
            r.RelativeItem().Padding(4).Text(label).FontSize(9).FontColor(TlGray500);
            r.ConstantItem(90).AlignRight().Padding(4).Text(amount.ToString("C2")).FontSize(9).FontColor(TlGray700);
        });
    }

    private static void DocFooter(PageDescriptor page, string originator)
    {
        page.Footer().Column(col =>
        {
            col.Item().BorderTop(1).BorderColor(TlGray200).PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span(CompanyName).FontSize(7.5f).FontColor(TlGray500).Bold();
                    t.Span("  ·  ").FontSize(7.5f).FontColor(TlGray200);
                    t.Span(CompanyAddress + ", " + CompanyCity).FontSize(7.5f).FontColor(TlGray500);
                    t.Span("  ·  ").FontSize(7.5f).FontColor(TlGray200);
                    t.Span(CompanyWeb).FontSize(7.5f).FontColor(TlTeal);
                });
                row.ConstantItem(100).AlignRight().Text(t =>
                {
                    t.Span("Page ").FontSize(7.5f).FontColor(TlGray500);
                    t.CurrentPageNumber().FontSize(7.5f).FontColor(TlGray700).Bold();
                    t.Span(" of ").FontSize(7.5f).FontColor(TlGray500);
                    t.TotalPages().FontSize(7.5f).FontColor(TlGray700).Bold();
                });
            });
        });
    }
}
