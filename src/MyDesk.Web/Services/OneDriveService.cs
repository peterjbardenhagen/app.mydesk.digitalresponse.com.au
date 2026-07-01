using System.Net.Http.Headers;
using System.Text.Json;
using MyDesk.Shared.Models;

namespace MyDesk.Web.Services;

/// <summary>
/// Lists OneDrive items via Microsoft Graph using the per-user access token
/// stored in <see cref="PlatformSettings.MyOutlookUserConnections"/>.
/// The same Microsoft Graph token that grants Outlook access also covers
/// OneDrive, so no additional OAuth flow is required when the user has
/// already connected "My Outlook" in Integrations.
/// </summary>
public class OneDriveService
{
    private readonly IHttpClientFactory _http;
    private readonly IHttpContextAccessor _ctx;
    private readonly ILogger<OneDriveService> _logger;

    private const string GraphDriveRoot = "https://graph.microsoft.com/v1.0/me/drive/root/children"
        + "?$select=id,name,file,folder,size,lastModifiedDateTime,webUrl"
        + "&$orderby=lastModifiedDateTime+desc&$top=100";

    public OneDriveService(
        IHttpClientFactory http,
        IHttpContextAccessor ctx,
        ILogger<OneDriveService> logger)
    {
        _http   = http;
        _ctx    = ctx;
        _logger = logger;
    }

    /// <summary>
    /// Returns the user's OneDrive root items, or an error snapshot if not connected.
    /// </summary>
    public async Task<OneDriveSnapshot> GetRootFilesAsync(
        PlatformSettings settings, CancellationToken ct = default)
    {
        var token = ResolveToken(settings);
        if (token is null)
        {
            return new OneDriveSnapshot(false, null,
                "OneDrive is not connected. Connect via Settings → Integrations → My Outlook " +
                "(enable 'Sync OneDrive').",
                Array.Empty<OneDriveItem>());
        }

        try
        {
            var client = _http.CreateClient("Graph");
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);

            var resp = await client.GetAsync(GraphDriveRoot, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var detail = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogWarning("Graph /me/drive returned {Status}: {Body}",
                    resp.StatusCode, detail.Length > 400 ? detail[..400] : detail);

                return new OneDriveSnapshot(true, null,
                    $"OneDrive access failed ({(int)resp.StatusCode}). " +
                    "Your access token may have expired — reconnect Outlook in Integrations.",
                    Array.Empty<OneDriveItem>());
            }

            using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync(ct));
            var items = new List<OneDriveItem>();

            if (doc.RootElement.TryGetProperty("value", out var arr))
            {
                foreach (var el in arr.EnumerateArray())
                {
                    var name = el.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    var id   = el.TryGetProperty("id",   out var i) ? i.GetString() ?? "" : "";
                    var url  = el.TryGetProperty("webUrl", out var u) ? u.GetString() : null;
                    var size = el.TryGetProperty("size", out var s) ? s.GetInt64() : 0L;

                    DateTime? modified = null;
                    if (el.TryGetProperty("lastModifiedDateTime", out var lm) &&
                        DateTime.TryParse(lm.GetString(), out var dt))
                        modified = dt;

                    bool isFolder = el.TryGetProperty("folder", out _);

                    items.Add(new OneDriveItem(id, name, isFolder, size, modified, url));
                }
            }

            return new OneDriveSnapshot(true, null, null, items);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OneDrive load failed");
            return new OneDriveSnapshot(true, null,
                $"Could not load OneDrive: {ex.Message}",
                Array.Empty<OneDriveItem>());
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    private string? ResolveToken(PlatformSettings settings)
    {
        var userCode = _ctx.HttpContext?.User?
            .FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        // Prefer the per-user "My Outlook" connection with SyncDrive enabled
        if (!string.IsNullOrWhiteSpace(userCode) &&
            settings.MyOutlookUserConnections.TryGetValue(userCode, out var perUser) &&
            perUser.Enabled && perUser.SyncDrive &&
            !string.IsNullOrWhiteSpace(perUser.AccessToken))
        {
            return perUser.AccessToken;
        }

        // Fall back to the shared "My Outlook" integration
        if (settings.MyOutlook.Enabled && settings.MyOutlook.SyncDrive &&
            !string.IsNullOrWhiteSpace(settings.MyOutlook.AccessToken))
        {
            return settings.MyOutlook.AccessToken;
        }

        return null;
    }
}

public record OneDriveSnapshot(
    bool IsConnected,
    string? MailboxName,
    string? ErrorMessage,
    IReadOnlyList<OneDriveItem> Items);

public record OneDriveItem(
    string   Id,
    string   Name,
    bool     IsFolder,
    long     Size,
    DateTime? ModifiedTime,
    string?  WebUrl);
