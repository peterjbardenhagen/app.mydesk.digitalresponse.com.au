# Work In Progress: Approval Workflows

## Current Phase
**Feature**: Manager Approval Workflows for Expenses & Timesheets  
**Branch**: `claude/approval-workflows`  
**Status**: Phase 1 Complete (Database Design)  
**Progress**: 20% (Database & Plan → API → UI → Testing)

---

## What's Been Completed ✅

### 1. Design & Planning
- ✅ APPROVAL-WORKFLOWS.md (comprehensive specification)
- ✅ Database schema design
- ✅ API endpoint definitions
- ✅ UI component requirements
- ✅ Business rules documented
- ✅ Notification strategy planned

### 2. Database Implementation
- ✅ Migration 012 created with 5 new tables:
  - `ApprovalWorkflows` - workflow templates
  - `ApprovalRules` - approval routing (by level/threshold)
  - `ApprovalRequests` - active approval instances
  - `ApprovalActions` - audit trail
  - `ApprovalDelegations` - temporary delegation
- ✅ All tables with proper indexes on TenantId, status, dates
- ✅ Foreign keys and cascade deletes configured
- ✅ Default workflows created for demo tenants
- ✅ Idempotent migration (IF NOT EXISTS)

---

## What Needs to Be Done Next 🔨

### Phase 2: API Implementation (Est. 1-2 days)

#### A. Approval Workflow Endpoints
```
GET /api/approval/workflows
POST /api/approval/workflows
PUT /api/approval/workflows/{id}
DELETE /api/approval/workflows/{id}
```

#### B. Approval Request Submission
```
POST /api/expenses/{id}/submit-for-approval
POST /api/timesheets/{id}/submit-for-approval
```

#### C. Approval Actions
```
POST /api/approval/requests/{id}/approve
POST /api/approval/requests/{id}/reject
POST /api/approval/requests/{id}/delegate
GET /api/approval/pending  -- Manager's approval queue
GET /api/approval/requests/{id}/history
```

#### D. Delegation Management
```
POST /api/approval/delegations
GET /api/approval/delegations
DELETE /api/approval/delegations/{id}
```

**Implementation Details:**
- Use existing DatabaseService for all queries
- Implement ICurrentTenantAccessor for tenant isolation
- Add status validation (can't approve if already rejected)
- Track who approved and when
- Support role-based routing (not just user-specific)
- Implement threshold-based routing

### Phase 3: UI Implementation (Est. 1-2 days)

#### A. Manager Dashboard Component
- New "Approvals" dashboard section
- Pending approvals count widget
- Quick approval cards with:
  - Item summary (expense amount, timesheet hours)
  - Submitter name
  - Submit date
  - Approve/Reject/Delegate buttons

#### B. Approval Modal
- Shows full item details (expense items, timesheet entries)
- Comments field for approver notes
- History of previous actions
- Delegate-to dropdown with date range

#### C. Submitter Experience
- "Submit for Approval" button on Expense/Timesheet detail
- Select workflow (if multiple exist)
- Optional comments field
- Shows current approval status and next approver
- Ability to withdraw if pending

#### D. Status Indicators
- Badge showing approval level (1/2, 2/2)
- Approval timeline
- Rejected reason display

### Phase 4: Testing & Polish (Est. 1 day)

- Unit tests for approval logic
- Integration tests for workflow routing
- UI testing for approval flow
- Edge cases: rejection → resubmit, delegation expiry
- Verify audit trail completeness

---

## Implementation Checklist

### API Endpoints
- [ ] GET /api/approval/workflows
- [ ] POST /api/approval/workflows
- [ ] PUT /api/approval/workflows/{id}
- [ ] DELETE /api/approval/workflows/{id}
- [ ] POST /api/expenses/{id}/submit-for-approval
- [ ] POST /api/timesheets/{id}/submit-for-approval
- [ ] GET /api/approval/pending
- [ ] POST /api/approval/requests/{id}/approve
- [ ] POST /api/approval/requests/{id}/reject
- [ ] POST /api/approval/requests/{id}/delegate
- [ ] GET /api/approval/requests/{id}/history
- [ ] POST /api/approval/delegations
- [ ] GET /api/approval/delegations
- [ ] DELETE /api/approval/delegations/{id}

### Blazor Components
- [ ] ApprovalDashboard.razor
- [ ] ApprovalCard.razor
- [ ] ApprovalModal.razor
- [ ] SubmitForApprovalModal.razor
- [ ] ApprovalHistoryPanel.razor
- [ ] DelegationManager.razor

### Database
- [x] Migration 012 created
- [ ] Database tested (IF NOT EXISTS working)
- [ ] Indexes verified for performance

### Testing
- [ ] API tests (happy path & edge cases)
- [ ] UI tests (approval flow)
- [ ] Audit trail verification
- [ ] Multi-level approval scenarios
- [ ] Delegation edge cases (overlap, expiry)

### Documentation
- [ ] API documentation updated
- [ ] Mobile app notes (view-only support)
- [ ] User guide for managers
- [ ] User guide for submitters

---

## Key Design Decisions

1. **Multi-Level Support**: Workflows can have 1+ approval levels
2. **Threshold Routing**: Different approvers for different amounts
3. **Role-Based**: Support both specific users and roles
4. **Delegation**: Temporary override for out-of-office managers
5. **Audit Trail**: Complete history of all actions
6. **Soft Delete**: Archive workflows, don't hard delete
7. **Status Tracking**: Pending → Approved/Rejected/Withdrawn
8. **Resubmit Flow**: Rejected items go back to Draft

---

## Success Criteria

✅ Database migrations run without errors  
⏳ All approval workflows routable via API  
⏳ Managers can view and act on pending approvals  
⏳ Complete audit trail of approvals  
⏳ Delegation working correctly  
⏳ Mobile app shows approval status  
⏳ End-to-end testing passes  

---

## Notes for Next Agent

1. Start with API endpoints (Program.cs) - follow existing pattern
2. Use DatabaseService.QueryAsync() for all queries
3. Remember ICurrentTenantAccessor for tenant filtering
4. Add status validation before state transitions
5. Test threshold-based routing logic thoroughly
6. Implement audit trail on every approval action
7. Blazor components should be reusable

---

## Related Documents
- APPROVAL-WORKFLOWS.md - Full specification
- src/Deployment/Migration/012_approval_workflows.sql - Database schema
- ROADMAP.md - Project roadmap

**Branch**: claude/approval-workflows  
**Next Review**: After API implementation complete
