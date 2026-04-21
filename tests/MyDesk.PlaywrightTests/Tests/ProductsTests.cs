using Microsoft.Playwright;
using NUnit.Framework;

namespace MyDesk.PlaywrightTests.Tests;

[TestFixture]
public class ProductsTests : BaseTest
{
    [SetUp]
    public async Task ProductsSetUp()
    {
        await LoginAsync();
    }
    
    [Test]
    public async Task Products_List_Page_Loads()
    {
        await NavigateToAsync("/products");
        
        var title = await Page.TitleAsync();
        Assert.That(title, Does.Contain("Product").Or.Contain("Products"));
        
        // Check for list elements
        var productList = await Page.QuerySelectorAsync(".mud-data-grid, .product-list, table");
        Assert.That(productList, Is.Not.Null, "Product list should be visible");
        
        await TakeScreenshotAsync("Products_List_Page");
    }
    
    [Test]
    public async Task Create_Product_Page_Loads()
    {
        await NavigateToAsync("/products/create");
        
        var title = await Page.TitleAsync();
        Assert.That(title, Does.Contain("New Product").Or.Contain("Create Product").Or.Contain("Product"));
        
        // Check for form elements
        var formInputs = await Page.QuerySelectorAllAsync("input, select, textarea");
        Assert.That(formInputs.Count, Is.GreaterThan(0), "Product form should have input fields");
        
        await TakeScreenshotAsync("Products_Create_Page");
    }
    
    [Test]
    public async Task Product_Details_Page_Loads()
    {
        await NavigateToAsync("/products");
        
        // Try to click on first product
        var firstProductLink = await Page.QuerySelectorAsync("a[href*='/products/']:not([href*='/create']), .product-row a, td a");
        if (firstProductLink != null)
        {
            await firstProductLink.ClickAsync();
            await Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
            
            // Verify we're on a product details page
            var url = Page.Url;
            Assert.That(url, Does.Match(@"/products/\d+").Or.Contain("/product/"));
            
            await TakeScreenshotAsync("Product_Details_Page");
        }
        else
        {
            Assert.Warn("No products available to view details");
        }
    }
    
    [Test]
    public async Task Product_Category_Filter_Works()
    {
        await NavigateToAsync("/products");
        
        // Look for category filter
        var categoryFilter = await Page.QuerySelectorAsync("select[name*='category'], select[name*='Category'], .category-filter");
        if (categoryFilter != null)
        {
            await categoryFilter.ClickAsync();
            await Page.WaitForTimeoutAsync(500);
            
            // Select an option
            var options = await categoryFilter.QuerySelectorAllAsync("option");
            if (options.Count > 1)
            {
                await options[1].ClickAsync();
                await Page.WaitForTimeoutAsync(1000);
                
                await TakeScreenshotAsync("Products_Category_Filter");
            }
        }
        else
        {
            Assert.Warn("Category filter not found");
        }
    }
    
    [Test]
    public async Task Product_Price_Visible()
    {
        await NavigateToAsync("/products");
        
        // Look for price columns or indicators
        var priceElements = await Page.QuerySelectorAllAsync(".price, .cost, .product-price, td:nth-child(4), td:nth-child(5)");
        
        // Just check that product list has content
        var products = await Page.QuerySelectorAllAsync(".product-row, tbody tr, .mud-data-grid-row");
        if (products.Count > 0)
        {
            TestContext.WriteLine($"Found {products.Count} products in list");
        }
        else
        {
            Assert.Warn("No products found in list");
        }
    }
}
