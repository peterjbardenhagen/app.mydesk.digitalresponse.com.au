using System.Data;
using Microsoft.Extensions.Logging;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

/// <summary>
/// Server-side PDF generation using QuestPDF.
/// Generates professional A4 PDFs for Quotes, Invoices, Purchase Orders, and Delivery Notes.
/// Branding is driven by PlatformSettings.
/// </summary>
public class PdfService
{
    private readonly DatabaseService _db;
    private readonly ILogger<PdfService> _logger;
    private PlatformSettings _settings;

    // Default branding constants as fallbacks
    private const string DefaultDark      = "#08121a";
    private const string DefaultPrimary   = "#008b8b";
    private const string DefaultAccent    = "#cca05a";
    private const string Gray50          = "#f8fafc";
    private const string Gray200         = "#eaecf0";
    private const string Gray500         = "#667085";
    private const string Gray700         = "#344054";
    private const string White           = "#ffffff";

    public PdfService(DatabaseService db, ILogger<PdfService> logger)
    {
        _db     = db;
        _logger = logger;
        _settings = new PlatformSettings(); // Will be overridden per-call or via SetSettings
    }

    public void SetSettings(PlatformSettings settings)
    {
        _settings = settings;
    }

    private string BrandDark => _settings?.PdfDarkBackground ?? DefaultDark;
    private string BrandPrimary => _settings?.PdfPrimaryColor ?? DefaultPrimary;
    private string BrandPrimaryLight => _settings?.PdfPrimaryColorLight ?? "#00a0a0";
    private string BrandAccent => _settings?.PdfAccentColor ?? DefaultAccent;

    private string CompanyName => _settings?.CompanyName ?? "MyDesk Customer";
    private string CompanyAddress => _settings?.PdfAddress1 ?? "";
    private string CompanyCity => $"{_settings?.PdfSuburb} {_settings?.PdfState} {_settings?.PdfPostCode}".Trim();
    private string CompanyWeb => _settings?.CompanyWebsite ?? "";
    private string CompanyPhone => _settings?.PdfContactPhone ?? "";
    private string CompanyEmail => _settings?.PdfContactEmail ?? "";

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
                   ISNULL(u.Email,'')                                AS OriginatorEmail,
                   ISNULL(d.GSTRate, 10.0)                           AS GSTRate,
                   ISNULL(d.Logo, '')                                AS DivisionLogo,
                   ISNULL(d.QuotePrefix, 'QT-')                      AS Prefix
            FROM Quotes q
            LEFT JOIN Contacts c ON c.ContactId = q.ContactId
            LEFT JOIN Companies co ON co.CompanyId = c.CompanyId
            LEFT JOIN Users u    ON u.Code = q.Code
            LEFT JOIN Divisions d ON d.DivisionId = q.DivisionId
            WHERE q.Qid = @Id",
            new() { ["Id"] = quoteId });

        if (hDt.Rows.Count == 0)
            throw new InvalidOperationException($"Quote {quoteId} not found");

        var h = hDt.Rows[0];
        var gstRate = Convert.ToDecimal(h["GSTRate"]) / 100m;
        var nettTotal = Convert.ToDecimal(h["NettPriceTotal"]);
        var gst       = nettTotal * gstRate;
        var prefix    = h["Prefix"].ToString();
        var docNum    = h["QuoteNumber"].ToString() is { Length: > 0 } qn ? qn : $"{prefix}{quoteId}";

        var itemsDt = await _db.QueryAsync(@"
            SELECT ISNULL(Description,'') AS Description,
                   ISNULL(Quantity,0)     AS Quantity,
                   ISNULL(NettPrice,0)    AS NettPrice,
                   ISNULL(ExtNettPrice,0) AS ExtNettPrice
            FROM QuoteContents
            WHERE Qid = @Id
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
                ORDER BY QuoteThirdPartyId",
                new() { ["Id"] = quoteId });
            tpRows = tpDt.Rows.Cast<DataRow>().ToList();
        }
        catch { }

        var lineRows  = itemsDt.Rows.Cast<DataRow>().ToList();

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
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("BILL TO").Bold().FontSize(7.5f).FontColor(BrandPrimary);
                            left.Item().PaddingTop(3).Text(h["CompanyName"].ToString()!).Bold().FontSize(11).FontColor(Gray700);
                            if (!string.IsNullOrWhiteSpace(h["ContactName"].ToString()))
                                left.Item().Text(h["ContactName"].ToString()!).FontSize(9).FontColor(Gray700);
                            if (!string.IsNullOrWhiteSpace(h["Attention"].ToString()))
                                left.Item().Text($"Attn: {h["Attention"]}").FontSize(9).Italic().FontColor(Gray500);
                            var addr = h["Address1"].ToString()!;
                            if (!string.IsNullOrWhiteSpace(addr))
                                left.Item().Text(addr).FontSize(9).FontColor(Gray500);
                            var suburb = h["Suburb"].ToString()!;
                            if (!string.IsNullOrWhiteSpace(suburb))
                            {
                                var statePc = $"{h["State"]} {h["Postcode"]}".Trim();
                                left.Item().Text($"{suburb}  {statePc}".Trim()).FontSize(9).FontColor(Gray500);
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
                                h2.Cell().Background(BrandDark).Padding(5).Text("Description").Bold().FontSize(8.5f).FontColor(Colors.White);
                                h2.Cell().Background(BrandDark).Padding(5).AlignCenter().Text("Qty").Bold().FontSize(8.5f).FontColor(Colors.White);
                                h2.Cell().Background(BrandDark).Padding(5).AlignRight().Text("Unit Price").Bold().FontSize(8.5f).FontColor(Colors.White);
                                h2.Cell().Background(BrandDark).Padding(5).AlignRight().Text("Total").Bold().FontSize(8.5f).FontColor(Colors.White);
                            });

                            bool alt = false;
                            void ItemRow(DataRow r)
                            {
                                var bg = alt ? Gray50 : White;
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
                                    .Background(Gray200).PaddingHorizontal(5).PaddingVertical(3)
                                    .Text("Third Party Supply").Bold().FontSize(8).FontColor(Gray700);
                                alt = false;
                                foreach (var r in tpRows) ItemRow(r);
                            }
                        });
                    }

                    col.Item().PaddingTop(14).AlignRight().Width(225).Column(totals =>
                    {
                        TotalRow(totals, "Subtotal (ex GST)", nettTotal);
                        TotalRow(totals, "GST (10%)", gst);
                        totals.Item().Background(BrandPrimary).Row(r =>
                        {
                            r.RelativeItem().Padding(5)
                                .Text("TOTAL (inc. GST)").Bold().FontSize(10.5f).FontColor(Colors.White);
                            r.ConstantItem(90).AlignRight().Padding(5)
                                .Text((nettTotal + gst).ToString("C2")).Bold().FontSize(10.5f).FontColor(Colors.White);
                        });
                    });

                    var notes = h["CustomerNotes"].ToString();
                    var terms = h["Terms"].ToString();
                    if (!string.IsNullOrWhiteSpace(notes))
                    {
                        col.Item().PaddingTop(18).Column(n =>
                        {
                            n.Item().Text("Notes").Bold().FontSize(9).FontColor(Gray700);
                            n.Item().PaddingTop(3).Text(notes!).FontSize(9).FontColor(Gray700);
                        });
                    }
                    if (!string.IsNullOrWhiteSpace(terms))
                    {
                        col.Item().PaddingTop(12).BorderTop(1).BorderColor(Gray200).Column(n =>
                        {
                            n.Item().PaddingTop(8).Text("Terms & Conditions").Bold().FontSize(9).FontColor(Gray700);
                            n.Item().PaddingTop(3).Text(terms!).FontSize(8).FontColor(Gray500);
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
                   ISNULL(d.InvoicePrefix, 'INV-') + CAST(i.InvoiceId AS NVARCHAR(20)) AS InvoiceNumber,
                   i.InvoiceDate, i.InvoiceDate                      AS DueDate,
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
                   ISNULL(u.Email,'')                                AS OriginatorEmail,
                   ISNULL(d.Logo, '')                                AS DivisionLogo
            FROM Invoices i
            LEFT JOIN Contacts c       ON c.ContactId = i.ContactId
            LEFT JOIN Companies co     ON co.CompanyId = i.CompanyId
            LEFT JOIN InvoiceStatus ists ON ists.InvoiceStatusId = i.InvoiceStatusId
            LEFT JOIN Users u          ON u.Code = i.Code
            LEFT JOIN Divisions d      ON i.DivisionId = d.DivisionId
            WHERE i.InvoiceId = @Id",
            new() { ["Id"] = invoiceId });

        if (dt.Rows.Count == 0)
            throw new InvalidOperationException($"Invoice {invoiceId} not found");

        var h           = dt.Rows[0];
        var amtExGst    = Convert.ToDecimal(h["AmountExGST"]);
        var gst         = Convert.ToDecimal(h["GST"]);
        var total       = amtExGst + gst;
        var invNum      = h["InvoiceNumber"].ToString();

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
                            left.Item().Text("BILL TO").Bold().FontSize(7.5f).FontColor(BrandPrimary);
                            left.Item().PaddingTop(3).Text(h["CompanyName"].ToString()!).Bold().FontSize(11).FontColor(Gray700);
                            if (!string.IsNullOrWhiteSpace(h["ContactName"].ToString()))
                                left.Item().Text(h["ContactName"].ToString()!).FontSize(9).FontColor(Gray700);
                            var addr = h["Address1"].ToString()!;
                            if (!string.IsNullOrWhiteSpace(addr)) left.Item().Text(addr).FontSize(9).FontColor(Gray500);
                            var suburb = h["Suburb"].ToString()!;
                            if (!string.IsNullOrWhiteSpace(suburb))
                            {
                                var sp = $"{h["State"]} {h["Postcode"]}".Trim();
                                left.Item().Text($"{suburb}  {sp}".Trim()).FontSize(9).FontColor(Gray500);
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
                        totals.Item().Background(BrandPrimary).Row(r =>
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
                   ISNULL(d.POPrefix, 'PO-') + CAST(p.POid AS NVARCHAR(20)) AS PONumber,
                   p.PODate, p.DateRequired          AS ExpectedDelivery,
                   ISNULL(p.PriceExTotal,0)          AS AmountExGST,
                   ISNULL(p.PriceIncTotal,0)         AS Amount,
                   ISNULL(p.Project,'')              AS Reference,
                   ISNULL(ps.POStatus,'')            AS StatusName,
                   COALESCE(NULLIF(s.Company,''), NULLIF(LTRIM(RTRIM(CONCAT(ISNULL(c.FirstName,''), ' ', ISNULL(cn.Surname,'')))), ''), '') AS SupplierName,
                   ISNULL(u.Name,'')                 AS OriginatorName,
                   ISNULL(u.Email,'')                AS OriginatorEmail,
                   ISNULL(d.Logo, '')                AS DivisionLogo
            FROM PurchaseOrders p
            LEFT JOIN Contacts cn         ON cn.ContactId = p.ContactId
            LEFT JOIN Companies s         ON s.CompanyId = cn.CompanyId
            LEFT JOIN PurchaseOrderStatus ps ON ps.POStatusId = p.POStatusId
            LEFT JOIN Users u             ON u.Code = p.Code
            LEFT JOIN Divisions d         ON d.DivisionId = p.DivisionId
            WHERE p.POid = @Id",
            new() { ["Id"] = poId });

        if (dt.Rows.Count == 0)
            throw new InvalidOperationException($"Purchase Order {poId} not found");

        var h         = dt.Rows[0];
        var amtExGst  = Convert.ToDecimal(h["AmountExGST"]);
        var gst       = h["Amount"] != DBNull.Value ? Convert.ToDecimal(h["Amount"]) - amtExGst : amtExGst * 0.1m;
        var total     = h["Amount"] != DBNull.Value ? Convert.ToDecimal(h["Amount"]) : amtExGst + gst;
        var poNum     = h["PONumber"].ToString();

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
                            left.Item().Text("SUPPLIER").Bold().FontSize(7.5f).FontColor(BrandPrimary);
                            left.Item().PaddingTop(3).Text(h["SupplierName"].ToString()!).Bold().FontSize(11).FontColor(Gray700);
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
                        totals.Item().Background(BrandPrimary).Row(r =>
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

    private void DocHeader(IContainer container, string docType, string docNumber,
        string originator, string email)
    {
        container.Column(col =>
        {
            col.Item().Background(BrandDark).Row(row =>
            {
                row.RelativeItem().PaddingHorizontal(16).PaddingVertical(12).Row(logoRow =>
                {
                    logoRow.ConstantItem(44).AlignMiddle().Svg(
                        $@"<svg width='44' height='44' viewBox='0 0 44 44' xmlns='http://www.w3.org/2000/svg'>
                          <circle cx='22' cy='22' r='20' stroke='{BrandPrimary}' stroke-width='2' fill='none' />
                          <circle cx='22' cy='22' r='14' stroke='{BrandPrimaryLight}' stroke-width='1.5' fill='none' />
                          <circle cx='22' cy='22' r='8' fill='{BrandDark}' />
                          <circle cx='22' cy='22' r='4' fill='{BrandPrimaryLight}' />
                        </svg>");
                    logoRow.RelativeItem().PaddingLeft(8).AlignMiddle().Column(lc =>
                    {
                        lc.Item().Text(CompanyName).FontSize(18).Bold().FontColor(Colors.White).FontFamily(Fonts.Arial);
                        lc.Item().Text(_settings?.CompanyLegalName?.Contains("Pty") == true ? "Pty Ltd" : "").FontSize(8).FontColor(BrandPrimaryLight).FontFamily(Fonts.Arial);
                    });
                });

                row.ConstantItem(200).PaddingVertical(12).PaddingRight(16).AlignRight().Column(right =>
                {
                    if (!string.IsNullOrWhiteSpace(CompanyAddress))
                        right.Item().Text(CompanyAddress).FontSize(8).FontColor(Gray500).FontFamily(Fonts.Arial);
                    if (!string.IsNullOrWhiteSpace(CompanyCity))
                        right.Item().Text(CompanyCity).FontSize(8).FontColor(Gray500).FontFamily(Fonts.Arial);
                    if (!string.IsNullOrWhiteSpace(CompanyWeb))
                        right.Item().Text(CompanyWeb).FontSize(8).FontColor(BrandPrimaryLight).FontFamily(Fonts.Arial);
                    if (!string.IsNullOrWhiteSpace(originator))
                        right.Item().PaddingTop(3).Text(originator).FontSize(8).FontColor(Colors.White).FontFamily(Fonts.Arial);
                    if (!string.IsNullOrWhiteSpace(email))
                        right.Item().Text(email).FontSize(8).FontColor(Gray500).FontFamily(Fonts.Arial);
                });
            });

            col.Item().Background(BrandAccent).Row(bar =>
            {
                bar.RelativeItem().PaddingHorizontal(16).PaddingVertical(8).Row(r =>
                {
                    r.RelativeItem().Text(docType).FontSize(14).Bold().FontColor(BrandDark).FontFamily(Fonts.Arial);
                    r.ConstantItem(180).AlignRight().Text(docNumber).FontSize(11).FontColor(BrandDark).Italic().FontFamily(Fonts.Arial);
                });
            });
        });
    }

    private static void DocDetailRow(ColumnDescriptor col, string label, string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        col.Item().Row(r =>
        {
            r.ConstantItem(100).Text(label).FontSize(9).FontColor(Gray500);
            r.RelativeItem().Text(value).FontSize(9).FontColor(Gray700);
        });
    }

    private static void TotalRow(ColumnDescriptor col, string label, decimal amount)
    {
        col.Item().BorderBottom(0.5f).BorderColor(Gray200).Row(r =>
        {
            r.RelativeItem().Padding(4).Text(label).FontSize(9).FontColor(Gray500);
            r.ConstantItem(90).AlignRight().Padding(4).Text(amount.ToString("C2")).FontSize(9).FontColor(Gray700);
        });
    }

    private void DocFooter(PageDescriptor page, string originator)
    {
        page.Footer().Column(col =>
        {
            col.Item().BorderTop(1).BorderColor(Gray200).PaddingTop(6).Row(row =>
            {
                row.RelativeItem().Text(t =>
                {
                    t.Span(CompanyName).FontSize(7.5f).FontColor(Gray500).Bold();
                    t.Span("  ·  ").FontSize(7.5f).FontColor(Gray200);
                    if (!string.IsNullOrWhiteSpace(CompanyAddress))
                    {
                        t.Span(CompanyAddress).FontSize(7.5f).FontColor(Gray500);
                        t.Span(", ").FontSize(7.5f).FontColor(Gray200);
                    }
                    t.Span(CompanyCity).FontSize(7.5f).FontColor(Gray500);
                    t.Span("  ·  ").FontSize(7.5f).FontColor(Gray200);
                    t.Span(CompanyWeb).FontSize(7.5f).FontColor(BrandPrimary);
                });
                row.ConstantItem(100).AlignRight().Text(t =>
                {
                    t.Span("Page ").FontSize(7.5f).FontColor(Gray500);
                    t.CurrentPageNumber().FontSize(7.5f).FontColor(Gray700).Bold();
                    t.Span(" of ").FontSize(7.5f).FontColor(Gray500);
                    t.TotalPages().FontSize(7.5f).FontColor(Gray700).Bold();
                });
            });
        });
    }
}