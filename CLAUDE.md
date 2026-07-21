# MyDesk Development Guide (CLAUDE.md)

**Version:** 1.0  
**Last Updated:** July 2026  
**Target Audience:** Developers, engineers, implementation teams

---

## Quick Start

Welcome to MyDesk! This guide will help you set up your development environment and understand how to contribute to the project.

### Prerequisites

- **.NET 10 SDK** ([download](https://dotnet.microsoft.com/download/dotnet/10.0))
- **SQL Server 2022 Express** or SQL Server Developer Edition
- **Git** ([download](https://git-scm.com/))
- **Visual Studio 2022 or VS Code** (with C# extension)
- **Azure CLI** (optional, for cloud deployment)

### Initial Setup (5 minutes)

**1. Clone the repository**
```bash
git clone https://github.com/peterjbardenhagen/app.mydesk.digitalresponse.com.au.git
cd app.mydesk.digitalresponse.com.au
```

**2. Restore dependencies**
```bash
dotnet restore
```

**3. Set up local database**
```bash
# Run all migration scripts in order
sqlcmd -S (localdb)\mssqllocaldb -i src/Deployment/Migration/001_initial_schema.sql
sqlcmd -S (localdb)\mssqllocaldb -i src/Deployment/Migration/002_users_and_auth.sql
sqlcmd -S (localdb)\mssqllocaldb -i src/Deployment/Migration/003_tenants.sql
# ... (continue for all migrations)
```

**4. Update connection string**
Edit `src/MyDesk.Web/appsettings.Development.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=MyDesk;Integrated Security=true;"
  }
}
```

**5. Run the application**
```bash
dotnet run --project src/MyDesk.Web
```

Application running at: https://localhost:7000

---

## Documentation Map

Before diving into code, understand the product architecture:

### 1. **For Understanding What We're Building**
→ Start with **[PRODUCT-REQUIREMENTS.md](./PRODUCT-REQUIREMENTS.md)**
- Describes all features (phases 1-6)
- Includes user personas and success metrics
- Lists acceptance criteria for each feature
- **Read time:** 20 minutes

### 2. **For Understanding How We're Positioning It**
→ Read **[PRODUCT-STRATEGY.md](./PRODUCT-STRATEGY.md)**
- Market opportunity and competitive positioning
- Go-to-market strategy and pricing
- 5-year roadmap and financial projections
- Risk analysis and mitigation
- **Read time:** 15 minutes

### 3. **For Understanding the Enterprise Requirements**
→ Read **[ENTERPRISE-ARCHITECTURE.md](./docs/ENTERPRISE-ARCHITECTURE.md)**
- Regulatory compliance requirements (ISO 27001, SOC 2, Sarbanes-Oxley)
- Security principles and implementation
- Disaster recovery and performance requirements
- Audit trail and compliance controls
- **Read time:** 25 minutes

### 4. **For Understanding the Technical Design**
→ Read **[SOLUTION-ARCHITECTURE.md](./docs/SOLUTION-ARCHITECTURE.md)**
- System architecture and design patterns
- Multi-tenancy model and enforcement
- API design and error handling
- Database schema layers
- Development workflow and build pipeline
- **Read time:** 30 minutes

### 5. **For Working on Specific Features**
→ Refer to **[agents.md](./agents.md)**
- 15 specialized implementation agents
- Which agent owns which feature
- Code locations and acceptance criteria
- Agent collaboration matrix
- **Read time:** 20 minutes (relevant sections only)

---

## Project Structure

```
MyDesk.sln
├── src/
│   ├── MyDesk.Web/                    # Main application
│   │   ├── Program.cs                 # Startup, DI configuration
│   │   ├── appsettings.json          # Production settings
│   │   ├── appsettings.Development.json  # Dev settings
│   │   ├── Services/                  # Business logic
│   │   │   ├── DatabaseService.cs     # SQL query execution
│   │   │   ├── AuthenticationService.cs
│   │   │   ├── ExpenseService.cs
│   │   │   ├── ApprovalService.cs
│   │   │   ├── NotificationService.cs
│   │   │   ├── PhotoProcessingService.cs
│   │   │   └── ...
│   │   ├── Controllers/               # API endpoints
│   │   │   ├── AuthController.cs
│   │   │   ├── ExpenseController.cs
│   │   │   ├── ApprovalController.cs
│   │   │   ├── NotificationController.cs
│   │   │   └── ...
│   │   ├── Components/                # Blazor components
│   │   │   ├── Layout/
│   │   │   │   ├── MainLayout.razor
│   │   │   │   └── NavMenu.razor
│   │   │   ├── Pages/                 # Page components
│   │   │   │   ├── Expenses.razor
│   │   │   │   ├── Approvals.razor
│   │   │   │   ├── Profile.razor
│   │   │   │   └── ...
│   │   │   └── Dialogs/               # Modal dialogs
│   │   │       ├── PhotoUploadDialog.razor
│   │   │       ├── ExpenseReceiptUploadDialog.razor
│   │   │       └── ...
│   │   └── wwwroot/                   # Static files (CSS, JS, images)
│   │       ├── css/
│   │       ├── js/
│   │       └── images/
│   └── Deployment/
│       ├── Migration/                 # Database migration scripts
│       │   ├── 001_initial_schema.sql
│       │   ├── 002_users_and_auth.sql
│       │   └── ...
│       └── Scripts/                   # DevOps scripts (backup, restore)
├── tests/
│   └── MyDesk.Web.Tests/             # Unit & integration tests
│       ├── Services/
│       ├── Controllers/
│       └── Integration/
├── docs/
│   ├── ENTERPRISE-ARCHITECTURE.md     # Enterprise requirements
│   ├── SOLUTION-ARCHITECTURE.md       # Technical design
│   ├── PRODUCT-REQUIREMENTS.md        # Feature specifications
│   ├── PRODUCT-STRATEGY.md            # Go-to-market strategy
│   ├── agents.md                      # Implementation agents
│   └── CLAUDE.md                      # This file
├── .github/
│   └── workflows/                     # CI/CD pipelines
│       └── build-and-deploy.yml
└── README.md                          # Project overview
```

---

## Development Workflow

### 1. Creating a Feature Branch

```bash
# Pull latest main
git checkout main
git pull origin main

# Create feature branch (use descriptive name)
git checkout -b feature/expenses-receipt-ocr

# Or if working on a specific phase:
git checkout -b claude/deploy-mydesk-phase4-teams
```

**Branch Naming Convention:**
- `feature/short-description` - New features
- `fix/short-description` - Bug fixes
- `claude/deploy-mydesk-XXX` - Major deployment branches
- `refactor/short-description` - Code cleanup

### 2. Making Changes

**Workflow:**
1. Edit code in your IDE
2. Run `dotnet build` to compile
3. Run `dotnet test` to verify tests pass
4. Commit with descriptive message:
   ```bash
   git add src/MyDesk.Web/Services/ExpenseService.cs
   git commit -m "feat(expenses): add receipt OCR integration"
   ```

**Commit Message Format:**
```
<type>(<scope>): <description>

<body (optional)>

<footer (optional)>
```

- `type`: feat, fix, docs, style, refactor, test, chore
- `scope`: the area affected (expenses, approvals, notifications, auth, etc.)
- `description`: concise description in present tense

### 3. Before Submitting Pull Request

**Run full test suite:**
```bash
# Build and run tests
dotnet build
dotnet test --configuration Release
```

**Code quality checks:**
```bash
# Check for code style issues (optional)
dotnet format --verify-no-changes
```

**Database migrations:**
```bash
# Ensure any database changes are in Migration/*.sql files
# Test migrations locally
```

### 4. Creating a Pull Request

```bash
git push -u origin feature/expenses-receipt-ocr
```

Then create PR via GitHub UI:
- Use template from `.github/PULL_REQUEST_TEMPLATE.md`
- Link related issues
- Describe what changed and why
- Include screenshots for UI changes
- List testing performed

---

## Agent Development Guidelines (2026-Standard)

### Agentic Architecture Principles

MyDesk uses Orchestrator-Worker agentic patterns. Follow these guidelines when developing agents:

**1. Stateless Worker Design**
```csharp
// ✅ GOOD: Worker is stateless, receives complete payload
public class ApprovalWorkflowWorker
{
    public async Task<ApprovalResult> ProcessAsync(ApprovalRequest request, UserContext context)
    {
        // All state passed in request
        // No instance variables retained between calls
        // Can scale horizontally
    }
}

// ❌ BAD: Worker retains state between invocations
public class ApprovalWorkflowWorker
{
    private ApprovalRequest _currentRequest; // DON'T DO THIS
    
    public async Task<ApprovalResult> ProcessAsync(ApprovalRequest request)
    {
        _currentRequest = request; // Shared state causes bugs
    }
}
```

**2. Structured Hand-Offs Between Workers**
```csharp
// Define clear JSON schema for hand-offs
public class NotificationHandoff
{
    public int[] ApproverIds { get; set; }
    public string EventType { get; set; }
    public Expense Expense { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Worker receiving hand-off validates schema
public async Task SendAsync(NotificationHandoff payload, UserContext context)
{
    if (payload?.ApproverIds?.Length == 0)
        throw new ArgumentException("ApproverIds required");
    
    // Process with confidence
}
```

**3. PII Filtering Before Agent Processing**
```csharp
// Always filter before sending to agents
var filteredDescription = _piiService.FilterPii(expense.Description);
var filteredBody = _piiService.FilterPii(emailBody);

// Then pass to agent
await _notificationWorker.SendAsync(new { Description = filteredDescription }, context);
```

**4. Auth Middleware for All Agent Calls**
```csharp
// Never call agent API directly without auth validation
// Always route through backend middleware

// ✅ GOOD: Backend validates tenant_id, then calls agent
[Authorize]
[HttpPost("expenses/{id}/submit")]
public async Task<IActionResult> SubmitAsync(int id)
{
    var tenantId = User.FindFirst("tenant_id")?.Value;
    var result = await _orchestrator.RouteAsync("expense_submitted", context, payload);
    return Ok(result);
}

// ❌ BAD: Client calls agent API directly
// var result = await _claudeApiClient.InvokeAsync(payload); // NO!
```

**5. Memory Management for Long Conversations**
```csharp
// Use Archivist agent to compress when exceeding token threshold
if (conversationLength > TokenThreshold)
{
    var summary = await _archiverAgent.SummarizeAsync(conversationHistory);
    conversationHistory = new { summary, recentMessages = Last5(conversationHistory) };
}
```

**6. Logging Every Agent Invocation**
```csharp
// Log to ComplianceAuditLog for audit trail
await _auditService.LogAsync("AgentInvoked", "System", new
{
    agent = "ApprovalWorkflowWorker",
    intent = "approval_requested",
    tenantId,
    userId,
    timestamp = DateTime.UtcNow
});
```

### Agentic Patterns Reference

| Pattern | When to Use | Example |
|---------|-------------|---------|
| **Router** | Classify intent into worker type | Determine "approval" vs. "notification" vs. "integration" |
| **Planner-Executor** | Multi-step workflow | Approval chain: validate → route → escalate → notify |
| **Critic/Verifier** | High-stakes validation | Check PII filtering, verify approver permissions, validate output |
| **Summarizer** | Prevent token explosion | Compress audit logs after 10k tokens |

---

## Coding Standards

### C# Code Style

**Follow .NET conventions:**
```csharp
// ✅ GOOD
public async Task<Expense> GetExpenseAsync(int tenantId, int expenseId)
{
    var result = await _db.QueryAsync(
        @"SELECT * FROM Expenses 
          WHERE TenantId = @TenantId AND ExpenseId = @ExpenseId",
        new() { ["TenantId"] = tenantId, ["ExpenseId"] = expenseId });
    
    return MapToExpense(result.Rows[0]);
}

// ❌ BAD
public Expense getExpense(int tenantId, int expenseId)
{
    var result = _db.QueryAsync(
        "SELECT * FROM Expenses WHERE TenantId = " + tenantId);  // SQL injection!
    
    return MapToExpense(result.Rows[0]);
}
```

**Guidelines:**
- PascalCase for public members, camelCase for private
- Use async/await for I/O operations
- Always use parameterized queries (never string concatenation)
- Inject dependencies via constructor
- Minimal comments (code should be self-documenting)
- < 100 lines per method
- < 200 lines per class (split large classes)

### Blazor Component Style

```razor
@* ✅ GOOD *@
@page "/expenses"
@using MyDesk.Web.Services
@inject ExpenseService ExpenseService
@inject IDialogService DialogService

<h1>Expenses</h1>

@if (expenses == null)
{
    <p>Loading...</p>
}
else if (expenses.Count == 0)
{
    <MudAlert Severity="Info">No expenses found</MudAlert>
}
else
{
    <MudDataGrid Items="expenses" Striped="true">
        <PropertyColumn Property="e => e.ExpenseId" Title="ID" />
        <PropertyColumn Property="e => e.Amount" Title="Amount" />
        <TemplateColumn Title="Actions">
            <MudButton OnClick="() => ViewDetails(context)">View</MudButton>
        </TemplateColumn>
    </MudDataGrid>
}

@code {
    private List<Expense>? expenses;

    protected override async Task OnInitializedAsync()
    {
        expenses = await ExpenseService.GetMyExpensesAsync();
    }

    private async Task ViewDetails(Expense expense)
    {
        await DialogService.ShowAsync<ExpenseDetailsDialog>(
            parameters: new() { { "ExpenseId", expense.ExpenseId } });
    }
}
```

**Guidelines:**
- Use @page directive at top
- Inject services via @inject
- Use MudBlazor components (MudDataGrid, MudButton, MudDialog)
- Bind to @bind-Value for two-way binding
- Use EventCallback for parent-child communication
- Keep components < 300 lines
- Responsive: Mobile first, then tablet, then desktop

### SQL Migration Style

```sql
-- ✅ GOOD
-- Migration 020: Expense Receipts
-- Purpose: Track photos and AI extraction results for expense claims
-- Features: Receipt storage, OCR results, extraction audit trail

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ExpenseReceipts')
BEGIN
    CREATE TABLE dbo.ExpenseReceipts (
        ReceiptId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        ExpenseId INT NOT NULL,
        
        -- Photo metadata
        PhotoUrl NVARCHAR(500),
        PhotoUploadedAt DATETIME2,
        PhotoUploadedBy INT,
        
        -- AI extraction results
        SupplierName NVARCHAR(255),
        TransactionDate DATE,
        GrossAmount DECIMAL(12,2),
        GstAmount DECIMAL(12,2),
        ExtractionConfidence DECIMAL(3,2),  -- 0.0 to 1.0
        
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT FK_ExpenseReceipts_Expense FOREIGN KEY (ExpenseId) REFERENCES dbo.Expenses(ExpenseId),
        CONSTRAINT FK_ExpenseReceipts_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        INDEX IX_ExpenseReceipts_TenantId (TenantId),
        INDEX IX_ExpenseReceipts_ExpenseId (ExpenseId)
    );
    PRINT 'Created table: ExpenseReceipts';
END
```

**Guidelines:**
- Include header comment with migration number and purpose
- Use IF NOT EXISTS to make migrations idempotent
- Include TenantId on every table
- Add indexes on WHERE and JOIN columns
- Use CONSTRAINT names for clarity
- PRINT statement to confirm migration ran

---

## Testing Guide

### Unit Tests

```csharp
[TestFixture]
public class ExpenseServiceTests
{
    private ExpenseService _service;
    private MockDatabaseService _mockDb;

    [SetUp]
    public void SetUp()
    {
        _mockDb = new MockDatabaseService();
        _service = new ExpenseService(_mockDb);
    }

    [Test]
    public async Task CreateExpenseAsync_WithValidData_ReturnsExpense()
    {
        // Arrange
        var tenantId = 1;
        var userId = 100;
        var amount = 1250.50m;
        
        // Act
        var result = await _service.CreateExpenseAsync(
            tenantId, userId, amount, "Client meeting", "Meals");
        
        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(amount, result.Amount);
        Assert.AreEqual("Meals", result.Category);
    }

    [Test]
    public async Task CreateExpenseAsync_WithNegativeAmount_ThrowsException()
    {
        // Arrange
        var tenantId = 1;
        var userId = 100;
        var amount = -100m;
        
        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(
            () => _service.CreateExpenseAsync(tenantId, userId, amount, "", ""));
    }
}
```

**Guidelines:**
- Test file per service: `ExpenseService.cs` → `ExpenseServiceTests.cs`
- Use Arrange-Act-Assert pattern
- Test one thing per test
- Use descriptive names: `MethodName_Condition_Expected`
- Aim for >80% code coverage
- Test error cases, not just happy path

### Integration Tests

```csharp
[TestFixture]
public class ExpenseControllerIntegrationTests : BaseIntegrationTest
{
    [Test]
    public async Task POST_CreateExpense_WithValidData_Returns201()
    {
        // Arrange
        var request = new CreateExpenseRequest
        {
            Amount = 1250.50m,
            Description = "Client meeting",
            Category = "Meals"
        };
        
        // Act
        var response = await Client.PostAsJsonAsync("/api/expenses", request);
        
        // Assert
        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        
        var content = await response.Content.ReadAsAsync<ExpenseResponse>();
        Assert.IsNotNull(content);
        Assert.AreEqual(request.Amount, content.Amount);
    }

    [Test]
    public async Task GET_GetExpense_WithDifferentTenant_Returns403()
    {
        // Arrange
        var expenseId = 1;
        var token = GetTokenForTenant(tenantId: 999);  // Different tenant
        
        // Act
        Client.DefaultRequestHeaders.Authorization = 
            new AuthenticationHeaderValue("Bearer", token);
        var response = await Client.GetAsync($"/api/expenses/{expenseId}");
        
        // Assert
        Assert.AreEqual(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
```

**Guidelines:**
- Inherit from `BaseIntegrationTest` which sets up test database
- Test with actual HTTP client
- Test security: verify tenant isolation
- Test error responses (400, 401, 403, 404, 500)
- Clean up test data after each test

---

## Database Development

### Running Migrations

**Manually:**
```bash
# Run all migrations in sequence
for i in {001..050}; do
  if [ -f "src/Deployment/Migration/${i}_*.sql" ]; then
    sqlcmd -S (localdb)\mssqllocaldb -d MyDesk -i "src/Deployment/Migration/${i}_*.sql"
  fi
done
```

**Automatic (via application startup):**
```csharp
// In Program.cs
app.Services.GetRequiredService<DatabaseMigrationService>()
    .RunMigrationsAsync()
    .Wait();
```

### Adding a New Table

**1. Create migration file:**
```
src/Deployment/Migration/035_add_expense_categories.sql
```

**2. Write migration SQL:**
```sql
-- Migration 035: Expense Categories
-- Purpose: Allow tenants to define custom expense categories

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ExpenseCategories')
BEGIN
    CREATE TABLE dbo.ExpenseCategories (
        CategoryId INT PRIMARY KEY IDENTITY(1,1),
        TenantId INT NOT NULL,
        Name NVARCHAR(100) NOT NULL,
        Description NVARCHAR(500),
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        
        CONSTRAINT FK_ExpenseCategories_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
        CONSTRAINT UQ_ExpenseCategories_Name UNIQUE (TenantId, Name),
        INDEX IX_ExpenseCategories_TenantId (TenantId),
        INDEX IX_ExpenseCategories_Active (IsActive)
    );
    PRINT 'Created table: ExpenseCategories';
END
```

**3. Add foreign key to related table:**
```sql
-- In the same migration or new one
ALTER TABLE dbo.Expenses
ADD CategoryId INT,
    CONSTRAINT FK_Expenses_Category 
    FOREIGN KEY (CategoryId) REFERENCES dbo.ExpenseCategories(CategoryId);
```

**4. Test locally:**
```bash
sqlcmd -S (localdb)\mssqllocaldb -d MyDesk -i "src/Deployment/Migration/035_add_expense_categories.sql"
```

---

## Deployment Process

### Local Build

```bash
# Build in Release mode
dotnet build --configuration Release

# Run tests
dotnet test --configuration Release --no-build

# Publish
dotnet publish -c Release -o ./publish
```

### Staging Deployment

```bash
# Push to staging branch
git push origin feature/expenses-receipt-ocr:staging

# CI/CD pipeline automatically:
# 1. Builds the application
# 2. Runs all tests
# 3. Deploys to staging environment
# 4. Runs smoke tests
```

**Check status:** https://github.com/peterjbardenhagen/app.mydesk.digitalresponse.com.au/actions

### Production Deployment

```bash
# Create pull request
# - Review code
# - Get approval
# - Merge to main

# CI/CD pipeline automatically:
# 1. Builds the application
# 2. Runs all tests
# 3. Runs security scan
# 4. Deploys to production
# 5. Runs post-deployment tests
# 6. Alerts if deployment fails
```

---

## Debugging Tips

### Visual Studio Debugging

```csharp
// Set breakpoint and debug
public async Task<Expense> CreateExpenseAsync(...)
{
    // Set breakpoint here by clicking margin
    var result = await _db.QueryAsync(...);
    
    // Inspect variables: hover over variable or use Watch window
    // Step through code: F10 (step over), F11 (step into)
}
```

### Logging in Development

```csharp
public class ExpenseService
{
    private readonly ILogger<ExpenseService> _logger;
    
    public async Task<Expense> CreateExpenseAsync(...)
    {
        _logger.LogInformation(
            "Creating expense: Amount={Amount}, Category={Category}, TenantId={TenantId}",
            amount, category, tenantId);
        
        // ... create expense ...
        
        _logger.LogInformation(
            "Expense created: ExpenseId={ExpenseId}",
            expense.ExpenseId);
    }
}
```

**View logs in development:**
- Console output in Visual Studio Debug window
- Or: `Application Insights` in Azure Portal (for deployed app)

### SQL Query Debugging

```sql
-- Enable query statistics
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

-- Your query
SELECT * FROM Expenses 
WHERE TenantId = 1 AND UserId = 100;

-- View results:
-- - Logical reads (IO)
-- - Execution time (CPU)

-- Analyze query plan
DBCC SHOWCONTIG (Expenses);
```

---

## Common Issues & Solutions

### Issue: "Could not connect to local database"

**Solution:**
```bash
# Verify SQL Server is running
sqllocaldb info mssqllocaldb

# Start if not running
sqllocaldb start mssqllocaldb

# Verify connection string
sqlcmd -S (localdb)\mssqllocaldb

# If still failing, recreate:
sqllocaldb delete mssqllocaldb
sqllocaldb create mssqllocaldb
```

### Issue: "Migration failed: table already exists"

**Solution:** Migrations should be idempotent:
```sql
-- Always use IF NOT EXISTS
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'MyTable')
BEGIN
    CREATE TABLE dbo.MyTable (...)
END
```

### Issue: "Tests fail with 'tenant_id' claim not found"

**Solution:** Ensure test adds JWT token with claims:
```csharp
var token = JwtTokenHelper.GenerateToken(
    userId: 100,
    tenantId: 1,
    roles: new[] { "Employee", "Manager" });

Client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", token);
```

### Issue: "Unit test timeout"

**Solution:** Add timeout attribute:
```csharp
[Test, Timeout(5000)]  // 5 second timeout
public async Task MyTest()
{
    // This test will timeout if it takes > 5 seconds
}
```

---

## Security Checklist

Before committing code:

- [ ] No hardcoded secrets (connection strings, API keys)
- [ ] All SQL queries parameterized (no string concatenation)
- [ ] All API endpoints validate tenant_id
- [ ] All database queries filter by TenantId
- [ ] Input validation on all user inputs
- [ ] Sensitive data not logged
- [ ] No console.log() calls left in production code
- [ ] Error messages don't leak sensitive info
- [ ] CORS policy restricted to known domains
- [ ] Authentication required on all APIs (except /api/auth/login)

---

## Performance Checklist

Before submitting PR:

- [ ] Added indexes to frequently queried columns
- [ ] No N+1 queries (use JOIN instead of loop)
- [ ] Used async/await for all I/O
- [ ] Database queries < 100ms
- [ ] API endpoints < 500ms (p95)
- [ ] No memory leaks (verify with profiler)
- [ ] No unnecessary allocations (use ValueTask where possible)
- [ ] Load test passed (1000 concurrent users)

---

## Useful Commands

```bash
# Build and run
dotnet run --project src/MyDesk.Web

# Run tests
dotnet test

# Format code
dotnet format

# Create migration
dotnet ef migrations add "MigrationName"

# Update database
dotnet ef database update

# View NuGet packages
dotnet list package

# Check for vulnerabilities
dotnet list package --vulnerable

# Clean build artifacts
dotnet clean
```

---

## Further Reading

- **Architecture Questions:** See `ENTERPRISE-ARCHITECTURE.md` and `SOLUTION-ARCHITECTURE.md`
- **Feature Specifications:** See `PRODUCT-REQUIREMENTS.md`
- **Implementation Details:** See `agents.md`
- **Market Strategy:** See `PRODUCT-STRATEGY.md`

---

## Getting Help

1. **Check the relevant architecture document** (see Documentation Map above)
2. **Review agents.md** to understand feature ownership
3. **Look for similar code** in the codebase
4. **Search GitHub issues** for related problems
5. **Ask in team chat** or create GitHub Discussion

---

## Document Revision History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 1.0 | 2026-07-05 | Claude | Initial development guide with setup, standards, and workflow |

