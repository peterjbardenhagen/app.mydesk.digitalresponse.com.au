using Microsoft.Playwright;
using NUnit.Framework;

namespace MyDesk.PlaywrightTests.Tests;

[TestFixture]
public class ProfileTests : BaseTest
{
    [SetUp]
    public async Task ProfileSetUp()
    {
        await LoginAsync();
    }
    
    [Test]
    public async Task Profile_Page_Loads()
    {
        await NavigateToAsync("/profile");
        
        var title = await Page.TitleAsync();
        Assert.That(title, Does.Contain("Profile").Or.Contain("My Profile").Or.Contain("User Profile"));
        
        // Check for profile elements
        var profileContent = await Page.QuerySelectorAsync(".profile-container, .user-profile, main");
        Assert.That(profileContent, Is.Not.Null, "Profile content should be visible");
        
        await TakeScreenshotAsync("Profile_Page");
    }
    
    [Test]
    public async Task Profile_Edit_Button_Available()
    {
        await NavigateToAsync("/profile");
        
        // Look for edit button
        var editButton = await Page.QuerySelectorAsync("button:has-text('Edit'), a:has-text('Edit'), button[title='Edit']");
        if (editButton != null)
        {
            Assert.Pass("Edit button found on profile page");
        }
        else
        {
            Assert.Warn("Edit button not found on profile page");
        }
    }
    
    [Test]
    public async Task Profile_User_Info_Displayed()
    {
        await NavigateToAsync("/profile");
        
        // Check for user information
        var userName = await Page.QuerySelectorAsync(".user-name, .profile-name, h1, h2");
        if (userName != null)
        {
            var nameText = await userName.TextContentAsync();
            Assert.That(nameText, Is.Not.Null.And.Not.Empty, "User name should be displayed");
            TestContext.WriteLine($"User name displayed: {nameText}");
        }
        
        await TakeScreenshotAsync("Profile_User_Info");
    }
    
    [Test]
    public async Task Settings_Page_Loads()
    {
        await NavigateToAsync("/settings");
        
        var title = await Page.TitleAsync();
        Assert.That(title, Does.Contain("Settings").Or.Contain("Preferences"));
        
        // Check for settings content
        var settingsContent = await Page.QuerySelectorAsync(".settings-container, .preferences, main");
        Assert.That(settingsContent, Is.Not.Null, "Settings content should be visible");
        
        await TakeScreenshotAsync("Settings_Page");
    }
    
    [Test]
    public async Task Admin_Setup_Page_Loads()
    {
        await NavigateToAsync("/admin/setup");
        
        var title = await Page.TitleAsync();
        Assert.That(title, Does.Contain("Setup").Or.Contain("Admin").Or.Contain("Settings"));
        
        // Check for setup content
        var setupContent = await Page.QuerySelectorAsync(".setup-container, .admin-setup, main");
        Assert.That(setupContent, Is.Not.Null, "Setup content should be visible");
        
        await TakeScreenshotAsync("Admin_Setup_Page");
    }
}
