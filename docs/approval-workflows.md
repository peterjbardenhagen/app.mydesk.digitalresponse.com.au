# Approval Workflows Implementation Plan

## Overview
This feature adds manager approval workflows for Expenses and Timesheets, including multi-level delegation and audit trails.

## Database Schema Changes

### New Tables

#### 1. ApprovalWorkflows
```sql
CREATE TABLE ApprovalWorkflows (
    WorkflowId INT PRIMARY KEY IDENTITY(1,1),
    TenantId UNIQUEIDENTIFIER NOT NULL,
    ModuleType NVARCHAR(50) NOT NULL,  -- 'Expense', 'Timesheet'
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(MAX),
    IsDefault BIT DEFAULT 1,
    ApprovalLevels INT DEFAULT 1,      -- Single/dual approval
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2
)
```

#### 2. ApprovalRules
```sql
CREATE TABLE ApprovalRules (
    RuleId INT PRIMARY KEY IDENTITY(1,1),
    WorkflowId INT NOT NULL,
    Level INT NOT NULL,                -- 1, 2, 3...
    ApproverUserId INT,                -- Specific approver or
    ApproverRole NVARCHAR(100),        -- Role-based approval
    ThresholdAmount DECIMAL(18,2),     -- Approval required above this
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_ApprovalRules_Workflows FOREIGN KEY (WorkflowId) REFERENCES ApprovalWorkflows(WorkflowId)
)
```

#### 3. ApprovalRequests
```sql
CREATE TABLE ApprovalRequests (
    RequestId INT PRIMARY KEY IDENTITY(1,1),
    TenantId UNIQUEIDENTIFIER NOT NULL,
    WorkflowId INT NOT NULL,
    ModuleType NVARCHAR(50) NOT NULL,  -- 'Expense', 'Timesheet'
    ModuleId INT NOT NULL,             -- ExpenseId or TimesheetId
    CurrentLevel INT DEFAULT 1,
    Status NVARCHAR(50) DEFAULT 'Pending',  -- Pending, Approved, Rejected, Withdrawn
    SubmittedById INT NOT NULL,        -- Who submitted for approval
    SubmittedAt DATETIME2 NOT NULL,
    CompletedAt DATETIME2,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_ApprovalRequests_Workflows FOREIGN KEY (WorkflowId) REFERENCES ApprovalWorkflows(WorkflowId),
    CONSTRAINT FK_ApprovalRequests_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId) ON DELETE CASCADE
)
```

#### 4. ApprovalActions
```sql
CREATE TABLE ApprovalActions (
    ActionId INT PRIMARY KEY IDENTITY(1,1),
    RequestId INT NOT NULL,
    ApprovalLevel INT NOT NULL,
    ApprovedById INT,                  -- Who approved/rejected
    Action NVARCHAR(50) NOT NULL,      -- 'Approved', 'Rejected', 'Delegated'
    Comments NVARCHAR(MAX),
    DelegatedToUserId INT,             -- If action = 'Delegated'
    ActionAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_ApprovalActions_Requests FOREIGN KEY (RequestId) REFERENCES ApprovalRequests(RequestId) ON DELETE CASCADE
)
```

#### 5. ApprovalDelegations
```sql
CREATE TABLE ApprovalDelegations (
    DelegationId INT PRIMARY KEY IDENTITY(1,1),
    TenantId UNIQUEIDENTIFIER NOT NULL,
    ApproverUserId INT NOT NULL,       -- Original approver
    DelegateUserId INT NOT NULL,       -- Temporary delegate
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    ModuleType NVARCHAR(50),           -- NULL = all modules
    IsActive BIT DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_ApprovalDelegations_Tenants FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId) ON DELETE CASCADE
)
```

## API Endpoints

### Approval Workflows
```
GET /api/approval/workflows
  - List all approval workflows for tenant
  - Response: { workflows: [...], totalCount }

POST /api/approval/workflows
  - Create new workflow
  - Request: { name, description, approvalLevels, rules: [...] }
  - Response: { id, createdAt }

PUT /api/approval/workflows/{id}
  - Update workflow rules
  - Request: { rules: [...] }

DELETE /api/approval/workflows/{id}
  - Archive workflow (soft delete)
```

### Approval Requests
```
POST /api/expenses/{id}/submit-for-approval
  - Submit expense for approval
  - Request: { workflowId?, comments? }
  - Response: { requestId, currentApprover, dueBy }

POST /api/timesheets/{id}/submit-for-approval
  - Submit timesheet for approval
  - Request: { workflowId?, comments? }
  - Response: { requestId, currentApprover, dueBy }

GET /api/approval/pending
  - List pending approvals for current user
  - Response: { approvals: [...], count }

POST /api/approval/requests/{requestId}/approve
  - Approve request
  - Request: { comments? }
  - Response: { status, nextLevel, nextApprover? }

POST /api/approval/requests/{requestId}/reject
  - Reject request (returns to submitter)
  - Request: { reason }
  - Response: { status, notifiedTo }

POST /api/approval/requests/{requestId}/delegate
  - Delegate to another approver (temporary)
  - Request: { delegateUserId, until }
  - Response: { delegatedTo, validUntil }

GET /api/approval/requests/{requestId}/history
  - View approval history/audit trail
  - Response: { actions: [...], timeline }
```

### Approval Delegations
```
POST /api/approval/delegations
  - Create temporary delegation
  - Request: { delegateUserId, startDate, endDate, moduleType? }
  - Response: { delegationId, validPeriod }

GET /api/approval/delegations
  - List active delegations for user

DELETE /api/approval/delegations/{id}
  - Revoke delegation
```

## UI Components (Blazor)

### Manager Dashboard
- Pending approvals widget (count, quick links)
- Approval queue table (sortable, filterable)
- Quick approve/reject buttons with comment modal
- Delegation management panel

### Submitter Experience
- "Submit for Approval" button on Expense/Timesheet detail
- Workflow selection (if multiple available)
- Approval status display (badges showing approval level)
- Approval history view
- Option to withdraw if still pending

### Approver Interface
- Approval cards with full item details
- Decision buttons: Approve, Reject, Delegate
- Comments/notes field
- Audit trail of previous actions
- Notification of assigned approvals

## Notifications

### Email Triggers
1. **Submitter notified when approved** - "Your expense has been approved"
2. **Submitter notified when rejected** - "Your expense requires revision"
3. **Approver notified of pending approval** - "New approval request awaiting your action"
4. **Approver delegated** - "Approval responsibility delegated to {name}"
5. **Delegation revoked** - "Delegation ended"

### In-App Notifications
- Toast notifications for approvals/rejections
- Dashboard badges for pending items
- Activity feed entries

## Business Rules

1. **Default Workflow**: All expenses/timesheets use default workflow unless specified
2. **Multi-Level Approval**: 
   - Level 1: Direct manager (or by threshold)
   - Level 2: Director/Finance (if amount > threshold)
3. **Auto-Approval**: Items below $500 can have auto-approval (no manager needed)
4. **Rejection**: Rejected items return to "Draft" status for revision
5. **Withdrawal**: Submitters can withdraw while pending (before approval)
6. **Delegation**: Temporary delegation while approver is away (dates specified)
7. **Escalation**: Auto-escalate if pending >5 days to director
8. **Audit Trail**: All actions logged with timestamp, user, and reason

## Implementation Order

1. ✅ Plan & design (this document)
2. Create database migrations
3. Implement API endpoints
4. Add Blazor UI components
5. Configure email notifications
6. Add status indicators to list views
7. Mobile app updates (view-only approval details)
8. Test end-to-end workflows
9. Deploy to staging
10. User training & documentation

## Success Metrics

- All expenses/timesheets can be routed through approval
- Approvers receive and process requests within 24 hours
- <5% of requests require escalation
- Audit trail complete for compliance
- Mobile app displays approval status correctly

## Timeline

**Phase 1 (Database & API)**: 1-2 days
**Phase 2 (UI & Notifications)**: 1-2 days
**Phase 3 (Testing & Polish)**: 1 day
**Total**: ~3-5 days

## Rollback Plan

- Approval status optional on submission
- Legacy path: Skip approval, go straight to processed
- No data loss if workflow disabled
