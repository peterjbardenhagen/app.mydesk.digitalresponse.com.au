# Phase 4 End-to-End Verification Checklist

**Version:** 1.0  
**Status:** Ready for Testing  
**Date:** July 6, 2026  
**Scope:** Teams, Departments, Approval Delegation, Budget Management, Bulk Import

---

## Overview

This document provides a comprehensive end-to-end verification plan for Phase 4 implementation. It covers functional testing, integration testing, security verification, performance validation, and deployment readiness.

**Estimated Testing Time:** 4-6 hours  
**Test Environment:** Development (local) → Staging → Production

---

## Part 1: Functional Verification

### 1.1 Department Management

#### Create Department
- [ ] Navigate to `/admin/departments`
- [ ] Click "New Department" button
- [ ] Fill form: Name="Engineering", Description="Engineering Dept", Manager=Peter Bardenhagen
- [ ] Submit form
- [ ] **Verify:** Department appears in list with correct details
- [ ] **Verify:** Department ID assigned (auto-increment)
- [ ] **Verify:** CreatedAt timestamp populated
- [ ] **Error Case:** Try creating with empty name → "Name is required" message
- [ ] **Error Case:** Try creating duplicate name → "Department name already exists" message

#### Read Department List
- [ ] Navigate to `/admin/departments`
- [ ] **Verify:** All departments listed in table
- [ ] **Verify:** Columns: DepartmentId, Name, Manager, Status, Actions
- [ ] **Verify:** Sort by Name works
- [ ] **Verify:** Status filter works (Active/Inactive/Archived)
- [ ] **Verify:** Pagination works for 20+ departments
- [ ] **Verify:** Parent department hierarchy visible (indentation or indicator)

#### Update Department
- [ ] Click Edit on existing department
- [ ] Change Name to "Engineering Team"
- [ ] Change Manager to John Bardenhagen
- [ ] Submit
- [ ] **Verify:** List updated with new values
- [ ] **Verify:** UpdatedAt timestamp changed
- [ ] **Verify:** Audit log entry created

#### Delete Department (Archive)
- [ ] Click Delete on a department
- [ ] **Verify:** Confirmation dialog appears
- [ ] Confirm deletion
- [ ] **Verify:** Department status changes to "Archived"
- [ ] **Verify:** Department no longer appears in active list (unless filter adjusted)
- [ ] **Verify:** Teams in archived department remain (cascade not hard delete)

#### Department Hierarchy
- [ ] Create parent department "Finance"
- [ ] Create child department "Accounting" with parent=Finance
- [ ] **Verify:** Parent department ID stored correctly
- [ ] **Verify:** Hierarchy query returns both departments with parent/child relationships
- [ ] **Verify:** Cannot set circular parent (A.parent = B, B.parent = A)

### 1.2 Team Management

#### Create Team
- [ ] Navigate to `/admin/teams`
- [ ] Click "New Team" button
- [ ] Select Department: "Engineering"
- [ ] Fill: Name="Platform Team", Description="Backend Platform Team"
- [ ] Select Team Lead: "Peter Bardenhagen"
- [ ] Check "Approval Team" checkbox
- [ ] Submit
- [ ] **Verify:** Team appears in list
- [ ] **Verify:** Department filter shows only this team when filtered to Engineering
- [ ] **Verify:** Team Lead displays correctly

#### List Teams
- [ ] View all teams in list
- [ ] **Verify:** Department filter dropdown populated
- [ ] Filter by Engineering department
- [ ] **Verify:** Only Engineering teams shown
- [ ] **Verify:** Columns: TeamId, Department, TeamName, Lead, Status, IsApprovalTeam, Actions
- [ ] **Verify:** IsApprovalTeam checkbox column shows correct state

#### Update Team
- [ ] Click Edit on team
- [ ] Change Name to "Platform Services Team"
- [ ] Change Team Lead to John Bardenhagen
- [ ] Uncheck "Approval Team"
- [ ] Submit
- [ ] **Verify:** List updated
- [ ] **Verify:** Approval workflow considers IsApprovalTeam flag

#### Team Members Management
- [ ] Click "Members" (people icon) on a team
- [ ] **Verify:** Current members listed (if any)
- [ ] Add new member:
  - [ ] Select user from dropdown: "John Bardenhagen"
  - [ ] Select role: "Member"
  - [ ] Click "Add Member"
  - [ ] **Verify:** John appears in members list with "Member" role
  - [ ] **Verify:** JoinedAt date set to today
- [ ] Add another member as "Lead":
  - [ ] Select user: "Peter Bardenhagen"
  - [ ] Select role: "Lead"
  - [ ] Click "Add Member"
  - [ ] **Verify:** Peter appears as "Lead"
- [ ] Remove a member:
  - [ ] Click delete icon on a member
  - [ ] **Verify:** Confirmation appears
  - [ ] Confirm
  - [ ] **Verify:** Member removed from list
  - [ ] **Verify:** Audit log created
- [ ] **Error Case:** Try adding user not in system → Error message
- [ ] **Error Case:** Try adding same user twice → Duplicate check or error

#### Team Deletion Cascade
- [ ] Create test team with 3 members
- [ ] Delete the team
- [ ] **Verify:** Team status set to Archived
- [ ] **Verify:** Members still exist (not deleted) but team reference removed or marked inactive
- [ ] **Verify:** Delegations for team members deactivated

### 1.3 Approval Delegation Management

#### Create Delegation
- [ ] Navigate to `/admin/approvals/delegations`
- [ ] Click "New Delegation" button
- [ ] Configure:
  - [ ] Delegate To: "John Bardenhagen"
  - [ ] Module: "Expense"
  - [ ] Min Amount: $0
  - [ ] Max Amount: $5,000
  - [ ] Start Date: Today
  - [ ] End Date: 30 days from today
  - [ ] Permissions:
    - [ ] ✓ Can Approve
    - [ ] ✓ Can Reject
    - [ ] ☐ Can Delegate
    - [ ] ✓ Can Comment
- [ ] Submit
- [ ] **Verify:** Delegation appears in "As Delegator" tab
- [ ] **Verify:** Delegation shows correct thresholds ($0-$5,000)
- [ ] **Verify:** Dates display correctly
- [ ] **Verify:** Permissions display correctly

#### View Delegations
- [ ] Two tabs visible: "As Delegator" and "Received Delegations"
- [ ] "As Delegator" tab:
  - [ ] **Verify:** Shows delegations I've created
  - [ ] **Verify:** For each: Delegate Name, Module, Amount Range, Dates, Permissions
  - [ ] **Verify:** Can edit/delete from this tab
- [ ] "Received Delegations" tab:
  - [ ] **Verify:** Shows delegations I've received
  - [ ] **Verify:** Shows Delegator Name, Module, Amount Range, Dates
  - [ ] **Verify:** Cannot edit (delegator controls)

#### Update Delegation
- [ ] Click Edit on an active delegation
- [ ] Change Max Amount from $5,000 to $10,000
- [ ] Change End Date to 60 days from today
- [ ] Submit
- [ ] **Verify:** List updated with new values
- [ ] **Verify:** Effective immediately (no approval needed)

#### Deactivate Delegation
- [ ] Click Delete on an active delegation
- [ ] **Verify:** Confirmation dialog
- [ ] Confirm
- [ ] **Verify:** Delegation marked inactive (IsActive = false)
- [ ] **Verify:** Delegation no longer shows in active list
- [ ] **Verify:** Cannot edit/delete inactive delegation
- [ ] **Verify:** Approval workflow no longer routes to this delegate

#### Delegation Validation
- [ ] Try creating delegation with Min > Max → Error
- [ ] Try creating delegation with End Date < Start Date → Error
- [ ] Try creating delegation with amount threshold $0 (allow) → Success
- [ ] Try delegating to non-existent user → Error
- [ ] Try delegating with no permissions checked → Error
- [ ] Try delegating outside my approval authority → Success (system trusts)

#### Multi-Module Delegation
- [ ] Create delegation for "Expense" module
- [ ] Create another for "PurchaseOrder" module
- [ ] Create another with null ModuleType (applies to all)
- [ ] **Verify:** Each delegation shows correct module type
- [ ] **Verify:** Query for active delegates by module filters correctly

### 1.4 Budget Management

#### Create Budget
- [ ] Navigate to `/admin/budgets`
- [ ] Click "New Budget" button
- [ ] Configure:
  - [ ] Department: "Engineering"
  - [ ] Fiscal Year: 2026
  - [ ] Allocated Amount: $100,000
  - [ ] Allow Overspend: ☐ (unchecked)
  - [ ] Alert Threshold: 80%
  - [ ] Category Budgets:
    - [ ] Expense: $50,000
    - [ ] Travel: $30,000
    - [ ] Meals: $15,000
    - [ ] Other: $5,000
- [ ] Submit
- [ ] **Verify:** Budget appears in list
- [ ] **Verify:** Department filter shows this budget when filtered
- [ ] **Verify:** Fiscal year dropdown shows 2026

#### View Budget List
- [ ] Navigate to `/admin/budgets`
- [ ] **Verify:** Fiscal year selector (default = current year)
- [ ] **Verify:** Department filter (optional)
- [ ] **Verify:** All budgets displayed:
  - [ ] Department name
  - [ ] Allocated amount
  - [ ] Spent amount
  - [ ] Remaining amount
  - [ ] % Used (progress bar, color-coded)
- [ ] **Verify:** Summary at bottom:
  - [ ] Total Allocated
  - [ ] Total Spent
  - [ ] Total Remaining
  - [ ] Average % Used

#### Budget Summary Calculations
- [ ] Create budget: Allocated=$100,000, Spent=$0, Encumbered=$0
- [ ] **Verify:** % Used = 0%, Progress color = Green
- [ ] Update spent to $50,000
- [ ] **Verify:** % Used = 50%, Progress color = Green
- [ ] Update spent to $65,000
- [ ] **Verify:** % Used = 65%, Progress color = Yellow (approaching threshold)
- [ ] Update spent to $85,000
- [ ] **Verify:** % Used = 85%, Progress color = Red (exceeded threshold)

#### Budget Enforcement
- [ ] Create budget: Allocated=$10,000, AllowOverspend=false
- [ ] Try approving expense for $12,000
- [ ] **Verify:** Approval rejected with "Budget exceeded" message
- [ ] Adjust spent to $8,000
- [ ] Try approving $3,000
- [ ] **Verify:** Approval rejected (8k + 3k > 10k)
- [ ] Try approving $1,500
- [ ] **Verify:** Approval succeeds
- [ ] Now set AllowOverspend=true on budget
- [ ] Try approving $15,000
- [ ] **Verify:** Approval succeeds (overspend allowed)

#### Category Budget Tracking
- [ ] Create budget with category breakdowns (as above)
- [ ] Create expense: Expense category, $30,000
- [ ] **Verify:** Spent (Expense) updated to $30,000
- [ ] **Verify:** Budget remaining = 100k - 30k = $70,000
- [ ] **Verify:** Category remaining (Expense) = 50k - 30k = $20,000
- [ ] Try creating Expense for $25,000 (would exceed category)
- [ ] **Verify:** Rejected with "Category budget exceeded" message

#### Multiple Years & Departments
- [ ] Create budgets for:
  - [ ] Engineering, 2025: $80,000
  - [ ] Engineering, 2026: $100,000
  - [ ] Finance, 2026: $150,000
- [ ] Select 2025 in dropdown
- [ ] **Verify:** Only 2025 budgets shown
- [ ] Select 2026
- [ ] **Verify:** Both Engineering and Finance shown
- [ ] Filter by Engineering
- [ ] **Verify:** Only Engineering 2026 shown
- [ ] Clear filter, Select 2025 again
- [ ] **Verify:** Only Engineering 2025 shown

### 1.5 Bulk User Import

#### Valid CSV Import
- [ ] Navigate to admin section with bulk import dialog
- [ ] Prepare CSV file:
  ```
  Email,FirstName,LastName,DepartmentId,TeamId,Role
  alice@example.com,Alice,Johnson,1,1,Member
  bob@example.com,Bob,Smith,1,2,Lead
  carol@example.com,Carol,Williams,2,3,Member
  ```
- [ ] Click "Choose CSV File" and select the file
- [ ] **Verify:** Filename displayed
- [ ] Click "Import Users"
- [ ] **Verify:** Progress bar shows
- [ ] **Verify:** Status updates: "Importing: Processing file..." → "Importing: Importing users..." → Complete
- [ ] **Verify:** Results displayed:
  - [ ] Total Rows: 3
  - [ ] Successful: 3
  - [ ] Failed: 0
- [ ] **Verify:** Users created in system with correct assignments
- [ ] **Verify:** Import log created in BulkUserImportLog table

#### CSV with Missing Optional Fields
- [ ] Prepare CSV (DepartmentId/TeamId/Role optional):
  ```
  Email,FirstName,LastName
  dave@example.com,Dave,Brown
  eve@example.com,Eve,Davis
  ```
- [ ] Import
- [ ] **Verify:** Users created successfully
- [ ] **Verify:** DepartmentId/TeamId null in database
- [ ] **Verify:** Role defaults or null

#### CSV with Invalid Data
- [ ] Prepare CSV with errors:
  ```
  Email,FirstName,LastName
  not-an-email,Frank,Green
  gloria@example.com,Gloria,Henry
  ```
- [ ] Import
- [ ] **Verify:** Result shows:
  - [ ] Total: 2
  - [ ] Successful: 1 (Gloria)
  - [ ] Failed: 1 (Frank)
- [ ] **Verify:** Error message shows "Invalid email format"
- [ ] **Verify:** Gloria created, Frank not created

#### CSV with Missing Headers
- [ ] Prepare CSV with wrong headers:
  ```
  FullName,FirstName,LastName
  john@example.com,John,Smith
  ```
- [ ] Import
- [ ] **Verify:** Error shown: "Missing required column: Email"
- [ ] **Verify:** No users created

#### CSV File Size Limit
- [ ] Create CSV with 10,000 rows (would be >5 MB)
- [ ] Try uploading
- [ ] **Verify:** Error: "File exceeds 5 MB limit"

#### Duplicate Email Handling
- [ ] Prepare CSV:
  ```
  Email,FirstName,LastName
  henry@example.com,Henry,Jones
  henry@example.com,Henry,Jackson
  ```
- [ ] Import
- [ ] **Verify:** Second row rejected as duplicate
- [ ] **Verify:** Result shows:
  - [ ] Total: 2
  - [ ] Successful: 1
  - [ ] Failed: 1 (duplicate email)

#### CSV with Quotes & Commas
- [ ] Prepare CSV with quoted fields containing commas:
  ```
  Email,FirstName,LastName,DepartmentId,TeamId,Role
  "iris@example.com","Iris, Jr.","King",1,1,Member
  ```
- [ ] Import
- [ ] **Verify:** FirstName parsed as "Iris, Jr." (not split on comma)
- [ ] **Verify:** User created correctly

---

## Part 2: Integration Testing

### 2.1 Department & Team Integration

#### Department to Team Relationships
- [ ] Create department "Sales"
- [ ] Create teams "Sales East", "Sales West" in Sales department
- [ ] Delete Sales department
- [ ] **Verify:** Teams remain but marked as archived or orphaned
- [ ] **Verify:** Team department reference still intact (not null)

#### Team Member to User Integration
- [ ] Create team "QA Team"
- [ ] Add users: Alice, Bob, Carol
- [ ] Delete user Bob from system (soft delete)
- [ ] **Verify:** Team still exists
- [ ] **Verify:** Bob removed from team (cascade delete)
- [ ] **Verify:** Alice and Carol still members

### 2.2 Delegation to Approval Workflow Integration

#### Delegation Routing
- [ ] Create delegation: Peter → John for Expense, $0-$10,000
- [ ] Submit expense for $5,000
- [ ] **Verify:** Approval routed to John (not Peter)
- [ ] **Verify:** Approval status shows "Delegated from Peter"
- [ ] John approves
- [ ] **Verify:** Expense approved
- [ ] **Verify:** Audit log shows John as approver, Peter as delegator

#### Escalation on Amount Threshold
- [ ] Create delegation: Peter → John for Expense, $0-$5,000
- [ ] Create Peter's manager: Manager=CEO (hierarchy)
- [ ] Submit expense for $8,000
- [ ] **Verify:** Routed to Peter (exceeds delegate limit)
- [ ] **Verify:** John skipped
- [ ] Submit another for $3,000
- [ ] **Verify:** Routed to John (within limit)

#### Delegation with Multiple Delegates
- [ ] Create delegation 1: Peter → John for Expense, $0-$5,000
- [ ] Create delegation 2: Peter → Carol for Expense, $5,001-$20,000
- [ ] Submit expense for $3,000
- [ ] **Verify:** Routed to John
- [ ] Submit for $10,000
- [ ] **Verify:** Routed to Carol
- [ ] Submit for $25,000
- [ ] **Verify:** Escalated to Peter (no delegate for this amount)

### 2.3 Budget to Approval Workflow Integration

#### Budget Enforcement on Approval
- [ ] Create budget: Allocated=$5,000, Spent=$0
- [ ] Create delegation: Peter → John
- [ ] Submit expense for $3,000
- [ ] John approves
- [ ] **Verify:** Approval succeeds
- [ ] **Verify:** Budget spent updated to $3,000
- [ ] Submit another for $3,000
- [ ] John tries to approve
- [ ] **Verify:** Rejected: "Insufficient budget (need $3k, have $2k available)"

#### Encumbrance Mechanism
- [ ] Create budget: Allocated=$10,000
- [ ] Submit expense for $4,000 (pending approval)
- [ ] **Verify:** Budget encumbered=$4,000, spent=$0
- [ ] Submit another for $4,000 (pending)
- [ ] **Verify:** Encumbered=$8,000, spent=$0
- [ ] **Verify:** Available=$2,000
- [ ] Try approving third expense for $3,000
- [ ] **Verify:** Rejected (only $2k available)
- [ ] First approval processed
- [ ] **Verify:** Spent=$4,000, encumbered=$4,000, available=$2,000
- [ ] Try third expense again
- [ ] **Verify:** Still rejected
- [ ] Reject second expense
- [ ] **Verify:** Encumbered=$0, available=$6,000
- [ ] Third expense approved
- [ ] **Verify:** Spent=$7,000

### 2.4 Bulk Import to Team Management Integration

#### Import Users into Teams
- [ ] Create department "Engineering", team "Backend"
- [ ] Prepare CSV:
  ```
  Email,FirstName,LastName,DepartmentId,TeamId,Role
  jack@example.com,Jack,Miller,1,1,Member
  ```
- [ ] Import
- [ ] **Verify:** Jack created
- [ ] **Verify:** Jack added to Backend team with Member role
- [ ] Open team members dialog
- [ ] **Verify:** Jack listed as member

#### Import with Invalid Department/Team
- [ ] Prepare CSV with DepartmentId=999 (doesn't exist)
- [ ] Import
- [ ] **Verify:** User created but DepartmentId validation fails
- [ ] **Verify:** Error logged with row number
- [ ] **Verify:** Partial success shows correct counts

### 2.5 Multi-Tenant Isolation Verification

#### Department Isolation
- [ ] User logs in as Tenant 1
- [ ] Creates department "Finance"
- [ ] User logs in as Tenant 2
- [ ] **Verify:** Tenant 1 department NOT visible
- [ ] Creates own "Finance" department in Tenant 2
- [ ] **Verify:** Can have same name in different tenants
- [ ] Back to Tenant 1
- [ ] **Verify:** Still shows original Finance (not confused with Tenant 2's)

#### Team Isolation
- [ ] Similar test with teams
- [ ] Create team "Platform" in Tenant 1
- [ ] Create team "Platform" in Tenant 2
- [ ] **Verify:** Teams isolated

#### Budget Isolation
- [ ] Create budget for $100k in Tenant 1
- [ ] Switch to Tenant 2
- [ ] Create budget for same department with $50k
- [ ] **Verify:** Each tenant sees only their budget
- [ ] **Verify:** No data leakage

#### User Access Isolation
- [ ] User from Tenant 1 cannot:
  - [ ] View Tenant 2 departments
  - [ ] View Tenant 2 teams
  - [ ] View Tenant 2 budgets
  - [ ] Create delegations in Tenant 2
- [ ] API calls with Tenant 2 data return 403 Forbidden

---

## Part 3: Security Verification

### 3.1 Authentication & Authorization

#### Token Validation
- [ ] Make API call with valid token
- [ ] **Verify:** Request succeeds
- [ ] Make API call with expired token
- [ ] **Verify:** 401 Unauthorized response
- [ ] Make API call without token
- [ ] **Verify:** 401 Unauthorized response
- [ ] Make API call with malformed token
- [ ] **Verify:** 401 Unauthorized response

#### Role-Based Access Control
- [ ] User with "Employee" role tries accessing `/admin/departments`
- [ ] **Verify:** 403 Forbidden or redirected to error page
- [ ] User with "Administrator" role accesses `/admin/departments`
- [ ] **Verify:** Page loads successfully

#### Tenant Isolation via API
- [ ] Call GET `/api/departments` as Tenant 1
- [ ] **Verify:** Only Tenant 1 departments returned
- [ ] Change JWT to Tenant 2
- [ ] **Verify:** Only Tenant 2 departments returned
- [ ] Try calling GET `/api/departments/1` (Tenant 1 ID) as Tenant 2
- [ ] **Verify:** 403 Forbidden

### 3.2 Data Validation

#### SQL Injection Prevention
- [ ] Try creating department with name: `'; DROP TABLE Departments; --`
- [ ] **Verify:** Name stored as literal string (not executed)
- [ ] **Verify:** Departments table still intact
- [ ] Query shows department with exact name (including quotes)

#### XSS Prevention (Blazor)
- [ ] Try creating department with name: `<script>alert('xss')</script>`
- [ ] **Verify:** Name displayed as text (not executed)
- [ ] **Verify:** No browser alert appears

#### Email Validation (Bulk Import)
- [ ] Import CSV with emails:
  - [ ] `valid@example.com` → Accepted
  - [ ] `notanemail` → Rejected
  - [ ] `name@domain` → Rejected (no TLD)
  - [ ] `name@domain.co.uk` → Accepted
  - [ ] `user+tag@domain.com` → Accepted

#### Numeric Validation
- [ ] Try creating budget with:
  - [ ] Amount = -1000 → Error "must be >= 0"
  - [ ] Amount = 0 → Error "must be > 0"
  - [ ] Amount = 999999999.99 → Accepted (large but valid)
  - [ ] Threshold = -1 → Error "must be 0-100"
  - [ ] Threshold = 101 → Error "must be 0-100"

#### Date Validation
- [ ] Create delegation with:
  - [ ] Start date = 2026-12-31, End date = 2026-01-01 → Error
  - [ ] Start date = 2026-01-01, End date = 2026-12-31 → Accepted
  - [ ] Start date = End date → Error or Warning

### 3.3 Audit Logging

#### Create Operation Logged
- [ ] Create a department
- [ ] Query ComplianceAuditLog table
- [ ] **Verify:** Entry exists:
  - [ ] Action = "DepartmentCreated"
  - [ ] ActorUserId = current user
  - [ ] Details = JSON with department data
  - [ ] Timestamp recent
  - [ ] IpAddress = client IP

#### Update Operation Logged
- [ ] Update department name
- [ ] **Verify:** Audit log shows:
  - [ ] Action = "DepartmentUpdated"
  - [ ] Details = old and new values (if tracked)

#### Delete Operation Logged
- [ ] Delete (archive) department
- [ ] **Verify:** Audit log shows:
  - [ ] Action = "DepartmentArchived" or "DepartmentDeleted"

#### Bulk Import Logged
- [ ] Import 5 users
- [ ] **Verify:** BulkUserImportLog entry:
  - [ ] TenantId correct
  - [ ] ImportedById = current user
  - [ ] Filename correct
  - [ ] TotalRows = 5
  - [ ] SuccessfulRows = result count
  - [ ] FailedRows = error count
  - [ ] Status = "Success" or "PartialSuccess"

### 3.4 Sensitive Data Protection

#### Passwords Not Logged
- [ ] Create user with password
- [ ] Search logs for password
- [ ] **Verify:** Password not found in logs

#### Sensitive Fields Excluded from Logs
- [ ] Import users
- [ ] Check audit log for import details
- [ ] **Verify:** Log does not contain:
  - [ ] API keys
  - [ ] Personal details (if any)
  - [ ] Raw file content

#### Error Messages Don't Leak Info
- [ ] Cause a database error
- [ ] **Verify:** Client sees generic message: "An error occurred"
- [ ] **Verify:** Log contains detailed error info (for support)

---

## Part 4: Performance Verification

### 4.1 Response Time

#### API Response Times
- [ ] GET `/api/departments` (empty result)
  - [ ] **Target:** <100ms
- [ ] GET `/api/departments` (100 departments)
  - [ ] **Target:** <200ms
- [ ] GET `/api/teams?departmentId=1` (50 teams)
  - [ ] **Target:** <150ms
- [ ] GET `/api/budgets?year=2026` (50 budgets)
  - [ ] **Target:** <200ms

#### UI Load Times
- [ ] Load `/admin/departments` page
  - [ ] **Target:** <2 seconds (full page render)
- [ ] Load department filter dropdown
  - [ ] **Target:** <500ms
- [ ] Open team members dialog
  - [ ] **Target:** <1 second
- [ ] Import dialog opens
  - [ ] **Target:** <500ms

### 4.2 Database Performance

#### Query Performance
- [ ] Select all departments by tenant
  - [ ] **Target:** <50ms (with index)
- [ ] Select all teams by department
  - [ ] **Target:** <50ms (with index)
- [ ] Calculate budget remaining
  - [ ] **Target:** <30ms
- [ ] Get approval chain for delegation
  - [ ] **Target:** <100ms

#### Concurrent Operations
- [ ] 10 simultaneous budget updates
  - [ ] **Verify:** All succeed without deadlock
  - [ ] **Target:** <500ms total
- [ ] 10 simultaneous team member adds
  - [ ] **Verify:** All succeed
  - [ ] **Target:** <500ms total

### 4.3 Caching

#### Department Caching
- [ ] First load departments list
- [ ] **Verify:** Cache miss (slow)
- [ ] Reload immediately
- [ ] **Verify:** Cache hit (fast, <50ms)
- [ ] Wait 5+ minutes
- [ ] Reload
- [ ] **Verify:** Cache expired (slow again)

#### Team Caching
- [ ] Similar test for teams
- [ ] **Verify:** Cached per department
- [ ] Change department filter
- [ ] **Verify:** New cache entry

---

## Part 5: Deployment Readiness

### 5.1 Code Quality

- [ ] All code compiles without warnings
- [ ] All tests pass (unit tests >80% coverage)
- [ ] No hardcoded secrets in code
- [ ] No commented-out code blocks
- [ ] Naming conventions followed
- [ ] Code review completed

### 5.2 Database

- [ ] Migrations create all tables correctly
- [ ] Indexes created on key columns
- [ ] Foreign keys enforced
- [ ] Constraints verified
- [ ] Seed data (administrators) loaded
- [ ] Backup tested and working

### 5.3 Documentation

- [ ] PHASE-4-IMPLEMENTATION.md comprehensive and accurate
- [ ] PHASE-4-SECURITY-REVIEW.md covers all controls
- [ ] agents.md updated with Phase 4 ownership
- [ ] Code comments for non-obvious logic
- [ ] API documentation complete
- [ ] Database schema documented

### 5.4 Configuration

- [ ] appsettings.json has all required settings
- [ ] Environment variables documented
- [ ] Feature flags configurable
- [ ] Logging configured
- [ ] Rate limiting configured
- [ ] CORS configured for deployment domains

### 5.5 Monitoring

- [ ] Application Insights configured
- [ ] Error logging setup
- [ ] Audit logging configured
- [ ] Alerts defined for critical operations
- [ ] Health check endpoint available
- [ ] Metrics dashboard created (optional)

### 5.6 Rollback Plan

- [ ] Previous migration scripts available
- [ ] Database backup before deployment
- [ ] Rollback procedure documented
- [ ] Feature flags allow disabling Phase 4
- [ ] Fallback to previous UI possible

---

## Execution Plan

### Day 1: Functional Testing (4 hours)
- [ ] 08:00 - Setup test environment
- [ ] 08:30 - Run 1.1 (Department tests)
- [ ] 09:30 - Run 1.2 (Team tests)
- [ ] 10:30 - Break
- [ ] 10:45 - Run 1.3 (Delegation tests)
- [ ] 12:00 - Lunch
- [ ] 13:00 - Run 1.4 (Budget tests)
- [ ] 14:30 - Run 1.5 (Bulk import tests)
- [ ] 15:30 - Document results

### Day 2: Integration & Security (4 hours)
- [ ] 08:00 - Run Part 2 (Integration tests)
- [ ] 10:00 - Run Part 3 (Security tests)
- [ ] 12:00 - Lunch
- [ ] 13:00 - Continue security tests
- [ ] 14:30 - Document findings
- [ ] 15:30 - Remediate any issues

### Day 3: Performance & Deployment (2 hours)
- [ ] 08:00 - Run Part 4 (Performance)
- [ ] 09:00 - Run Part 5 (Deployment checks)
- [ ] 10:00 - Final review and sign-off

---

## Test Reporting

### Passing Test Template
```
✅ [Test ID] - [Test Name]
   Status: PASS
   Duration: XX seconds
   Notes: All assertions verified
```

### Failing Test Template
```
❌ [Test ID] - [Test Name]
   Status: FAIL
   Duration: XX seconds
   Error: [Description]
   Severity: [Critical/High/Medium/Low]
   Remediation: [Action required]
```

### Deferred Test Template
```
⏸️ [Test ID] - [Test Name]
   Status: DEFERRED
   Reason: [Why not tested]
   Follow-up: [When/how to test]
```

---

## Sign-Off

**Testing Lead:** [Name]  
**Test Date:** July 6-8, 2026  
**Total Tests:** 80+  
**Passing:** TBD  
**Failing:** TBD  
**Deferred:** TBD

**Overall Status:** 🔄 Ready to Test

**Approval for Deployment:** ❌ Pending test completion

---

## Appendix: Test Data

### Sample Users
- Peter Bardenhagen (peterb) - Administrator, CEO, ID=1000
- John Bardenhagen (johnb) - Administrator, CFO, ID=1001
- Test users created via bulk import (dynamic IDs)

### Sample Departments
- Engineering (auto-created for tests)
- Finance (auto-created for tests)
- Sales (auto-created for tests)

### Sample Teams
- Platform Team (Engineering)
- Finance Operations (Finance)
- Sales East (Sales)
- Sales West (Sales)

### Sample Budgets
- Engineering, 2026: $100,000
- Finance, 2026: $150,000
- Sales, 2026: $80,000

### Sample CSV Files
See test cases 1.5 for CSV examples

---
