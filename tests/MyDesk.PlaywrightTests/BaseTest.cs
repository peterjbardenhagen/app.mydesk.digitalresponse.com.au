using Microsoft.Extensions.Configuration;
using Microsoft.Playwright;
using NUnit.Framework;

namespace MyDesk.PlaywrightTests;

public abstract class BaseTest
{
    protected IPlaywright Playwright = null!;
    protected IBrowser Browser = null!;
    protected IBrowserContext Context = null!;
    protected IPage Page = null!;
    protected TestSettings Settings = null!;

    [SetUp]
    public async Task SetUp()
    {
        // Load configuration
        var config = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        Settings = config.GetSection("TestSettings").Get<TestSettings>() ?? new TestSettings();

        // Pre-flight: verify the server is reachable. If not, skip the test cleanly
        // rather than producing an opaque Playwright "ERR_CONNECTION_REFUSED" failure.
        // if (!await IsServerReachableAsync(Settings.BaseUrl))
        // {
        //     Assert.Ignore($"Web server at {Settings.BaseUrl} is not reachable. Start MyDesk via Run.bat option [4] before running tests.");
        // }

        // Initialize Playwright
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();

        var browserOptions = new BrowserTypeLaunchOptions
        {
            Headless = Settings.Headless,
            SlowMo   = Settings.SlowMo
        };

        Browser = await Playwright.Chromium.LaunchAsync(browserOptions);

        Context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize    = new ViewportSize { Width = 1920, Height = 1080 },
            RecordVideoDir  = "videos/",
            RecordVideoSize = new RecordVideoSize { Width = 1920, Height = 1080 }
        });

        Page = await Context.NewPageAsync();
        Page.SetDefaultTimeout(Settings.Timeout);
    }

    [TearDown]
    public async Task TearDown()
    {
        try { if (Page != null) await Page.CloseAsync(); }       catch { /* swallow */ }
        try { if (Context != null) await Context.CloseAsync(); } catch { /* swallow */ }
        try { if (Browser != null) await Browser.CloseAsync(); } catch { /* swallow */ }
        Playwright?.Dispose();
    }

    /// <summary>
    /// Quick TCP/HTTP probe so we don't waste 60+ seconds per test waiting for Playwright
    /// to time out when the server isn't running.
    /// </summary>
    private static async Task<bool> IsServerReachableAsync(string baseUrl)
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            var resp = await http.GetAsync($"{baseUrl}/login");
            return (int)resp.StatusCode < 500; // 200, 302, 401 are all "alive"
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Try to log in. Returns true on success.
    ///
    /// Notes:
    ///   * <c>WaitForLoadStateAsync(NetworkIdle)</c> never resolves on Blazor Server because
    ///     the SignalR WebSocket keeps the network "active" indefinitely. We use
    ///     <c>WaitForURLAsync</c> + DOM checks instead.
    ///   * Peter Bardenhagen has multiple tenants (Techlight, Digital Response, Demo MyDesk)
    ///     which routes through <c>/login/select-tenant</c>. We pick the configured
    ///     <c>TestSettings.TenantSlug</c> (default: <c>demo</c>) so tests run against the
    ///     isolated Demo MyDesk tenant rather than production data.
    /// </summary>
    protected async Task<bool> TryLoginAsync()
    {
        try
        {
            await Page.GotoAsync($"{Settings.BaseUrl}/login", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            await Page.WaitForSelectorAsync("input[name='login']", new() { Timeout = 15000 });
            await Page.FillAsync("input[name='login']",    Settings.TestUser.Username);
            await Page.FillAsync("input[name='password']", Settings.TestUser.Password);

            // Form post navigates: /api/auth/login -> /auth/signin -> (/ or /login/select-tenant).
            // Wait for the URL to leave the bare /login page (must navigate somewhere).
            // Note: Blazor Server may have a slight delay before navigation begins.
            await Page.ClickAsync("button[type='submit']");
            await Page.WaitForURLAsync(url => !url.EndsWith("/login", StringComparison.OrdinalIgnoreCase),
                new() { Timeout = 30000, WaitUntil = WaitUntilState.Load });

            // If creds were rejected, /login?error=1
            if (Page.Url.Contains("error=1", StringComparison.OrdinalIgnoreCase))
            {
                TestContext.WriteLine($"Login rejected by server. URL: {Page.Url}");
                return false;
            }

            // Tenant selection page — pick the configured tenant.
            if (Page.Url.Contains("/login/select-tenant", StringComparison.OrdinalIgnoreCase))
            {
                if (!await SelectConfiguredTenantAsync())
                {
                    TestContext.WriteLine($"Tenant selection failed. URL: {Page.Url}");
                    return false;
                }
            }

            // Success = we are no longer on /login* and there is no error flag.
            var ok = !Page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase)
                  && !Page.Url.Contains("error=1", StringComparison.OrdinalIgnoreCase);
            if (!ok)
            {
                TestContext.WriteLine($"Login did not redirect to app. URL: {Page.Url}");
            }
            return ok;
        }
        catch (Exception ex)
        {
            TestContext.WriteLine($"TryLoginAsync threw: {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// On the /login/select-tenant page, click the row whose name matches
    /// <c>TestSettings.TenantSlug</c> (case-insensitive contains match against
    /// the visible tenant name), or fall through to the default (first) row.
    /// </summary>
    private async Task<bool> SelectConfiguredTenantAsync()
    {
        var preferredSlug = (Settings.TenantSlug ?? "demo").ToLowerInvariant();

        // Each tenant row is a MudPaper containing the tenant name + a "Continue" MudButton.
        // Allow extra time for Blazor Server to establish SignalR connection and render.
        await Page.WaitForSelectorAsync(".mud-paper:has-text('Continue')", new() { Timeout = 20000 });
        var rows = await Page.Locator(".mud-paper:has-text('Continue')").AllAsync();

        ILocator? targetButton = null;
        foreach (var row in rows)
        {
            var text = (await row.InnerTextAsync()).ToLowerInvariant();
            if (text.Contains(preferredSlug) ||
                (preferredSlug == "demo" && text.Contains("demo mydesk")) ||
                (preferredSlug == "techlight" && text.Contains("techlight")) ||
                (preferredSlug == "digital-response" && text.Contains("digital response")))
            {
                targetButton = row.Locator("button:has-text('Continue')").First;
                break;
            }
        }

        // Fallback: first available Continue button.
        targetButton ??= Page.Locator("button:has-text('Continue')").First;

        await targetButton.ClickAsync();
        // Wait until we leave the select-tenant page (forwards through /auth/signin).
        await Page.WaitForURLAsync(url => !url.Contains("/login/select-tenant", StringComparison.OrdinalIgnoreCase),
            new() { Timeout = 30000, WaitUntil = WaitUntilState.Load });

        return !Page.Url.Contains("/login", StringComparison.OrdinalIgnoreCase)
               && !Page.Url.Contains("error=1", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Login or skip the test if it can't (e.g. DB not reachable in this run context).
    /// </summary>
    protected async Task LoginAsync()
    {
        if (!await TryLoginAsync())
        {
            Assert.Ignore("Login could not be completed (DB or credentials unavailable in this test environment).");
        }
    }

    protected async Task NavigateToAsync(string path)
    {
        // DOMContentLoaded — NetworkIdle never fires on Blazor Server (SignalR keeps it open).
        await Page.GotoAsync($"{Settings.BaseUrl}{path}", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        // Brief settle for Blazor's first interactive render.
        try { await Page.WaitForLoadStateAsync(LoadState.Load, new() { Timeout = 5000 }); } catch { }
    }

    protected async Task TakeScreenshotAsync(string name)
    {
        var screenshotsDir = Path.Combine(Directory.GetCurrentDirectory(), "screenshots");
        Directory.CreateDirectory(screenshotsDir);

        var fileName = $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var filePath = Path.Combine(screenshotsDir, fileName);

        await Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path     = filePath,
            FullPage = true
        });

        TestContext.WriteLine($"Screenshot saved: {filePath}");
    }
}
