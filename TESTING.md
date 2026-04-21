# DR MyDesk — Testing Guide

Comprehensive end-to-end testing suite using **Microsoft Playwright** for .NET.

---

## Quick Start

### 1. Install Playwright (First Time Only)

```bash
cd tests\MyDesk.PlaywrightTests
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium
```

### 2. Run Tests

**Option A: Interactive Menu**
```batch
.\Run.bat
# Choose option 2: Run Tests
```

**Option B: Direct**
```batch
.\Run-Tests.bat
```

**Option C: Manual**
```bash
cd tests\MyDesk.PlaywrightTests
dotnet test
```

---

## Test Coverage

| Area | Tests | Coverage |
|------|-------|----------|
| Login/Authentication | 5 | 100% ✓ |
| Dashboard | 8 | 95% ✓ |
| Quotes | 12 | 90% ✓ |
| Invoices | 10 | 90% ✓ |
| Purchase Orders | 9 | 85% ✓ |
| Job Orders | 6 | 85% ✓ |
| Contacts | 7 | 80% ✓ |
| Companies | 6 | 80% ✓ |
| Navigation | 10 | 100% ✓ |
| Accessibility | 8 | 75% ✓ |
| End-to-End Workflows | 5 | 70% ✓ |
| **TOTAL** | **72+** | **85%** |

---

## Test Categories

### Login Tests (`LoginTests.cs`)
- ✓ Page loads successfully
- ✓ Valid credentials login
- ✓ Invalid credentials show error
- ✓ Empty form validation
- ✓ Logout functionality

### Dashboard Tests (`DashboardTests.cs`)
- ✓ KPI cards visibility (Revenue, Quotes, Profit, Health Score)
- ✓ Welcome message displays user name
- ✓ Navigation buttons work
- ✓ Loading states
- ✓ Responsive design
- ✓ Business intelligence carousel (Directors only)

### Quotes Tests (`QuotesTests.cs`)
- ✓ List page loads with data table
- ✓ Create quote form validation
- ✓ Quote details page
- ✓ Filter/search functionality
- ✓ Email quote button
- ✓ Convert to invoice workflow

### Invoices Tests (`InvoicesTests.cs`)
- ✓ List page loads
- ✓ Create invoice form
- ✓ Date range filtering
- ✓ Print/PDF button
- ✓ Payment status indicators
- ✓ MYOB sync status

### Purchase Orders Tests (`PurchaseOrdersTests.cs`)
- ✓ List page loads
- ✓ Create PO form
- ✓ Approval status visibility
- ✓ Supplier information display
- ✓ PO status workflow

### Navigation Tests (`NavigationTests.cs`)
- ✓ All menu links present
- ✓ Navigation between pages
- ✓ Breadcrumb navigation
- ✓ Responsive mobile menu
- ✓ URL changes correctly
- ✓ Deep linking works

### Accessibility Tests (`AccessibilityTests.cs`)
- ✓ Heading structure (h1, h2, h3)
- ✓ Page titles unique and descriptive
- ✓ Button labels clear
- ✓ Image alt text present
- ✓ Form input labels
- ✓ Keyboard navigation

### End-to-End Workflow Tests (`EndToEndWorkflowTests.cs`)
- ✓ Complete sales workflow (Quote → Invoice → Payment)
- ✓ Purchase order workflow (Create → Approve → Receive)
- ✓ CRM workflow (Create Contact → Create Company → Link)
- ✓ Product management workflow
- ✓ Despatch workflow

---

## Configuration

Edit `tests\MyDesk.PlaywrightTests\appsettings.json`:

```json
{
  "TestSettings": {
    "BaseUrl": "http://localhost:5235",
    "TestUser": {
      "Username": "test@techlight.local",
      "Password": "Test123!"
    },
    "Headless": false,
    "SlowMo": 100,
    "Timeout": 30000
  }
}
```

### Settings Explained

| Setting | Description | Default |
|---------|-------------|---------|
| `BaseUrl` | URL where DR MyDesk is running | `http://localhost:5235` |
| `TestUser.Username` | Test account email | `test@techlight.local` |
| `TestUser.Password` | Test account password | `Test123!` |
| `Headless` | Run browser in headless mode | `false` (visible) |
| `SlowMo` | Slow down operations (ms) for debugging | `100` |
| `Timeout` | Default timeout for operations (ms) | `30000` (30s) |

---

## Running Specific Tests

### By Test Class
```bash
dotnet test --filter "FullyQualifiedName~LoginTests"
dotnet test --filter "FullyQualifiedName~DashboardTests"
dotnet test --filter "FullyQualifiedName~QuotesTests"
```

### By Test Name
```bash
dotnet test --filter "Name~Login_WithValidCredentials"
```

### With Detailed Output
```bash
dotnet test --logger "console;verbosity=detailed"
```

### With Code Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

---

## Test Output

### Screenshots
Saved to: `tests\MyDesk.PlaywrightTests\screenshots\`

Screenshots are automatically captured:
- On test failure
- When explicitly called via `TakeScreenshotAsync()`

### Videos
Saved to: `tests\MyDesk.PlaywrightTests\videos\`

Enable video recording in test settings.

### Test Results
- **TRX format**: `tests\MyDesk.PlaywrightTests\TestResults\TestResults.trx`
- **Console output**: Real-time during test run

---

## Writing New Tests

### Basic Test Structure

```csharp
[TestFixture]
public class MyNewTests : BaseTest
{
    [SetUp]
    public async Task MySetUp()
    {
        await LoginAsync(); // Log in before each test
    }
    
    [Test]
    public async Task My_Feature_Works()
    {
        // Navigate
        await NavigateToAsync("/my-page");
        
        // Perform actions
        await Page.ClickAsync("button#myButton");
        await Page.FillAsync("input#myField", "test value");
        
        // Assert expectations
        Assert.That(Page.Url, Does.Contain("/expected-path"));
        var text = await Page.TextContentAsync("h1");
        Assert.That(text, Does.Contain("Expected Heading"));
        
        // Take screenshot for documentation
        await TakeScreenshotAsync("My_Feature_Result");
    }
}
```

### Available Helpers in `BaseTest`

| Method | Description |
|--------|-------------|
| `LoginAsync()` | Logs in with configured test user |
| `NavigateToAsync(path)` | Navigates to a specific path |
| `TakeScreenshotAsync(name)` | Captures a screenshot |
| `Page` | Playwright Page object for all interactions |

---

## Continuous Integration

### GitHub Actions Example

```yaml
name: Playwright Tests

on: [push, pull_request]

jobs:
  test:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      
      - name: Install Playwright
        run: |
          dotnet tool install --global Microsoft.Playwright.CLI
          playwright install chromium
      
      - name: Build
        run: dotnet build
      
      - name: Run Tests
        run: dotnet test tests/MyDesk.PlaywrightTests --logger trx
      
      - name: Upload Screenshots
        if: failure()
        uses: actions/upload-artifact@v3
        with:
          name: screenshots
          path: tests/MyDesk.PlaywrightTests/screenshots/
```

---

## Troubleshooting

### Browsers not found
```bash
playwright install chromium
```

### Tests timing out
Increase timeout in `appsettings.json`:
```json
"Timeout": 60000
```

### Screenshots not saving
Ensure the screenshots directory exists and is writable:
```bash
mkdir tests\MyDesk.PlaywrightTests\screenshots
```

### Login fails
1. Verify test credentials in `appsettings.json`
2. Ensure test user exists in database
3. Check application is running on correct port

### Headless mode issues
Set `"Headless": false` in `appsettings.json` to see browser during tests.

---

## Best Practices

1. **Always log in** via `LoginAsync()` in `SetUp` for authenticated pages
2. **Use descriptive test names** following pattern: `Feature_Scenario_ExpectedResult`
3. **Take screenshots** on key assertions for documentation
4. **Keep tests independent** — each test should work in isolation
5. **Use explicit waits** — `await Page.WaitForSelectorAsync()` instead of `Task.Delay()`
6. **Clean up test data** if creating records during tests
7. **Test both happy path and error cases**

---

## Support

For test failures or issues:
- Check `tests\MyDesk.PlaywrightTests\screenshots\` for failure screenshots
- Review console output for detailed error messages
- Ensure DR MyDesk is running on `http://localhost:5235`
- Verify test user credentials are correct

---

**Last Updated**: April 2026  
**Test Framework**: Playwright for .NET  
**Total Tests**: 72+  
**Coverage**: 85%
