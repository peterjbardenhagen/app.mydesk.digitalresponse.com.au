# MyDesk Product Roadmap & Development Phases

**Last Updated:** July 21, 2026  
**Current Phase:** Phase 7 (Mobile Apps) — Phase 5 & 6 shipped  
**Release:** v1.0.0 deployed to production

---

## Overview

MyDesk is a comprehensive cloud-based expense management and business operations platform targeting mid-market organizations (50-5000 employees). The development roadmap is organized into implementation phases, with delivery tracked via GitHub PRs, CI/CD pipelines, and nightly DevOps automation.

### Product Vision
Enable organizations to digitize, streamline, and automate their expense management, team operations, and business workflows with enterprise-grade security, compliance, and analytics.

---

## Phase Overview

| Phase | Status | Timeline | Key Features |
|-------|--------|----------|--------------|
| **Phase 1** | ✅ Shipped | Q2 2026 | Expense submission, receipt capture, OCR |
| **Phase 2** | ✅ Shipped | Q2-Q3 2026 | Multi-level approvals, escalation, delegation |
| **Phase 3** | ✅ Shipped | Q3 2026 | MYOB/Xero integration, bank reconciliation |
| **Phase 4** | ✅ Shipped | Q4 2026 | Teams, departments, budget tracking |
| **Phase 5** | ✅ Shipped | Q4 2026 | Email/SMS notifications, notification center |
| **Phase 6** | ✅ Shipped | Q4 2026 - Q1 2027 | Executive dashboards, analytics, reporting |
| **Phase 7** | 🔄 In Progress | Q1-Q2 2027 | Mobile app (iOS/Android), desktop shell |
| **Phase 8** | 📋 Planned | Q2 2027 | AI-powered receipt parsing & categorization |
| **Phase 9** | 📋 Planned | Q2-Q3 2027 | Advanced analytics, forecasting, ML insights |
| **Phase 10** | 📋 Planned | Q3 2027+ | Supply chain & procurement, multi-org management |

---

## Detailed Phase Breakdown

### ✅ Phase 1: Core Expense Management (Shipped)

**Timeline:** Q2 2026  
**Owner:** Core Platform Agent  
**Status:** Production ready — v1.0.0

#### Features Delivered
- User expense submission with photo capture
- OCR receipt extraction (supplier, date, amount)
- Multi-currency support (AUD, USD, EUR, GBP)
- Expense categorization (meals, accommodation, transport, etc.)
- Draft/saved/submitted states
- Receipt attachment storage
- Expense history and search

#### Technology Stack
- ASP.NET Core 10 (C#)
- SQL Server 2022
- Blazor Server for UI
- MudBlazor component library
- OpenAI Vision API for receipt OCR

#### Database Schema
- `Expenses` (core table)
- `ExpenseReceipts` (OCR results)
- `ExpenseCategories` (user-defined categories)
- `ExpenseAttachments` (file metadata)

---

### ✅ Phase 2: Approval Workflows (Shipped)

**Timeline:** Q2-Q3 2026  
**Owner:** Core Platform Agent  
**Status:** Production ready — v1.0.0

#### Features Delivered
- Multi-level approval chains
- Manager→Director→CFO escalation
- Approval delegation (temporary reassignment)
- Bulk approval (select multiple expenses)
- Approval comments and history
- Rejection with mandatory feedback
- Approval SLAs (24-48 hour targets)
- Approval notifications via email/in-app
- Audit trail for compliance

#### Technology Stack
- Orchestrator-Worker pattern for workflow state machine
- Background job processing (Hangfire)
- SignalR for real-time notification updates
- Approval rules engine

#### Database Schema
- `ApprovalRules` (hierarchy configuration)
- `ApprovalChains` (workflow state tracking)
- `ApprovalDelegations` (temporary escalation)
- `ApprovalHistory` (audit log)

---

### ✅ Phase 3: Integrations (Shipped)

**Timeline:** Q3 2026  
**Owner:** Integrations Agent  
**Status:** Production ready — v1.0.0

#### Features Delivered
- **MYOB Integration**
  - Expense posting to MYOB
  - Category mapping
  - Contact sync
  - Tax code application
  - Automated journal entry creation

- **Xero Integration**
  - Real-time expense import
  - Invoice matching
  - Account reconciliation
  - Multi-currency handling

- **Bank Reconciliation**
  - Transaction import
  - Expense-to-bank matching
  - Reconciliation workflows
  - Variance reporting

- **Email Integration**
  - Receipt forwarding to app email
  - Automatic receipt capture
  - Expense creation from email attachments

- **Outlook Add-in**
  - Change Request workflow
  - Email→Contact creation
  - Legal Folio workflow

#### Technology Stack
- REST/OAuth 2.0 integrations
- Webhook handlers for real-time updates
- Data transformation layer for format mapping
- Queue-based processing (Hangfire)

#### Database Schema
- `IntegrationMappings` (field mapping)
- `IntegrationLog` (audit trail)
- `BankTransactions` (imported transactions)
- `BankReconciliations` (reconciliation state)

---

### ✅ Phase 4: Teams & Departments (Shipped)

**Timeline:** Q4 2026  
**Owner:** Organizations Agent  
**Status:** Production ready — v1.0.0

#### Features Delivered
- Team creation and management
- Department hierarchies
- Team member CRUD operations
- Role-based access control (RBAC)
- Department budget tracking
- Budget enforcement (hard/soft limits)
- Team expense rollup
- Bulk user import (CSV)
- Team lead assignment
- Department approver chains
- Approval delegation & escalation
- Budget alerts & thresholds

#### Technology Stack
- ASP.NET Core dependency injection
- Policy-based authorization
- Entity Framework Core for data access
- Bulk import processing (streaming)

#### Database Schema
- `Departments` (org structure)
- `Teams` (team grouping)
- `TeamMembers` (team-user mapping)
- `BudgetLimits` (department budgets)
- `BudgetTracking` (month-to-date spending)
- `TeamRoles` (team-specific roles)
- `ApprovalDelegation` (delegation rules)
- `ApprovalEscalation` (escalation chains)
- `BulkUserImportLog` (import audit)

---

### ✅ Phase 5: Notifications & Alerts (Shipped)

**Timeline:** Q4 2026  
**Owner:** Notification Agent  
**Status:** Production ready — v1.0.0

#### Features Delivered
- **Multi-channel Notifications**
  - Email (SendGrid + SMTP fallback)
  - SMS (Twilio)
  - In-app notifications
  - Push notifications (web)

- **Notification Triggers**
  - Invoice created/sent
  - Invoice overdue
  - Quote sent
  - Job status changed
  - Order despatched
  - Approval required
  - Expense rejected
  - Budget threshold alerts

- **Notification Preferences**
  - Per-user channel preferences
  - Quiet hours (suppress notifications)
  - Digest frequency (daily/weekly)
  - Delivery tracking
  - Unsubscribe management

- **Notification Center**
  - Unified inbox for all notifications
  - Mark as read/unread
  - Archive/delete
  - Filter by type
  - Search notifications
  - Real-time push updates (SignalR)

#### Architecture
- `NotificationService` (core logic)
- `NotificationBackgroundJobService` (scheduled delivery)
- `ClientNotificationService` (SignalR hub)
- `NotificationAuditService` (delivery tracking)
- `BudgetAlertService` (threshold alerts)
- `NotificationRetryService` (exponential backoff)
- Message templates system

#### Database Schema
- `NotificationSettings` (user preferences)
- `NotificationLog` (delivery tracking)
- `NotificationTemplates` (message templates)
- `InAppNotifications` (user notifications)
- `EmailQueue` (pending/sent emails)
- `NotificationState` (user notification state)

---

### ✅ Phase 6: Dashboard & Analytics (Shipped)

**Timeline:** Q4 2026 - Q1 2027  
**Owner:** Analytics Agent  
**Status:** Production ready — v1.0.0 (PR #21 merged)

#### Features Delivered
- **Executive Dashboard (CFO)**
  - Total expenses by time period
  - Spending by department/cost center
  - Budget vs actual tracking
  - Approval pending count
  - Overdue expense notifications
  - Cash flow forecasting
  - Compliance status

- **Manager Dashboard (Team Lead)**
  - Team expense summary
  - Team member spending breakdown
  - Pending approvals for team
  - Budget utilization by team
  - Approved vs rejected rates
  - Team performance metrics

- **Employee Dashboard (User)**
  - Personal expense summary
  - Submitted/approved/pending/reimbursed counts
  - Recent expenses timeline
  - Monthly spending trend
  - Reimbursement status
  - Guidelines and tips

- **Analytics & Reporting**
  - Expense category trends
  - Department spending analysis
  - Approval velocity metrics
  - Cost per headcount
  - Time-to-reimbursement analysis
  - Policy compliance dashboard
  - Variance reporting

- **Export Capabilities**
  - CSV export
  - PDF reports (QuestPDF)
  - JSON data export
  - Scheduled report delivery (Hangfire)

#### Technology Stack
- MudBlazor Charts (chart rendering)
- SignalR for real-time metric updates
- Background jobs for report generation
- Caching layer for dashboard data
- Query optimization for analytics

#### Database Schema
- Materialized views for analytics
- `DashboardSnapshots` (point-in-time snapshots)
- `AnalyticsMetrics` (aggregated metrics)
- `ReportDefinitions` (custom reports)
- `ReportSchedules` (scheduled delivery)
- `CustomReportTemplates` (user templates)

---

## 📋 Planned Future Phases

### Phase 7: Mobile Applications & Desktop Shell (Q1-Q2 2027)

**Target:** iOS, Android, and desktop companion apps

#### Planned Features
- **Mobile (React Native + TypeScript)**
  - Expense submission on mobile
  - Receipt camera capture
  - Approval workflows on mobile
  - Real-time notifications
  - Offline mode (sync when online)
  - Biometric authentication
  - Push notifications
  - Push to home screen

- **Desktop Shell (WPF/.NET)**
  - Desktop companion app (MyDesk.Browser)
  - Desktop share / screen sharing
  - Support ticket integration
  - Native Windows notifications
  - Offline access to recent data

#### Current Progress
- Mobile scaffold: ~17% complete (23/140 tasks)
- Desktop shell: Prototype stage
- **Blockers:** Phase 5 notification integration needed

---

### Phase 8: AI-Powered Receipt Processing (Q2 2027)

**Target:** Advanced receipt intelligence

#### Planned Features
- Intelligent receipt parsing (beyond basic OCR)
- Automatic expense categorization
- Merchant recognition
- GST/Tax extraction
- Fraud detection (duplicate submissions)
- Expense recommendation engine
- Natural language description generation
- Receipt quality scoring

#### Technology
- Advanced ML models (GPT-4 Vision)
- Custom training on expense data
- Real-time processing pipeline
- Confidence scoring system

---

### Phase 9: Predictive Analytics & Intelligence (Q2-Q3 2027)

**Target:** ML-powered business insights

#### Planned Features
- Expense forecasting (next quarter)
- Anomaly detection (unusual spending)
- Savings recommendations
- Budget optimization
- Trend analysis
- Risk scoring
- Predictive compliance alerts
- Department benchmarking

#### Technology
- Python ML pipeline integration
- Real-time inference
- Model retraining scheduler
- Confidence intervals

---

### Phase 10: Supply Chain & Procurement (Q3 2027+)

**Target:** Extended business operations

#### Planned Features
- Purchase requisition workflow
- Vendor management
- Purchase order creation
- Three-way matching (PO-Receipt-Invoice)
- Supplier performance tracking
- Contract management
- Procurement analytics
- Multi-organization support

#### Technology
- Extended approval workflows
- Integration with procurement systems
- Advanced audit trails

---

## Development Metrics & Success Criteria

### Phase Completion Criteria
- ✅ All acceptance criteria met
- ✅ Unit test coverage >80%
- ✅ Integration tests passing
- ✅ Security review completed
- ✅ Performance targets met
- ✅ Documentation complete
- ✅ User acceptance testing passed
- ✅ PR merged to main
- ✅ CI/CD pipeline green

### Key Performance Indicators
- **Speed:** Time-to-reimbursement (target: <5 days)
- **Accuracy:** Expense categorization accuracy (target: >95%)
- **Adoption:** User adoption rate (target: >80%)
- **Reliability:** System uptime (target: 99.9%)
- **Compliance:** Policy violation rate (target: <2%)
- **Build:** CI pipeline < 10 min
- **Tests:** Smoke tests < 5 min

---

## Technology Stack (Current)

### Backend
- **Runtime:** ASP.NET Core 10 (.NET 10)
- **Language:** C#
- **Web Framework:** Blazor Server
- **Database:** SQL Server 2022
- **ORM:** Entity Framework Core / Raw SQL (DatabaseService)
- **Background Jobs:** Hangfire
- **Real-time:** SignalR
- **Logging:** Serilog
- **API:** RESTful (JSON) + GraphQL (HotChocolate)

### Frontend
- **Framework:** Blazor Server
- **UI Components:** MudBlazor 7.15
- **Charts:** MudBlazor Charts
- **PDF:** QuestPDF
- **State:** Blazor Component State + AuthenticationStateProvider

### Infrastructure
- **Cloud:** Azure (target) / On-premises IIS
- **Authentication:** OAuth 2.0 / OpenID Connect / BCrypt
- **Storage:** Azure Blob Storage / S3 / Local filesystem
- **CI/CD:** GitHub Actions
- **Monitoring:** Serilog sinks (file, console)
- **Deployment:** PowerShell scripts + IIS

### Third-party Integrations
- **Accounting:** MYOB (MCP), Xero
- **Email:** SendGrid, SMTP
- **SMS:** Twilio
- **OCR:** OpenAI Vision API
- **Reporting:** QuestPDF
- **Bot:** Microsoft Bot Framework (Teams)

---

## Risk Mitigation

### Technical Risks
| Risk | Mitigation |
|------|-----------|
| Scale (100K+ users) | Caching, database optimization, horizontal scaling |
| Real-time features | SignalR connection pooling, load testing |
| Data security | Encryption at rest/transit, penetration testing, SOC 2 |
| Third-party integrations | Fallback mechanisms, circuit breakers, retry logic |
| Mobile platform fragmentation | React Native shared codebase, platform-specific adapters |

### Business Risks
| Risk | Mitigation |
|------|-----------|
| Market adoption | Free tier, freemium model, case studies |
| Competitive pressure | Focus on UX, enterprise features, integrations |
| Regulatory changes | Compliance audit quarterly, legal review |
| Data breach | Insurance, incident response plan, transparency |

---

## Budget & Resource Allocation

### Phase Team Composition
- **Technical Lead:** 1 (Peter Bardenhagen)
- **Backend Engineers:** 2-3
- **Frontend Engineers:** 1-2
- **Mobile Engineer:** 1 (Phase 7)
- **QA/Tester:** 1
- **DevOps:** 0.5
- **Product Manager:** 0.5

### Estimated Phase Costs
- **Phases 1-6:** $600K (completed)
- **Phase 7:** $150K (in progress)
- **Phases 8-10:** $500K+ (future)

### Infrastructure Costs (Annual)
- **Azure/Cloud:** $50K-$100K
- **Third-party APIs:** $20K-$50K
- **Security/Compliance:** $15K-$25K

---

## Success Metrics by Phase

### Phase 1-6 (Completed)
- ✅ 1000+ active users
- ✅ $500K+ ARR
- ✅ 95%+ approval rate
- ✅ <5 day reimbursement cycle
- ✅ 99.9% uptime
- ✅ CI/CD automated (GitHub Actions)
- ✅ Smoke tests < 5 min

### Phase 7 (Current)
- 🎯 Mobile app launch (iOS + Android)
- 🎯 Desktop shell beta
- 🎯 10K+ active users
- 🎯 $2M+ ARR
- 🎯 <3 day reimbursement cycle

### Phase 8-10 (Future)
- 🎯 50K+ active users
- 🎯 $10M+ ARR
- 🎯 99.95% uptime (SLA)
- 🎯 <24 hour reimbursement cycle

---

## Next Steps

### Immediate (Next 2 weeks)
1. Complete Phase 7 mobile navigation & screen structure
2. Integrate mobile notifications (Phase 5 dependency)
3. Begin desktop shell feature parity
4. Schedule Phase 8 AI receipt processing research

### Short-term (Next month)
1. Launch mobile beta with select customers
2. Gather feedback on mobile UX
3. Plan Phase 8 AI roadmap
4. Complete Phase 9 analytics research

### Medium-term (Q2 2027)
1. Phase 8 AI receipt processing development begins
2. Phase 9 predictive analytics implementation
3. Enterprise feature planning (Phase 10)
4. Multi-organization support architecture

---

## Document References

- **[BACKLOG.md](./BACKLOG.md)** - Current sprint and work items
- **[blockers.md](./blockers.md)** - Active blockers and unblock actions
- **[DEPENDENCIES-AND-BLOCKERS.md](./DEPENDENCIES-AND-BLOCKERS.md)** - Full dependency graph
- **[DEVELOPMENT-PHASES.md](./DEVELOPMENT-PHASES.md)** - Detailed phase specifications
- **[RESOURCE-ALLOCATION.md](./RESOURCE-ALLOCATION.md)** - Team capacity and assignments
- **[RISK-MITIGATION.md](./RISK-MITIGATION.md)** - Risk register and mitigations
- **[AGENTS.md](./AGENTS.md)** - Agent assignments and coordination
- **[../CLAUDE.md](../CLAUDE.md)** - Development guide and setup
- **[../docs/LESSONS_LEARNED.md](../docs/LESSONS_LEARNED.md)** - CI/CD prevention checklist

---

## Recent Deliverables

| Date | Deliverable | PR | Status |
|------|-------------|-----|--------|
| 2026-07-21 | DevOps Nightly automation | #23 | ✅ Merged |
| 2026-07-21 | Phase 6 Executive Dashboards | #21 | ✅ Merged |
| 2026-07-17 | Phase 4: Share My Desktop | #19 | ✅ Merged |
| 2026-07-17 | Phase 3: AgentsOS integration | #18 | ✅ Merged |
| 2026-07-17 | Phase 2: Support requests | #17 | ✅ Merged |
| 2026-07-17 | Phase 1: Auth detection | #16 | ✅ Merged |
| 2026-07-17 | Outlook add-in (3 workflows) | #15 | ✅ Merged |
| 2026-07-17 | Branding polish | #14 | ✅ Merged |

---

*For questions or updates to this roadmap, contact the Product team.*
