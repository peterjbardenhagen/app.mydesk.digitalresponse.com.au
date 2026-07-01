using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;


namespace MyDesk.Shared.Services.Integrations;

/// <summary>
/// Xero OAuth 2.0 + two-way accounting sync via direct HttpClient (no Xero SDK).
/// All endpoints use the Xero API base: https://api.xero.com/api.xro/2.0/
/// </summary>
public class XeroSyncService
{
    private const string AuthUrl   = "https://login.xero.com/identity/connect/authorize";
    private const string TokenUrl  = "https://identity.xero.com/connect/token";
    private const string ApiBase   = "https://api.xero.com/api.xro/2.0/";
    private const string ConnUrl   = "https://api.xero.com/connections";

    private readonly DatabaseService _db;
    private readonly IAccountingSettingsService _settings;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<XeroSyncService> _logger;

    public XeroSyncService(
        DatabaseService db,
        IAccountingSettingsService settings,
        IHttpClientFactory httpFactory,
        ILogger<XeroSyncService> logger)
    {
        _db = db;
        _settings = settings;
        _httpFactory = httpFactory;
        _logger = logger;
    }

    // ── OAuth ──────────────────────────────────────────────────────────────────

    public Task<string> GetAuthUrlAsync(string redirectUri)
    {
        var cfg = _settings.Current.Xero;
        var url = $"{AuthUrl}?response_type=code" +
                  $"&client_id={Uri.EscapeDataString(cfg.ClientId ?? "")}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&scope={Uri.EscapeDataString("openid profile email accounting.contacts accounting.transactions accounting.settings offline_access")}" +
                  $"&state=xero";
        return Task.FromResult(url);
    }

    public async Task<bool> ExchangeCodeAsync(string code, string redirectUri)
    {
        var cfg = _settings.Current.Xero;
        if (string.IsNullOrWhiteSpace(cfg.ClientId) || string.IsNullOrWhiteSpace(cfg.ClientSecret))
            return false;

        var form = new Dictionary<string, string>
        {
            ["grant_type"]    = "authorization_code",
            ["code"]          = code,
            ["redirect_uri"]  = redirectUri,
            ["client_id"]     = cfg.ClientId!,
            ["client_secret"] = cfg.ClientSecret!,
        };

        var http = _httpFactory.CreateClient();
        var req  = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
        {
            Content = new FormUrlEncodedContent(form)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{cfg.ClientId}:{cfg.ClientSecret}")));

        using var resp = await http.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Xero token exchange failed: {Status} {Body}", resp.StatusCode, body);
            return false;
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        cfg.AccessToken  = root.TryGetProperty("access_token", out var at)  ? at.GetString() : null;
        cfg.RefreshToken = root.TryGetProperty("refresh_token", out var rt)  ? rt.GetString() : null;
        cfg.TokenExpiry  = root.TryGetProperty("expires_in",   out var ei)
            ? DateTime.UtcNow.AddSeconds(ei.GetInt32() - 60) : DateTime.UtcNow.AddMinutes(29);
        cfg.IsConnected  = true;
        cfg.Status       = "Connected";

        // Fetch tenant id
        cfg.TenantId = await FetchTenantIdAsync(cfg.AccessToken!);

        await _settings.SaveAsync();
        return true;
    }

    public async Task<bool> RefreshTokenIfNeededAsync()
    {
        var cfg = _settings.Current.Xero;
        if (!cfg.IsConnected || string.IsNullOrWhiteSpace(cfg.RefreshToken)) return false;
        if (cfg.TokenExpiry.HasValue && cfg.TokenExpiry.Value > DateTime.UtcNow.AddMinutes(2)) return true;

        var form = new Dictionary<string, string>
        {
            ["grant_type"]    = "refresh_token",
            ["refresh_token"] = cfg.RefreshToken!,
        };

        var http = _httpFactory.CreateClient();
        var req  = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
        {
            Content = new FormUrlEncodedContent(form)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{cfg.ClientId}:{cfg.ClientSecret}")));

        using var resp = await http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            cfg.IsConnected = false;
            cfg.Status = "Token refresh failed";
            await _settings.SaveAsync();
            return false;
        }

        var body = await resp.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        cfg.AccessToken  = root.TryGetProperty("access_token", out var at)  ? at.GetString() : cfg.AccessToken;
        cfg.RefreshToken = root.TryGetProperty("refresh_token", out var rt)  ? rt.GetString() : cfg.RefreshToken;
        cfg.TokenExpiry  = root.TryGetProperty("expires_in",   out var ei)
            ? DateTime.UtcNow.AddSeconds(ei.GetInt32() - 60) : DateTime.UtcNow.AddMinutes(29);

        await _settings.SaveAsync();
        return true;
    }

    // ── Contacts ───────────────────────────────────────────────────────────────

    public async Task<SyncLogEntry> SyncContactsFromXeroAsync()
    {
        var log = StartLog("Xero", "Contact", "Pull");
        if (!await EnsureReadyAsync()) return FailLog(log, "Not connected or not enabled");

        try
        {
            var resp = await CallXeroAsync("Contacts?where=IsSupplier=false OR IsCustomer=true&includeArchived=false");
            if (resp is null) return FailLog(log, "No response from Xero Contacts API");

            using var doc  = JsonDocument.Parse(resp);
            var contacts   = doc.RootElement.TryGetProperty("Contacts", out var arr) ? arr : default;
            if (contacts.ValueKind != JsonValueKind.Array) return FailLog(log, "Unexpected Contacts response");

            int count = 0;
            foreach (var c in contacts.EnumerateArray())
            {
                try
                {
                    var xeroId   = c.TryGetProperty("ContactID",   out var xid) ? xid.GetString() : null;
                    var name     = c.TryGetProperty("Name",        out var n)   ? n.GetString()   : null;
                    var email    = c.TryGetProperty("EmailAddress",out var em)  ? em.GetString()  : null;
                    var phone    = GetFirstPhone(c);
                    if (string.IsNullOrWhiteSpace(name)) continue;

                    // Upsert into Companies (organisation-level contact)
                    await _db.ExecuteNonQueryAsync(@"
IF EXISTS (SELECT 1 FROM Companies WHERE CustomerCode = @XeroId)
    UPDATE Companies SET Company = @Name, Email = @Email WHERE CustomerCode = @XeroId;
ELSE
    INSERT INTO Companies (Company, Email, CustomerCode)
    VALUES (@Name, @Email, @XeroId);",
                        new()
                        {
                            ["XeroId"] = xeroId,
                            ["Name"]   = name,
                            ["Email"]  = (object?)email ?? DBNull.Value,
                        });

                    await RecordSyncAsync("Xero", "Contact", xeroId ?? "", name, "Pull", "OK");
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Xero: failed to upsert contact");
                }
            }

            return FinishLog(log, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncContactsFromXeroAsync failed");
            return FailLog(log, ex.Message);
        }
    }

    public async Task<SyncLogEntry> SyncContactsToXeroAsync()
    {
        var log = StartLog("Xero", "Contact", "Push");
        if (!await EnsureReadyAsync()) return FailLog(log, "Not connected or not enabled");

        try
        {
            // Push companies that don't yet have a Xero CustomerCode
            var dt = await _db.QueryAsync(@"
                SELECT TOP 200 CompanyId, Company, Email
                FROM Companies
                WHERE (CustomerCode IS NULL OR CustomerCode = '')
                  AND Company IS NOT NULL AND Company <> ''");

            int count = 0;
            foreach (System.Data.DataRow row in dt.Rows)
            {
                try
                {
                    var name  = row["Company"]?.ToString() ?? "";
                    var email = row["Email"]?.ToString();
                    var id    = row["CompanyId"]?.ToString();

                    var payload = JsonSerializer.Serialize(new
                    {
                        Contacts = new[]
                        {
                            new { Name = name, EmailAddress = email ?? "" }
                        }
                    });

                    var result = await PutXeroAsync("Contacts", payload);
                    if (result is not null)
                    {
                        using var doc  = JsonDocument.Parse(result);
                        if (doc.RootElement.TryGetProperty("Contacts", out var arr) &&
                            arr.GetArrayLength() > 0)
                        {
                            var xeroId = arr[0].TryGetProperty("ContactID", out var cid)
                                ? cid.GetString() : null;
                            if (!string.IsNullOrWhiteSpace(xeroId))
                            {
                                await _db.ExecuteNonQueryAsync(
                                    "UPDATE Companies SET CustomerCode = @XeroId WHERE CompanyId = @Id",
                                    new() { ["XeroId"] = xeroId, ["Id"] = id });
                                await RecordSyncAsync("Xero", "Contact", xeroId, name, "Push", "OK");
                                count++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Xero: failed to push contact");
                }
            }

            return FinishLog(log, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncContactsToXeroAsync failed");
            return FailLog(log, ex.Message);
        }
    }

    // ── Invoices ───────────────────────────────────────────────────────────────

    public async Task<SyncLogEntry> SyncInvoicesFromXeroAsync()
    {
        var log = StartLog("Xero", "Invoice", "Pull");
        if (!await EnsureReadyAsync()) return FailLog(log, "Not connected or not enabled");

        try
        {
            var resp = await CallXeroAsync("Invoices?where=Type%3D%22ACCREC%22%20AND%20(Status%3D%22AUTHORISED%22%20OR%20Status%3D%22PAID%22)&order=UpdatedDateUTC+DESC");
            if (resp is null) return FailLog(log, "No response from Xero Invoices API");

            using var doc = JsonDocument.Parse(resp);
            var invoices = doc.RootElement.TryGetProperty("Invoices", out var arr) ? arr : default;
            if (invoices.ValueKind != JsonValueKind.Array) return FailLog(log, "Unexpected Invoices response");

            int count = 0;
            foreach (var inv in invoices.EnumerateArray())
            {
                try
                {
                    var xeroId   = inv.TryGetProperty("InvoiceID",     out var xid) ? xid.GetString()     : null;
                    var invNum   = inv.TryGetProperty("InvoiceNumber",  out var num) ? num.GetString()     : null;
                    var total    = inv.TryGetProperty("Total",          out var tot) ? tot.GetDecimal()    : 0m;
                    var taxAmt   = inv.TryGetProperty("TotalTax",       out var tax) ? tax.GetDecimal()    : 0m;
                    var dateStr  = inv.TryGetProperty("DateString",     out var ds)  ? ds.GetString()      : null;
                    var status   = inv.TryGetProperty("Status",         out var st)  ? st.GetString()      : null;
                    var contact  = inv.TryGetProperty("Contact",        out var con) ? con : default;
                    var company  = contact.ValueKind != JsonValueKind.Undefined
                        && contact.TryGetProperty("Name", out var cn) ? cn.GetString() : null;

                    if (string.IsNullOrWhiteSpace(xeroId)) continue;

                    // Check if already synced
                    var existing = await _db.ScalarAsync<int?>(
                        "SELECT InvoiceId FROM Invoices WHERE ExternalRef = @Ref",
                        new() { ["Ref"] = $"XERO:{xeroId}" });

                    if (existing.HasValue && existing.Value > 0)
                    {
                        await RecordSyncAsync("Xero", "Invoice", xeroId, invNum ?? xeroId, "Pull", "Exists");
                        continue;
                    }

                    DateTime.TryParse(dateStr, out var invoiceDate);
                    var nett = total - taxAmt;

                    await _db.ExecuteNonQueryAsync(@"
INSERT INTO Invoices (InvoiceDate, InvCompany, NettPriceTotal, GSTTotal, ExternalRef, InvoiceStatusId, Code)
VALUES (@Date, @Company, @Nett, @Gst, @Ref, 1, 'XERO')",
                        new()
                        {
                            ["Date"]    = invoiceDate == default ? (object)DateTime.Today : invoiceDate,
                            ["Company"] = (object?)(company ?? "") ?? DBNull.Value,
                            ["Nett"]    = nett,
                            ["Gst"]     = taxAmt,
                            ["Ref"]     = $"XERO:{xeroId}",
                        });

                    await RecordSyncAsync("Xero", "Invoice", xeroId, invNum ?? xeroId, "Pull", "OK");
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Xero: failed to upsert invoice");
                }
            }

            return FinishLog(log, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncInvoicesFromXeroAsync failed");
            return FailLog(log, ex.Message);
        }
    }

    public async Task<SyncLogEntry> SyncInvoicesToXeroAsync()
    {
        var log = StartLog("Xero", "Invoice", "Push");
        if (!await EnsureReadyAsync()) return FailLog(log, "Not connected or not enabled");

        try
        {
            // Push approved invoices not yet exported to Xero
            var dt = await _db.QueryAsync(@"
                SELECT TOP 50 i.InvoiceId,
                       ISNULL(co.Company, ISNULL(i.InvCompany,'')) AS Company,
                       ISNULL(co.CustomerCode,'') AS XeroContactId,
                       ISNULL(i.NettPriceTotal,0) AS NettPriceTotal,
                       ISNULL(i.GSTTotal,0) AS GSTTotal,
                       i.InvoiceDate
                FROM Invoices i
                LEFT JOIN Companies co ON co.CompanyId = i.CompanyId
                WHERE i.InvoiceStatusId IN (2,3)
                  AND (i.ExportedToMYOB = 0 OR i.ExportedToMYOB IS NULL)
                  AND (i.ExternalRef IS NULL OR i.ExternalRef = '')
                ORDER BY i.InvoiceDate DESC");

            int count = 0;
            foreach (System.Data.DataRow row in dt.Rows)
            {
                try
                {
                    var invoiceId = row["InvoiceId"]?.ToString();
                    var company   = row["Company"]?.ToString() ?? "";
                    var xeroConId = row["XeroContactId"]?.ToString();
                    var nett      = Convert.ToDecimal(row["NettPriceTotal"]);
                    var gst       = Convert.ToDecimal(row["GSTTotal"]);
                    var date      = row["InvoiceDate"] is DateTime dt2 ? dt2 : DateTime.Today;

                    object contactObj = string.IsNullOrWhiteSpace(xeroConId)
                        ? (object)new { Name = company }
                        : new { ContactID = xeroConId };

                    var payload = JsonSerializer.Serialize(new
                    {
                        Invoices = new[]
                        {
                            new
                            {
                                Type       = "ACCREC",
                                Contact    = contactObj,
                                Date       = date.ToString("yyyy-MM-dd"),
                                DueDate    = date.AddDays(30).ToString("yyyy-MM-dd"),
                                Status     = "AUTHORISED",
                                LineItems  = new[]
                                {
                                    new
                                    {
                                        Description  = $"Invoice #{invoiceId} from MyDesk",
                                        Quantity     = 1,
                                        UnitAmount   = nett,
                                        TaxType      = gst > 0 ? "OUTPUT" : "NONE",
                                        AccountCode  = "200"
                                    }
                                }
                            }
                        }
                    });

                    var result = await PutXeroAsync("Invoices", payload);
                    if (result is not null)
                    {
                        using var doc = JsonDocument.Parse(result);
                        if (doc.RootElement.TryGetProperty("Invoices", out var arr) &&
                            arr.GetArrayLength() > 0)
                        {
                            var xeroId = arr[0].TryGetProperty("InvoiceID", out var iid)
                                ? iid.GetString() : null;
                            if (!string.IsNullOrWhiteSpace(xeroId))
                            {
                                await _db.ExecuteNonQueryAsync(
                                    "UPDATE Invoices SET ExternalRef = @Ref, ExportedToMYOB = 1, ExportedDate = GETDATE() WHERE InvoiceId = @Id",
                                    new() { ["Ref"] = $"XERO:{xeroId}", ["Id"] = invoiceId });
                                await RecordSyncAsync("Xero", "Invoice", xeroId, invoiceId ?? "", "Push", "OK");
                                count++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Xero: failed to push invoice {Id}", row["InvoiceId"]);
                }
            }

            return FinishLog(log, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncInvoicesToXeroAsync failed");
            return FailLog(log, ex.Message);
        }
    }

    // ── Items / Products ───────────────────────────────────────────────────────

    public async Task<SyncLogEntry> SyncItemsFromXeroAsync()
    {
        var log = StartLog("Xero", "Item", "Pull");
        if (!await EnsureReadyAsync()) return FailLog(log, "Not connected or not enabled");

        try
        {
            var resp = await CallXeroAsync("Items");
            if (resp is null) return FailLog(log, "No response from Xero Items API");

            using var doc   = JsonDocument.Parse(resp);
            var items = doc.RootElement.TryGetProperty("Items", out var arr) ? arr : default;
            if (items.ValueKind != JsonValueKind.Array) return FailLog(log, "Unexpected Items response");

            int count = 0;
            foreach (var item in items.EnumerateArray())
            {
                try
                {
                    var code  = item.TryGetProperty("Code",        out var c)  ? c.GetString()  : null;
                    var name  = item.TryGetProperty("Name",        out var n)  ? n.GetString()  : null;
                    var desc  = item.TryGetProperty("Description", out var d)  ? d.GetString()  : null;
                    var price = item.TryGetProperty("SalesDetails", out var sd)
                        && sd.TryGetProperty("UnitPrice", out var up)
                        ? (decimal?)up.GetDecimal() : null;

                    if (string.IsNullOrWhiteSpace(code)) continue;

                    await _db.ExecuteNonQueryAsync(@"
IF EXISTS (SELECT 1 FROM Products WHERE ProductCode = @Code)
    UPDATE Products SET ProductName = @Name, ProductDescription = @Desc, Price = ISNULL(@Price, Price)
    WHERE ProductCode = @Code;
ELSE
    INSERT INTO Products (ProductCode, ProductName, ProductDescription, Price)
    VALUES (@Code, @Name, @Desc, @Price);",
                        new()
                        {
                            ["Code"]  = code,
                            ["Name"]  = (object?)(name ?? code) ?? DBNull.Value,
                            ["Desc"]  = (object?)desc ?? DBNull.Value,
                            ["Price"] = (object?)price ?? DBNull.Value,
                        });

                    await RecordSyncAsync("Xero", "Item", code, name ?? code, "Pull", "OK");
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Xero: failed to upsert item");
                }
            }

            return FinishLog(log, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncItemsFromXeroAsync failed");
            return FailLog(log, ex.Message);
        }
    }

    // ── Full sync orchestration ────────────────────────────────────────────────

    public async Task RunFullSyncAsync()
    {
        var cfg = _settings.Current.Xero;
        if (!cfg.Enabled || !cfg.IsConnected) return;

        _logger.LogInformation("Xero: starting full sync");

        if (cfg.SyncContacts)
        {
            await SyncContactsFromXeroAsync();
            await SyncContactsToXeroAsync();
        }
        if (cfg.SyncInvoices)
        {
            await SyncInvoicesFromXeroAsync();
            await SyncInvoicesToXeroAsync();
        }

        // Always sync items
        await SyncItemsFromXeroAsync();

        cfg.LastSync = DateTime.UtcNow;
        await _settings.SaveAsync();
        _logger.LogInformation("Xero: full sync complete");
    }

    // ── Internal helpers ───────────────────────────────────────────────────────

    private async Task<bool> EnsureReadyAsync()
    {
        var cfg = _settings.Current.Xero;
        if (!cfg.Enabled || !cfg.IsConnected) return false;
        return await RefreshTokenIfNeededAsync();
    }

    private async Task<string?> CallXeroAsync(string path)
    {
        var cfg  = _settings.Current.Xero;
        var http = _httpFactory.CreateClient();
        var req  = new HttpRequestMessage(HttpMethod.Get, ApiBase + path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.AccessToken);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(cfg.TenantId))
            req.Headers.Add("xero-tenant-id", cfg.TenantId);

        using var resp = await http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("Xero GET {Path} returned {Status}", path, resp.StatusCode);
            return null;
        }
        return await resp.Content.ReadAsStringAsync();
    }

    private async Task<string?> PutXeroAsync(string path, string jsonPayload)
    {
        var cfg  = _settings.Current.Xero;
        var http = _httpFactory.CreateClient();
        var req  = new HttpRequestMessage(HttpMethod.Post, ApiBase + path)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.AccessToken);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        if (!string.IsNullOrWhiteSpace(cfg.TenantId))
            req.Headers.Add("xero-tenant-id", cfg.TenantId);

        using var resp = await http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync();
            _logger.LogWarning("Xero POST {Path} returned {Status}: {Body}", path, resp.StatusCode, err);
            return null;
        }
        return await resp.Content.ReadAsStringAsync();
    }

    private async Task<string?> FetchTenantIdAsync(string accessToken)
    {
        try
        {
            var http = _httpFactory.CreateClient();
            var req  = new HttpRequestMessage(HttpMethod.Get, ConnUrl);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            using var resp = await http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;
            var body = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                return doc.RootElement[0].TryGetProperty("tenantId", out var tid) ? tid.GetString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch Xero tenant id");
        }
        return null;
    }

    private static string? GetFirstPhone(JsonElement contact)
    {
        if (contact.TryGetProperty("Phones", out var phones) && phones.ValueKind == JsonValueKind.Array)
            foreach (var p in phones.EnumerateArray())
                if (p.TryGetProperty("PhoneNumber", out var n) && !string.IsNullOrWhiteSpace(n.GetString()))
                    return n.GetString();
        return null;
    }

    private async Task RecordSyncAsync(string provider, string entityType, string externalId, string internalId, string direction, string status)
    {
        try
        {
            await _db.ExecuteNonQueryAsync(@"
IF EXISTS (SELECT 1 FROM AccountingSyncRecords WHERE Provider=@P AND EntityType=@ET AND ExternalId=@EId)
    UPDATE AccountingSyncRecords SET SyncedAt=GETDATE(), Direction=@Dir, LastStatus=@St, InternalId=@IId WHERE Provider=@P AND EntityType=@ET AND ExternalId=@EId;
ELSE
    INSERT INTO AccountingSyncRecords (Provider, EntityType, ExternalId, InternalId, SyncedAt, Direction, LastStatus)
    VALUES (@P, @ET, @EId, @IId, GETDATE(), @Dir, @St);",
                new()
                {
                    ["P"]   = provider,
                    ["ET"]  = entityType,
                    ["EId"] = externalId,
                    ["IId"] = internalId,
                    ["Dir"] = direction,
                    ["St"]  = status,
                });
        }
        catch { /* best-effort */ }
    }

    private static SyncLogEntry StartLog(string provider, string entityType, string direction) => new()
    {
        Provider   = provider,
        EntityType = entityType,
        Direction  = direction,
        StartedAt  = DateTime.UtcNow,
        Status     = "Running",
    };

    private SyncLogEntry FinishLog(SyncLogEntry log, int count)
    {
        log.Count       = count;
        log.Status      = "Success";
        log.CompletedAt = DateTime.UtcNow;
        _ = WriteLogAsync(log);
        return log;
    }

    private SyncLogEntry FailLog(SyncLogEntry log, string error)
    {
        log.Status       = "Failed";
        log.ErrorMessage = error;
        log.CompletedAt  = DateTime.UtcNow;
        _ = WriteLogAsync(log);
        return log;
    }

    private async Task WriteLogAsync(SyncLogEntry log)
    {
        try
        {
            await _db.ExecuteNonQueryAsync(@"
INSERT INTO AccountingSyncLog (Provider, EntityType, Direction, Count, Status, StartedAt, CompletedAt, ErrorMessage)
VALUES (@P, @ET, @Dir, @Cnt, @St, @SA, @CA, @Err)",
                new()
                {
                    ["P"]   = log.Provider,
                    ["ET"]  = log.EntityType,
                    ["Dir"] = log.Direction,
                    ["Cnt"] = log.Count,
                    ["St"]  = log.Status,
                    ["SA"]  = log.StartedAt,
                    ["CA"]  = (object?)log.CompletedAt ?? DBNull.Value,
                    ["Err"] = (object?)log.ErrorMessage ?? DBNull.Value,
                });
        }
        catch { /* best-effort */ }
    }
}
