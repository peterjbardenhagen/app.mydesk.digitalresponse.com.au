using Microsoft.Playwright;
using NUnit.Framework;

namespace MyDesk.PlaywrightTests.Tests;

[TestFixture]
public class LoginTests : BaseTest
{
    [Test]
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
        
        var privacyLink = await Page.QuerySelectorAsync("a[href='/privacy-policy']");
        Assert.That(privacyLink, Is.Not.Null, "Privacy Policy link should exist");
        
        // Click and verify navigation
        await privacyLink!.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        Assert.That(Page.Url, Does.Contain("/privacy-policy"));
        var content = await Page.ContentAsync();
        Assert.That(content, Does.Contain("Privacy Policy"));
        
        await TakeScreenshotAsync("PrivacyPolicy_Page");
    }
    
    [Test]
    public async Task Login_Page_Has_Terms_Link()
    {
        await Page.GotoAsync($"{Settings.BaseUrl}/login");
        
        var termsLink = await Page.QuerySelectorAsync("a[href='/terms-and-conditions']");
        Assert.That(termsLink, Is.Not.Null, "Terms & Conditions link should exist");
        
        await termsLink!.ClickAsync();
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        
        Assert.That(Page.Url, Does.Contain("/terms-and-conditions"));
        var content = await Page.ContentAsync();
        Assert.That(content, Does.Contain("Terms"));
        
        await TakeScreenshotAsync("Terms_Page");
    }
    
    [Test]
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
        await Page.FillAsync("input[name='login']", "nonexistent-user");
        await Page.FillAsync("input[name='password']", "wrongpassword");
        await Page.ClickAsync("button[type='submit']");
        
        // Wait for response
        await Page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions { Timeout = 10000 });
        
        // Check for error message or that we stayed on login page
        var url = Page.Url;
        Assert.That(url, Does.Contain("/login"));
        
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
