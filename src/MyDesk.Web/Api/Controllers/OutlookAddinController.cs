using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MyDesk.Shared.Models;
using MyDesk.Shared.Services;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace MyDesk.Web.Api.Controllers;

/// <summary>
/// Backend for the MyDesk Outlook add-in (Outlook.Addin/manifest.xml).
/// Three workflows:
///   1. POST /change-request  — log an email as a lightweight client change request.
///   2. POST /contact-email   — save an email against a MyDesk contact (auto-create if missing).
///   3. POST /legal-report    — receive a batch of Outlook messages and return a folio-billing PDF.
///
/// Auth: X-Api-Key (same scheme used by the rest of /api/v1/*).
/// </summary>
[ApiController]
[Route("api/outlook-addin")]
[Authorize(AuthenticationSchemes = ApiKeyAuthenticationHandler.SchemeName)]
[Produces("application/json")]
public class OutlookAddinController : ControllerBase
{
    private readonly DatabaseService _db;
    private readonly ILogger<OutlookAddinController> _logger;

    public OutlookAddinController(DatabaseService db, ILogger<OutlookAddinController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // ------------------------------------------------------------------
    // Health / auth-check
    // ------------------------------------------------------------------
    [HttpGet("ping")]
    public IActionResult Ping() => Ok(new { ok = true, service = "outlook-addin" });

    // ------------------------------------------------------------------
    // 1. Log Change Request
    // ------------------------------------------------------------------
    [HttpPost("change-request")]
    public async Task<IActionResult> LogChangeRequest([FromBody] ChangeRequestPayload payload)
    {
        if (payload is null || string.IsNullOrWhiteSpace(payload.Subject))
            return BadRequest(new { error = "Subject required." });

        await EnsureChangeRequestTableAsync();

        var sql = @"
            INSERT INTO OutlookChangeRequests
              (LoggedAt, LoggedBy, FromEmail, FromName, Subject, ReceivedAt, Body, Notes,
               EstimatedImpact, OutlookItemId, Status)
            VALUES
              (SYSUTCDATETIME(), @LoggedBy, @FromEmail, @FromName, @Subject, @ReceivedAt, @Body, @Notes,
               @EstimatedImpact, @OutlookItemId, 'New')";

        var id = await _db.InsertAsync(sql, new()
        {
            ["LoggedBy"] = (object?)User.Identity?.Name ?? DBNull.Value,
            ["FromEmail"] = Trunc(payload.FromEmail, 320),
            ["FromName"] = Trunc(payload.FromName, 200),
            ["Subject"] = Trunc(payload.Subject, 500),
            ["ReceivedAt"] = (object?)payload.ReceivedAt ?? DBNull.Value,
            ["Body"] = Trunc(payload.Body, 8000),
            ["Notes"] = Trunc(payload.Notes, 4000),
            ["EstimatedImpact"] = (object?)payload.ImpactCost ?? DBNull.Value,
            ["OutlookItemId"] = Trunc(payload.OutlookItemId, 500),
        });

        _logger.LogInformation("Outlook add-in logged change request #{Id} from {From}", id, payload.FromEmail);
        return Ok(new { id, ok = true });
    }

    // ------------------------------------------------------------------
    // 2. Add Email to Contact
    // ------------------------------------------------------------------
    [HttpPost("contact-email")]
    public async Task<IActionResult> AddEmailToContact([FromBody] ContactEmailPayload payload)
    {
        if (payload is null || string.IsNullOrWhiteSpace(payload.FromEmail))
            return BadRequest(new { error = "Sender email required." });

        var email = payload.FromEmail.Trim().ToLowerInvariant();

        // 1. Try to find an existing contact by email (case-insensitive on SQL side too).
        var findSql = @"SELECT TOP 1 ContactId FROM Contacts WHERE LOWER(ISNULL(Email,'')) = @Email";
        var existing = await _db.ExecuteScalarAsync<int?>(findSql, new { Email = email });

        int contactId;
        bool contactCreated = false;

        if (existing.HasValue && existing.Value > 0)
        {
            contactId = existing.Value;
        }
        else
        {
            // 2. Create a new Contact from the sender's display name.
            var (first, last) = SplitName(payload.FromName, payload.FromEmail);
            var insertSql = @"
                INSERT INTO Contacts (FirstName, Surname, Email, Code)
                VALUES (@First, @Last, @Email, @Code)";
            contactId = await _db.InsertAsync(insertSql, new()
            {
                ["First"] = Trunc(first, 100),
                ["Last"] = Trunc(last, 100),
                ["Email"] = Trunc(email, 200),
                ["Code"] = "Outlook",
            });
            contactCreated = true;
            _logger.LogInformation("Outlook add-in created contact #{Id} for {Email}", contactId, email);
        }

        // 3. Add the ContactNote.
        var noteText = ComposeNoteText(payload);
        var noteSql = @"
            INSERT INTO ContactNotes (ContactId, Date, NoteType, NoteText, CreatedBy)
            VALUES (@ContactId, @Date, @NoteType, @NoteText, @CreatedBy)";
        var noteId = await _db.InsertAsync(noteSql, new()
        {
            ["ContactId"] = contactId,
            ["Date"] = payload.ReceivedAt ?? DateTime.UtcNow,
            ["NoteType"] = string.IsNullOrWhiteSpace(payload.NoteType) ? "Email" : payload.NoteType!,
            ["NoteText"] = Trunc(noteText, 4000),
            ["CreatedBy"] = User.Identity?.Name ?? "Outlook Add-in",
        });

        return Ok(new { ok = true, contactId, contactCreated, noteId });
    }

    private static string ComposeNoteText(ContactEmailPayload p)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Subject: {p.Subject}");
        if (p.ReceivedAt.HasValue)
            sb.AppendLine($"Received: {p.ReceivedAt.Value.ToLocalTime():yyyy-MM-dd HH:mm}");
        if (!string.IsNullOrWhiteSpace(p.OutlookItemId))
            sb.AppendLine($"Outlook item: {p.OutlookItemId}");
        sb.AppendLine();
        sb.Append(p.Body ?? "");
        return sb.ToString();
    }

    private static (string first, string last) SplitName(string? displayName, string email)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            // "Jane Smith" → ("Jane", "Smith"); "Smith, Jane" → ("Jane", "Smith")
            var name = displayName.Trim();
            if (name.Contains(','))
            {
                var parts = name.Split(',', 2, StringSplitOptions.TrimEntries);
                return (parts.Length > 1 ? parts[1] : "", parts[0]);
            }
            var space = name.Split(' ', 2, StringSplitOptions.TrimEntries);
            return space.Length == 1 ? (space[0], "") : (space[0], space[1]);
        }
        // Fall back to the local-part of the email.
        var local = email.Split('@')[0];
        return (local, "");
    }

    // ------------------------------------------------------------------
    // 3. Legal Report (PDF)
    // ------------------------------------------------------------------
    [HttpPost("legal-report")]
    public IActionResult LegalReport([FromBody] LegalReportPayload payload)
    {
        if (payload is null || payload.Messages is null || payload.Messages.Count == 0)
            return BadRequest(new { error = "No messages supplied." });

        // Recompute folios on the server so the client can't accidentally mis-count.
        foreach (var m in payload.Messages)
            m.Folios = Math.Max(1, (int)Math.Ceiling(m.WordCount / 100.0));

        var pdfBytes = BuildLegalReportPdf(payload);
        var filename = $"legal-folio-report-{payload.FromDate:yyyy-MM-dd}_to_{payload.ToDate:yyyy-MM-dd}.pdf";
        return File(pdfBytes, "application/pdf", filename);
    }

    private static byte[] BuildLegalReportPdf(LegalReportPayload payload)
    {
        var totalWords = payload.Messages.Sum(m => m.WordCount);
        var totalFolios = payload.Messages.Sum(m => m.Folios);
        var totalAttachments = payload.Messages.Sum(m => m.Attachments?.Count ?? 0);

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.MarginHorizontal(1.5f, Unit.Centimetre);
                page.MarginVertical(1.2f, Unit.Centimetre);
                page.DefaultTextStyle(s => s.FontFamily(Fonts.Arial).FontSize(9));

                page.Header().Column(h =>
                {
                    h.Item().Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("Legal Folio Report").Bold().FontSize(18).FontColor("#08121a");
                            c.Item().PaddingTop(2).Text($"{payload.FromDate:d MMM yyyy} — {payload.ToDate:d MMM yyyy}")
                                .FontSize(10).FontColor("#667085");
                        });
                        r.ConstantItem(140).AlignRight().Column(c =>
                        {
                            c.Item().Text("Generated").FontSize(8).FontColor("#667085");
                            c.Item().Text(DateTime.Now.ToString("d MMM yyyy HH:mm")).FontSize(9).FontColor("#344054");
                        });
                    });
                    h.Item().PaddingTop(6).LineHorizontal(1).LineColor("#008b8b");
                });

                page.Content().PaddingTop(12).Column(col =>
                {
                    // Filter summary
                    col.Item().Column(f =>
                    {
                        f.Item().Text("Filter").Bold().FontSize(10).FontColor("#344054");
                        f.Item().PaddingTop(2).Text(
                            payload.EmailAddresses is { Count: > 0 }
                                ? $"Email addresses: {string.Join(", ", payload.EmailAddresses)}"
                                : "Email addresses: (all senders / recipients)"
                        ).FontSize(9).FontColor("#667085");
                    });

                    // Totals band
                    col.Item().PaddingTop(10).Background("#f8fafc").Padding(10).Row(r =>
                    {
                        void Kpi(string label, string value)
                        {
                            r.RelativeItem().Column(c =>
                            {
                                c.Item().Text(label).FontSize(8).FontColor("#667085");
                                c.Item().Text(value).FontSize(14).Bold().FontColor("#08121a");
                            });
                        }
                        Kpi("Emails", payload.Messages.Count.ToString());
                        Kpi("Words",  totalWords.ToString("N0"));
                        Kpi("Folios", totalFolios.ToString("N0"));
                        Kpi("Attachments", totalAttachments.ToString("N0"));
                    });

                    // Table
                    col.Item().PaddingTop(12).Table(table =>
                    {
                        table.ColumnsDefinition(cd =>
                        {
                            cd.ConstantColumn(70);   // Date
                            cd.RelativeColumn(3);    // Subject
                            cd.RelativeColumn(2);    // Correspondents
                            cd.RelativeColumn(2);    // Attachments
                            cd.ConstantColumn(45);   // Words
                            cd.ConstantColumn(45);   // Folios
                        });

                        // Header
                        void Head(string t) => table.Cell().Background("#08121a").Padding(5)
                            .Text(t).Bold().FontSize(9).FontColor("#ffffff");
                        Head("Date");
                        Head("Subject");
                        Head("Correspondents");
                        Head("Attachments");
                        table.Cell().Background("#08121a").Padding(5).AlignRight().Text("Words").Bold().FontSize(9).FontColor("#ffffff");
                        table.Cell().Background("#08121a").Padding(5).AlignRight().Text("Folios").Bold().FontSize(9).FontColor("#ffffff");

                        var alt = false;
                        foreach (var m in payload.Messages)
                        {
                            var bg = alt ? "#f8fafc" : "#ffffff";

                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(5)
                                .Text(m.ReceivedAt?.ToString("dd MMM yyyy") ?? "").FontSize(9);

                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(5).Column(c =>
                            {
                                c.Item().Text(string.IsNullOrWhiteSpace(m.Subject) ? "(no subject)" : m.Subject).FontSize(9).Bold();
                                if (!string.IsNullOrWhiteSpace(m.BodyPreview))
                                    c.Item().PaddingTop(1).Text(m.BodyPreview!.Length > 140 ? m.BodyPreview[..140] + "…" : m.BodyPreview)
                                        .FontSize(8).FontColor("#667085");
                            });

                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(5).Column(c =>
                            {
                                if (!string.IsNullOrWhiteSpace(m.FromEmail))
                                    c.Item().Text($"From: {m.FromEmail}").FontSize(8);
                                if (m.ToRecipients is { Count: > 0 })
                                    c.Item().Text($"To: {string.Join(", ", m.ToRecipients)}").FontSize(8).FontColor("#667085");
                            });

                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(5).Column(c =>
                            {
                                if (m.Attachments is null || m.Attachments.Count == 0)
                                {
                                    c.Item().Text("—").FontSize(8).FontColor("#667085");
                                }
                                else
                                {
                                    foreach (var a in m.Attachments)
                                        c.Item().Text($"• {a.Name}").FontSize(8);
                                }
                            });

                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(5).AlignRight()
                                .Text(m.WordCount.ToString("N0")).FontSize(9);
                            table.Cell().Background(bg).BorderBottom(0.5f).BorderColor("#e5e7eb").Padding(5).AlignRight()
                                .Text(m.Folios.ToString("N0")).FontSize(9).Bold();

                            alt = !alt;
                        }

                        // Footer totals row
                        table.Cell().ColumnSpan(4).Background("#008b8b").Padding(5)
                            .Text("TOTAL").Bold().FontSize(10).FontColor("#ffffff");
                        table.Cell().Background("#008b8b").Padding(5).AlignRight()
                            .Text(totalWords.ToString("N0")).Bold().FontSize(10).FontColor("#ffffff");
                        table.Cell().Background("#008b8b").Padding(5).AlignRight()
                            .Text(totalFolios.ToString("N0")).Bold().FontSize(10).FontColor("#ffffff");
                    });

                    col.Item().PaddingTop(14).Text(
                        "Folios calculated at 1 folio per 100 words (or part thereof), minimum 1 folio per message. " +
                        "Only the latest message per conversation thread is included."
                    ).FontSize(8).Italic().FontColor("#667085");
                });

                page.Footer().AlignCenter().Row(r =>
                {
                    r.RelativeItem().AlignLeft().Text("MyDesk Outlook add-in").FontSize(8).FontColor("#667085");
                    r.RelativeItem().AlignRight().Text(x =>
                    {
                        x.Span("Page ").FontSize(8).FontColor("#667085");
                        x.CurrentPageNumber().FontSize(8).FontColor("#667085");
                        x.Span(" of ").FontSize(8).FontColor("#667085");
                        x.TotalPages().FontSize(8).FontColor("#667085");
                    });
                });
            });
        }).GeneratePdf();
    }

    // ------------------------------------------------------------------
    // Schema bootstrap for the OutlookChangeRequests table.
    // ------------------------------------------------------------------
    private async Task EnsureChangeRequestTableAsync()
    {
        const string sql = @"
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'OutlookChangeRequests')
            BEGIN
                CREATE TABLE OutlookChangeRequests (
                    Id INT IDENTITY(1,1) PRIMARY KEY,
                    LoggedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
                    LoggedBy NVARCHAR(200) NULL,
                    FromEmail NVARCHAR(320) NULL,
                    FromName NVARCHAR(200) NULL,
                    Subject NVARCHAR(500) NOT NULL,
                    ReceivedAt DATETIME2 NULL,
                    Body NVARCHAR(MAX) NULL,
                    Notes NVARCHAR(MAX) NULL,
                    EstimatedImpact DECIMAL(12,2) NULL,
                    OutlookItemId NVARCHAR(500) NULL,
                    Status NVARCHAR(50) NOT NULL DEFAULT 'New'
                );
                CREATE INDEX IX_OutlookChangeRequests_LoggedAt ON OutlookChangeRequests(LoggedAt DESC);
                CREATE INDEX IX_OutlookChangeRequests_FromEmail ON OutlookChangeRequests(FromEmail);
            END";
        await _db.ExecuteNonQueryAsync(sql);
    }

    private static string Trunc(string? s, int max) =>
        string.IsNullOrEmpty(s) ? string.Empty : (s.Length <= max ? s : s.Substring(0, max));
}

// ----------------------------------------------------------------------
// DTOs
// ----------------------------------------------------------------------
public sealed class ChangeRequestPayload
{
    public string? Title { get; set; }
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
    public string Subject { get; set; } = "";
    public DateTime? ReceivedAt { get; set; }
    public string? Body { get; set; }
    public string? Notes { get; set; }
    public decimal? ImpactCost { get; set; }
    public string? OutlookItemId { get; set; }
}

public sealed class ContactEmailPayload
{
    public string FromEmail { get; set; } = "";
    public string? FromName { get; set; }
    public string Subject { get; set; } = "";
    public DateTime? ReceivedAt { get; set; }
    public string? Body { get; set; }
    public string? NoteType { get; set; } = "Email";
    public string? OutlookItemId { get; set; }
}

public sealed class LegalReportPayload
{
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public List<string>? EmailAddresses { get; set; }
    public List<LegalReportMessage> Messages { get; set; } = new();
}

public sealed class LegalReportMessage
{
    public string? Id { get; set; }
    public string? ConversationId { get; set; }
    public string? Subject { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? FromEmail { get; set; }
    public string? FromName { get; set; }
    public List<string>? ToRecipients { get; set; }
    public int WordCount { get; set; }
    public int Folios { get; set; }
    public string? BodyPreview { get; set; }
    public List<LegalReportAttachment>? Attachments { get; set; }
    public string? WebLink { get; set; }
}

public sealed class LegalReportAttachment
{
    public string Name { get; set; } = "";
    public long Size { get; set; }
    public string? ContentType { get; set; }
}
