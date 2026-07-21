# Phase 5: Notifications & Alerts - Implementation Plan

**Version:** 1.0  
**Status:** In Progress  
**Target Release:** Q4 2026  
**Last Updated:** July 6, 2026

---

## Overview

Phase 5 integrates the notification service with Phase 4 organizational features, enabling:
- Real-time budget threshold alerts
- Approval delegation notifications
- Approval escalation notifications
- User preference management
- Multi-channel delivery (Email, In-App, SMS)

---

## Features to Implement

### 1. Budget Alert Service

**Purpose:** Monitor departmental spending and alert when thresholds approached

**Service:** `BudgetAlertService.cs`
- `CheckBudgetThresholdAsync()` - Monitor budget usage percentage
- `SendBudgetAlertAsync()` - Notify department managers when threshold exceeded
- `GetBudgetAlertHistoryAsync()` - Retrieve alert history
- `AcknowledgeBudgetAlertAsync()` - Mark alert as read

**Triggers:**
- When spending reaches 80% (configurable)
- When spending reaches 100% (budget fully allocated)
- When overspend occurs and AllowOverspend=false

**Recipients:**
- Department managers
- Finance team members
- Budget owners

**Template Variables:**
- `{DepartmentName}` - Department being alerted
- `{BudgetUsage}%` - Current percentage used
- `{SpentAmount}` - Total spent
- `{AllocatedAmount}` - Total allocated
- `{RemainingAmount}` - Funds remaining
- `{AlertThreshold}%` - Alert threshold

### 2. Approval Delegation Notifications

**Purpose:** Notify delegates when approval authority routed to them

**Service:** `ApprovalNotificationService.cs` (extends ApprovalEscalationService)
- `NotifyDelegateAsync()` - Implement delegation notification
- `NotifyEscalationAsync()` - Implement escalation notification
- `SendApprovalReminderAsync()` - Send pending approval reminders
- `NotifyApprovalCompletedAsync()` - Notify original approver when delegate acts

**Triggers:**
- When approval routed to delegate
- When approval escalated to next level
- When approval approved by delegate
- When approval rejected by delegate
- After 2 days of pending approval (reminder)

**Notification Events:**
- `ApprovalDelegated` - Delegate receives new approval
- `ApprovalEscalated` - Higher authority receives escalated approval
- `ApprovalApproved` - Requester and delegates notified of approval
- `ApprovalRejected` - Requester and managers notified of rejection
- `ApprovalReminder` - Delegate reminded of pending approval

**Template Variables:**
- `{ApprovalType}` - Expense/PurchaseOrder/Invoice/Timesheet
- `{Amount}` - Amount being approved
- `{Description}` - Item description
- `{Requester}` - Who submitted
- `{DueDate}` - When approval needed by
- `{ApprovalLink}` - Direct link to approval in app

### 3. Notification Preferences UI

**UI Components:**
- `NotificationPreferences.razor` - User notification settings page
  - Email notifications (on/off)
  - In-app notifications (on/off)
  - SMS notifications (on/off) - Phase 6
  - Budget alert frequency (immediately/daily digest/weekly)
  - Approval notification frequency
  - Digest preferences (individual emails vs daily summary)

**Page Route:** `/user/notification-preferences`

**Database:**
- `NotificationSettings` table (already exists per NotificationService)
  - EnableEmailNotifications
  - EnableInAppNotifications
  - EnableSmsNotifications (Phase 6)
  - BudgetAlertFrequency (Immediate, Daily, Weekly)
  - ApprovalAlertFrequency (Immediate, Daily, Weekly)
  - DigestEnabled
  - CreatedAt, UpdatedAt

### 4. Notification Templates Management

**Purpose:** Allow admins to customize notification messages

**UI Components:**
- `NotificationTemplates.razor` - Admin page for template management
- `NotificationTemplateEditor.razor` - WYSIWYG template editor

**Page Route:** `/admin/notification-templates`

**Template Management:**
- Select event type (ApprovalDelegated, BudgetAlert, etc.)
- Edit subject line
- Edit HTML body with placeholder helper
- Preview rendered output
- Enable/disable template
- Test send to self

### 5. Notification History & Dashboard

**UI Components:**
- `NotificationCenter.razor` - User notification center
- `NotificationHistory.razor` - View past notifications

**Features:**
- Mark as read/unread
- Delete notifications
- Filter by type
- Search notifications
- Archive old notifications

---

## Implementation Strategy

### Phase 5.1: Budget Alerts (First Priority)

**Steps:**
1. Create `BudgetAlertService.cs`
2. Add background job to check budgets (Hangfire)
3. Create notification templates for budget alerts
4. Add database tables for alert history
5. Create UI for notification preferences
6. Write unit tests for BudgetAlertService
7. Write integration tests for notification flow

**Timeline:** 2-3 days

### Phase 5.2: Approval Notifications (Second Priority)

**Steps:**
1. Implement `NotifyDelegateAsync()` in ApprovalEscalationService
2. Implement `NotifyEscalationAsync()` in ApprovalEscalationService
3. Create notification templates for approval events
4. Call notification service from approval workflow
5. Add tests for approval notifications
6. Update UI to show notification status

**Timeline:** 2-3 days

### Phase 5.3: Notification Preferences & Templates UI

**Steps:**
1. Create notification preferences page
2. Create template management admin page
3. Add template testing functionality
4. Write tests for preference management
5. Update navigation to include notification center

**Timeline:** 2-3 days

### Phase 5.4: Background Jobs & Reminders

**Steps:**
1. Setup Hangfire job scheduler
2. Create budget check job (daily)
3. Create approval reminder job (checks pending >2 days)
4. Create digest compilation job (if daily digest enabled)
5. Write tests for job scheduling

**Timeline:** 2-3 days

---

## Database Schema Changes

### New Tables

**NotificationSettings** (likely exists, verify)
```sql
CREATE TABLE NotificationSettings (
    SettingId INT PRIMARY KEY IDENTITY(1,1),
    TenantId INT NOT NULL,
    UserId INT NOT NULL,
    EnableEmailNotifications BIT DEFAULT 1,
    EnableInAppNotifications BIT DEFAULT 1,
    BudgetAlertFrequency NVARCHAR(50) DEFAULT 'Immediate',
    ApprovalAlertFrequency NVARCHAR(50) DEFAULT 'Immediate',
    DigestEnabled BIT DEFAULT 0,
    CreatedAt DATETIME2,
    UpdatedAt DATETIME2,
    
    CONSTRAINT FK_Settings_Tenant FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId),
    CONSTRAINT FK_Settings_User FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT UQ_Settings_User UNIQUE (TenantId, UserId)
);
```

**BudgetAlerts** (new)
```sql
CREATE TABLE BudgetAlerts (
    AlertId INT PRIMARY KEY IDENTITY(1,1),
    TenantId INT NOT NULL,
    DepartmentId INT NOT NULL,
    BudgetId INT NOT NULL,
    UsagePercentage INT,
    SpentAmount DECIMAL(12,2),
    AllocatedAmount DECIMAL(12,2),
    AlertType NVARCHAR(50), -- 'Threshold', 'Full', 'Overspend'
    AlertLevel NVARCHAR(50), -- 'Warning', 'Critical'
    IsAcknowledged BIT DEFAULT 0,
    AcknowledgedAt DATETIME2,
    AcknowledgedBy INT,
    CreatedAt DATETIME2,
    
    CONSTRAINT FK_Alerts_Tenant FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId),
    CONSTRAINT FK_Alerts_Department FOREIGN KEY (DepartmentId) REFERENCES Departments(DepartmentId),
    CONSTRAINT FK_Alerts_Budget FOREIGN KEY (BudgetId) REFERENCES DepartmentBudgets(BudgetId),
    INDEX IX_BudgetAlerts_DepartmentId (DepartmentId),
    INDEX IX_BudgetAlerts_Unacknowledged (IsAcknowledged)
);
```

**ApprovalNotifications** (new)
```sql
CREATE TABLE ApprovalNotifications (
    NotificationId INT PRIMARY KEY IDENTITY(1,1),
    TenantId INT NOT NULL,
    ApprovalId INT NOT NULL,
    EventType NVARCHAR(50), -- 'Delegated', 'Escalated', 'Approved', 'Rejected'
    RecipientUserId INT NOT NULL,
    SentAt DATETIME2,
    DeliveredAt DATETIME2,
    Status NVARCHAR(50), -- 'Pending', 'Sent', 'Delivered', 'Failed'
    
    CONSTRAINT FK_ApprovalNotif_Tenant FOREIGN KEY (TenantId) REFERENCES Tenants(TenantId),
    CONSTRAINT FK_ApprovalNotif_User FOREIGN KEY (RecipientUserId) REFERENCES Users(UserId),
    INDEX IX_ApprovalNotif_Recipient (RecipientUserId),
    INDEX IX_ApprovalNotif_Status (Status)
);
```

---

## Service Integration Points

### BudgetAlertService Integration
```csharp
// In BudgetService after amount update
public async Task<bool> AddExpenseAsync(int tenantId, int deptId, decimal amount)
{
    // ... existing code ...
    
    // Check and send alert if threshold exceeded
    await _budgetAlertService.CheckBudgetThresholdAsync(tenantId, deptId);
}
```

### ApprovalNotificationService Integration
```csharp
// In ApprovalEscalationService when routing approval
public async Task<ApprovalRouting> RouteApprovalAsync(...)
{
    var result = await ResolveDelegationAsync(...);
    
    // Notify delegate if delegated
    if (result.IsDelegated)
        await _notificationService.NotifyDelegateAsync(tenantId, result.ApproverId, approvalId);
    
    return result;
}
```

---

## Testing Strategy

### Unit Tests

1. **BudgetAlertServiceTests**
   - Test threshold detection (80%)
   - Test full budget alert
   - Test overspend alert
   - Test alert acknowledgment

2. **ApprovalNotificationServiceTests**
   - Test delegation notification
   - Test escalation notification
   - Test approval completion notification
   - Test reminder generation

3. **NotificationPreferenceTests**
   - Test preference save/load
   - Test preference filtering on notifications

### Integration Tests

1. **Budget Alert Workflow**
   - Create budget
   - Add expense that reaches 80%
   - Verify alert created
   - Verify notification queued
   - Verify email in queue

2. **Approval Notification Workflow**
   - Create delegation
   - Submit approval
   - Verify routed to delegate
   - Verify delegate notified
   - Delegate approves
   - Verify original approver notified

---

## Configuration & Settings

**Feature Flags:**
- `EnableBudgetAlerts` - Enable budget threshold monitoring
- `EnableApprovalNotifications` - Enable approval workflow notifications
- `EnableNotificationTemplateEditor` - Allow admins to edit templates

**Settings (appsettings.json):**
```json
{
  "Notifications": {
    "BudgetAlertThresholdPercent": 80,
    "ApprovalReminderDays": 2,
    "DigestSchedule": "08:00", // Morning digest time
    "MaxNotificationRetries": 3,
    "NotificationRetryDelayMinutes": 5,
    "FromEmail": "notifications@mydesk.app",
    "FromName": "MyDesk Notifications"
  }
}
```

---

## Deployment Considerations

### Database Migration
- Create new tables (NotificationSettings, BudgetAlerts, ApprovalNotifications)
- Verify NotificationTemplates table exists
- Seed default templates for event types

### Feature Rollout
- Deploy with feature flags disabled
- Test in staging
- Enable flags in production
- Monitor notification queue

### Performance
- Ensure budget alert job doesn't impact query performance
- Implement notification batching for high-volume scenarios
- Monitor email queue size

---

## Success Metrics

- Users receive budget alerts within 5 minutes of threshold
- 99% of approval notifications delivered within 1 minute
- <1% notification send failures
- Users configure preferences (optional metric)
- Admin customizes templates (optional metric)

---

## Timeline

| Week | Phase | Deliverables |
|------|-------|---|
| Week 1 | 5.1 | BudgetAlertService + tests + migration |
| Week 2 | 5.2 | ApprovalNotificationService + tests |
| Week 3 | 5.3 | UI components + template editor |
| Week 4 | 5.4 | Background jobs + refinement |

---

## Known Limitations

- SMS notifications deferred to Phase 6
- Notification digest generation in Phase 5.4 (optional)
- Slack/Teams integration deferred to Phase 7
- Webhook notifications deferred to Phase 7

---
