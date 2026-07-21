# MyDesk Solution Architecture

**Version:** 1.0  
**Last Updated:** July 2026  
**Target Audience:** Technical architects, backend engineers, DevOps teams

---

## Executive Summary

MyDesk's solution architecture translates enterprise requirements into a concrete, implementable technical design. Built on .NET 10 with Blazor Server frontend and SQL Server 2022 backend, the architecture emphasizes security, scalability, and compliance through domain-driven design, event-driven workflows, and immutable audit logging.

This document describes the technical decisions, system components, data flow, API design, and deployment patterns that enable MyDesk to serve as a secure, multi-tenant SaaS platform meeting ISO 27001, SOC 2 Type II, and Sarbanes-Oxley compliance requirements.

---

## System Architecture Overview

```
┌────────────────────────────────────────────────────────┐
│                   CLIENT TIER                          │
│  ┌────────────────┐          ┌──────────────────────┐ │
│  │  Blazor Server │          │  Mobile Apps (iOS)   │ │
│  │  (Web UI)      │          │  (Future)            │ │
│  └────────────────┘          └──────────────────────┘ │
└────────────────────────────────────────────────────────┘
                         ↓ TLS 1.3
┌────────────────────────────────────────────────────────┐
│              GATEWAY & LOAD BALANCER TIER              │
│  ┌──────────────────────────────────────────────────┐ │
│  │  Azure API Gateway                               │ │
│  │  - WAF (Web Application Firewall)               │ │
│  │  - DDoS Protection                              │ │
│  │  - Rate Limiting (API-level)                    │ │
│  │  - SSL/TLS Termination                          │ │
│  └──────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────┘
                         ↓
┌────────────────────────────────────────────────────────┐
│               APPLICATION TIER (Stateless)             │
│  ┌──────────────────────────────────────────────────┐ │
│  │  MyDesk.Web (ASP.NET Core 10, Blazor Server)    │ │
│  │  Running in Auto-Scale App Service              │ │
│  │                                                  │ │
│  │  Responsibilities:                              │ │
│  │  - Authentication (JWT)                         │ │
│  │  - Authorization (RBAC + Tenant Claims)         │ │
│  │  - Business Logic (Expenses, Approvals)         │ │
│  │  - API Endpoints (REST)                         │ │
│  │  - Blazor Components (Interactive UI)           │ │
│  │  - Background Jobs (Email, Notifications)       │ │
│  └──────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────┘
                         ↓
┌────────────────────────────────────────────────────────┐
│                    DATA TIER                           │
│  ┌──────────────────────────────────────────────────┐ │
│  │  SQL Server 2022 (Always-On Availability Group) │ │
│  │  - Transactional Data                           │ │
│  │  - Audit Logs (Immutable)                       │ │
│  │  - Configuration                                │ │
│  │  - Backups & PITR                               │ │
│  └──────────────────────────────────────────────────┘ │
│  ┌──────────────────────────────────────────────────┐ │
│  │  Azure Blob Storage                             │ │
│  │  - User Photos                                  │ │
│  │  - Expense Receipts                             │ │
│  │  - Document Archives                            │ │
│  └──────────────────────────────────────────────────┘ │
└────────────────────────────────────────────────────────┘
                         ↓
┌────────────────────────────────────────────────────────┐
│              EXTERNAL SERVICES (Optional)              │
│  - SendGrid/Azure Email                              │
│  - Twilio SMS (future)                               │
│  - Azure Document Intelligence (OCR)                  │
│  - Azure Key Vault (secrets)                          │
└────────────────────────────────────────────────────────┘
```

---

## Core Design Patterns

### 0. **Orchestrator-Worker Agentic Architecture (2026-Standard)**

MyDesk uses distributed agentic patterns to manage complex workflows safely and efficiently:

```
User Request
    ↓
┌─────────────────────────────────────────┐
│  Orchestrator Agent (Manager)           │
│  - Intent classification (Router)       │
│  - Security validation (Critic)         │
│  - Worker invocation & verification     │
└─────────────────────────────────────────┘
    ↓
┌──────────────┬──────────────┬───────────┐
│  Auth Agent  │ Approval     │Notification
│              │ Worker       │Worker
└──────────────┴──────────────┴───────────┘
```

**Key Agentic Patterns:**
- **Router:** Logic-based intent classifier (not LLM-based for cost/latency)
- **Planner-Executor:** Multi-step approval workflows (validate → route → escalate)
- **Critic/Verifier:** Security gates (PII filtering, permission validation)
- **Summarizer:** Memory compression via "Archivist" agent (prevents token explosion)

**Benefits:**
- Avoid monolithic "god agent" bottleneck
- Stateless workers enable horizontal scaling
- Memory-efficient (compress long conversations)
- Audit trail per agent invocation
- Security-first (PII filtering, auth middleware)

---

### 1. **Domain-Driven Design (DDD)**

The application organizes code around business domains:

**Core Domains:**
- **Tenant Domain**: Manages multi-tenant isolation, domains, billing configuration
- **User Domain**: Manages users, roles, authentication, profiles
- **Expense Domain**: Manages expense creation, submission, receipt processing
- **Approval Domain**: Manages approval workflows, permissions, thresholds, audit trail
- **Notification Domain**: Manages templates, preferences, delivery channels, queuing
- **Compliance Domain**: Manages audit logs, export tracking, consent management

**Data Organization:**
Each domain has its own set of tables organized into 5 layers:
1. **Core Business Layer**: Users, Tenants, Departments, Expenses, Timesheets
2. **Workflows Layer**: ApprovalWorkflows, ApprovalRules, ApprovalRequests, ApprovalActions
3. **Notifications Layer**: NotificationTemplates, NotificationSettings, NotificationLog, EmailQueue
4. **Compliance Layer**: ComplianceAuditLog (immutable), SecurityAuditEvents, DataExportAudit
5. **Tenancy Layer**: ClientBillingConfig, ClientUsageLog, ClientOnboardingSession

### 2. **Multi-Tenant Architecture**

**Domain-Based Routing** (Automatic user provisioning without admin intervention):
```
User Login: john@digitalresponse.com.au
                    ↓
Domain Lookup: SELECT TenantId FROM TenantDomains 
               WHERE Domain = 'digitalresponse.com.au'
                    ↓
Auto-Assign: TenantId = 2 (Digital Response tenant)
User Created: If first-time login for this domain
                    ↓
JWT Claims: { tenant_id: 2, user_id: 456, email: "john@..." }
```

**Enforcement Strategy:**
- **API Level**: Every endpoint validates `tenant_id` claim
- **Database Level**: All queries filtered by `WHERE TenantId = @TenantId`
- **Application Level**: `CurrentTenantAccessor` service provides tenant context
- **Row-Level Security**: Each table includes `TenantId` foreign key

**Isolation Guarantee:**
Impossible for one tenant to query another's data (if someone modifies JWT claims, database queries still filter by TenantId from the modified JWT).

### 3. **Event-Driven Workflows**

Approval workflows are implemented as a sequence of domain events:

```
USER SUBMITS EXPENSE
        ↓
Event: ExpenseSubmitted
    - Create Expense record
    - Determine approval chain (based on amount & ApprovalPermissions)
    - Send notifications to approvers
        ↓
APPROVER APPROVES
        ↓
Event: ApprovalApproved
    - Update Expense status
    - Check if all approvals complete
    - Send notification to submitter
    - If final approval: update payment status
        ↓
PAYMENT PROCESSING (Future)
        ↓
Event: ExpensePaid
    - Create accounting entry
    - Send confirmation to submitter
```

**Implementation:**
Each event creates an immutable `ComplianceAuditLog` entry for regulatory tracking.

### 4. **Immutable Audit Trail**

The `ComplianceAuditLog` table is append-only with NO UPDATE/DELETE permissions:

```sql
CREATE TABLE dbo.ComplianceAuditLog (
    AuditId BIGINT PRIMARY KEY IDENTITY(1,1),
    EntityType VARCHAR(50),      -- Expense, Approval, User, etc.
    EntityId INT,                 -- Which record was affected
    Action VARCHAR(50),           -- created, updated, approved, exported
    UserId INT,                   -- Who performed the action
    TenantId INT,                 -- Which tenant
    Timestamp DATETIME2,          -- When (UTC)
    IpAddress VARCHAR(45),        -- IPv4 or IPv6
    UserAgent NVARCHAR(MAX),      -- Browser/client info
    Details NVARCHAR(MAX)         -- JSON of what changed
    -- NO UPDATE or DELETE triggers
)
```

**Compliance Requirements Met:**
- ✅ 7-year retention (automated archive)
- ✅ Complete immutability (no application code can delete)
- ✅ Tamper detection (hash verification in future)
- ✅ Timezone auditing (always UTC)
- ✅ Segregation of duties tracking (who approved vs. who submitted)

### 5. **Permission Model (Zero-Trust RBAC)**

**Role Hierarchy:**
```
1. Administrator (Tenant-level)
   - Full tenant access
   - Can modify all workflows and permissions
   - Can view all audit logs

2. Director (Department-level)
   - Can approve all expenses (no limit)
   - Can approve timesheets
   - Can view department reports
   - Can delegate to managers

3. Manager (Team-level)
   - Can approve expenses up to threshold
   - Can view team expenses
   - Can delegate to other managers

4. Employee (Individual)
   - Can create own expenses
   - Can view own expense history
   - Can submit for approval
   - Cannot modify after submission
```

**Threshold-Based Authority:**
```sql
CREATE TABLE dbo.ApprovalPermissions (
    PermissionId INT PRIMARY KEY,
    TenantId INT,
    RoleId INT,
    MaxApprovalAmount DECIMAL(12,2),  -- NULL = unlimited
    CanApproveDepartments NVARCHAR(MAX),  -- JSON array or NULL (all)
    CanApproveCategories NVARCHAR(MAX)    -- JSON array or NULL (all)
)

-- Example:
-- Manager role: MaxApprovalAmount = 5000 (approves < $5k)
-- Director role: MaxApprovalAmount = NULL (approves all)
```

**Permission Check Logic:**
```csharp
// In ApprovalService
var requiredApprovals = approvalRules
    .Where(r => r.Amount >= expense.Amount)
    .Where(r => r.MinimumRole <= currentUser.Role)
    .Select(r => r.ApproverRoleId)
    .ToList();

// Only send to approvers who have authority
var eligibleApprovers = approversInChain
    .Where(a => permissions[a.RoleId].MaxApprovalAmount >= expense.Amount)
    .ToList();
```

---

## API Design

### Authentication Flow

**Endpoint: POST /api/auth/login**
```
Request:
{
  "email": "john@digitalresponse.com.au",
  "password": "secure_password"
}

Process:
1. Extract domain from email: "digitalresponse.com.au"
2. Look up TenantId from TenantDomains table
3. Query Users table: WHERE TenantId = @TenantId AND Email = @Email
4. Hash input password using BCrypt and compare with stored hash
5. Create JWT with claims: { tenant_id, user_id, email, roles }
6. Log to ComplianceAuditLog (UserLoggedIn)
7. Return JWT token + user details

Response:
{
  "accessToken": "eyJhbGc...",
  "tokenType": "Bearer",
  "expiresIn": 3600,
  "user": {
    "userId": 456,
    "email": "john@digitalresponse.com.au",
    "name": "John Smith",
    "roles": ["Employee", "Manager"],
    "tenantId": 2
  }
}
```

### Expense Submission with Approval Routing

**Endpoint: POST /api/expenses**
```
Request:
{
  "amount": 1250.50,
  "description": "Client meeting lunch",
  "category": "Meals",
  "receiptPhotoId": 789,
  "departmentId": 5
}

Process:
1. Validate tenant_id claim from JWT
2. Validate user can create in department
3. Create Expense record in database
4. Determine approval chain:
   a. Get ApprovalRules for TenantId
   b. Find approvers for amount $1250.50
   c. Get ApprovalPermissions to filter eligible approvers
5. Create ApprovalRequest records (one for each approver)
6. Send notifications to approvers (via NotificationService)
7. Log to ComplianceAuditLog (ExpenseCreated)
8. Return expense details with approval status

Response:
{
  "expenseId": 1001,
  "status": "PendingApproval",
  "approvalRequests": [
    {
      "approvalRequestId": 5001,
      "approverId": 100,
      "approverName": "Jane Manager",
      "status": "Pending",
      "dueDate": "2026-07-08"
    }
  ]
}
```

### Approval Action with Notification

**Endpoint: POST /api/approval/requests/{id}/approve**
```
Request:
{
  "approvalRequestId": 5001,
  "comment": "Looks good, approved.",
  "approvalDate": "2026-07-05T10:30:00Z"
}

Process:
1. Validate tenant_id claim
2. Validate user is the assigned approver
3. Update ApprovalRequest: Status = Approved
4. Check if all approvals complete
5. If all approved: Update Expense: Status = Approved
6. Send notification to expense submitter
7. Log to ComplianceAuditLog (ApprovalApproved)
8. Increment unread notification count
9. Return updated approval status

Response:
{
  "approvalRequestId": 5001,
  "status": "Approved",
  "approvedAt": "2026-07-05T10:30:00Z",
  "expenseStatus": "Approved",
  "notificationSent": true,
  "submitterNotified": {
    "userId": 456,
    "email": "john@digitalresponse.com.au",
    "channels": ["Email", "InApp"]
  }
}
```

### Notification Retrieval

**Endpoint: GET /api/notifications?limit=10&offset=0**
```
Process:
1. Extract tenant_id and user_id from JWT
2. Query InAppNotifications WHERE TenantId = @TenantId AND UserId = @UserId
3. Order by CreatedAt DESC, limit 10
4. Get unread count from NotificationState
5. Return both notifications and summary

Response:
{
  "unreadCount": 3,
  "notifications": [
    {
      "notificationId": 1001,
      "title": "Expense Needs Revision",
      "message": "Jane Manager returned your $1250.50 expense",
      "category": "Approval",
      "entityType": "Expense",
      "entityId": 999,
      "actionUrl": "/expenses/999",
      "isRead": false,
      "createdAt": "2026-07-05T10:15:00Z"
    },
    ...
  ]
}
```

---

## Database Schema Design

### Tenant Isolation Pattern

Every table includes `TenantId`:
```sql
CREATE TABLE dbo.Expenses (
    ExpenseId INT PRIMARY KEY,
    TenantId INT NOT NULL,        -- ← Always included
    UserId INT NOT NULL,
    Amount DECIMAL(12,2),
    Status VARCHAR(20),
    CreatedAt DATETIME2,
    
    FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId),
    INDEX IX_Expenses_TenantId_UserId (TenantId, UserId),
    INDEX IX_Expenses_TenantId_Status (TenantId, Status)
)

-- All queries follow this pattern:
-- SELECT * FROM Expenses WHERE TenantId = @TenantId AND UserId = @UserId
```

### Immutable Audit Log Pattern

```sql
CREATE TABLE dbo.ComplianceAuditLog (
    AuditId BIGINT PRIMARY KEY IDENTITY(1,1),
    TenantId INT NOT NULL,
    EntityType VARCHAR(50) NOT NULL,
    EntityId INT NOT NULL,
    Action VARCHAR(50) NOT NULL,
    UserId INT,
    Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    IpAddress VARCHAR(45),
    UserAgent NVARCHAR(MAX),
    Details NVARCHAR(MAX),  -- JSON
    
    INDEX IX_ComplianceAuditLog_TenantId (TenantId),
    INDEX IX_ComplianceAuditLog_EntityType_EntityId (EntityType, EntityId),
    INDEX IX_ComplianceAuditLog_Timestamp (Timestamp)
)

-- Permissions: Application role can only INSERT
-- No UPDATE/DELETE triggers
-- SQL Agent job archives to cold storage after 90 days
```

### Notification Queue Pattern

```sql
CREATE TABLE dbo.EmailQueue (
    QueueId INT PRIMARY KEY,
    TenantId INT,
    ToEmail NVARCHAR(255),
    Subject NVARCHAR(255),
    BodyHtml NVARCHAR(MAX),
    Status VARCHAR(20),         -- Pending, Sent, Failed, Bounced
    Priority INT,               -- 1-10, lower = higher priority
    RetryCount INT,
    MaxRetries INT DEFAULT 3,
    LastAttemptAt DATETIME2,
    CreatedAt DATETIME2
)

-- Background job (every 30 seconds):
-- SELECT * FROM EmailQueue WHERE Status = 'Pending' ORDER BY Priority, CreatedAt
-- Attempt to send via SendGrid
-- If successful: Status = 'Sent', update NotificationLog
-- If failed: Increment RetryCount, retry with exponential backoff
```

---

## Security Implementation

### Input Validation

**All API endpoints validate:**
```csharp
// Example: ExpenseController.CreateAsync()
public async Task<IActionResult> CreateAsync(CreateExpenseRequest request)
{
    // 1. Model validation (via attributes)
    if (!ModelState.IsValid)
        return BadRequest(ModelState);

    // 2. Domain validation
    if (request.Amount <= 0)
        return BadRequest("Amount must be positive");

    if (request.Amount > 1_000_000)
        return BadRequest("Amount exceeds maximum");

    // 3. Authorization validation
    var user = User.FindFirst("user_id");
    var tenantId = int.Parse(User.FindFirst("tenant_id")?.Value ?? "0");

    var expense = await _expenseService.CreateAsync(
        tenantId,
        int.Parse(user.Value),
        request);

    return Created($"/api/expenses/{expense.ExpenseId}", expense);
}
```

### SQL Injection Prevention

**Pattern: Always use parameterized queries:**
```csharp
// ✅ SAFE - Parameterized
var result = await _db.QueryAsync(
    @"SELECT * FROM Expenses 
      WHERE TenantId = @TenantId AND ExpenseId = @ExpenseId",
    new() { ["TenantId"] = tenantId, ["ExpenseId"] = expenseId });

// ❌ UNSAFE - String concatenation (never use)
// var result = await _db.QueryAsync(
//     $"SELECT * FROM Expenses WHERE TenantId = {tenantId}");
```

### Cross-Site Scripting (XSS) Prevention

**Blazor Component Pattern:**
```razor
@* Safe: Blazor automatically HTML-encodes property bindings *@
<p>Welcome, @User.Name</p>  <!-- HTML-encoded -->

@* Safe: For HTML content, explicitly mark as safe after validation *@
@if (expense.ReceiptPhotoUrl != null)
{
    <img src="@expense.ReceiptPhotoUrl" alt="Receipt" />  <!-- URL-encoded -->
}

@* Safe: Form inputs are validated server-side *@
<EditForm Model="createExpenseModel" OnValidSubmit="HandleValidSubmit">
    <InputNumber @bind-Value="createExpenseModel.Amount" />
    <button type="submit">Submit</button>
</EditForm>
```

### CSRF Protection

**Built-in via Blazor Server:**
- Blazor Server automatically includes anti-CSRF tokens in forms
- REST APIs protected via `[ValidateAntiForgeryToken]` attribute
- No need for additional CSRF middleware

### Password Security

```csharp
// Using BCrypt (secure hashing with salt)
public class AuthenticationService
{
    public async Task<User> AuthenticateAsync(string email, string password, int tenantId)
    {
        // Get user from database
        var user = await _db.GetUserAsync(tenantId, email);
        
        if (user == null)
            return null;

        // BCrypt automatically validates salt and hash
        bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        
        if (!isValid)
            return null;

        // Log successful login to audit trail
        await _auditService.LogAsync(tenantId, user.UserId, "UserLoggedIn", 
            new { email, timestamp = DateTime.UtcNow });

        return user;
    }

    // When creating user:
    public string HashPassword(string plaintext)
    {
        // BCrypt generates salt internally, cost 12 = ~250ms per hash
        return BCrypt.Net.BCrypt.HashPassword(plaintext, workFactor: 12);
    }
}
```

### Rate Limiting with Exponential Backoff

```sql
CREATE TABLE dbo.RateLimitingRules (
    RuleId INT PRIMARY KEY,
    TenantId INT,
    Path VARCHAR(255),              -- /api/expenses, /api/auth/login
    MaxRequestsPerMinute INT,       -- 100
    MaxRequestsPerHour INT,         -- 1000
    BackoffStrategy VARCHAR(50)     -- exponential, fixed
)

-- Implementation in middleware:
public class RateLimitingMiddleware
{
    public async Task InvokeAsync(HttpContext context)
    {
        var userId = context.User.FindFirst("user_id")?.Value;
        var ipAddress = context.Connection.RemoteIpAddress.ToString();
        var path = context.Request.Path;

        // Check rate limit
        var violations = await _db.GetViolationsAsync(userId, path, DateTime.UtcNow.AddMinutes(-1));
        
        if (violations.Count >= rule.MaxRequestsPerMinute)
        {
            // Calculate backoff: 1s, 2s, 4s, 8s, 16s
            int backoffSeconds = Math.Min(16, (int)Math.Pow(2, violations.Count - rule.MaxRequestsPerMinute));
            
            context.Response.StatusCode = 429;
            context.Response.Headers.RetryAfter = backoffSeconds.ToString();
            await context.Response.WriteAsync($"Rate limit exceeded. Retry after {backoffSeconds}s");
            return;
        }

        // Log attempt
        await _db.LogAttemptAsync(userId, ipAddress, path);
        await next(context);
    }
}
```

---

## Agentic Implementation Patterns

### Orchestrator Agent: Intent Routing

The Orchestrator receives all user intents and routes to appropriate workers:

```csharp
public class OrchestratorAgent
{
    public async Task<ApiResponse> RouteAsync(string intent, UserContext user, object payload)
    {
        // 1. Router Pattern: Classify intent
        var classification = ClassifyIntent(intent);
        
        // 2. Critic Pattern: Validate permissions
        ValidateUserPermissions(user, classification);
        
        // 3. Route to appropriate worker
        var workerResult = classification.Type switch
        {
            "expense_submitted" => await _expenseWorker.CreateAsync(payload, user),
            "approval_requested" => await _approvalWorker.ProcessAsync(payload, user),
            "notification_trigger" => await _notificationWorker.SendAsync(payload, user),
            _ => throw new InvalidOperationException($"Unknown intent: {intent}")
        };
        
        // 4. Critic Pattern: Verify worker output
        VerifyWorkerOutput(workerResult, classification);
        
        return new ApiResponse { Success = true, Data = workerResult };
    }
}
```

### Worker Agent: Stateless Execution

Workers are stateless, focused, and invoke via structured hand-offs:

```csharp
public class ApprovalWorkflowWorker
{
    // Input: Structured hand-off from Orchestrator
    public async Task<ApprovalResult> ProcessAsync(ApprovalRequest request, UserContext context)
    {
        // Planner-Executor Pattern:
        
        // Step 1: Plan approval chain
        var approvers = await PlanApprovalChainAsync(request, context);
        
        // Step 2: Validate each approver's authority
        foreach (var approver in approvers)
        {
            ValidateApproverPermissions(approver, request);
        }
        
        // Step 3: Create approval requests
        var approvalIds = await CreateApprovalRequestsAsync(approvers, request);
        
        // Step 4: Hand off to Notification Worker (structured JSON)
        var notificationPayload = new NotificationHandoff
        {
            ApproverIds = approvalIds,
            EventType = "ApprovalRequested",
            Expense = request.Expense
        };
        
        await _notificationWorker.SendAsync(notificationPayload, context);
        
        // Step 5: Log to immutable audit trail
        await _auditService.LogAsync("ApprovalChainCreated", context, new { approvalIds });
        
        return new ApprovalResult { ApprovalIds = approvalIds };
    }
}
```

### Memory Management: Summarization Agent

Prevent token explosion in long conversations:

```csharp
public class ArchiverAgent
{
    private const int TokenThreshold = 10000;
    
    public async Task<string> CompressStateAsync(string conversationHistory, string stateId)
    {
        if (CountTokens(conversationHistory) < TokenThreshold)
            return conversationHistory; // No compression needed
        
        // Summarize full history
        var summary = await _summarizerWorker.SummarizeAsync(conversationHistory);
        
        // Keep only recent messages (last 5) + summary
        var compressedState = new
        {
            summary,
            recentMessages = conversationHistory.Split('\n').TakeLast(5),
            compressedAt = DateTime.UtcNow
        };
        
        // Cache in Redis
        await _cache.SetAsync($"agent_state:{stateId}", compressedState, 
            TimeSpan.FromHours(24));
        
        return JsonSerializer.Serialize(compressedState);
    }
}
```

### Security: PII Filtering & Auth Middleware

Before agents process user data:

```csharp
public class PiiFilterService
{
    public string FilterPii(string text)
    {
        // Strip PII before sending to agents
        text = Regex.Replace(text, @"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}", "[EMAIL]");
        text = Regex.Replace(text, @"\b(\+61|0)[0-9\s\-\.]{7,}\b", "[PHONE]");
        text = Regex.Replace(text, @"\b[0-9]{11}\b", "[ABN]");
        
        _auditService.LogAsync("PiiFiltered", "Notification", text);
        return text;
    }
}

// Auth Middleware: All agent invocations logged
app.Use(async (context, next) =>
{
    var tenantId = context.User.FindFirst("tenant_id")?.Value;
    if (string.IsNullOrEmpty(tenantId))
    {
        context.Response.StatusCode = 401;
        return;
    }
    
    // Log agent invocation to ComplianceAuditLog
    await _auditService.LogAsync("AgentInvoked", "System", new
    {
        path = context.Request.Path,
        tenantId,
        userId = context.User.FindFirst("user_id")?.Value,
        timestamp = DateTime.UtcNow
    });
    
    await next();
});
```

### UI/UX: Non-Blocking Agent Operations

When agents process (approval routing, notification queuing), show async state:

**Blazor:**
```razor
@if (isSubmitting)
{
    <MudProgressLinear Indeterminate="true" />
    <p>Routing to approvers... <i class="spinner"></i></p>
}

@code {
    private async Task SubmitAsync()
    {
        isSubmitting = true;
        try
        {
            // Async invocation without blocking UI thread
            await Orchestrator.RouteAsync("expense_submitted", context, payload);
        }
        finally
        {
            isSubmitting = false;
        }
    }
}
```

**Android/Compose:**
```kotlin
Button(onClick = {
    viewModelScope.launch {
        orchestrator.routeAsync("expense_submitted", context, payload)
    }
}) {
    Text("Submit")
}
```

---

## Scalability Patterns

### Stateless Application Design

**No in-process state:**
```csharp
// ❌ BAD - Stores state in memory
public class NotificationService
{
    private static Dictionary<int, Queue<Notification>> _unreadNotifications;
    
    public void SendNotification(Notification n)
    {
        if (!_unreadNotifications.ContainsKey(n.UserId))
            _unreadNotifications[n.UserId] = new Queue<Notification>();
        _unreadNotifications[n.UserId].Enqueue(n);
    }
}
// Problem: If request goes to different server, state is lost

// ✅ GOOD - All state in database
public class NotificationService
{
    private readonly DatabaseService _db;
    
    public async Task SendNotificationAsync(Notification n)
    {
        await _db.ExecuteNonQueryAsync(
            @"INSERT INTO dbo.InAppNotifications (...) VALUES (...)",
            parameters);
        // No in-memory state
    }
}
// Any server instance can read from database
```

### Database Connection Pooling

```csharp
// In Program.cs:
builder.Services.AddScoped<DatabaseService>(sp =>
{
    var connectionString = configuration["ConnectionStrings:DefaultConnection"];
    
    // ADO.NET automatically pools connections
    // Configurable via connection string:
    // "Server=...;Min Pool Size=10;Max Pool Size=100;Connection Timeout=30"
    
    return new DatabaseService(connectionString);
});

// Connection pool lifecycle:
// - Min 10 connections always open
// - Up to 100 connections created on demand
// - Idle connections closed after 8 minutes
// - Timeout of 30 seconds to acquire connection
```

### Async/Await Throughout

```csharp
// ✅ Async - Doesn't block thread pool
public async Task<Expense> GetExpenseAsync(int tenantId, int expenseId)
{
    var result = await _db.QueryAsync(
        @"SELECT * FROM Expenses WHERE TenantId = @TenantId AND ExpenseId = @ExpenseId",
        new() { ["TenantId"] = tenantId, ["ExpenseId"] = expenseId });
    
    return MapToExpense(result.Rows[0]);
}

// ❌ Blocking - Blocks thread while waiting
public Expense GetExpense(int tenantId, int expenseId)
{
    var result = _db.Query(
        @"SELECT * FROM Expenses WHERE TenantId = @TenantId AND ExpenseId = @ExpenseId",
        new() { ["TenantId"] = tenantId, ["ExpenseId"] = expenseId });
    
    return MapToExpense(result.Rows[0]);
}
```

### Auto-Scaling Configuration

```yaml
# Azure App Service Auto-Scale Rules
Minimum Instances: 2
Maximum Instances: 10

Scale-Out Rule:
  CPU >= 70% for 5 minutes → Add 1 instance (max 10)

Scale-In Rule:
  CPU <= 30% for 10 minutes → Remove 1 instance (min 2)

Additional Metric:
  If Email Queue Depth > 1000 → Force scale-out
```

---

## Development Workflow

### Project Structure

```
MyDesk.sln
├── src/
│   ├── MyDesk.Web/
│   │   ├── Program.cs                 -- Dependency injection, middleware
│   │   ├── appsettings.json          -- Configuration
│   │   ├── Services/
│   │   │   ├── DatabaseService.cs    -- Query execution
│   │   │   ├── AuthenticationService.cs
│   │   │   ├── ExpenseService.cs
│   │   │   ├── ApprovalService.cs
│   │   │   ├── NotificationService.cs
│   │   │   ├── PhotoProcessingService.cs
│   │   │   └── ...
│   │   ├── Controllers/
│   │   │   ├── AuthController.cs     -- Authentication endpoints
│   │   │   ├── ExpenseController.cs  -- Expense endpoints
│   │   │   ├── ApprovalController.cs -- Approval endpoints
│   │   │   ├── NotificationController.cs
│   │   │   └── ...
│   │   ├── Components/
│   │   │   ├── Layout/
│   │   │   │   ├── MainLayout.razor
│   │   │   │   └── NavMenu.razor
│   │   │   ├── Pages/
│   │   │   │   ├── Expenses.razor
│   │   │   │   ├── Approvals.razor
│   │   │   │   ├── Profile.razor
│   │   │   │   └── ...
│   │   │   └── Dialogs/
│   │   │       ├── PhotoUploadDialog.razor
│   │   │       ├── ExpenseReceiptUploadDialog.razor
│   │   │       └── ...
│   │   └── Migrations/
│   │       ├── 001_initial_schema.sql
│   │       ├── 002_users_and_auth.sql
│   │       └── ...
│   └── Deployment/
│       ├── Migration/
│       │   ├── 001_initial_schema.sql
│       │   ├── 002_users_and_auth.sql
│       │   └── ...
│       └── Scripts/
│           ├── backup.ps1
│           ├── restore.ps1
│           └── ...
└── tests/
    ├── MyDesk.Web.Tests/
    │   ├── Services/
    │   ├── Controllers/
    │   └── Integration/
```

### Build & Deployment

**Local Development:**
```bash
# 1. Install dependencies
dotnet restore

# 2. Set up local database
sqlcmd -S (localdb)\mssqllocaldb -i src/Deployment/Migration/001_initial_schema.sql

# 3. Run application
dotnet run --project src/MyDesk.Web

# Application running at https://localhost:7000
```

**CI/CD Pipeline (GitHub Actions):**
```yaml
name: Build and Deploy

on: [push, pull_request]

jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '10.0.x'
      
      - name: Restore dependencies
        run: dotnet restore
      
      - name: Build
        run: dotnet build --configuration Release --no-restore
      
      - name: Run tests
        run: dotnet test --configuration Release --no-build --verbosity normal
      
      - name: Publish
        run: dotnet publish --configuration Release --output ./publish
      
      - name: Deploy to Azure App Service
        uses: azure/webapps-deploy@v2
        with:
          app-name: mydesk-app
          publish-profile: ${{ secrets.AZURE_PUBLISH_PROFILE }}
          package: ./publish
```

---

## Deployment Checklist

- [ ] Environment variables configured (connection string, API keys)
- [ ] SQL Server TLS enabled (minimum 1.3)
- [ ] Blob storage encryption enabled
- [ ] Azure Key Vault secrets rotated
- [ ] Application logging configured (Serilog to Application Insights)
- [ ] Alerts set up (API latency, error rate, database connection pool)
- [ ] Database backups scheduled and tested
- [ ] CORS policy restricted to known domains
- [ ] API rate limiting thresholds tuned
- [ ] Authentication tokens validated and signed
- [ ] Audit logging enabled and monitored

---

## Performance Considerations

### Query Optimization

```sql
-- ❌ SLOW: No index on UserId
SELECT * FROM Expenses WHERE UserId = @UserId

-- ✅ FAST: Composite index on (TenantId, UserId)
CREATE INDEX IX_Expenses_TenantId_UserId 
  ON dbo.Expenses(TenantId, UserId)
  
SELECT * FROM Expenses WHERE TenantId = @TenantId AND UserId = @UserId
```

### Caching Strategy (Future)

```csharp
// Level 1: In-Memory Cache (per-instance, short-lived)
public class TenantService
{
    private readonly IMemoryCache _cache;
    private const string TENANT_CACHE_KEY = "tenant_{0}";
    
    public async Task<Tenant> GetTenantAsync(int tenantId)
    {
        if (_cache.TryGetValue(string.Format(TENANT_CACHE_KEY, tenantId), out var tenant))
            return (Tenant)tenant;
        
        // Not in cache, fetch from database
        var dbTenant = await _db.GetTenantAsync(tenantId);
        
        // Cache for 5 minutes
        _cache.Set(string.Format(TENANT_CACHE_KEY, tenantId), dbTenant, 
            new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
        
        return dbTenant;
    }
}

// Level 2: Distributed Cache (Redis, shared across instances)
// To be implemented in Phase 7 when performance needs justify the complexity
```

---

## Monitoring & Observability

### Structured Logging

```csharp
// Using Serilog
logger.LogInformation(
    "Expense created: ExpenseId={ExpenseId}, TenantId={TenantId}, Amount={Amount:C}",
    expense.ExpenseId, expense.TenantId, expense.Amount);

// Output (JSON format):
{
  "Timestamp": "2026-07-05T10:15:30.1234567Z",
  "Level": "Information",
  "MessageTemplate": "Expense created: ExpenseId={ExpenseId}, TenantId={TenantId}, Amount={Amount:C}",
  "Properties": {
    "ExpenseId": 1001,
    "TenantId": 2,
    "Amount": 1250.50
  }
}
```

### Key Metrics

**Application Metrics:**
- API response time (p50, p95, p99)
- Request count by endpoint
- Error rate (4xx, 5xx)
- Active user sessions

**Database Metrics:**
- Query execution time
- Connection pool utilization
- Slow query log analysis
- Backup completion status

**Business Metrics:**
- Expenses submitted per day
- Average approval time
- Cost per expense
- Top expense categories

---

## References

- **Enterprise Architecture:** See `enterprise-architecture.md`
- **Product Requirements:** See `PRODUCT-REQUIREMENTS.md`
- **Product Strategy:** See `PRODUCT-STRATEGY.md`
- **Development Guide:** See `CLAUDE.md`
- **Implementation Agents:** See `agents.md`

