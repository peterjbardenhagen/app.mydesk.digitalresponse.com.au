# MyDesk Web — Wireframe Prototype

**Platform:** Blazor Server (.NET 8)  
**Version:** 1.0 | July 2026

## Overview

The MyDesk web application is the primary workspace for the Enterprise AI Brain platform. All major business modules are accessible from the sidebar navigation.

## Modules Covered

| Module | Status |
|--------|--------|
| Dashboard — KPI tiles, AgentsOS summary, upcoming tasks | ✅ |
| Companies (CRM) — Master-detail with search, ABN, phone, status | ✅ |
| Quotes — Sales pipeline with conversion rate and export | ✅ |
| Invoices — Tabular listing with status chips (Paid, Pending, Overdue) | ✅ |
| Expenses — Tab-filterable (All, Draft, Submitted, Approved, Rejected) | ✅ |
| Timesheets — Week view with hours, approve/reject actions | 🔄 |
| AgentsOS DAG — Node-based workflow visualizer with gated approvals | ✅ |
| Approvals — Human-in-loop approve/reject | ✅ |
| Banking — Financial dashboard | ❌ |
| Settings — Account, notifications, tenant config | 🔄 |
| Admin / Tenant Management — User roles, permissions | ❌ |

## Key Layout

- **Sidebar navigation** — Fixed left rail with module icons and labels
- **KPI header row** — Top-level metrics across all modules
- **Table-based data** — Sortable, searchable data tables with status badges
- **Modal dialogs** — For create/edit forms and approval actions

## Viewing

Open `Design/web/index.html` in any browser. Navigation links at top jump between sections.

## Next Steps

1. Timesheets full view with weekly calendar
2. Banking integration dashboard
3. Admin panel for tenant management
4. Dark mode toggle
5. Accessibility improvements (keyboard nav, screen reader support)
