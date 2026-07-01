using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;


namespace MyDesk.Shared.Services.Integrations;

/// <summary>
/// MYOB AccountRight / MYOB Business OAuth 2 + two-way accounting sync via direct HttpClient.
/// API base: https://api.myob.com/accountright/
/// Auth: https://secure.myob.com/oauth2/account/authorize
/// </summary>
public class MyobSyncService
{
    private const string AuthUrl  = "https://secure.myob.com/oauth2/account/authorize";
    private const string TokenUrl = "https://secure.myob.com/oauth2/v1/authorize";
    private const string ApiBase  = "https://api.myob.com/accountright/";

    private readonly DatabaseService _db;
    private readonly IAccountingSettingsService _settings;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<MyobSyncService> _logger;

    public MyobSyncService(
        DatabaseService db,
        IAccountingSettingsService settings,
        IHttpClientFactory httpFactory,
        ILogger<MyobSyncService> logger)
    {
        _db          = db;
        _settings    = settings;
        _httpFactory = httpFactory;
        _logger      = logger;
    }

    // ── OAuth ──────────────────────────────────────────────────────────────────

    public Task<string> GetAuthUrlAsync(string redirectUri)
    {
        var cfg = _settings.Current.MYOB;
        var url = $"{AuthUrl}?client_id={Uri.EscapeDataString(cfg.ClientId ?? "")}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&response_type=code" +
                  $"&scope={Uri.EscapeDataString("CompanyFile")}" +
                  $"&state=myob";
        return Task.FromResult(url);
    }

    public async Task<bool> ExchangeCodeAsync(string code, string redirectUri)
    {
        var cfg = _settings.Current.MYOB;
        if (string.IsNullOrWhiteSpace(cfg.ClientId) || string.IsNullOrWhiteSpace(cfg.ClientSecret))
            return false;

        var form = new Dictionary<string, string>
        {
            ["client_id"]     = cfg.ClientId!,
            ["client_secret"] = cfg.ClientSecret!,
            ["grant_type"]    = "authorization_code",
            ["code"]          = code,
            ["redirect_uri"]  = redirectUri,
        };

        var http = _httpFactory.CreateClient();
        var req  = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
        {
            Content = new FormUrlEncodedContent(form)
        };
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var resp = await http.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("MYOB token exchange failed: {Status} {Body}", resp.StatusCode, body);
            return false;
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        cfg.AccessToken  = root.TryGetProperty("access_token",  out var at) ? at.GetString() : null;
        cfg.RefreshToken = root.TryGetProperty("refresh_token",  out var rt) ? rt.GetString() : null;
        cfg.TokenExpiry  = root.TryGetProperty("expires_in",    out var ei)
            ? DateTime.UtcNow.AddSeconds(ei.GetInt32() - 60) : DateTime.UtcNow.AddMinutes(19);
        cfg.IsConnected  = true;
        cfg.Status       = "Connected";

        // Try to get the first company file UID
        cfg.TenantId = await FetchCompanyFileUidAsync(cfg.AccessToken!);

        await _settings.SaveAsync();
        return true;
    }

    public async Task<bool> RefreshTokenIfNeededAsync()
    {
        var cfg = _settings.Current.MYOB;
        if (!cfg.IsConnected || string.IsNullOrWhiteSpace(cfg.RefreshToken)) return false;
        if (cfg.TokenExpiry.HasValue && cfg.TokenExpiry.Value > DateTime.UtcNow.AddMinutes(2)) return true;

        var form = new Dictionary<string, string>
        {
            ["client_id"]     = cfg.ClientId ?? "",
            ["client_secret"] = cfg.ClientSecret ?? "",
            ["grant_type"]    = "refresh_token",
            ["refresh_token"] = cfg.RefreshToken!,
        };

        var http = _httpFactory.CreateClient();
        var req  = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
        {
            Content = new FormUrlEncodedContent(form)
        };
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

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

        cfg.AccessToken  = root.TryGetProperty("access_token",  out var at) ? at.GetString() : cfg.AccessToken;
        cfg.RefreshToken = root.TryGetProperty("refresh_token",  out var rt) ? rt.GetString() : cfg.RefreshToken;
        cfg.TokenExpiry  = root.TryGetProperty("expires_in",    out var ei)
            ? DateTime.UtcNow.AddSeconds(ei.GetInt32() - 60) : DateTime.UtcNow.AddMinutes(19);

        await _settings.SaveAsync();
        return true;
    }

    // ── Contacts ───────────────────────────────────────────────────────────────

    public async Task<SyncLogEntry> SyncContactsFromMyobAsync()
    {
        var log = StartLog("MYOB", "Contact", "Pull");
        if (!await EnsureReadyAsync()) return FailLog(log, "Not connected or not enabled");

        try
        {
            var resp = await CallMyobAsync("Contact/Customer");
            if (resp is null) return FailLog(log, "No response from MYOB Contacts API");

            using var doc = JsonDocument.Parse(resp);
            if (!doc.RootElement.TryGetProperty("Items", out var items) ||
                items.ValueKind != JsonValueKind.Array)
                return FinishLog(log, 0);

            int count = 0;
            foreach (var c in items.EnumerateArray())
            {
                try
                {
                    var myobId = c.TryGetProperty("UID",          out var uid)  ? uid.GetString()  : null;
                    var name   = c.TryGetProperty("CompanyName",  out var cn)   ? cn.GetString()   : null;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        var fname = c.TryGetProperty("FirstName", out var fn) ? fn.GetString() : "";
                        var lname = c.TryGetProperty("LastName",  out var ln) ? ln.GetString() : "";
                        name = $"{fname} {lname}".Trim();
                    }
                    var email = c.TryGetProperty("Addresses", out var addrs) && addrs.ValueKind == JsonValueKind.Array
                        ? GetFirstEmail(addrs) : null;

                    if (string.IsNullOrWhiteSpace(name)) continue;

                    await _db.ExecuteNonQueryAsync(@"
IF EXISTS (SELECT 1 FROM Companies WHERE CustomerCode = @MyobId)
    UPDATE Companies SET Company = @Name, Email = @Email WHERE CustomerCode = @MyobId;
ELSE
    INSERT INTO Companies (Company, Email, CustomerCode) VALUES (@Name, @Email, @MyobId);",
                        new()
                        {
                            ["MyobId"] = $"MYOB:{myobId}",
                            ["Name"]   = name,
                            ["Email"]  = (object?)email ?? DBNull.Value,
                        });

                    await RecordSyncAsync("MYOB", "Contact", myobId ?? "", name, "Pull", "OK");
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "MYOB: failed to upsert contact");
                }
            }

            return FinishLog(log, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncContactsFromMyobAsync failed");
            return FailLog(log, ex.Message);
        }
    }

    public async Task<SyncLogEntry> SyncContactsToMyobAsync()
    {
        var log = StartLog("MYOB", "Contact", "Push");
        if (!await EnsureReadyAsync()) return FailLog(log, "Not connected or not enabled");

        try
        {
            var dt = await _db.QueryAsync(@"
                SELECT TOP 100 CompanyId, Company, Email
                FROM Companies
                WHERE (CustomerCode IS NULL OR CustomerCode = '' OR LEFT(CustomerCode,5) <> 'MYOB:')
                  AND Company IS NOT NULL AND Company <> ''");

            int count = 0;
            foreach (System.Data.DataRow row in dt.Rows)
            {
                try
                {
                    var name      = row["Company"]?.ToString() ?? "";
                    var email     = row["Email"]?.ToString();
                    var companyId = row["CompanyId"]?.ToString();

                    var payload = JsonSerializer.Serialize(new
                    {
                        CompanyName = name,
                        IsActive    = true,
                        Addresses   = string.IsNullOrWhiteSpace(email)
                            ? null
                            : new[] { new { Type = "Email", Email = email } }
                    });

                    var result = await PostMyobAsync("Contact/Customer", payload);
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        using var doc = JsonDocument.Parse(result);
                        if (doc.RootElement.TryGetProperty("UID", out var uid))
                        {
                            var myobId = uid.GetString();
                            await _db.ExecuteNonQueryAsync(
                                "UPDATE Companies SET CustomerCode = @MyobId WHERE CompanyId = @Id",
                                new() { ["MyobId"] = $"MYOB:{myobId}", ["Id"] = companyId });
                            await RecordSyncAsync("MYOB", "Contact", myobId ?? "", name, "Push", "OK");
                            count++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "MYOB: failed to push contact");
                }
            }

            return FinishLog(log, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncContactsToMyobAsync failed");
            return FailLog(log, ex.Message);
        }
    }

    // ── Invoices ───────────────────────────────────────────────────────────────

    public async Task<SyncLogEntry> SyncInvoicesFromMyobAsync()
    {
        var log = StartLog("MYOB", "Invoice", "Pull");
        if (!await EnsureReadyAsync()) return FailLog(log, "Not connected or not enabled");

        try
        {
            var resp = await CallMyobAsync("Sale/Invoice/Item?$top=200&$orderby=Date+desc");
            if (resp is null) return FailLog(log, "No response from MYOB Invoices API");

            using var doc = JsonDocument.Parse(resp);
            if (!doc.RootElement.TryGetProperty("Items", out var items) ||
                items.ValueKind != JsonValueKind.Array)
                return FinishLog(log, 0);

            int count = 0;
            foreach (var inv in items.EnumerateArray())
            {
                try
                {
                    var myobId  = inv.TryGetProperty("UID",         out var uid) ? uid.GetString()    : null;
                    var total   = inv.TryGetProperty("TotalAmount",  out var ta)  ? ta.GetDecimal()   : 0m;
                    var tax     = inv.TryGetProperty("TotalTax",     out var tt)  ? tt.GetDecimal()   : 0m;
                    var dateStr = inv.TryGetProperty("Date",         out var d)   ? d.GetString()     : null;
                    var custRef = inv.TryGetProperty("Customer",     out var cust)
                        && cust.TryGetProperty("DisplayID", out var cid) ? cid.GetString() : null;

                    if (string.IsNullOrWhiteSpace(myobId)) continue;

                    var existing = await _db.ScalarAsync<int?>(
                        "SELECT InvoiceId FROM Invoices WHERE ExternalRef = @Ref",
                        new() { ["Ref"] = $"MYOB:{myobId}" });
                    if (existing.HasValue && existing.Value > 0) continue;

                    DateTime.TryParse(dateStr, out var invoiceDate);
                    var nett = total - tax;

                    await _db.ExecuteNonQueryAsync(@"
INSERT INTO Invoices (InvoiceDate, InvCompany, NettPriceTotal, GSTTotal, ExternalRef, InvoiceStatusId, Code)
VALUES (@Date, @Company, @Nett, @Gst, @Ref, 1, 'MYOB')",
                        new()
                        {
                            ["Date"]    = invoiceDate == default ? (object)DateTime.Today : invoiceDate,
                            ["Company"] = (object?)(custRef ?? "") ?? DBNull.Value,
                            ["Nett"]    = nett,
                            ["Gst"]     = tax,
                            ["Ref"]     = $"MYOB:{myobId}",
                        });

                    await RecordSyncAsync("MYOB", "Invoice", myobId, custRef ?? myobId, "Pull", "OK");
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "MYOB: failed to upsert invoice");
                }
            }

            return FinishLog(log, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncInvoicesFromMyobAsync failed");
            return FailLog(log, ex.Message);
        }
    }

    public async Task<SyncLogEntry> SyncInvoicesToMyobAsync()
    {
        var log = StartLog("MYOB", "Invoice", "Push");
        if (!await EnsureReadyAsync()) return FailLog(log, "Not connected or not enabled");

        try
        {
            var dt = await _db.QueryAsync(@"
                SELECT TOP 50 i.InvoiceId,
                       ISNULL(co.Company, ISNULL(i.InvCompany,'')) AS Company,
                       ISNULL(co.CustomerCode,'') AS MyobCustomerId,
                       ISNULL(i.NettPriceTotal,0) AS NettPriceTotal,
                       ISNULL(i.GSTTotal,0) AS GSTTotal,
                       i.InvoiceDate
                FROM Invoices i
                LEFT JOIN Companies co ON co.CompanyId = i.CompanyId
                WHERE i.InvoiceStatusId IN (2,3)
                  AND (i.ExportedToMYOB = 0 OR i.ExportedToMYOB IS NULL)
                  AND (i.ExternalRef IS NULL OR i.ExternalRef = '')");

            int count = 0;
            foreach (System.Data.DataRow row in dt.Rows)
            {
                try
                {
                    var invoiceId   = row["InvoiceId"]?.ToString();
                    var company     = row["Company"]?.ToString() ?? "";
                    var myobCustId  = row["MyobCustomerId"]?.ToString();
                    var nett        = Convert.ToDecimal(row["NettPriceTotal"]);
                    var gst         = Convert.ToDecimal(row["GSTTotal"]);
                    var date        = row["InvoiceDate"] is DateTime dt2 ? dt2 : DateTime.Today;

                    if (string.IsNullOrWhiteSpace(myobCustId) || !myobCustId.StartsWith("MYOB:"))
                    {
                        _logger.LogDebug("MYOB: skipping invoice {Id} — no mapped MYOB customer", invoiceId);
                        continue;
                    }

                    var myobUid = myobCustId.Substring(5); // strip "MYOB:"

                    var payload = JsonSerializer.Serialize(new
                    {
                        Customer    = new { UID = myobUid },
                        Date        = date.ToString("yyyy-MM-ddTHH:mm:ss"),
                        IsTaxInclusive = false,
                        Lines       = new[]
                        {
                            new
                            {
                                Type        = "Transaction",
                                Description = $"MyDesk Invoice #{invoiceId}",
                                UnitCount   = 1m,
                                UnitPrice   = nett,
                                DiscountPercent = 0m,
                                Total       = nett,
                            }
                        }
                    });

                    var result = await PostMyobAsync("Sale/Invoice/Service", payload);
                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        using var doc = JsonDocument.Parse(result);
                        if (doc.RootElement.TryGetProperty("UID", out var uid))
                        {
                            var myobInvId = uid.GetString();
                            await _db.ExecuteNonQueryAsync(
                                "UPDATE Invoices SET ExternalRef = @Ref, ExportedToMYOB = 1, ExportedDate = GETDATE() WHERE InvoiceId = @Id",
                                new() { ["Ref"] = $"MYOB:{myobInvId}", ["Id"] = invoiceId });
                            await RecordSyncAsync("MYOB", "Invoice", myobInvId ?? "", invoiceId ?? "", "Push", "OK");
                            count++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "MYOB: failed to push invoice {Id}", row["InvoiceId"]);
                }
            }

            return FinishLog(log, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncInvoicesToMyobAsync failed");
            return FailLog(log, ex.Message);
        }
    }

    // ── Items / Products ───────────────────────────────────────────────────────

    public async Task<SyncLogEntry> SyncItemsFromMyobAsync()
    {
        var log = StartLog("MYOB", "Item", "Pull");
        if (!await EnsureReadyAsync()) return FailLog(log, "Not connected or not enabled");

        try
        {
            var resp = await CallMyobAsync("Inventory/Item");
            if (resp is null) return FailLog(log, "No response from MYOB Items API");

            using var doc = JsonDocument.Parse(resp);
            if (!doc.RootElement.TryGetProperty("Items", out var items) ||
                items.ValueKind != JsonValueKind.Array)
                return FinishLog(log, 0);

            int count = 0;
            foreach (var item in items.EnumerateArray())
            {
                try
                {
                    var myobId = item.TryGetProperty("UID",         out var uid) ? uid.GetString()  : null;
                    var code   = item.TryGetProperty("Number",      out var n)   ? n.GetString()    : null;
                    var name   = item.TryGetProperty("Name",        out var nm)  ? nm.GetString()   : null;
                    var desc   = item.TryGetProperty("Description", out var d)   ? d.GetString()    : null;
                    var price  = item.TryGetProperty("BaseSellingPrice", out var sp) ? (decimal?)sp.GetDecimal() : null;

                    var productCode = code ?? $"MYOB:{myobId}";
                    if (string.IsNullOrWhiteSpace(productCode)) continue;

                    await _db.ExecuteNonQueryAsync(@"
IF EXISTS (SELECT 1 FROM Products WHERE ProductCode = @Code)
    UPDATE Products SET ProductName = @Name, ProductDescription = @Desc, Price = ISNULL(@Price, Price) WHERE ProductCode = @Code;
ELSE
    INSERT INTO Products (ProductCode, ProductName, ProductDescription, Price) VALUES (@Code, @Name, @Desc, @Price);",
                        new()
                        {
                            ["Code"]  = productCode,
                            ["Name"]  = (object?)(name ?? productCode) ?? DBNull.Value,
                            ["Desc"]  = (object?)desc ?? DBNull.Value,
                            ["Price"] = (object?)price ?? DBNull.Value,
                        });

                    await RecordSyncAsync("MYOB", "Item", myobId ?? code ?? "", name ?? productCode, "Pull", "OK");
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "MYOB: failed to upsert item");
                }
            }

            return FinishLog(log, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncItemsFromMyobAsync failed");
            return FailLog(log, ex.Message);
        }
    }

    // ── Full sync ──────────────────────────────────────────────────────────────

    public async Task RunFullSyncAsync()
    {
        var cfg = _settings.Current.MYOB;
        if (!cfg.Enabled || !cfg.IsConnected) return;

        _logger.LogInformation("MYOB: starting full sync");

        if (cfg.SyncContacts)
        {
            await SyncContactsFromMyobAsync();
            await SyncContactsToMyobAsync();
        }
        if (cfg.SyncInvoices)
        {
            await SyncInvoicesFromMyobAsync();
            await SyncInvoicesToMyobAsync();
        }

        await SyncItemsFromMyobAsync();

        cfg.LastSync = DateTime.UtcNow;
        await _settings.SaveAsync();
        _logger.LogInformation("MYOB: full sync complete");
    }

    // ── Internal helpers ───────────────────────────────────────────────────────

    private async Task<bool> EnsureReadyAsync()
    {
        var cfg = _settings.Current.MYOB;
        if (!cfg.Enabled || !cfg.IsConnected) return false;
        return await RefreshTokenIfNeededAsync();
    }

    private string CompanyFileUrl => string.IsNullOrWhiteSpace(_settings.Current.MYOB.TenantId)
        ? ApiBase : $"{ApiBase}{_settings.Current.MYOB.TenantId}/";

    private async Task<string?> CallMyobAsync(string path)
    {
        var cfg  = _settings.Current.MYOB;
        var http = _httpFactory.CreateClient();
        var req  = new HttpRequestMessage(HttpMethod.Get, CompanyFileUrl + path);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.AccessToken);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        req.Headers.Add("x-myobapi-key", cfg.ClientId ?? "");

        using var resp = await http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("MYOB GET {Path} returned {Status}", path, resp.StatusCode);
            return null;
        }
        return await resp.Content.ReadAsStringAsync();
    }

    private async Task<string?> PostMyobAsync(string path, string jsonPayload)
    {
        var cfg  = _settings.Current.MYOB;
        var http = _httpFactory.CreateClient();
        var req  = new HttpRequestMessage(HttpMethod.Post, CompanyFileUrl + path)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.AccessToken);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        req.Headers.Add("x-myobapi-key", cfg.ClientId ?? "");

        using var resp = await http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync();
            _logger.LogWarning("MYOB POST {Path} failed: {Status}: {Body}", path, resp.StatusCode, err);
            return null;
        }

        if (resp.StatusCode == System.Net.HttpStatusCode.Created)
        {
            // MYOB returns Location header on 201; fetch the resource
            var location = resp.Headers.Location?.ToString();
            if (!string.IsNullOrWhiteSpace(location))
            {
                var getReq = new HttpRequestMessage(HttpMethod.Get, location);
                getReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.AccessToken);
                getReq.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                getReq.Headers.Add("x-myobapi-key", cfg.ClientId ?? "");
                using var getResp = await http.SendAsync(getReq);
                if (getResp.IsSuccessStatusCode)
                    return await getResp.Content.ReadAsStringAsync();
            }
        }

        return await resp.Content.ReadAsStringAsync();
    }

    private async Task<string?> FetchCompanyFileUidAsync(string accessToken)
    {
        try
        {
            var cfg  = _settings.Current.MYOB;
            var http = _httpFactory.CreateClient();
            var req  = new HttpRequestMessage(HttpMethod.Get, ApiBase);
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            req.Headers.Add("x-myobapi-key", cfg.ClientId ?? "");
            using var resp = await http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return null;
            var body = await resp.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() > 0)
                return doc.RootElement[0].TryGetProperty("Id", out var id) ? id.GetString() : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not fetch MYOB company file UID");
        }
        return null;
    }

    private static string? GetFirstEmail(JsonElement addresses)
    {
        foreach (var addr in addresses.EnumerateArray())
            if (addr.TryGetProperty("Email", out var em) && !string.IsNullOrWhiteSpace(em.GetString()))
                return em.GetString();
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
