# Phase 6: Dashboard & Analytics Implementation Guide

**Version:** 1.0  
**Last Updated:** July 2026  
**Status:** In Progress (Feature Development)

---

## Overview

Phase 6 focuses on building comprehensive dashboards and analytics capabilities for MyDesk. The implementation provides three role-based dashboard views with mobile-first responsive design and real-time metrics.

---

## Implemented Features

### 1. Executive Dashboard (CFO View)

**Route:** `/dashboards/executive`

**Purpose:** Provides C-suite overview of organizational spending, approvals, and budget management.

**Components:**
- `src/MyDesk.Web/Components/Pages/Dashboard/ExecutiveDashboard.razor` (Main component)
- `src/MyDesk.Web/Components/Shared/MetricCard.razor` (Reusable metric display)

**Metrics Displayed:**
- Total Expenses Month-to-Date (primary KPI)
- Pending Approvals (count)
- Approved This Month (count)
- Average Approval Time (hours)
- Expenses by Department (breakdown with progress bars)
- Expenses by Category (Meals, Travel, Equipment breakdown)
- Budget vs Actual by Department (table with utilization percentage)

**Mobile Optimization:**
- Responsive grid: xs=12 (full width), sm=6, md=3 for metric cards
- Metric cards stack vertically on mobile
- Tables include horizontal scroll on mobile
- Progress bars for visual hierarchy
- Color-coded budget status (green <50%, yellow <80%, red ≥80%)

---

### 2. Manager Dashboard (Team View)

**Route:** `/dashboards/manager`

**Purpose:** Enables managers to monitor team spending and approve pending expenses.

**Components:**
- `src/MyDesk.Web/Components/Pages/Dashboard/ManagerDashboard.razor` (Main component)
- Uses `MetricCard` component for consistent styling

**Metrics Displayed:**
- Team Members Count
- Team Expenses Month-to-Date
- Pending Approvals (count)
- Overdue Items (count requiring action)
- Team Spending by Category
- Overdue Approvals with Employee Names (list with days overdue)

**Features:**
- Quick action buttons (View All, Review Pending, Export)
- Overdue items highlighted with error color
- Team spending breakdown with progress bars

**Mobile Optimization:**
- Responsive grid: xs=12 (full width), sm=6, md=3 for metric cards
- Quick action buttons stack vertically on mobile
- List items display cleanly on small screens

---

### 3. Employee Dashboard (Personal View)

**Route:** `/dashboards/employee`

**Purpose:** Allows employees to track their expense submissions and reimbursement status.

**Components:**
- `src/MyDesk.Web/Components/Pages/Dashboard/EmployeeDashboard.razor` (Main component)
- Uses `MetricCard` component for consistent styling

**Metrics Displayed:**
- Submitted This Month (count)
- Approved (count)
- Pending Review (count)
- Reimbursed (count)
- Recent Expenses Table (last 10 with status)
- Monthly Summary (3-month trend)

**Features:**
- Expense table with description, amount, category, and status
- Status color coding (green=approved, yellow=pending, red=rejected)
- View Details button for each expense
- Submit New Expense button
- Submission Guidelines section with helpful tips

**Mobile Optimization:**
- Responsive grid for metric cards
- Table with horizontal scroll on mobile
- Helpful guidelines section prominent on all sizes

---

## Services

### AnalyticsService

**Location:** `src/MyDesk.Web/Services/AnalyticsService.cs`

**Methods:**
- `GetExecutiveDashboardAsync(tenantId)` → Returns `ExecutiveDashboard` with all CFO metrics
- `GetManagerDashboardAsync(tenantId, managerId)` → Returns `ManagerDashboard` with team metrics
- `GetEmployeeDashboardAsync(tenantId, userId)` → Returns `EmployeeDashboard` with personal metrics

**Models:**
- `ExecutiveDashboard` - CFO metrics and breakdowns
- `ManagerDashboard` - Team oversight data
- `EmployeeDashboard` - Personal expense summary
- `DepartmentMetric` - Department-level aggregates
- `CategoryMetric` - Category-level aggregates
- `BudgetMetric` - Budget allocation vs spending
- `OverdueItem` - Overdue approval details
- `ExpenseRecord` - Individual expense snapshot
- `MonthlySummary` - Month-level summary

**Current Implementation:** Mock data (stub)
**TODO:** Replace with actual database queries aggregating expense, approval, and budget data

---

## API Endpoints

### AnalyticsController

**Location:** `src/MyDesk.Web/Controllers/AnalyticsController.cs`

**Endpoints:**
- `GET /api/analytics/executive-dashboard?tenantId={id}` - Executive dashboard data
- `GET /api/analytics/manager-dashboard?tenantId={id}&managerId={id}` - Manager dashboard data
- `GET /api/analytics/employee-dashboard?tenantId={id}&userId={id}` - Employee dashboard data
- `GET /api/analytics/export-csv?tenantId={id}&dashboardType=executive` - Export as CSV (not yet implemented)
- `GET /api/analytics/export-pdf?tenantId={id}&dashboardType=executive` - Export as PDF (not yet implemented)

**Authorization:** All endpoints require `[Authorize]` attribute
**TODO:** Implement role-based access control (validate CFO, Manager, Employee roles)

---

## Database Integration

**Current Status:** Mock data from AnalyticsService

**Required Database Queries (TODO):**

### Executive Dashboard
```sql
SELECT 
    SUM(e.Amount) as TotalExpenses,
    COUNT(CASE WHEN a.Status = 'Pending' THEN 1 END) as PendingApprovals,
    COUNT(CASE WHEN a.ApprovedDate >= FirstDayOfMonth() THEN 1 END) as ApprovedThisMonth,
    AVG(DATEDIFF(hour, e.CreatedAt, a.ApprovedDate)) as AvgApprovalTime
FROM dbo.Expenses e
LEFT JOIN dbo.Approvals a ON e.ExpenseId = a.ExpenseId
WHERE e.TenantId = @TenantId AND e.CreatedAt >= FirstDayOfMonth()
GROUP BY e.TenantId
```

### Manager Dashboard
```sql
SELECT 
    t.TeamName,
    COUNT(DISTINCT tm.UserId) as TeamMembersCount,
    SUM(e.Amount) as TeamExpensesMonthToDate
FROM dbo.Teams t
JOIN dbo.TeamMembers tm ON t.TeamId = tm.TeamId
LEFT JOIN dbo.Expenses e ON tm.UserId = e.SubmittedBy AND e.CreatedAt >= FirstDayOfMonth()
WHERE t.TenantId = @TenantId AND t.ManagerId = @ManagerId
GROUP BY t.TeamId, t.TeamName
```

### Employee Dashboard
```sql
SELECT 
    COUNT(*) as SubmittedThisMonth,
    COUNT(CASE WHEN Status = 'Approved' THEN 1 END) as ApprovedThisMonth,
    COUNT(CASE WHEN Status = 'Pending' THEN 1 END) as PendingApproval
FROM dbo.Expenses
WHERE TenantId = @TenantId AND SubmittedBy = @UserId AND CreatedAt >= FirstDayOfMonth()
```

---

## Mobile-First Design Patterns

### Responsive Grid Breakpoints

All dashboard components use MudBlazor's grid system:

```razor
<MudGrid>
    <MudItem xs="12" sm="6" md="3">
        <!-- On mobile (xs): full width (12/12)
             On tablet (sm): half width (6/12)
             On desktop (md): quarter width (3/12)
        -->
    </MudItem>
</MudGrid>
```

### CSS Responsive Classes

```css
.responsive-header {
    flex-wrap: wrap;
    gap: 1rem;
}

@media (max-width: 599px) {
    .responsive-header {
        flex-direction: column;
        align-items: stretch;
    }
    
    :deep(.mud-button) {
        width: 100%;
    }
}
```

### Best Practices Implemented

1. **Cards Stack Vertically** - Metric cards display as single column on mobile
2. **Table Horizontal Scroll** - Tables wrap in `<div style="overflow-x: auto;">` for mobile
3. **Button Full Width** - Buttons expand to full width on mobile screens
4. **Filter Controls Stack** - Multi-column filters become vertical on mobile
5. **Spacing Adjustments** - Reduced padding/margins on small screens
6. **Touch-Friendly** - Minimum touch targets of 44px (MudBlazor default)

---

## Notification Center Enhancement

**Location:** `src/MyDesk.Web/Components/Pages/Notifications/NotificationCenter.razor`

**Enhancements:**
- Mobile-responsive filter section with vertical stacking
- Created `NotificationItem.razor` component for reusability
- Relative time formatting (just now, 5m ago, 2h ago)
- Hover effects for better UX
- Color-coded notification types

---

## Charts & Visualizations

**Current Status:** Placeholder sections (not implemented)

**Planned Implementation:**
- Use MudBlazor Chart components or Chart.js
- Department expense breakdown pie chart
- Category expense breakdown bar chart
- Monthly trend line chart (manager & employee dashboards)
- Budget utilization gauge chart

---

## Export Functionality

**Current Status:** Scaffolded endpoints only

**TODO - CSV Export:**
- Generate comma-separated values
- Include dashboard metrics and detailed breakdowns
- Support all three dashboard types
- Filename: `Dashboard-Executive-2026-07-08.csv`

**TODO - PDF Export:**
- Use QuestPDF (already in dependencies)
- Generate formatted PDF with charts
- Include logo, date, and tenant branding
- Support all three dashboard types
- Filename: `Dashboard-Executive-2026-07-08.pdf`

---

## Real-Time Updates

**Current Status:** Not implemented

**Planned (Phase 6+ future work):**
- SignalR integration for live metric updates
- WebSocket connection to `/dashboards-hub`
- Auto-refresh dashboard every 5 minutes
- Toast notifications when metrics change significantly
- Use `ClientNotificationService` for real-time alerts

---

## Testing

### Unit Tests (TODO)

Location: `tests/MyDesk.Web.Tests/Analytics/`

Test cases needed:
- AnalyticsService metric calculations
- Mock data generation
- Boundary conditions (empty datasets, single record)
- Percentage calculations

### Integration Tests (TODO)

Location: `tests/MyDesk.Web.Tests/Integration/Analytics/`

Test cases needed:
- AnalyticsController authorization
- API response format validation
- Tenant isolation (verify data belongs to correct tenant)
- Export functionality with sample data

### E2E Tests (TODO)

Location: `tests/MyDesk.Web.Tests/E2E/`

Test cases needed:
- Load all three dashboards in browser
- Verify metrics display correctly
- Test mobile responsiveness (using Playwright)
- Test filter interactions (if implemented)
- Verify export buttons work

---

## Security Considerations

### Tenant Isolation

All dashboard methods validate `tenantId` parameter:
```csharp
// ✅ GOOD: All queries filtered by TenantId
WHERE e.TenantId = @TenantId AND ...

// ❌ BAD: Missing tenant filter
SELECT * FROM Expenses WHERE UserId = @UserId
```

### Authorization

API endpoints require `[Authorize]` attribute.

**TODO - Implement role validation:**
```csharp
// CFO/Admin only
if (!User.HasClaim("role", "CFO") && !User.HasClaim("role", "Admin"))
    return Forbid();

// Manager can only see own team
if (managerId != GetCurrentUserId() && !User.HasClaim("role", "Admin"))
    return Forbid();

// Employee can only see own data
if (userId != GetCurrentUserId() && !User.HasClaim("role", "Admin"))
    return Forbid();
```

### Data Sensitivity

- Expenses contain sensitive business information
- Budget data is confidential
- Salaries and cost centers should be restricted to Finance role
- Implement field-level security (some users see all categories, others see filtered)

---

## Performance Optimization

### Query Optimization

Database queries should:
- Use indexes on TenantId, SubmittedBy, CreatedAt, Status columns
- Aggregate data in database, not in application memory
- Use `SELECT SUM()`, `COUNT()` instead of loading all rows

### Caching Strategy

Consider caching for:
- Dashboard metrics (invalidate on expense creation/approval)
- Department/category lists (static, longer TTL)
- Budget allocations (update daily)

Example:
```csharp
var cacheKey = $"dashboard-executive-{tenantId}";
if (!cache.TryGetValue(cacheKey, out ExecutiveDashboard dashboard))
{
    dashboard = await FetchFromDatabase();
    cache.Set(cacheKey, dashboard, TimeSpan.FromMinutes(5));
}
return dashboard;
```

---

## Future Enhancements

1. **Custom Date Ranges** - Allow CFO to compare YTD vs Prior Year
2. **Department Drill-Down** - Click pie chart segment to see department expenses
3. **Approval Workflows Visualization** - Show approval chain status
4. **Budget Forecasting** - Project spending trends
5. **Anomaly Detection** - Alert on unusual spending patterns
6. **Approval SLA Tracking** - Measure approval time vs SLA targets
7. **Cost Center Allocation** - Break down by cost center
8. **Team Comparison** - Benchmark teams against each other

---

## Related Documentation

- **Product Requirements:** `PRODUCT-REQUIREMENTS.md § Phase 6`
- **Solution Architecture:** `solution-architecture.md § Monitoring & Observability`
- **Development Guide:** `CLAUDE.md § Development Workflow`
- **Lessons Learned:** `docs/LESSONS_LEARNED.md`
- **Agents:** `agents.md § 7. Dashboard & Analytics Agent`

---

## Implementation Timeline

| Task | Status | Owner | ETA |
|------|--------|-------|-----|
| Dashboard Blazor components | ✅ Complete | Claude | 2026-07-08 |
| Analytics service (mock data) | ✅ Complete | Claude | 2026-07-08 |
| Analytics API controller | ✅ Complete | Claude | 2026-07-08 |
| Database query implementation | ⏳ Pending | Phase 6 Agent | 2026-07-15 |
| Charts integration | ⏳ Pending | Phase 6 Agent | 2026-07-22 |
| CSV/PDF export | ⏳ Pending | Phase 6 Agent | 2026-07-29 |
| Unit tests | ⏳ Pending | Phase 6 Agent | 2026-08-05 |
| Integration tests | ⏳ Pending | Phase 6 Agent | 2026-08-12 |
| SignalR real-time updates | ⏳ Pending | Phase 6+ | 2026-09-01 |

---

## Deployment Checklist

Before production release:

- [ ] All database queries tested with production-like data volumes
- [ ] Performance benchmarks pass (< 2 second load time on dashboards)
- [ ] Mobile responsiveness verified on iOS Safari, Android Chrome
- [ ] Security audit completed (tenant isolation, role validation)
- [ ] Export functionality tested with various data sets
- [ ] Error handling and fallback UI working
- [ ] Accessibility review (WCAG 2.1 AA compliance)
- [ ] Documentation updated for users
- [ ] Training materials prepared for Finance team

---

**Document Version History:**

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2026-07-08 | Claude | Initial Phase 6 implementation guide with dashboard components, services, and API endpoints |
