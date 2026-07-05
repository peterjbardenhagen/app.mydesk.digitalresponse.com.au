# MyDesk Product Requirements Document (PRD)

**Version:** 1.0  
**Last Updated:** July 2026  
**Target Audience:** Product managers, business analysts, stakeholders

---

## Executive Summary

MyDesk is a cloud-native, enterprise-grade SaaS platform for managing employee expense claims and approval workflows. Designed specifically for Australian enterprises subject to Privacy Act compliance, MyDesk automates the expense submission, approval, and reconciliation processes while maintaining complete audit trails required for regulatory compliance.

The platform serves mid-sized organizations (100-5000 employees) with distributed teams, complex approval hierarchies, and stringent financial controls. It replaces fragmented solutions (email chains, spreadsheets, disconnected tools) with a unified, auditable system that cuts approval times by 80% while reducing compliance risk.

---

## Product Vision

**Long-term Goal:** MyDesk becomes the standard platform for managing expense workflows in Australian enterprises, trusted by CFOs, compliance officers, and employees alike for its security, auditability, and ease of use.

**Core Promise:** "Complex approval workflows made simple, auditable, and compliant."

---

## User Personas

### 1. CFO / Finance Director
**Goal:** Maintain financial controls while reducing operational overhead.
**Pain Points:**
- Can't see real-time expense status across the organization
- Manual reconciliation is time-consuming and error-prone
- Audit requirements mean extensive manual documentation
- No visibility into approval bottlenecks

**Needs:**
- Executive dashboard showing expense metrics
- Automated audit trail for regulatory compliance
- Real-time approval workflow status
- Budget tracking and forecasting

**Success Metric:** 50% reduction in monthly reconciliation time

### 2. Finance Manager
**Goal:** Process expense claims efficiently while enforcing policies.
**Pain Points:**
- Email chains with missing receipts or attachments
- Difficulty tracking which expenses need follow-up
- Manual data entry from receipts
- No way to bulk-approve similar expenses

**Needs:**
- Clear inbox showing expenses requiring action
- AI-powered receipt OCR for auto-populated fields
- Bulk action capabilities
- Customizable approval workflows

**Success Metric:** 20 minutes to process 100 expense claims (vs. 2+ hours currently)

### 3. Line Manager
**Goal:** Quickly review and approve team member expenses.
**Pain Points:**
- Hard to find expense details when approving via email
- No context on whether amount is unusual
- Can't easily see team spending patterns

**Needs:**
- Mobile-friendly approval interface
- Quick access to previous similar expenses for context
- Ability to reject with reason (feedback to submitter)
- Notifications when expenses need approval

**Success Metric:** 90% of approvals completed within 24 hours

### 4. Finance Operations (Accounting)
**Goal:** Reconcile expenses with GL accounts and accounts payable.
**Pain Points:**
- Expenses submitted without cost center codes
- Duplicate expense submissions
- No link between approved expenses and payment
- Manual journal entries

**Needs:**
- Integration with accounting systems (Xero, MYOB)
- Cost center and project tracking
- Duplicate detection
- Export for payment processing

**Success Metric:** 100% of approved expenses reconciled automatically

### 5. Employee
**Goal:** Claim personal out-of-pocket expenses easily.
**Pain Points:**
- Don't know when something is approvable vs. not
- Have to fill out long forms
- Don't know approval status or timelines
- Reimbursement takes weeks

**Needs:**
- Simple, mobile-first submission process
- Real-time status updates
- Photo capture for receipts (instead of manual entry)
- Quick reimbursement (within 48 hours)

**Success Metric:** 5 minutes to submit an expense with receipt

### 6. IT / Security Admin
**Goal:** Ensure the system is secure and compliant.
**Pain Points:**
- Can't audit user activity
- No way to enforce compliance policies
- Data isn't encrypted
- No access controls

**Needs:**
- Complete audit logs (who, what, when, where, why)
- User access management and role-based controls
- Data encryption at rest and in transit
- Compliance reports for auditors

**Success Metric:** Pass ISO 27001 and SOC 2 Type II audits

---

## Core Features (MVP - Phase 1-3)

### Phase 1: Authentication & Multi-Tenancy (Complete)

**Feature 1.1: Domain-Based Tenant Assignment**
- Users sign up using corporate email address
- Domain automatically maps to tenant (e.g., @digitalresponse.com.au)
- First-time users auto-provisioned into correct tenant
- No admin intervention needed for standard onboarding

**Acceptance Criteria:**
- ✅ When user logs in with @digitalresponse.com.au, they land in Digital Response tenant
- ✅ User cannot access other tenant data even if JWT is modified
- ✅ New domain can be added via tenant admin interface

**Feature 1.2: Role-Based Access Control**
- 4 roles: Employee, Manager, Director, Administrator
- Roles assigned at tenant level
- Can be refined later to department/team level
- Roles map to approval authority

**Acceptance Criteria:**
- ✅ Employee cannot approve any expense
- ✅ Manager can approve expenses up to $5,000
- ✅ Director can approve all expenses
- ✅ Administrator can modify roles for all users

**Feature 1.3: JWT Authentication**
- Username/password authentication
- JWT token issued on successful login
- Token includes tenant_id and user roles
- Token expires after 1 hour
- Refresh token available for mobile apps (future)

**Acceptance Criteria:**
- ✅ Valid credentials → JWT issued
- ✅ Invalid credentials → 401 Unauthorized
- ✅ Expired token → 401 Unauthorized
- ✅ Modified JWT claims → denied by backend

---

### Phase 2: Expense Management (Complete)

**Feature 2.1: Expense Submission**
- Employee submits expense with amount, category, description
- System calculates whether approval needed
- Attachments/receipts uploaded at submission
- Email confirmation sent to submitter

**Acceptance Criteria:**
- ✅ Expense < $500 → No approval needed, immediately marked paid
- ✅ Expense $500-$5,000 → Requires manager approval
- ✅ Expense > $5,000 → Requires director + CFO approval
- ✅ Category required from predefined list
- ✅ Receipt optional but recommended

**Feature 2.2: Photo Processing**
- User can upload photo from mobile or web
- System crops and converts to square (for avatar)
- System creates thumbnail for list views
- Photo compressed to < 1MB for efficient storage

**Acceptance Criteria:**
- ✅ Supported formats: JPEG, PNG
- ✅ Max file size: 5MB
- ✅ Min dimensions: 100x100px
- ✅ Output: Square 500x500px + thumbnail 100x100px

**Feature 2.3: Receipt Photo Capture & OCR**
- Mobile app supports photo capture
- AI extracts: supplier, date, total amount, GST amount
- User can edit extracted fields before saving
- Receipt photo stored with expense

**Acceptance Criteria:**
- ✅ Photo captured and displayed
- ✅ Key fields extracted (supplier, date, amount)
- ✅ Extraction confidence shown (70-100%)
- ✅ User can edit any field
- ✅ Photo searchable by receipt date/amount

---

### Phase 3: Notifications (Complete - Just Merged)

**Feature 3.1: Email Notifications**
- Approvers notified when expense requires action
- Submitters notified of approval/rejection decisions
- Configurable templates per tenant
- Customizable from/subject/body text

**Acceptance Criteria:**
- ✅ Template includes {{placeholder}} substitution (e.g., {{ApproverName}})
- ✅ Email sent via SendGrid within 5 seconds
- ✅ Failed emails retried with exponential backoff
- ✅ User can opt-out of email notifications

**Feature 3.2: In-App Notifications**
- Real-time notifications in browser (Blazor Server)
- Notification bell icon in header with unread count
- Dropdown shows recent 20 notifications
- Click notification to jump to related expense

**Acceptance Criteria:**
- ✅ Bell icon shows unread count in red badge
- ✅ Dropdown sorts by newest first
- ✅ Mark as read updates count
- ✅ Click "View" navigates to expense details

**Feature 3.3: Notification Preferences**
- User can enable/disable email notifications
- User can set email digest frequency (Immediate, Daily, Weekly)
- User can set quiet hours (don't send 10pm-6am)
- SMS notifications for future

**Acceptance Criteria:**
- ✅ Preferences persist across logins
- ✅ Email digest delays emails until scheduled time
- ✅ Quiet hours prevent notifications during sleep time
- ✅ User can opt-out of all channels at once

---

### Phase 4: Team & Department Management (Coming Q3 2026)

**Feature 4.1: Department Structure**
- Admin can create departments and teams
- Users assigned to primary department
- Department has budget, approval authority, managers
- Department head can pre-approve within sub-threshold

**Feature 4.2: Approval Hierarchies**
- Workflows defined as rules (amount → approvers)
- Manager can delegate approval to deputy
- Out-of-office: Auto-delegate to backup
- Escalation: If pending > 3 days, escalate to director

**Feature 4.3: Bulk User Import**
- Admin uploads CSV with user data
- System creates users in bulk
- Auto-assigns departments based on CSV
- Sends welcome emails with temporary password

---

### Phase 5: Expense & Timesheet Enhancements (Coming Q3 2026)

**Feature 5.1: Expense Categories & Cost Centers**
- Predefined categories (Meals, Travel, Supplies, etc.)
- Cost center/project assignment
- Tag expenses for budget tracking
- Category-based approval rules (Food ≤ $50)

**Feature 5.2: Multi-Currency Support**
- Default currency per tenant (AUD)
- User can specify alternative currency for expense
- System auto-converts using daily rates
- Accounting records in home currency

**Feature 5.3: Timesheet Management**
- Weekly timesheet submission
- Approval workflow similar to expenses
- Track billable vs. non-billable hours
- Integration with payroll (future)

---

### Phase 6: Dashboard & Analytics (Coming Q3 2026)

**Feature 6.1: Executive Dashboard (CFO)**
- Total expenses month-to-date
- Expenses by department, category, status
- Average approval time
- Top spenders, top categories
- Budget vs. actual tracking

**Feature 6.2: Manager Dashboard**
- Team expenses (pending, approved, paid)
- Overdue approvals requiring action
- Team spending by category
- Cost center utilization

**Feature 6.3: Employee Dashboard**
- My expenses (submitted, approved, paid)
- Reimbursement status
- Monthly spend summary
- Submission guidelines

---

## Non-Functional Requirements

### Security Requirements

**Req: SEC-001 - Encryption in Transit**
- All API communication over TLS 1.3
- Minimum cipher suite: TLS_AES_256_GCM_SHA384
- Automatic redirect from HTTP to HTTPS
- HSTS header on all responses

**Req: SEC-002 - Encryption at Rest**
- Database encrypted with SQL Server Transparent Data Encryption
- Blob storage encrypted with Azure Storage Service Encryption
- Encryption keys managed in Azure Key Vault
- Automatic key rotation yearly

**Req: SEC-003 - Authentication**
- Passwords hashed with BCrypt (workFactor=12)
- Session timeout after 1 hour of inactivity
- Failed login throttled: 5 attempts → 15 minute lockout
- MFA support (future)

**Req: SEC-004 - Authorization**
- All API endpoints validate tenant_id claim
- All database queries filter by TenantId
- Row-level security enforced at application layer
- No direct database access from client

**Req: SEC-005 - Audit Logging**
- Every data modification logged to ComplianceAuditLog
- Logs include: who, what, when, where, why
- Logs immutable (no UPDATE/DELETE)
- 7-year retention policy

**Req: SEC-006 - Data Residency**
- All data stored in Australia (AU East Azure region)
- Backups in Australia only
- No data transferred internationally
- Compliance with Privacy Act

### Performance Requirements

**Req: PERF-001 - API Response Time**
- 95% of requests complete in < 500ms
- 99% of requests complete in < 2s
- Includes database query time and network latency
- Measured from client to first byte

**Req: PERF-002 - Concurrent Users**
- Support 100 concurrent users per tenant
- 1000 concurrent users across all tenants
- Database connection pool sized for peak load
- No degradation of response time with increased load

**Req: PERF-003 - Database Performance**
- Queries with < 100ms execution time
- Indexes on all WHERE and JOIN columns
- Query plans reviewed quarterly
- Slow query log monitored daily

**Req: PERF-004 - Uptime SLA**
- 99.9% uptime commitment (43 minutes downtime/month)
- Planned maintenance window: Tuesday 2-4 AM UTC
- Automatic failover within 60 seconds
- Health checks every 30 seconds

### Compliance Requirements

**Req: COMP-001 - Privacy Act Compliance**
- Data residency in Australia (non-negotiable)
- User can request data export (GDPR-style)
- User can request deletion (soft delete)
- Consent tracking for communications

**Req: COMP-002 - ISO 27001 Compliance**
- Documented information security policy
- Risk assessments conducted annually
- Access controls with least privilege
- Incident response plan

**Req: COMP-003 - SOC 2 Type II Compliance**
- Annual SOC 2 Type II audit
- Security, availability, and confidentiality controls
- Logical access controls reviewed
- Change management documented

**Req: COMP-004 - Sarbanes-Oxley Compliance**
- Complete audit trail of financial transactions
- Segregation of duties (submitter ≠ approver)
- Approval workflow enforced by system
- Executive sign-off capability

**Req: COMP-005 - Immutable Audit Log**
- No application code can delete audit records
- Database triggers enforce immutability
- Archive old logs (> 7 years) to cold storage
- Hash verification for tamper detection (future)

### Scalability Requirements

**Req: SCALE-001 - Database Scalability**
- Support up to 1M expenses in database
- Query performance < 100ms even with large datasets
- Partitioning strategy for tables > 100M rows
- Read replicas for reporting (future)

**Req: SCALE-002 - Application Scalability**
- Stateless design allows horizontal scaling
- Auto-scale: 2-10 instances based on load
- Connection pooling supports 100+ database connections
- Session data in database, not in memory

**Req: SCALE-003 - Storage Scalability**
- Blob storage supports unlimited file storage
- Pricing scales linearly with usage
- Archive old receipts to cold storage (< $0.01/GB/month)
- CDN caching for static assets

### Usability Requirements

**Req: UX-001 - Mobile Responsiveness**
- Full functionality on screens 320px and larger
- Touch-friendly buttons (minimum 44x44px)
- Landscape and portrait orientation support
- Native mobile app planned for Phase 4

**Req: UX-002 - Accessibility**
- WCAG 2.1 Level AA compliance
- Keyboard navigation supported
- Color contrast ratio minimum 4.5:1
- Screen reader compatible (ARIA labels)

**Req: UX-003 - Internationalization**
- Multi-language support (English, Spanish, Mandarin)
- Regional currency and date formatting
- RTL language support (future)

---

## Acceptance Criteria Format

For each feature, acceptance criteria follow this format:

```
Feature: [Name]
User Story: As [persona], I want [action] so that [benefit]

Acceptance Criteria:
✅ Scenario 1: [Given/When/Then]
✅ Scenario 2: [Given/When/Then]
❌ Scenario 3 (Rejection): [Given/When/Then]

Definition of Done:
- Code written and reviewed
- Unit tests pass (>80% coverage)
- Integration tests pass
- Manual testing complete
- Documentation updated
- No blockers in QA
```

---

## Data Requirements

### Data Schema Layers

**Layer 1: Core Business**
- Users, Tenants, Departments, Teams
- Expenses, Timesheets, Projects
- UserPhotos, ExpenseReceipts

**Layer 2: Approvals & Workflows**
- ApprovalWorkflows, ApprovalRules
- ApprovalRequests, ApprovalActions
- ApprovalPermissions, ApprovalDelegations

**Layer 3: Notifications**
- NotificationTemplates, NotificationSettings
- NotificationLog, EmailQueue, InAppNotifications
- NotificationEventPreferences, NotificationState

**Layer 4: Compliance**
- ComplianceAuditLog (immutable, append-only)
- SecurityAuditEvents, DataExportAudit
- RateLimitingRules, RateLimitingViolations

**Layer 5: Billing**
- ClientBillingConfig, ClientInvoice
- ClientUsageLog, ClientBillingHistory
- ClientOnboardingSession

### Data Retention Policy

| Data Type | Retention | Archive Destination |
|-----------|-----------|-------------------|
| Transaction Records (Expenses, Approvals) | 7 years | Cold storage |
| Audit Logs | 7 years | Cold storage, immutable |
| User Session Logs | 6 months | Delete |
| Error Logs | 90 days | Delete |
| Photo/Receipt Files | While expense exists | Delete with expense |

---

## Integration Requirements

### Accounting System Integration (Future)

**Xero Integration:**
- Map approved expenses to Xero bills
- Cost center → Tracking category
- Approval date → Invoice date
- Sync daily at 2 AM UTC

**MYOB Integration:**
- Similar to Xero
- Export format: MYOB TXT
- Sync via batch job

### Email System Integration

**SendGrid:**
- Transactional email delivery
- List management for unsubscribes
- Open tracking, click tracking
- Webhook callbacks for delivery status

**Exchange/Office 365 (Future):**
- Calendar integration for out-of-office
- Approval notifications in Outlook
- Expense submission from Outlook

### Payment Integration (Future)

**Xpay / PaySmart:**
- Batch payment files for approved expenses
- Payment status tracking
- Reconciliation reports

---

## Success Metrics

### Business Metrics

| Metric | Target | Baseline |
|--------|--------|----------|
| Approval time (avg) | < 24 hours | 5 days (email-based) |
| Expense accuracy | > 99% | 85% (manual) |
| Compliance audit pass rate | 100% | 95% (gaps found yearly) |
| Cost per transaction | < AUD 1 | AUD 5 (manual) |
| Time to process 100 expenses | 20 min | 2+ hours (manual) |

### Product Metrics

| Metric | Target |
|--------|--------|
| Monthly active users | 500+ per tenant |
| API availability | 99.9% |
| API response time (p95) | < 500ms |
| Page load time | < 3 seconds |
| Mobile app adoption | 60% of users (Phase 4+) |

### User Satisfaction

| Metric | Target |
|--------|--------|
| NPS (Net Promoter Score) | > 70 |
| CSAT (Customer Satisfaction) | > 4.5/5 |
| Feature adoption rate | > 80% |
| User retention | > 90% annual |

---

## References

- **Enterprise Architecture:** See `ENTERPRISE-ARCHITECTURE.md`
- **Solution Architecture:** See `SOLUTION-ARCHITECTURE.md`
- **Product Strategy:** See `PRODUCT-STRATEGY.md`
- **Development Guide:** See `CLAUDE.md`
- **Implementation Agents:** See `agents.md`

