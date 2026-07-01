using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Teams;
using Microsoft.Bot.Schema;
using MyDesk.Shared.Services;

namespace MyDesk.Web.Bot;

/// <summary>
/// MyDesk Teams Bot — handles personal and channel messages via Bot Framework.
/// Registered in Teams app manifest as the bot handler for the MyDesk Teams app.
/// </summary>
public class MyDeskTeamsBot : TeamsActivityHandler
{
    private readonly PlatformSettingsService _settings;
    private readonly ILogger<MyDeskTeamsBot> _logger;

    public MyDeskTeamsBot(
        PlatformSettingsService settings,
        ILogger<MyDeskTeamsBot> logger)
    {
        _settings = settings;
        _logger = logger;
    }

    protected override async Task OnMessageActivityAsync(
        ITurnContext<IMessageActivity> turnContext,
        CancellationToken ct)
    {
        var text = (turnContext.Activity.Text ?? "").Trim().ToLowerInvariant();

        var reply = text switch
        {
            var t when t.Contains("help") =>
                BuildHelpCard(),

            var t when t.Contains("timesheet") =>
                MessageFactory.Text(
                    $"Open your timesheets here: {_settings.Current.CompanyWebsite}/timesheets\n\n" +
                    "You can also open the **Timesheets** tab in the MyDesk Teams app."),

            var t when t.Contains("approval") =>
                MessageFactory.Text(
                    $"Check your pending approvals here: {_settings.Current.CompanyWebsite}/approvals/pending"),

            _ => BuildHelpCard()
        };

        await turnContext.SendActivityAsync(reply, ct);
    }

    protected override async Task OnMembersAddedAsync(
        IList<ChannelAccount> membersAdded,
        ITurnContext<IConversationUpdateActivity> turnContext,
        CancellationToken ct)
    {
        foreach (var member in membersAdded)
        {
            if (member.Id == turnContext.Activity.Recipient.Id) continue;

            var welcome = MessageFactory.Text(
                $"Welcome to **{_settings.GetBrandingName()}**! 👋\n\n" +
                "I'm your MyDesk AI assistant. Type `help` to see what I can do.");

            await turnContext.SendActivityAsync(welcome, ct);
        }
    }

    private static IActivity BuildHelpCard()
    {
        return MessageFactory.Text(
            "**MyDesk Commands**\n\n" +
            "- `timesheets` — Open your timesheets\n" +
            "- `approvals` — View pending approvals\n" +
            "- `help` — Show this help message\n\n" +
            "You can also use the **Ask AI** tab for natural language queries.");
    }
}
