using System.Text.Json;
using MyDesk.Shared.Models;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Services;

/// <summary>
/// JSON-backed Email Campaign store + sender.
/// Uses existing EmailService for outbound delivery.
/// </summary>
public class CampaignService
{
    private readonly string _path;
    private readonly EmailService _email;
    private readonly DatabaseService _db;
    private readonly MarketingDataService _data;
    private readonly ILogger<CampaignService> _logger;
    private readonly object _lock = new();

    public CampaignService(
        IWebHostEnvironment env,
        EmailService email,
        DatabaseService db,
        MarketingDataService data,
        ILogger<CampaignService> logger)
    {
        var dir = Path.Combine(env.ContentRootPath, "Config");
        Directory.CreateDirectory(dir);
        _path = Path.Combine(dir, "marketing-campaigns.json");
        _email = email;
        _db = db;
        _data = data;
        _logger = logger;
    }

    public List<EmailCampaign> GetAll()
    {
        lock (_lock) return Load().OrderByDescending(c => c.CreatedAt).ToList();
    }

    public EmailCampaign? Get(string id)
    {
        lock (_lock) return Load().FirstOrDefault(c => c.Id == id);
    }

    public EmailCampaign Save(EmailCampaign c)
    {
        lock (_lock)
        {
            var all = Load();
            var existing = all.FirstOrDefault(x => x.Id == c.Id);
            if (existing != null) all.Remove(existing);
            all.Add(c);
            Persist(all);
            return c;
        }
    }

    public void Delete(string id)
    {
        lock (_lock)
        {
            var all = Load();
            all.RemoveAll(c => c.Id == id);
            Persist(all);
        }
    }

    // ── Audiences ─────────────────────────────────────────────────────────────
    public async Task<List<CampaignRecipient>> ResolveAudienceAsync(string audience, List<string>? customEmails = null)
    {
        var list = new List<CampaignRecipient>();

        if (audience == "custom" && customEmails != null)
        {
            foreach (var e in customEmails.Where(e => !string.IsNullOrWhiteSpace(e)))
            {
                list.Add(new CampaignRecipient { Email = e.Trim(), Reason = "Custom list" });
            }
            return list;
        }

        var cdp = await _data.GetCustomerDataAsync();
        List<CustomerScoreCard> targets = audience switch
        {
            "champions"        => cdp.All.Where(c => c.Segment == "Champions").ToList(),
            "loyal"            => cdp.All.Where(c => c.Segment is "Loyal" or "Champions").ToList(),
            "at-risk"          => cdp.All.Where(c => c.Segment is "At Risk" or "Hibernating Whale" or "Needs Attention").ToList(),
            "top-50-customers" => cdp.All.Take(50).ToList(),
            "all-active"       => cdp.All.Where(c => c.DaysSinceLastActivity <= 180).ToList(),
            _                  => cdp.All.Take(25).ToList()
        };

        // Resolve contact emails for these companies
        if (targets.Count == 0) return list;

        var ids = string.Join(",", targets.Select(t => t.CompanyId));
        var sql = $@"
            SELECT TOP 500 c.CompanyId, c.Company, ct.FirstName, ct.Surname, ct.Email
            FROM Contacts ct
            INNER JOIN Companies c ON c.CompanyId = ct.CompanyId
            WHERE ct.CompanyId IN ({ids})
              AND ct.Email IS NOT NULL AND ct.Email <> ''
              AND (ct.Active = 1 OR ct.Active IS NULL)";

        var dt = await _db.QueryAsync(sql);
        foreach (System.Data.DataRow r in dt.Rows)
        {
            var cid = Convert.ToInt32(r["CompanyId"]);
            var card = targets.FirstOrDefault(t => t.CompanyId == cid);
            list.Add(new CampaignRecipient
            {
                CompanyId = cid,
                CompanyName = r["Company"].ToString() ?? "",
                ContactName = $"{r["FirstName"]} {r["Surname"]}".Trim(),
                Email = r["Email"].ToString() ?? "",
                Reason = card?.Segment ?? ""
            });
        }

        // Deduplicate by email
        return list
            .GroupBy(r => r.Email.ToLowerInvariant())
            .Select(g => g.First())
            .ToList();
    }

    // ── Send ──────────────────────────────────────────────────────────────────
    public async Task<EmailCampaign> SendAsync(string id, string userCode)
    {
        var c = Get(id) ?? throw new InvalidOperationException("Campaign not found");
        if (c.Status == "Sent")
            throw new InvalidOperationException("Campaign already sent");

        c.Status = "Sending";
        c.SentCount = 0;
        c.FailedCount = 0;
        c.Log.Clear();
        Save(c);

        try
        {
            var recipients = await ResolveAudienceAsync(c.Audience, c.CustomEmails);
            c.Log.Add($"Resolved {recipients.Count} recipients from audience '{c.Audience}'");
            foreach (var r in recipients)
            {
                try
                {
                    // Personalise body with simple {{ContactName}} / {{CompanyName}} tokens
                    var body = c.BodyHtml
                        .Replace("{{ContactName}}", r.ContactName)
                        .Replace("{{CompanyName}}", r.CompanyName);
                    var subject = c.Subject
                        .Replace("{{ContactName}}", r.ContactName)
                        .Replace("{{CompanyName}}", r.CompanyName);

                    await _email.SendAsync(r.Email, subject, body,
                        fromEmail: string.IsNullOrWhiteSpace(c.FromAddress) ? null : c.FromAddress,
                        fromName:  string.IsNullOrWhiteSpace(c.FromName)    ? null : c.FromName);
                    c.SentCount++;
                }
                catch (Exception ex)
                {
                    c.FailedCount++;
                    c.Log.Add($"[FAIL] {r.Email}: {ex.Message}");
                    _logger.LogWarning(ex, "Campaign send failed for {Email}", r.Email);
                }
            }

            c.Status = c.FailedCount == 0 ? "Sent" : "Sent (with errors)";
            c.SentAt = DateTime.Now;
        }
        catch (Exception ex)
        {
            c.Status = "Failed";
            c.Log.Add($"[ERROR] {ex.Message}");
            _logger.LogError(ex, "Campaign send failed: {Id}", c.Id);
        }

        Save(c);
        return c;
    }

    private List<EmailCampaign> Load()
    {
        if (!File.Exists(_path)) return new();
        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<List<EmailCampaign>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        }
        catch { return new(); }
    }

    private void Persist(List<EmailCampaign> all)
    {
        var json = JsonSerializer.Serialize(all,
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_path, json);
    }
}
