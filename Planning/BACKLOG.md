# MyDesk Development Backlog

**Current Status:** Phases 1-4 Complete | Phases 5-6 In Progress | Phases 7-10 Planned  
**Last Updated:** July 16, 2026

---

## What is This?

This backlog consolidates work items across all development phases. For detailed specifications, refer to **DEVELOPMENT-PHASES.md**. For phase timelines and ownership, see **RESOURCE-ALLOCATION.md**.

---

## ✅ Completed Phases (Shipped)

### Phase 1: Core Expense Management
- ✅ User expense submission with photo capture
- ✅ OCR receipt extraction
- ✅ Multi-currency support (AUD, USD, EUR, GBP)
- ✅ Receipt attachment storage (S3)

### Phase 2: Multi-Level Approvals
- ✅ Manager approval workflows
- ✅ Director escalation rules
- ✅ Approval chains & delegation
- ✅ Approval audit trail

### Phase 3: Accounting Integrations
- ✅ MYOB GL export integration
- ✅ Xero invoice sync
- ✅ Bank reconciliation dashboard
- ✅ Dual-currency bank accounts

### Phase 4: Team & Department Management
- ✅ Organizational hierarchy (Teams, Departments, Divisions)
- ✅ Budget allocation by department
- ✅ Budget reporting & forecasting
- ✅ Department-level approval workflows

---

## 🔄 In Progress (Current Sprint)

### Phase 5: Notification System
**Owner:** NotificationService Team  
**Target:** Q4 2026

- 🔄 Email notifications (immediate & digest)
- 🔄 SMS notifications with Twilio
- 🔄 In-app notification center
- 🔄 Notification preferences & quiet hours
- 🔄 Approval request alerts
- 🔄 Status change notifications

**Blockers:** None  
**PR Status:** #15 Merged

### Phase 6: Executive Dashboards & Analytics
**Owner:** Analytics & Reporting Team  
**Target:** Q4 2026 - Q1 2027

- 🔄 Executive dashboard (KPIs, trends, forecasts)
- 🔄 Manager dashboard (team spend, approvals, budgets)
- 🔄 Employee dashboard (my expenses, reimbursements)
- 🔄 Advanced analytics (drill-down, cohort analysis)
- 🔄 Scheduled report delivery (Hangfire)
- 🔄 CSV/PDF/JSON export formats
- 🔄 MudBlazor chart integration

**Blockers:** None  
**Branch:** claude/deploy-mydesk-iis-dns-6o5qn0

---

## 📋 Planned (Backlog)

### Phase 7: Mobile Apps (iOS/Android)
**Target:** Q1 2027  
**Estimated Effort:** 12-16 weeks

- 📋 React Native mobile app scaffold
- 📋 Offline-first expense submission
- 📋 Mobile camera integration (receipt capture)
- 📋 Push notifications
- 📋 Mobile approval workflows
- 📋 Sync with cloud backend
- 📋 iOS App Store & Android Play Store deployment

**Dependencies:** Phase 5 (Notifications)

### Phase 8: AI-Powered Receipt Parsing
**Target:** Q1-Q2 2027  
**Estimated Effort:** 8-12 weeks

- 📋 Receipt image preprocessing (rotation, crop)
- 📋 Supplier & merchant name extraction
- 📋 Date & amount parsing
- 📋 GST/tax detection
- 📋 Line item extraction (if available)
- 📋 Confidence scoring & manual override UI
- 📋 ML model training pipeline (internal)

**Dependencies:** Phase 1 (Receipt capture), Phase 8 (AI infrastructure)

### Phase 9: Advanced Analytics & Forecasting
**Target:** Q2 2027  
**Estimated Effort:** 10-14 weeks

- 📋 Predictive budget forecasting
- 📋 Spend anomaly detection
- 📋 Department spending trends
- 📋 Seasonal pattern analysis
- 📋 Custom report builder
- 📋 Data warehouse (Snowflake/BigQuery)
- 📋 BI tool integration (Tableau/Power BI)

**Dependencies:** Phase 6 (Dashboards)

### Phase 10: Procurement & Multi-Org Management
**Target:** Q2-Q3 2027  
**Estimated Effort:** 14-18 weeks

- 📋 Procurement module (PO, supplier management)
- 📋 Contract management
- 📋 Compliance & audit trail
- 📋 Multi-organization support
- 📋 Subsidiary/branch structure
- 📋 Consolidated reporting

**Dependencies:** Phase 4 (Org structure)

---

## 🎯 High Priority Fixes & Improvements

### Security
- [ ] Implement field-level encryption for PII
- [ ] Add rate limiting on API endpoints
- [ ] Implement API key rotation
- [ ] GDPR data export functionality

### Performance
- [ ] Add database query indexes for reporting tables
- [ ] Implement caching layer (Redis) for dashboards
- [ ] Lazy-load large reports (pagination)
- [ ] Optimize PDF generation (async background jobs)

### UX & Accessibility
- [ ] Mobile-responsive dashboard redesign
- [ ] Dark mode support
- [ ] WCAG 2.1 AA compliance audit
- [ ] Keyboard navigation on all dialogs

### Documentation
- [ ] API OpenAPI/Swagger docs
- [ ] User guides for each module
- [ ] Video tutorials for workflows
- [ ] Admin onboarding guide

---

## Work Item Tracking

### How to Use This Backlog
1. **Find your phase** in the roadmap
2. **Check current status** (In Progress, Planned, etc.)
3. **Review acceptance criteria** in DEVELOPMENT-PHASES.md
4. **Check blockers** in DEPENDENCIES-AND-BLOCKERS.md
5. **Claim ownership** in RESOURCE-ALLOCATION.md
6. **Open a feature branch** and link to this backlog

### Submitting Work
```bash
# 1. Create feature branch
git checkout -b feature/phase6-dashboard-export

# 2. Link backlog item in commit message
git commit -m "feat(phase6): add CSV export to executive dashboard

Backlog: Phase 6 / CSV/PDF/JSON export formats
Closes #42"

# 3. Create PR with backlog reference
gh pr create --title "Phase 6: CSV Export for Dashboards"
```

---

## Current Sprint Focus (July-August 2026)

**Phase 5 Tasks (Notifications):**
- [ ] Complete email notification service
- [ ] Integrate SMS gateway (Twilio)
- [ ] Build notification preferences UI
- [ ] Implement approval alerts
- [ ] Add digest email scheduling

**Phase 6 Tasks (Dashboards):**
- [ ] Complete executive dashboard layout
- [ ] Integrate MudBlazor charts
- [ ] Build CSV/PDF export
- [ ] Implement scheduled report delivery
- [ ] Performance optimization for large datasets

**Blocked:** None reported

---

## Key Metrics

| Metric | Target | Current | Status |
|--------|--------|---------|--------|
| Test Coverage | >80% | ~75% | 🔄 Improving |
| Build Time | <5 min | ~4:30 min | ✅ On track |
| API Response Time (p95) | <500ms | ~450ms | ✅ Good |
| UI Load Time | <2s | ~1.8s | ✅ Good |
| Accessibility Score (Lighthouse) | >90 | ~87 | 🔄 Improving |

---

## Release Cadence

- **Patches (v1.0.x):** Weekly (security, hotfixes)
- **Minor (v1.1.0):** Bi-weekly (new features, Phase completions)
- **Major (v2.0.0):** Quarterly (architecture changes)

---

## Questions?

- **Phase specs?** → See DEVELOPMENT-PHASES.md
- **Blocked?** → Update DEPENDENCIES-AND-BLOCKERS.md
- **Agent assignment?** → Check RESOURCE-ALLOCATION.md
- **Risk concern?** → Document in RISK-MITIGATION.md
