using Microsoft.AspNetCore.Components.Server.Circuits;

namespace MyDesk.Web.Services;

/// <summary>
/// Logs Blazor Server circuit lifecycle events. The CircuitHandler interface lets us
/// observe when a circuit opens, closes, connects, disconnects, or throws an unhandled
/// exception — useful for diagnosing the "Kestrel feels like it died" reports
/// (which are usually individual circuits dying, not the host process).
/// </summary>
public class ResilientCircuitHandler : CircuitHandler
{
    private readonly ILogger<ResilientCircuitHandler> _logger;

    public ResilientCircuitHandler(ILogger<ResilientCircuitHandler> logger)
    {
        _logger = logger;
    }

    public override Task OnCircuitOpenedAsync(Circuit circuit, CancellationToken ct)
    {
        _logger.LogDebug("Circuit opened: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnCircuitClosedAsync(Circuit circuit, CancellationToken ct)
    {
        _logger.LogDebug("Circuit closed: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionDownAsync(Circuit circuit, CancellationToken ct)
    {
        _logger.LogDebug("Circuit connection down: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }

    public override Task OnConnectionUpAsync(Circuit circuit, CancellationToken ct)
    {
        _logger.LogDebug("Circuit reconnected: {CircuitId}", circuit.Id);
        return Task.CompletedTask;
    }
}
