using System.Linq;
using System.Text.Json;
using MyDesk.Shared.Models;
using Xunit;

namespace MyDesk.UnitTests;

/// <summary>
/// Tests for Telegram Bot PlatformSettings models
/// </summary>
public class TelegramBotModelTests
{
    [Fact]
    public void TelegramSettings_Defaults_AreValid()
    {
        var settings = new TelegramSettings();
        
        Assert.Null(settings.ProdBotToken);
        Assert.Null(settings.DevBotToken);
        Assert.Empty(settings.AllowedUsers);
        Assert.Empty(settings.AllowedChatIds);
        Assert.Equal("prod", settings.DefaultEnvironment);
        Assert.True(settings.EnableVoiceTranscription);
        Assert.True(settings.EnableMarkdown);
    }

    [Fact]
    public void TelegramBotConfig_Defaults_AreValid()
    {
        var config = new TelegramBotConfig();
        
        Assert.Equal("", config.BotToken);
        Assert.Equal("", config.BotUsername);
        Assert.Empty(config.AllowedUsers);
        Assert.Empty(config.AllowedChatIds);
        Assert.Equal("prod", config.Environment);
        Assert.True(config.Enabled);
    }

    [Fact]
    public void TelegramBotConfig_CanConfigureProd()
    {
        var config = new TelegramBotConfig
        {
            BotToken = "123456:PROD-TOKEN",
            BotUsername = "mydeskdr_bot",
            Environment = "prod",
            WebhookUrl = "https://mydesk.digitalresponse.com.au/api/telegram/webhook/prod",
            AllowedUsers = new[] { "peterb", "admin" },
            AllowedChatIds = new[] { 123456789L },
            Enabled = true
        };

        Assert.Equal("123456:PROD-TOKEN", config.BotToken);
        Assert.Equal("mydeskdr_bot", config.BotUsername);
        Assert.Equal("prod", config.Environment);
        Assert.Equal(2, config.AllowedUsers.Length);
        Assert.Single(config.AllowedChatIds);
    }

    [Fact]
    public void TelegramBotConfig_CanConfigureDev()
    {
        var config = new TelegramBotConfig
        {
            BotToken = "987654:DEV-TOKEN",
            BotUsername = "mydeskdev_bot",
            Environment = "dev",
            WebhookUrl = "https://dev.digitalresponse.com.au/api/telegram/webhook/dev",
            AllowedUsers = new[] { "peterb" },
            AllowedChatIds = Array.Empty<long>(),
            Enabled = true
        };

        Assert.Equal("987654:DEV-TOKEN", config.BotToken);
        Assert.Equal("mydeskdev_bot", config.BotUsername);
        Assert.Equal("dev", config.Environment);
        Assert.Single(config.AllowedUsers);
    }

    [Fact]
    public void PlatformSettings_CanHoldTelegramBots()
    {
        var settings = new PlatformSettings();
        
        Assert.NotNull(settings.Telegram);
        Assert.NotNull(settings.TelegramBots);
        Assert.Empty(settings.TelegramBots);
        
        // Add a prod bot
        settings.TelegramBots["prod"] = new TelegramBotConfig
        {
            BotToken = "123:ABC",
            BotUsername = "myprod_bot",
            WebhookUrl = "https://example.com/api/telegram/webhook/prod"
        };
        
        // Add a dev bot
        settings.TelegramBots["dev"] = new TelegramBotConfig
        {
            BotToken = "456:DEF",
            BotUsername = "mydev_bot",
            WebhookUrl = "https://dev.example.com/api/telegram/webhook/dev"
        };
        
        Assert.Equal(2, settings.TelegramBots.Count);
        Assert.True(settings.TelegramBots.ContainsKey("prod"));
        Assert.True(settings.TelegramBots.ContainsKey("dev"));
        Assert.Equal("myprod_bot", settings.TelegramBots["prod"].BotUsername);
        Assert.Equal("mydev_bot", settings.TelegramBots["dev"].BotUsername);
    }

    [Fact]
    public void WebhookEndpoints_ProdAndDev_AreDifferent()
    {
        var prodUrl = "https://mydesk.digitalresponse.com.au/api/telegram/webhook/prod";
        var devUrl = "https://dev.digitalresponse.com.au/api/telegram/webhook/dev";

        Assert.NotEqual(prodUrl, devUrl);
        Assert.Contains("prod", prodUrl);
        Assert.Contains("dev", devUrl);
        Assert.DoesNotContain("prod", devUrl);
        Assert.DoesNotContain("dev", prodUrl);
    }

    [Fact]
    public void TelegramSettings_JsonSerialization_RoundTrip()
    {
        var original = new TelegramSettings
        {
            ProdBotToken = "123:PROD",
            DevBotToken = "456:DEV",
            AllowedUsers = new[] { "user1", "user2" },
            AllowedChatIds = new[] { 123L, 456L },
            DefaultEnvironment = "prod",
            WebhookBaseUrl = "https://example.com",
            EnableVoiceTranscription = true,
            EnableMarkdown = true
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<TelegramSettings>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.ProdBotToken, deserialized.ProdBotToken);
        Assert.Equal(original.DevBotToken, deserialized.DevBotToken);
        Assert.Equal(original.AllowedUsers, deserialized.AllowedUsers);
        Assert.Equal(original.AllowedChatIds, deserialized.AllowedChatIds);
    }

    [Fact]
    public void TelegramBotConfig_JsonSerialization_RoundTrip()
    {
        var original = new TelegramBotConfig
        {
            BotToken = "789:BOT",
            BotUsername = "my_bot",
            Environment = "staging",
            WebhookUrl = "https://staging.example.com/api/telegram/webhook/staging",
            AllowedUsers = new[] { "dev1", "dev2", "dev3" },
            AllowedChatIds = new[] { 789L },
            Enabled = true,
            CustomCommands = new Dictionary<string, string>
            {
                ["/status"] = "Check system status",
                ["/report"] = "Generate daily report"
            }
        };

        var json = JsonSerializer.Serialize(original);
        var deserialized = JsonSerializer.Deserialize<TelegramBotConfig>(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.BotToken, deserialized.BotToken);
        Assert.Equal(original.BotUsername, deserialized.BotUsername);
        Assert.Equal("staging", deserialized.Environment);
        Assert.Equal(3, deserialized.AllowedUsers.Length);
        Assert.Single(deserialized.AllowedChatIds);
        Assert.True(deserialized.Enabled);
        Assert.Equal(2, deserialized.CustomCommands.Count);
    }

    [Fact]
    public void MultiEnvConfig_Serialization()
    {
        var configs = new Dictionary<string, TelegramBotConfig>
        {
            ["prod"] = new() { BotToken = "P1", BotUsername = "prod_bot", WebhookUrl = "https://prod.example.com/webhook" },
            ["dev"] = new() { BotToken = "D1", BotUsername = "dev_bot", WebhookUrl = "https://dev.example.com/webhook" },
            ["staging"] = new() { BotToken = "S1", BotUsername = "staging_bot", WebhookUrl = "https://staging.example.com/webhook" }
        };

        // Simulate storing in PlatformSettings
        var settings = new PlatformSettings();
        foreach (var kvp in configs)
        {
            settings.TelegramBots[kvp.Key] = kvp.Value;
        }

        // Serialize to JSON (same way PlatformSettingsEntities stores it)
        var json = JsonSerializer.Serialize(settings);
        var restored = JsonSerializer.Deserialize<PlatformSettings>(json);

        Assert.NotNull(restored);
        Assert.Equal(3, restored.TelegramBots.Count);
        Assert.Equal("prod_bot", restored.TelegramBots["prod"].BotUsername);
        Assert.Equal("dev_bot", restored.TelegramBots["dev"].BotUsername);
        Assert.Equal("staging_bot", restored.TelegramBots["staging"].BotUsername);
        Assert.Equal("https://prod.example.com/webhook", restored.TelegramBots["prod"].WebhookUrl);
    }
}

/// <summary>
/// Tests for Teams app manifests
/// </summary>
public class TeamsManifestTests
{
    private string GetProjectRoot()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        // Walk up from bin/Debug/net10.0 to find project root (look for .slnx or .git)
        var di = new DirectoryInfo(dir);
        while (di != null && !di.GetFiles("*.slnx").Any() && !di.GetFiles("*.sln").Any() && !Directory.Exists(Path.Combine(di.FullName, ".git")))
        {
            di = di.Parent;
        }
        return di?.FullName ?? Directory.GetCurrentDirectory();
    }

    [Fact]
    public void DevManifest_Exists()
    {
        var root = GetProjectRoot();
        var manifestPath = Path.Combine(root, "src", "MyDesk.Teams", "manifest-dev.json");

        Assert.True(File.Exists(manifestPath), $"Dev manifest not found at {manifestPath}");
    }

    [Fact]
    public void DevManifest_HasDevDomain()
    {
        var root = GetProjectRoot();
        var manifestPath = Path.Combine(root, "src", "MyDesk.Teams", "manifest-dev.json");
        if (!File.Exists(manifestPath)) return;

        var json = File.ReadAllText(manifestPath);
        using var doc = JsonDocument.Parse(json);
        var rootEl = doc.RootElement;

        Assert.Equal("MyDesk Dev", rootEl.GetProperty("name").GetProperty("short").GetString());
        
        var domains = rootEl.GetProperty("validDomains");
        var hasDevDomain = false;
        foreach (var d in domains.EnumerateArray())
        {
            if (d.GetString() == "dev.digitalresponse.com.au")
                hasDevDomain = true;
        }
        Assert.True(hasDevDomain, "Dev manifest should include dev.digitalresponse.com.au");
    }

    [Fact]
    public void PackageScript_Exists()
    {
        var root = GetProjectRoot();
        var scriptPath = Path.Combine(root, "src", "MyDesk.Teams", "Package-TeamsApp.ps1");
        Assert.True(File.Exists(scriptPath), $"Package script not found at {scriptPath}");
    }

    [Fact]
    public void ProdManifest_Exists()
    {
        var root = GetProjectRoot();
        Assert.True(File.Exists(Path.Combine(root, "src", "MyDesk.Teams", "manifest.json")));
    }

    [Fact]
    public void DevDeclarativeAgent_Exists()
    {
        var root = GetProjectRoot();
        var agentPath = Path.Combine(root, "src", "MyDesk.Teams", "declarativeAgent-dev.json");
        Assert.True(File.Exists(agentPath), $"Dev declarative agent not found at {agentPath}");
    }

    [Fact]
    public void DevAiPlugin_Exists()
    {
        var root = GetProjectRoot();
        var pluginPath = Path.Combine(root, "src", "MyDesk.Teams", "ai-plugin-dev.json");
        Assert.True(File.Exists(pluginPath), $"Dev AI plugin not found at {pluginPath}");
    }

    [Fact]
    public void PackageScript_PackagesDevManifests()
    {
        var root = GetProjectRoot();
        var scriptPath = Path.Combine(root, "src", "MyDesk.Teams", "Package-TeamsApp.ps1");
        if (!File.Exists(scriptPath)) return;

        var script = File.ReadAllText(scriptPath);
        Assert.Contains("mydesk-teams-dev-copilot.zip", script);
        Assert.Contains("mydesk-teams-dev-basic.zip", script);
        Assert.Contains("manifest-dev.json", script);
    }
}

/// <summary>
/// Tests for agents-handoff skill auto-trigger logic
/// </summary>
public class AutoTriggerTests
{
    private static readonly string[] Triggers = 
    {
        "code", "fix", "refactor", "test", "implement",
        "create.*script", "add.*test", "debug", "build",
        "write.*function", "write.*class", "create.*component"
    };

    private static readonly string[] Excluded = 
    {
        @"^read", @"^search", @"^list", @"^explain", @"^document",
        @"^what is", @"^how to", @"^show me"
    };

    private static bool ShouldTrigger(string task)
    {
        var taskLower = task.ToLower();
        foreach (var pattern in Excluded)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(taskLower, pattern))
                return false;
        }
        foreach (var pattern in Triggers)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(taskLower, pattern))
                return true;
        }
        return false;
    }

    [Theory]
    [InlineData("Fix the isVoice warning in TelegramBotService.cs", true)]
    [InlineData("Add unit tests for TelegramBotService", true)]
    [InlineData("Refactor the config loading method", true)]
    [InlineData("Implement new webhook handler", true)]
    [InlineData("Debug the connection issue", true)]
    [InlineData("Read the current configuration", false)]
    [InlineData("List all available endpoints", false)]
    [InlineData("Explain how the bot works", false)]
    [InlineData("Document the API", false)]
    [InlineData("Show me the logs", false)]
    [InlineData("What is the current status", false)]
    [InlineData("How to configure the bot", false)]
    [InlineData("Create a setup script", true)]
    public void TriggerPatterns_MatchCorrectly(string task, bool expected)
    {
        Assert.Equal(expected, ShouldTrigger(task));
    }
}

/// <summary>
/// Tests for webhook setup script
/// </summary>
public class SetupScriptTests
{
    [Fact]
    public void WebhookScript_Exists()
    {
        var root = GetProjectRoot();
        var scriptPath = Path.Combine(root, "scripts", "setup-telegram-webhooks.ps1");
        Assert.True(File.Exists(scriptPath), $"Setup script not found at {scriptPath}");
    }

    [Fact]
    public void WebhookScript_HasProdAndDevSupport()
    {
        var root = GetProjectRoot();
        var scriptPath = Path.Combine(root, "scripts", "setup-telegram-webhooks.ps1");
        if (!File.Exists(scriptPath)) return;

        var script = File.ReadAllText(scriptPath);
        Assert.Contains("Production", script);
        Assert.Contains("Development", script);
        Assert.Contains("ProdToken", script);
        Assert.Contains("DevToken", script);
    }

    private static string GetProjectRoot()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        var di = new DirectoryInfo(dir);
        while (di != null && !di.GetFiles("*.slnx").Any() && !di.GetFiles("*.sln").Any() && !Directory.Exists(Path.Combine(di.FullName, ".git")))
        {
            di = di.Parent;
        }
        return di?.FullName ?? Directory.GetCurrentDirectory();
    }
}

/// <summary>
/// Tests for documentation
/// </summary>
public class DocumentationTests
{
    [Fact]
    public void TelegramDocs_Exists()
    {
        var root = GetProjectRoot();
        var docPath = Path.Combine(root, "docs", "telegram.md");
        Assert.True(File.Exists(docPath), $"Telegram docs not found at {docPath}");
    }

    [Fact]
    public void TelegramDocs_HasSetupInstructions()
    {
        var root = GetProjectRoot();
        var docPath = Path.Combine(root, "docs", "telegram.md");
        if (!File.Exists(docPath)) return;

        var content = File.ReadAllText(docPath);
        Assert.Contains("BotFather", content);
        Assert.Contains("Webhook", content);
        Assert.Contains("prod", content);
        Assert.Contains("dev", content);
    }

    private static string GetProjectRoot()
    {
        var dir = AppDomain.CurrentDomain.BaseDirectory;
        var di = new DirectoryInfo(dir);
        while (di != null && !di.GetFiles("*.slnx").Any() && !di.GetFiles("*.sln").Any() && !Directory.Exists(Path.Combine(di.FullName, ".git")))
        {
            di = di.Parent;
        }
        return di?.FullName ?? Directory.GetCurrentDirectory();
    }
}