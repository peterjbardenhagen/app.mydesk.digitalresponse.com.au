# Detailed Development Phases & Implementation Plan

**Version:** 1.0  
**Last Updated:** July 2026

---

## Phase Progression Timeline

```
2026 Q2       2026 Q3       2026 Q4       2027 Q1       2027 Q2       2027 Q3
├─────────────┼─────────────┼─────────────┼─────────────┼─────────────┼─────────────┤
│ Phase 1-2   │ Phase 2-3   │ Phase 4-5   │ Phase 6-7   │ Phase 8-9   │ Phase 10+   │
│ Released    │ Released    │ In Progress │ Planned     │ Planned     │ Planned     │
└─────────────┴─────────────┴─────────────┴─────────────┴─────────────┴─────────────┘
```

---

## Phase 1: Core Expense Management

### Completion Status: ✅ 100% (Released Q3 2026)

### Deliverables
- ✅ Expense submission form (web)
- ✅ Receipt image capture (camera/upload)
- ✅ OCR extraction pipeline
- ✅ Expense categorization
- ✅ Draft save functionality
- ✅ Expense history
- ✅ Search and filter
- ✅ Mobile-responsive UI
- ✅ Database schema
- ✅ API endpoints

### Key Metrics
- **Lines of Code:** ~8,000
- **Database Tables:** 5
- **API Endpoints:** 12
- **Components:** 8 Blazor components
- **Test Coverage:** 85%
- **Development Time:** 8 weeks

### Technology Decisions
- **OCR:** OpenAI Vision API (vs. Tesseract/AWS Textract)
  - Rationale: Accuracy, multi-language support, cost-effective
- **File Storage:** Azure Blob Storage (vs. S3/Local)
  - Rationale: Azure-native, security, compliance
- **UI Framework:** MudBlazor (vs. custom CSS/Bootstrap)
  - Rationale: Enterprise components, responsive, accessibility

---

## Phase 2: Approval Workflows

### Completion Status: ✅ 100% (Released Q3 2026)

### Deliverables
- ✅ Multi-level approval chains (Manager→Director→CFO)
- ✅ Approval delegation
- ✅ Bulk approval interface
- ✅ Approval comments
- ✅ Rejection with feedback
- ✅ SLA tracking
- ✅ Email notifications
- ✅ Audit trail
- ✅ Approval rules configuration
- ✅ API endpoints

### Key Metrics
- **Lines of Code:** ~12,000
- **Database Tables:** 8
- **API Endpoints:** 18
- **Components:** 12 Blazor components
- **Test Coverage:** 82%
- **Development Time:** 10 weeks

### Architecture Pattern
- **State Machine:** Workflow states (Submitted→Pending→Approved/Rejected)
- **Orchestrator Pattern:** Approval orchestration service
- **Event-driven:** SignalR for real-time updates
- **Background Jobs:** Hangfire for scheduled notifications

### Design Decisions
- **Approval Rules Engine:** Custom (vs. BPEL/workflow engine)
  - Rationale: Simpler to maintain, specific to expense domain
- **Notification Timing:** Async queue (vs. synchronous)
  - Rationale: Non-blocking, reliable delivery, retry capability
- **Audit Log:** Database-based (vs. event streaming)
  - Rationale: Simpler queries, compliance reporting, cost

---

## Phase 3: Integrations

### Completion Status: ✅ 100% (Released Q4 2026)

### Deliverables
- ✅ MYOB integration (OAuth, data sync)
- ✅ Xero integration (OAuth, real-time import)
- ✅ Bank reconciliation module
- ✅ Email forwarding integration
- ✅ Field mapping configuration
- ✅ Error handling and retry logic
- ✅ Integration logging/audit trail
- ✅ Webhook handlers
- ✅ Data transformation layer
- ✅ Configuration UI

### Key Metrics
- **Lines of Code:** ~15,000
- **Database Tables:** 10
- **API Endpoints:** 25
- **External APIs:** 3 (MYOB, Xero, Email)
- **Test Coverage:** 78%
- **Development Time:** 12 weeks

### Integration Architecture

#### MYOB Integration
```
MyDesk Expense → MYOB API
  ├─ Authentication: OAuth 2.0
  ├─ Data Format: Expense → Journal Entry
  ├─ Sync Frequency: Real-time (on approval)
  ├─ Error Handling: Retry queue, manual intervention
  └─ Audit Trail: Integration log
```

#### Xero Integration
```
Xero Invoice/Receipt → MyDesk Expense
  ├─ Authentication: OAuth 2.0
  ├─ Data Format: Bill → Expense
  ├─ Sync Frequency: Daily batch
  ├─ Matching: Fuzzy matching on date/amount
  └─ Reconciliation: Three-way matching
```

### Design Decisions
- **Queue-based Processing:** Hangfire (vs. direct HTTP calls)
  - Rationale: Reliability, retry capability, job tracking
- **Data Mapping:** Configuration-driven (vs. hardcoded)
  - Rationale: Flexibility for multi-tenant customization
- **Rate Limiting:** Respect API limits (vs. burst requests)
  - Rationale: Avoid throttling, maintain API health

---

## Phase 4: Teams & Departments

### Completion Status: ✅ 100% (Released Q4 2026)

### Deliverables
- ✅ Department management CRUD
- ✅ Team creation and assignment
- ✅ Org hierarchy visualization
- ✅ Budget limit configuration
- ✅ Budget enforcement (hard/soft)
- ✅ Bulk user import (CSV)
- ✅ Team expense rollup/analytics
- ✅ RBAC implementation
- ✅ Team lead assignment
- ✅ Department approver chains

### Key Metrics
- **Lines of Code:** ~10,000
- **Database Tables:** 6
- **API Endpoints:** 20
- **Components:** 15 Blazor components
- **Test Coverage:** 80%
- **Development Time:** 9 weeks

### Authorization Architecture

```
User
  ├─ Department: Engineering
  ├─ Team: Platform
  ├─ Role: Team Member
  └─ Permissions:
      ├─ Submit expenses
      ├─ View team expenses
      ├─ Approve expenses (if Manager)
      └─ View budget tracking
```

### Budget Enforcement

```
Department Budget: $100,000/month
  ├─ Soft Limit: $90,000 (warning at 90%)
  ├─ Hard Limit: $100,000 (block submission at 100%)
  ├─ Current Usage: $75,000 (75%)
  ├─ Remaining: $25,000
  └─ Alert: Send notification at 85%, 95%
```

---

## Phase 5: Notifications & Alerts

### Completion Status: 🔄 80% (In Progress Q4 2026)

### Deliverables
- ✅ Notification service core
- ✅ Email notifications (SendGrid)
- ✅ SMS notifications (Twilio)
- ✅ In-app notifications
- ✅ Notification preferences
- ✅ Notification center UI
- ✅ Real-time push (SignalR)
- ✅ Message templates
- ✅ Delivery tracking
- 🔄 Push notifications (iOS/Android)
- 🔄 Notification analytics

### Key Metrics
- **Lines of Code:** ~18,000
- **Database Tables:** 8
- **API Endpoints:** 22
- **Components:** 10 Blazor components
- **Test Coverage:** 75% (target 85%)
- **Development Time:** 14 weeks (ongoing)

### Notification Architecture

```
Event Trigger (Expense Submitted)
  ↓
Notification Service
  ├─ Check user preferences
  ├─ Get recipient list
  ├─ Generate message from template
  └─ Queue for delivery
      ├─ Email queue
      ├─ SMS queue
      └─ In-app queue
  ↓
Delivery Service (Background Job)
  ├─ Email delivery (SendGrid API)
  ├─ SMS delivery (Twilio API)
  └─ In-app update (SignalR broadcast)
  ↓
Status Tracking
  ├─ Log delivery status
  ├─ Track failures
  └─ Retry on failure (exponential backoff)
```

### Key Components
- **NotificationService:** Core business logic
- **NotificationPreferences:** User settings management
- **NotificationCenter:** UI component
- **NotificationBackgroundJobService:** Scheduled delivery
- **ClientNotificationService:** Real-time SignalR updates

### Notification Triggers (Implemented)
1. Invoice Created
2. Invoice Sent to Client
3. Invoice Overdue (recurring)
4. Quote Sent
5. Job Status Changed
6. Order Despatched
7. Approval Required
8. Approval Completed
9. Expense Rejected

### Outstanding Tasks
- [ ] Push notification integration (Firebase Cloud Messaging)
- [ ] SMS template customization
- [ ] Notification delivery analytics dashboard
- [ ] Batch notification sending optimization
- [ ] DND (Do Not Disturb) scheduling
- [ ] User notification preferences UI completion

---

## Phase 6: Dashboard & Analytics

### Completion Status: 🔄 70% (In Progress Q4 2026 - Q1 2027)

### Deliverables
- 🔄 Executive dashboard (CFO view)
- 🔄 Manager dashboard (Team view)
- 🔄 Employee dashboard (Personal view)
- 🔄 Analytics service & queries
- 🔄 Chart components
- ⏳ Export to CSV/PDF/JSON
- ⏳ Scheduled report delivery
- ⏳ Custom report builder
- ⏳ Real-time metric updates
- ⏳ Performance optimization

### Key Metrics
- **Lines of Code:** ~22,000 (estimated)
- **Database Views:** 15+
- **API Endpoints:** 30+
- **Components:** 20+ Blazor components
- **Chart Types:** 8-10 (bar, pie, line, area, gauge)
- **Test Coverage:** 65% (target 80%)
- **Development Time:** 16 weeks (ongoing)

### Dashboard Layouts

#### Executive Dashboard (CFO)
```
┌─────────────────────────────────────────────┐
│ Total Expenses Month-to-Date: $125,450      │
│ Budget Utilization: 87% ($175,000)          │
│ Pending Approvals: 23                        │
│ Overdue Reimbursements: 5                    │
├─────────────────────────────────────────────┤
│ Department Breakdown        │ Category        │
│ Engineering: $45,200        │ Travel: $32K    │
│ Sales: $38,100              │ Meals: $28K     │
│ Marketing: $25,300          │ Other: $65.5K   │
│ Operations: $16,850         │                 │
├─────────────────────────────────────────────┤
│ Approval Metrics                             │
│ Avg Approval Time: 2.3 days                  │
│ Approval Rate: 96%                           │
│ Average Expense Size: $312                   │
├─────────────────────────────────────────────┤
│ Trends & Forecasts                           │
│ Month-over-month: +12%                       │
│ 90-day forecast: $380K                       │
└─────────────────────────────────────────────┘
```

#### Manager Dashboard (Team)
```
┌─────────────────────────────────────────────┐
│ Team: Platform Engineering                  │
│ Team Expenses This Month: $32,150           │
│ Team Members: 8                              │
│ Budget Remaining: $45,200 (58%)             │
├─────────────────────────────────────────────┤
│ Team Member Spending                         │
│ John Smith: $4,250 (2 pending)               │
│ Sarah Chen: $3,890 (approved)                │
│ Mike Johnson: $2,145 (1 pending)             │
├─────────────────────────────────────────────┤
│ Pending Approvals: 5                         │
│ Oldest: 3 days old                           │
│ Total Amount: $12,340                        │
├─────────────────────────────────────────────┤
│ Team Performance                             │
│ Avg Days to Reimburse: 2.1                   │
│ Policy Violations: 1                         │
└─────────────────────────────────────────────┘
```

#### Employee Dashboard (Personal)
```
┌─────────────────────────────────────────────┐
│ My Expense Summary (This Month)              │
│ Submitted: $3,240 (12 expenses)              │
│ Approved: $2,890 (11 expenses)               │
│ Pending Review: $350 (1 expense)             │
│ Reimbursed: $2,500                          │
├─────────────────────────────────────────────┤
│ Recent Expenses                              │
│ Jul 8 - Acme Hotel - $185 (Approved)        │
│ Jul 7 - Uber Ride - $45 (Pending)           │
│ Jul 6 - Client Lunch - $92 (Approved)       │
├─────────────────────────────────────────────┤
│ Monthly Trend                                │
│ June: $2,890  July: $3,240  (↑12%)          │
├─────────────────────────────────────────────┤
│ Guidelines                                   │
│ All expenses within policy ✓                 │
│ Next reimbursement: Jul 10                   │
└─────────────────────────────────────────────┘
```

### Technology Choices

#### Charting
- **Primary:** MudBlazor Charts
- **Alternative:** Chart.js / Plotly.NET
- **Rationale:** MudBlazor native, responsive, light-weight

#### Real-time Updates
- **Technology:** SignalR Hub
- **Update Frequency:** 30-second refresh
- **Scope:** Broadcast metrics on expense approval/submission

#### Query Optimization
- **Materialized Views:** For expensive aggregations
- **Caching:** 5-minute cache for dashboard metrics
- **Indexes:** On TenantId, CreatedAt, Status

---

## Phase 7: Mobile Applications

### Planned Status: 📋 Scheduled Q1 2027

### Scope
- iOS app (native Swift)
- Android app (native Kotlin)
- Cross-platform: React Native (evaluate)

### Key Features
1. **Expense Submission**
   - Receipt camera capture
   - Auto-category suggestion
   - Draft saving
   - Offline capability

2. **Approvals**
   - Review pending expenses
   - Approve/reject inline
   - Bulk approval
   - Comments

3. **Notifications**
   - Real-time push notifications
   - Notification inbox
   - Action buttons in notifications

4. **Analytics**
   - Dashboard sync
   - Personal metrics
   - Spending trends

### Technology Stack
- **Framework:** React Native (cross-platform)
- **State Management:** Redux
- **Storage:** SQLite (offline)
- **API Client:** Custom REST client
- **Authentication:** OAuth 2.0 flow
- **Push Notifications:** Firebase Cloud Messaging (FCM)

### Development Timeline
- Design & Prototyping: 3 weeks
- Core Implementation: 8 weeks
- Testing & Optimization: 3 weeks
- Beta Testing: 2 weeks
- App Store Review: 2 weeks

---

## Phase 8: AI Receipt Processing

### Planned Status: 📋 Scheduled Q1-Q2 2027

### Goals
- Increase OCR accuracy from 85% to 98%+
- Reduce manual review by 60%
- Auto-categorization (90%+ accuracy)
- Duplicate detection

### Technology
- **Model:** GPT-4V / Claude Vision (evaluate)
- **Fallback:** Tesseract + custom training
- **Pipeline:** Async processing (Hangfire)
- **Confidence Scoring:** ML model scoring

### Implementation Phases
1. **Phase 8A:** Enhanced OCR (Weeks 1-6)
   - Better merchant extraction
   - GST/tax identification
   - Item-level parsing

2. **Phase 8B:** Auto-categorization (Weeks 7-12)
   - Train category model
   - Integrate into submission flow
   - User feedback loop

3. **Phase 8C:** Fraud Detection (Weeks 13+)
   - Duplicate detection algorithm
   - Anomaly scoring
   - Policy violation detection

---

## Phase 9: Predictive Analytics

### Planned Status: 📋 Scheduled Q2 2027

### ML Features
1. **Expense Forecasting**
   - Predict next quarter spending
   - Department-level forecasting
   - Seasonality adjustments

2. **Anomaly Detection**
   - Unusual spending patterns
   - Out-of-policy expenses
   - Fraud indicators

3. **Recommendations**
   - Cost reduction opportunities
   - Budget optimization
   - Policy improvements

### Implementation
- **Python ML Stack:** scikit-learn, pandas, TensorFlow
- **Real-time Inference:** Model serving (TensorFlow Serving)
- **Training:** Monthly retraining with new data
- **Confidence Intervals:** Uncertainty quantification

---

## Phase 10: Supply Chain & Procurement

### Planned Status: 📋 Scheduled Q2-Q3 2027

### New Modules
1. **Purchase Requisition**
   - Employee requisition creation
   - Approval workflow
   - Budget checking

2. **Purchase Orders**
   - PO generation from requisition
   - Vendor assignment
   - Delivery tracking

3. **Vendor Management**
   - Vendor master data
   - Performance tracking
   - Contract management

4. **Three-way Matching**
   - PO ↔ Receipt ↔ Invoice matching
   - Variance tolerance
   - Exception handling

### Database Additions
- `Vendors` (vendor master)
- `PurchaseRequisitions` (requisition workflow)
- `PurchaseOrders` (purchase orders)
- `POLines` (line items)
- `POReceipts` (delivery receipts)
- `ThreeWayMatches` (matching records)

---

## Parallel Workstreams

### Security & Compliance (Ongoing)
- **Q4 2026:** SOC 2 Type II audit
- **Q1 2027:** ISO 27001 certification
- **Q2 2027:** Penetration testing
- **Q3 2027:** GDPR/CCPA compliance

### Infrastructure & DevOps (Ongoing)
- **Q4 2026:** Azure Kubernetes Service (AKS) migration
- **Q1 2027:** Infrastructure-as-Code (Terraform)
- **Q2 2027:** Blue-green deployment pipeline
- **Q3 2027:** Disaster recovery testing

### Support & Documentation (Ongoing)
- **User documentation:** Updated with each phase
- **API documentation:** Swagger/OpenAPI
- **Developer guide:** Setup and contribution
- **Training materials:** Customer and support team

---

## Resource Allocation by Phase

| Phase | Backend | Frontend | QA | DevOps | PM | Total |
|-------|---------|----------|-----|--------|-----|-------|
| 1-2   | 2       | 2        | 1   | 0.5   | 0.5 | 6    |
| 3-4   | 2.5     | 1.5      | 1   | 0.5   | 0.5 | 5.5  |
| 5-6   | 2       | 2        | 1.5 | 1     | 1   | 7.5  |
| 7     | 1       | 3        | 1   | 1     | 1   | 7    |
| 8-9   | 1.5     | 1        | 0.5 | 0.5   | 1   | 4.5  |
| 10    | 2.5     | 2        | 1   | 0.5   | 1   | 7    |

---

## Budget Breakdown

### Development Costs
- **Phase 1-4 (Completed):** $400K
- **Phase 5-6 (Current):** $200K
- **Phase 7-10 (Planned):** $500K+

### Infrastructure Costs
- **Cloud Services:** $50K-$100K/year
- **Third-party APIs:** $20K-$50K/year
- **Security/Compliance:** $15K-$25K/year

### Total Investment (5-year)
- **Development:** $1.2M+
- **Infrastructure:** $350K+
- **Operations:** $300K+
- **Total:** $1.85M+ 

---

*For detailed task breakdowns, see the agent assignments in [../docs/agents.md](../docs/agents.md)*
