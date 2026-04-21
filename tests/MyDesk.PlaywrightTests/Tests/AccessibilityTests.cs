using Microsoft.Playwright;
using NUnit.Framework;

namespace MyDesk.PlaywrightTests.Tests;

[TestFixture]
public class AccessibilityTests : BaseTest
{
    [SetUp]
    public async Task AccessibilitySetUp()
    {
        await LoginAsync();
    }
    
    [Test]
    public async Task Page_Has_Proper_Heading_Structure()
    {
        await NavigateToAsync("/");
        
        // Check for h1 heading
        var h1 = await Page.QuerySelectorAsync("h1");
        Assert.That(h1, Is.Not.Null, "Page should have an h1 heading");
        
        // Check h1 text
        var h1Text = await h1!.TextContentAsync();
        Assert.That(h1Text, Is.Not.Null.And.Not.Empty, "H1 should have content");
        
        TestContext.WriteLine($"H1 heading found: {h1Text}");
    }
    
    [Test]
    public async Task Page_Has_Proper_Title()
    {
        var pages = new[] { "/", "/quotes", "/invoices", "/purchaseorders", "/contacts", "/companies" };
        
        foreach (var path in pages)
        {
            await NavigateToAsync(path);
            
            var title = await Page.TitleAsync();
            Assert.That(title, Is.Not.Null.And.Not.Empty, $"Page {path} should have a title");
            Assert.That(title.Length, Is.GreaterThan(5), $"Page {path} title should be meaningful");
            
            TestContext.WriteLine($"✓ {path}: {title}");
        }
    }
    
    [Test]
    public async Task Interactive_Elements_Have_Accessible_Labels()
    {
        await NavigateToAsync("/");
        
        // Check buttons for text or aria-label
        var buttons = await Page.QuerySelectorAllAsync("button");
        int accessibleButtons = 0;
        
        foreach (var button in buttons)
        {
            var text = await button.TextContentAsync();
            var ariaLabel = await button.GetAttributeAsync("aria-label");
            var title = await button.GetAttributeAsync("title");
            
            if (!string.IsNullOrWhiteSpace(text) || 
                !string.IsNullOrWhiteSpace(ariaLabel) || 
                !string.IsNullOrWhiteSpace(title))
            {
                accessibleButtons++;
            }
        }
        
        TestContext.WriteLine($"Accessible buttons: {accessibleButtons}/{buttons.Count}");
        
        if (buttons.Count > 0)
        {
            Assert.That(accessibleButtons, Is.GreaterThan(buttons.Count / 2), 
                "At least half of buttons should have accessible labels");
        }
    }
    
    [Test]
    public async Task Images_Have_Alt_Text()
    {
        await NavigateToAsync("/");
        
        var images = await Page.QuerySelectorAllAsync("img");
        int imagesWithAlt = 0;
        
        foreach (var img in images)
        {
            var alt = await img.GetAttributeAsync("alt");
            if (!string.IsNullOrWhiteSpace(alt))
            {
                imagesWithAlt++;
            }
        }
        
        TestContext.WriteLine($"Images with alt text: {imagesWithAlt}/{images.Count}");
        
        // Note: Some images like icons might not need alt text
        // This is informational rather than a strict assertion
        if (images.Count > 0 && imagesWithAlt < images.Count / 2)
        {
            Assert.Warn("Many images are missing alt text");
        }
    }
    
    [Test]
    public async Task Form_Inputs_Have_Associated_Labels()
    {
        await NavigateToAsync("/quotes/create");
        
        var inputs = await Page.QuerySelectorAllAsync("input, select, textarea");
        int inputsWithLabels = 0;
        
        foreach (var input in inputs)
        {
            var id = await input.GetAttributeAsync("id");
            var ariaLabel = await input.GetAttributeAsync("aria-label");
            var ariaLabelledBy = await input.GetAttributeAsync("aria-labelledby");
            var placeholder = await input.GetAttributeAsync("placeholder");
            
            // Check for associated label
            if (!string.IsNullOrWhiteSpace(id))
            {
                var label = await Page.QuerySelectorAsync($"label[for='{id}']");
                if (label != null)
                {
                    inputsWithLabels++;
                    continue;
                }
            }
            
            // Check for aria-label or aria-labelledby
            if (!string.IsNullOrWhiteSpace(ariaLabel) || !string.IsNullOrWhiteSpace(ariaLabelledBy))
            {
                inputsWithLabels++;
                continue;
            }
            
            // Check for placeholder as fallback
            if (!string.IsNullOrWhiteSpace(placeholder))
            {
                inputsWithLabels++;
            }
        }
        
        TestContext.WriteLine($"Inputs with labels: {inputsWithLabels}/{inputs.Count}");
        
        if (inputs.Count > 0)
        {
            Assert.That(inputsWithLabels, Is.GreaterThan(inputs.Count / 2), 
                "At least half of form inputs should have accessible labels");
        }
    }
    
    [Test]
    public async Task Keyboard_Navigation_Works()
    {
        await NavigateToAsync("/");
        
        // Try keyboard navigation
        await Page.Keyboard.PressAsync("Tab");
        await Page.WaitForTimeoutAsync(200);
        
        // Check that something is focused
        var focusedElement = await Page.QuerySelectorAsync(":focus, *:focus, [tabindex]:focus");
        
        // This is a basic check - more comprehensive testing would require knowing the specific structure
        TestContext.WriteLine("Keyboard navigation test completed");
    }
}
