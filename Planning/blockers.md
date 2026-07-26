# MyDesk Active Blockers

**Last Updated:** July 26, 2026  
**Review Frequency:** Daily standup + weekly deep dive  
**Escalation Path:** Agent → Orchestrator → Product Lead

---

## 🔴 Critical Blockers

### ~~1. Mobile Notification Integration~~ ✅ RESOLVED
**Resolution:** NotificationsScreen implemented with FCM integration, expo-notifications, and deep linking. EAS Build config added. All 10 mobile screens complete.

### 2. Desktop Shell Feature Parity
**Blocking:** Phase 7 desktop companion adoption  
**Impact:** MyDesk.Browser is prototype-only, missing core features  
**Dependency:** Phase 6 dashboards + Phase 5 notifications  
**Owner:** Desktop Agent  
**Unblock Action:**
- [ ] Port authentication flow from web to WPF
- [ ] Implement expense list and detail views
- [ ] Add notification tray integration
- [ ] Test offline mode with local SQLite cache  
**Target Unblock:** August 15, 2026

---

## 🟡 High Priority Blockers

### ~~3. Code Quality: Claim Type Inconsistency~~ ✅ RESOLVED
**Resolution:** Verified codebase uses `ClaimTypes.NameIdentifier` consistently across all 30+ references.

### ~~4. Code Quality: Async Anti-patterns~~ ✅ RESOLVED
**Resolution:** `.Result` usage in McpIntegrationService.cs confirmed as property access (not Task.Result). `async void` patterns are standard WPF event handlers.

### 5. PDF Export Library Decision
**Blocking:** Phase 6 scheduled report delivery  
**Impact:** QuestPDF chosen but not validated for complex reports  
**Dependency:** Phase 6 analytics team evaluation  
**Owner:** Analytics Agent  
**Unblock Action:**
- [ ] Complete QuestPDF evaluation (complex tables, charts)
- [ ] Benchmark against iTextSharp alternative
- [ ] Make final decision by August 1
- [ ] If QuestPDF insufficient: add iText via NuGet  
**Target Unblock:** August 1, 2026
**Blocking:** Phase 6 scheduled report delivery  
**Impact:** QuestPDF chosen but not validated for complex reports  
**Dependency:** Phase 6 analytics team evaluation  
**Owner:** Analytics Agent  
**Unblock Action:**
- [ ] Complete QuestPDF evaluation (complex tables, charts)
- [ ] Benchmark against iTextSharp alternative
- [ ] Make final decision by August 1
- [ ] If QuestPDF insufficient: add iText via NuGet  
**Target Unblock:** August 1, 2026

---

## 🟢 Low Priority / Informational

### 6. SMS Templates Not Designed
**Blocking:** None (text-only fallback acceptable)  
**Impact:** SMS notifications use plain text only  
**Owner:** Notification Agent  
**Mitigation:** Text-only SMS is acceptable for MVP  
**Target:** August 15, 2026

### 7. ML Infrastructure for Phase 8
**Blocking:** Phase 8 AI receipt parsing  
**Impact:** No ML training pipeline or labeled dataset  
**Owner:** AI/ML Agent  
**Mitigation:** Can use OpenAI Vision API as interim solution  
**Target:** Q4 2026

### 8. GDPR Data Export
**Blocking:** Compliance certification  
**Impact:** Cannot demonstrate GDPR compliance without data export  
**Owner:** Security Agent  
**Mitigation:** SQL scripts can generate exports manually for now  
**Target:** August 31, 2026

---

## Open PRs Requiring Action

| PR | Title | Status | Action Required |
|----|-------|--------|-----------------|
| #22 | Bump uuid (mobile) | Open | Approve & merge |
| #20 | Bump nuget group | Open | Approve & merge |

---

## Recent Blockers Resolved

| Date | Blocker | Resolution |
|------|---------|-----------|
| 2026-07-21 | PR #21 merge conflicts | Resolved — merged to main |
| 2026-07-17 | MyDesk.Browser build errors | Resolved — fixed naming conflicts |
| 2026-07-17 | ChartSeries ambiguity | Resolved — explicit using directives |
| 2026-07-17 | Service duplication | Resolved — removed duplicates |

---

## Blocker Definitions

| Severity | Criteria | Response Time |
|----------|----------|---------------|
| 🔴 Critical | Blocks Phase 7+ development, production outage | 4 hours |
| 🟡 High | Blocks PR merge, security issue, significant UX | 24 hours |
| 🟢 Low | Non-blocking, workaround available | 1 week |

---

## How to Report a Blocker

1. **Document in this file** with:
   - What's blocking
   - Impact (who/what is blocked)
   - Owner (who can fix)
   - Unblock action (specific steps)
   - Target date

2. **Notify in team channel** with `@mention` of owner

3. **Escalate if unresolved >24hrs** to Orchestrator

---

## Questions?

- **"What's blocking me?"** → Check this file or DEPENDENCIES-AND-BLOCKERS.md
- **"Who can unblock X?"** → See Owner column above
- **"When will Y be unblocked?"** → See Target Unblock date
