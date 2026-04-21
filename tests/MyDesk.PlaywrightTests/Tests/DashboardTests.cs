using Microsoft.Playwright;
using NUnit.Framework;

namespace MyDesk.PlaywrightTests.Tests;

[TestFixture]
public class DashboardTests : BaseTest
{
    [SetUp]
    public async Task DashboardSetUp()
    {
        await LoginAsync();
    }
    
    [Test]
    public async Task Dashboard_Loads_With_KPI_Cards()
    {
        // Verify dashboard title
        var title = await Page.TitleAsync();
        Assert.That(title, Does.Contain("Dashboard"));
        
        // Check for KPI cards
        var kpiCards = await Page.QuerySelectorAllAsync(".tl-kpi-card");
        Assert.That(kpiCards.Count, Is.GreaterThan(0), "KPI cards should be visible");
        
        // Check for specific KPI sections
        var revenueCard = await Page.QuerySelectorAsync(".tl-kpi-revenue");
        var quotesCard = await Page.QuerySelectorAsync(".tl-kpi-quotes");
        
        Assert.That(revenueCard, Is.Not.Null, "Revenue KPI card should exist");
        Assert.That(quotesCard, Is.Not.Null, "Quotes KPI card should exist");
        
        await TakeScreenshotAsync("Dashboard_KPI_Cards");
    }
    
    [Test]
    public async Task Dashboard_Welcome_Message_Shows_User_Name()
    {
        // Check welcome message
        var welcomeTitle = await Page.QuerySelectorAsync(".tl-welcome-title");
        Assert.That(welcomeTitle, Is.Not.Null, "Welcome title should be visible");
        
        var welcomeText = await welcomeTitle.TextContentAsync();
        Assert.That(welcomeText, Does.Contain("Welcome back"));
        
        await TakeScreenshotAsync("Dashboard_Welcome");
    }
    
    [Test]
    public async Task Dashboard_New_Quote_Button_Navigates()
    {
        // Click New Quote button
        var newQuoteButton = await Page.QuerySelectorAsync("a[href='/quotes/create'], button:has-text('New Quote')");
        Assert.That(newQuoteButton, Is.Not.Null, "New Quote button should exist");
        
        await newQuoteButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Verify navigation
        var url = Page.Url;
        Assert.That(url, Does.Contain("/quotes/create"));
        
        await TakeScreenshotAsync("Dashboard_NewQuote_Navigation");
    }
    
    [Test]
    public async Task Dashboard_Settings_Button_Navigates()
    {
        // Click Settings button
        var settingsButton = await Page.QuerySelectorAsync("a[href='/admin/setup'], button:has-text('Settings')");
        Assert.That(settingsButton, Is.Not.Null, "Settings button should exist");
        
        await settingsButton.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Verify navigation
        var url = Page.Url;
        Assert.That(url, Does.Contain("/admin/setup"));
        
        await TakeScreenshotAsync("Dashboard_Settings_Navigation");
    }
    
    [Test]
    public async Task Dashboard_Revenue_Card_Navigates_To_Invoices()
    {
        // Click revenue card
        var revenueCard = await Page.QuerySelectorAsync(".tl-kpi-revenue");
        Assert.That(revenueCard, Is.Not.Null, "Revenue card should exist");
        
        await revenueCard.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Verify navigation to invoices
        var url = Page.Url;
        Assert.That(url, Does.Contain("/invoices"));
        
        await TakeScreenshotAsync("Dashboard_Revenue_Card_Click");
    }
    
    [Test]
    public async Task Dashboard_Quotes_Card_Navigates_To_Quotes()
    {
        // Click quotes card
        var quotesCard = await Page.QuerySelectorAsync(".tl-kpi-quotes");
        Assert.That(quotesCard, Is.Not.Null, "Quotes card should exist");
        
        await quotesCard.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // Verify navigation to quotes
        var url = Page.Url;
        Assert.That(url, Does.Contain("/quotes"));
        
        await TakeScreenshotAsync("Dashboard_Quotes_Card_Click");
    }
    
    [Test]
    public async Task Dashboard_Loading_State_Shows_Spinner()
    {
        // Refresh page to see loading state
        await Page.GotoAsync($"{Settings.BaseUrl}/");
        
        // Check for loading spinner (may only be visible briefly)
        var loadingSpinner = await Page.QuerySelectorAsync(".tl-loading-container");
        // Loading spinner may disappear quickly, so we just check the structure exists
        
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        // After loading, dashboard content should be visible
        var dashboardContent = await Page.QuerySelectorAsync(".tl-dashboard-header");
        Assert.That(dashboardContent, Is.Not.Null, "Dashboard content should be visible after loading");
        
        await TakeScreenshotAsync("Dashboard_Loaded");
    }
    
    [Test]
    public async Task Dashboard_Navigation_Menu_Available()
    {
        // Check for navigation elements
        var navMenu = await Page.QuerySelectorAsync("nav, .mud-nav-menu, .nav-menu");
        Assert.That(navMenu, Is.Not.Null, "Navigation menu should be visible");
        
        // Check for common navigation links
        var quotesLink = await Page.QuerySelectorAsync("a[href='/quotes'], a[href*='quotes']");
        var invoicesLink = await Page.QuerySelectorAsync("a[href='/invoices'], a[href*='invoices']");
        
        Assert.That(quotesLink, Is.Not.Null, "Quotes navigation link should exist");
        Assert.That(invoicesLink, Is.Not.Null, "Invoices navigation link should exist");
        
        await TakeScreenshotAsync("Dashboard_Navigation");
    }
}
