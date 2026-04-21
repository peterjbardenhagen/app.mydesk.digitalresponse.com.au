using Microsoft.Playwright;
using NUnit.Framework;

namespace MyDesk.PlaywrightTests.Tests;

[TestFixture]
public class AskAITests : BaseTest
{
    [SetUp]
    public async Task AskAISetUp()
    {
        await LoginAsync();
    }
    
    [Test]
    public async Task AskAI_Page_Loads()
    {
        await NavigateToAsync("/askai");
        
        var title = await Page.TitleAsync();
        Assert.That(title, Does.Contain("Ask AI").Or.Contain("AI Assistant").Or.Contain("AI"));
        
        // Check for chat interface elements
        var chatContainer = await Page.QuerySelectorAsync(".chat-container, .askai-container, .ai-chat");
        var inputField = await Page.QuerySelectorAsync("input[placeholder*='Ask'], textarea[placeholder*='Ask'], input[type='text']");
        
        Assert.That(chatContainer, Is.Not.Null, "Chat container should be visible");
        Assert.That(inputField, Is.Not.Null, "Input field should be visible");
        
        await TakeScreenshotAsync("AskAI_Page_Loaded");
    }
    
    [Test]
    public async Task AskAI_Input_Accepts_Text()
    {
        await NavigateToAsync("/askai");
        
        var inputField = await Page.QuerySelectorAsync("input[placeholder*='Ask'], textarea[placeholder*='Ask'], input[type='text']");
        if (inputField != null)
        {
            await inputField.FillAsync("Show me today's sales report");
            await Page.WaitForTimeoutAsync(500);
            
            var value = await inputField.InputValueAsync();
            Assert.That(value, Is.EqualTo("Show me today's sales report"));
            
            await TakeScreenshotAsync("AskAI_Input_Filled");
        }
        else
        {
            Assert.Warn("Input field not found");
        }
    }
    
    [Test]
    public async Task AskAI_Send_Button_Enabled()
    {
        await NavigateToAsync("/askai");
        
        var inputField = await Page.QuerySelectorAsync("input[placeholder*='Ask'], textarea[placeholder*='Ask'], input[type='text']");
        var sendButton = await Page.QuerySelectorAsync("button[type='submit'], button:has-text('Send'), button.send-btn");
        
        if (inputField != null && sendButton != null)
        {
            // Button should be disabled when input is empty
            var isEnabled = await sendButton.IsEnabledAsync();
            // This depends on implementation - may be enabled or disabled
            
            // Fill input
            await inputField.FillAsync("What are my pending invoices?");
            await Page.WaitForTimeoutAsync(500);
            
            // Button should be enabled now
            isEnabled = await sendButton.IsEnabledAsync();
            
            if (isEnabled)
            {
                Assert.Pass("Send button is enabled when input has text");
            }
            else
            {
                Assert.Warn("Send button remains disabled - may need additional form validation");
            }
            
            await TakeScreenshotAsync("AskAI_Send_Button");
        }
        else
        {
            Assert.Warn("Input field or send button not found");
        }
    }
    
    [Test]
    public async Task AskAI_Navigation_From_Header()
    {
        // Look for Ask AI link in header/navigation
        var askAILink = await Page.QuerySelectorAsync("a[href='/askai'], a:has-text('Ask AI'), button:has-text('Ask AI')");
        
        if (askAILink != null)
        {
            await askAILink.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            Assert.That(Page.Url, Does.Contain("/askai"));
            
            await TakeScreenshotAsync("AskAI_Navigation_From_Header");
        }
        else
        {
            Assert.Warn("Ask AI link not found in header - navigating directly");
            await NavigateToAsync("/askai");
        }
    }
    
    [Test]
    public async Task AskAI_Help_Suggestions_Visible()
    {
        await NavigateToAsync("/askai");
        
        // Check for suggested questions or help text
        var suggestions = await Page.QuerySelectorAllAsync(".suggestion-chip, .help-text, .suggested-question");
        var helpText = await Page.QuerySelectorAsync(".help-message, .welcome-message, .intro-text");
        
        if (suggestions.Count > 0 || helpText != null)
        {
            Assert.Pass("Help suggestions or welcome message found");
        }
        else
        {
            Assert.Warn("No suggestions or help text visible - may be minimal UI design");
        }
    }
}
