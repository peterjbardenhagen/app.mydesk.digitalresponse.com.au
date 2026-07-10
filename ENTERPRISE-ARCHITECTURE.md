# MyDesk Enterprise Architecture

**Version:** 1.0  
**Last Updated:** July 2026  
**Target Audience:** Enterprise architects, security teams, compliance officers

---

## Executive Summary

MyDesk is a cloud-native, multi-tenant SaaS platform designed for enterprise approval workflows and expense management. Built with security-first principles, it meets ISO 27001, SOC 2 Type II, and Sarbanes-Oxley compliance requirements while serving Australian enterprises subject to the Privacy Act.

The architecture supports domain-based multi-tenancy for automatic user routing, fine-grained approval permissions with threshold-based authority, immutable compliance audit logging, and flexible billing models.

---

## Enterprise Requirements

### Regulatory Compliance
- **ISO 27001** - Information security management
- **SOC 2 Type II** - Security, availability, and confidentiality controls
- **Sarbanes-Oxley** - Financial controls and audit trail
- **Australian Privacy Act** - Data residency and user consent
- **GDPR** - Data portability and right to deletion (future)

### Security Requirements
- Data encryption at rest and in transit (TLS 1.3)
- Field-level encryption for sensitive data
- Role-based access control (RBAC)
- Multi-tenant isolation via tenant_id claims
- Rate limiting and brute-force protection
- Complete immutable audit trail
- IP whitelisting support (future)
- MFA support (future)

### Business Requirements
- Support 100+ concurrent users per tenant
- Automatic user provisioning via email domain
- Multiple billing models (Monthly, Yearly, Pay-as-You-Go, Flat Rate)
- Delegation support for out-of-office scenarios
- Approval workflow customization per client
- Real-time notifications (email, SMS, in-app)
- Team-based approval hierarchies

### Data Requirements
- 99.9% uptime SLA
- Data residency in Australia (appsettings configuration)
- 7-year audit log retention
- Automated backups (daily)
- Disaster recovery (48-hour RTO)
- PITR (Point-in-Time Recovery) support

---

## Architectural Principles

### 1. **Zero-Trust Security**
Every request is authenticated and authorized, regardless of network location. All database queries use parameterized statements.

### 2. **Tenant Isolation**
Multi-tenant isolation enforced at:
- Database row level (TenantId filtering on every query)
- API level (tenant_id claim validation)
- Application layer (CurrentTenantAccessor service)

### 3. **Immutable Audit Trail**
All compliance-sensitive operations logged to append-only ComplianceAuditLog table with no UPDATE/DELETE permissions.

### 4. **Defense in Depth**
- Network: API Gateway with rate limiting
- Transport: TLS 1.3 encryption
- Application: Input validation, parameterized queries
- Data: Encryption at rest, field-level encryption option

### 5. **Observability**
- Structured logging (Serilog)
- Distributed tracing (OpenTelemetry ready)
- Metrics (Prometheus/Grafana compatible)
- Audit events (NotificationLog, ComplianceAuditLog)

---

## Deployment Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                        CDN / Edge                            │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                    API Gateway (Azure)                       │
│                  - Rate Limiting                             │
│                  - WAF Rules                                 │
│                  - SSL/TLS Termination                       │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│              Load Balancer (Auto-scaling)                    │
│  ┌─────────────────────────────────────────────────────┐    │
│  │  MyDesk.Web (Blazor Server + API)                   │    │
│  │  - Runs in Azure App Service                        │    │
│  │  - Handles auth, business logic, notifications      │    │
│  │  - Connects to SQL Database                         │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│            SQL Server (Azure SQL Database)                   │
│  - SQL Server 2022                                           │
│  - Always-On Availability Group (High Availability)          │
│  - Automated backups (35-day retention)                       │
│  - Transparent Data Encryption (at rest)                      │
│  - All queries use parameterized statements                   │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│              Blob Storage (Archive/Photos)                   │
│  - User profile photos                                       │
│  - Expense receipts                                          │
│  - Encryption at rest (Azure-managed keys)                   │
│  - Versioning enabled                                        │
└─────────────────────────────────────────────────────────────┘
```

---

## Data Architecture

### Multi-Tenancy Model
```
┌─────────────────────────────────────────────────────────────┐
│                    User Login                                │
│              user@digitalresponse.com.au                     │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│              Domain-Based Tenant Lookup                      │
│  Query: SELECT TenantId FROM TenantDomains                  │
│         WHERE Domain = 'digitalresponse.com.au'             │
│                   AND IsVerified = 1                        │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│              Automatic Tenant Assignment                     │
│  Claims: tenant_id = "2", UserId = "456"                    │
│          Email = "user@digitalresponse.com.au"              │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│            All Queries Filtered by TenantId                 │
│  SELECT * FROM Expenses                                      │
│  WHERE TenantId = @TenantId AND UserId = @UserId            │
└─────────────────────────────────────────────────────────────┘
```

### Database Schema Layers

**Layer 1: Core Business**
- Users, Tenants, Departments
- Expenses, Timesheets, Approvals
- UserPhotos, ExpenseReceipts

**Layer 2: Approvals & Workflows**
- ApprovalWorkflows, ApprovalRules
- ApprovalRequests, ApprovalActions
- ApprovalPermissions, ApprovalDelegations

**Layer 3: Notifications & Communication**
- NotificationTemplates, NotificationSettings
- NotificationLog, EmailQueue
- InAppNotifications

**Layer 4: Compliance & Security**
- ComplianceAuditLog (immutable append-only)
- SecurityAuditEvents, DataExportAudit
- RateLimitingRules, RateLimitingViolations
- EncryptionKeys, FieldEncryption

**Layer 5: Billing & Tenancy**
- ClientBillingConfig, ClientInvoice
- ClientUsageLog, ClientBillingHistory
- ClientOnboardingSession, OnboardingWorkflowTemplates

---

## Security Architecture

### Authentication
```
┌─────────────────────────────────────────────────────────────┐
│                    Login Request                             │
│              (Email, Password, Domain)                       │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│           1. Resolve Domain to Tenant                        │
│        SELECT TenantId FROM TenantDomains                    │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│        2. Verify User Belongs to Tenant                      │
│        SELECT UserId FROM Users WHERE                        │
│        TenantId = @TenantId AND Email = @Email              │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│           3. Hash Password + Validate                        │
│        BCrypt.Verify(PasswordHash, InputPassword)            │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│        4. Issue JWT with Tenant Claims                       │
│        Claims: tenant_id, user_id, email, roles              │
│        Token: Signed with RS256 (RSA asymmetric)             │
└─────────────────────────────────────────────────────────────┘
```

### Authorization
- **Role-Based Access Control (RBAC)**
  - Administrator: Full tenant access
  - Manager: Can approve expenses up to threshold
  - Director: Can approve all expenses
  - Employee: Can view/create own expenses

- **Threshold-Based Authority**
  - Expense < $5,000 → Manager approval
  - Expense ≥ $5,000 → Director approval
  - Configurable per tenant in ApprovalPermissions

- **Tenant Isolation Enforcement**
  - Every API endpoint validates tenant_id claim
  - Every database query filters by TenantId
  - Impossible to query another tenant's data

### Rate Limiting
```
┌─────────────────────────────────────────────────────────────┐
│                  Incoming Request                            │
│            IP: 192.168.1.1, UserId: 456                      │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│          Check RateLimitingRules                             │
│  Rule: Max 100 requests/min per user                         │
│  Current: 98 requests in last 60 seconds                     │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│             Allow Request, Log Attempt                       │
│        If limit exceeded:                                    │
│        - Return 429 Too Many Requests                        │
│        - Log violation to RateLimitingViolations             │
│        - Apply exponential backoff: 1s, 2s, 4s, 8s, 16s      │
└─────────────────────────────────────────────────────────────┘
```

### Audit Trail
```
ComplianceAuditLog (Immutable Append-Only)
├── Every user login
├── Every expense creation/modification
├── Every approval decision (approve/reject)
├── Every permission change
├── Every data export request
└── Never deleted (compliance requirement)

Fields:
- EntityType, EntityId (what was affected)
- Action (created, updated, approved, exported)
- UserId, IpAddress, UserAgent (who & where)
- Timestamp, TenantId (when & which tenant)
- Details (JSON blob of what changed)
```

---

## Compliance Implementation

### Audit Requirements
✅ **Immutable Audit Log** - ComplianceAuditLog table with no UPDATE/DELETE
✅ **Approval Tracking** - Every approval recorded with timestamp and approver
✅ **Permission Changes** - ApprovalPermissionAudit tracks all permission modifications
✅ **User Activity** - SecurityAuditEvents logs all sensitive operations
✅ **Data Exports** - DataExportAudit tracks all compliance data requests
✅ **7-Year Retention** - Automated archive of old logs

### Financial Controls
✅ **Segregation of Duties** - Submitter ≠ Approver enforced
✅ **Multi-Level Approvals** - Large expenses require director approval
✅ **Audit Trail** - Every approval decision recorded
✅ **Rejectio​n Reasons** - Approvers must provide reason when rejecting
✅ **Approval Limits** - Configurable thresholds per approver

### Privacy Controls
✅ **Data Residency** - Australia-only data storage (configurable)
✅ **Right to Be Forgotten** - User deletion process (soft delete)
✅ **Data Portability** - Export API for all user data
✅ **Consent Management** - Notification opt-in/opt-out tracking
✅ **Encryption at Rest** - All sensitive data encrypted in database

---

## Disaster Recovery

### Recovery Time Objectives (RTO)
- **Critical Systems:** 1 hour RTO
- **Financial Data:** 4 hour RTO
- **Non-critical Systems:** 24 hour RTO

### Recovery Point Objectives (RPO)
- **Database:** 15 minutes (automated backups)
- **File Storage:** 1 hour
- **Configuration:** Real-time (version control)

### Backup Strategy
```
Daily Backups:
├── Full backup at 2:00 AM (UTC+10)
├── Incremental every 4 hours
├── 35-day retention in Azure
├── Test restore weekly
└── Off-site copy to separate region (future)

Database:
├── Always-On Availability Group (HA)
├── Automatic failover (< 60 seconds)
├── PITR (Point-in-Time Recovery) enabled
└── Encrypted backups

Configuration:
├── Version control (Git)
├── Deployment automation (CI/CD)
└── Infrastructure-as-code (future)
```

---

## Performance & Scalability

### Database Performance
- **Indexes** on TenantId, UserId, Status, CreatedAt for all major tables
- **Partitioning** by TenantId for large tables (future)
- **Read Replicas** for reporting (future)
- **Query Optimization** using execution plans

### Application Scalability
- **Stateless design** - Any instance can handle any request
- **Auto-scaling** based on CPU/Memory metrics
- **Distributed caching** for frequently accessed data (future)
- **Async processing** for long-running operations (email queue)

### Monitoring & Alerting
```
Metrics Tracked:
├── API Response Time (p50, p95, p99)
├── Database Query Performance
├── Error Rate by Endpoint
├── Rate Limiting Violations
├── Email Queue Depth
├── Notification Delivery Success Rate
└── Audit Log Size

Alerts Triggered:
├── API Response Time > 2s
├── Error Rate > 1%
├── Database Connection Pool Exhaustion
├── Email Queue > 10k pending
└── Rate Limiting Active (suspicious activity)
```

---

## Technology Stack

| Layer | Technology | Version | Purpose |
|-------|-----------|---------|---------|
| **Frontend** | Blazor Server | .NET 10 | Interactive web UI |
| **UI Components** | MudBlazor | Latest | Material Design components |
| **Backend** | ASP.NET Core | .NET 10 | REST API, business logic |
| **Database** | SQL Server | 2022 | Primary data store |
| **ORM** | ADO.NET | Built-in | Direct SQL (parameterized) |
| **Authentication** | JWT | RS256 | Token-based auth |
| **Logging** | Serilog | Latest | Structured logging |
| **Image Processing** | ImageSharp | Latest | Photo cropping, compression |
| **Document Extraction** | Azure Document Intelligence | Latest | Receipt OCR |
| **Email** | SendGrid/Azure | Latest | Email delivery |
| **Cloud** | Microsoft Azure | Current | Infrastructure |

---

## Future Enhancements

### Near-term (Q3-Q4 2026)
- [ ] Mobile app (iOS/Android)
- [ ] SMS notifications (Twilio integration)
- [ ] Advanced reporting (Power BI)
- [ ] Slack/Teams integration
- [ ] Single Sign-On (SAML 2.0)

### Mid-term (2027)
- [ ] Xero/MYOB accounting integration
- [ ] Machine learning for expense categorization
- [ ] Workflow automation rules engine
- [ ] Advanced delegation (interim managers)
- [ ] Approval escalation rules

### Long-term (2027+)
- [ ] Blockchain for immutable audit trail (POC)
- [ ] AI-powered approval recommendations
- [ ] Global multi-region deployment
- [ ] Advanced analytics & forecasting
- [ ] GraphQL API

---

## References

- **Solution Architecture:** See `SOLUTION-ARCHITECTURE.md`
- **Product Requirements:** See `PRODUCT-REQUIREMENTS.md`
- **Product Strategy:** See `PRODUCT-STRATEGY.md`
- **Implementation Agents:** See `agents.md`
- **Development Guide:** See `CLAUDE.md`
