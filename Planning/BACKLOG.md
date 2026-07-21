# MyDesk Development Backlog

**Current Status:** Phases 1-6 Shipped | Phase 7 In Progress | Phases 8-10 Planned  
**Last Updated:** July 21, 2026  
**Active Sprint:** July 21 - August 4, 2026

---

## What is This?

This backlog consolidates work items across all development phases. For detailed specifications, refer to **DEVELOPMENT-PHASES.md**. For phase timelines and ownership, see **RESOURCE-ALLOCATION.md**. For active blockers, see **blockers.md**.

---

## ✅ Shipped (v1.0.0)

### Phase 1: Core Expense Management
- ✅ User expense submission with photo capture
- ✅ OCR receipt extraction (OpenAI Vision)
- ✅ Multi-currency support (AUD, USD, EUR, GBP)
- ✅ Receipt attachment storage
- ✅ Expense history and search

### Phase 2: Multi-Level Approvals
- ✅ Manager approval workflows
- ✅ Director escalation rules
- ✅ Approval chains & delegation
- ✅ Approval audit trail
- ✅ Bulk approval
- ✅ Rejection with feedback

### Phase 3: Integrations
- ✅ MYOB GL export integration
- ✅ Xero invoice sync
- ✅ Bank reconciliation dashboard
- ✅ Outlook add-in (3 workflows)
- ✅ Email receipt capture
- ✅ MCP integration layer

### Phase 4: Teams & Departments
- ✅ Organizational hierarchy (Teams, Departments)
- ✅ Budget allocation by department
- ✅ Budget reporting & forecasting
- ✅ Department-level approval workflows
- ✅ Bulk user import (CSV)
- ✅ Approval delegation & escalation

### Phase 5: Notifications & Alerts
- ✅ Email notifications (SendGrid + SMTP fallback)
- ✅ SMS notifications (Twilio)
- ✅ In-app notification center
- ✅ Notification preferences & quiet hours
- ✅ Approval request alerts
- ✅ Status change notifications
- ✅ Real-time SignalR updates
- ✅ Background job delivery (Hangfire)

### Phase 6: Dashboard & Analytics
- ✅ Executive dashboard (CFO)
- ✅ Manager dashboard (Team Lead)
- ✅ Employee dashboard (User)
- ✅ MudBlazor charts (bar, line, pie)
- ✅ CSV/PDF/JSON export
- ✅ Scheduled report delivery
- ✅ Custom report templates
- ✅ Analytics notification service
- ✅ Dashboard preferences

---

## 🔄 In Progress (Current Sprint)

### Phase 7: Mobile & Desktop Shell

**Owner:** Mobile & Cross-Platform Agent  
**Target:** Q2 2027  
**Progress:** ~17% (23/140 tasks)

#### Mobile (React Native + TypeScript)
- 🔄 Core navigation and screen structure
- 🔄 Authentication flow (PAT)
- 🔌 Offline-first expense submission
- 🔌 Mobile camera integration (receipt capture)
- 🔌 Push notifications
- 🔌 Mobile approval workflows
- 🔌 Sync with cloud backend
- 🔌 iOS App Store & Android Play Store deployment

#### Desktop Shell (WPF/.NET)
- 🔄 Desktop companion app (MyDesk.Browser)
- 🔄 Desktop share / screen sharing
- 🔄 Support ticket integration
- 🔄 Native Windows notifications
- 🔌 Offline access to recent data

**Blockers:** See [blockers.md](./blockers.md)

---

## 📋 Planned (Backlog)

### Phase 8: AI-Powered Receipt Processing
**Target:** Q2 2027  
**Estimated Effort:** 8-12 weeks

- 📋 Receipt image preprocessing (rotation, crop)
- 📋 Supplier & merchant name extraction
- 📋 Date & amount parsing
- 📋 GST/tax detection
- 📋 Line item extraction
- 📋 Confidence scoring & manual override UI
- 📋 ML model training pipeline

**Dependencies:** Phase 1 (Receipt capture), Phase 6 (Analytics infrastructure)

### Phase 9: Predictive Analytics & Intelligence
**Target:** Q2-Q3 2027  
**Estimated Effort:** 10-14 weeks

- 📋 Predictive budget forecasting
- 📋 Spend anomaly detection
- 📋 Department spending trends
- 📋 Seasonal pattern analysis
- 📋 Custom report builder
- 📋 BI tool integration (Tableau/Power BI)

**Dependencies:** Phase 6 (Dashboards)

### Phase 10: Procurement & Multi-Org Management
**Target:** Q3 2027+  
**Estimated Effort:** 14-18 weeks

- 📋 Purchase requisition workflow
- 📋 Vendor management
- 📋 Purchase order creation
- 📋 Three-way matching
- 📋 Supplier performance tracking
- 📋 Contract management
- 📋 Multi-organization support
- 📋 Consolidated reporting

**Dependencies:** Phase 4 (Org structure)

---

## 🎯 High Priority Fixes & Improvements

### Security
- [ ] Field-level encryption for PII (in progress — Phase 6 security review)
- [ ] API key rotation mechanism
- [ ] GDPR data export functionality
- [ ] Rate limiting on all API endpoints
- [ ] Security headers (CSP, HSTS, X-Frame-Options)

### Performance
- [ ] Database query indexes for reporting tables
- [ ] Redis caching for dashboards (evaluated, deferred to Phase 7)
- [ ] Lazy-load large reports (pagination)
- [ ] Optimize PDF generation (async background jobs)
- [ ] Chart rendering performance at scale

### UX & Accessibility
- [ ] Mobile-responsive dashboard redesign
- [ ] Dark mode support
- [ ] WCAG 2.1 AA compliance audit
- [ ] Keyboard navigation on all dialogs
- [ ] Loading states for all async operations

### Code Quality
- [ ] Remove async void in browser app (Converters.cs, ViewModels)
- [ ] Replace `.Result` usage in McpIntegrationService with async/await
- [ ] Standardize claim types (`user_id` vs `sub` vs `ClaimTypes.NameIdentifier`)
- [ ] Add structured logging to all services
- [ ] Remove empty catch blocks (FinancialExtractionService)

### Documentation
- [ ] API OpenAPI/Swagger docs
- [ ] User guides for each module
- [ ] Admin onboarding guide
- [ ] Deployment runbooks

---

## Work Item Tracking

### How to Use This Backlog
1. **Find your phase** in the roadmap
2. **Check current status** (In Progress, Planned, etc.)
3. **Review acceptance criteria** in DEVELOPMENT-PHASES.md
4. **Check blockers** in [blockers.md](./blockers.md)
5. **Claim ownership** in RESOURCE-ALLOCATION.md
6. **Open a feature branch** and link to this backlog

### Submitting Work
```bash
# 1. Create feature branch
git checkout -b feature/phase7-mobile-auth

# 2. Link backlog item in commit message
git commit -m "feat(phase7): implement mobile PAT authentication

Backlog: Phase 7 / Authentication flow
Closes #42"

# 3. Create PR with backlog reference
gh pr create --title "Phase 7: Mobile PAT Authentication"
```

---

## Current Sprint Focus (July 21 - August 4, 2026)

**Phase 7 Tasks (Mobile & Desktop):**
- [ ] Complete mobile navigation structure
- [ ] Implement mobile expense list screen
- [ ] Integrate mobile notifications
- [ ] Desktop shell: basic navigation
- [ ] Fix code quality issues from audit

**Blocked:** See [blockers.md](./blockers.md)

---

## Key Metrics

| Metric | Target | Current | Status |
|--------|--------|---------|--------|
| Test Coverage | >80% | ~75% | 🔄 Improving |
| Build Time | <10 min | ~4:30 min | ✅ On track |
| API Response Time (p95) | <500ms | ~450ms | ✅ Good |
| UI Load Time | <2s | ~1.8s | ✅ Good |
| Accessibility Score | >90 | ~87 | 🔄 Improving |
| Open Blockers | 0 | 3 | ⚠️ Needs attention |

---

## Release Cadence

- **Patches (v1.0.x):** Weekly (security, hotfixes)
- **Minor (v1.1.0):** Bi-weekly (new features, Phase completions)
- **Major (v2.0.0):** Quarterly (architecture changes)

---

## Questions?

- **Phase specs?** → See DEVELOPMENT-PHASES.md
- **Blocked?** → See [blockers.md](./blockers.md)
- **Agent assignment?** → Check RESOURCE-ALLOCATION.md
- **Risk concern?** → Document in RISK-MITIGATION.md
- **CI failing?** → See .github/workflows/ and CLAUDE.md
