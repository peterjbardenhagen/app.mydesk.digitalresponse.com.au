# MyDesk Implementation Agents

**Version:** 2.0  
**Last Updated:** July 2026  
**Target Audience:** AI agents, developers, implementation teams

---

## Overview

This document describes the specialized implementation agents responsible for building and maintaining MyDesk using 2026-standard Orchestrator-Worker agentic patterns. MyDesk uses a distributed agent architecture to manage complexity, avoid monolithic single-agent bottlenecks, and maintain security, memory efficiency, and audit compliance across multi-tenant approval workflows and integrations.

**Key Architectural Pattern:** Orchestrator-Worker (Manager delegates to stateless task agents via structured hand-offs)

**See also:**
- **Enterprise Architecture:** `ENTERPRISE-ARCHITECTURE.md` - System principles, deployment architecture, security model, compliance framework
- **Solution Architecture:** `SOLUTION-ARCHITECTURE.md` - Technical design, API patterns, database schema, development workflow, agentic patterns
- **Product Requirements:** `PRODUCT-REQUIREMENTS.md` - Feature specifications, acceptance criteria, data requirements
- **Product Strategy:** `PRODUCT-STRATEGY.md` - Market positioning, go-to-market, roadmap, financial projections
- **Development Guide:** `CLAUDE.md` - Local setup, build instructions, development best practices, agent development guidelines
- **Lessons Learned (CRITICAL):** `docs/LESSONS_LEARNED.md` - Common CI/CD failures, prevention checklists, best practices for service registration and Razor components

---

## Agentic Architecture Overview

### The Orchestrator-Worker Pattern

MyDesk implements a **distributed agentic architecture** to handle complex workflows safely and efficiently:

```
User Request (Expense Submitted)
        ↓
┌─────────────────────────────────────────┐
│  Orchestrator Agent (Manager)           │
│  - Receives intent: "ApprovalRequested" │
│  - Routes to correct worker agent       │
│  - Maintains conversation memory        │
│  - Verifies worker output               │
└─────────────────────────────────────────┘
        ↓
┌──────────────────┬──────────────────┬──────────────────┐
│ Approval Worker  │ Notification     │ Integration      │
│ - Validate       │ Worker           │ Worker (Future)  │
│   thresholds     │ - Draft emails   │ - Outlook sync   │
│ - Route to       │ - Queue SMS      │ - OneDrive scan  │
│   approvers      │ - Render HTML    │ - Xero mapping   │
│ - Check perms    │ - Track delivery │ - MYOB export    │
└──────────────────┴──────────────────┴──────────────────┘
```

### Key Agentic Patterns Used in MyDesk

| Pattern | Implementation | Use Case |
|---------|----------------|----------|
| **Router** | Logic-based (not LLM) intent classifier | Determine which worker handles request (approval? notification? integration?) |
| **Planner-Executor** | Approval workflow as multi-step sequence | Multi-approver chains with escalation |
| **Critic/Verifier** | Security and compliance gates | Validate PII before sending notifications, verify approver permissions |
| **Summarizer** | Background "Archivist" agent | Compress audit logs, cache frequently accessed data |

---

## Agent Architecture & Responsibilities

### Phase 4: Organizations & Teams Agent

**Owner:** Teams & Departments Implementation  
**Feature Areas:** Departments, Teams, Approval Delegation, Budget Management, Bulk Import  
**Status:** Complete (Feature Development)  
**Database:** Migrations 022 (Schema), 023 (Admin Users)

#### Responsibilities

The Phase 4 agent manages organizational structure, approval workflows, and budget controls:

| Component | Service | Responsibility |
|-----------|---------|-----------------|
| **Departments** | `DepartmentService` | Multi-level department hierarchy, manager assignment, cost center tracking |
| **Teams** | `TeamService` | Team creation, member management, approval team flagging |
| **Approvals** | `ApprovalDelegationService` | Delegation creation, threshold management, permission control |
| **Escalation** | `ApprovalEscalationService` | Route approvals to delegates, escalate to managers, build approval chains |
| **Budgets** | `BudgetService` | Allocate budgets, enforce spending limits, category tracking (Expense/Travel/Meals/Other) |
| **Bulk Import** | `BulkUserImportService` | CSV parsing, user creation, team/department assignment, validation |

#### Services & Methods

**DepartmentService.cs** (127 lines)
- `GetDepartmentsAsync(tenantId)` - List all departments with hierarchy
- `GetDepartmentAsync(tenantId, id)` - Get specific department details
- `CreateDepartmentAsync(tenantId, name, description, managerId, costCenter)` - Create new department
- `UpdateDepartmentAsync(tenantId, id, name, description, managerId)` - Update department
- `DeleteDepartmentAsync(tenantId, id)` - Archive/soft delete department

**TeamService.cs** (175 lines)
- `GetTeamsAsync(tenantId, departmentFilter)` - List teams by department
- `GetTeamAsync(tenantId, id)` - Get team details
- `CreateTeamAsync(tenantId, deptId, name, description, leadId)` - Create team
- `UpdateTeamAsync(tenantId, id, name, description)` - Update team
- `AddTeamMemberAsync(tenantId, teamId, userId, role)` - Add user to team
- `RemoveTeamMemberAsync(tenantId, teamId, userId)` - Remove user from team
- `GetTeamMembersAsync(tenantId, teamId)` - List team members with roles
- `GetUserTeamsAsync(tenantId, userId)` - Get teams user belongs to

**ApprovalDelegationService.cs** (196 lines)
- `CreateDelegationAsync(tenantId, teamId, fromId, toId, module, minAmount, maxAmount, startDate, endDate, perms...)` - Create delegation
- `GetActiveDelegatesAsync(tenantId, userId, module)` - Get active delegates for user
- `GetDelegationAsync(tenantId, id)` - Get delegation details
- `CanApproveAsync(tenantId, delegateId, amount)` - Check if delegate can approve amount
- `GetUserDelegationsAsync(tenantId, userId)` - List user's delegations (as delegator or delegatee)
- `DeactivateDelegationAsync(tenantId, id)` - Deactivate delegation

**ApprovalEscalationService.cs** (189 lines)
- `ResolveApprovalChainAsync(tenantId, userId, amount, module)` - Build complete approval chain with delegates
- `RouteApprovalAsync(tenantId, approverId, amount, module)` - Route to delegate or escalate to manager
- `NotifyDelegateAsync(tenantId, delegateId, approvalId)` - Send delegation notification (Phase 5)
- `NotifyEscalationAsync(tenantId, approverId, approvalId)` - Send escalation notification (Phase 5)

**BudgetService.cs** (176 lines)
- `GetBudgetAsync(tenantId, deptId, year)` - Get budget for fiscal year
- `CreateBudgetAsync(tenantId, deptId, year, allocated, allowOverspend, thresholdPercent)` - Allocate budget
- `CanApproveAsync(tenantId, deptId, amount)` - Check if approval would exceed budget
- `GetRemainingBudgetAsync(tenantId, deptId)` - Calculate available funds
- `GetBudgetAlertPercentageAsync(tenantId, deptId)` - Calculate usage percentage
- `AddExpenseAsync(tenantId, deptId, amount)` - Record expense against budget
- `EncumberAmountAsync(tenantId, deptId, amount)` - Reserve amount for pending approval
- `GetDepartmentBudgetsAsync(tenantId, deptFilter)` - List budgets with filters

**BulkUserImportService.cs** (266 lines)
- `ImportUsersAsync(tenantId, importedById, stream, filename)` - Process CSV upload
  - Parse CSV with quote handling and header validation
  - Map to user records with department/team assignment
  - Handle role assignment (Member, Lead, Manager)
  - Log import results with success/failure counts

#### Blazor Components

| Component | Purpose | Location |
|-----------|---------|----------|
| **DepartmentsList.razor** | Department management with CRUD and filtering | `/admin/departments` |
| **DepartmentEditDialog.razor** | Create/edit department modal | Shared modal |
| **TeamsList.razor** | Team management with department filtering | `/admin/teams` |
| **TeamEditDialog.razor** | Create/edit team modal | Shared modal |
| **TeamMembersDialog.razor** | Manage team membership dialog | Shared modal |
| **ApprovalDelegationManager.razor** | View/manage delegations (As Delegator + Received tabs) | `/admin/approvals/delegations` |
| **ApprovalDelegationDialog.razor** | Create/edit delegation modal | Shared modal |
| **BudgetManager.razor** | Budget tracking with fiscal year and department filters | `/admin/budgets` |
| **BudgetEditDialog.razor** | Create/edit budget allocation modal | Shared modal |
| **BulkUserImportDialog.razor** | CSV file upload with progress tracking | Shared modal |

#### Database Integration

**Tables Created (Migration 022):**
- `Departments` - Multi-level hierarchy with parent references
- `Teams` - Team records scoped to departments
- `TeamMembers` - User membership in teams with roles
- `ApprovalDelegation` - Delegate authority with amount thresholds and permissions
- `DepartmentBudgets` - Budget allocation and tracking with category breakdown
- `BulkUserImportLog` - Audit trail of all imports

**Indexes:**
- `IX_Departments_TenantId` - Fast tenant filtering
- `IX_Teams_DepartmentId` - Fast team lookup by department
- `IX_ApprovalDelegation_Active` - Fast active delegation queries
- `IX_DepartmentBudgets_FiscalYear` - Fast budget queries by year

#### Administrator Users (Migration 023)

Two tenant administrators created with full privileges:
- **Peter Bardenhagen** (peterb@digitalresponse.com.au) - CEO, User ID 1000
- **John Bardenhagen** (johnb@digitalresponse.com.au) - CFO, User ID 1001

Both configured as Tenant Directors with all administrative privileges.

#### Integration Points

**With Expense Module:**
- Budget enforcement on expense approval
- Expense routing through approval delegation chain
- Category-based budget tracking (Expense, Travel, Meals, Other)

**With Approval Workflow:**
- Delegation-based approval routing (Phase 4)
- Escalation to manager for amounts exceeding delegate limits
- Approval chain resolution with multiple approvers
- Delegation history and audit trail

**With Notifications (Phase 5):**
- Notify delegates when approval routed to them
- Notify on escalation events
- Budget threshold alerts

#### Testing

**Unit Tests:** 6 test files with >50 test cases covering:
- Department CRUD operations with hierarchy
- Team management and member operations
- Budget allocation and enforcement
- Delegation threshold validation
- Approval routing and escalation logic
- CSV import with validation and error handling

**Integration Tests:** Verify multi-tenant isolation, budget enforcement, delegation activation/expiration, cascade operations

#### Configuration

**Feature Flags:**
- `EnableApprovalDelegation` - Show delegation UI
- `EnableBudgetTracking` - Enforce budget controls
- `EnableTeamsFeature` - Show teams in navigation

**Budget Settings:**
- Default threshold alert: 80% of allocated
- Support overspend allowance per department
- Category tracking with optional sub-budgets

**Delegation Rules:**
- Min/max amount thresholds
- Time-based validity (start/end dates)
- Granular permissions (approve, reject, delegate, comment)
- Support for null ModuleType (applies to all modules)

#### Security Considerations

- All queries filtered by TenantId
- Department/Team management restricted to Administrators
- Delegation creation restricted to approval authority
- Budget editing restricted to Finance or Admin roles
- User passwords hashed (BCrypt)
- Audit trail of all modifications

#### Performance

- Department lists cached (5 min TTL)
- Team lists cached by department (5 min TTL)
- Active delegations cached (10 min TTL)
- Budget threshold calculations on-demand
- Supports 1000s of departments/teams without performance degradation

---

## Security & Memory Management for Agents

### PII Filtering (Day 5 Implementation)

Before any agent processes user data, apply PII sanitization:

```csharp
public class PiiFilterService
{
    // Regex patterns for PII detection
    private static readonly Regex EmailRegex = new(@"[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}");
    private static readonly Regex PhoneRegex = new(@"\b(\+61|0)[0-9\s\-\.]{7,}\b");
    private static readonly Regex ABNRegex = new(@"\b[0-9]{11}\b");
    
    public string FilterPii(string text, string context = "general")
    {
        // Strip PII before sending to LLM agents
        text = EmailRegex.Replace(text, "[EMAIL_REDACTED]");
        text = PhoneRegex.Replace(text, "[PHONE_REDACTED]");
        text = ABNRegex.Replace(text, "[ABN_REDACTED]");
        
        // Log PII removal to audit trail (for compliance)
        _auditService.LogAsync("PiiFiltered", context, text);
        
        return text;
    }
}
```

**Applied to:**
- Expense descriptions before sending to Approval Worker
- Email bodies before sending to Notification Worker
- File contents before sending to Integration Worker (OneDrive)

### Auth Middleware (Day 5 Implementation)

All agent calls routed through backend auth:

```csharp
app.Use(async (context, next) =>
{
    // Verify tenant_id claim
    var tenantId = context.User.FindFirst("tenant_id")?.Value;
    if (string.IsNullOrEmpty(tenantId))
    {
        context.Response.StatusCode = 401;
        await context.Response.WriteAsync("Unauthorized: Missing tenant_id");
        return;
    }
    
    // All agent invocations logged to ComplianceAuditLog
    var agentRequest = new { agent = context.Request.Path, tenantId, timestamp = DateTime.UtcNow };
    await _auditService.LogAsync("AgentInvoked", "System", agentRequest);
    
    await next();
});
```

**Never:** Have client call LLM API directly with raw keys
**Always:** Route through backend to enable token logging and PII filtering

### Memory Management (Day 4 Implementation)

Implement "Summarizer Agent" to prevent token explosion:

```csharp
public class ArchiverAgent
{
    private const int TokenThreshold = 10000;
    
    public async Task<string> SummarizeConversationAsync(string conversationHistory)
    {
        if (CountTokens(conversationHistory) < TokenThreshold)
            return conversationHistory; // No compression needed yet
        
        // Invoke "Archivist" worker to summarize
        var summary = await _summarizerWorker.SummarizeAsync(conversationHistory);
        
        // Store summary + recent messages (last 5) in context
        var compressedState = new
        {
            summary = summary,
            recentMessages = conversationHistory.Split('\n').TakeLast(5),
            originalLength = conversationHistory.Length,
            compressedAt = DateTime.UtcNow
        };
        
        // Cache in Redis for fast retrieval
        await _cache.SetAsync($"agent_state:{approvalId}", compressedState, TimeSpan.FromHours(24));
        
        return JsonSerializer.Serialize(compressedState);
    }
}
```

---

## Orchestrator Agent (System Router)

**Agentic Role:** Manager/Orchestrator—receives all user intents, routes to appropriate workers, verifies worker output before returning to client.

**Responsibility:** Intent classification and routing, memory management, security validation.

**Implementation:** Logic-based router (not LLM-based) to minimize latency and cost.

```csharp
public class OrchestratorAgent
{
    public async Task<ApiResponse> RouteAsync(string intent, UserContext user, object payload)
    {
        // Step 1: Classify intent (Router pattern)
        var classification = ClassifyIntent(intent);
        
        // Step 2: Validate security (Critic pattern)
        ValidateUserPermissions(user, classification);
        
        // Step 3: Route to appropriate worker (stateless hand-off)
        var workerResult = classification.Type switch
        {
            "approval_requested" => await _approvalWorker.ProcessAsync(payload, user),
            "notification_trigger" => await _notificationWorker.ProcessAsync(payload, user),
            "integration_sync" => await _integrationWorker.ProcessAsync(payload, user),
            _ => throw new InvalidOperationException($"Unknown intent: {intent}")
        };
        
        // Step 4: Verify output (Critic pattern)
        VerifyWorkerOutput(workerResult, classification);
        
        // Step 5: Return to client
        return new ApiResponse { Success = true, Data = workerResult };
    }
    
    private void ValidateUserPermissions(UserContext user, ClassificationResult classification)
    {
        // Ensure user has permission for this action
        // E.g., can user submit expense? Can user approve?
        if (!user.Roles.Contains(classification.RequiredRole))
            throw new UnauthorizedAccessException($"User lacks {classification.RequiredRole} role");
    }
}
```

**Intent Types Handled:**
- `approval_requested` → Approval Workflow Worker
- `notification_trigger` → Notification Service Worker
- `integration_sync` → Integration Worker (future)
- `user_profile_update` → Photo Processing Worker
- `expense_submitted` → Expense Management + Approval Workflow Workers (chain)

---

## Backend Service Agents

### 1. Authentication & Authorization Agent

**Responsibility:** Implement authentication flows, JWT token management, role-based access control, multi-tenant isolation.

**Reference Documents:**
- SOLUTION-ARCHITECTURE.md § Security Implementation
- ENTERPRISE-ARCHITECTURE.md § Authentication & Authorization
- PRODUCT-REQUIREMENTS.md § Security Requirements

**Key Tasks:**
- Domain-based tenant lookup on login
- Password hashing with BCrypt
- JWT creation and validation
- Tenant_id claim enforcement on all API endpoints
- Row-level security via database queries

**Code Locations:**
- Controllers: `src/MyDesk.Web/Controllers/AuthController.cs`
- Services: `src/MyDesk.Web/Services/AuthenticationService.cs`
- Migrations: `src/Deployment/Migration/002_users_and_auth.sql`

**Acceptance Criteria:**
- ✅ User logs in with corporate email → auto-assigned to correct tenant
- ✅ JWT claims include tenant_id, user_id, roles
- ✅ Invalid credentials → 401 with rate limiting
- ✅ Token expiration after 1 hour
- ✅ Row-level queries filtered by TenantId

---

### 2. Expense Management Agent

**Responsibility:** Implement expense creation, submission, receipt processing, status tracking, and approval workflow integration.

**Reference Documents:**
- SOLUTION-ARCHITECTURE.md § Event-Driven Workflows
- PRODUCT-REQUIREMENTS.md § Phase 2 & 5 (Expense Management, Enhancements)
- ENTERPRISE-ARCHITECTURE.md § Approval permissions & threshold-based authority

**Key Tasks:**
- Expense CRUD operations
- Automatic approval chain determination based on amount
- Receipt OCR integration (via DocumentExtractionService)
- Photo processing for receipt storage
- Status transitions (Draft → Submitted → Pending Approval → Approved → Paid)
- Immutable audit logging

**Code Locations:**
- Controllers: `src/MyDesk.Web/Controllers/ExpenseController.cs`
- Services: `src/MyDesk.Web/Services/ExpenseService.cs`
- Migrations: `src/Deployment/Migration/009_expenses.sql` & `020_expense_receipts.sql`

**Acceptance Criteria:**
- ✅ Expense amount < $500 → auto-approved
- ✅ Expense $500-$5000 → routes to manager
- ✅ Expense > $5000 → routes to director
- ✅ Receipt photo extracted automatically (supplier, date, amount)
- ✅ User can edit extracted fields
- ✅ Full immutable audit trail per ComplianceAuditLog

---

### 3. Approval Workflow Agent (Worker)

**Agentic Role:** Worker agent—stateless, handles approval chain logic, invoked by Orchestrator.

**Responsibility:** Implement approval request creation, approval actions, escalation, delegation, and audit tracking.

**Reference Documents:**
- SOLUTION-ARCHITECTURE.md § Event-Driven Workflows, Agentic Patterns
- ENTERPRISE-ARCHITECTURE.md § Segregation of Duties, Multi-Level Approvals
- PRODUCT-REQUIREMENTS.md § Phase 2 & 4 (Approvals, Hierarchies)

**Agentic Patterns Used:**
- **Planner-Executor:** Multi-step workflow (validate → route → escalate → notify)
- **Critic/Verifier:** Validate approver permissions before executing
- **Router:** Determine approval chain based on amount, category, department

**Key Tasks:**
- Create ApprovalRequest for each eligible approver
- Validate approver authority (threshold, department, category) via permission gate
- Approval action (approve/reject) with optional comment
- Check if all approvals complete
- Escalation if pending > 3 days (Phase 4) via Planner-Executor
- Delegation to deputy for out-of-office (Phase 4) with audit trail
- Trigger Notification Worker after approval decision (structured hand-off)

**Code Locations:**
- Controllers: `src/MyDesk.Web/Controllers/ApprovalController.cs`
- Services: `src/MyDesk.Web/Services/ApprovalService.cs`
- Migrations: `src/Deployment/Migration/010_approval_workflows.sql` & `011_approval_rules.sql`

**Acceptance Criteria:**
- ✅ Manager cannot approve expense > $5000 (permission check enforced)
- ✅ Submitter cannot be approver (segregation of duties)
- ✅ Rejection includes reason (audit trail)
- ✅ Approval triggers notification to submitter
- ✅ All approvals immutably logged

---

### 4. Notification Service Agent (Worker)

**Agentic Role:** Worker agent—stateless, receives notification hand-off from Approval Workflow Worker, invokes independently.

**Responsibility:** Implement multi-channel notification delivery (Email, SMS, In-App), preference management, template substitution, and delivery queue management.

**Reference Documents:**
- SOLUTION-ARCHITECTURE.md § Notification Queue Pattern, Agentic Patterns
- PRODUCT-REQUIREMENTS.md § Phase 3 & Preferences
- ENTERPRISE-ARCHITECTURE.md § Notifications & Communication

**Agentic Patterns Used:**
- **Critic/Verifier:** Validate PII filtering before sending emails (regex + local model)
- **Executor:** Queue delivery and handle async retries with exponential backoff
- **Router:** Determine delivery channels based on user preferences and event type

**Key Tasks:**
- PII filter (email bodies, expense descriptions) before sending
- Send email notifications via SendGrid with template substitution
- Send in-app notifications with unread counting
- Template substitution ({{ApproverName}}, {{Amount}}, etc.)
- Respect user preferences (opt-out, quiet hours, digest frequency)
- Queue management with retry logic (1s, 2s, 4s, 8s, 16s exponential backoff)
- Delivery status tracking and failure handling (log to NotificationLog)

**Code Locations:**
- Services: `src/MyDesk.Web/Services/NotificationService.cs`
- Components: `src/MyDesk.Web/Components/NotificationBell.razor`
- Controllers: `src/MyDesk.Web/Controllers/NotificationController.cs`
- Migrations: `src/Deployment/Migration/021_notification_system.sql`

**Acceptance Criteria:**
- ✅ Approver notified within 5 seconds of expense submission
- ✅ User can opt-out of email notifications
- ✅ Quiet hours prevent notifications 10pm-6am
- ✅ Digest frequency delays emails until scheduled time
- ✅ Failed emails retry with exponential backoff (1s, 2s, 4s, 8s, 16s)
- ✅ In-app unread count accurate and updates in real-time

---

### 5. Photo Processing Agent

**Responsibility:** Implement photo upload, cropping, square conversion, compression, and storage for user avatars and expense receipts.

**Reference Documents:**
- SOLUTION-ARCHITECTURE.md § Scalability Patterns
- PRODUCT-REQUIREMENTS.md § Phase 2 (Photo Processing)
- ENTERPRISE-ARCHITECTURE.md § Blob Storage, Photo Management

**Key Tasks:**
- Validate image file (type, size, dimensions)
- Crop image to user-specified rectangle
- Convert to square with white padding
- Compress to appropriate quality level
- Generate thumbnail for list views
- Store in Azure Blob Storage
- Return URL for database linking

**Code Locations:**
- Services: `src/MyDesk.Web/Services/PhotoProcessingService.cs`
- Components: `src/MyDesk.Web/Components/Dialogs/PhotoUploadDialog.razor`
- Controllers: `src/MyDesk.Web/Controllers/PhotoController.cs`
- Migrations: `src/Deployment/Migration/019_user_profile_photos.sql`

**Acceptance Criteria:**
- ✅ Supported formats: JPEG, PNG
- ✅ Max file size: 5MB
- ✅ Output: 500x500px (profile) + 100x100px (thumbnail)
- ✅ User can crop before finalizing
- ✅ Photo compressed to < 1MB
- ✅ Photo appears as circular avatar in UI

---

## Frontend/UI Agents

### 6. Blazor Components Agent

**Responsibility:** Build interactive Blazor Server components for expenses, approvals, notifications, preferences, and user management.

**Reference Documents:**
- SOLUTION-ARCHITECTURE.md § Development Workflow
- PRODUCT-REQUIREMENTS.md § UI/UX Requirements
- ENTERPRISE-ARCHITECTURE.md § Deployment Architecture (Blazor Server)

**Key Tasks:**
- Expense list grid with sorting, filtering, pagination
- Expense detail view with approval history
- Approval queue with action buttons
- Notification bell with dropdown
- User profile with photo upload
- Settings page for preferences
- Responsive design (mobile, tablet, desktop)

**Code Locations:**
- Pages: `src/MyDesk.Web/Components/Pages/`
- Dialogs: `src/MyDesk.Web/Components/Dialogs/`
- Shared: `src/MyDesk.Web/Components/Shared/`

**Acceptance Criteria:**
- ✅ All features work on mobile (320px+)
- ✅ Touch-friendly buttons (44x44px minimum)
- ✅ Loading states and error handling
- ✅ Accessibility: WCAG 2.1 Level AA
- ✅ No console errors
- ✅ < 3 second page load time

---

### 7. Dashboard & Analytics Agent

**Responsibility:** Build dashboards for CFO, Manager, and Employee personas with charts, metrics, exports.

**Reference Documents:**
- PRODUCT-REQUIREMENTS.md § Phase 6 (Dashboard & Analytics)
- PRODUCT-STRATEGY.md § Success Metrics
- SOLUTION-ARCHITECTURE.md § Monitoring & Observability

**Key Tasks:**
- Executive dashboard: Total expenses, by department, by category, approval time
- Manager dashboard: Team expenses (pending, approved, paid), cost breakdown
- Employee dashboard: My expenses status, reimbursement timeline
- Export to CSV/PDF for reporting
- Charts: Bar, line, pie using MudBlazor
- Real-time updates via SignalR (future)

**Code Locations:**
- Pages: `src/MyDesk.Web/Components/Pages/Dashboard.razor`
- Services: `src/MyDesk.Web/Services/AnalyticsService.cs`

**Acceptance Criteria:**
- ✅ CFO dashboard shows total spent month-to-date
- ✅ Charts render in < 1 second
- ✅ Data refreshes every 5 minutes
- ✅ Export to CSV working
- ✅ Mobile-responsive charts

---

## Data & Integration Agents

### 8. Database Schema Agent

**Responsibility:** Design and implement SQL Server schema, migrations, indexes, and stored procedures.

**Reference Documents:**
- SOLUTION-ARCHITECTURE.md § Database Schema Design
- ENTERPRISE-ARCHITECTURE.md § Data Architecture
- PRODUCT-REQUIREMENTS.md § Data Requirements

**Key Tasks:**
- Create migration SQL files for each feature
- Design schema layers (Core, Workflows, Notifications, Compliance, Billing)
- Add appropriate indexes for performance
- Enforce referential integrity via foreign keys
- Immutable audit log table (no UPDATE/DELETE)
- Data retention policies (7-year archive)

**Code Locations:**
- Migrations: `src/Deployment/Migration/*.sql`
- Scripts: `src/Deployment/Scripts/`

**Acceptance Criteria:**
- ✅ All tables have TenantId column
- ✅ Foreign keys referencing Tenants(TenantId)
- ✅ Indexes on WHERE and JOIN columns
- ✅ ComplianceAuditLog is append-only
- ✅ Migrations idempotent (can run multiple times safely)

---

### 9. Integration Agent (Future)

**Responsibility:** Build integrations with external systems (Xero, MYOB, SendGrid, Twilio).

**Reference Documents:**
- PRODUCT-REQUIREMENTS.md § Integration Requirements
- PRODUCT-STRATEGY.md § Phase 8 (Integrations)

**Key Tasks (Phase 8, 2027):**
- Xero API integration (sync approved expenses as bills)
- MYOB integration (export format, scheduled sync)
- SendGrid webhook handling (delivery status updates)
- Twilio SMS integration (future notifications)
- Stripe/PayFast payment integration (future)

**Code Locations:**
- Services: `src/MyDesk.Web/Services/Integrations/`
- Controllers: `src/MyDesk.Web/Controllers/IntegrationController.cs`

**Acceptance Criteria:**
- ✅ Xero sync within 24 hours of approval
- ✅ SendGrid webhook updates NotificationLog
- ✅ Error handling with retry logic
- ✅ Audit log entry for each integration action

---

## Security & Compliance Agents

### 10. Security & Audit Agent

**Responsibility:** Implement security controls, audit logging, compliance checks, and vulnerability management.

**Reference Documents:**
- ENTERPRISE-ARCHITECTURE.md § Security Architecture
- SOLUTION-ARCHITECTURE.md § Security Implementation
- PRODUCT-REQUIREMENTS.md § Security & Compliance Requirements

**Key Tasks:**
- Input validation on all API endpoints
- SQL injection prevention (parameterized queries)
- XSS prevention (Blazor built-in HTML encoding)
- CSRF protection (anti-forgery tokens)
- Rate limiting implementation
- Audit log entries for all sensitive operations
- Encryption key management
- Vulnerability scanning and patching

**Code Locations:**
- Middleware: `src/MyDesk.Web/Middleware/`
- Services: `src/MyDesk.Web/Services/SecurityService.cs`

**Acceptance Criteria:**
- ✅ No SQL injection vulnerabilities (parameterized queries)
- ✅ No XSS vulnerabilities (HTML encoding)
- ✅ Rate limiting: max 100 requests/min per user
- ✅ Every data modification logged to ComplianceAuditLog
- ✅ Zero security incidents in production
- ✅ Passes OWASP Top 10 assessment

---

### 11. Compliance Certification Agent

**Responsibility:** Achieve and maintain compliance certifications (ISO 27001, SOC 2 Type II, Sarbanes-Oxley).

**Reference Documents:**
- ENTERPRISE-ARCHITECTURE.md § Compliance Implementation
- PRODUCT-STRATEGY.md § Compliance Timeline
- PRODUCT-REQUIREMENTS.md § Compliance Requirements

**Key Tasks (Year 1-2):**
- ISO 27001 certification (Q4 2026)
- SOC 2 Type II audit (Q2 2027)
- Sarbanes-Oxley readiness assessment
- Compliance documentation and evidence collection
- Internal audit preparation
- Vendor risk assessments
- Incident response plan testing

**Code Locations:**
- Docs: `docs/Compliance/`
- Policies: `docs/Policies/`

**Acceptance Criteria:**
- ✅ ISO 27001 certification issued
- ✅ SOC 2 Type II audit report issued
- ✅ Zero compliance findings
- ✅ Evidence of controls available for auditors

---

## DevOps & Infrastructure Agents

### 12. DevOps & Deployment Agent

**Responsibility:** Implement CI/CD pipeline, infrastructure-as-code, monitoring, alerting, and disaster recovery.

**Reference Documents:**
- SOLUTION-ARCHITECTURE.md § Build & Deployment
- ENTERPRISE-ARCHITECTURE.md § Deployment Architecture, Disaster Recovery
- PRODUCT-STRATEGY.md § Implementation Timeline

**Key Tasks:**
- GitHub Actions CI/CD pipeline
- Build, test, deploy to Azure App Service
- Infrastructure-as-Code (Azure Resource Manager templates)
- Database migration automation
- Backup and restore procedures
- Health checks and alerting
- Monitoring with Application Insights
- Disaster recovery testing

**Code Locations:**
- CI/CD: `.github/workflows/`
- Infrastructure: `infra/azure/`
- Scripts: `src/Deployment/Scripts/`

**Acceptance Criteria:**
- ✅ Pull request → Run tests → Merge → Deploy to staging → Deploy to production
- ✅ Automated database migrations on deploy
- ✅ Zero-downtime deployments
- ✅ Rollback capability if deployment fails
- ✅ 99.9% uptime SLA met monthly

---

### 13. Database Administration Agent

**Responsibility:** Manage SQL Server, backups, performance tuning, scaling, and disaster recovery.

**Reference Documents:**
- ENTERPRISE-ARCHITECTURE.md § Disaster Recovery, Performance
- SOLUTION-ARCHITECTURE.md § Database Performance

**Key Tasks:**
- Daily automated backups (35-day retention)
- Point-in-time recovery (PITR) testing
- Query performance tuning
- Index maintenance
- Growth planning (disk space, connections)
- Disaster recovery drills (quarterly)
- Always-On Availability Group failover testing
- Security patches and updates

**Code Locations:**
- Scripts: `src/Deployment/Scripts/backup.ps1`, `restore.ps1`
- Maintenance: `src/Deployment/Maintenance/`

**Acceptance Criteria:**
- ✅ Daily backups completed successfully
- ✅ Restore time tested quarterly
- ✅ RTO (Recovery Time Objective): 1 hour for critical systems
- ✅ RPO (Recovery Point Objective): 15 minutes (database), 1 hour (files)

---

## Quality Assurance Agents

### 14. Testing & QA Agent

**Responsibility:** Implement automated testing (unit, integration, e2e), test coverage, and quality metrics.

**Reference Documents:**
- SOLUTION-ARCHITECTURE.md § Development Workflow
- PRODUCT-REQUIREMENTS.md § Acceptance Criteria

**Key Tasks:**
- Unit tests for services (>80% coverage)
- Integration tests for API endpoints
- End-to-end tests for critical flows
- Performance testing
- Security testing (OWASP)
- Load testing (1000 concurrent users)
- Accessibility testing

**Code Locations:**
- Tests: `tests/MyDesk.Web.Tests/`
- Performance: `tests/Performance/`

**Acceptance Criteria:**
- ✅ Unit test coverage > 80%
- ✅ All critical flows have e2e tests
- ✅ No performance regressions
- ✅ Accessibility audit score > 90
- ✅ Zero high-severity vulnerabilities

---

## Documentation Agents

### 15. Documentation & Knowledge Agent

**Responsibility:** Maintain architecture documentation, API documentation, development guides, and knowledge base.

**Reference Documents:**
- ENTERPRISE-ARCHITECTURE.md
- SOLUTION-ARCHITECTURE.md
- PRODUCT-REQUIREMENTS.md
- PRODUCT-STRATEGY.md
- CLAUDE.md (Development Guide)

**Key Tasks:**
- Keep documentation in sync with code
- Write API documentation (OpenAPI/Swagger)
- Create architecture decision records (ADRs)
- Maintain knowledge base for support team
- Write runbooks for common tasks
- Document troubleshooting procedures

**Code Locations:**
- Docs: `docs/`
- API Docs: `src/MyDesk.Web/wwwroot/swagger/`

**Acceptance Criteria:**
- ✅ Architecture documentation reflects current system
- ✅ API endpoints documented with examples
- ✅ Deployment procedures documented
- ✅ Troubleshooting guides available

---

## Agent Collaboration Matrix (Orchestrator-Worker Pattern)

**Data Flow:**
```
Orchestrator (Router + Intent Classifier)
├── Auth Agent (validate user permissions)
├── Approval Workflow Worker (handle approval chain)
│   └── Notification Service Worker (send notifications)
├── Photo Processing Worker (process images)
├── Integration Worker (future: Outlook, OneDrive, Xero)
└── Dashboard Agent (aggregate and display)
```

| Role | Agent | Orchestrator | Workers It Invokes | Async/Blocking |
|------|-------|--------------|-------------------|-----------------|
| Orchestrator | System Router | Yes | Auth, Approval, Notification, Photo, Integration | Non-blocking (routes & verifies) |
| Worker | Approval Workflow | No | Structured hand-off to Notification | Non-blocking (uses background jobs) |
| Worker | Notification Service | No | SendGrid, database | Async (queue-based) |
| Worker | Photo Processing | No | Blob storage | Non-blocking (background job) |
| Worker | Integration (Future) | No | External APIs (Outlook, OneDrive) | Async (with retry logic) |
| Support | Dashboard Agent | No | Database queries | Async (aggregation) |

---

## UI/UX Patterns: Async Agent Operations (2026 Standard)

### The "Thinking..." State Pattern

When an agent is processing (approval routing, notification queuing, etc.), **never block the UI**. Show async state:

**Blazor Component Example:**
```razor
@page "/expenses/{expenseId}/submit"
@using MyDesk.Web.Services
@inject OrchestratorAgent Orchestrator

<MudCard>
    <h3>Submit Expense</h3>
    
    @if (isSubmitting)
    {
        <MudProgressLinear Indeterminate="true" />
        <p>Routing to approvers... <i class="fas fa-spinner fa-spin"></i></p>
    }
    else if (submissionComplete)
    {
        <MudAlert Severity="Success">
            Submitted to {{ApprovalChain}} for review
        </MudAlert>
    }
    else
    {
        <MudButton OnClick="SubmitAsync" Variant="Variant.Filled">Submit</MudButton>
    }
</MudCard>

@code {
    private bool isSubmitting = false;
    private bool submissionComplete = false;
    
    private async Task SubmitAsync()
    {
        isSubmitting = true;
        
        // Invoke orchestrator WITHOUT awaiting synchronously
        // Use background task so UI remains responsive
        var task = Orchestrator.RouteAsync("expense_submitted", userContext, expensePayload);
        
        try
        {
            await task; // Wait for result, but don't block rendering
            submissionComplete = true;
        }
        catch (Exception ex)
        {
            MudSnackbar.Add($"Error: {ex.Message}", Severity.Error);
        }
        finally
        {
            isSubmitting = false;
        }
    }
}
```

**Android/Compose Example:**
```kotlin
@Composable
fun ExpenseSubmitScreen(orchestrator: OrchestratorAgent) {
    var isSubmitting by remember { mutableStateOf(false) }
    var submissionMessage by remember { mutableStateOf("") }
    
    Column {
        if (isSubmitting) {
            LinearProgressIndicator(modifier = Modifier.fillMaxWidth())
            Text("Routing to approvers...", style = MaterialTheme.typography.bodyMedium)
        } else {
            Button(
                onClick = {
                    isSubmitting = true
                    // Launch coroutine so UI thread remains free
                    viewModelScope.launch {
                        try {
                            val result = orchestrator.routeAsync("expense_submitted", userContext, payload)
                            submissionMessage = "Submitted to approvers for review"
                        } catch (ex: Exception) {
                            submissionMessage = "Error: ${ex.message}"
                        } finally {
                            isSubmitting = false
                        }
                    }
                }
            ) {
                Text("Submit Expense")
            }
        }
        
        if (submissionMessage.isNotEmpty()) {
            Text(submissionMessage, style = MaterialTheme.typography.bodySmall)
        }
    }
}
```

### Real-Time Updates via SignalR (Future)

For multi-step agent operations (e.g., multi-approver chains), push status updates to UI:

```csharp
public class ApprovalHub : Hub
{
    public async Task SubscribeToApprovalAsync(int approvalId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"approval_{approvalId}");
    }
}

// In ApprovalWorkflowWorker:
await _hubContext.Clients.Group($"approval_{approvalId}")
    .SendAsync("ApprovalRouted", new {
        approvalId,
        approverId = approver.UserId,
        approverName = approver.Name,
        dueDate = DateTime.UtcNow.AddDays(3)
    });
```

---

## Agent Collaboration Matrix

---

## Development Workflow

### Code Review Checklist

All agents should ensure code changes meet these criteria:

**Security**
- [ ] No SQL injection vulnerabilities (parameterized queries)
- [ ] No XSS vulnerabilities (HTML encoding)
- [ ] No hardcoded secrets (use Key Vault)
- [ ] Rate limiting appropriate
- [ ] Tenant isolation verified

**Architecture**
- [ ] Follows SOLUTION-ARCHITECTURE patterns
- [ ] Stateless design maintained
- [ ] Async/await used for I/O
- [ ] No circular dependencies
- [ ] Logging at appropriate levels

**Testing**
- [ ] Unit tests written (>80% coverage)
- [ ] Edge cases tested
- [ ] Integration tests pass
- [ ] No performance regression
- [ ] Accessibility verified

**Documentation**
- [ ] Code comments for non-obvious logic only
- [ ] API endpoints documented
- [ ] Database schema documented
- [ ] Architecture decision recorded (if major)
- [ ] User-facing changes documented

**Compliance**
- [ ] Audit log entry created for sensitive operations
- [ ] Data validation applied
- [ ] Error handling implemented
- [ ] Sensitive data not logged
- [ ] GDPR requirements considered

---

## References

- **Enterprise Architecture:** `ENTERPRISE-ARCHITECTURE.md` - System principles, security, compliance, disaster recovery
- **Solution Architecture:** `SOLUTION-ARCHITECTURE.md` - Technical design, API patterns, database schema, development workflow
- **Product Requirements:** `PRODUCT-REQUIREMENTS.md` - Feature specifications, acceptance criteria, data requirements
- **Product Strategy:** `PRODUCT-STRATEGY.md` - Market positioning, go-to-market, roadmap, financial projections
- **Development Guide:** `CLAUDE.md` - Local setup, build instructions, development best practices

---

## Document Revision History

| Version | Date | Author | Notes |
|---------|------|--------|-------|
| 2.0 | 2026-07-05 | Claude | Integrated Orchestrator-Worker agentic patterns, PII filtering, auth middleware, memory management, async UI patterns, Router/Planner-Executor/Critic patterns |
| 1.0 | 2026-07-05 | Claude | Initial agent definitions and collaboration matrix |

