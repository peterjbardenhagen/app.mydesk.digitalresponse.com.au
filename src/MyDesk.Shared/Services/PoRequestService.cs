using Microsoft.Extensions.Logging;
using MyDesk.Shared.Models;

namespace MyDesk.Shared.Services;

/// <summary>
/// In-memory PO request service ported from legacy MyDesk.
/// Routes vehicle maintenance requests by division/state to the appropriate
/// inbox via the existing EmailService.
/// TODO: persist to database
/// </summary>
public class PoRequestService
{
    private readonly EmailService _email;
    private readonly ILogger<PoRequestService> _logger;
    private static readonly object _lock = new();
    private static readonly List<PoRequest> _store = new();

    // State / division → email routing.  Keep small + obvious so it can be
    // moved to platform-settings later without code changes.
    private static readonly Dictionary<string, string> RoutingTable = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Traffic Mgmt"] = "trafficmgmt-fleet@example.com",
        ["QLD"]          = "qld-fleet@example.com",
        ["NSW"]          = "nsw-fleet@example.com",
        ["VIC"]          = "vic-fleet@example.com",
        ["SA"]           = "sa-fleet@example.com"
    };

    public PoRequestService(EmailService email, ILogger<PoRequestService> logger)
    {
        _email = email;
        _logger = logger;
    }

    public Task<List<PoRequest>> GetAllAsync()
    {
        lock (_lock)
        {
            return Task.FromResult(_store.OrderByDescending(p => p.SubmittedAt).ToList());
        }
    }

    public IReadOnlyDictionary<string, string> GetRouting() => RoutingTable;

    public async Task<PoRequest> SubmitAsync(PoRequest request)
    {
        // Resolve routing email from division.
        if (!RoutingTable.TryGetValue(request.Division, out var routedEmail))
        {
            routedEmail = RoutingTable["Traffic Mgmt"];
        }
        request.RoutedToEmail = routedEmail;
        request.SubmittedAt = DateTime.Now;
        request.Status = "Submitted";

        lock (_lock)
        {
            request.Id = _store.Count == 0 ? 1 : _store.Max(p => p.Id) + 1;
            _store.Add(request);
        }

        // Send notification email through the existing EmailService infrastructure.
        var subject = $"PO Request — {request.MaintenanceType} — Vehicle {request.VehicleRegistration}";
        var body = BuildEmailBody(request);
        try
        {
            // Use raw send to keep this service decoupled from quote/invoice flows.
            await _email.SendAsync(routedEmail, subject, body);
            _logger.LogInformation("PO Request {Id} routed to {Email}", request.Id, routedEmail);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "PO Request {Id} email failed (still saved)", request.Id);
        }
        return request;
    }

    private static string BuildEmailBody(PoRequest r)
    {
        var sb = new System.Text.StringBuilder();
        sb.Append("<h2>Vehicle Maintenance PO Request</h2>");
        sb.Append("<table cellpadding='4'>");
        sb.Append($"<tr><td><b>Submitted by:</b></td><td>{r.RequesterName} ({r.RequesterUserCode})</td></tr>");
        sb.Append($"<tr><td><b>Division:</b></td><td>{r.Division}</td></tr>");
        sb.Append($"<tr><td><b>Vehicle rego:</b></td><td>{r.VehicleRegistration}</td></tr>");
        sb.Append($"<tr><td><b>Vehicle:</b></td><td>{r.VehicleDescription}</td></tr>");
        sb.Append($"<tr><td><b>Maintenance type:</b></td><td>{r.MaintenanceType}</td></tr>");
        sb.Append($"<tr><td><b>Supplier:</b></td><td>{r.Supplier}</td></tr>");
        sb.Append($"<tr><td><b>Estimated amount:</b></td><td>{r.EstimatedAmount:C2}</td></tr>");
        sb.Append($"<tr><td><b>Required by:</b></td><td>{r.RequiredByDate:dd MMM yyyy}</td></tr>");
        sb.Append("</table>");
        sb.Append("<p><b>Description:</b><br>");
        sb.Append(System.Net.WebUtility.HtmlEncode(r.Description ?? "")).Append("</p>");
        return sb.ToString();
    }
}
