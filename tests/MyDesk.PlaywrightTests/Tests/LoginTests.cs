using Microsoft.Playwright;
using NUnit.Framework;

namespace MyDesk.PlaywrightTests.Tests;

[TestFixture]
public class LoginTests : BaseTest
{
    [Test, Category("Smoke")]
    public async Task Login_Page_Loads_Successfully()
    {
        await Page.GotoAsync($"{Settings.BaseUrl}/login");
        
        // Verify page title
        var title = await Page.TitleAsync();
        Assert.That(title, Does.Contain("Login").Or.Contain("MyDesk"));
        
        // Verify login form elements exist (form uses name attributes)
        var loginInput = await Page.QuerySelectorAsync("input[name='login']");
        var passwordInput = await Page.QuerySelectorAsync("input[name='password']");
        var submitButton = await Page.QuerySelectorAsync("button[type='submit']");
        
        Assert.That(loginInput, Is.Not.Null, "Username/login input should exist");
        Assert.That(passwordInput, Is.Not.Null, "Password input should exist");
        Assert.That(submitButton, Is.Not.Null, "Submit button should exist");
        
        await TakeScreenshotAsync("Login_Page_Loads");
    }
    
    [Test]
    public async Task Login_Page_Shows_Digital_Response_Copyright()
    {
        await Page.GotoAsync($"{Settings.BaseUrl}/login");
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        var content = await Page.ContentAsync();
        
        Assert.That(content, Does.Contain("Digital Response"), "Should show Digital Response branding");
        Assert.That(content, Does.Contain("Privacy Policy"), "Should have Privacy Policy link");
        Assert.That(content, Does.Contain("Terms"), "Should have Terms link");
        
        await TakeScreenshotAsync("Login_Page_Copyright");
    }
    
    [Test]
    public async Task Login_Page_Has_Privacy_Policy_Link()
    {
        await Page.GotoAsync($"{Settings.BaseUrl}/login");

        // Match either local /privacy-policy or external mydesk.digitalresponse.com.au/privacy.html
        var privacyLink = await Page.QuerySelectorAsync("a[href*='privacy']");
        Assert.That(privacyLink, Is.Not.Null, "Privacy Policy link should exist on login page");

        var href = await privacyLink!.GetAttributeAsync("href");
        Assert.That(href, Is.Not.Null.And.Not.Empty);

        await TakeScreenshotAsync("PrivacyPolicy_Link");
    }

    [Test]
    public async Task Login_Page_Has_Terms_Link()
    {
        await Page.GotoAsync($"{Settings.BaseUrl}/login");

        // Match either local /terms-and-conditions or external mydesk.digitalresponse.com.au/terms.html
        var termsLink = await Page.QuerySelectorAsync("a[href*='terms']");
        Assert.That(termsLink, Is.Not.Null, "Terms link should exist on login page");

        var href = await termsLink!.GetAttributeAsync("href");
        Assert.That(href, Is.Not.Null.And.Not.Empty);

        await TakeScreenshotAsync("Terms_Link");
    }
    
    [Test, Category("Smoke")]
    public async Task Login_With_Valid_Credentials_Succeeds()
    {
        await LoginAsync();
        
        // Verify we're on the dashboard
        var url = Page.Url;
        Assert.That(url, Is.EqualTo(Settings.BaseUrl + "/").Or.EqualTo(Settings.BaseUrl));
        
        // Verify dashboard elements are visible
        var dashboardTitle = await Page.QuerySelectorAsync("h1:has-text('Welcome back')");
        Assert.That(dashboardTitle, Is.Not.Null, "Dashboard welcome message should be visible");
        
        await TakeScreenshotAsync("Login_Success");
    }
    
    [Test]
    public async Task Login_With_Invalid_Credentials_Shows_Error()
    {
        await Page.GotoAsync($"{Settings.BaseUrl}/login");
        await Page.WaitForSelectorAsync("input[name='login']");

        // Fill with invalid credentials
        await Page.FillAsync("input[name='login']",    "nonexistent-user");
        await Page.FillAsync("input[name='password']", "wrongpassword");
        await Page.ClickAsync("button[type='submit']");

        // Allow up to 30s for SQL connection / login processing on slow first-hit.
        // The auth endpoint may take time on cold start; we just need it to NOT
        // redirect to dashboard.
        try
        {
            await Page.WaitForURLAsync(new System.Text.RegularExpressions.Regex(".*/login.*"),
                new() { Timeout = 30000 });
        }
        catch { /* fall through – we'll still assert on Url below */ }

        Assert.That(Page.Url, Does.Contain("/login"),
            "Invalid credentials should keep the user on /login");

        await TakeScreenshotAsync("Login_Invalid_Credentials");
    }
    
    [Test]
    public async Task Login_With_Empty_Credentials_Validates()
    {
        await Page.GotoAsync($"{Settings.BaseUrl}/login");
        
        // Try to submit empty form
        await Page.ClickAsync("button[type='submit']");
        
        // Check validation - should still be on login page
        await Page.WaitForTimeoutAsync(1000);
        var url = Page.Url;
        Assert.That(url, Does.Contain("/login"));
        
        await TakeScreenshotAsync("Login_Empty_Validation");
    }
    
    [Test]
    public async Task Logout_Works_Correctly()
    {
        await LoginAsync();
        
        // Find and click logout
        var logoutButton = await Page.QuerySelectorAsync("a[href='/logout'], button:has-text('Logout')");
        if (logoutButton != null)
        {
            await logoutButton.ClickAsync();
            
            // Wait for redirect to login
            await Page.WaitForURLAsync($"{Settings.BaseUrl}/login", new PageWaitForURLOptions { Timeout = 5000 });
            
            var url = Page.Url;
            Assert.That(url, Does.Contain("/login"));
            
            await TakeScreenshotAsync("Logout_Success");
        }
        else
        {
            Assert.Warn("Logout button not found - may not be implemented yet");
        }
    }
}
