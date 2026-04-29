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
        
        // Initialize Playwright
        Playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        
        var browserOptions = new BrowserTypeLaunchOptions
        {
            Headless = Settings.Headless,
            SlowMo = Settings.SlowMo
        };
        
        Browser = await Playwright.Chromium.LaunchAsync(browserOptions);
        
        Context = await Browser.NewContextAsync(new BrowserNewContextOptions
        {
            ViewportSize = new ViewportSize { Width = 1920, Height = 1080 },
            RecordVideoDir = "videos/",
            RecordVideoSize = new RecordVideoSize { Width = 1920, Height = 1080 }
        });
        
        Page = await Context.NewPageAsync();
        Page.SetDefaultTimeout(Settings.Timeout);
    }
    
    [TearDown]
    public async Task TearDown()
    {
        if (Page != null)
        {
            await Page.CloseAsync();
        }
        
        if (Context != null)
        {
            await Context.CloseAsync();
        }
        
        if (Browser != null)
        {
            await Browser.CloseAsync();
        }
        
        Playwright?.Dispose();
    }
    
    protected async Task LoginAsync()
    {
        await Page.GotoAsync($"{Settings.BaseUrl}/login");
        
        // Wait for login form - the form uses name="login" not type="email"
        await Page.WaitForSelectorAsync("input[name='login']", new PageWaitForSelectorOptions { Timeout = 10000 });
        
        // Fill credentials
        await Page.FillAsync("input[name='login']", Settings.TestUser.Username);
        await Page.FillAsync("input[name='password']", Settings.TestUser.Password);
        
        // Click login button (MudBlazor renders as button.mud-button-filled, we also have id="submit-btn")
        await Page.ClickAsync("#submit-btn");
        
        // Wait for navigation away from login (successful or error state)
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });
    }
    
    protected async Task NavigateToAsync(string path)
    {
        await Page.GotoAsync($"{Settings.BaseUrl}{path}");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
    
    protected async Task TakeScreenshotAsync(string name)
    {
        var screenshotsDir = Path.Combine(Directory.GetCurrentDirectory(), "screenshots");
        Directory.CreateDirectory(screenshotsDir);
        
        var fileName = $"{name}_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        var filePath = Path.Combine(screenshotsDir, fileName);
        
        await Page.ScreenshotAsync(new PageScreenshotOptions 
        { 
            Path = filePath,
            FullPage = true 
        });
        
        TestContext.WriteLine($"Screenshot saved: {filePath}");
    }
}
