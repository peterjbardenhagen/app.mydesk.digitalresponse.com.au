# MyDesk Playwright Tests

Comprehensive end-to-end testing suite for MyDesk application using Microsoft Playwright.

## Quick Start

### 1. Install Playwright Browsers (First Time Only)
```bash
cd tests\MyDesk.PlaywrightTests
dotnet tool install --global Microsoft.Playwright.CLI
playwright install chromium
```

### 2. Run Tests Using Batch File
```bash
.\Run-Tests.bat
```

This provides an interactive menu to run specific test categories.

### 3. Or Run Tests Using Dotnet CLI
```bash
# Run all tests
dotnet test tests/MyDesk.PlaywrightTests/MyDesk.PlaywrightTests.csproj

# Run specific test class
dotnet test tests/MyDesk.PlaywrightTests --filter "FullyQualifiedName~LoginTests"

# Run with detailed output
dotnet test tests/MyDesk.PlaywrightTests --logger "console;verbosity=detailed"

# Run with coverage
dotnet test tests/MyDesk.PlaywrightTests --collect:"XPlat Code Coverage"
```

## Test Categories

### Login Tests (`LoginTests.cs`)
- Page loads successfully
- Valid credentials login
- Invalid credentials show error
- Empty form validation
- Logout functionality

### Dashboard Tests (`DashboardTests.cs`)
- KPI cards visibility
- Welcome message
- Navigation buttons work
- Loading states
- Responsive design

### Quotes Tests (`QuotesTests.cs`)
- List page loads
- Create quote form
- Validation on empty submit
- Quote details page
- Filter/search functionality

### Invoices Tests (`InvoicesTests.cs`)
- List page loads
- Create invoice form
- Date range filtering
- Print button availability
- Payment status indicators

### Purchase Orders Tests (`PurchaseOrdersTests.cs`)
- List page loads
- Create PO form
- Approval status visibility
- Supplier information display

### Job Orders Tests (`JobOrdersTests.cs`)
- List page loads
- Status filtering
- Workflow status display

### Contacts Tests (`ContactsTests.cs`)
- List page loads
- Create contact form
- Search functionality
- Contact details page

### Companies Tests (`CompaniesTests.cs`)
- List page loads
- Create company form
- Company details page

### Navigation Tests (`NavigationTests.cs`)
- All menu links present
- Navigation between pages
- Breadcrumb navigation
- Responsive mobile menu
- URL changes correctly

### Profile/Settings Tests (`ProfileTests.cs`)
- Profile page loads
- Settings page loads
- Admin setup page loads

### Accessibility Tests (`AccessibilityTests.cs`)
- Heading structure
- Page titles
- Button labels
- Image alt text
- Form input labels
- Keyboard navigation

### End-to-End Workflow Tests (`EndToEndWorkflowTests.cs`)
- Complete sales workflow (Quote → Invoice)
- Purchase order workflow
- CRM workflow (Create Contact & Company)

## Configuration

Edit `appsettings.json` to configure test settings:

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

### Settings Explanation

| Setting | Description |
|---------|-------------|
| `BaseUrl` | URL where MyDesk is running |
| `TestUser.Username` | Test account email |
| `TestUser.Password` | Test account password |
| `Headless` | Run browser in headless mode (true/false) |
| `SlowMo` | Slow down operations by N milliseconds (for debugging) |
| `Timeout` | Default timeout for operations in milliseconds |

## Test Output

### Screenshots
Screenshots are saved to `tests/MyDesk.PlaywrightTests/screenshots/` during test execution.

### Videos
Test execution videos are saved to `tests/MyDesk.PlaywrightTests/videos/` (when video recording is enabled).

### Test Results
- TRX format: `tests/MyDesk.PlaywrightTests/TestResults/TestResults.trx`
- Console output: Detailed logs during test run

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
    public async Task My_Test()
    {
        await NavigateToAsync("/my-page");
        
        // Perform actions
        await Page.ClickAsync("button#myButton");
        
        // Assert expectations
        Assert.That(Page.Url, Does.Contain("/expected-path"));
        
        // Take screenshot for documentation
        await TakeScreenshotAsync("My_Test_Result");
    }
}
```

### Available Helpers in BaseTest

| Method | Description |
|--------|-------------|
| `LoginAsync()` | Logs in with configured test user |
| `NavigateToAsync(path)` | Navigates to a specific path |
| `TakeScreenshotAsync(name)` | Captures a screenshot |

## Continuous Integration

For CI/CD pipelines:

```yaml
# Example GitHub Actions
- name: Run Playwright Tests
  run: |
    dotnet build
    dotnet test tests/MyDesk.PlaywrightTests --logger trx
```

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
Ensure the screenshots directory exists and is writable.

### Login fails
Verify test credentials in `appsettings.json` match a valid user in the database.

## Coverage Goals

| Area | Target Coverage | Current |
|------|----------------|---------|
| Login/Authentication | 100% | ✓ |
| Dashboard | 95% | ✓ |
| Quotes | 90% | ✓ |
| Invoices | 90% | ✓ |
| Purchase Orders | 85% | ✓ |
| Job Orders | 85% | ✓ |
| Contacts | 80% | ✓ |
| Companies | 80% | ✓ |
| Navigation | 100% | ✓ |
| Accessibility | 75% | ✓ |
| End-to-End Workflows | 70% | ✓ |
