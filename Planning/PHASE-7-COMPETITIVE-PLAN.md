# Phase 7 Mobile App: Competitive Continuation Plan

**Version:** 1.0  
**Date:** July 2026  
**Status:** In Progress (Tasks 21-45 complete, Tasks 46+ queued)

---

## Executive Summary

Based on competitive analysis of Expensify, Concur, Zoho Expense, Rydoo, Fyle, and Brex/Ramp, MyDesk has **four defensible competitive advantages** that justify a refined Phase 7 sequence:

1. **Australian compliance-first architecture** (ATO/GST/BAS native)
2. **True offline-first architecture** (SQLite + background sync)
3. **Department budget enforcement** (Phase 4 delivered)
4. **Multi-level approval delegation** (Phase 2 + 4 delivered)

---

## Competitive Positioning Matrix

| Feature Area | Expensify | Concur | Zoho | Fyle | Brex/Ramp | **MyDesk Advantage** |
|-------------|-----------|--------|------|------|-----------|----------------------|
| **OCR Accuracy** | Best-in-class | Good | Average | Good | Average | **Match Expensify, exceed AUS formats** |
| **Offline Support** | Limited | None | Basic | Basic | None | **Full offline-first (unique)** |
| **AUS Tax Compliance** | Weak | Weak | Partial | Weak | US-only | **Native ATO/GST/FBT** |
| **Budget Enforcement** | Policy rules | Complex | Basic | Rules | Card-linked | **Dept budgets + delegation** |
| **Approval Workflows** | Linear | Enterprise | Basic | Rules engine | Limited | **Multi-level + delegation + OOO** |
| **Mobile UX** | Good | Legacy | Good | Modern | Modern | **Platform-native + offline** |

---

## Revised Phase 7 Sprint Plan (6 Sprints / 12 Weeks)

### Sprint 1-2 (Weeks 1-4): Expense Submission Complete
**Focus:** Production-ready expense flow with AUS compliance

| Task | Description | Competitive Angle |
|------|-------------|-------------------|
| 24 ✅ | Camera capture + preview + crop | Native camera, not web view |
| 25 🔄 | OCR with AUS receipt formats | Extract ABN, GST, ATO categories |
| 26 | Multi-receipt attachment | Per-expense gallery |
| 27 | Real-time validation + Australian formats | AUD/$, ATO category mapping |
| 28 | Auto-save drafts (30s interval) | SQLite persistence |
| 29 ✅ | Submission confirmation | Shows sync status |
| 30 ✅ | Offline queue + conflict resolution | Background sync with retry |

**Deliverable:** Employee can submit expense on airplane mode, auto-syncs on landing

---

### Sprint 3-4 (Weeks 5-8): Manager Approval Mobile UX
**Focus:** Approve from lock screen, SLA visibility

| Task | Description | Competitive Angle |
|------|-------------|-------------------|
| 46-48 | Approval list + detail + inline actions | Swipe gestures, native UX |
| 49-50 | Comments + bulk approval | Thread support, select-all |
| 51-52 | SLA tracking + priority badges | Color-coded urgency |
| 53-54 | Delegation + OOO workflows | Unique delegation depth |
| 55 | Approval history + metrics | Personal + team view |
| **NEW** | Push action buttons (Approve/Reject) | **Fyle parity + offline queue** |

**Deliverable:** Manager approves 5 expenses in 10 seconds from notification shade

---

### Sprint 5 (Weeks 9-10): Australian Compliance Features
**Focus:** Unique market differentiator

| Feature | Description | Competitive Angle |
|---------|-------------|-------------------|
| ATO GST extraction | Auto-calc from OCR | 10% GST auto-split |
| ATO per-diem rates | 2024/25 rates baked in | Meal/accom rates auto-applied |
| FBT category tagging | Entertainment/travel split | Employer reporting ready |
| BAS-ready export | GST summary by period | Accountant handoff |
| ABN validation | Supplier lookup | Prevents invalid claims |

**Deliverable:** Expense auto-categorizes for Australian tax, zero manual GST entry

---

### Sprint 6 (Weeks 11-12): Analytics + Polish
**Focus:** Phase 6 parity + mobile optimization

| Task | Description | Competitive Angle |
|------|-------------|-------------------|
| 76-78 | Dashboard layout + charts | Recharts Native, touch-interactive |
| 79-80 | Category breakdown + budget widget | Real-time Phase 4 budget sync |
| 81-83 | Recent expenses + monthly summary | Pull-to-refresh, offline cached |
| 84-85 | Customization + offline view | Cached charts, sync indicator |
| 86-90 | Manager team view | Team drill-down, cost/employee |

**Deliverable:** CFO/Manager sees real-time team spend on mobile, offline-capable

---

## Technical Architecture Decisions (Locked)

| Area | Decision | Rationale |
|------|----------|-----------|
| **State** | Redux Toolkit + RTK Query | Type-safe, dev tools, cache |
| **Offline** | SQLite + Redux Persist + custom queue | Proven, ACID, migration support |
| **Charts** | Recharts Native | Touch-friendly, tree-shakeable |
| **Auth** | SecureStore + JWT + refresh | Platform keystore, no plaintext |
| **OCR** | OpenAI Vision API (backend) | Best accuracy, multi-format |
| **Notifications** | FCM + local SQLite store | 7-day retention, action buttons |
| **CI/CD** | EAS Build + GitHub Actions | Automated, signed, staged |

---

## Risk Mitigation (Phase 7 Specific)

| Risk | Likelihood | Impact | Mitigation |
|------|------------|--------|------------|
| iOS App Store OCR rejection | Medium | High | Pre-flight test with Apple guidelines, fallback to gallery-only |
| Android camera permission changes | Medium | Medium | Runtime permission handling + graceful fallback |
| OCR cost at scale | Low | Medium | Caching + confidence threshold (skip <60%) |
| Offline sync conflicts | Medium | High | Timestamp + user resolution UI (Task 96) |
| Bundle size > 30MB | Low | Medium | Lazy-load charts, analyze with `expo analyze` |

---

## Success Metrics (Phase 7)

| Metric | Target | Measurement |
|--------|--------|-------------|
| **Crash-free rate** | 99.5% | Sentry + Play Console |
| **App startup (cold)** | < 3s | Firebase Performance |
| **Expense submit (offline)** | < 2s | Internal timer |
| **Approval action (push)** | < 1s | FCM delivery + app open |
| **30-day retention** | 80% | Firebase Analytics |
| **iOS/Android parity** | 100% feature | Test matrix |
| **Australian tax accuracy** | 100% ATO test suite | Automated test cases |

---

## Resource Allocation

| Role | Weeks 1-4 | Weeks 5-8 | Weeks 9-10 | Weeks 11-12 |
|------|-----------|-----------|------------|-------------|
| React Native Lead | 1.0 | 1.0 | 1.0 | 1.0 |
| Frontend Dev | 1.0 | 1.0 | 0.5 | 1.0 |
| Backend Liaison | 0.5 | 0.5 | 0.5 | 0.5 |
| QA Engineer | 0.5 | 1.0 | 1.0 | 1.0 |
| **Total FTE** | **3.0** | **3.5** | **3.0** | **3.5** |

---

## Immediate Next Actions (This Week)

1. [ ] **Complete Task 25**: ATO-compliant OCR extraction (ABN, GST, date parsing)
2. [ ] **Complete Task 26**: Multi-receipt attachment with thumbnail gallery
3. [ ] **Complete Task 27**: Real-time validation (AUD format, ATO category mapping)
4. [ ] **Complete Task 28**: Auto-save draft (30s debounce, SQLite, recovery on resume)
5. [ ] **Create PR** for expense submission flow with test coverage
6. [ ] **EAS Build** internal testflight for camera/OCR validation

---

## Phase 8 Preview (Post-Mobile Launch)

| Quarter | Initiative | Rationale |
|---------|------------|-----------|
| Q1 2027 | AI Receipt Intelligence | Build on OCR pipeline, categorization ML |
| Q1 2027 | Corporate Card Integration | Direct feed (Brex/Ramp parity) |
| Q2 2027 | Predictive Budget Alerts | Leverage Phase 6 analytics + mobile push |
| Q2 2027 | Multi-org Support | Enterprise expansion |

---

*This plan aligns Phase 7 execution with verified competitive gaps. Each sprint delivers a market-differentiating capability, not just feature parity.*