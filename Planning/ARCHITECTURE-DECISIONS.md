# MyDesk Architecture Decisions & Design Patterns

**Last Updated:** July 2026  
**Architecture Version:** 2.0 (Multi-tenant SaaS)  
**Target Scale:** 100K+ users, 99.95% uptime

---

## Architecture Decision Record (ADR) Process

Each major technical decision is documented following the ADR pattern:

```
ADR #XXX: [Title]
Status: [Proposed/Accepted/Deprecated/Superseded]
Date: [YYYY-MM-DD]
Context: [Problem statement and constraints]
Decision: [What was decided]
Rationale: [Why this decision was made]
Consequences: [Positive and negative impacts]
Alternatives Considered: [Rejected options and why]
```

---

## Core Architecture Decisions

### ADR-001: Multi-Tenant Architecture

**Status:** Accepted (Phase 1)  
**Date:** 2026-05-01

**Context:**
MyDesk targets mid-market organizations (50-5000 employees). A multi-tenant SaaS architecture provides:
- Cost efficiency (shared infrastructure)
- Regulatory compliance (data isolation per tenant)
- Operational scalability (single deployment)

**Decision:**
Implement database-per-table multi-tenancy with TenantId on all tables.

```sql
-- Every table includes TenantId
CREATE TABLE dbo.Expenses (
    ExpenseId INT PRIMARY KEY,
    TenantId INT NOT NULL,  -- ← Mandatory
    UserId INT NOT NULL,
    Amount DECIMAL(12, 2),
    ...
    CONSTRAINT FK_Expenses_Tenant FOREIGN KEY (TenantId) REFERENCES dbo.Tenants(TenantId)
);

-- All queries filter by TenantId
SELECT * FROM Expenses WHERE TenantId = @TenantId AND UserId = @UserId;
```

**Rationale:**
- **Cost:** Single database cluster = 40% lower infrastructure cost vs separate DBs
- **Compliance:** SQL Server row-level security (RLS) enforces tenant isolation
- **Operational:** One backup/restore, one version, one maintenance window
- **Performance:** Shared indexes = better resource utilization

**Consequences:**
- **Positive:**
  - Cost-efficient for bootstrapping phase
  - Easy to add new tenants (minutes)
  - Tenant data portable (entire schema)
  - Simpler operational procedures
  
- **Negative:**
  - Must filter TenantId on every query (developer discipline)
  - Risk of data leakage if TenantId check missed
  - Query performance slightly degraded vs dedicated DB (offset by shared indexes)
  - Shared resource contention (noisy neighbor problem)

**Mitigation:**
- Code review checklist requires TenantId filter on all queries
- Automated linting to detect missing TenantId filters
- Application-level tenant context validation
- Database RLS as safety net

**Alternatives Considered:**
1. **Database-per-tenant:** Each customer gets own database
   - Rejected: Cost prohibitive for SMB customers, operational overhead
2. **Schema-per-tenant:** Multiple schemas, shared database
   - Rejected: More complex migration strategies, less common patterns
3. **Row-level security (RLS):** SQL Server RLS with database roles
   - Considered but rejected: Added complexity, harder to debug, not fully mature in 2026

---

### ADR-002: ASP.NET Core + Blazor for Full-Stack

**Status:** Accepted (Phase 1)  
**Date:** 2026-05-02

**Context:**
Need a modern, enterprise-grade web framework for rapid development. Evaluated:
- ASP.NET Core (C#) + Blazor
- Next.js (TypeScript)
- Spring Boot (Java)

**Decision:**
Use ASP.NET Core 10 with Blazor Server for full-stack development.

**Rationale:**
- **C# Ecosystem:** Strong enterprise libraries (Entity Framework, MudBlazor)
- **Unified Language:** Backend + frontend in same language (C#)
- **Type Safety:** Strong typing reduces runtime errors vs JavaScript
- **Performance:** Compiled language, better optimization than interpreted
- **Enterprise:** Wide adoption in financial services, compliance frameworks
- **Framework Maturity:** Blazor stable since 2019, .NET 10 released in 2024

**Architecture:**

```
┌─────────────────────────────────────────────────┐
│         Client Browser (Blazor WebAssembly)     │
│   - MudBlazor components                        │
│   - Real-time updates via SignalR               │
└────────────────────┬────────────────────────────┘
                     │ WebSocket / HTTP
                     ↓
┌─────────────────────────────────────────────────┐
│      ASP.NET Core Server (Backend API)          │
│ ┌──────────────────────────────────────────────┐│
│ │ Controllers (REST API endpoints)             ││
│ ├──────────────────────────────────────────────┤│
│ │ Services (Business logic)                    ││
│ │ - ExpenseService                            ││
│ │ - ApprovalService                           ││
│ │ - NotificationService                       ││
│ ├──────────────────────────────────────────────┤│
│ │ Data Access Layer (EF Core + Raw SQL)       ││
│ └──────────────────────────────────────────────┘│
└────────────────────┬────────────────────────────┘
                     │ ODBC
                     ↓
        ┌────────────────────────┐
        │   SQL Server 2022      │
        │  - Multi-tenant schema │
        │  - Row-level security  │
        │  - Materialized views  │
        └────────────────────────┘
```

**Consequences:**
- **Positive:**
  - Rapid development (C# + Blazor reduces client code)
  - Strong typing catches errors at compile time
  - Unified deployment (server + client in same package)
  - Excellent debugging experience
  - Rich component library (MudBlazor)
  
- **Negative:**
  - Smaller ecosystem vs JavaScript frameworks
  - Blazor has learning curve for JavaScript developers
  - Real-time features require SignalR (more memory than stateless APIs)
  - Server resources consumed by connected clients

**Alternatives Considered:**
1. **Next.js + Node.js:** Modern, vibrant ecosystem, but JavaScript ecosystem fragmentation
2. **Spring Boot + React:** Proven at scale, but polyglot team complexity
3. **FastAPI + Vue:** Lightweight, but less enterprise support

---

### ADR-003: Event-Driven Architecture for Notifications

**Status:** Accepted (Phase 5)  
**Date:** 2026-11-01

**Context:**
Notifications triggered by domain events (expense submitted, approval required, budget exceeded). Need scalable, decoupled architecture.

**Decision:**
Implement event-driven architecture with Hangfire for background job processing.

```csharp
// Domain event raised
public class ExpenseSubmittedEvent : IDomainEvent
{
    public int TenantId { get; set; }
    public int ExpenseId { get; set; }
    public int SubmittedByUserId { get; set; }
    public decimal Amount { get; set; }
    public DateTime OccurredAt { get; set; }
}

// Event handler (worker)
public class ExpenseSubmittedHandler : IEventHandler<ExpenseSubmittedEvent>
{
    public async Task Handle(ExpenseSubmittedEvent @event)
    {
        // Notify approvers
        await _notificationService.NotifyApproversAsync(@event);
        
        // Update analytics
        await _analyticsService.RecordExpenseAsync(@event);
        
        // Integrate with MYOB
        await _myobService.CreateJournalEntryAsync(@event);
    }
}

// Register in Program.cs
services.AddScoped<IEventHandler<ExpenseSubmittedEvent>, ExpenseSubmittedHandler>();

// Publish event
await _eventPublisher.PublishAsync(new ExpenseSubmittedEvent { ... });
```

**Architecture:**

```
┌───────────────────────────────────────────────────────┐
│ Domain Event (ExpenseSubmittedEvent)                  │
└──────────────────────┬────────────────────────────────┘
                       │ Publish
                       ↓
        ┌──────────────────────────┐
        │ Event Dispatcher         │
        │ (EventPublisher service) │
        └──────────┬───────────────┘
                   │
        ┌──────────┴────────────────────┐
        ↓                               ↓
    ┌─────────────────┐      ┌──────────────────┐
    │ Sync Handler    │      │ Async Handler    │
    │ (Immediate)     │      │ (Hangfire queue) │
    │                 │      │                  │
    │ - Validation    │      │ - Notifications  │
    │ - Logging       │      │ - Integrations   │
    │ - State update  │      │ - Analytics      │
    └─────────────────┘      └──────────────────┘
```

**Hangfire Configuration:**

```csharp
services.AddHangfire(config => config
    .UseSqlServerStorage(connectionString)
    .UseConsole()
);

// In background job service
BackgroundJob.Enqueue<NotificationWorker>(x => 
    x.SendApprovalNotificationAsync(expenseId, approverId));

// Schedule recurring jobs
RecurringJob.AddOrUpdate<NotificationWorker>(
    "send-approval-reminders",
    x => x.SendApprovalReminders(),
    Cron.Hourly);
```

**Consequences:**
- **Positive:**
  - Decoupled: Events don't wait for handlers
  - Scalable: Handlers can run on separate servers
  - Reliable: Hangfire persists jobs to database
  - Observable: Can track event processing through database
  - Extensible: New handlers added without modifying event publishing
  
- **Negative:**
  - Eventual consistency (not immediate)
  - Debugging harder (events may fail asynchronously)
  - Hangfire adds operational complexity
  - Database as queue (slower than dedicated message bus)

**Mitigation:**
- Logging every event and handler execution
- Dead-letter queue for failed events
- Monitoring dashboard for Hangfire jobs
- Retry policies with exponential backoff

**When to Migrate:**
- At >100K events/day: Consider RabbitMQ or Azure Service Bus
- At >10K requests/sec: Consider distributed message broker

**Alternatives Considered:**
1. **Synchronous handlers:** Simple but blocks requests, poor UX
2. **RabbitMQ:** Mature, but operational overhead for small scale
3. **Azure Service Bus:** Managed, but vendor lock-in
4. **SignalR only:** Real-time but not durable, no retry logic

---

### ADR-004: MudBlazor for UI Components

**Status:** Accepted (Phase 1)  
**Date:** 2026-06-01

**Context:**
Need professional, accessible UI components for Blazor. Options:
- MudBlazor (free, MIT license)
- Telerik Blazor (paid, $700/developer/year)
- Syncfusion (paid, $700-1500/developer/year)

**Decision:**
Use MudBlazor for all UI components due to cost-benefit.

**Component Usage Patterns:**

```razor
@* MudContainer for layout *@
<MudContainer MaxWidth="MaxWidth.Large" Class="py-8">
    
    @* MudDataGrid for tables *@
    <MudDataGrid Items="expenses" Striped="true" Hover="true">
        <PropertyColumn Property="e => e.ExpenseId" Title="ID" />
        <PropertyColumn Property="e => e.Amount" Title="Amount" Format="C2" />
        <TemplateColumn Title="Actions">
            <MudButton OnClick="() => EditExpense(context)">Edit</MudButton>
        </TemplateColumn>
    </MudDataGrid>

    @* MudChart for analytics *@
    <MudChart ChartType="ChartType.Pie" 
              InputData="chartData" 
              InputLabels="chartLabels" />
    
    @* MudDialog for forms *@
    <MudButton OnClick="async () => await OpenDialog()">New</MudButton>
    
    @* MudAlert for notifications *@
    <MudAlert Severity="Severity.Success">Expense submitted successfully</MudAlert>
</MudContainer>
```

**Consequences:**
- **Positive:**
  - Zero licensing cost
  - Active community and updates
  - Comprehensive component library (90+ components)
  - Excellent documentation
  - MIT license (permissive)
  - Regular updates aligned with .NET releases
  
- **Negative:**
  - Smaller community than React/Vue ecosystems
  - Customization requires CSS deep knowledge
  - Performance slower than lightweight frameworks
  - Dependency on external package (vendor risk)

**Customization Strategy:**
- CSS variables for theming (light/dark mode)
- Tailwind CSS for supplementary styling
- Custom components built on MudBlazor base classes

**Alternatives Considered:**
1. **Telerik Blazor:** More features but costly, overkill for SMB product
2. **Syncfusion:** Similar to Telerik, expensive
3. **Bootstrap:** Free but requires more custom styling

---

### ADR-005: SignalR for Real-Time Notifications

**Status:** Accepted (Phase 5)  
**Date:** 2026-10-01

**Context:**
Need real-time updates for notifications and approval workflows. Options:
- SignalR (ASP.NET Core native)
- WebSockets (raw implementation)
- Server-Sent Events (one-way push)

**Decision:**
Use SignalR for bi-directional real-time communication.

**Architecture:**

```csharp
// SignalR Hub
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst("sub")?.Value;
        await Groups.AddToGroupAsync(
            Context.ConnectionId, $"user_{userId}");
        await base.OnConnectedAsync();
    }

    public async Task MarkAsRead(int notificationId)
    {
        await _notificationService.MarkAsReadAsync(notificationId);
        await Clients.Group($"user_{userId}")
            .SendAsync("NotificationRead", notificationId);
    }
}

// In Program.cs
services.AddSignalR();
app.MapHub<NotificationHub>("/hubs/notifications");

// Client-side (Blazor)
<script src="_framework/blazor.web.js"></script>
<script src="_content/Microsoft.AspNetCore.SignalR.Client/signalr.min.js"></script>

@code {
    private HubConnection? hubConnection;

    protected override async Task OnInitializedAsync()
    {
        hubConnection = new HubConnectionBuilder()
            .WithUrl(Navigation.ToAbsoluteUri("/hubs/notifications"))
            .WithAutomaticReconnect()
            .Build();

        hubConnection.On<int>("NotificationRead", (notifId) =>
        {
            RemoveNotification(notifId);
            StateHasChanged();
        });

        await hubConnection.StartAsync();
    }
}
```

**Scaling Considerations:**

| Scenario | Max Connections | Solution |
|----------|-----------------|----------|
| Development | 10 | Default in-memory |
| Staging | 100 | Default in-memory |
| Production < 1K | 1K | Redis backplane |
| Production 1K-10K | 10K | Azure SignalR Service |
| Production > 10K | 100K+ | Azure SignalR Premium + partitioning |

**Consequences:**
- **Positive:**
  - Native to ASP.NET Core (no external dependencies)
  - Automatic fallback: WebSocket → Server-Sent Events → Long Polling
  - Bi-directional communication
  - Excellent Blazor integration
  - Built-in reconnection logic
  
- **Negative:**
  - Stateful (server keeps connections in memory)
  - Doesn't scale horizontally without Redis backplane
  - Higher resource consumption than HTTP
  - Complexity in multi-server deployments

**Scaling Plan:**
- **Phase 5-6:** In-memory (single server)
- **Phase 7:** Redis backplane for load balancing
- **Phase 8+:** Azure SignalR Service for managed scale

**Alternatives Considered:**
1. **Server-Sent Events:** Simpler but one-way only
2. **Polling:** Simple but network inefficient
3. **gRPC:** Faster but only for modern browsers

---

### ADR-006: Entity Framework Core + Raw SQL Hybrid

**Status:** Accepted (Phase 1)  
**Date:** 2026-05-15

**Context:**
Need balance between ORM convenience and query performance. Decisions:
- Pure EF Core: Simple but queries less optimized
- Pure Raw SQL: Performant but verbose, hard to maintain
- Hybrid: Best of both

**Decision:**
Use EF Core for CRUD operations and raw SQL for complex queries.

```csharp
// EF Core for simple CRUD
public async Task<Expense> GetExpenseAsync(int expenseId)
{
    return await _context.Expenses
        .Where(e => e.ExpenseId == expenseId)
        .FirstOrDefaultAsync();
}

// Raw SQL for complex queries (analytics, reports)
public async Task<DataTable> GetExpensesByDepartmentAsync(
    int tenantId, DateTime startDate, DateTime endDate)
{
    var sql = @"
        SELECT 
            d.DepartmentName,
            COUNT(*) as ExpenseCount,
            SUM(e.Amount) as TotalAmount,
            AVG(e.Amount) as AvgAmount,
            MAX(e.Amount) as MaxAmount
        FROM dbo.Expenses e
        INNER JOIN dbo.TeamMembers tm ON e.UserId = tm.UserId
        INNER JOIN dbo.Departments d ON tm.DepartmentId = d.DepartmentId
        WHERE e.TenantId = @TenantId
            AND e.CreatedAt >= @StartDate
            AND e.CreatedAt < @EndDate
        GROUP BY d.DepartmentName
        ORDER BY TotalAmount DESC
    ";

    return await _db.QueryAsync(sql, new()
    {
        ["TenantId"] = tenantId,
        ["StartDate"] = startDate,
        ["EndDate"] = endDate
    });
}
```

**Query Patterns:**

| Pattern | When to Use | Example |
|---------|------------|---------|
| EF Core simple | Single entity read | `GetExpenseAsync(id)` |
| EF Core Include | Navigation properties | `Expenses.Include(e => e.ApprovalChain)` |
| EF Core Where | Filtering | `Expenses.Where(e => e.Status == "Pending")` |
| Raw SQL | Aggregations | SUM, COUNT, GROUP BY |
| Raw SQL | Joins > 3 tables | Complex domain logic |
| Raw SQL | Performance critical | Dashboard queries |
| Stored Procedures | Very complex | Complex calculation or legacy |

**Consequences:**
- **Positive:**
  - Query flexibility (best tool for each job)
  - Performance-optimized (raw SQL for heavy queries)
  - Maintainable (EF Core for simple ops)
  - Type-safe (parameterized queries prevent SQL injection)
  
- **Negative:**
  - Inconsistency in query patterns
  - Developers need SQL knowledge
  - Raw SQL harder to refactor
  - Version-specific SQL syntax issues

**Best Practices:**
- Parameterize all queries (never string concatenation)
- Add indexes for frequently queried columns
- Query results should be read-only (no in-memory updates)
- Complex logic belongs in services, not SQL

**Alternatives Considered:**
1. **Pure EF Core:** Less control over queries, harder to optimize
2. **Dapper:** Lower-level ORM, more SQL required
3. **Stored Procedures:** Centralized but version control complexity

---

### ADR-007: Role-Based Access Control (RBAC)

**Status:** Accepted (Phase 2)  
**Date:** 2026-08-01

**Context:**
Need fine-grained authorization (employee vs manager vs admin). Options:
- Role-based access control (RBAC)
- Attribute-based access control (ABAC)
- Policy-based authorization

**Decision:**
Implement policy-based authorization (PBAC) built on roles.

```csharp
// Define authorization policies
services.AddAuthorization(options =>
{
    // Role-based policies
    options.AddPolicy("AdminOnly", policy =>
        policy.RequireRole("Admin"));
    
    options.AddPolicy("CanApprove", policy =>
        policy.RequireClaim("Roles", "Manager", "Director", "CFO", "Admin"));
    
    // Custom policies
    options.AddPolicy("CanViewDashboard", policy =>
        policy.Requirements.Add(new DashboardViewRequirement()));
    
    options.AddPolicy("CanExportReport", policy =>
        policy.Requirements.Add(new ReportExportRequirement()));
});

// Register handlers
services.AddScoped<IAuthorizationHandler, DashboardViewHandler>();
services.AddScoped<IAuthorizationHandler, ReportExportHandler>();
```

**Role Hierarchy:**

```
System Admin (Global)
├── Can manage all tenants
├── Can manage users
└── Can view all reports

Tenant Admin (Per-tenant)
├── Can manage organization settings
├── Can manage users within tenant
├── Can view all expenses
└── Can view all reports

CFO (Department-wide)
├── Can view all department expenses
├── Can approve expenses (if in chain)
├── Can view department dashboard
└── Can export reports

Manager/Team Lead (Team-scoped)
├── Can view team expenses
├── Can approve team member expenses
├── Can view team dashboard
└── Can view team reports

Employee (Self + delegated)
├── Can view own expenses
├── Can submit expenses
├── Can approve delegated expenses
└── Cannot view others' expenses

Guest (Read-only)
├── Can view shared reports
└── Cannot modify anything
```

**Usage in Controllers:**

```csharp
[ApiController]
[Route("api/[controller]")]
public class ExpenseController : ControllerBase
{
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetExpense(int id)
    {
        var tenantId = User.GetTenantId();
        var userId = User.GetUserId();
        
        var expense = await _expenseService.GetExpenseAsync(
            tenantId, id);
        
        // Check authorization
        if (!await AuthorizeAsync("CanViewExpense", 
            new { ExpenseId = id, UserId = userId }))
        {
            return Forbid();
        }
        
        return Ok(expense);
    }
    
    [HttpPost("{id}/approve")]
    [Authorize(Policy = "CanApprove")]
    public async Task<IActionResult> ApproveExpense(int id)
    {
        // CanApprove policy enforced automatically
        var result = await _approvalService.ApproveAsync(id);
        return Ok(result);
    }
}
```

**Consequences:**
- **Positive:**
  - Fine-grained control (custom policies)
  - Flexible (easy to add new permissions)
  - Declarative (attributes on controllers)
  - Testable (mock authorization handlers)
  
- **Negative:**
  - More complex than simple role checks
  - Requires understanding of policy system
  - Custom handlers require testing
  - Performance impact of handler evaluation

**Alternatives Considered:**
1. **Simple [Authorize(Roles = "Admin")]:** Too inflexible for complex requirements
2. **ABAC:** More powerful but harder to reason about
3. **Row-level security (RLS):** Database-level but harder to test

---

### ADR-008: Caching Strategy (Multi-Layer)

**Status:** Accepted (Phase 6)  
**Date:** 2026-11-15

**Context:**
Dashboard queries expensive (complex joins, aggregations). Need caching strategy.

**Decision:**
Implement 3-layer caching: In-memory → Redis → Database

**Architecture:**

```
Request
  ↓
┌──────────────────────────────────────────┐
│ Layer 1: In-Memory Cache (IMemoryCache) │  TTL: 1-5 min
│ - Dashboard data                         │  Size: 500MB
│ - Frequently accessed data               │
└───────────────┬──────────────────────────┘
                │ (Miss)
                ↓
┌──────────────────────────────────────────┐
│ Layer 2: Redis Cache                    │  TTL: 30 min
│ - Cross-server cache                     │  Size: 10GB
│ - Session state (if needed)              │
└───────────────┬──────────────────────────┘
                │ (Miss)
                ↓
┌──────────────────────────────────────────┐
│ Layer 3: Database                       │  Source of truth
│ - SQL queries (materialized views)       │
│ - Query results                          │
└──────────────────────────────────────────┘
```

**Implementation:**

```csharp
// Caching service
public class CachingService
{
    private readonly IMemoryCache _memoryCache;
    private readonly IDistributedCache _redisCache;
    private readonly DatabaseService _db;

    public async Task<DashboardData> GetDashboardAsync(
        int tenantId, int userId, CacheDuration duration = CacheDuration.Medium)
    {
        var cacheKey = $"dashboard_{tenantId}_{userId}";
        
        // Layer 1: In-memory cache
        if (_memoryCache.TryGetValue(cacheKey, out DashboardData? cached))
        {
            _logger.LogDebug("Cache hit (memory): {Key}", cacheKey);
            return cached!;
        }
        
        // Layer 2: Redis cache
        var redisData = await _redisCache.GetStringAsync(cacheKey);
        if (!string.IsNullOrEmpty(redisData))
        {
            var data = JsonSerializer.Deserialize<DashboardData>(redisData);
            
            // Also populate in-memory cache
            _memoryCache.Set(cacheKey, data, TimeSpan.FromMinutes(5));
            
            _logger.LogDebug("Cache hit (redis): {Key}", cacheKey);
            return data!;
        }
        
        // Layer 3: Database query (materialized view)
        var dashboard = await _db.GetDashboardDataAsync(tenantId, userId);
        
        // Cache at both layers
        var cacheOptions = duration switch
        {
            CacheDuration.Short => TimeSpan.FromMinutes(1),
            CacheDuration.Medium => TimeSpan.FromMinutes(5),
            CacheDuration.Long => TimeSpan.FromMinutes(30),
            _ => TimeSpan.FromMinutes(5)
        };
        
        _memoryCache.Set(cacheKey, dashboard, cacheOptions);
        await _redisCache.SetStringAsync(cacheKey, 
            JsonSerializer.Serialize(dashboard), 
            new DistributedCacheEntryOptions 
            { 
                AbsoluteExpirationRelativeToNow = cacheOptions 
            });
        
        _logger.LogDebug("Cache miss (database): {Key}", cacheKey);
        return dashboard;
    }

    public async Task InvalidateDashboardCacheAsync(int tenantId)
    {
        // Invalidate all users' dashboard for this tenant
        var pattern = $"dashboard_{tenantId}_*";
        
        // In-memory cache: Can't pattern-match, so remove all known keys
        // (In real app, maintain a list of active keys per tenant)
        
        // Redis cache: Pattern delete
        var server = _redisCache.GetServer();
        var keys = server.Keys(pattern: pattern);
        foreach (var key in keys)
        {
            await _redisCache.RemoveAsync(key);
        }
        
        _logger.LogInformation("Invalidated dashboard cache for tenant {TenantId}", 
            tenantId);
    }
}

public enum CacheDuration
{
    Short = 1,      // 1 minute
    Medium = 5,     // 5 minutes
    Long = 30       // 30 minutes
}
```

**Cache Invalidation Events:**

```csharp
// When expense approved, invalidate relevant caches
public class ExpenseApprovedHandler : IEventHandler<ExpenseApprovedEvent>
{
    public async Task Handle(ExpenseApprovedEvent @event)
    {
        // Invalidate caches that reference this expense
        await _cachingService.InvalidateDashboardCacheAsync(@event.TenantId);
        await _cachingService.InvalidateExpenseListCacheAsync(
            @event.TenantId, @event.ExpenseId);
        
        // Then handle event
        await _notificationService.NotifyAsync(@event);
    }
}
```

**Materialized Views (Database-Level Caching):**

```sql
-- Create materialized view for dashboard summary
CREATE VIEW dbo.v_DashboardSummary
WITH SCHEMABINDING
AS
SELECT 
    e.TenantId,
    COUNT(DISTINCT e.ExpenseId) as TotalExpenses,
    SUM(e.Amount) as TotalAmount,
    COUNT(CASE WHEN e.Status = 'Pending' THEN 1 END) as PendingCount,
    COUNT(CASE WHEN e.Status = 'Approved' THEN 1 END) as ApprovedCount
FROM dbo.Expenses e
GROUP BY e.TenantId;

-- Refresh every 5 minutes
CREATE INDEX idx_DashboardSummary ON v_DashboardSummary (TenantId);
```

**Consequences:**
- **Positive:**
  - Significant performance improvement (10-100x faster)
  - Reduced database load
  - Better user experience (fast dashboards)
  - Scales to 10K+ concurrent users
  
- **Negative:**
  - Complexity in cache invalidation
  - Stale data window (until cache expires)
  - Requires Redis for multi-server deployments
  - Memory overhead
  - Cache coordination issues in distributed systems

**Cache Invalidation Strategies:**
1. **TTL-based:** Simple, eventual consistency (OK for dashboards)
2. **Event-based:** Immediate but requires event publishing (used here)
3. **Manual:** Admin dashboard to clear caches (for emergencies)

**Monitoring:**
- Cache hit rate (target: >80%)
- Cache size (alert if >90% of limit)
- Cache staleness (alert if TTL errors)
- Event-based invalidation latency (target: <100ms)

**Alternatives Considered:**
1. **Redis only:** Simpler but higher latency than in-memory
2. **Database view + no additional caching:** Slower queries
3. **Eager loading (pre-compute):** Wastes resources when data doesn't change

---

## Integration Architecture

### ADR-009: Third-Party Integrations (MYOB, Xero)

**Status:** Accepted (Phase 3)  
**Date:** 2026-09-01

**Decision:**
Implement REST-based integrations with circuit breakers and retry logic.

```csharp
// Integration facade
public class XeroIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly IAsyncPolicy<HttpResponseMessage> _resiliencePolicy;

    public XeroIntegrationService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        
        // Implement resilience (retry + circuit breaker)
        var retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TimeoutRejectedException>()
            .OrResult<HttpResponseMessage>(r => 
                (int)r.StatusCode >= 500 || 
                (int)r.StatusCode == 429)  // 429 = rate limit
            .WaitAndRetryAsync(
                retryCount: 3,
                sleepDurationProvider: attempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    _logger.LogWarning(
                        "Retry {RetryCount} after {DelayMs}ms for {Url}",
                        retryCount, timespan.TotalMilliseconds, context["url"]);
                });

        var circuitBreakerPolicy = Policy
            .Handle<HttpRequestException>()
            .OrResult<HttpResponseMessage>(r => (int)r.StatusCode >= 500)
            .CircuitBreakerAsync<HttpResponseMessage>(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (outcome, duration) =>
                {
                    _logger.LogError(
                        "Circuit breaker opened for {Duration}s", 
                        duration.TotalSeconds);
                });

        _resiliencePolicy = Policy.WrapAsync(retryPolicy, circuitBreakerPolicy);
    }

    public async Task<Invoice> CreateInvoiceAsync(int tenantId, Invoice invoice)
    {
        var context = new Polly.Context { ["url"] = "POST /invoices" };
        
        var response = await _resiliencePolicy.ExecuteAsync(context, async ctx =>
        {
            var request = new HttpRequestMessage(HttpMethod.Post, 
                "https://api.xero.com/invoices")
            {
                Content = JsonContent.Create(invoice)
            };
            
            return await _httpClient.SendAsync(request);
        });

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Xero API error: {StatusCode} {Content}",
                response.StatusCode, 
                await response.Content.ReadAsStringAsync());
            throw new IntegrationException($"Xero API returned {response.StatusCode}");
        }

        return await response.Content.ReadAsAsync<Invoice>();
    }
}
```

**Fallback Strategies:**

| Integration | Primary | Fallback | Impact |
|-------------|---------|----------|--------|
| MYOB | Auto-post | Queue for manual review | Manual step added |
| Xero | Auto-import | Manual CSV import | 1-day delay |
| SendGrid | Email | SMTP | Slightly slower |
| Twilio | SMS | Email notification | Different medium |
| OpenAI | Receipt OCR | User manual entry | More user effort |

---

## Performance & Scalability

### ADR-010: Database Optimization Strategy

**Status:** Accepted (Phase 6)  
**Date:** 2026-11-01

**Indexing Strategy:**

```sql
-- Indexes on frequently queried columns
CREATE INDEX IX_Expenses_TenantId_Status 
    ON dbo.Expenses(TenantId, Status) 
    INCLUDE (Amount, CreatedAt);

CREATE INDEX IX_Expenses_TenantId_UserId 
    ON dbo.Expenses(TenantId, UserId) 
    INCLUDE (Status, CreatedAt);

CREATE INDEX IX_ApprovalChains_TenantId_ExpenseId 
    ON dbo.ApprovalChains(TenantId, ExpenseId, ApprovalStatus);

-- Filtered index for active records only
CREATE INDEX IX_Expenses_Active 
    ON dbo.Expenses(TenantId, CreatedAt DESC) 
    WHERE Status <> 'Deleted';
```

**Query Optimization Patterns:**

```sql
-- ❌ BAD: N+1 query (Cartesian product)
SELECT * FROM Expenses e, ApprovalChains a 
WHERE e.ExpenseId = a.ExpenseId
AND e.TenantId = @TenantId;

-- ✅ GOOD: JOIN with specific columns
SELECT 
    e.ExpenseId, e.Amount, e.Status,
    a.ApprovalId, a.ApproverId, a.Status as ApprovalStatus
FROM dbo.Expenses e
LEFT JOIN dbo.ApprovalChains a ON e.ExpenseId = a.ExpenseId
WHERE e.TenantId = @TenantId
AND e.CreatedAt >= DATEADD(DAY, -30, GETDATE())
ORDER BY e.CreatedAt DESC;

-- ✅ BEST: Materialized view + cache
SELECT * FROM dbo.v_ExpenseWithApprovals_Last30Days
WHERE TenantId = @TenantId;
```

**Query Performance Targets:**

| Query Type | Target | Monitoring |
|-----------|--------|-----------|
| Single entity read | < 10ms | Query store |
| List (1000 items) | < 100ms | Application Insights |
| Dashboard (aggregated) | < 500ms (cached) | Custom metrics |
| Report (complex) | < 5s | Background job logs |
| Bulk operation (10K) | < 30s | Job duration |

---

## Security & Compliance

### ADR-011: Data Encryption Strategy

**Status:** Accepted (Enterprise)  
**Date:** 2026-06-01

**Encryption Layers:**

```
┌─────────────────────────────────────────┐
│ Layer 1: Transport (TLS 1.3)           │
│ - HTTPS for all API calls               │
│ - Certificate pinning (optional)        │
└──────────────┬──────────────────────────┘
               ↓
┌─────────────────────────────────────────┐
│ Layer 2: Application (AES-256)         │
│ - Sensitive fields encrypted in DB      │
│ - PII (phone, SSN, etc.)               │
└──────────────┬──────────────────────────┘
               ↓
┌─────────────────────────────────────────┐
│ Layer 3: Database (TDE)                │
│ - SQL Server Transparent Data Encryption│
│ - Entire database encrypted             │
└──────────────┬──────────────────────────┘
               ↓
        Physical Disk (at rest)
```

**Implementation:**

```csharp
public class EncryptionService
{
    private readonly IDataProtectionProvider _protectionProvider;

    public string Encrypt(string plaintext, string purpose)
    {
        var protector = _protectionProvider.CreateProtector(purpose);
        return protector.Protect(plaintext);
    }

    public string Decrypt(string ciphertext, string purpose)
    {
        var protector = _protectionProvider.CreateProtector(purpose);
        return protector.Unprotect(ciphertext);
    }
}

// Usage
services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"\\server\share\keys"))
    .ProtectKeysWithCertificate(cert);

// In entity
[Encrypted]
public string SocialSecurityNumber { get; set; }

[Encrypted]
public string PhoneNumber { get; set; }
```

---

## Monitoring & Observability

### ADR-012: Logging & Distributed Tracing

**Status:** Accepted (Operations)  
**Date:** 2026-07-01

**Log Levels Strategy:**

| Level | When to Use | Example |
|-------|------------|---------|
| **Fatal** | Application cannot continue | Database connection lost |
| **Error** | Operation failed, needs investigation | API call failed after retries |
| **Warning** | Degraded state but operational | Cache miss rate > 50% |
| **Info** | Business events | Expense submitted, approval granted |
| **Debug** | Development only | Query execution time |
| **Trace** | Very detailed (disabled in prod) | Object serialization |

**Implementation:**

```csharp
_logger.LogInformation(
    "Expense {ExpenseId} submitted by user {UserId} for amount {Amount} in tenant {TenantId}",
    expenseId, userId, amount, tenantId);

_logger.LogWarning(
    "High expense amount {Amount} for category {Category}, may require additional review",
    amount, category);

_logger.LogError(ex,
    "Failed to post expense {ExpenseId} to MYOB after {RetryCount} retries",
    expenseId, retryCount);
```

**Structured Logging (JSON):**

```json
{
  "timestamp": "2026-07-08T14:30:00Z",
  "level": "Information",
  "messageTemplate": "Expense {ExpenseId} submitted by user {UserId}",
  "properties": {
    "ExpenseId": 12345,
    "UserId": 100,
    "TenantId": 1,
    "Amount": 1250.50,
    "Category": "Meals",
    "RequestPath": "/api/expenses",
    "Duration": 245
  }
}
```

**Distributed Tracing:**

```csharp
// Add Application Insights
services.AddApplicationInsightsTelemetry();

// Automatic correlation
var activity = new Activity("CreateExpense").Start();

await _expenseService.CreateAsync(expense);
await _myobService.PostAsync(expense);  // Automatically linked

activity.Stop();
```

---

## Architecture Evolution

### Roadmap for Scale (100K+ users)

**Phase 7-8 (Q1-Q2 2027):**
- [ ] Migrate from Blazor Server → Blazor WASM for stateless architecture
- [ ] Implement CQRS pattern (separate read/write models)
- [ ] Migrate database to sharding strategy (by TenantId)
- [ ] Evaluate Kubernetes for container orchestration

**Phase 9-10 (Q2-Q3 2027):**
- [ ] Event sourcing for audit trail (instead of just logs)
- [ ] Implement saga pattern for long-running transactions
- [ ] GraphQL API for client flexibility
- [ ] Microservices (if growth warrants decomposition)

---

*For questions on architectural decisions, contact the Technical Lead.*
