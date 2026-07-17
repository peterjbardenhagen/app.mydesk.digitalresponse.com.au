# Phase 5-6 Completion Plan: 100 Tasks

**Approach:** Close PR #21, audit Phase 5, rebuild Phase 6 cleanly  
**Duration:** ~8 days  
**Status:** Planning  
**Last Updated:** July 17, 2026

---

## Phase 5: Notifications - Completion Audit (Tasks 1-15)

### Verify Phase 5 Status
1. Check PR #15 merge status (notifications-system) - already merged
2. Review Phase 5 feature checklist in BACKLOG.md
3. List all Phase 5 files changed in main branch
4. Verify EmailNotificationService implementation exists
5. Verify SmsNotificationService (Twilio) implementation status
6. Check NotificationCenter UI component in Components/
7. Verify notification preferences storage & retrieval
8. Check approval request notification triggers
9. Verify status change notification flows
10. Check digest email scheduling (Hangfire job)
11. Review notification audit logging
12. Verify timezone handling for quiet hours
13. Test Phase 5 endpoints: GET/POST /api/notifications/preferences
14. Verify Phase 5 database schema migrations
15. Document any Phase 5 gaps found in BLOCKERS.md

### Phase 5 Gap Fixing (Tasks 16-25)
16. If SMS integration incomplete: implement Twilio SMS service wrapper
17. If digest emails incomplete: implement DigestEmailSchedulerJob
18. If quiet hours incomplete: add QuietHoursService with time validation
19. If notification preferences incomplete: complete PreferencesController
20. If audit logging incomplete: add notifications to ComplianceAuditLog
21. Run Phase 5 unit tests (create if missing)
22. Run Phase 5 integration tests
23. Test notification delivery end-to-end
24. Verify notification preferences UI functionality
25. Document Phase 5 completion in IMPLEMENTATION-TIMELINE.md

---

## Phase 6: Fresh Start - Foundation (Tasks 26-40)

### Close PR #21 & Branch Cleanup
26. Close PR #21 (Phase 6 draft) without merging
27. Delete local branch: work/phase6-dashboards
28. Keep origin/claude/deploy-mydesk-iis-dns-6o5qn0 as archive (reference only)
29. Fetch latest origin/main
30. Create fresh feature branch: feature/phase6-dashboards

### Define Phase 6 Scope (Tasks 31-40)
31. Review Phase 6 requirements in DEVELOPMENT-PHASES.md
32. Extract dashboard feature list from old branch commits
33. Prioritize Phase 6 features into MVP vs. nice-to-have
34. **MVP Features (Required for Phase 6):**
    - Executive KPI dashboard (spend, trends, forecasts)
    - Manager dashboard (team spend, approvals)
    - Employee dashboard (my expenses, reimbursements)
    - MudBlazor chart integration
    - CSV export
    - PDF export (QuestPDF)
    - Scheduled report delivery (Hangfire)
35. Document MVP vs. Phase 7 features
36. Create GitHub issue for Phase 6 epic
37. Break Phase 6 into sub-tasks (tasks 41-100)
38. Estimate effort: dashboards (2 days), charts (1 day), export (1 day), reports (1 day)
39. Plan code review schedule
40. Identify test requirements for Phase 6

---

## Phase 6: Architecture & Setup (Tasks 41-55)

### Database Schema (Tasks 41-45)
41. Review existing DashboardService schema requirements
42. Create migration: DashboardPreferences table
43. Create migration: ReportTemplates table
44. Create migration: ScheduledReports table
45. Add indexes on commonly queried columns (TenantId, UserId, CreatedAt)

### Service Layer (Tasks 46-50)
46. Create DashboardService (core aggregation logic)
47. Create AnalyticsService (calculations, anomaly detection)
48. Create ReportService (template rendering, scheduling)
49. Create ExportService (CSV, PDF, JSON formats)
50. Wire services into Program.cs dependency injection

### API Controllers (Tasks 51-55)
51. Create DashboardController (GET endpoints for each dashboard type)
52. Create AnalyticsController (anomaly detection, drill-down)
53. Create ReportController (CRUD for templates and schedules)
54. Create ExportController (CSV/PDF/JSON generation)
55. Add authorization checks to all endpoints

---

## Phase 6: Component Development (Tasks 56-75)

### Razor Components - Dashboards (Tasks 56-62)
56. Create Dashboard.razor (main page router)
57. Create ExecutiveDashboard.razor (KPI dashboard)
58. Create ManagerDashboard.razor (team oversight)
59. Create EmployeeDashboard.razor (personal expenses)
60. Create DashboardOverview.razor (summary view)
61. Create DashboardPreferences.razor (user settings)
62. Add responsive layout (mobile-first CSS)

### Chart Components (Tasks 63-68)
63. Set up MudBlazor Chart library
64. Create ChartBase.razor component
65. Create SpendByCategory chart
66. Create SpendByDepartment chart
67. Create BudgetVsActual chart
68. Create Trend/Forecast chart

### Export Components (Tasks 69-72)
69. Create ExportDialog.razor (format selection)
70. Create CSV export templates
71. Create PDF export layout (QuestPDF)
72. Create JSON export serialization

### Settings & Preferences (Tasks 73-75)
73. Create NotificationPreferences component
74. Create ReportPreferences component
75. Create DisplaySettings component

---

## Phase 6: Feature Implementation (Tasks 76-90)

### CSV Export (Tasks 76-78)
76. Implement CSV builder in ExportService
77. Add export endpoint: POST /api/reports/{id}/export/csv
78. Test CSV output (headers, formatting, special chars)

### PDF Export (Tasks 79-82)
79. Set up QuestPDF document builder
80. Create PDF layout template (header, KPI band, table, footer)
81. Implement PDF generation in ExportService
82. Add export endpoint: POST /api/reports/{id}/export/pdf

### Scheduled Reports (Tasks 83-86)
83. Create Hangfire recurring job for daily reports
84. Implement email delivery via EmailService
85. Create report scheduling UI
86. Add job monitoring dashboard

### Anomaly Detection (Tasks 87-90)
87. Implement anomaly detection algorithm (spend outliers)
88. Add AlertService for threshold breaches
89. Create anomaly alert notification
90. Add anomaly drill-down UI

---

## Phase 6: Testing & Quality (Tasks 91-100)

### Unit Tests (Tasks 91-94)
91. Test DashboardService data aggregation
92. Test AnalyticsService calculations
93. Test ExportService (CSV, PDF, JSON)
94. Test ReportService scheduling

### Integration Tests (Tasks 95-97)
95. Test full dashboard rendering
96. Test export file generation end-to-end
97. Test scheduled report delivery

### Performance & Security (Tasks 98-100)
98. Load test dashboards with large datasets
99. Security audit: verify tenant isolation, auth enforcement
100. Performance optimization: add caching, pagination for large reports

---

## Execution Timeline

| Phase | Tasks | Duration | Start | End |
|-------|-------|----------|-------|-----|
| Phase 5 Audit | 1-25 | 1 day | Day 1 | Day 1 |
| Phase 6 Setup | 26-55 | 2 days | Day 2 | Day 3 |
| Phase 6 Build | 56-90 | 4 days | Day 4-7 | Day 7 |
| Phase 6 QA | 91-100 | 1 day | Day 8 | Day 8 |
| **Total** | **100** | **~8 days** | | |

---

## Critical Path

**Must Complete in Order:**
1. Phase 5 audit (discover gaps)
2. Phase 5 gap fixes (if needed)
3. Phase 6 database schema (before services)
4. Phase 6 services (before components)
5. Phase 6 components (dashboard pages)
6. Phase 6 features (export, reports, analytics)
7. Phase 6 testing (unit, integration, performance)

---

## Git Workflow

```bash
# Day 1: Phase 5 Audit
git checkout main && git pull
# ... document Phase 5 status ...
# Commit findings to BLOCKERS.md if needed

# Day 2-3: Phase 6 Setup
git checkout -b feature/phase6-dashboards
# ... schema migrations ...
git commit -m "feat(phase6): add dashboard tables and migrations"

# Day 4-7: Phase 6 Build
# Regular commits for each component/feature
git commit -m "feat(phase6): add executive dashboard component"

# Day 8: Testing & Merge
git commit -m "test(phase6): add integration tests"
git push -u origin feature/phase6-dashboards
gh pr create --draft --title "Phase 6: Dashboards & Analytics"
# ... resolve CI issues ...
gh pr ready
# ... await review & merge ...
```

---

## Deliverables

- ✅ Phase 5 completion verified (or gaps documented)
- ✅ Phase 6 branch with clean commit history (56 targeted commits, not 126)
- ✅ All CI tests passing
- ✅ PR #22 (Phase 6) merged to main
- ✅ IMPLEMENTATION-TIMELINE.md updated with actual dates
- ✅ Planning/BACKLOG.md marked as "Phases 1-6 Complete"

---

## Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| Phase 5 has gaps | Complete audit in Task 1-25 before starting Phase 6 |
| Merge conflicts | Start fresh on clean main - no rebasing 126 commits |
| CI failures | Build incrementally with tests at each stage |
| Performance | Load test dashboards before merge (Task 98) |
| Security issues | Tenant isolation audit before merge (Task 99) |

---

## Status Tracking

Use this table to track progress as work proceeds:

| Task Range | Description | Status | Assignee | Completed |
|-----------|-------------|--------|----------|-----------|
| 1-25 | Phase 5 Audit & Fixes | 🔄 In Progress | - | - |
| 26-40 | Phase 6 Foundation | 📋 Blocked | - | - |
| 41-55 | Phase 6 Architecture | 📋 Blocked | - | - |
| 56-75 | Phase 6 Components | 📋 Blocked | - | - |
| 76-90 | Phase 6 Features | 📋 Blocked | - | - |
| 91-100 | Phase 6 Testing | 📋 Blocked | - | - |

**Legend:** 🔄 In Progress | ✅ Completed | 📋 Blocked | ⏳ Queued

---

## References

- [ROADMAP.md](./ROADMAP.md) — Product roadmap
- [BACKLOG.md](./BACKLOG.md) — Work items tracking
- [AGENTS.md](./AGENTS.md) — Agent assignments
- [DEVELOPMENT-PHASES.md](../docs/DEVELOPMENT-PHASES.md) — Phase specifications
