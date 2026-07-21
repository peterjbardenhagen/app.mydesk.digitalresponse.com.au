# Phase 4: Teams & Departments Implementation Guide

**Version:** 1.0  
**Status:** Complete (Feature Development)  
**Target Release:** Q3 2026  
**Last Updated:** July 6, 2026

---

## Overview

Phase 4 introduces comprehensive organizational structure management with multi-level departments, teams, approval delegations, and budget tracking. This enables enterprises to organize users into hierarchies, delegate approval authority, and enforce budget controls.

---

## Core Features Implemented

### 1. **Multi-Level Department Management**
- **Hierarchical Structure**: Support for parent-child department relationships
- **Department Properties**:
  - Name, Description
  - Manager assignment
  - Cost center tracking
  - Status (Active, Inactive, Archived)
  - Creation/update timestamps

**Service**: `DepartmentService.cs`
- `GetDepartmentsAsync()` - List all departments for a tenant
- `GetDepartmentAsync()` - Get specific department
- `CreateDepartmentAsync()` - Create new department
- `UpdateDepartmentAsync()` - Update department details
- `DeleteDepartmentAsync()` - Archive/delete department

**UI Components**:
- `DepartmentsList.razor` - Department listing page with filters
- `DepartmentEditDialog.razor` - Create/edit department dialog

---

### 2. **Team Management**
- **Team Structure**: Teams belong to departments
- **Team Properties**:
  - Name, Description
  - Team Lead assignment
  - Approval team flag
  - Status management
  - Team membership tracking

**Service**: `TeamService.cs`
- `GetTeamsAsync()` - List teams (by department filter)
- `GetTeamAsync()` - Get specific team
- `CreateTeamAsync()` - Create new team
- `UpdateTeamAsync()` - Update team details
- `AddTeamMemberAsync()` - Add user to team with role
- `RemoveTeamMemberAsync()` - Remove user from team
- `GetTeamMembersAsync()` - List team members with details
- `GetUserTeamsAsync()` - List teams for a user
- `GetTeamUserIdsAsync()` - Get user IDs in team

**UI Components**:
- `TeamsList.razor` - Team listing with department filter
- `TeamEditDialog.razor` - Create/edit team dialog
- `TeamMembersDialog.razor` - Manage team membership

---

### 3. **Approval Delegation System**
- **Flexible Delegation**: Delegate approval authority with fine-grained controls
- **Delegation Properties**:
  - From/To users
  - Module type (Expense, Timesheet, PurchaseOrder, Invoice, etc.)
  - Amount thresholds (min/max)
  - Time-based validity (start/end dates)
  - Granular permissions (approve, reject, delegate, comment)

**Service**: `ApprovalDelegationService.cs`
- `CreateDelegationAsync()` - Create approval delegation
- `GetActiveDelegatesAsync()` - Get active delegates for user/module
- `GetDelegationAsync()` - Get specific delegation
- `CanApproveAsync()` - Check if delegate can approve given amount
- `GetUserDelegationsAsync()` - List user's delegations (as delegator or delegatee)
- `DeactivateDelegationAsync()` - Deactivate delegation
- `ResolveApprovalChainAsync()` - Build approval chain considering delegations

**UI Components**:
- `ApprovalDelegationManager.razor` - View and manage delegations
- `ApprovalDelegationDialog.razor` - Create/edit delegation

**Database**:
- `ApprovalDelegation` table with indexes on:
  - TenantId, TeamId
  - FromUserId, ToUserId
  - IsActive status

---

### 4. **Approval Escalation & Routing**
- **Smart Escalation**: Route approvals to delegates or escalate to manager
- **Escalation Logic**:
  - Check if primary approver has active delegations
  - Filter delegates by amount threshold
  - Escalate to team manager if amount exceeds delegate limits
  - Return ordered approval chain

**Service**: `ApprovalEscalationService.cs`
- `ResolveApprovalChainAsync()` - Build approval chain with delegates and escalation
- `RouteApprovalAsync()` - Route single approval to actual approver
- `NotifyDelegateAsync()` - Send notification when delegated (placeholder)
- `NotifyEscalationAsync()` - Send notification on escalation (placeholder)

**Supporting Class**: `ApprovalRouting`
- ApproverId - Who will actually approve
- IsDelegated - Whether delegated or escalated
- DelegationId - Link to delegation record
- Notes - Reason (delegated/escalated)
- ApprovalChain - Ordered list of all possible approvers

---

### 5. **Department Budget Tracking**
- **Budget Allocation & Tracking**: Control spending by department and category
- **Budget Properties**:
  - Fiscal year
  - Allocated amount
  - Spent amount tracking
  - Encumbered amount (approved but not spent)
  - Overspend allowance flag
  - Threshold alert percentage
  - Category-based breakdown (Expense, Travel, Meals, Other)

**Service**: `BudgetService.cs`
- `GetBudgetAsync()` - Get department budget for year
- `CreateBudgetAsync()` - Allocate budget
- `AddExpenseAsync()` - Record expense against budget
- `EncumberAmountAsync()` - Reserve amount for pending approval
- `CanApproveAsync()` - Check if approval would exceed budget
- `GetRemainingBudgetAsync()` - Calculate available funds
- `GetBudgetAlertPercentageAsync()` - Calculate usage percentage
- `GetDepartmentBudgetsAsync()` - List budgets with filters

**UI Components**:
- `BudgetManager.razor` - Budget overview and tracking
- `BudgetEditDialog.razor` - Create/edit budget allocation

**Database**:
- `DepartmentBudgets` table with indexes on:
  - TenantId, DepartmentId
  - FiscalYear
  - Status

---

### 6. **Bulk User Import**
- **Batch Import**: Import multiple users from CSV
- **CSV Format**: Email, FirstName, LastName, DepartmentId, TeamId, Role
- **Validation**: Email format, required fields, duplicate checking
- **Audit Trail**: Log all imports with success/failure counts

**Service**: `BulkUserImportService.cs`
- `ImportUsersAsync()` - Process CSV upload
  - Parse CSV with quote handling
  - Validate headers
  - Map to user records
  - Handle role assignment
  - Log import results
- Helper classes:
  - `BulkImportUser` - User data model
  - `BulkImportResult` - Import result summary

**UI Components**:
- `BulkUserImportDialog.razor` - File upload and progress tracking

**Database**:
- `BulkUserImportLog` table for audit trail

---

## Database Schema

### New Tables Created (Migration 022)

```sql
-- Departments: Multi-level organizational structure
CREATE TABLE Departments (
    DepartmentId INT PRIMARY KEY,
    TenantId INT,
    ParentDepartmentId INT,  -- Self-referencing for hierarchy
    Name NVARCHAR(255),
    Description NVARCHAR(500),
    ManagerUserId INT,
    Status NVARCHAR(50),
    CostCenter NVARCHAR(50),
    CreatedAt DATETIME2,
    UpdatedAt DATETIME2
)

-- Teams: Organizational teams within departments
CREATE TABLE Teams (
    TeamId INT PRIMARY KEY,
    TenantId INT,
    DepartmentId INT,
    Name NVARCHAR(255),
    Description NVARCHAR(500),
    TeamLeadUserId INT,
    Status NVARCHAR(50),
    IsApprovalTeam BIT,
    CreatedAt DATETIME2,
    UpdatedAt DATETIME2
)

-- TeamMembers: User membership in teams
CREATE TABLE TeamMembers (
    TeamMemberId INT PRIMARY KEY,
    TenantId INT,
    TeamId INT,
    UserId INT,
    Role NVARCHAR(100),
    Status NVARCHAR(50),
    JoinedAt DATETIME2
)

-- ApprovalDelegation: Delegate approval authority
CREATE TABLE ApprovalDelegation (
    DelegationId INT PRIMARY KEY,
    TenantId INT,
    TeamId INT,
    FromUserId INT,
    ToUserId INT,
    ModuleType NVARCHAR(50),
    MinThreshold DECIMAL(12,2),
    MaxThreshold DECIMAL(12,2),
    StartDate DATE,
    EndDate DATE,
    CanApprove BIT,
    CanReject BIT,
    CanDelegate BIT,
    CanComment BIT,
    IsActive BIT,
    CreatedAt DATETIME2,
    UpdatedAt DATETIME2
)

-- DepartmentBudgets: Track allocated vs spent budgets
CREATE TABLE DepartmentBudgets (
    BudgetId INT PRIMARY KEY,
    TenantId INT,
    DepartmentId INT,
    FiscalYear INT,
    AllocatedAmount DECIMAL(12,2),
    SpentAmount DECIMAL(12,2),
    EncumberedAmount DECIMAL(12,2),
    AllowOverspend BIT,
    ThresholdAlertPercentage INT,
    CatExpense DECIMAL(12,2),
    CatTravel DECIMAL(12,2),
    CatMeals DECIMAL(12,2),
    CatOther DECIMAL(12,2),
    Status NVARCHAR(50),
    CreatedAt DATETIME2,
    UpdatedAt DATETIME2
)

-- BulkUserImportLog: Audit trail for user imports
CREATE TABLE BulkUserImportLog (
    ImportId INT PRIMARY KEY,
    TenantId INT,
    ImportedById INT,
    Filename NVARCHAR(255),
    TotalRows INT,
    SuccessfulRows INT,
    FailedRows INT,
    Status NVARCHAR(50),
    ErrorMessage NVARCHAR(MAX),
    CreatedAt DATETIME2
)

-- Users table extended with:
ALTER TABLE Users ADD PrimaryDepartmentId INT
ALTER TABLE Users ADD PrimaryTeamId INT
```

### Migrations
- **Migration 022**: Teams & Departments schema
- **Migration 023**: Add administrator users (Peter & John Bardenhagen)

---

## Administrator Users Created

**Migration 023** creates two administrator users with full tenant access:

| Name | Email | Username | Password | Role | Position |
|------|-------|----------|----------|------|----------|
| Peter Bardenhagen | peterb@digitalresponse.com.au | peterb | Omnfxop09! | Director | CEO |
| John Bardenhagen | johnb@digitalresponse.com.au | johnb | Omnfxop90! | Director | CFO |

Both are configured as **Tenant Directors** with full administrative privileges.

---

## UI Routes & Navigation

### Administration Pages
- `/admin/departments` - Department management
- `/admin/teams` - Team management
- `/admin/budgets` - Budget allocation and tracking
- `/admin/approvals/delegations` - Approval delegation management

### Key Dialogs
- `DepartmentEditDialog` - Create/edit departments
- `TeamEditDialog` - Create/edit teams
- `TeamMembersDialog` - Manage team membership
- `BudgetEditDialog` - Create/edit budgets
- `ApprovalDelegationDialog` - Create/edit delegations
- `BulkUserImportDialog` - Import users from CSV

---

## API Endpoints

All endpoints are secured and validated for tenant isolation.

### Departments
```csharp
GET    /api/departments              // List departments
GET    /api/departments/{id}         // Get department
POST   /api/departments              // Create department
PUT    /api/departments/{id}         // Update department
DELETE /api/departments/{id}         // Delete department
```

### Teams
```csharp
GET    /api/teams                    // List teams
GET    /api/teams/{id}               // Get team
POST   /api/teams                    // Create team
PUT    /api/teams/{id}               // Update team
DELETE /api/teams/{id}               // Delete team
POST   /api/teams/{id}/members       // Add team member
DELETE /api/teams/{id}/members/{uid} // Remove team member
GET    /api/teams/{id}/members       // List team members
```

### Approval Delegations
```csharp
GET    /api/approval/delegations              // List user's delegations
POST   /api/approval/delegations              // Create delegation
GET    /api/approval/delegations/{id}         // Get delegation
DELETE /api/approval/delegations/{id}         // Deactivate delegation
POST   /api/approval/requests/{id}/delegate   // Route through delegation
```

### Budgets
```csharp
GET    /api/budgets                    // List budgets
GET    /api/budgets/{id}               // Get budget
POST   /api/budgets                    // Create budget
PUT    /api/budgets/{id}               // Update budget
POST   /api/budgets/{id}/expense       // Record expense
```

### Bulk Import
```csharp
POST   /api/bulk-import/users         // Upload and import CSV
GET    /api/bulk-import/history       // View import history
```

---

## Integration Points

### With Expense Module
- Budget enforcement on expense approval
- Expense routing through approval delegation chain
- Category-based budget tracking

### With Approval Workflow
- Delegation-based approval routing
- Escalation to manager for large amounts
- Delegation history and audit trail

### With Notifications (Phase 5)
- Notify delegates when approval routed to them
- Notify on escalation
- Budget threshold alerts

---

## Configuration & Settings

### Feature Flags
- `EnableApprovalDelegation` - Enable delegation UI
- `EnableBudgetTracking` - Enforce budget controls
- `EnableTeamsFeature` - Show teams in nav

### Budget Settings
- Default threshold alert: 80%
- Support overspend allowance per department
- Category tracking (Expense, Travel, Meals, Other)

### Delegation Rules
- Min/max amount thresholds
- Time-based validity (start/end dates)
- Granular permissions (approve, reject, delegate, comment)
- Support for null ModuleType (all modules)

---

## Testing Checklist

### Unit Tests (Pending)
- [ ] DepartmentService CRUD operations
- [ ] TeamService with member management
- [ ] ApprovalDelegationService delegation logic
- [ ] ApprovalEscalationService routing and escalation
- [ ] BudgetService budget enforcement
- [ ] BulkUserImportService CSV parsing and import

### Integration Tests (Pending)
- [ ] Department hierarchy queries
- [ ] Team member role assignments
- [ ] Delegation activation and expiration
- [ ] Budget threshold enforcement
- [ ] Bulk import with error handling
- [ ] Multi-tenant isolation

### UI Tests (Pending)
- [ ] Department CRUD workflows
- [ ] Team management with members
- [ ] Delegation creation and management
- [ ] Budget allocation and tracking
- [ ] Bulk user import dialog

---

## Security Considerations

### Tenant Isolation
- All queries filtered by `TenantId`
- Department access scoped to tenant
- Team membership scoped to tenant
- Budget controls enforced per tenant

### Authorization
- Department/Team management requires Administrator role
- Delegation creation requires approval authority
- Budget editing requires Finance or Admin role

### Data Protection
- User passwords hashed (BCrypt)
- Sensitive data not logged
- Import logs retain minimal detail
- Audit trail of all modifications

---

## Performance Optimizations

### Database Indexes
- `IX_Departments_TenantId` - Fast tenant filtering
- `IX_Teams_DepartmentId` - Fast team lookup by department
- `IX_TeamMembers_UserId` - Fast member lookup
- `IX_ApprovalDelegation_TenantId` - Fast delegation queries
- `IX_ApprovalDelegation_Active` - Fast active delegation filtering
- `IX_DepartmentBudgets_FiscalYear` - Fast budget queries

### Caching
- Department list cached (5 minute TTL)
- Team lists cached by department (5 minute TTL)
- Active delegations cached (10 minute TTL)
- Budget thresholds calculated on-demand

---

## Known Limitations & TODOs

### Current Session TODOs
- [ ] Notification service integration (Phase 5)
- [ ] Budget threshold alerts (Phase 5)
- [ ] Audit logging enhancement
- [ ] Mobile UI optimization
- [ ] Export budget reports
- [ ] Department reorganization (move teams)
- [ ] Delegation templates/groups
- [ ] Advanced approval workflows

### Deferred Features
- Approval chains with multiple levels
- Delegation conflict detection
- Budget carryover between years
- Spend forecasting
- Budget variance analysis

---

## Files & Structure

```
src/MyDesk.Web/
├── Services/
│   ├── DepartmentService.cs                 [127 lines]
│   ├── TeamService.cs                       [175 lines]
│   ├── ApprovalDelegationService.cs         [196 lines]
│   ├── ApprovalEscalationService.cs         [189 lines]
│   ├── BudgetService.cs                     [176 lines]
│   └── BulkUserImportService.cs             [266 lines]
│
├── Components/Pages/Administration/
│   ├── DepartmentsList.razor                - Department management UI
│   ├── TeamsList.razor                      - Team management UI
│   ├── ApprovalDelegationManager.razor      - Delegation management UI
│   └── BudgetManager.razor                  - Budget tracking UI
│
├── Components/Shared/
│   ├── DepartmentEditDialog.razor           - Create/edit department
│   ├── TeamEditDialog.razor                 - Create/edit team
│   ├── TeamMembersDialog.razor              - Manage team members
│   ├── ApprovalDelegationDialog.razor       - Create/edit delegation
│   ├── BudgetEditDialog.razor               - Create/edit budget
│   └── BulkUserImportDialog.razor           - CSV import UI
│
└── Program.cs                                - DI registration (lines 388-394)

src/Deployment/Migration/
├── 022_teams_and_departments.sql            - Phase 4 schema
└── 023_add_administrators.sql               - Admin user seed data
```

---

## Next Steps for Deployment

1. **Build & Test**
   - Run full test suite
   - Verify no compilation errors
   - Performance testing

2. **Database Setup**
   - Run migrations 022 & 023
   - Verify tables and indexes created
   - Seed test data

3. **Deployment**
   - Deploy to development environment
   - Deploy to staging environment
   - Production deployment

4. **Feature Enablement**
   - Configure feature flags
   - Enable in admin settings
   - Roll out to users

5. **Monitoring**
   - Track API performance
   - Monitor budget constraint violations
   - Watch approval throughput metrics

---

## Version History

| Version | Date | Status | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-07-06 | Complete | Initial Phase 4 implementation with departments, teams, delegations, budgets, and bulk import |

---

## Support & Questions

For implementation details, see the individual service class documentation in the code.

For UI/UX questions, reference the Blazor component code.

For database schema questions, see `src/Deployment/Migration/022_teams_and_departments.sql`.
