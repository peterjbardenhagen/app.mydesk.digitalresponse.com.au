using Microsoft.AspNetCore.Mvc;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Integration.AspNet.Core;

namespace MyDesk.Web.Api.Controllers;

/// <summary>
/// Receives incoming Bot Framework activities from the Microsoft Teams channel.
/// Registered as the webhook endpoint in the Teams app manifest bot section.
/// URL: POST /bot/messages
/// </summary>
[ApiController]
[Route("bot")]
public class BotController : ControllerBase
{
    private readonly IBotFrameworkHttpAdapter _adapter;
    private readonly IBot _bot;

    public BotController(IBotFrameworkHttpAdapter adapter, IBot bot)
    {
        _adapter = adapter;
        _bot = bot;
    }

    [HttpPost("messages")]
    public async Task PostAsync(CancellationToken ct)
    {
        await _adapter.ProcessAsync(Request, Response, _bot, ct);
    }
}
