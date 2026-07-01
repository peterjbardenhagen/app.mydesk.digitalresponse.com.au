using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;


namespace MyDesk.Shared.Services.Integrations;

/// <summary>
/// QuickBooks Online OAuth 2.0 + two-way accounting sync via direct HttpClient (no QBO SDK).
/// API base: https://quickbooks.api.intuit.com/v3/company/{realmId}/
/// Auth: https://appcenter.intuit.com/connect/oauth2
/// </summary>
public class QuickBooksSyncService
{
    private const string AuthUrl  = "https://appcenter.intuit.com/connect/oauth2";
    private const string TokenUrl = "https://oauth.platform.intuit.com/oauth2/v1/tokens/bearer";
    private const string ApiBase  = "https://quickbooks.api.intuit.com/v3/company/";

    private readonly DatabaseService _db;
    private readonly IAccountingSettingsService _settings;
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger<QuickBooksSyncService> _logger;

    public QuickBooksSyncService(
        DatabaseService db,
        IAccountingSettingsService settings,
        IHttpClientFactory httpFactory,
        ILogger<QuickBooksSyncService> logger)
    {
        _db       = db;
        _settings = settings;
        _httpFactory = httpFactory;
        _logger   = logger;
    }

    // ── OAuth ──────────────────────────────────────────────────────────────────

    public Task<string> GetAuthUrlAsync(string redirectUri)
    {
        var cfg = _settings.Current.QuickBooks;
        var url = $"{AuthUrl}?client_id={Uri.EscapeDataString(cfg.ClientId ?? "")}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&response_type=code" +
                  $"&scope={Uri.EscapeDataString("com.intuit.quickbooks.accounting")}" +
                  $"&state=quickbooks";
        return Task.FromResult(url);
    }

    public async Task<bool> ExchangeCodeAsync(string code, string realmId, string redirectUri)
    {
        var cfg = _settings.Current.QuickBooks;
        if (string.IsNullOrWhiteSpace(cfg.ClientId) || string.IsNullOrWhiteSpace(cfg.ClientSecret))
            return false;

        var form = new Dictionary<string, string>
        {
            ["grant_type"]    = "authorization_code",
            ["code"]          = code,
            ["redirect_uri"]  = redirectUri,
        };

        var http = _httpFactory.CreateClient();
        var req  = new HttpRequestMessage(HttpMethod.Post, TokenUrl)
        {
            Content = new FormUrlEncodedContent(form)
        };
        req.Headers.Authorization = new AuthenticationHeaderValue(
            "Basic",
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{cfg.ClientId}:{cfg.ClientSecret}")));
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var resp = await http.SendAsync(req);
        var body = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("QBO token exchange failed: {Status} {Body}", resp.StatusCode, body);
            return false;
        }

        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        cfg.AccessToken  = root.TryGetProperty("access_token",  out var at) ? at.GetString() : null;
        cfg.RefreshToken = root.TryGetProperty("refresh_token",  out var rt) ? rt.GetString() : null;
        cfg.TokenExpiry  = root.TryGetProperty("expires_in",    out var ei)
            ? DateTime.UtcNow.AddSeconds(ei.GetInt32() - 60) : DateTime.UtcNow.AddMinutes(59);
        cfg.TenantId     = realmId;   // QBO uses "realmId" as tenant identifier
        cfg.IsConnected  = true;
        cfg.Status       = "Connected";

        await _settings.SaveAsync();
        return true;
    }

    public async Task<bool> RefreshTokenIfNeededAsync()
    {
        var cfg = _settings.Current.QuickBooks;
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
            ? DateTime.UtcNow.AddSeconds(ei.GetInt32() - 60) : DateTime.UtcNow.AddMinutes(59);

        await _settings.SaveAsync();
        return true;
    }

    // ── Customers ──────────────────────────────────────────────────────────────

    public async Task<SyncLogEntry> SyncCustomersFromQbAsync()
    {
        var log = StartLog("QuickBooks", "Contact", "Pull");
        if (!await EnsureReadyAsync()) return FailLog(log, "Not connected or not enabled");

        try
        {
            var resp = await QueryQboAsync("SELECT * FROM Customer WHERE Active = true MAXRESULTS 500");
            if (resp is null) return FailLog(log, "No response from QBO Customers query");

            using var doc = JsonDocument.Parse(resp);
            if (!doc.RootElement.TryGetProperty("QueryResponse", out var qr) ||
                !qr.TryGetProperty("Customer", out var customers) ||
                customers.ValueKind != JsonValueKind.Array)
                return FinishLog(log, 0);

            int count = 0;
            foreach (var c in customers.EnumerateArray())
            {
                try
                {
                    var qbId      = c.TryGetProperty("Id",          out var id)   ? id.GetString()   : null;
                    var name      = c.TryGetProperty("CompanyName", out var cn)   ? cn.GetString()   : null;
                    if (string.IsNullOrWhiteSpace(name))
                        name      = c.TryGetProperty("DisplayName", out var dn)   ? dn.GetString()   : null;
                    var email     = c.TryGetProperty("PrimaryEmailAddr", out var ea)
                        && ea.TryGetProperty("Address", out var addr) ? addr.GetString() : null;

                    if (string.IsNullOrWhiteSpace(name)) continue;

                    await _db.ExecuteNonQueryAsync(@"
IF EXISTS (SELECT 1 FROM Companies WHERE CustomerCode = @QbId)
    UPDATE Companies SET Company = @Name, Email = @Email WHERE CustomerCode = @QbId;
ELSE
    INSERT INTO Companies (Company, Email, CustomerCode) VALUES (@Name, @Email, @QbId);",
                        new()
                        {
                            ["QbId"]  = $"QB:{qbId}",
                            ["Name"]  = name,
                            ["Email"] = (object?)email ?? DBNull.Value,
                        });

                    await RecordSyncAsync("QuickBooks", "Contact", qbId ?? "", name, "Pull", "OK");
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "QBO: failed to upsert customer");
                }
            }

            return FinishLog(log, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncCustomersFromQbAsync failed");
            return FailLog(log, ex.Message);
        }
    }

    public async Task<SyncLogEntry> SyncCustomersToQbAsync()
    {
        var log = StartLog("QuickBooks", "Contact", "Push");
        if (!await EnsureReadyAsync()) return FailLog(log, "Not connected or not enabled");

        try
        {
            var dt = await _db.QueryAsync(@"
                SELECT TOP 100 CompanyId, Company, Email
                FROM Companies
                WHERE (CustomerCode IS NULL OR CustomerCode = '' OR LEFT(CustomerCode,3) <> 'QB:')
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
                        DisplayName = name,
                        PrimaryEmailAddr = string.IsNullOrWhiteSpace(email)
                            ? null
                            : new { Address = email }
                    });

                    var result = await PostQboAsync("customer", payload);
                    if (result is not null)
                    {
                        using var doc = JsonDocument.Parse(result);
                        if (doc.RootElement.TryGetProperty("Customer", out var cust) &&
                            cust.TryGetProperty("Id", out var qbId))
                        {
                            await _db.ExecuteNonQueryAsync(
                                "UPDATE Companies SET CustomerCode = @QbId WHERE CompanyId = @Id",
                                new() { ["QbId"] = $"QB:{qbId.GetString()}", ["Id"] = companyId });
                            await RecordSyncAsync("QuickBooks", "Contact", qbId.GetString() ?? "", name, "Push", "OK");
                            count++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "QBO: failed to push customer");
                }
            }

            return FinishLog(log, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncCustomersToQbAsync failed");
            return FailLog(log, ex.Message);
        }
    }

    // ── Invoices ───────────────────────────────────────────────────────────────

    public async Task<SyncLogEntry> SyncInvoicesFromQbAsync()
    {
        var log = StartLog("QuickBooks", "Invoice", "Pull");
        if (!await EnsureReadyAsync()) return FailLog(log, "Not connected or not enabled");

        try
        {
            var resp = await QueryQboAsync("SELECT * FROM Invoice ORDERBY TxnDate DESC MAXRESULTS 200");
            if (resp is null) return FailLog(log, "No response from QBO Invoice query");

            using var doc = JsonDocument.Parse(resp);
            if (!doc.RootElement.TryGetProperty("QueryResponse", out var qr) ||
                !qr.TryGetProperty("Invoice", out var invoices) ||
                invoices.ValueKind != JsonValueKind.Array)
                return FinishLog(log, 0);

            int count = 0;
            foreach (var inv in invoices.EnumerateArray())
            {
                try
                {
                    var qbId   = inv.TryGetProperty("Id",       out var id)  ? id.GetString()      : null;
                    var total  = inv.TryGetProperty("TotalAmt", out var ta)  ? ta.GetDecimal()     : 0m;
                    var tax    = inv.TryGetProperty("TxnTaxDetail", out var ttd)
                        && ttd.TryGetProperty("TotalTax", out var tt) ? tt.GetDecimal() : 0m;
                    var dateStr = inv.TryGetProperty("TxnDate",out var td)   ? td.GetString()      : null;
                    var custRef = inv.TryGetProperty("CustomerRef", out var cr)
                        && cr.TryGetProperty("name", out var cn) ? cn.GetString() : null;

                    if (string.IsNullOrWhiteSpace(qbId)) continue;

                    var existing = await _db.ScalarAsync<int?>(
                        "SELECT InvoiceId FROM Invoices WHERE ExternalRef = @Ref",
                        new() { ["Ref"] = $"QB:{qbId}" });
                    if (existing.HasValue && existing.Value > 0) continue;

                    DateTime.TryParse(dateStr, out var invoiceDate);
                    var nett = total - tax;

                    await _db.ExecuteNonQueryAsync(@"
INSERT INTO Invoices (InvoiceDate, InvCompany, NettPriceTotal, GSTTotal, ExternalRef, InvoiceStatusId, Code)
VALUES (@Date, @Company, @Nett, @Gst, @Ref, 1, 'QBO')",
                        new()
                        {
                            ["Date"]    = invoiceDate == default ? (object)DateTime.Today : invoiceDate,
                            ["Company"] = (object?)(custRef ?? "") ?? DBNull.Value,
                            ["Nett"]    = nett,
                            ["Gst"]     = tax,
                            ["Ref"]     = $"QB:{qbId}",
                        });

                    await RecordSyncAsync("QuickBooks", "Invoice", qbId, custRef ?? qbId, "Pull", "OK");
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "QBO: failed to upsert invoice");
                }
            }

            return FinishLog(log, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncInvoicesFromQbAsync failed");
            return FailLog(log, ex.Message);
        }
    }

    public async Task<SyncLogEntry> SyncInvoicesToQbAsync()
    {
        var log = StartLog("QuickBooks", "Invoice", "Push");
        if (!await EnsureReadyAsync()) return FailLog(log, "Not connected or not enabled");

        try
        {
            var dt = await _db.QueryAsync(@"
                SELECT TOP 50 i.InvoiceId,
                       ISNULL(co.Company, ISNULL(i.InvCompany,'')) AS Company,
                       ISNULL(co.CustomerCode,'') AS QbCustomerId,
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
                    var invoiceId = row["InvoiceId"]?.ToString();
                    var company   = row["Company"]?.ToString() ?? "";
                    var qbCustId  = row["QbCustomerId"]?.ToString();
                    var nett      = Convert.ToDecimal(row["NettPriceTotal"]);
                    var gst       = Convert.ToDecimal(row["GSTTotal"]);
                    var date      = row["InvoiceDate"] is DateTime dt2 ? dt2 : DateTime.Today;

                    // QBO requires a valid CustomerRef; use a generic one if not mapped
                    if (string.IsNullOrWhiteSpace(qbCustId) || !qbCustId.StartsWith("QB:"))
                    {
                        _logger.LogDebug("QBO: skipping invoice {Id} — no mapped QBO customer", invoiceId);
                        continue;
                    }

                    var qbCustomerIdNum = qbCustId.Substring(3); // strip "QB:" prefix

                    var payload = JsonSerializer.Serialize(new
                    {
                        Line = new[]
                        {
                            new
                            {
                                Amount     = nett + gst,
                                DetailType = "SalesItemLineDetail",
                                SalesItemLineDetail = new
                                {
                                    ItemRef = new { value = "1", name = "Services" }
                                }
                            }
                        },
                        CustomerRef   = new { value = qbCustomerIdNum },
                        TxnDate       = date.ToString("yyyy-MM-dd"),
                        PrivateNote   = $"MyDesk Invoice #{invoiceId}"
                    });

                    var result = await PostQboAsync("invoice", payload);
                    if (result is not null)
                    {
                        using var doc = JsonDocument.Parse(result);
                        if (doc.RootElement.TryGetProperty("Invoice", out var inv) &&
                            inv.TryGetProperty("Id", out var qbInvId))
                        {
                            await _db.ExecuteNonQueryAsync(
                                "UPDATE Invoices SET ExternalRef = @Ref, ExportedToMYOB = 1, ExportedDate = GETDATE() WHERE InvoiceId = @Id",
                                new() { ["Ref"] = $"QB:{qbInvId.GetString()}", ["Id"] = invoiceId });
                            await RecordSyncAsync("QuickBooks", "Invoice", qbInvId.GetString() ?? "", invoiceId ?? "", "Push", "OK");
                            count++;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "QBO: failed to push invoice {Id}", row["InvoiceId"]);
                }
            }

            return FinishLog(log, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncInvoicesToQbAsync failed");
            return FailLog(log, ex.Message);
        }
    }

    // ── Items / Products ───────────────────────────────────────────────────────

    public async Task<SyncLogEntry> SyncItemsFromQbAsync()
    {
        var log = StartLog("QuickBooks", "Item", "Pull");
        if (!await EnsureReadyAsync()) return FailLog(log, "Not connected or not enabled");

        try
        {
            var resp = await QueryQboAsync("SELECT * FROM Item WHERE Active = true MAXRESULTS 500");
            if (resp is null) return FailLog(log, "No response from QBO Item query");

            using var doc = JsonDocument.Parse(resp);
            if (!doc.RootElement.TryGetProperty("QueryResponse", out var qr) ||
                !qr.TryGetProperty("Item", out var items) ||
                items.ValueKind != JsonValueKind.Array)
                return FinishLog(log, 0);

            int count = 0;
            foreach (var item in items.EnumerateArray())
            {
                try
                {
                    var qbId  = item.TryGetProperty("Id",          out var id)  ? id.GetString()  : null;
                    var name  = item.TryGetProperty("Name",        out var n)   ? n.GetString()   : null;
                    var desc  = item.TryGetProperty("Description", out var d)   ? d.GetString()   : null;
                    var price = item.TryGetProperty("UnitPrice",   out var up)  ? (decimal?)up.GetDecimal() : null;
                    var code  = $"QB:{qbId}";

                    if (string.IsNullOrWhiteSpace(qbId)) continue;

                    await _db.ExecuteNonQueryAsync(@"
IF EXISTS (SELECT 1 FROM Products WHERE ProductCode = @Code)
    UPDATE Products SET ProductName = @Name, ProductDescription = @Desc, Price = ISNULL(@Price, Price) WHERE ProductCode = @Code;
ELSE
    INSERT INTO Products (ProductCode, ProductName, ProductDescription, Price) VALUES (@Code, @Name, @Desc, @Price);",
                        new()
                        {
                            ["Code"]  = code,
                            ["Name"]  = (object?)(name ?? code) ?? DBNull.Value,
                            ["Desc"]  = (object?)desc ?? DBNull.Value,
                            ["Price"] = (object?)price ?? DBNull.Value,
                        });

                    await RecordSyncAsync("QuickBooks", "Item", qbId, name ?? qbId, "Pull", "OK");
                    count++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "QBO: failed to upsert item");
                }
            }

            return FinishLog(log, count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SyncItemsFromQbAsync failed");
            return FailLog(log, ex.Message);
        }
    }

    // ── Full sync ──────────────────────────────────────────────────────────────

    public async Task RunFullSyncAsync()
    {
        var cfg = _settings.Current.QuickBooks;
        if (!cfg.Enabled || !cfg.IsConnected) return;

        _logger.LogInformation("QuickBooks: starting full sync");

        if (cfg.SyncContacts)
        {
            await SyncCustomersFromQbAsync();
            await SyncCustomersToQbAsync();
        }
        if (cfg.SyncInvoices)
        {
            await SyncInvoicesFromQbAsync();
            await SyncInvoicesToQbAsync();
        }

        await SyncItemsFromQbAsync();

        cfg.LastSync = DateTime.UtcNow;
        await _settings.SaveAsync();
        _logger.LogInformation("QuickBooks: full sync complete");
    }

    // ── Internal helpers ───────────────────────────────────────────────────────

    private async Task<bool> EnsureReadyAsync()
    {
        var cfg = _settings.Current.QuickBooks;
        if (!cfg.Enabled || !cfg.IsConnected) return false;
        return await RefreshTokenIfNeededAsync();
    }

    private string RealmUrl => $"{ApiBase}{_settings.Current.QuickBooks.TenantId}/";

    private async Task<string?> QueryQboAsync(string query)
    {
        var cfg  = _settings.Current.QuickBooks;
        var http = _httpFactory.CreateClient();
        var url  = $"{RealmUrl}query?query={Uri.EscapeDataString(query)}&minorversion=65";
        var req  = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.AccessToken);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var resp = await http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            _logger.LogWarning("QBO query failed: {Status}", resp.StatusCode);
            return null;
        }
        return await resp.Content.ReadAsStringAsync();
    }

    private async Task<string?> PostQboAsync(string entity, string jsonPayload)
    {
        var cfg  = _settings.Current.QuickBooks;
        var http = _httpFactory.CreateClient();
        var url  = $"{RealmUrl}{entity}?minorversion=65";
        var req  = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json")
        };
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", cfg.AccessToken);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var resp = await http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync();
            _logger.LogWarning("QBO POST {Entity} failed: {Status}: {Body}", entity, resp.StatusCode, err);
            return null;
        }
        return await resp.Content.ReadAsStringAsync();
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
