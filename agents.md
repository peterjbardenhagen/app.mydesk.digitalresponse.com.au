# MyDesk Implementation Agents

**Version:** 1.0  
**Last Updated:** July 2026  
**Target Audience:** AI agents, developers, implementation teams

---

## Overview

This document describes the specialized implementation agents responsible for building and maintaining MyDesk. Each agent is designed to focus on specific areas of the application while maintaining consistency with the enterprise architecture, solution architecture, product requirements, and product strategy.

**See also:**
- **Enterprise Architecture:** `ENTERPRISE-ARCHITECTURE.md` - System principles, deployment architecture, security model, compliance framework
- **Solution Architecture:** `SOLUTION-ARCHITECTURE.md` - Technical design, API patterns, database schema, development workflow
- **Product Requirements:** `PRODUCT-REQUIREMENTS.md` - Feature specifications, acceptance criteria, data requirements
- **Product Strategy:** `PRODUCT-STRATEGY.md` - Market positioning, go-to-market, roadmap, financial projections
- **Development Guide:** `CLAUDE.md` - Local setup, build instructions, development best practices

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

### 3. Approval Workflow Agent

**Responsibility:** Implement approval request creation, approval actions, escalation, delegation, and audit tracking.

**Reference Documents:**
- SOLUTION-ARCHITECTURE.md § Event-Driven Workflows
- ENTERPRISE-ARCHITECTURE.md § Segregation of Duties, Multi-Level Approvals
- PRODUCT-REQUIREMENTS.md § Phase 2 & 4 (Approvals, Hierarchies)

**Key Tasks:**
- Create ApprovalRequest for each eligible approver
- Validate approver authority (threshold, department, category)
- Approval action (approve/reject) with optional comment
- Check if all approvals complete
- Escalation if pending > 3 days (Phase 4)
- Delegation to deputy for out-of-office (Phase 4)
- Notification trigger after approval decision

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

### 4. Notification Service Agent

**Responsibility:** Implement multi-channel notification delivery (Email, SMS, In-App), preference management, template substitution, and delivery queue management.

**Reference Documents:**
- SOLUTION-ARCHITECTURE.md § Notification Queue Pattern
- PRODUCT-REQUIREMENTS.md § Phase 3 & Preferences
- ENTERPRISE-ARCHITECTURE.md § Notifications & Communication

**Key Tasks:**
- Send email notifications via SendGrid
- Send in-app notifications with unread counting
- Template substitution ({{ApproverName}}, {{Amount}}, etc.)
- Respect user preferences (opt-out, quiet hours, digest frequency)
- Queue management with retry logic
- Delivery status tracking and failure handling

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

## Agent Collaboration Matrix

| Agent | Depends On | Feeds Into | Sync Frequency |
|-------|-----------|-----------|-----------------|
| Auth Agent | Database Agent | All Services | Weekly |
| Expense Agent | Auth, Approval, Notification | Dashboard | Weekly |
| Approval Agent | Auth, Database | Notification, Expense | Weekly |
| Notification Agent | All Services | None (broadcast) | Daily |
| Photo Agent | Expense Agent | Blob Storage | On-demand |
| Components Agent | All Services | Deployment | Weekly |
| Dashboard Agent | Expense, Approval | None | Weekly |
| Database Agent | Requirements | All Agents | As-needed |
| DevOps Agent | All Agents | Infrastructure | Weekly |
| QA Agent | All Services | Deployment | Weekly |

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
| 1.0 | 2026-07-05 | Claude | Initial agent definitions and collaboration matrix |

